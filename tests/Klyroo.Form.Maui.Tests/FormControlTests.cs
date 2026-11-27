using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormControlTests
{
    // The native unfocus event: what the platform handler does when the control loses the focus.
    private static void Blur(View view)
    {
        ((IView)view).IsFocused = true;
        ((IView)view).IsFocused = false;
    }

    private static IEnumerable<Entry> Entries(View root) => root.Views<Entry>();

    // ---- text, phone, one-time code, textarea

    [Theory]
    [InlineData(FormFieldKind.Text, "Default")]
    [InlineData(FormFieldKind.PhoneNumber, "Telephone")]
    [InlineData(FormFieldKind.OneTimeCode, "Numeric")]
    public void A_text_field_takes_the_keyboard_of_its_kind(FormFieldKind kind, string keyboard)
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Kind = kind, Placeholder = "type here", MaxLength = 6 }));
        var input = engine.Control<FormTextControl>("f").Input;

        Assert.Equal(keyboard, input.Keyboard.ToString() == "Plain" ? "Default" : KeyboardName(input.Keyboard));
        Assert.Equal("type here", input.Placeholder);
        Assert.Equal(6, input.MaxLength);
    }

    private static string KeyboardName(Keyboard keyboard) => keyboard == Keyboard.Telephone ? "Telephone" : keyboard == Keyboard.Numeric ? "Numeric" : "Default";

    [Fact]
    public void Typing_clears_to_an_empty_text_and_the_form_calls_that_empty()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Required = true }), new Dictionary<string, object?> { ["f"] = "x" });
        var input = engine.Control<FormTextControl>("f").Input;

        input.Text = "";

        Assert.Equal("", engine.EffectiveController!.GetValue("f"));
        Assert.True(engine.EffectiveController.Validate() is false);
    }

    [Fact]
    public void Leaving_a_field_marks_it_touched_so_its_error_shows()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Label = "F", Required = true }));
        var view = engine.FieldView("f");
        Assert.Null(view.Field.Error);

        Blur(view.Control.Inner.Views<Entry>().First());

        Assert.Equal(FormMessages.Default.Required, view.Field.Error);
        Assert.Equal(FormMessages.Default.Required, engine.EffectiveController!.GetError("f"));
    }

    [Fact]
    public void A_required_field_shows_the_mark_in_its_label_and_a_field_with_an_error_is_invalid()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Label = "Name", Required = true }));
        var view = engine.FieldView("f");

        Assert.Equal("Name", view.Field.Label);
        Assert.True(view.Field.Required);

        engine.EffectiveController!.SetError("f", "Taken");

        Assert.Equal("Taken", view.Field.Error);
        Assert.True(engine.Control<FormTextControl>("f").Input.Invalid);
    }

    [Fact]
    public void Description_hint_and_help_go_to_the_field_and_the_label_is_the_accessible_name()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Label = "Name", Description = "As on your ID", Hint = "Optional", Help = "Shown on the badge" }));
        var field = engine.FieldView("f").Field;

        Assert.Equal("As on your ID", field.Description);
        Assert.Equal("Optional", field.Hint);
        Assert.Equal("Shown on the badge", field.Help);
        Assert.Equal("Name", engine.Control<FormTextControl>("f").Input.AccessibleName);
    }

    [Fact]
    public async Task A_disabled_field_is_disabled_with_its_reason_and_so_is_everything_while_the_form_saves()
    {
        var gate = new TaskCompletionSource<bool>();
        var engine = new KlyDynamicFormEngine
        {
            Schema = FormKit.Schema(new DynamicFieldSchema { Key = "a", Disabled = true, DisabledReason = "Locked by an admin" }, FormKit.Field("b")),
            OnSave = (_, _) => gate.Task,
        };
        var a = engine.FieldView("a");

        Assert.False(engine.Control<FormTextControl>("a").Input.IsEnabled);
        Assert.True(a.DisabledReason.IsVisible);
        Assert.Equal("🔒 Locked by an admin", a.DisabledReason.Text);
        Assert.True(engine.Control<FormTextControl>("b").Input.IsEnabled);

        var submit = engine.SubmitAsync();
        Assert.False(engine.Control<FormTextControl>("b").Input.IsEnabled);
        gate.SetResult(true);
        Assert.True(await submit);
        Assert.True(engine.Control<FormTextControl>("b").Input.IsEnabled);
    }

    [Fact]
    public void A_clearable_text_field_shows_a_clear_button_only_while_it_has_text()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Clearable = true }));
        var control = engine.ControlOf("f");
        Assert.NotNull(control.ClearButton);
        Assert.False(control.ClearButton!.IsVisible);

        engine.Control<FormTextControl>("f").Input.Text = "abc";
        Assert.True(control.ClearButton.IsVisible);

        control.ClearButton.SendClicked();
        Assert.Equal("", engine.EffectiveController!.GetValue("f"));
        Assert.Equal("", engine.Control<FormTextControl>("f").Input.Text);
        Assert.False(control.ClearButton.IsVisible);
    }

    [Fact]
    public void A_field_that_is_not_clearable_has_no_clear_button()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("f")));

        Assert.Null(engine.ControlOf("f").ClearButton);
    }

    [Fact]
    public void The_return_key_in_a_text_field_calls_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("f")));
        var pressed = 0;
        engine.FieldView("f").Slot.Enter = () => pressed++;

        ((IEntryController)engine.Control<FormTextControl>("f").Input.Content!).SendCompleted();

        Assert.Equal(1, pressed);
    }

    [Fact]
    public void A_textarea_has_three_rows_and_writes_what_is_typed()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Kind = FormFieldKind.Textarea, MaxLength = 100 }));
        var area = engine.Control<FormTextareaControl>("f").Input;

        area.Text = "line 1\nline 2";

        Assert.Equal(3, area.Rows);
        Assert.Equal(100, area.MaxLength);
        Assert.Equal("line 1\nline 2", engine.EffectiveController!.GetValue("f"));
    }

    // ---- password

    [Fact]
    public void A_password_is_masked_until_the_user_shows_it_and_the_button_says_what_it_does()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "p", Kind = FormFieldKind.Password }));
        var password = engine.Control<FormPasswordControl>("p");

        Assert.True(password.Input.IsPassword);
        Assert.Equal(FormTexts.Current.Show, password.Toggle.Text);

        password.Toggle.SendClicked();

        Assert.False(password.Input.IsPassword);
        Assert.True(password.Revealed);
        Assert.Equal(FormTexts.Current.Hide, password.Toggle.Text);
        Assert.Equal(FormTexts.Current.HidePassword, SemanticProperties.GetDescription(password.Toggle));
    }

    [Fact]
    public void A_password_lists_its_requirements_and_ticks_the_ones_that_are_met()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema
        {
            Key = "p",
            Kind = FormFieldKind.Password,
            PasswordRequirements = [new FormPasswordRequirement("len", "8 characters", s => s.Length >= 8), new FormPasswordRequirement("digit", "A digit", s => s.Any(char.IsDigit))],
        }));
        var password = engine.Control<FormPasswordControl>("p");
        Assert.All(password.Requirements, r => Assert.Equal("○", r.Mark.Text));

        password.Input.Text = "abcdefgh1";

        Assert.All(password.Requirements, r => Assert.Equal("✓", r.Mark.Text));
        Assert.Contains(FormTexts.Current.RequirementMet, SemanticProperties.GetDescription(password.Requirements[0].Line));

        password.Input.Text = "abc";
        Assert.All(password.Requirements, r => Assert.Equal("○", r.Mark.Text));
        Assert.Contains(FormTexts.Current.RequirementNotMet, SemanticProperties.GetDescription(password.Requirements[0].Line));
    }

    [Fact]
    public void A_password_without_requirements_shows_no_checklist()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "p", Kind = FormFieldKind.Password }));

        Assert.Empty(engine.Control<FormPasswordControl>("p").Requirements);
    }

    // ---- number

    [Fact]
    public void A_number_writes_a_double_and_shows_the_value_of_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "n", Kind = FormFieldKind.Number, Step = 0.5 }));
        var input = engine.Control<FormNumberControl>("n").Input;
        Assert.Equal(0.5m, input.Step);

        input.Value = 12.5m;
        Assert.Equal(12.5, engine.EffectiveController!.GetValue("n"));

        engine.EffectiveController.SetValue("n", 3.0);
        Assert.Equal(3m, input.Value);

        input.Value = null;
        Assert.Null(engine.EffectiveController.GetValue("n"));
    }

    [Fact]
    public void A_number_keeps_every_digit_of_a_decimal()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "n", Kind = FormFieldKind.Number }));
        var input = engine.Control<FormNumberControl>("n").Input;

        input.Value = 1234567890.123456789m;

        Assert.Equal(1234567890.123456789m, engine.EffectiveController!.GetValue("n"));
        Assert.Equal(1234567890.123456789m, input.Value);
    }

    [Fact]
    public void The_bounds_of_a_number_are_reported_by_the_form_not_applied_silently_by_the_field()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "n", Label = "N", Kind = FormFieldKind.Number, Min = 1, Max = 10 }));
        var control = engine.Control<FormNumberControl>("n");

        control.Input.Value = 20m;
        Blur(control.Input.Views<Entry>().First());

        Assert.Null(control.Input.Max);
        Assert.Equal(20m, control.Input.Value);
        Assert.Equal(FormMessages.Default.MaxValue(10), engine.FieldView("n").Field.Error);
    }

    [Fact]
    public void A_clearable_number_clears_to_nothing()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "n", Kind = FormFieldKind.Number, Clearable = true }), new Dictionary<string, object?> { ["n"] = 4.0 });
        var control = engine.ControlOf("n");
        Assert.True(control.ClearButton!.IsVisible);

        control.ClearButton.SendClicked();

        Assert.Null(engine.EffectiveController!.GetValue("n"));
        Assert.Null(engine.Control<FormNumberControl>("n").Input.Value);
    }

    // ---- slider

    [Fact]
    public void A_slider_takes_the_bounds_of_the_field_and_shows_the_minimum_until_it_moves()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Slider, Min = 10, Max = 50, Step = 5 }));
        var slider = engine.Control<FormSliderControl>("s");

        Assert.Equal(10, slider.Slider.Min);
        Assert.Equal(50, slider.Slider.Max);
        Assert.Equal(5, slider.Slider.Step);
        Assert.Equal(10, slider.Slider.Value);
        Assert.Equal("10", slider.ValueLabel.Text);
        Assert.Null(engine.EffectiveController!.GetValue("s"));

        slider.Slider.Value = 30;

        Assert.Equal(30.0, engine.EffectiveController.GetValue("s"));
        Assert.Equal("30", slider.ValueLabel.Text);
        Assert.True(engine.EffectiveController.IsDirty);
    }

    [Fact]
    public void A_slider_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Slider, Min = 0, Max = 100 }));

        engine.EffectiveController!.SetValue("s", 42.0);

        Assert.Equal(42, engine.Control<FormSliderControl>("s").Slider.Value);
    }

    // ---- checkbox and switch

    [Fact]
    public void A_checkbox_has_the_label_of_the_field_as_its_text_and_commits_what_is_checked()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "c", Kind = FormFieldKind.Checkbox, Label = "Accept", Required = true }));
        var check = engine.Control<FormCheckControl>("c");

        Assert.NotNull(check.Checkbox);
        Assert.Null(engine.FieldView("c").Field.Label);
        Assert.Equal("Accept *", check.Checkbox!.Text);

        check.Checkbox.IsChecked = true;

        Assert.Equal(true, engine.EffectiveController!.GetValue("c"));
        Assert.Null(engine.FieldView("c").Field.Error);
    }

    [Fact]
    public void A_checkbox_follows_the_form_and_a_switch_is_a_switch()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            new DynamicFieldSchema { Key = "c", Kind = FormFieldKind.Checkbox, Label = "A" },
            new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Switch, Label = "B" }));

        engine.EffectiveController!.SetValue("c", true);
        engine.EffectiveController.SetValue("s", true);

        Assert.True(engine.Control<FormCheckControl>("c").Checkbox!.IsChecked);
        Assert.Null(engine.Control<FormCheckControl>("c").Switch);
        Assert.True(engine.Control<FormCheckControl>("s").Switch!.IsToggled);
        Assert.Equal("B", engine.Control<FormCheckControl>("s").Switch!.Text);
    }

    [Fact]
    public void A_checkbox_cannot_be_cleared()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "c", Kind = FormFieldKind.Checkbox, Clearable = true }));

        Assert.Null(engine.ControlOf("c").ClearButton);
    }

    // ---- rating, color, tags

    [Fact]
    public void A_rating_has_the_stars_of_the_field_and_commits_an_int()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "r", Kind = FormFieldKind.Rating, Max = 4, Clearable = true }));
        var rating = engine.Control<FormRatingControl>("r");
        Assert.Equal(4, rating.Rating.Max);

        rating.Rating.Value = 3;
        Assert.Equal(3, engine.EffectiveController!.GetValue("r"));
        Assert.True(engine.ControlOf("r").ClearButton!.IsVisible);

        engine.ControlOf("r").ClearButton!.SendClicked();
        Assert.Null(engine.EffectiveController.GetValue("r"));
        Assert.Equal(0, rating.Rating.Value);
    }

    [Fact]
    public void A_rating_defaults_to_five_stars_and_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "r", Kind = FormFieldKind.Rating }));

        engine.EffectiveController!.SetValue("r", 2);

        Assert.Equal(5, engine.Control<FormRatingControl>("r").Rating.Max);
        Assert.Equal(2, engine.Control<FormRatingControl>("r").Rating.Value);
    }

    [Fact]
    public void A_color_commits_the_hex_and_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "c", Kind = FormFieldKind.ColorPicker }));
        var picker = engine.Control<FormColorControl>("c").Picker;

        picker.Value = "#ff8800";
        Assert.Equal("#ff8800", engine.EffectiveController!.GetValue("c"));

        engine.EffectiveController.SetValue("c", "#00ff00");
        Assert.Equal("#00ff00", picker.Value);
    }

    [Fact]
    public void Tags_commit_a_list_and_follow_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "t", Kind = FormFieldKind.Tag, Placeholder = "Add" }));
        var tags = engine.Control<FormTagsControl>("t").Tags;
        Assert.Equal("Add", tags.Placeholder);

        tags.Value = ["a", "b"];
        Assert.Equal(["a", "b"], engine.EffectiveController!.GetValue<List<string>>("t"));

        engine.EffectiveController.SetValue("t", new List<string> { "x" });
        Assert.Equal(["x"], tags.Value);
    }

    [Fact]
    public void Tags_without_a_placeholder_say_what_the_texts_say()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "t", Kind = FormFieldKind.Tag }));

        Assert.Equal(FormTexts.Current.TagPlaceholder, engine.Control<FormTagsControl>("t").Tags.Placeholder);
    }

    // ---- dates and times

    [Fact]
    public void A_date_is_stored_as_yyyy_MM_dd_and_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "d", Kind = FormFieldKind.Date }));
        var input = engine.Control<FormDateControl>("d").Input;

        input.Value = new DateTime(2026, 3, 9);
        Assert.Equal("2026-03-09", engine.EffectiveController!.GetValue("d"));

        engine.EffectiveController.SetValue("d", "2025-12-31");
        Assert.Equal(new DateTime(2025, 12, 31), input.Value);

        input.Value = null;
        Assert.Null(engine.EffectiveController.GetValue("d"));
    }

    [Fact]
    public void A_time_is_stored_as_HH_mm_and_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "t", Kind = FormFieldKind.Time }));
        var input = engine.Control<FormTimeControl>("t").Input;

        input.Value = new TimeOnly(7, 5);
        Assert.Equal("07:05", engine.EffectiveController!.GetValue("t"));

        engine.EffectiveController.SetValue("t", "18:30");
        Assert.Equal(new TimeOnly(18, 30), input.Value);
    }

    [Fact]
    public void A_date_and_time_waits_for_both_halves_and_keeps_the_first_one_on_screen()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "dt", Kind = FormFieldKind.DateTime }));
        var control = engine.Control<FormDateTimeControl>("dt");

        control.DateInput.Value = new DateTime(2026, 1, 2);
        Assert.Null(engine.EffectiveController!.GetValue("dt"));
        Assert.Equal(new DateTime(2026, 1, 2), control.DateInput.Value);

        engine.EffectiveController.SetValue("other", "forces a refresh");
        Assert.Equal(new DateTime(2026, 1, 2), control.DateInput.Value);

        control.TimeInput.Value = new TimeOnly(9, 30);
        Assert.Equal("2026-01-02T09:30", engine.EffectiveController.GetValue("dt"));

        engine.EffectiveController.SetValue("dt", "2027-05-06T23:15");
        Assert.Equal(new DateTime(2027, 5, 6), control.DateInput.Value);
        Assert.Equal(new TimeOnly(23, 15), control.TimeInput.Value);

        engine.EffectiveController.SetValue("dt", null);
        Assert.Null(control.DateInput.Value);
        Assert.Null(control.TimeInput.Value);
    }

    [Fact]
    public void A_date_range_stores_two_dates_names_them_and_keeps_the_end_after_the_start()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "r", Kind = FormFieldKind.DateRange }));
        var range = engine.Control<FormDateRangeControl>("r");
        Assert.Equal(FormTexts.Current.StartDate, range.Start.AccessibleName);
        Assert.Equal(FormTexts.Current.EndDate, range.End.AccessibleName);

        range.Start.Value = new DateTime(2026, 2, 1);
        Assert.Equal(["2026-02-01", ""], engine.EffectiveController!.GetValue<List<string>>("r"));
        Assert.Equal(new DateTime(2026, 2, 1), range.End.Min);

        range.End.Value = new DateTime(2026, 2, 9);
        Assert.Equal(["2026-02-01", "2026-02-09"], engine.EffectiveController.GetValue<List<string>>("r"));
        Assert.Equal(new DateTime(2026, 2, 9), range.Start.Max);

        range.Start.Value = null;
        range.End.Value = null;
        Assert.Null(engine.EffectiveController.GetValue("r"));
    }

    [Fact]
    public void A_date_range_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "r", Kind = FormFieldKind.DateRange }));

        engine.EffectiveController!.SetValue("r", new List<string> { "2026-03-01", "2026-03-31" });

        var range = engine.Control<FormDateRangeControl>("r");
        Assert.Equal(new DateTime(2026, 3, 1), range.Start.Value);
        Assert.Equal(new DateTime(2026, 3, 31), range.End.Value);
    }

    // ---- choices

    private static readonly FormOptionItem[] Fruits = [new("a", "Apple", Description: "Crunchy"), new("b", "Banana"), new("c", "Cherry", Disabled: true)];

    [Fact]
    public void Radio_buttons_have_the_options_and_commit_the_chosen_value()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "r", Kind = FormFieldKind.Radio, Options = Fruits }));
        var choice = engine.Control<FormChoiceControl>("r");
        var radios = Assert.IsType<KlyRadioGroup>(Assert.Single(choice.Controls));

        Assert.Equal(["Apple", "Banana", "Cherry"], radios.Items!.Select(i => i.Label).ToArray());
        Assert.Equal("Crunchy", radios.Items![0].Description);
        Assert.True(radios.Items[2].Disabled);

        radios.Value = "b";

        Assert.Equal("b", engine.EffectiveController!.GetValue("r"));
        engine.EffectiveController.SetValue("r", "a");
        Assert.Equal("a", radios.Value);
    }

    [Fact]
    public void Radio_options_in_groups_go_under_their_legend_and_act_as_one()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema
        {
            Key = "r",
            Kind = FormFieldKind.Radio,
            Options = [new("a", "A", Group: "Fruit"), new("b", "B", Group: "Veg"), new("c", "C", Group: "Fruit")],
        }));
        var groups = engine.Control<FormChoiceControl>("r").Controls.Cast<KlyRadioGroup>().ToList();

        Assert.Equal(["Fruit", "Veg"], groups.Select(g => g.Legend).ToArray());
        Assert.Equal(["A", "C"], groups[0].Items!.Select(i => i.Label).ToArray());

        groups[0].Value = "a";
        groups[1].Value = "b";

        Assert.Equal("b", engine.EffectiveController!.GetValue("r"));
        Assert.Null(groups[0].Value);
        Assert.Equal("b", groups[1].Value);
    }

    [Fact]
    public void A_checkbox_group_commits_the_list_in_the_order_things_were_checked()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "g", Kind = FormFieldKind.CheckboxGroup, Options = Fruits }));
        var boxes = Assert.IsType<KlyCheckboxGroup>(Assert.Single(engine.Control<FormChoiceControl>("g").Controls));

        boxes.Value = new object[] { "b" };
        boxes.Value = new object[] { "b", "a" };
        Assert.Equal(["b", "a"], engine.EffectiveController!.GetValue<List<string>>("g"));

        boxes.Value = new object[] { "a" };
        Assert.Equal(["a"], engine.EffectiveController.GetValue<List<string>>("g"));

        engine.EffectiveController.SetValue("g", new List<string> { "b" });
        Assert.Equal(["b"], boxes.Value!.Select(v => v.ToString()).ToArray());
    }

    [Fact]
    public void A_multiple_select_is_a_list_of_checkboxes_like_on_the_web()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "m", Kind = FormFieldKind.Select, Multiple = true, Options = Fruits }));

        Assert.IsType<KlyCheckboxGroup>(Assert.Single(engine.Control<FormChoiceControl>("m").Controls));
    }

    [Fact]
    public void The_options_of_a_choice_follow_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            FormKit.Field("country"),
            new DynamicFieldSchema
            {
                Key = "city",
                Kind = FormFieldKind.Radio,
                OptionsFactory = f => f.GetValue<string>("country") == "fr" ? [new("paris", "Paris")] : [new("rome", "Rome")],
            }));
        var radios = (KlyRadioGroup)engine.Control<FormChoiceControl>("city").Controls[0];
        Assert.Equal(["Rome"], radios.Items!.Select(i => i.Label).ToArray());

        engine.Control<FormTextControl>("country").Input.Text = "fr";

        radios = (KlyRadioGroup)engine.Control<FormChoiceControl>("city").Controls[0];
        Assert.Equal(["Paris"], radios.Items!.Select(i => i.Label).ToArray());
    }

    [Fact]
    public void A_select_is_a_picker_with_an_empty_choice_first()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Select, Placeholder = "Pick one", Options = [new("a", "Apple"), new("b", "Banana")] }));
        var select = engine.Control<FormSelectControl>("s");
        Assert.NotNull(select.Picker);
        Assert.Null(select.Menu);
        Assert.Equal("Pick one", select.Picker!.Placeholder);
        Assert.Equal(["Pick one", "Apple", "Banana"], select.Picker.ItemsSource!.Cast<object>().Select(o => o.ToString()).ToArray());
        Assert.Null(select.Picker.SelectedItem);

        select.Picker.SelectedItem = select.Picker.ItemsSource!.Cast<FormChoice>().First(c => c.Value == "b");
        Assert.Equal("b", engine.EffectiveController!.GetValue("s"));

        engine.EffectiveController.SetValue("s", "a");
        Assert.Equal("a", ((FormChoice)select.Picker.SelectedItem!).Value);

        select.Picker.SelectedItem = select.Picker.ItemsSource!.Cast<FormChoice>().First(c => c.Value is null);
        Assert.Null(engine.EffectiveController.GetValue("s"));
    }

    [Fact]
    public void A_select_with_a_placeholder_default_says_what_the_texts_say()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Select, Options = [new("a", "Apple")] }));

        Assert.Equal(FormTexts.Current.SelectPlaceholder, engine.Control<FormSelectControl>("s").Picker!.Placeholder);
    }

    [Fact]
    public void A_select_with_groups_or_disabled_options_uses_the_menu_that_draws_them()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema
        {
            Key = "s",
            Kind = FormFieldKind.Select,
            Options = [new("a", "Apple", Group: "Fruit"), new("b", "Beet", Group: "Veg", Disabled: true)],
        }));
        var select = engine.Control<FormSelectControl>("s");

        Assert.Null(select.Picker);
        var items = select.Menu!.Items!.Cast<KlySelectItem>().ToList();
        Assert.Equal(["Fruit", "Veg"], items.Select(i => i.Group).ToArray());
        Assert.True(items[1].Disabled);

        select.Menu.Value = new object[] { "a" };
        Assert.Equal("a", engine.EffectiveController!.GetValue("s"));

        engine.EffectiveController.SetValue("s", null);
        Assert.Empty(select.Menu.Value!);
    }

    [Fact]
    public void A_clearable_select_clears_to_nothing()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Select, Clearable = true, Options = [new("a", "Apple")] }), new Dictionary<string, object?> { ["s"] = "a" });
        var control = engine.ControlOf("s");
        Assert.True(control.ClearButton!.IsVisible);

        control.ClearButton.SendClicked();

        Assert.Null(engine.EffectiveController!.GetValue("s"));
        Assert.Null(engine.Control<FormSelectControl>("s").Picker!.SelectedItem);
    }

    [Fact]
    public void An_autocomplete_stores_the_value_of_the_option_whose_label_is_typed_or_the_text_itself()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "a", Kind = FormFieldKind.AutoComplete, Options = [new("fr", "France"), new("de", "Germany")] }));
        var menu = engine.Control<FormAutoCompleteControl>("a").Menu;
        Assert.Equal(["France", "Germany"], menu.Items!.Cast<KlySelectItem>().Select(i => i.Label).ToArray());

        menu.Value = "france";
        Assert.Equal("fr", engine.EffectiveController!.GetValue("a"));

        menu.Value = "Atlantis";
        Assert.Equal("Atlantis", engine.EffectiveController.GetValue("a"));

        engine.EffectiveController.SetValue("a", "de");
        Assert.Equal("Germany", menu.Value);
    }

    [Theory]
    [InlineData("france", "fr")]
    [InlineData("FRANCE", "fr")]
    [InlineData("fr", "fr")]
    [InlineData("Atlantis", "Atlantis")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void An_autocomplete_resolves_a_label_then_a_value_then_the_text(string? typed, string? expected)
    {
        var options = new FormOptionItem[] { new("fr", "France"), new("de", "Germany", Disabled: true) };

        Assert.Equal(expected, FormAutoCompleteControl.Resolve(options, typed));
        Assert.Equal("germany", FormAutoCompleteControl.Resolve(options, "germany"));
    }

    [Fact]
    public void An_option_to_create_shows_an_input_that_adds_and_selects_it()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema
        {
            Key = "s",
            Kind = FormFieldKind.Select,
            Options = [new("a", "Apple")],
            OnCreateOption = text => new FormOptionItem(text.ToLowerInvariant(), text),
        }));
        var view = engine.FieldView("s");
        Assert.NotNull(view.CreateInput);
        Assert.False(view.CreateButton!.IsEnabled);

        view.CreateInput!.Text = "Kiwi";
        Assert.True(view.CreateButton.IsEnabled);
        view.CreateOption();

        Assert.Equal("kiwi", engine.EffectiveController!.GetValue("s"));
        Assert.Equal("", view.CreateInput.Text);
        Assert.Contains("Kiwi", engine.Control<FormSelectControl>("s").Picker!.ItemsSource!.Cast<object>().Select(o => o.ToString()));
    }

    [Fact]
    public void A_field_without_an_option_to_create_has_no_create_input()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "s", Kind = FormFieldKind.Select, Options = [new("a", "Apple")] }));

        Assert.Null(engine.FieldView("s").CreateInput);
    }

    // ---- file

    // The plain framework target has no file system service behind FileResult: the tests give the control the bytes of each file by name.
    private static readonly Dictionary<string, byte[]> Contents = [];

    private static FileResult MemoryFile(string name, byte[] bytes, string contentType)
    {
        var path = Path.Combine(Path.GetTempPath(), $"kly-{Guid.NewGuid():N}-{name}");
        Contents[path] = bytes;
        return new FileResult(path, contentType);
    }

    private static FormFileControl Files(KlyDynamicFormEngine engine, string key)
    {
        var control = engine.Control<FormFileControl>(key);
        control.Open = file => Task.FromResult<Stream>(new MemoryStream(Contents[file.FullPath]));
        return control;
    }

    [Fact]
    public async Task A_file_field_reads_the_picked_files_into_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Kind = FormFieldKind.File, Accept = ".txt", MaxFileSize = 1000 }));
        var control = Files(engine, "f");
        Assert.Equal(".txt", control.Upload.Accept);
        Assert.Equal(1000, control.Upload.MaxSize);
        Assert.False(control.Upload.Multiple);

        await control.ReadAsync([MemoryFile("a.txt", [1, 2, 3], "text/plain")]);

        var file = Assert.IsType<FormFileValue>(engine.EffectiveController!.GetValue("f"));
        Assert.EndsWith("a.txt", file.Name);
        Assert.Equal(3, file.Size);
        Assert.Equal("text/plain", file.ContentType);
        await using var stream = await file.OpenReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public async Task A_file_above_the_limit_is_not_read_but_stays_in_the_value_so_validation_names_it()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Label = "File", Kind = FormFieldKind.File, MaxFileSize = 2 }));
        var control = Files(engine, "f");

        await control.ReadAsync([MemoryFile("big.bin", [1, 2, 3], "application/octet-stream")]);

        var file = Assert.IsType<FormFileValue>(engine.EffectiveController!.GetValue("f"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await file.OpenReadAsync(TestContext.Current.CancellationToken));
        Assert.False(engine.EffectiveController.Validate());
    }

    [Fact]
    public async Task A_multiple_file_field_holds_a_list_and_a_reset_form_empties_the_control()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "f", Kind = FormFieldKind.File, Multiple = true }));
        var control = Files(engine, "f");
        Assert.True(control.Upload.Multiple);

        await control.ReadAsync([MemoryFile("a", [1], ""), MemoryFile("b", [2], "")]);

        Assert.Equal(2, engine.EffectiveController!.GetValue<List<FormFileValue>>("f")!.Count);

        await control.ReadAsync([]);
        Assert.Null(engine.EffectiveController.GetValue("f"));
    }

    [Fact]
    public void The_button_of_a_file_field_says_one_file_or_several()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            new DynamicFieldSchema { Key = "one", Kind = FormFieldKind.File },
            new DynamicFieldSchema { Key = "many", Kind = FormFieldKind.File, Multiple = true }));

        Assert.Equal(FormTexts.Current.ChooseFile, engine.Control<FormFileControl>("one").Upload.Label);
        Assert.Equal(FormTexts.Current.ChooseFiles, engine.Control<FormFileControl>("many").Upload.Label);
    }
}
