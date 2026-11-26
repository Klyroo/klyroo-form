using Bunit;
using Klyroo.Form;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class ComprehensiveFormTests : BunitContext
{
    [Fact]
    public async Task FormController_evaluates_conditions_transforms_dirty_paths_and_server_errors()
    {
        var watched = "";
        var schema = new DynamicFormSchema
        {
            FormKey = "account",
            Title = "Account",
            Fields =
            [
                new DynamicFieldSchema
                {
                    Key = "name", Label = "Name", Required = true,
                    TransformOutput = v => v?.ToString()?.Trim(),
                    Watch = (val, _) => watched = val?.ToString() ?? ""
                },
                new DynamicFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, DefaultValue = 25.0 },
                new DynamicFieldSchema
                {
                    Key = "company", Label = "Company",
                    Condition = f => f.GetValue<double>("age") >= 21,
                    RequiredWhen = f => f.GetValue<double>("age") >= 21
                }
            ]
        };

        var ctrl = new DynamicFormController(schema, new Dictionary<string, object?> { ["name"] = "Alice", ["company"] = "Acme" });
        Assert.False(ctrl.IsDirty);

        ctrl.SetValue("name", "  Bob  ", markTouched: true);
        Assert.Equal("  Bob  ", watched);
        Assert.True(ctrl.IsDirty);
        Assert.Contains("name", ctrl.DirtyPaths);

        // Hide company when age < 21 -> excluded from BuildOutput & validation
        ctrl.SetValue("age", 19.0);
        ctrl.SetValue("company", "");
        Assert.True(ctrl.Validate());
        var output = ctrl.BuildOutput();
        Assert.Equal("Bob", output["name"]);
        Assert.False(output.ContainsKey("company"));

        // Server blocking error blocks submission until cleared
        ctrl.SetError("name", "Taken on server", blocking: true);
        Assert.True(ctrl.HasBlockingErrors);
        Assert.False(await ctrl.SubmitAsync());
        ctrl.ClearError("name");
        Assert.True(await ctrl.SubmitAsync());
        Assert.False(ctrl.IsDirty);
    }

    [Fact]
    public async Task FormController_wizard_SkipWhen_and_ArrayTable_and_SectionStates_work_deterministically()
    {
        var itemsField = new DynamicFieldSchema
        {
            Key = "items", Label = "Items", Kind = FormFieldKind.ArrayTable,
            Fields = [new DynamicFieldSchema { Key = "sku", Label = "SKU", Required = true }]
        };
        var wizard = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "wiz",
            Steps =
            [
                new DynamicStepSchema { Key = "s1", Title = "Plan", Fields = [new DynamicFieldSchema { Key = "plan", Label = "Plan", DefaultValue = "free" }, itemsField] },
                new DynamicStepSchema { Key = "s2", Title = "Billing", SkipWhen = f => f.GetValue<string>("plan") == "free", Fields = [new DynamicFieldSchema { Key = "vat", Label = "VAT", Required = true }] },
                new DynamicStepSchema { Key = "s3", Title = "Done", Fields = [new DynamicFieldSchema { Key = "ok", Label = "OK", Kind = FormFieldKind.Checkbox, DefaultValue = true }] }
            ]
        });

        Assert.True(wizard.IsStepped);
        wizard.AddArrayItem(itemsField);
        wizard.SetArrayItemValue("items", 0, "sku", "KLY-01");
        Assert.Single(wizard.GetArrayItems("items"));

        Assert.True(await wizard.NextStepAsync());
        Assert.Equal("s3", wizard.CurrentStep?.Key); // Skipped Billing automatically!

        var sectioned = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "sec",
            Sections =
            [
                new DynamicSectionSchema { Key = "req", Label = "Required", Fields = [new DynamicFieldSchema { Key = "email", Label = "Email", Required = true }] },
                new DynamicSectionSchema { Key = "opt", Label = "Optional", Optional = true, Fields = [new DynamicFieldSchema { Key = "bio", Label = "Bio" }] }
            ]
        });
        var states = sectioned.GetSectionStates();
        Assert.Equal(FormPageSectionStatus.Pending, states[0].Status);
        Assert.Equal(FormPageSectionStatus.Optional, states[1].Status);
        sectioned.SetValue("email", "a@b.com");
        Assert.Equal(FormPageSectionStatus.Complete, sectioned.GetSectionStates()[0].Status);
    }
}
