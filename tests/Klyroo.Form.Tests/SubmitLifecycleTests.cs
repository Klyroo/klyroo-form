using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class SubmitLifecycleTests : BunitContext
{
    public SubmitLifecycleTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static DynamicFormSchema Schema(params DynamicFieldSchema[] fields) => new() { FormKey = "life", Fields = fields };

    private static Task<bool> Submit(DynamicFormController form, Func<DynamicFormController, CancellationToken, Task<bool>> handler) =>
        form.SubmitAsync(handler, Xunit.TestContext.Current.CancellationToken);

    private static DynamicFieldSchema Name(bool required = false) => new() { Key = "name", Label = "Name", Required = required };

    [Fact]
    public async Task Handler_runs_while_submitting_and_the_snapshot_is_retaken_only_on_success()
    {
        var form = new DynamicFormController(Schema(Name()), new Dictionary<string, object?> { ["name"] = "a" });
        form.SetValue("name", "b");
        var submittingInside = false;
        var dirtyInside = false;

        var ok = await Submit(form, (f, _) =>
        {
            submittingInside = f.IsSubmitting;
            dirtyInside = f.IsDirty;
            return Task.FromResult(true);
        });

        Assert.True(ok);
        Assert.True(submittingInside);
        Assert.True(dirtyInside);
        Assert.False(form.IsSubmitting);
        Assert.False(form.IsDirty);
        Assert.False(form.SubmitFailed);
    }

    [Fact]
    public async Task Handler_returning_false_keeps_the_edits_dirty_and_flags_the_failure()
    {
        var form = new DynamicFormController(Schema(Name()), new Dictionary<string, object?> { ["name"] = "a" });
        form.SetValue("name", "b");

        var ok = await Submit(form, (f, _) =>
        {
            f.SetFormError("The server refused it");
            return Task.FromResult(false);
        });

        Assert.False(ok);
        Assert.False(form.IsSubmitting);
        Assert.True(form.IsDirty);
        Assert.True(form.SubmitFailed);
        Assert.Equal(["The server refused it"], form.FormErrors);
    }

    [Fact]
    public async Task Handler_that_throws_propagates_and_leaves_the_form_dirty_and_editable()
    {
        var form = new DynamicFormController(Schema(Name()), new Dictionary<string, object?> { ["name"] = "a" });
        form.SetValue("name", "b");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Submit(form, (_, _) => throw new InvalidOperationException("boom")));

        Assert.False(form.IsSubmitting);
        Assert.True(form.IsDirty);
    }

    [Fact]
    public async Task A_second_submit_while_one_runs_is_refused_without_running_the_handler_again()
    {
        var form = new DynamicFormController(Schema(Name()));
        var release = new TaskCompletionSource<bool>();
        var calls = 0;

        var first = Submit(form, async (_, _) =>
        {
            calls++;
            return await release.Task;
        });
        var second = await Submit(form, (_, _) =>
        {
            calls++;
            return Task.FromResult(true);
        });
        var plain = await form.SubmitAsync();
        release.SetResult(true);

        Assert.False(second);
        Assert.False(plain);
        Assert.True(await first);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Handler_receives_the_cancellation_token()
    {
        var form = new DynamicFormController(Schema(Name()));
        using var cts = new CancellationTokenSource();
        CancellationToken seen = default;

        await form.SubmitAsync((_, ct) =>
        {
            seen = ct;
            return Task.FromResult(true);
        }, cts.Token);

        Assert.Equal(cts.Token, seen);
    }

    [Fact]
    public async Task Handler_does_not_run_when_validation_fails()
    {
        var form = new DynamicFormController(Schema(Name(required: true)));
        var ran = false;

        var ok = await Submit(form, (_, _) =>
        {
            ran = true;
            return Task.FromResult(true);
        });

        Assert.False(ok);
        Assert.False(ran);
        Assert.True(form.SubmitFailed);
    }

    [Fact]
    public void A_blocking_form_level_error_makes_Validate_fail_even_for_a_single_step()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "wizard",
            Steps =
            [
                new DynamicStepSchema { Key = "a", Title = "A", Fields = [new DynamicFieldSchema { Key = "x" }] },
                new DynamicStepSchema { Key = "b", Title = "B", Fields = [new DynamicFieldSchema { Key = "y" }] },
            ],
        };
        var form = new DynamicFormController(schema);

        form.SetError("somewhere-else", "Not a field", blocking: true);

        Assert.False(form.Validate());
        Assert.False(form.Validate(currentStepOnly: true));
        Assert.Equal(["Not a field"], form.FormErrors);

        form.ClearError("somewhere-else");
        Assert.True(form.Validate());
    }

    [Fact]
    public void A_non_blocking_form_level_error_does_not_fail_validation_but_is_listed()
    {
        var form = new DynamicFormController(Schema(Name()));
        form.SetFormError("Heads up", blocking: false);

        Assert.True(form.Validate());
        var entry = Assert.Single(form.GetErrorEntries());
        Assert.True(entry.IsFormLevel);
        Assert.Equal(DynamicFormController.FormErrorKey, entry.Path);
    }

    [Fact]
    public void A_blocking_error_on_a_field_of_another_step_does_not_block_the_current_step()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "wizard",
            Steps =
            [
                new DynamicStepSchema { Key = "a", Title = "A", Fields = [new DynamicFieldSchema { Key = "x" }] },
                new DynamicStepSchema { Key = "b", Title = "B", Fields = [new DynamicFieldSchema { Key = "y" }] },
            ],
        };
        var form = new DynamicFormController(schema);
        form.SetError("y", "Taken", blocking: true);

        Assert.True(form.Validate(currentStepOnly: true));
        Assert.False(form.Validate());
    }

    [Fact]
    public async Task Concurrent_NextStepAsync_calls_advance_one_step_only()
    {
        var gate = new TaskCompletionSource<bool>();
        var schema = new DynamicFormSchema
        {
            FormKey = "wizard",
            Steps =
            [
                new DynamicStepSchema { Key = "a", Title = "A", Fields = [new DynamicFieldSchema { Key = "x" }] },
                new DynamicStepSchema { Key = "b", Title = "B", Fields = [new DynamicFieldSchema { Key = "y" }] },
                new DynamicStepSchema { Key = "c", Title = "C", Fields = [new DynamicFieldSchema { Key = "z" }] },
            ],
            OnBeforeNext = (_, _) => gate.Task,
        };
        var form = new DynamicFormController(schema);

        var first = form.NextStepAsync();
        var second = await form.NextStepAsync();
        gate.SetResult(true);

        Assert.False(second);
        Assert.True(await first);
        Assert.Equal(1, form.CurrentStepIndex);
    }

    [Fact]
    public void Entries_list_fields_in_form_order_then_cells_then_form_level_errors()
    {
        var schema = Schema(
            new DynamicFieldSchema { Key = "a", Required = true },
            new DynamicFieldSchema
            {
                Key = "lines",
                Kind = FormFieldKind.ArrayList,
                Fields = [new DynamicFieldSchema { Key = "sku", Required = true }],
            },
            new DynamicFieldSchema { Key = "b", Required = true });
        var form = new DynamicFormController(schema);
        form.AddArrayItem(schema.Fields[1]);
        form.SetFormError("Global");

        Assert.False(form.Validate());

        var entries = form.GetErrorEntries();
        Assert.Equal(["a", "lines[0].sku", "b", DynamicFormController.FormErrorKey], entries.Select(e => e.Path));
        Assert.Equal("lines", entries[1].FieldKey);
        Assert.Equal(0, entries[1].RowIndex);
        Assert.Equal("sku", entries[1].ChildKey);
    }

    [Fact]
    public async Task Engine_OnSave_keeps_the_form_submitting_until_the_save_ends_and_raises_OnSubmit_on_success()
    {
        var release = new TaskCompletionSource<bool>();
        IReadOnlyDictionary<string, object?>? saved = null;
        IReadOnlyDictionary<string, object?>? submitted = null;
        var form = new DynamicFormController(Schema(Name()));
        form.SetValue("name", "Ada");

        var cut = Render<KlyDynamicFormEngine>(p => p
            .Add(c => c.Form, form)
            .Add(c => c.OnSave, async (data, _) =>
            {
                saved = data;
                return await release.Task;
            })
            .Add(c => c.OnSubmit, data => submitted = data));

        var submit = cut.Find("form").SubmitAsync();
        cut.WaitForAssertion(() => Assert.True(form.IsSubmitting));

        var button = cut.Find("[data-slot=form-action-submit]");
        Assert.True(button.HasAttribute("disabled"));
        Assert.Contains("Saving", button.TextContent, StringComparison.Ordinal);
        Assert.Equal("true", cut.Find("form").GetAttribute("aria-busy"));
        Assert.Null(submitted);

        release.SetResult(true);
        await submit;

        Assert.Equal("Ada", saved!["name"]);
        Assert.Equal("Ada", submitted!["name"]);
        Assert.False(form.IsDirty);
        Assert.False(form.IsSubmitting);
    }

    [Fact]
    public async Task Engine_OnSave_returning_false_does_not_raise_OnSubmit_and_keeps_the_form_dirty()
    {
        var form = new DynamicFormController(Schema(Name()));
        form.SetValue("name", "Ada");
        var submitted = false;

        var cut = Render<KlyDynamicFormEngine>(p => p
            .Add(c => c.Form, form)
            .Add(c => c.OnSave, (_, _) =>
            {
                form.SetFormError("Name already used");
                return Task.FromResult(false);
            })
            .Add(c => c.OnSubmit, _ => submitted = true));

        await cut.Find("form").SubmitAsync();

        Assert.False(submitted);
        Assert.True(form.IsDirty);
        Assert.Contains("Name already used", cut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Engine_does_not_start_a_second_save_while_one_runs()
    {
        var release = new TaskCompletionSource<bool>();
        var calls = 0;
        var form = new DynamicFormController(Schema(Name()));
        var cut = Render<KlyDynamicFormEngine>(p => p
            .Add(c => c.Form, form)
            .Add(c => c.OnSave, async (_, _) =>
            {
                calls++;
                return await release.Task;
            }));

        var first = cut.Find("form").SubmitAsync();
        cut.WaitForAssertion(() => Assert.True(form.IsSubmitting));
        await cut.Find("form").SubmitAsync();
        release.SetResult(true);
        await first;

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Page_OnSave_runs_inside_the_submission()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "page",
            Sections = [new DynamicSectionSchema { Key = "s", Label = "S", Fields = [Name()] }],
        };
        var form = new DynamicFormController(schema);
        form.SetValue("name", "Ada");
        var insideSubmitting = false;

        var cut = Render<KlyDynamicFormPage>(p => p
            .Add(c => c.Form, form)
            .Add(c => c.OnSave, (_, _) =>
            {
                insideSubmitting = form.IsSubmitting;
                return Task.FromResult(true);
            }));

        await cut.Find("form").SubmitAsync();

        Assert.True(insideSubmitting);
        Assert.False(form.IsDirty);
    }

    [Fact]
    public async Task Summary_lists_field_errors_after_a_failed_submit_and_form_level_errors_always()
    {
        var form = new DynamicFormController(Schema(Name(required: true)));
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        Assert.Empty(cut.FindAll("[data-slot=form-error-summary]"));
        await cut.Find("form").SubmitAsync();

        var summary = cut.Find("[data-slot=form-error-summary]");
        Assert.Contains("There is 1 error", summary.TextContent, StringComparison.Ordinal);
        Assert.Contains("Name: This field is required", summary.TextContent, StringComparison.Ordinal);
        Assert.Contains("Name: This field is required", cut.Find("[data-slot=form-live-region]").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[role=alert]"));

        form.SetValue("name", "Ada", markTouched: true);
        Assert.Empty(cut.FindAll("[data-slot=form-error-summary]"));
        Assert.Equal("", cut.Find("[data-slot=form-live-region]").TextContent);

        form.SetFormError("Server unavailable");
        Assert.Contains("Server unavailable", cut.Find("[data-slot=form-error-summary]").TextContent, StringComparison.Ordinal);
        Assert.Contains("Server unavailable", cut.Find("[data-slot=form-live-region]").TextContent, StringComparison.Ordinal);
    }
}
