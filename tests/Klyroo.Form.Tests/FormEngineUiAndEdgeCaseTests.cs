using Bunit;
using Klyroo.Form;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Klyroo.Form.Dynamic;

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
        var schema = new DynamicFormSchema
        {
            FormKey = "validation-matrix",
            Fields =
            [
                new DynamicFieldSchema
                {
                    Key = "code", Label = "Code", Required = true, MaxLength = 6,
                    Validators =
                    [
                        new DynamicValidatorRule("minlen", "At least 3 chars", (v, _) => (v?.ToString()?.Length ?? 0) >= 3),
                        new DynamicValidatorRule("alnum", "Uppercase alphanumeric only", (v, _) => System.Text.RegularExpressions.Regex.IsMatch(v?.ToString() ?? "", "^[A-Z0-9]+$"))
                    ]
                },
                new DynamicFieldSchema { Key = "seats", Label = "Seats", Kind = FormFieldKind.Number, Min = 1, Max = 100 },
                new DynamicFieldSchema
                {
                    Key = "domain", Label = "Domain",
                    Validators = [new DynamicValidatorRule("tld", "Must end with .dev", (v, _) => v?.ToString()?.EndsWith(".dev", StringComparison.Ordinal) == true)]
                }
            ]
        };

        var ctrl = new DynamicFormController(schema, new Dictionary<string, object?>
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
        var schema = new DynamicFormSchema
        {
            FormKey = "resets",
            Sections =
            [
                new DynamicSectionSchema
                {
                    Key = "profile", Label = "Profile",
                    Fields = [new DynamicFieldSchema { Key = "name", Label = "Name" }, new DynamicFieldSchema { Key = "role", Label = "Role" }]
                },
                new DynamicSectionSchema
                {
                    Key = "billing", Label = "Billing",
                    Fields = [new DynamicFieldSchema { Key = "currency", Label = "Currency" }]
                }
            ]
        };

        var ctrl = new DynamicFormController(schema, new Dictionary<string, object?>
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
        var linesField = new DynamicFieldSchema
        {
            Key = "lines", Label = "Lines", Kind = FormFieldKind.ArrayTable,
            Fields =
            [
                new DynamicFieldSchema { Key = "sku", Label = "SKU", Required = true },
                new DynamicFieldSchema { Key = "qty", Label = "Qty", Kind = FormFieldKind.Number, DefaultValue = 1.0 }
            ]
        };

        var ctrl = new DynamicFormController(new DynamicFormSchema { FormKey = "invoice", Fields = [linesField] });
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
        var wizard = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "checkout",
            OnBeforeNext = (_, _) => Task.FromResult(allowNext),
            Steps =
            [
                new DynamicStepSchema { Key = "info", Title = "Info", Fields = [new DynamicFieldSchema { Key = "email", Label = "Email", DefaultValue = "a@b.com" }] },
                new DynamicStepSchema { Key = "confirm", Title = "Confirm", Fields = [new DynamicFieldSchema { Key = "agree", Label = "Agree", Kind = FormFieldKind.Checkbox, DefaultValue = true }] }
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
        var tagsField = new DynamicFieldSchema
        {
            Key = "tag", Label = "Tag", Kind = FormFieldKind.Select,
            Options = [new("core", "Core")],
            OnCreateOption = label => new FormOptionItem(label.ToLowerInvariant(), label)
        };
        var lockedField = new DynamicFieldSchema
        {
            Key = "locked", Label = "Locked", DisabledWhen = _ => true
        };

        var ctrl = new DynamicFormController(new DynamicFormSchema { FormKey = "opts", Fields = [tagsField, lockedField] });
        var created = ctrl.CreateFieldOption(tagsField, "Enterprise");
        Assert.NotNull(created);
        Assert.Equal(2, ctrl.GetFieldOptions(tagsField).Count);
        Assert.True(ctrl.IsFieldDisabled(lockedField));

        Assert.True(DynamicFormController.IsEmptyValue(null));
        Assert.True(DynamicFormController.IsEmptyValue("   "));
        Assert.False(DynamicFormController.IsEmptyValue("ok"));
        Assert.False(DynamicFormController.IsEmptyValue(0));
    }

    [Fact]
    public void KlyFormEngine_and_LxFormPage_render_fields_sections_and_validation_in_bUnit()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "org-form",
            Title = "Organization",
            Fields =
            [
                new DynamicFieldSchema { Key = "orgName", Label = "Organization Name", Required = true },
                new DynamicFieldSchema { Key = "plan", Label = "Plan", Kind = FormFieldKind.Select, DefaultValue = "pro", Options = [new("pro", "Pro"), new("ent", "Enterprise")] }
            ]
        };

        var ctrl = new DynamicFormController(schema);
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, ctrl));
        Assert.Contains("Organization Name", cut.Markup);
        Assert.Contains("Plan", cut.Markup);

        var pageSchema = new DynamicFormSchema
        {
            FormKey = "settings-page",
            Title = "Workspace Settings",
            Sections =
            [
                new DynamicSectionSchema { Key = "general", Label = "General", Fields = [new DynamicFieldSchema { Key = "site", Label = "Site Name", Required = true }] },
                new DynamicSectionSchema { Key = "notes", Label = "Notes", Optional = true, Fields = [new DynamicFieldSchema { Key = "note", Label = "Note" }] }
            ]
        };
        var pageCtrl = new DynamicFormController(pageSchema);
        var pageCut = Render<KlyDynamicFormPage>(p => p.Add(x => x.Form, pageCtrl));
        Assert.Contains("General", pageCut.Markup);
        Assert.Contains("Notes", pageCut.Markup);
    }

    [Fact]
    public async Task KlyFormProvider_and_FormService_handle_both_Submit_and_Cancel_outcomes()
    {
        var provider = Render<KlyFormProvider>();
        var svc = Services.GetRequiredService<DynamicFormService>();

        var schema = new DynamicFormSchema
        {
            FormKey = "modal-create",
            Title = "Create Project",
            Fields = [new DynamicFieldSchema { Key = "name", Label = "Name", Required = true, DefaultValue = "KlyrooApp" }]
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

    [Fact]
    public async Task KlyFormProvider_submit_button_runs_schema_hooks_once()
    {
        var provider = Render<KlyFormProvider>();
        var svc = Services.GetRequiredService<DynamicFormService>();
        var beforeSubmitCalls = 0;
        var submitCalls = 0;
        var schema = new DynamicFormSchema
        {
            FormKey = "overlay-once",
            Fields = [new DynamicFieldSchema { Key = "name", Label = "Name", DefaultValue = "Klyroo" }],
            OnBeforeSubmit = _ => { beforeSubmitCalls++; return Task.FromResult(true); },
            OnSubmit = _ => { submitCalls++; return Task.CompletedTask; },
        };

        var task = svc.CreateFormAsync(schema);
        provider.Render();
        await provider.Find("[data-slot=form-engine]").SubmitAsync();

        var outcome = await task;
        Assert.True(outcome.IsCompleted);
        Assert.Equal("Klyroo", outcome.FormData["name"]);
        Assert.Equal(1, beforeSubmitCalls);
        Assert.Equal(1, submitCalls);
        Assert.False(svc.IsOpen);
    }

    [Theory]
    [InlineData(FormFieldKind.Checkbox)]
    [InlineData(FormFieldKind.Switch)]
    public void Required_checkbox_and_switch_must_be_checked(FormFieldKind kind)
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "consent",
            Fields = [new DynamicFieldSchema { Key = "terms", Label = "Terms", Kind = kind, Required = true }]
        };
        var ctrl = new DynamicFormController(schema, new Dictionary<string, object?> { ["terms"] = false });

        Assert.False(ctrl.Validate());
        Assert.Equal("This field is required", ctrl.GetError("terms"));

        ctrl.SetValue("terms", true);
        Assert.True(ctrl.Validate());
    }

    [Fact]
    public void MaxLength_is_validated_without_a_custom_rule()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "max-length",
            Fields = [new DynamicFieldSchema { Key = "code", Label = "Code", MaxLength = 4 }]
        };
        var ctrl = new DynamicFormController(schema, new Dictionary<string, object?> { ["code"] = "TOO-LONG" });

        Assert.False(ctrl.Validate());
        Assert.Equal("Must be at most 4 characters", ctrl.GetError("code"));

        ctrl.SetValue("code", "KLY4");
        Assert.True(ctrl.Validate());
    }

    [Fact]
    public void Hidden_and_omitted_list_columns_leave_the_output_rows()
    {
        var showNotes = false;
        var lines = new DynamicFieldSchema
        {
            Key = "lines", Label = "Lines", Kind = FormFieldKind.ArrayList,
            Fields =
            [
                new DynamicFieldSchema { Key = "sku", Label = "SKU" },
                new DynamicFieldSchema { Key = "notes", Label = "Notes", Condition = _ => showNotes },
                new DynamicFieldSchema { Key = "tmp", Label = "Tmp", OmitFromOutput = true },
            ],
        };
        var ctrl = new DynamicFormController(new DynamicFormSchema { FormKey = "order", Fields = [lines] });
        ctrl.AddArrayItem(lines);
        ctrl.SetArrayItemValue("lines", 0, "sku", "A-1");
        ctrl.SetArrayItemValue("lines", 0, "notes", "fragile");

        var row = Assert.Single((List<Dictionary<string, object?>>)ctrl.BuildOutput()["lines"]!);
        Assert.Equal(["sku"], row.Keys.Order());
        Assert.Equal("fragile", ctrl.GetArrayItems("lines")[0]["notes"]);

        showNotes = true;
        var shown = Assert.Single((List<Dictionary<string, object?>>)ctrl.BuildOutput()["lines"]!);
        Assert.Equal(["notes", "sku"], shown.Keys.Order());
    }
}
