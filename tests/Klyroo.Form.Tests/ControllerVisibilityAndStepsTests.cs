using Klyroo.Form;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class ControllerVisibilityAndStepsTests
{
    private static FormFieldSchema Required(string key) => new() { Key = key, Label = key, Required = true };

    [Fact]
    public void Children_of_a_hidden_container_are_not_validated_or_output_but_keep_their_value()
    {
        var showGroup = false;
        var schema = new FormSchema
        {
            Fields =
            [
                new FormFieldSchema
                {
                    Key = "company", Kind = FormFieldKind.Card, Condition = _ => showGroup,
                    Fields = [Required("vat"), new FormFieldSchema { Key = "inner", Kind = FormFieldKind.Group, Fields = [Required("siret")] }],
                },
                new FormFieldSchema { Key = "name", DefaultValue = "Acme" },
            ],
        };
        var form = new FormController(schema, new Dictionary<string, object?> { ["siret"] = "123" });

        Assert.True(form.Validate());
        Assert.False(form.HasBlockingErrors);
        var output = form.BuildOutput();
        Assert.False(output.ContainsKey("vat"));
        Assert.False(output.ContainsKey("siret"));
        Assert.Equal("123", form.GetValue("siret"));

        showGroup = true;
        Assert.False(form.Validate());
        Assert.NotNull(form.GetError("vat"));
        Assert.True(form.BuildOutput().ContainsKey("siret"));

        showGroup = false;
        Assert.True(form.Validate());
        Assert.Null(form.GetError("vat"));
    }

    [Fact]
    public void Fields_of_a_hidden_tab_are_not_validated_or_output()
    {
        var schema = new FormSchema
        {
            Fields =
            [
                new FormFieldSchema
                {
                    Key = "tabs", Kind = FormFieldKind.Tabs,
                    Tabs =
                    [
                        new FormTabItemSchema { Key = "a", Label = "A", Fields = [new FormFieldSchema { Key = "visible", DefaultValue = "x" }] },
                        new FormTabItemSchema { Key = "b", Label = "B", Condition = _ => false, Fields = [Required("hidden")] },
                    ],
                },
            ],
        };
        var form = new FormController(schema);

        Assert.True(form.Validate());
        Assert.Equal(["visible"], form.BuildOutput().Keys);
    }

    [Fact]
    public void Fields_of_a_hidden_section_are_not_validated_or_output()
    {
        var schema = new FormSchema
        {
            Sections =
            [
                new FormPageSectionSchema { Key = "main", Label = "Main", Fields = [new FormFieldSchema { Key = "name", DefaultValue = "x" }] },
                new FormPageSectionSchema { Key = "extra", Label = "Extra", Condition = _ => false, Fields = [Required("hidden")] },
            ],
        };
        var form = new FormController(schema);

        Assert.True(form.Validate());
        Assert.Equal(["name"], form.BuildOutput().Keys);
    }

    [Fact]
    public async Task Fields_of_a_skipped_step_are_not_validated_or_output_on_submit()
    {
        var schema = new FormSchema
        {
            Steps =
            [
                new FormStepSchema { Key = "one", Title = "One", Fields = [new FormFieldSchema { Key = "plan", DefaultValue = "free" }] },
                new FormStepSchema
                {
                    Key = "billing", Title = "Billing",
                    SkipWhen = f => f.GetValue<string>("plan") == "free",
                    Fields = [Required("vat")],
                },
            ],
        };
        var form = new FormController(schema);

        Assert.True(await form.SubmitAsync());
        Assert.False(form.BuildOutput().ContainsKey("vat"));
    }

    [Fact]
    public void Containers_without_children_are_layout_only()
    {
        var schema = new FormSchema
        {
            Fields =
            [
                new FormFieldSchema { Key = "emptyGroup", Kind = FormFieldKind.Group },
                new FormFieldSchema { Key = "emptyTabs", Kind = FormFieldKind.Tabs },
                new FormFieldSchema { Key = "name" },
            ],
        };
        var form = new FormController(schema);

        Assert.Equal(["name"], form.AllFields.Select(f => f.Key));
        Assert.False(form.InternalState.ContainsKey("emptyGroup"));
        Assert.Equal(["name"], form.BuildOutput().Keys);
    }

    private static FormSchema WizardStartingWithSkippedStep(Func<FormController, bool> skipFirst) => new()
    {
        Steps =
        [
            new FormStepSchema { Key = "intro", Title = "Intro", SkipWhen = skipFirst },
            new FormStepSchema { Key = "details", Title = "Details", Fields = [new FormFieldSchema { Key = "mode", DefaultValue = "short" }] },
            new FormStepSchema { Key = "extra", Title = "Extra", SkipWhen = f => f.GetValue<string>("mode") == "short" },
        ],
    };

    [Fact]
    public void A_stepped_form_starts_and_resets_on_the_first_step_that_is_not_skipped()
    {
        var form = new FormController(WizardStartingWithSkippedStep(_ => true));

        Assert.Equal("details", form.CurrentStep?.Key);
        Assert.True(form.IsFirstStep);
        Assert.True(form.IsLastStep);

        form.Reset();
        Assert.Equal("details", form.CurrentStep?.Key);
    }

    [Fact]
    public async Task IsLastStep_and_IsFirstStep_look_past_skipped_steps()
    {
        var form = new FormController(WizardStartingWithSkippedStep(_ => false));
        Assert.Equal("intro", form.CurrentStep?.Key);
        Assert.False(form.IsLastStep);

        Assert.True(await form.NextStepAsync());
        Assert.Equal("details", form.CurrentStep?.Key);
        Assert.False(form.IsFirstStep);
        Assert.True(form.IsLastStep);

        form.SetValue("mode", "long");
        Assert.False(form.IsLastStep);
    }

    [Fact]
    public void A_change_clears_the_error_of_a_submit_triggered_field_without_validating()
    {
        var validations = 0;
        var schema = new FormSchema
        {
            Fields =
            [
                new FormFieldSchema
                {
                    Key = "code", Required = true, Trigger = FormValidationTrigger.Submit,
                    Validators = [new FormValidatorRule("len", "Too short", (v, _) => { validations++; return v?.ToString()?.Length >= 3; })],
                },
            ],
        };
        var form = new FormController(schema);
        form.SetValue("code", "a");
        Assert.False(form.Validate());
        Assert.Equal("Too short", form.GetError("code"));
        var before = validations;

        form.SetValue("code", "b", markTouched: true);

        Assert.Null(form.GetError("code"));
        Assert.False(form.HasBlockingErrors);
        Assert.Equal(before, validations);
    }

    [Fact]
    public async Task Opening_a_form_over_another_completes_the_replaced_task_as_not_completed()
    {
        var service = new FormService();
        var first = service.CreateFormAsync(new FormSchema { Fields = [new FormFieldSchema { Key = "a", DefaultValue = "1" }] });
        var second = service.CreateFormAsync(new FormSchema { Fields = [new FormFieldSchema { Key = "b" }] });

        var outcome = await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(outcome.IsCompleted);
        Assert.Equal("1", outcome.FormData["a"]);
        Assert.False(second.IsCompleted);
        Assert.True(service.IsOpen);
    }

    [Fact]
    public void ResetField_calls_Watch_with_the_restored_value_and_Reset_does_not()
    {
        var watched = new List<object?>();
        var schema = new FormSchema
        {
            Fields = [new FormFieldSchema { Key = "zip", DefaultValue = "75001", Watch = (v, _) => watched.Add(v) }],
        };
        var form = new FormController(schema);
        form.SetValue("zip", "69001");
        form.ResetField("zip");
        Assert.Equal(["69001", "75001"], watched);

        form.ResetField("zip");
        form.Reset();
        Assert.Equal(2, watched.Count);
    }

    [Fact]
    public void A_required_date_range_needs_both_dates_in_order()
    {
        var schema = new FormSchema { Fields = [new FormFieldSchema { Key = "stay", Kind = FormFieldKind.DateRange, Required = true }] };
        var form = new FormController(schema);

        form.SetValue("stay", new List<string> { "2026-05-01", "" });
        Assert.False(form.Validate());
        Assert.Equal("This field is required", form.GetError("stay"));

        form.SetValue("stay", new List<string> { "2026-05-10", "2026-05-01" });
        Assert.False(form.Validate());
        Assert.Equal("The end date must not be before the start date", form.GetError("stay"));

        form.SetValue("stay", new List<string> { "2026-05-01", "2026-05-10" });
        Assert.True(form.Validate());
    }
}
