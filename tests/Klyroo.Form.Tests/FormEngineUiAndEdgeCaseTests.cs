using Bunit;
using Klyroo.Form;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class FormEngineUiAndEdgeCaseTests : BunitContext
{
    public FormEngineUiAndEdgeCaseTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void FormController_validates_Min_Max_MaxLength_and_Custom_Validators()
    {
        var schema = new FormSchema
        {
            FormKey = "validation-matrix",
            Fields =
            [
                new FormFieldSchema
                {
                    Key = "code", Label = "Code", Required = true, MaxLength = 6,
                    Validators =
                    [
                        new FormValidatorRule("minlen", "At least 3 chars", (v, _) => (v?.ToString()?.Length ?? 0) >= 3),
                        new FormValidatorRule("alnum", "Uppercase alphanumeric only", (v, _) => System.Text.RegularExpressions.Regex.IsMatch(v?.ToString() ?? "", "^[A-Z0-9]+$"))
                    ]
                },
                new FormFieldSchema { Key = "seats", Label = "Seats", Kind = FormFieldKind.Number, Min = 1, Max = 100 },
                new FormFieldSchema
                {
                    Key = "domain", Label = "Domain",
                    Validators = [new FormValidatorRule("tld", "Must end with .dev", (v, _) => v?.ToString()?.EndsWith(".dev", StringComparison.Ordinal) == true)]
                }
            ]
        };

        var ctrl = new FormController(schema, new Dictionary<string, object?>
        {
            ["code"] = "ab",
            ["seats"] = 250.0,
            ["domain"] = "example.com"
        });

        Assert.False(ctrl.Validate());
        Assert.NotNull(ctrl.GetError("code"));
        Assert.NotNull(ctrl.GetError("seats"));
        Assert.Equal("Must end with .dev", ctrl.GetError("domain"));

        ctrl.SetValue("code", "TOO-LONG-99");
        Assert.False(ctrl.Validate());
        Assert.NotNull(ctrl.GetError("code"));

        ctrl.SetValue("code", "KLY42");
        ctrl.SetValue("seats", 25.0);
        ctrl.SetValue("domain", "klyroo.dev");
        Assert.True(ctrl.Validate());
        Assert.Null(ctrl.GetError("code"));
        Assert.Null(ctrl.GetError("seats"));
        Assert.Null(ctrl.GetError("domain"));
    }

    [Fact]
    public void FormController_ResetField_ResetSection_and_Reset_with_new_input_restore_clean_state()
    {
        var schema = new FormSchema
        {
            FormKey = "resets",
            Sections =
            [
                new FormPageSectionSchema
                {
                    Key = "profile", Label = "Profile",
                    Fields = [new FormFieldSchema { Key = "name", Label = "Name" }, new FormFieldSchema { Key = "role", Label = "Role" }]
                },
                new FormPageSectionSchema
                {
                    Key = "billing", Label = "Billing",
                    Fields = [new FormFieldSchema { Key = "currency", Label = "Currency" }]
                }
            ]
        };

        var ctrl = new FormController(schema, new Dictionary<string, object?>
        {
            ["name"] = "Alice", ["role"] = "Admin", ["currency"] = "EUR"
        });

        ctrl.SetValue("name", "Bob");
        ctrl.SetValue("role", "Owner");
        ctrl.SetValue("currency", "USD");
        Assert.Equal(3, ctrl.DirtyPaths.Count);

        ctrl.ResetField("name");
        Assert.Equal("Alice", ctrl.GetValue<string>("name"));
        Assert.Equal(2, ctrl.DirtyPaths.Count);

        ctrl.ResetSection("profile");
        Assert.Equal("Admin", ctrl.GetValue<string>("role"));
        Assert.Single(ctrl.DirtyPaths);

        ctrl.Reset(new Dictionary<string, object?> { ["name"] = "Claire", ["role"] = "Member", ["currency"] = "GBP" });
        Assert.False(ctrl.IsDirty);
        Assert.Equal("Claire", ctrl.GetValue<string>("name"));
        Assert.Equal("GBP", ctrl.GetValue<string>("currency"));
    }

    [Fact]
    public void FormController_ArrayTable_supports_Add_Update_Remove_and_serializes_cleanly()
    {
        var linesField = new FormFieldSchema
        {
            Key = "lines", Label = "Lines", Kind = FormFieldKind.ArrayTable,
            Fields =
            [
                new FormFieldSchema { Key = "sku", Label = "SKU", Required = true },
                new FormFieldSchema { Key = "qty", Label = "Qty", Kind = FormFieldKind.Number, DefaultValue = 1.0 }
            ]
        };

        var ctrl = new FormController(new FormSchema { FormKey = "invoice", Fields = [linesField] });
        ctrl.AddArrayItem(linesField);
        ctrl.AddArrayItem(linesField);
        ctrl.SetArrayItemValue("lines", 0, "sku", "A-100");
        ctrl.SetArrayItemValue("lines", 1, "sku", "B-200");
        ctrl.SetArrayItemValue("lines", 1, "qty", 5.0);

        Assert.Equal(2, ctrl.GetArrayItems("lines").Count);
        ctrl.RemoveArrayItem("lines", 0);

        var items = ctrl.GetArrayItems("lines");
        Assert.Single(items);
        Assert.Equal("B-200", items[0]["sku"]);
        Assert.Equal(5.0, items[0]["qty"]);
        Assert.True(ctrl.Validate());
    }

    [Fact]
    public async Task FormController_wizard_honors_OnBeforeNext_and_PreviousStepAsync()
    {
        var allowNext = false;
        var wizard = new FormController(new FormSchema
        {
            FormKey = "checkout",
            OnBeforeNext = (_, _) => Task.FromResult(allowNext),
            Steps =
            [
                new FormStepSchema { Key = "info", Title = "Info", Fields = [new FormFieldSchema { Key = "email", Label = "Email", DefaultValue = "a@b.com" }] },
                new FormStepSchema { Key = "confirm", Title = "Confirm", Fields = [new FormFieldSchema { Key = "agree", Label = "Agree", Kind = FormFieldKind.Checkbox, DefaultValue = true }] }
            ]
        });

        Assert.False(await wizard.NextStepAsync());
        Assert.Equal("info", wizard.CurrentStep?.Key);

        allowNext = true;
        Assert.True(await wizard.NextStepAsync());
        Assert.Equal("confirm", wizard.CurrentStep?.Key);

        Assert.True(await wizard.PreviousStepAsync());
        Assert.Equal("info", wizard.CurrentStep?.Key);
    }

    [Fact]
    public void FormController_CreateFieldOption_and_DisabledWhen_and_IsEmptyValue_work()
    {
        var tagsField = new FormFieldSchema
        {
            Key = "tag", Label = "Tag", Kind = FormFieldKind.Select,
            Options = [new("core", "Core")],
            OnCreateOption = label => new FormOptionItem(label.ToLowerInvariant(), label)
        };
        var lockedField = new FormFieldSchema
        {
            Key = "locked", Label = "Locked", DisabledWhen = _ => true
        };

        var ctrl = new FormController(new FormSchema { FormKey = "opts", Fields = [tagsField, lockedField] });
        var created = ctrl.CreateFieldOption(tagsField, "Enterprise");
        Assert.NotNull(created);
        Assert.Equal(2, ctrl.GetFieldOptions(tagsField).Count);
        Assert.True(ctrl.IsFieldDisabled(lockedField));

        Assert.True(FormController.IsEmptyValue(null));
        Assert.True(FormController.IsEmptyValue("   "));
        Assert.False(FormController.IsEmptyValue("ok"));
        Assert.False(FormController.IsEmptyValue(0));
    }

    [Fact]
    public void KlyFormEngine_and_LxFormPage_render_fields_sections_and_validation_in_bUnit()
    {
        var schema = new FormSchema
        {
            FormKey = "org-form",
            Title = "Organization",
            Fields =
            [
                new FormFieldSchema { Key = "orgName", Label = "Organization Name", Required = true },
                new FormFieldSchema { Key = "plan", Label = "Plan", Kind = FormFieldKind.Select, DefaultValue = "pro", Options = [new("pro", "Pro"), new("ent", "Enterprise")] }
            ]
        };

        var ctrl = new FormController(schema);
        var cut = Render<KlyFormEngine>(p => p.Add(x => x.Form, ctrl));
        Assert.Contains("Organization Name", cut.Markup);
        Assert.Contains("Plan", cut.Markup);

        var pageSchema = new FormSchema
        {
            FormKey = "settings-page",
            Title = "Workspace Settings",
            Sections =
            [
                new FormPageSectionSchema { Key = "general", Label = "General", Fields = [new FormFieldSchema { Key = "site", Label = "Site Name", Required = true }] },
                new FormPageSectionSchema { Key = "notes", Label = "Notes", Optional = true, Fields = [new FormFieldSchema { Key = "note", Label = "Note" }] }
            ]
        };
        var pageCtrl = new FormController(pageSchema);
        var pageCut = Render<KlyFormPage>(p => p.Add(x => x.Form, pageCtrl));
        Assert.Contains("General", pageCut.Markup);
        Assert.Contains("Notes", pageCut.Markup);
    }

    [Fact]
    public async Task KlyFormProvider_and_FormService_handle_both_Submit_and_Cancel_outcomes()
    {
        var provider = Render<KlyFormProvider>();
        var svc = Services.GetRequiredService<FormService>();

        var schema = new FormSchema
        {
            FormKey = "modal-create",
            Title = "Create Project",
            Fields = [new FormFieldSchema { Key = "name", Label = "Name", Required = true, DefaultValue = "KlyrooApp" }]
        };

        // 1. Open in Modal & submit
        var task1 = svc.CreateFormAsync(schema, mode: FormShellMode.Modal);
        provider.Render();
        Assert.Contains("Create Project", provider.Markup);

        var ok = await provider.InvokeAsync(() => svc.SubmitActiveFormAsync());
        Assert.True(ok);
        var outcome1 = await task1;
        Assert.True(outcome1.IsCompleted);
        Assert.Equal("KlyrooApp", outcome1.FormData["name"]);

        // 2. Open in Drawer & close (cancel)
        var task2 = svc.CreateFormAsync(schema, mode: FormShellMode.Drawer);
        provider.Render();
        await provider.InvokeAsync(() => svc.CloseForm());
        var outcome2 = await task2;
        Assert.False(outcome2.IsCompleted);
    }
}
