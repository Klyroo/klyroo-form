using Bunit;
using Klyroo.Form.Dynamic;
using Klyroo.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class TypedComponentTests : BunitContext
{
    private const string ModulePath = "./_content/Klyroo.Form/klyroo-form.js";

    private readonly BunitJSModuleInterop _module;

    public TypedComponentTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(ModulePath);
        _module.Mode = JSRuntimeMode.Loose;
    }

    private static readonly SignupForm Signup = new();

    private static string[] FocusedIds(BunitJSModuleInterop module) =>
        module.Invocations.Where(i => i.Identifier == "focusById").Select(i => (string)i.Arguments[0]!).ToArray();

    [Fact]
    public async Task The_engine_infers_the_model_and_submits_the_model_it_was_given()
    {
        var model = new Signup { Id = 7, Name = "Ada" };
        Signup? saved = null;
        Signup? submitted = null;
        var cut = Render<TypedSignupPage>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Save, (m, _) => { saved = m; return Task.FromResult(true); })
            .Add(x => x.Submitted, EventCallback.Factory.Create<Signup>(this, m => submitted = m)));

        Assert.Equal("Sign up", cut.Find("[data-slot=form-title]").TextContent);
        cut.Find("[data-form-field=Name] input").Input("Grace");
        cut.Find("[data-form-field=Email] input").Input("grace@example.com");
        await cut.Find("form").SubmitAsync();

        Assert.Same(model, saved);
        Assert.Same(model, submitted);
        Assert.Equal(7, model.Id);
        Assert.Equal("Grace", model.Name);
        Assert.Equal("grace@example.com", model.Email);
    }

    [Fact]
    public async Task Without_a_model_a_submit_creates_a_new_one()
    {
        Signup? saved = null;
        var cut = Render<TypedSignupPage>(p => p.Add(x => x.Save, (m, _) => { saved = m; return Task.FromResult(true); }));

        cut.Find("[data-form-field=Name] input").Input("Grace");
        await cut.Find("form").SubmitAsync();

        Assert.NotNull(saved);
        Assert.Equal("Grace", saved.Name);
        Assert.Equal(3, saved.Rating);
    }

    [Fact]
    public async Task A_failed_save_puts_the_model_back_and_raises_no_submit()
    {
        var model = new Signup { Name = "Ada" };
        var submitted = 0;
        var cut = Render<TypedSignupPage>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Save, (_, _) => Task.FromResult(false))
            .Add(x => x.Submitted, EventCallback.Factory.Create<Signup>(this, _ => submitted++)));

        cut.Find("[data-form-field=Name] input").Input("Grace");
        await cut.Find("form").SubmitAsync();

        Assert.Equal("Ada", model.Name);
        Assert.Equal(0, submitted);
    }

    [Fact]
    public async Task A_save_that_throws_shows_a_message_and_rolls_back()
    {
        var model = new Signup { Name = "Ada" };
        var cut = Render<TypedSignupPage>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.Save, (_, _) => throw new InvalidOperationException("boom")));

        cut.Find("[data-form-field=Name] input").Input("Grace");
        await cut.Find("form").SubmitAsync();

        Assert.Equal("Ada", model.Name);
        Assert.Contains("Something went wrong", cut.Find("[data-slot=form-error-summary]").TextContent);
    }

    [Fact]
    public async Task The_engine_renders_a_controller_given_through_Form_and_infers_the_model_from_it()
    {
        var controller = new FormController<Signup>(Signup.Schema, new Signup { Name = "Ada" });
        var cut = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Form, controller));

        Assert.Equal("Ada", cut.Find("[data-form-field=Name] input").GetAttribute("value"));
        cut.Find("[data-form-field=Name] input").Input("Grace");
        Assert.Equal("Grace", controller.Get(Signup.Name));
        Assert.True(controller.IsDirty);
        Assert.Same(controller, cut.Instance.EffectiveController);
        await cut.InvokeAsync(() => controller.Reset());
        cut.WaitForAssertion(() => Assert.Equal("Ada", cut.Find("[data-form-field=Name] input").GetAttribute("value")));
    }

    [Fact]
    public void A_new_model_instance_reloads_the_form()
    {
        var cut = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, Signup.Schema).Add(x => x.Model, new Signup { Name = "Ada" }));
        Assert.Equal("Ada", cut.Find("[data-form-field=Name] input").GetAttribute("value"));

        cut.Render(p => p.Add(x => x.Schema, Signup.Schema).Add(x => x.Model, new Signup { Name = "Grace" }));

        Assert.Equal("Grace", cut.Find("[data-form-field=Name] input").GetAttribute("value"));
    }

    [Fact]
    public void Cancel_and_the_close_button_raise_OnCancel_only_when_it_is_bound()
    {
        var plain = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, Signup.Schema));
        Assert.Empty(plain.FindAll("[data-slot=form-action-cancel]"));

        var cancelled = 0;
        var cut = Render<KlyFormEngine<Signup>>(p => p
            .Add(x => x.Schema, Signup.Schema)
            .Add(x => x.Shell, FormShellMode.Modal)
            .Add(x => x.OnCancel, EventCallback.Factory.Create(this, () => cancelled++)));
        cut.Find("[data-slot=form-action-cancel]").Click();
        cut.Find("[data-slot=form-close]").Click();

        Assert.Equal(2, cancelled);
    }

    [Fact]
    public void The_shell_the_labels_and_the_reset_button_work_like_the_dynamic_engine()
    {
        var cut = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, Signup.Schema).Add(x => x.SubmitLabel, "Join"));

        Assert.Equal("Join", cut.Find("[data-slot=form-action-submit]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-slot=form-action-reset]"));
        cut.Find("[data-form-field=Name] input").Input("x");
        cut.Find("[data-slot=form-action-reset]").Click();
        Assert.Equal("", cut.Find("[data-form-field=Name] input").GetAttribute("value") ?? "");
    }

    [Fact]
    public async Task A_sectioned_page_renders_navigation_and_submits_the_typed_model()
    {
        var schema = FormSchema.For<Signup>(s =>
        {
            s.Title = "Account";
            s.Section("who", "Who", sec => { sec.Field(m => m.Name, f => f.Required()); sec.Field(m => m.Email); });
            s.Section("how", "How much", sec => sec.Field(m => m.Rating, f => f.Range(1, 5)), optional: true);
        });
        Signup? saved = null;
        var cut = Render<TypedPageSample>(p => p
            .Add(x => x.Schema, schema)
            .Add(x => x.Save, (m, _) => { saved = m; return Task.FromResult(true); }));

        Assert.Equal(2, cut.FindAll("[data-slot=form-page-nav-item]").Count);
        Assert.Equal(2, cut.FindAll("[data-slot=form-page-section]").Count);
        cut.Find("[data-form-field=Name] input").Input("Grace");
        await cut.Find("form").SubmitAsync();

        Assert.Equal("Grace", saved?.Name);
    }

    [Fact]
    public void A_template_draws_the_control_of_its_field_with_the_value_and_the_id_of_the_context()
    {
        var page = Render<TypedSignupPage>();

        var rating = page.Find("[data-form-field=Rating] [data-testid=rating]");
        Assert.Equal("kly-ff-Rating", rating.Id);
        Assert.Equal("3", rating.GetAttribute("value"));
        Assert.Empty(page.FindAll("[data-form-field=Rating] input[type=number]"));
    }

    [Fact]
    public async Task A_template_sets_typed_values_and_text_on_the_controller_of_the_page()
    {
        Signup? saved = null;
        var page = Render<TypedSignupPage>(p => p.Add(x => x.Save, (m, _) => { saved = m; return Task.FromResult(true); }));

        page.Find("[data-form-field=Name] input").Input("Ada");
        page.Find("[data-testid=rating]").Input("5");
        page.Find("[data-testid=nick]").Input("ada");
        await page.Find("form").SubmitAsync();

        Assert.Equal(5, saved?.Rating);
        Assert.Equal("ada", saved?.Nickname);
    }

    [Fact]
    public async Task A_template_sits_inside_the_label_description_help_and_error_of_the_field()
    {
        var page = Render<TypedSignupPage>();

        // The label points at the control of the template, and the description and help are tied to it.
        var name = page.Find("[data-form-field=Name]");
        Assert.Equal("kly-ff-Name", name.QuerySelector("label")!.GetAttribute("for"));
        Assert.Equal("kly-ff-Name-description kly-ff-Name-help", name.QuerySelector("input")!.GetAttribute("aria-describedby"));

        await page.Find("form").SubmitAsync();
        var error = page.Find("[data-form-field=Name] [data-slot=form-field-error]");
        Assert.Equal("kly-ff-Name-error", error.Id);
        Assert.Equal("kly-ff-Name-description kly-ff-Name-help kly-ff-Name-error", page.Find("[data-form-field=Name] input").GetAttribute("aria-describedby"));
        Assert.Equal("true", page.Find("[data-form-field=Name] input").GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task A_failed_submit_focuses_the_first_invalid_field_and_lists_it_in_the_summary()
    {
        var page = Render<TypedSignupPage>();

        await page.Find("form").SubmitAsync();

        page.WaitForAssertion(() => Assert.Equal(["kly-ff-Name"], FocusedIds(_module)));
        var summary = page.Find("[data-slot=form-error-summary]");
        Assert.Equal("-1", summary.GetAttribute("tabindex"));
        Assert.Contains("Name", page.Find("[data-slot=form-error-summary-link]").TextContent);
        Assert.NotNull(page.Find("[data-slot=form-live-region]"));
    }

    [Fact]
    public async Task A_form_level_error_alone_moves_focus_to_the_summary()
    {
        var controller = new FormController<Signup>(Signup.Schema, new Signup { Name = "Ada" });
        controller.SetFormError("Server unavailable");
        var cut = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Form, controller));

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal(["kly-ff-summary-" + controller.FormKey], FocusedIds(_module)));
    }

    [Fact]
    public async Task The_error_summary_and_the_guard_accept_a_typed_controller()
    {
        var controller = new FormController<Signup>(Signup.Schema);
        controller.Validate();
        var summary = Render<KlyFormErrorSummary>(p => p.Add(x => x.Form, controller));
        Assert.Empty(summary.FindAll("[data-slot=form-error-summary]"));

        await controller.SubmitAsync();
        summary.Render();
        Assert.Single(summary.FindAll("[data-slot=form-error-summary-item]"));
        Render<KlyFormNavigationGuard>(p => p.Add(x => x.Form, controller));
        Render<KlyFormErrorSummary>(p => p.Add(x => x.Form, new DynamicFormController(new DynamicFormSchema())));
    }

    [Fact]
    public void A_field_view_draws_one_field_of_a_controller_in_a_layout_of_your_own()
    {
        var controller = new FormController<Signup>(Signup.Schema, new Signup { Name = "Ada" });
        var cut = Render<TypedLayoutPage>(p => p.Add(x => x.Form, Signup).Add(x => x.Controller, controller));

        Assert.Equal(2, cut.FindAll("[data-slot=form-field]").Count);
        Assert.Equal("Ada", cut.Find("[data-form-field=Name] input").GetAttribute("value"));
        cut.Find("[data-form-field=Name] input").Input("Grace");
        Assert.Equal("Grace", controller.Get(Signup.Name));
    }

    [Fact]
    public void A_field_view_renders_nothing_for_a_hidden_field()
    {
        var schema = FormSchema.For<Signup>(s =>
        {
            var name = s.Field(m => m.Name);
            s.Field(m => m.Email, f => f.VisibleWhen(c => c.Get(name) == "show"));
        });
        var controller = new FormController<Signup>(schema);
        var cut = Render<KlyFormFieldView<Signup>>(p => p.Add(x => x.Field, schema.Field(m => m.Email)).Add(x => x.Form, controller));
        Assert.Empty(cut.Markup.Trim());
        controller.Set(schema.Field(m => m.Name), "show");
        cut.Render();
        Assert.NotEmpty(cut.FindAll("input"));
    }

    private static RenderFragment<FormControlContext<string>> TextBox(string marker) => ctx => builder =>
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "id", ctx.Id);
        builder.AddAttribute(2, "data-marker", marker);
        builder.AddAttribute(3, "value", ctx.Text);
        builder.AddAttribute(4, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(Owner, e => ctx.SetText(e.Value?.ToString())));
        builder.CloseElement();
    };

    private static readonly object Owner = new();

    [Fact]
    public void A_global_custom_renderer_of_the_value_type_draws_the_field()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f.CustomField("nick", TextBox("registered")));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var controller = new FormController<Signup>(Signup.Schema, new Signup { Nickname = "ada" });
        var cut = ctx.Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Form, controller));

        var input = cut.Find("[data-form-field=Nickname] input[data-marker=registered]");
        Assert.Equal("ada", input.GetAttribute("value"));
        Assert.Equal("kly-ff-Nickname", input.Id);
    }

    [Fact]
    public void A_renderer_for_another_value_type_shows_the_no_renderer_notice()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f.CustomField<int>("nick", c => b => b.AddContent(0, "never")));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, Signup.Schema));

        var notice = cut.Find("[data-form-field=Nickname] [data-slot=form-custom-missing]");
        Assert.Contains("nick", notice.TextContent);
        Assert.DoesNotContain("never", cut.Markup);
    }

    [Fact]
    public void A_dynamic_renderer_is_not_used_by_a_typed_field_and_the_other_way_round()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddKlyrooForm(f => f.CustomField("nick", (RenderFragment<DynamicControlContext>)(c => b => b.AddContent(0, "dynamic"))));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var typed = ctx.Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, Signup.Schema));
        Assert.Single(typed.FindAll("[data-slot=form-custom-missing]"));

        var dynamic = ctx.Render<KlyDynamicFormEngine>(p => p.Add(x => x.Schema, new DynamicFormSchema
        {
            Fields = [new DynamicFieldSchema { Key = "nick", Kind = FormFieldKind.Custom, CustomKey = "nick" }],
        }));
        Assert.Contains("dynamic", dynamic.Markup);
    }

    [Fact]
    public void A_custom_field_without_key_or_template_names_itself_in_the_notice()
    {
        var schema = FormSchema.For<Signup>(s => s.Field(m => m.Nickname, f => f.AsCustom()));
        var bare = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, schema));
        Assert.Contains("Nickname", bare.Find("[data-slot=form-custom-missing]").TextContent);
    }

    [Fact]
    public async Task The_overlay_service_opens_a_typed_form_and_returns_the_submitted_model()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<FormService>();
        var model = new Signup { Id = 4, Name = "Ada" };

        var task = service.CreateFormAsync(Signup.Schema, model, FormShellMode.Drawer);
        provider.Render();
        Assert.True(service.IsOpen);
        Assert.Equal("Sign up", provider.Find("[data-slot=form-title]").TextContent);
        provider.Find("[data-form-field=Name] input").Input("Grace");
        await provider.Find("[data-slot=form-engine]").SubmitAsync();

        var outcome = await task;
        Assert.True(outcome.IsCompleted);
        Assert.Same(model, outcome.Model);
        Assert.Equal("Grace", outcome.Model!.Name);
        Assert.False(service.IsOpen);
    }

    [Fact]
    public async Task The_overlay_service_creates_a_model_and_reports_a_close_and_a_replacement()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<FormService>();

        var first = service.CreateFormAsync(Signup.Schema);
        provider.Render();
        var ok = await provider.InvokeAsync(() => service.SubmitActiveFormAsync());
        Assert.False(ok);

        var second = service.CreateFormAsync(Signup.Schema, new Signup { Name = "Ada" });
        provider.Render();
        Assert.False((await first).IsCompleted);

        ok = await provider.InvokeAsync(() => service.SubmitActiveFormAsync());
        Assert.True(ok);
        var outcome = await second;
        Assert.True(outcome.IsCompleted);
        Assert.Equal("Ada", outcome.Model!.Name);

        var third = service.CreateFormAsync(Signup.Schema, mode: FormShellMode.Fullscreen);
        provider.Render();
        await provider.InvokeAsync(() => service.CloseForm());
        var closed = await third;
        Assert.False(closed.IsCompleted);
        Assert.Null(closed.Model);
    }

    [Fact]
    public async Task The_overlay_is_a_modal_dialog_labelled_by_the_title_and_closes_with_the_dialog()
    {
        var provider = Render<KlyFormProvider>();
        var service = Services.GetRequiredService<FormService>();
        var task = service.CreateFormAsync(Signup.Schema);
        provider.Render();

        var modal = provider.Find("kly-modal");
        Assert.True(modal.HasAttribute("open"));
        Assert.Equal("Sign up", provider.Find("dialog").GetAttribute("aria-label"));
        await modal.TriggerEventAsync("onklytoggle", new KlyToggleEventArgs { Open = false, Sequence = 1 });

        Assert.False(service.IsOpen);
        Assert.False((await task).IsCompleted);
    }

    [Fact]
    public async Task A_typed_and_a_dynamic_overlay_can_be_open_together()
    {
        var provider = Render<KlyFormProvider>();
        var typed = Services.GetRequiredService<FormService>();
        var dynamic = Services.GetRequiredService<DynamicFormService>();
        _ = typed.CreateFormAsync(Signup.Schema);
        _ = dynamic.CreateFormAsync(new DynamicFormSchema { Title = "Runtime", Fields = [new DynamicFieldSchema { Key = "a" }] });
        provider.Render();

        Assert.Equal(2, provider.FindAll("[data-slot=form-engine]").Count);
        await provider.InvokeAsync(() => dynamic.CloseForm());
        provider.Render();
        Assert.True(typed.IsOpen);
    }

    [Fact]
    public void The_dynamic_and_the_typed_engine_render_the_same_control_markup()
    {
        var schema = FormSchema.For<Signup>(s => s.Field(m => m.Name, f => f.Label("Name").Required().Description("As on your ID")));
        var typed = Render<KlyFormEngine<Signup>>(p => p.Add(x => x.Schema, schema));
        var dynamic = Render<KlyDynamicFormEngine>(p => p.Add(x => x.Schema, new DynamicFormSchema
        {
            Fields = [new DynamicFieldSchema { Key = "Name", Label = "Name", Required = true, Description = "As on your ID" }],
        }));

        // Only the numbers Blazor gives the event handlers differ.
        static string Html(AngleSharp.Dom.IElement e) => System.Text.RegularExpressions.Regex.Replace(e.InnerHtml, "blazor:(\\w+)=\"\\d+\"", "blazor:$1");
        Assert.Equal(Html(dynamic.Find("[data-form-field=Name]")), Html(typed.Find("[data-form-field=Name]")));
    }
}
