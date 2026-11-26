using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class AsyncValidationTests : BunitContext
{
    public AsyncValidationTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // A rule whose result the test releases by hand and that records the values it saw.
    private sealed class Gate
    {
        public List<object?> Calls { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public Dictionary<string, TaskCompletionSource<string?>> Pending { get; } = [];

        public FormAsyncValidatorRule Rule(TimeSpan? debounce = null) =>
            new("free", async (value, _, ct) =>
            {
                var text = value?.ToString() ?? "";
                Calls.Add(value);
                Tokens.Add(ct);
                var source = Pending[text] = new TaskCompletionSource<string?>();
                await using var registration = ct.Register(() => source.TrySetCanceled(ct));
                return await source.Task;
            })
            { Debounce = debounce ?? TimeSpan.Zero };

        public void Release(string value, string? message = null) => Pending[value].SetResult(message);
    }

    private static FormFieldSchema Username(Gate gate, FormValidationTrigger trigger = FormValidationTrigger.Blur, TimeSpan? debounce = null, bool required = false) => new()
    {
        Key = "username",
        Label = "Username",
        Required = required,
        Trigger = trigger,
        Validators = [new FormValidatorRule("length", "Too short", (v, _) => (v?.ToString() ?? "").Length is 0 or >= 3)],
        AsyncValidators = [gate.Rule(debounce)],
    };

    private static FormController Form(params FormFieldSchema[] fields) => new(new FormSchema { FormKey = "a", Fields = fields });

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Condition not met.");
            await Task.Delay(5, Xunit.TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Rule_runs_when_the_field_is_touched_and_its_error_shows_once_it_completes()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada");
        Assert.Empty(gate.Calls);

        form.MarkTouched("username");
        Assert.True(form.IsValidating("username"));
        Assert.True(form.HasPendingValidation);
        await Until(() => gate.Calls.Count == 1);
        Assert.Null(form.GetError("username"));

        gate.Release("ada", "Taken");
        await Until(() => !form.IsValidating("username"));

        Assert.Equal("Taken", form.GetError("username"));
        Assert.True(form.HasBlockingErrors);
        Assert.False(form.HasPendingValidation);
    }

    [Fact]
    public async Task A_valid_result_leaves_no_error_and_is_reused_for_the_same_value()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada");
        form.MarkTouched("username");
        await Until(() => gate.Calls.Count == 1);
        gate.Release("ada");
        await Until(() => !form.IsValidating("username"));

        form.MarkTouched("username");
        Assert.True(await form.ValidateAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Single(gate.Calls);
        Assert.Null(form.GetError("username"));
    }

    [Fact]
    public async Task Typing_cancels_the_stale_run_and_ignores_its_result()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);
        var firstToken = gate.Tokens[0];

        form.SetValue("username", "adam");
        await Until(() => gate.Calls.Count == 2);

        Assert.True(firstToken.IsCancellationRequested);
        gate.Release("adam");
        await Until(() => !form.IsValidating("username"));
        Assert.Null(form.GetError("username"));
        Assert.Equal(["ada", "adam"], gate.Calls);
    }

    [Fact]
    public async Task Debounce_waits_for_the_value_to_settle_before_calling_the_service()
    {
        var gate = new Gate();
        var form = Form(Username(gate, debounce: TimeSpan.FromMilliseconds(150)));
        form.SetValue("username", "a", markTouched: true);
        form.SetValue("username", "ad");
        form.SetValue("username", "ada");
        Assert.True(form.IsValidating("username"));
        Assert.Empty(gate.Calls);

        await Until(() => gate.Calls.Count == 1);

        Assert.Equal(["ada"], gate.Calls);
        gate.Release("ada");
        await Until(() => !form.IsValidating("username"));
    }

    [Fact]
    public async Task Rule_does_not_run_for_an_empty_value_or_one_that_fails_a_synchronous_rule()
    {
        var gate = new Gate();
        var form = Form(Username(gate));

        form.SetValue("username", "", markTouched: true);
        form.SetValue("username", "ab");

        Assert.Equal("Too short", form.GetError("username"));
        Assert.False(form.IsValidating("username"));
        await Task.Delay(30, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(gate.Calls);
    }

    [Fact]
    public async Task Fields_with_the_Submit_trigger_wait_for_submission()
    {
        var gate = new Gate();
        var form = Form(Username(gate, FormValidationTrigger.Submit));

        form.SetValue("username", "ada", markTouched: true);
        form.MarkTouched("username");

        Assert.False(form.IsValidating("username"));
        await Task.Delay(30, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(gate.Calls);
    }

    [Fact]
    public async Task SubmitAsync_waits_for_the_rule_and_fails_on_its_error()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada");
        var submitted = false;

        var submit = form.SubmitAsync((_, _) =>
        {
            submitted = true;
            return Task.FromResult(true);
        }, Xunit.TestContext.Current.CancellationToken);
        await Until(() => gate.Calls.Count == 1);
        Assert.True(form.IsSubmitting);
        Assert.True(form.IsValidating("username"));
        Assert.False(await form.SubmitAsync());

        gate.Release("ada", "Taken");

        Assert.False(await submit);
        Assert.False(submitted);
        Assert.False(form.IsSubmitting);
        Assert.Equal("Taken", form.GetError("username"));
        Assert.True(form.SubmitFailed);
    }

    [Fact]
    public async Task SubmitAsync_succeeds_when_the_rule_passes()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada");

        var submit = form.SubmitAsync();
        await Until(() => gate.Calls.Count == 1);
        gate.Release("ada");

        Assert.True(await submit);
        Assert.False(form.IsDirty);
    }

    [Fact]
    public async Task SubmitAsync_reuses_the_result_of_a_completed_background_run()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);
        gate.Release("ada");
        await Until(() => !form.IsValidating("username"));

        Assert.True(await form.SubmitAsync());

        Assert.Single(gate.Calls);
    }

    [Fact]
    public async Task SubmitAsync_joins_a_background_run_already_in_flight_for_the_same_value()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);

        var submit = form.SubmitAsync();
        await Task.Delay(30, Xunit.TestContext.Current.CancellationToken);
        gate.Release("ada");

        Assert.True(await submit);
        Assert.Single(gate.Calls);
    }

    [Fact]
    public async Task SubmitAsync_does_not_wait_for_the_debounce_of_a_background_run()
    {
        var gate = new Gate();
        var form = Form(Username(gate, debounce: TimeSpan.FromMinutes(5)));
        form.SetValue("username", "ada", markTouched: true);
        Assert.True(form.IsValidating("username"));

        var submit = form.SubmitAsync();
        await Until(() => gate.Calls.Count == 1);
        gate.Release("ada");

        Assert.True(await submit);
        Assert.Single(gate.Calls);
        Assert.False(form.HasPendingValidation);
    }

    [Fact]
    public async Task An_exception_in_a_background_run_is_swallowed_and_submission_reports_it()
    {
        var calls = 0;
        var field = new FormFieldSchema
        {
            Key = "username",
            AsyncValidators = [new FormAsyncValidatorRule("net", (_, _, _) =>
            {
                calls++;
                throw new HttpRequestException("offline");
            }) { Debounce = TimeSpan.Zero }],
        };
        var form = Form(field);

        form.SetValue("username", "ada", markTouched: true);
        await Until(() => !form.IsValidating("username"));
        Assert.Equal(1, calls);
        Assert.Null(form.GetError("username"));

        await Assert.ThrowsAsync<HttpRequestException>(() => form.SubmitAsync());
        Assert.False(form.IsSubmitting);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task NextStepAsync_waits_for_the_rules_of_the_current_step()
    {
        var gate = new Gate();
        var form = new FormController(new FormSchema
        {
            FormKey = "wizard",
            Steps =
            [
                new FormStepSchema { Key = "a", Title = "A", Fields = [Username(gate)] },
                new FormStepSchema { Key = "b", Title = "B", Fields = [new FormFieldSchema { Key = "other" }] },
            ],
        });
        form.SetValue("username", "ada");

        var next = form.NextStepAsync();
        await Until(() => gate.Calls.Count == 1);
        Assert.Equal(0, form.CurrentStepIndex);
        gate.Release("ada", "Taken");

        Assert.False(await next);
        Assert.Equal(0, form.CurrentStepIndex);
        Assert.Equal("Taken", form.GetError("username"));

        form.SetValue("username", "bob");
        await Until(() => gate.Calls.Count == 2);
        gate.Release("bob");
        await Until(() => !form.IsValidating("username"));
        Assert.True(await form.NextStepAsync());
        Assert.Equal(1, form.CurrentStepIndex);
        Assert.Equal(2, gate.Calls.Count);
    }

    [Fact]
    public async Task Rules_of_a_field_run_in_order_and_the_first_message_wins()
    {
        var order = new List<string>();
        var field = new FormFieldSchema
        {
            Key = "x",
            AsyncValidators =
            [
                new FormAsyncValidatorRule("one", (_, _, _) => { order.Add("one"); return ValueTask.FromResult<string?>(null); }) { Debounce = TimeSpan.Zero },
                new FormAsyncValidatorRule("two", (_, _, _) => { order.Add("two"); return ValueTask.FromResult<string?>("two failed"); }) { Debounce = TimeSpan.Zero },
                new FormAsyncValidatorRule("three", (_, _, _) => { order.Add("three"); return ValueTask.FromResult<string?>("three failed"); }) { Debounce = TimeSpan.Zero },
            ],
        };
        var form = Form(field);
        form.SetValue("x", "v");

        Assert.False(await form.ValidateAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Equal(["one", "two"], order);
        Assert.Equal("two failed", form.GetError("x"));
    }

    [Fact]
    public async Task ResetField_and_Reset_cancel_a_pending_run_and_forget_results()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada", markTouched: true);
        await Until(() => gate.Calls.Count == 1);
        var token = gate.Tokens[0];

        form.Reset();

        Assert.True(token.IsCancellationRequested);
        Assert.False(form.HasPendingValidation);
        await Until(() => !form.IsValidating("username"));
        Assert.Null(form.GetError("username"));
    }

    [Fact]
    public async Task Cancelling_the_token_of_ValidateAsync_cancels_the_rules()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada");
        using var cts = new CancellationTokenSource();

        var run = form.ValidateAsync(cancellationToken: cts.Token);
        await Until(() => gate.Calls.Count == 1);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.False(form.HasPendingValidation);
    }

    [Fact]
    public async Task Hidden_fields_are_not_validated_asynchronously()
    {
        var gate = new Gate();
        var hidden = Username(gate);
        var form = Form(new FormFieldSchema { Key = "gate", DefaultValue = "closed" },
            new FormFieldSchema
            {
                Key = "username",
                Condition = f => f.GetValue<string>("gate") == "open",
                AsyncValidators = hidden.AsyncValidators,
            });
        form.SetValue("username", "ada");

        Assert.True(await form.ValidateAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken));
        Assert.Empty(gate.Calls);
    }

    [Fact]
    public async Task Component_shows_a_pending_state_and_then_the_error()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));
        var input = cut.Find("#kly-ff-username");

        await input.InputAsync("ada");
        await input.BlurAsync();

        cut.WaitForAssertion(() =>
        {
            var field = cut.Find("[data-form-field=username]");
            Assert.Equal("true", field.GetAttribute("aria-busy"));
            Assert.Equal("true", field.GetAttribute("data-validating"));
            Assert.NotNull(cut.Find("[data-slot=form-field-pending]"));
        });

        await Until(() => gate.Calls.Count == 1);
        gate.Release("ada", "Already taken");

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("[data-slot=form-field-pending]"));
            Assert.Contains("Already taken", cut.Find("[data-slot=form-field-error]").TextContent, StringComparison.Ordinal);
            Assert.Null(cut.Find("[data-form-field=username]").GetAttribute("aria-busy"));
        });
    }

    [Fact]
    public async Task Component_submit_waits_for_the_rule_and_stays_busy()
    {
        var gate = new Gate();
        var form = Form(Username(gate));
        form.SetValue("username", "ada");
        var cut = Render<KlyFormEngine>(p => p.Add(c => c.Form, form));

        var submit = cut.Find("form").SubmitAsync();
        await Until(() => gate.Calls.Count == 1);

        cut.WaitForAssertion(() => Assert.True(cut.Find("[data-slot=form-action-submit]").HasAttribute("disabled")));
        gate.Release("ada", "Taken");
        await submit;

        cut.WaitForAssertion(() => Assert.Contains("Taken", cut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal));
    }
}
