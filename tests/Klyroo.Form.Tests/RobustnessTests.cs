using Bunit;
using Klyroo.UI;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class RobustnessTests : BunitContext
{
    private sealed class CapturingLogger : ILoggerProvider, ILogger
    {
        public List<Exception?> Errors { get; } = [];
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error) Errors.Add(exception);
        }
        public void Dispose() { }
    }

    private readonly CapturingLogger _log = new();

    public RobustnessTests()
    {
        Services.AddKlyrooForm();
        Services.AddLogging(b => b.AddProvider(_log));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static DynamicFormController Form(params DynamicFieldSchema[] fields) => new(new DynamicFormSchema { FormKey = "r", Fields = fields });

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Condition not met.");
            await Task.Delay(5, Xunit.TestContext.Current.CancellationToken);
        }
    }

    // A rule the test releases by hand, by the value it was asked about.
    private sealed class Gate
    {
        public List<string> Calls { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public Dictionary<string, TaskCompletionSource<string?>> Pending { get; } = [];

        public DynamicAsyncValidatorRule Rule() => new("gate", async (value, _, ct) =>
        {
            var text = value?.ToString() ?? "";
            var source = Pending[text] = new TaskCompletionSource<string?>();
            Tokens.Add(ct);
            Calls.Add(text);
            await using var registration = ct.Register(() => source.TrySetCanceled(ct));
            return await source.Task;
        }) { Debounce = TimeSpan.Zero };
    }

    // 1. Disabled while submitting, exceptions as messages, cancellation

    [Fact]
    public async Task Every_field_is_disabled_while_a_submission_runs()
    {
        var field = new DynamicFieldSchema { Key = "a" };
        var form = Form(field);
        var release = new TaskCompletionSource<bool>();
        Assert.False(form.IsFieldDisabled(field));

        var submit = form.SubmitAsync((_, _) => release.Task, Xunit.TestContext.Current.CancellationToken);

        Assert.True(form.IsFieldDisabled(field));
        release.SetResult(true);
        await submit;
        Assert.False(form.IsFieldDisabled(field));
    }

    [Fact]
    public async Task Controls_are_disabled_in_the_component_while_the_form_saves()
    {
        var release = new TaskCompletionSource<bool>();
        var form = Form(
            new DynamicFieldSchema { Key = "a", Label = "A" },
            new DynamicFieldSchema { Key = "t", Kind = FormFieldKind.Checkbox, Label = "T" });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form).Add(c => c.OnSave, async (_, _) => await release.Task));

        var submit = cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find("#kly-ff-a").HasAttribute("disabled"));
            Assert.True(cut.Find("#kly-ff-t").HasAttribute("disabled"));
        });
        release.SetResult(true);
        await submit;
        cut.WaitForAssertion(() => Assert.False(cut.Find("#kly-ff-a").HasAttribute("disabled")));
    }

    [Fact]
    public async Task An_exception_in_OnSave_becomes_a_localized_form_error_and_is_logged()
    {
        var form = Form(new DynamicFieldSchema { Key = "a" });
        var cut = Render<KlyDynamicFormEngine>(p => p
            .AddCascadingValue(KlyLocales.Fr)
            .Add(c => c.Form, form)
            .Add(c => c.OnSave, (_, _) => throw new InvalidOperationException("db down")));

        await cut.Find("form").SubmitAsync();

        Assert.False(form.IsSubmitting);
        Assert.Contains("Une erreur est survenue. Réessayez.", cut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);
        Assert.Contains(_log.Errors, e => e is InvalidOperationException { Message: "db down" });
    }

    [Fact]
    public async Task The_failure_notice_does_not_block_a_second_attempt_and_goes_away_with_it()
    {
        var calls = 0;
        var form = Form(new DynamicFieldSchema { Key = "a" });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form).Add(c => c.OnSave, (_, _) =>
        {
            if (++calls == 1) throw new InvalidOperationException("once");
            return Task.FromResult(true);
        }));

        await cut.Find("form").SubmitAsync();
        Assert.NotEmpty(cut.FindAll("[data-slot=form-error-summary]"));
        await cut.Find("form").SubmitAsync();

        Assert.Equal(2, calls);
        Assert.Empty(cut.FindAll("[data-slot=form-error-summary]"));
        Assert.False(form.IsDirty);
    }

    [Fact]
    public async Task An_exception_in_an_async_rule_an_OnSubmit_callback_and_a_Next_hook_is_reported_in_the_form()
    {
        var throwing = new DynamicFieldSchema
        {
            Key = "a",
            DefaultValue = "x",
            AsyncValidators = [new DynamicAsyncValidatorRule("net", (_, _, _) => throw new HttpRequestException("offline")) { Debounce = TimeSpan.Zero }],
        };
        var rule = Form(throwing);
        var ruleCut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, rule));
        await ruleCut.Find("form").SubmitAsync();
        Assert.Contains("Something went wrong", ruleCut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);
        Assert.False(rule.IsSubmitting);

        var plain = Form(new DynamicFieldSchema { Key = "a" });
        var callbackCut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, plain).Add(c => c.OnSubmit, _ => throw new InvalidOperationException("callback")));
        await callbackCut.Find("form").SubmitAsync();
        Assert.Contains("Something went wrong", callbackCut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);

        var wizard = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "w",
            Steps = [new DynamicStepSchema { Key = "a", Title = "A" }, new DynamicStepSchema { Key = "b", Title = "B" }],
            OnBeforeNext = (_, _) => throw new InvalidOperationException("hook"),
        });
        var nextCut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, wizard));
        await nextCut.Find("[data-slot=form-action-next]").ClickAsync();
        Assert.Contains("Something went wrong", nextCut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);
        Assert.Equal(0, wizard.CurrentStepIndex);
        Assert.True(_log.Errors.Count >= 3);
    }

    [Fact]
    public async Task An_exception_in_the_page_save_is_reported_too()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "p",
            Sections = [new DynamicSectionSchema { Key = "s", Label = "S", Fields = [new DynamicFieldSchema { Key = "a" }] }],
        });
        var cut = Render<KlyDynamicFormPage>(p => p.Add(c => c.Form, form).Add(c => c.OnSave, (_, _) => throw new InvalidOperationException("boom")));

        await cut.Find("form").SubmitAsync();

        Assert.Contains("Something went wrong", cut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);
        Assert.False(form.IsSubmitting);
    }

    [Fact]
    public async Task An_edit_that_replaces_a_running_rule_never_surfaces_as_a_cancellation_from_ValidateAsync()
    {
        var gate = new Gate();
        var form = Form(new DynamicFieldSchema { Key = "u", AsyncValidators = [gate.Rule()] });
        form.SetValue("u", "ada");

        var run = form.ValidateAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);
        await Until(() => gate.Calls.Count == 1);
        form.SetValue("u", "adam");
        await Until(() => gate.Calls.Count == 2);
        gate.Pending["adam"].SetResult("Taken");

        Assert.False(await run);
        Assert.Equal("Taken", form.GetError("u"));
        Assert.True(gate.Tokens[0].IsCancellationRequested);
    }

    [Fact]
    public async Task A_submit_whose_rule_is_replaced_by_an_edit_waits_for_the_new_value()
    {
        var gate = new Gate();
        var form = Form(new DynamicFieldSchema { Key = "u", AsyncValidators = [gate.Rule()] });
        form.SetValue("u", "ada");
        var handled = false;

        var submit = form.SubmitAsync((_, _) => { handled = true; return Task.FromResult(true); }, Xunit.TestContext.Current.CancellationToken);
        await Until(() => gate.Calls.Count == 1);
        form.SetValue("u", "bob");
        await Until(() => gate.Calls.Count == 2);
        gate.Pending["bob"].SetResult(null);

        Assert.True(await submit);
        Assert.True(handled);
        Assert.Equal(["ada", "bob"], gate.Calls);
    }

    // 2. A submit never succeeds with a value no rule has checked

    [Fact]
    public async Task A_value_that_keeps_changing_under_its_rule_is_never_reported_valid()
    {
        var calls = 0;
        var field = new DynamicFieldSchema
        {
            Key = "u",
            Trigger = FormValidationTrigger.Submit,
            AsyncValidators = [new DynamicAsyncValidatorRule("churn", (value, form, _) =>
            {
                calls++;
                form.SetValue("u", value + "!");
                return ValueTask.FromResult<string?>(null);
            }) { Debounce = TimeSpan.Zero }],
        };
        var form = Form(field);
        form.SetValue("u", "a");
        var handled = false;

        var ok = await form.SubmitAsync((_, _) => { handled = true; return Task.FromResult(true); }, Xunit.TestContext.Current.CancellationToken);

        Assert.False(ok);
        Assert.False(handled);
        Assert.Equal(3, calls);
        Assert.False(form.IsSubmitting);
        Assert.True(form.SubmitFailed);
    }

    // 3. The snapshot holds what the handler was given

    [Fact]
    public async Task The_baseline_after_a_save_is_what_the_handler_received_not_what_the_state_became()
    {
        var form = new DynamicFormController(
            new DynamicFormSchema { FormKey = "s", Fields = [new DynamicFieldSchema { Key = "name" }] },
            new Dictionary<string, object?> { ["name"] = "old" });
        form.SetValue("name", "typed");
        object? received = null;

        var ok = await form.SubmitAsync((f, _) =>
        {
            received = f.BuildOutput()["name"];
            f.SetValue("name", "edited while saving");
            return Task.FromResult(true);
        }, Xunit.TestContext.Current.CancellationToken);

        Assert.True(ok);
        Assert.Equal("typed", received);
        Assert.Equal("typed", form.GetInitialValue("name"));
        Assert.Equal(["name"], form.DirtyPaths);
    }

    // 4 and 5. Drafts

    private static DynamicFieldSchema Lines() => new()
    {
        Key = "lines",
        Kind = FormFieldKind.ArrayList,
        Fields =
        [
            new DynamicFieldSchema { Key = "sku" },
            new DynamicFieldSchema { Key = "pin", Kind = FormFieldKind.Password },
            new DynamicFieldSchema { Key = "otp", Kind = FormFieldKind.OneTimeCode },
            new DynamicFieldSchema { Key = "scan", Kind = FormFieldKind.File },
            new DynamicFieldSchema { Key = "extra", Kind = FormFieldKind.Custom, CustomKey = "x" },
        ],
    };

    [Fact]
    public void ExportState_leaves_sensitive_and_file_columns_of_a_list_out()
    {
        var lines = Lines();
        var form = Form(lines);
        form.AddArrayItem(lines);
        form.SetArrayItemValue("lines", 0, "sku", "A-1");
        form.SetArrayItemValue("lines", 0, "pin", "1234");
        form.SetArrayItemValue("lines", 0, "otp", "999999");
        form.SetArrayItemValue("lines", 0, "scan", FormFileValue.FromBytes("a", [1]));

        var row = ((List<Dictionary<string, object?>>)form.ExportState().Values["lines"]!)[0];
        Assert.Equal("A-1", row["sku"]);
        Assert.False(row.ContainsKey("pin"));
        Assert.False(row.ContainsKey("otp"));
        Assert.False(row.ContainsKey("scan"));

        var sensitive = ((List<Dictionary<string, object?>>)form.ExportState(includeSensitive: true).Values["lines"]!)[0];
        Assert.Equal("1234", sensitive["pin"]);
        Assert.False(sensitive.ContainsKey("scan"));
        Assert.Equal("1234", form.GetArrayItems("lines")[0]["pin"]);
    }

    [Fact]
    public void RestoreDraft_never_fills_file_or_custom_fields_or_columns()
    {
        var lines = Lines();
        var schema = new DynamicFormSchema
        {
            FormKey = "d",
            Fields =
            [
                new DynamicFieldSchema { Key = "doc", Kind = FormFieldKind.File },
                new DynamicFieldSchema { Key = "pick", Kind = FormFieldKind.Custom, CustomKey = "x" },
                new DynamicFieldSchema { Key = "name" },
                lines,
            ],
        };
        var form = new DynamicFormController(schema);

        form.RestoreDraft(FormDraft.FromJson("""{"version":1,"step":0,"values":{"doc":"evil","pick":"evil","name":"ok","lines":[{"sku":"A","scan":"evil","extra":"evil"}]}}"""));

        Assert.Null(form.GetValue("doc"));
        Assert.Null(form.GetValue("pick"));
        Assert.Equal("ok", form.GetValue("name"));
        var row = form.GetArrayItems("lines")[0];
        Assert.Equal("A", row["sku"]);
        Assert.False(row.ContainsKey("scan"));
        Assert.False(row.ContainsKey("extra"));
    }

    // 6. Files are read when chosen

    [Fact]
    public async Task A_chosen_file_is_read_into_memory_and_survives_the_input_being_unmounted()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "w",
            Steps =
            [
                new DynamicStepSchema { Key = "a", Title = "A", Fields = [new DynamicFieldSchema { Key = "doc", Kind = FormFieldKind.File, Label = "Doc" }] },
                new DynamicStepSchema { Key = "b", Title = "B", Fields = [new DynamicFieldSchema { Key = "other" }] },
            ],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));
        await Until(() => form.GetValue("doc") is not null);
        await cut.Find("[data-slot=form-action-next]").ClickAsync();
        Assert.Equal(1, form.CurrentStepIndex);
        Assert.Empty(cut.FindAll("input[type=file]"));

        var file = Assert.IsType<FormFileValue>(form.GetValue("doc"));
        await using var stream = await file.OpenReadAsync(Xunit.TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        Assert.Equal("hello", await reader.ReadToEndAsync(Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Removing_one_of_several_files_keeps_the_others_readable()
    {
        var form = Form(new DynamicFieldSchema { Key = "docs", Kind = FormFieldKind.File, Label = "Docs", Multiple = true });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("one", "a.txt"), InputFileContent.CreateFromText("two", "b.txt"));
        await Until(() => form.GetValue("docs") is not null);

        await cut.FindAll("[data-slot=form-file-remove]")[0].ClickAsync();

        var kept = Assert.Single(Assert.IsType<List<FormFileValue>>(form.GetValue("docs")));
        await using var stream = await kept.OpenReadAsync(Xunit.TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        Assert.Equal("two", await reader.ReadToEndAsync(Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_file_above_MaxFileSize_is_not_read_fails_validation_and_cannot_be_opened()
    {
        var form = Form(new DynamicFieldSchema { Key = "doc", Kind = FormFieldKind.File, Label = "Doc", MaxFileSize = 3 });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "big.txt"));
        await Until(() => form.GetValue("doc") is not null);

        var file = Assert.IsType<FormFileValue>(form.GetValue("doc"));
        Assert.Equal(5, file.Size);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await file.OpenReadAsync(Xunit.TestContext.Current.CancellationToken));
        Assert.Contains("big.txt is larger than 3 B", cut.Find("[data-slot=form-field-error]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_MaxFileSize_a_file_is_limited_to_the_default()
    {
        var form = Form(new DynamicFieldSchema { Key = "doc", Kind = FormFieldKind.File });
        var big = new FormFileValue("big.bin", FormFileValue.DefaultMaxSize + 1, "", _ => ValueTask.FromResult<Stream>(Stream.Null));
        var fine = new FormFileValue("fine.bin", FormFileValue.DefaultMaxSize, "", _ => ValueTask.FromResult<Stream>(Stream.Null));

        form.SetValue("doc", fine);
        Assert.True(form.Validate());
        form.SetValue("doc", big);
        Assert.False(form.Validate());
        Assert.Equal("big.bin is larger than 10 MB", form.GetError("doc"));
    }

    // 7. The live region

    [Fact]
    public async Task The_live_region_speaks_once_per_failed_attempt_and_not_on_every_edit()
    {
        var form = Form(
            new DynamicFieldSchema { Key = "a", Label = "A", Required = true },
            new DynamicFieldSchema { Key = "b", Label = "B", Required = true });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));
        string Live() => cut.Find("[data-slot=form-live-region]").TextContent;
        Assert.Equal("", Live());

        await cut.Find("form").SubmitAsync();
        var first = Live();
        Assert.Contains("2 errors", first, StringComparison.Ordinal);

        cut.Find("#kly-ff-a").Input("x");
        Assert.Equal(first, Live());
        Assert.Contains("1 error", cut.Find("[data-slot=form-error-summary-title]").TextContent, StringComparison.Ordinal);

        await cut.Find("form").SubmitAsync();
        Assert.Contains("1 error", Live(), StringComparison.Ordinal);
        Assert.NotEqual(first, Live());

        cut.Find("#kly-ff-b").Input("y");
        Assert.Equal("", Live());
    }

    [Fact]
    public void The_live_region_announces_a_form_level_error_when_it_appears()
    {
        var form = Form(new DynamicFieldSchema { Key = "a" });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        form.SetFormError("Server unavailable");
        Assert.Contains("Server unavailable", cut.Find("[data-slot=form-live-region]").TextContent, StringComparison.Ordinal);
        var text = cut.Find("[data-slot=form-live-region]").TextContent;

        cut.Find("#kly-ff-a").Input("typing");
        Assert.Equal(text, cut.Find("[data-slot=form-live-region]").TextContent);
    }

    // 8. Dispose

    [Fact]
    public async Task Disposing_the_controller_cancels_pending_validations_and_stops_new_ones()
    {
        var gate = new Gate();
        var form = Form(new DynamicFieldSchema { Key = "u", AsyncValidators = [gate.Rule()] });
        form.SetValue("u", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);

        form.Dispose();

        Assert.True(gate.Tokens[0].IsCancellationRequested);
        Assert.False(form.HasPendingValidation);
        form.SetValue("u", "adam");
        await Task.Delay(30, Xunit.TestContext.Current.CancellationToken);
        Assert.Single(gate.Calls);
        Assert.Equal("adam", form.GetValue("u"));
    }

    [Fact]
    public async Task Closing_an_overlay_form_disposes_its_controller()
    {
        var gate = new Gate();
        var service = Services.GetRequiredService<DynamicFormService>();
        var task = service.CreateFormAsync(new DynamicFormSchema { FormKey = "o", Fields = [new DynamicFieldSchema { Key = "u", AsyncValidators = [gate.Rule()] }] });
        var form = service.ActiveForm!;
        form.SetValue("u", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);

        service.CloseForm();
        await task;

        Assert.True(gate.Tokens[0].IsCancellationRequested);
    }

    [Fact]
    public async Task A_component_cancels_the_pending_validations_when_it_goes_away()
    {
        var gate = new Gate();
        var form = Form(new DynamicFieldSchema { Key = "u", AsyncValidators = [gate.Rule()] });
        _ = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));
        form.SetValue("u", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);

        await DisposeComponentsAsync();

        Assert.True(gate.Tokens[0].IsCancellationRequested);
    }

    // 9. Converter

    [Fact]
    public void A_comma_combination_is_an_enum_value_only_for_flags_enums()
    {
        var form = Form(new DynamicFieldSchema { Key = "v" });
        form.SetValue("v", "Low, High");
        Assert.False(form.TryGetValue<Level>("v", out _));
        form.SetValue("v", "Read, Write");
        Assert.Equal(Perm.Read | Perm.Write, form.GetValue<Perm>("v"));
        form.SetValue("v", "high");
        Assert.Equal(Level.High, form.GetValue<Level>("v"));
    }

    private enum Level { Low, High }

    [Flags]
    private enum Perm { None = 0, Read = 1, Write = 2 }

    // 10. Culture of the messages

    [Fact]
    public void Messages_format_numbers_and_sizes_with_the_separator_of_their_language()
    {
        Assert.Equal("Value must be at least 3.5", FormMessageLocales.En.MinValue(3.5));
        Assert.Equal("La valeur doit être supérieure ou égale à 3,5", FormMessageLocales.Fr.MinValue(3.5));
        Assert.Equal("La valeur doit être inférieure ou égale à 99,25", FormMessageLocales.Fr.MaxValue(99.25));
        Assert.Equal("Der Wert muss mindestens 3,5 betragen", FormMessageLocales.De.MinValue(3.5));
        Assert.Equal("3.5 以上の値を入力してください", FormMessageLocales.Ja.MinValue(3.5));
        Assert.Equal("1.5 MB", FormMessageLocales.En.FileSize((long)(1.5 * 1024 * 1024)));
        Assert.Equal("1,5 Mo", FormMessageLocales.Fr.FileSize((long)(1.5 * 1024 * 1024)));
        Assert.Equal("512 o", FormMessageLocales.Fr.FileSize(512));
        Assert.Equal("1,5 МБ", FormMessageLocales.Ru.FileSize((long)(1.5 * 1024 * 1024)));
        Assert.Equal("1,5 Ko", FormFileValue.FormatSize(1536, ",", ["o", "Ko", "Mo", "Go", "To"]));
        Assert.All(FormMessageLocales.Tables.Values, m => Assert.False(string.IsNullOrWhiteSpace(m.FileSize(2048))));
    }

    [Fact]
    public void A_french_controller_reports_a_fractional_bound_and_a_file_limit_in_french()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "f",
            Fields =
            [
                new DynamicFieldSchema { Key = "n", Kind = FormFieldKind.Number, Min = 3.5 },
                new DynamicFieldSchema { Key = "doc", Kind = FormFieldKind.File, MaxFileSize = 1536 },
            ],
        }) { Messages = FormMessageLocales.Fr };
        form.SetValue("n", 1.0);
        form.SetValue("doc", FormFileValue.FromBytes("gros.bin", new byte[4000]));

        Assert.False(form.Validate());

        Assert.Equal("La valeur doit être supérieure ou égale à 3,5", form.GetError("n"));
        Assert.Equal("gros.bin dépasse 1,5 Ko", form.GetError("doc"));
    }

    [Fact]
    public void The_file_list_shows_sizes_in_the_locale()
    {
        var form = Form(new DynamicFieldSchema { Key = "doc", Kind = FormFieldKind.File, Label = "Doc" });
        form.SetValue("doc", FormFileValue.FromBytes("a.bin", new byte[1536]));
        var cut = Render<KlyDynamicFormEngine>(p => p.AddCascadingValue(KlyLocales.Fr).Add(c => c.Form, form));

        Assert.Contains("1,5 Ko", cut.Find("[data-slot=form-file]").TextContent, StringComparison.Ordinal);
    }
}
