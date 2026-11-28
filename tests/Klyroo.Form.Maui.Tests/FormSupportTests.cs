using System.Globalization;
using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormSupportTests
{
    // ---- texts

    [Fact]
    public void The_default_texts_are_english_with_the_validation_messages_of_the_ui_culture()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            FormTexts.ResetCurrent();
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            Assert.Equal("Submit", FormTexts.Current.Submit);
            Assert.Same(FormMessageLocales.Fr, FormTexts.Current.Messages);

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Same(FormMessageLocales.En, FormTexts.Current.Messages);
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
            FormTexts.ResetCurrent();
        }
    }

    [Fact]
    public void Replacing_the_texts_translates_the_views_that_read_them()
    {
        try
        {
            FormTexts.Current = FormTexts.Current with { Submit = "Envoyer", Reset = "Annuler les changements", NoItemsYet = "Rien" };
            var engine = new KlyDynamicFormEngine { Schema = FormKit.Schema(FormKit.Field("a")) };
            engine.Control<FormTextControl>("a").Input.Text = "x";

            Assert.Equal("Envoyer", engine.ShellView!.SubmitButton.Text);
            Assert.Equal("Annuler les changements", engine.ShellView.ResetButton.Text);
        }
        finally
        {
            FormTexts.ResetCurrent();
        }
    }

    [Fact]
    public void The_texts_cannot_be_replaced_by_nothing()
    {
        Assert.Throws<ArgumentNullException>(() => FormTexts.Current = null!);
    }

    [Fact]
    public void The_texts_that_hold_a_number_or_a_name_are_functions()
    {
        var texts = new FormTexts();

        Assert.Equal("There is 1 error to fix", texts.ErrorsToFix(1));
        Assert.Equal("There are 3 errors to fix", texts.ErrorsToFix(3));
        Assert.Equal("Email: This field is required", texts.FieldError("Email", "This field is required"));
        Assert.Equal("SKU (Lines, 2): bad", texts.CellError("SKU", "Lines", 2, "bad"));
        Assert.Equal("Remove item 4", texts.RemoveItem(4));
        Assert.Equal("Leave", texts.LeavePage);
        Assert.Equal("Stay", texts.StayOnPage);
    }

    // ---- services

    [Fact]
    public void The_registration_adds_both_services_as_singletons_once()
    {
        var services = new ServiceCollection().AddKlyrooFormMaui().AddKlyrooFormMaui();
        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<FormService>(), provider.GetRequiredService<FormService>());
        Assert.Same(provider.GetRequiredService<DynamicFormService>(), provider.GetRequiredService<DynamicFormService>());
        Assert.Equal(2, services.Count);
        Assert.Throws<ArgumentNullException>(() => FormServiceCollectionExtensions.AddKlyrooFormMaui(null!));
    }

    // ---- dynamic field view and custom renderers

    private static DynamicFormController Controller() => new(FormKit.Schema(FormKit.Field("name"), new DynamicFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number }));

    [Fact]
    public void A_dynamic_field_view_shows_a_field_by_its_key_and_follows_the_controller()
    {
        var form = Controller();
        var view = new KlyDynamicFieldView { Form = form, Key = "age" };

        var field = Assert.IsType<FormFieldView>(view.NodeView);
        Assert.IsType<FormNumberControl>(field.Control);
        ((FormNumberControl)field.Control).Input.Value = 42m;
        Assert.Equal(42.0, form.GetValue("age"));

        form.SetValue("age", 7.0);
        Assert.Equal(7m, ((FormNumberControl)field.Control).Input.Value);
    }

    [Fact]
    public void An_unknown_key_shows_nothing_and_a_given_field_wins_over_the_key()
    {
        var form = Controller();

        Assert.Null(new KlyDynamicFieldView { Form = form, Key = "nope" }.Content);
        var view = new KlyDynamicFieldView { Form = form, Field = form.Schema.Fields[0], Key = "age" };
        Assert.IsType<FormTextControl>(((FormFieldView)view.NodeView!).Control);
    }

    [Fact]
    public void A_dynamic_field_view_without_a_controller_shows_nothing()
    {
        Assert.Null(new KlyDynamicFieldView { Key = "name" }.Content);
    }

    [Fact]
    public void A_dynamic_field_view_hides_while_its_field_is_not_in_effect()
    {
        var form = new DynamicFormController(FormKit.Schema(FormKit.Field("a"), new DynamicFieldSchema { Key = "b", Condition = f => f.GetValue<string>("a") == "x" }));
        var view = new KlyDynamicFieldView { Form = form, Key = "b" };
        Assert.False(view.IsVisible);

        form.SetValue("a", "x");

        Assert.True(view.IsVisible);
    }

    private static DynamicFieldSchema Custom(string key = "star") => new() { Key = "rate", Kind = FormFieldKind.Custom, CustomKey = key, Label = "Rate" };

    [Fact]
    public void The_field_templates_of_a_dynamic_form_draw_its_custom_fields_from_the_context()
    {
        DynamicControlContext? context = null;
        var engine = new KlyDynamicFormEngine
        {
            Schema = FormKit.Schema(Custom()),
            FieldTemplates = new Dictionary<string, Func<DynamicControlContext, View>>
            {
                ["star"] = ctx =>
                {
                    context = ctx;
                    return new Label { Text = "stars" };
                },
            },
        };

        var control = Assert.IsType<FormCustomControl>(engine.ControlOf("rate"));
        Assert.Equal("stars", Assert.IsType<Label>(control.Content).Text);
        Assert.Equal("Rate", engine.FieldView("rate").Field.Label);

        context!.Commit(4);
        Assert.Equal(4, engine.EffectiveController!.GetValue("rate"));
        Assert.Equal(context.Id, engine.Id("rate"));
    }

    [Fact]
    public void The_field_templates_win_over_the_renderers_registered_for_every_form_and_a_missing_key_shows_the_notice()
    {
        FormCustomFields.Clear();
        try
        {
            FormCustomFields.Register("star", _ => new Label { Text = "global" });
            var withTemplate = new KlyDynamicFormEngine
            {
                Schema = FormKit.Schema(Custom()),
                FieldTemplates = new Dictionary<string, Func<DynamicControlContext, View>> { ["star"] = _ => new Label { Text = "local" } },
            };
            var withoutTemplate = new KlyDynamicFormEngine { Schema = FormKit.Schema(Custom()) };
            var missing = new KlyDynamicFormEngine { Schema = FormKit.Schema(Custom("unknown")) };

            Assert.Equal("local", ((Label)((FormCustomControl)withTemplate.ControlOf("rate")).Content!).Text);
            Assert.Equal("global", ((Label)((FormCustomControl)withoutTemplate.ControlOf("rate")).Content!).Text);
            Assert.Contains("\"unknown\"", ((Label)((FormCustomControl)missing.ControlOf("rate")).Content!).Text);
        }
        finally
        {
            FormCustomFields.Clear();
        }
    }

    [Fact]
    public void A_custom_field_without_a_key_shows_the_notice()
    {
        var engine = new KlyDynamicFormEngine { Schema = FormKit.Schema(new DynamicFieldSchema { Key = "x", Kind = FormFieldKind.Custom }) };

        Assert.Contains("No renderer", ((Label)((FormCustomControl)engine.ControlOf("x")).Content!).Text);
    }

    [Fact]
    public void A_custom_field_in_a_cell_of_a_list_uses_the_templates_too()
    {
        var engine = new KlyDynamicFormEngine
        {
            Schema = FormKit.Schema(new DynamicFieldSchema { Key = "rows", Kind = FormFieldKind.ArrayList, Fields = [Custom()] }),
            FieldTemplates = new Dictionary<string, Func<DynamicControlContext, View>> { ["star"] = ctx => new Label { Text = "cell " + ctx.IsCell } },
        };

        engine.ShellView!.Views<FormArrayView>().Single().AddButton.SendClicked();

        var cell = engine.ShellView!.Views<FormArrayView>().Single().Rows[0].Cells[0];
        Assert.Equal("cell True", ((Label)((FormCustomControl)cell.Control).Content!).Text);
    }

    [Fact]
    public void Registering_a_renderer_checks_its_arguments()
    {
        Assert.Throws<ArgumentException>(() => FormCustomFields.Register(" ", _ => new Label()));
        Assert.Throws<ArgumentNullException>(() => FormCustomFields.Register("k", (Func<DynamicControlContext, View>)null!));
        Assert.Throws<ArgumentException>(() => FormCustomFields.Register<string>("", _ => new Label()));
        Assert.Throws<ArgumentNullException>(() => FormCustomFields.Register<string>("k", null!));
    }

    // ---- error summary

    private sealed class NotOurs : IFormRuntime
    {
        public string FormKey => "x";

        public string? Title => null;

        public bool IsDirty => false;

        public bool IsStepped => false;

        public string? CurrentStepKey => null;

        public IReadOnlyList<IFormFieldAccess> Fields => [];

        public event Action? Changed
        {
            add { }
            remove { }
        }

        public Task<bool> SubmitAsync() => Task.FromResult(false);

        public void Reset()
        {
        }

        public Task<bool> NextStepAsync() => Task.FromResult(false);

        public Task<bool> PreviousStepAsync() => Task.FromResult(false);
    }

    [Fact]
    public void The_summary_takes_no_room_while_there_is_no_error_and_lists_the_errors_of_the_form()
    {
        var form = Controller();
        var summary = new KlyFormErrorSummary { Form = form };
        Assert.False(summary.Box.IsVisible);
        Assert.Null(summary.Title);
        Assert.Empty(summary.Entries);

        form.SetFormError("The server is down");

        Assert.True(summary.Box.IsVisible);
        Assert.Equal(FormTexts.Current.ErrorsToFix(1), summary.Title);
        Assert.Equal("The server is down", Assert.Single(summary.Lines).Label.Text);
        Assert.Empty(summary.Lines[0].Label.GestureRecognizers);
    }

    [Fact]
    public async Task The_summary_lists_the_errors_of_the_fields_only_after_a_failed_attempt_and_stays_in_step_with_the_form()
    {
        var form = new DynamicFormController(FormKit.Schema(new DynamicFieldSchema { Key = "a", Label = "A", Required = true }, new DynamicFieldSchema { Key = "b", Label = "B", Required = true }));
        var summary = new KlyFormErrorSummary { Form = form };
        form.SetError("a", "Not good");
        Assert.False(summary.Box.IsVisible);

        await form.SubmitAsync();

        Assert.Equal(2, summary.Entries.Count);
        Assert.Equal("A: Not good", summary.Lines[0].Label.Text);
        Assert.Equal(FormTexts.Current.ErrorsToFix(2), summary.Title);

        form.SetValue("b", "fixed");
        form.ClearError("a");
        form.SetValue("a", "fixed");

        Assert.False(summary.Box.IsVisible);
    }

    // The platform calls the tap recognizer through an internal interface; a test raises the event the recognizer exposes.
    private static void Tap(TapGestureRecognizer tap, View view)
    {
        var send = typeof(TapGestureRecognizer).GetMethod("SendTapped", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (send is not null)
        {
            send.Invoke(tap, send.GetParameters().Length == 1 ? [view] : [view, null]);
            return;
        }
        var field = typeof(TapGestureRecognizer).GetField("Tapped", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        ((EventHandler<TappedEventArgs>?)field?.GetValue(tap))?.Invoke(view, new TappedEventArgs(null));
    }

    [Fact]
    public async Task A_tap_on_an_entry_raises_EntrySelected_with_the_error()
    {
        var form = new DynamicFormController(FormKit.Schema(new DynamicFieldSchema { Key = "a", Label = "A", Required = true }));
        var summary = new KlyFormErrorSummary { Form = form };
        await form.SubmitAsync();
        FormErrorEntry? selected = null;
        summary.EntrySelected += (_, e) => selected = e.Entry;
        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(summary.Lines[0].Label.GestureRecognizers));

        Tap(tap, summary.Lines[0].Label);

        Assert.Equal("a", selected!.FieldKey);
        Assert.Equal(FormMessages.Default.Required, selected.Message);
    }

    [Fact]
    public async Task The_live_text_changes_after_a_failed_attempt_and_is_empty_once_the_last_error_is_gone()
    {
        var form = new DynamicFormController(FormKit.Schema(new DynamicFieldSchema { Key = "a", Label = "A", Required = true }));
        var summary = new KlyFormErrorSummary { Form = form };
        Assert.Equal("", summary.LiveText);

        await form.SubmitAsync();
        Assert.Equal($"{FormTexts.Current.ErrorsToFix(1)}. A: {FormMessages.Default.Required}", summary.LiveText);

        form.SetValue("a", "x");
        Assert.Equal("", summary.LiveText);
    }

    [Fact]
    public void A_summary_refuses_a_runtime_that_klyroo_did_not_create_and_forgets_a_form_set_to_null()
    {
        var summary = new KlyFormErrorSummary();

        Assert.Throws<ArgumentException>(() => summary.Form = new NotOurs());

        var form = Controller();
        summary.Form = form;
        form.SetFormError("x");
        summary.Form = null;
        Assert.False(summary.Box.IsVisible);
    }

    [Fact]
    public void A_summary_inside_a_form_moves_the_focus_through_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "a", Required = true }));
        var summary = engine.ShellView!.Summary;
        FormErrorEntry? focused = null;
        summary.FocusEntry = entry =>
        {
            focused = entry;
            return true;
        };

        summary.Select(new FormErrorEntry("a", "x", "a", null, null));

        Assert.Equal("a", focused!.FieldKey);
    }
}
