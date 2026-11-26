using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class FormEngineTests : BunitContext
{
    public FormEngineTests()
    {
        Services.AddKlyrooForm();
    }

    [Fact]
    public async Task FormController_handles_conditions_dirty_tracking_transforms_and_validation()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "profile",
            Title = "User Profile",
            Fields =
            [
                new DynamicFieldSchema
                {
                    Key = "name",
                    Label = "Full Name",
                    Required = true,
                    DirtyCheck = true,
                    TransformOutput = v => v?.ToString()?.Trim(),
                },
                new DynamicFieldSchema
                {
                    Key = "age",
                    Label = "Age",
                    Kind = FormFieldKind.Number,
                    Min = 18,
                    Max = 120,
                    DefaultValue = 25.0,
                },
                new DynamicFieldSchema
                {
                    Key = "company",
                    Label = "Company",
                    Required = true,
                    Condition = f => f.GetValue<double>("age") >= 21,
                },
                new DynamicFieldSchema
                {
                    Key = "password",
                    Label = "Password",
                    Kind = FormFieldKind.Password,
                    PasswordRequirements =
                    [
                        new FormPasswordRequirement("len", "At least 8 characters", s => s.Length >= 8)
                    ]
                }
            ]
        };

        var controller = new DynamicFormController(schema, new Dictionary<string, object?> { ["name"] = "Alice" });
        Assert.False(controller.IsDirty);

        var cut = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, controller));
        Assert.Equal("User Profile", cut.Find("[data-slot=form-title]").TextContent);

        // Company is visible because age default is 25 >= 21, and required
        Assert.False(await controller.SubmitAsync());
        cut.Render();
        Assert.Contains("This field is required", cut.Find("[data-form-field=company] [data-slot=form-field-error]").TextContent);

        // Set age to 19 -> hides company -> company is skipped in validation & output!
        controller.SetValue("age", 19.0, markTouched: true);
        Assert.True(controller.IsDirty);
        Assert.Contains("age", controller.DirtyPaths);

        // Password too short fails requirement
        controller.SetValue("password", "short", markTouched: true);
        Assert.False(controller.Validate());
        Assert.Equal("At least 8 characters", controller.GetError("password"));

        // Fix password and add trailing whitespace to name to test TransformOutput
        controller.SetValue("password", "strongpass123", markTouched: true);
        controller.SetValue("name", "  Alice Smith  ", markTouched: true);
        Assert.True(await controller.SubmitAsync());

        // After successful submit, snapshot is rebaselined so IsDirty is false
        Assert.False(controller.IsDirty);
        var output = controller.BuildOutput();
        Assert.Equal("Alice Smith", output["name"]);
        Assert.False(output.ContainsKey("company"));
    }

    [Fact]
    public async Task KlyFormEngine_supports_multi_step_wizard_with_skipStep_and_array_fields()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "onboarding",
            Title = "Onboarding",
            Steps =
            [
                new DynamicStepSchema
                {
                    Key = "account",
                    Title = "Account",
                    Fields =
                    [
                        new DynamicFieldSchema { Key = "plan", Label = "Plan", Required = true, DefaultValue = "free" }
                    ]
                },
                new DynamicStepSchema
                {
                    Key = "billing",
                    Title = "Billing",
                    SkipWhen = f => f.GetValue<string>("plan") == "free",
                    Fields =
                    [
                        new DynamicFieldSchema { Key = "vat", Label = "VAT Number", Required = true }
                    ]
                },
                new DynamicStepSchema
                {
                    Key = "team",
                    Title = "Team",
                    Fields =
                    [
                        new DynamicFieldSchema
                        {
                            Key = "members",
                            Label = "Members",
                            Kind = FormFieldKind.ArrayList,
                            Fields = [new DynamicFieldSchema { Key = "email", Label = "Email" }]
                        }
                    ]
                }
            ]
        };

        var controller = new DynamicFormController(schema);
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Form, controller));

        // Step 0 ("account") -> Next should skip Step 1 ("billing", since plan == "free") and land on Step 2 ("team")
        Assert.Equal(0, controller.CurrentStepIndex);
        await cut.InvokeAsync(() => controller.NextStepAsync());
        Assert.Equal(2, controller.CurrentStepIndex);

        // Add an array item on Step 2
        cut.Find("[data-slot=form-array-add]").Click();
        Assert.Single(controller.GetArrayItems("members"));
        cut.Find("[data-slot=form-array-input]").Input("dev@klyroo.dev");
        Assert.Equal("dev@klyroo.dev", controller.GetArrayItems("members")[0]["email"]);
    }

    [Fact]
    public async Task KlyFormPage_and_LxFormProvider_track_section_statuses_and_resolve_overlay_forms()
    {
        var pageSchema = new DynamicFormSchema
        {
            FormKey = "settings",
            Title = "Workspace Settings",
            Sections =
            [
                new DynamicSectionSchema
                {
                    Key = "general",
                    Label = "General",
                    Fields =
                    [
                        new DynamicFieldSchema { Key = "workspaceName", Label = "Workspace Name", Required = true }
                    ]
                },
                new DynamicSectionSchema
                {
                    Key = "notes",
                    Label = "Notes",
                    Optional = true,
                    Fields =
                    [
                        new DynamicFieldSchema { Key = "bio", Label = "Bio" }
                    ]
                }
            ]
        };

        var pageController = new DynamicFormController(pageSchema);
        var pageCut = Render<KlyDynamicFormPage>(p => p.Add(x => x.Form, pageController));

        var statuses = pageController.GetSectionStates();
        Assert.Equal(FormPageSectionStatus.Pending, statuses[0].Status);
        Assert.Equal(FormPageSectionStatus.Optional, statuses[1].Status);
        Assert.Contains("1 left to complete.", pageCut.Find("[data-slot=form-page-summary]").TextContent);

        pageController.SetValue("workspaceName", "Acme Corp", markTouched: true);
        pageCut.Render();
        Assert.Equal(FormPageSectionStatus.Complete, pageController.GetSectionStates()[0].Status);
        Assert.Contains("Everything is ready.", pageCut.Find("[data-slot=form-page-summary]").TextContent);

        // Test programmatic overlay via DynamicFormService + KlyFormProvider
        var providerCut = Render<KlyFormProvider>();
        var formService = Services.GetRequiredService<DynamicFormService>();
        var modalSchema = new DynamicFormSchema
        {
            Title = "Quick Create",
            Fields = [new DynamicFieldSchema { Key = "title", Label = "Title", DefaultValue = "New Task" }]
        };

        var task = formService.CreateFormAsync(modalSchema);
        providerCut.Render();
        Assert.True(formService.IsOpen);
        Assert.NotNull(providerCut.Find("[data-slot=form-provider-overlay]"));

        await providerCut.InvokeAsync(() => formService.SubmitActiveFormAsync());
        var outcome = await task;
        Assert.True(outcome.IsCompleted);
        Assert.Equal("New Task", outcome.FormData["title"]);
    }
}
