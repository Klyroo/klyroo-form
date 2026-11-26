using Bunit;
using Klyroo.UI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class AccessibilityTests : BunitContext
{
    private const string ModulePath = "./_content/Klyroo.Form/klyroo-form.js";

    private readonly BunitJSModuleInterop _module;

    public AccessibilityTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(ModulePath);
        _module.Mode = JSRuntimeMode.Loose;
    }

    private static string[] FocusedIds(BunitJSModuleInterop module) =>
        module.Invocations.Where(i => i.Identifier == "focusById").Select(i => (string)i.Arguments[0]!).ToArray();

    [Fact]
    public async Task Provider_overlay_is_a_modal_dialog_that_closes_the_form_when_the_dialog_closes()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<DynamicFormService>();
        var task = service.CreateFormAsync(new DynamicFormSchema { Title = "Quick create", Fields = [new DynamicFieldSchema { Key = "a" }] });
        provider.Render();

        var modal = provider.Find("kly-modal");
        Assert.True(modal.HasAttribute("open"));
        Assert.True(modal.HasAttribute("dismissible"));
        Assert.Equal("Quick create", provider.Find("dialog").GetAttribute("aria-label"));

        // Escape, the backdrop and the browser all end up as the kly-toggle event of the modal.
        await modal.TriggerEventAsync("onklytoggle", new KlyToggleEventArgs { Open = false, Sequence = 1 });

        Assert.False(service.IsOpen);
        var outcome = await task;
        Assert.False(outcome.IsCompleted);
        Assert.False(provider.Find("kly-modal").HasAttribute("open"));
        Assert.Empty(provider.FindAll("[data-slot=form-engine]"));
    }

    [Fact]
    public async Task Provider_overlay_cannot_be_dismissed_while_the_form_is_saving()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<DynamicFormService>();
        var release = new TaskCompletionSource();
        _ = service.CreateFormAsync(new DynamicFormSchema
        {
            Fields = [new DynamicFieldSchema { Key = "a" }],
            OnSubmit = _ => release.Task,
        });
        provider.Render();
        Assert.True(provider.Find("kly-modal").HasAttribute("dismissible"));

        var submit = provider.Find("form").SubmitAsync();
        provider.WaitForAssertion(() => Assert.False(provider.Find("kly-modal").HasAttribute("dismissible")));
        Assert.True(provider.Find("[data-slot=form-action-cancel]").HasAttribute("disabled"));

        release.SetResult();
        await submit;
        Assert.False(service.IsOpen);
    }

    [Fact]
    public async Task Failed_submit_focuses_the_first_invalid_field()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "f",
            Fields =
            [
                new DynamicFieldSchema { Key = "name", Label = "Name", DefaultValue = "ok" },
                new DynamicFieldSchema { Key = "email", Label = "Email", Required = true },
                new DynamicFieldSchema { Key = "phone", Label = "Phone", Required = true },
            ],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal(["kly-ff-email"], FocusedIds(_module)));
        Assert.NotNull(cut.Find("#kly-ff-email"));
    }

    [Fact]
    public async Task Failed_Next_focuses_the_first_invalid_field_of_the_step()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "wizard",
            Steps =
            [
                new DynamicStepSchema { Key = "a", Title = "A", Fields = [new DynamicFieldSchema { Key = "first", Required = true }] },
                new DynamicStepSchema { Key = "b", Title = "B", Fields = [new DynamicFieldSchema { Key = "second" }] },
            ],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        await cut.Find("[data-slot=form-action-next]").ClickAsync();

        cut.WaitForAssertion(() => Assert.Equal(["kly-ff-first"], FocusedIds(_module)));
        Assert.Equal(0, form.CurrentStepIndex);
    }

    [Fact]
    public async Task A_form_level_error_alone_moves_focus_to_the_summary()
    {
        var form = new DynamicFormController(new DynamicFormSchema { FormKey = "f", Fields = [new DynamicFieldSchema { Key = "name" }] });
        form.SetFormError("Server unavailable");
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal(["kly-ff-summary-f"], FocusedIds(_module)));
        Assert.Equal("-1", cut.Find("[data-slot=form-error-summary]").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task A_failed_cell_focuses_the_cell_control()
    {
        var lines = new DynamicFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayList,
            Fields = [new DynamicFieldSchema { Key = "sku", Label = "SKU", Required = true }],
        };
        var form = new DynamicFormController(new DynamicFormSchema { FormKey = "f", Fields = [lines] });
        form.AddArrayItem(lines);
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal(["kly-ff-lines-0-sku"], FocusedIds(_module)));
        Assert.NotNull(cut.Find("#kly-ff-lines-0-sku"));
        var summaryLink = cut.Find("[data-slot=form-error-summary-link]");
        Assert.Contains("SKU (lines, 1)", summaryLink.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Summary_link_focuses_its_field()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "f",
            Fields = [new DynamicFieldSchema { Key = "email", Label = "Email", Required = true }],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));
        await cut.Find("form").SubmitAsync();
        cut.WaitForAssertion(() => Assert.Single(FocusedIds(_module)));

        await cut.Find("[data-slot=form-error-summary-link]").ClickAsync();

        Assert.Equal(["kly-ff-email", "kly-ff-email"], FocusedIds(_module));
    }

    [Fact]
    public async Task A_successful_submit_does_not_move_focus()
    {
        var form = new DynamicFormController(new DynamicFormSchema { FormKey = "f", Fields = [new DynamicFieldSchema { Key = "name", DefaultValue = "a" }] });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));

        await cut.Find("form").SubmitAsync();

        Assert.DoesNotContain(_module.Invocations, i => i.Identifier == "focusById");
    }

    [Fact]
    public async Task Field_errors_are_linked_with_aria_describedby_and_are_not_alerts()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "f",
            Fields = [new DynamicFieldSchema { Key = "email", Label = "Email", Required = true, Help = "Work address" }],
        });
        var cut = Render<KlyDynamicFormEngine>(p => p.Add(c => c.Form, form));
        await cut.Find("form").SubmitAsync();

        var input = cut.Find("#kly-ff-email");
        var error = cut.Find("[data-slot=form-field-error]");
        Assert.Null(error.GetAttribute("role"));
        Assert.Contains(error.Id!, input.GetAttribute("aria-describedby"), StringComparison.Ordinal);
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll("[role=alert]"));
        var live = cut.Find("[data-slot=form-live-region]");
        Assert.Equal("polite", live.GetAttribute("aria-live"));
        Assert.Single(cut.FindAll("[aria-live]"));
    }
}
