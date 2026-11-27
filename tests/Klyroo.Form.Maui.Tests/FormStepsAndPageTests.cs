using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormStepsAndPageTests
{
    private static DynamicFormSchema Wizard(bool skipMiddle = false) => new()
    {
        FormKey = "wizard",
        Title = "Join",
        Steps =
        [
            new DynamicStepSchema { Key = "who", Title = "Who", Description = "Tell us about you", Fields = [new DynamicFieldSchema { Key = "name", Label = "Name", Required = true }] },
            new DynamicStepSchema { Key = "extra", Title = "Extra", SkipWhen = _ => skipMiddle, Fields = [FormKit.Field("note")] },
            new DynamicStepSchema { Key = "done", Title = "Done", Fields = [FormKit.Field("email")] },
        ],
    };

    [Fact]
    public void A_wizard_shows_the_stepper_the_description_of_the_step_and_only_its_fields()
    {
        var engine = FormKit.Engine(Wizard());
        var shell = engine.ShellView!;

        Assert.True(shell.Stepper.IsVisible);
        Assert.Equal(["Who", "Extra", "Done"], shell.Stepper.Items.Select(i => i.Title).ToArray());
        Assert.Equal(0, shell.Stepper.Current);
        Assert.Equal("Tell us about you", shell.StepDescription.Text);
        Assert.Equal(["name"], shell.Views<FormFieldView>().Select(v => v.Node.Key).ToArray());
    }

    [Fact]
    public void The_footer_of_a_wizard_has_next_until_the_last_step_and_back_after_the_first()
    {
        var engine = FormKit.Engine(Wizard());
        var shell = engine.ShellView!;

        Assert.True(shell.NextButton.IsVisible);
        Assert.False(shell.SubmitButton.IsVisible);
        Assert.False(shell.BackButton.IsVisible);
    }

    [Fact]
    public async Task Next_validates_the_step_and_stays_with_the_errors_when_it_fails()
    {
        var engine = FormKit.Engine(Wizard());
        var shell = engine.ShellView!;

        Assert.False(await shell.NextAsync());

        Assert.Equal(0, engine.EffectiveController!.CurrentStepIndex);
        Assert.Equal(FormMessages.Default.Required, engine.FieldView("name").Field.Error);
        Assert.True(shell.Summary.Box.IsVisible);
    }

    [Fact]
    public async Task Next_and_back_move_through_the_steps_and_the_footer_follows()
    {
        var engine = FormKit.Engine(Wizard());
        var shell = engine.ShellView!;
        engine.Control<FormTextControl>("name").Input.Text = "Ada";

        Assert.True(await shell.NextAsync());

        Assert.Equal(1, shell.Stepper.Current);
        Assert.Equal(["note"], shell.Views<FormFieldView>().Select(v => v.Node.Key).ToArray());
        Assert.True(shell.BackButton.IsVisible);
        Assert.False(shell.StepDescription.IsVisible);

        Assert.True(await shell.NextAsync());
        Assert.False(shell.NextButton.IsVisible);
        Assert.True(shell.SubmitButton.IsVisible);
        Assert.Equal(["email"], shell.Views<FormFieldView>().Select(v => v.Node.Key).ToArray());

        Assert.True(await shell.PreviousAsync());
        Assert.Equal(1, shell.Stepper.Current);
    }

    [Fact]
    public async Task The_return_key_before_the_last_step_means_next_not_submit()
    {
        var engine = FormKit.Engine(Wizard());
        engine.OnSave = (_, _) => throw new InvalidOperationException("must not save");
        engine.Control<FormTextControl>("name").Input.Text = "Ada";

        Assert.True(await engine.SubmitAsync());

        Assert.Equal(1, engine.EffectiveController!.CurrentStepIndex);
    }

    [Fact]
    public async Task The_last_step_submits_and_raises_Submitted()
    {
        var engine = FormKit.Engine(Wizard());
        IReadOnlyDictionary<string, object?>? output = null;
        engine.Submitted += (_, e) => output = e.Output;
        engine.Control<FormTextControl>("name").Input.Text = "Ada";
        await engine.ShellView!.NextAsync();
        await engine.ShellView.NextAsync();
        engine.Control<FormTextControl>("email").Input.Text = "ada@example.com";

        Assert.True(await engine.SubmitAsync());

        Assert.Equal("ada@example.com", output!["email"]);
    }

    [Fact]
    public void A_skipped_step_is_not_on_the_stepper_unless_the_form_is_on_it()
    {
        var engine = FormKit.Engine(Wizard(skipMiddle: true));
        var shell = engine.ShellView!;

        Assert.Equal(["Who", "Done"], shell.Stepper.Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public async Task Next_skips_a_step_whose_condition_says_so()
    {
        var engine = FormKit.Engine(Wizard(skipMiddle: true));
        engine.Control<FormTextControl>("name").Input.Text = "Ada";

        Assert.True(await engine.ShellView!.NextAsync());

        Assert.Equal(2, engine.EffectiveController!.CurrentStepIndex);
        Assert.Equal(1, engine.ShellView.Stepper.Current);
    }

    [Fact]
    public void ShowStepper_off_hides_the_stepper_but_keeps_the_steps()
    {
        var schema = new DynamicFormSchema { ShowStepper = false, Steps = [new DynamicStepSchema { Key = "a", Title = "A", Fields = [FormKit.Field("x")] }, new DynamicStepSchema { Key = "b", Title = "B", Fields = [FormKit.Field("y")] }] };
        var engine = FormKit.Engine(schema);

        Assert.False(engine.ShellView!.Stepper.IsVisible);
        Assert.True(engine.ShellView.NextButton.IsVisible);
    }

    [Fact]
    public async Task Reset_puts_a_wizard_back_on_its_first_step()
    {
        var engine = FormKit.Engine(Wizard());
        engine.Control<FormTextControl>("name").Input.Text = "Ada";
        await engine.ShellView!.NextAsync();
        Assert.Equal(1, engine.ShellView.Stepper.Current);

        engine.ShellView.ResetButton.SendClicked();

        Assert.Equal(0, engine.ShellView.Stepper.Current);
        Assert.Equal(["name"], engine.ShellView.Views<FormFieldView>().Select(v => v.Node.Key).ToArray());
    }

    // ---- page

    private static DynamicFormSchema Sections() => new()
    {
        FormKey = "page",
        Title = "Profile",
        Description = "Everything about you",
        Eyebrow = "Account",
        Sections =
        [
            new DynamicSectionSchema { Key = "identity", Label = "Identity", Description = "Who you are", Span = 6, Fields = [new DynamicFieldSchema { Key = "name", Label = "Name", Required = true }] },
            new DynamicSectionSchema { Key = "contact", Label = "Contact", Optional = true, Span = 6, Fields = [FormKit.Field("email")] },
            new DynamicSectionSchema { Key = "secret", Label = "Secret", Span = 12, Condition = f => f.GetValue<string>("name") == "root", Fields = [FormKit.Field("key")] },
        ],
    };

    private static KlyDynamicFormPage Page(DynamicFormSchema schema, IReadOnlyDictionary<string, object?>? input = null) =>
        new() { Form = new DynamicFormController(schema, input) };

    [Fact]
    public void A_page_shows_its_title_the_sections_in_effect_and_the_submit_button()
    {
        var page = Page(Sections());
        var shell = page.ShellView!;

        Assert.Equal("Profile", shell.TitleLabel.Text);
        Assert.Equal(["identity", "contact"], shell.Sections.Views.Select(v => ((KlyCard)v).AutomationId.Split("section-")[1]).ToArray());
        Assert.NotNull(shell.CardOf("identity"));
        Assert.Null(shell.CardOf("secret"));
        Assert.Equal(FormTexts.Current.Submit, shell.SubmitButton.Text);
    }

    [Fact]
    public void The_page_lays_sections_out_by_span()
    {
        var page = Page(Sections());

        var placement = page.ShellView!.Sections.Placement;

        Assert.Equal([(0, 0, 6), (0, 6, 6)], placement.Select(p => (p.Row, p.Column, p.ColumnSpan)).ToArray());
    }

    [Fact]
    public void The_navigation_lists_the_sections_in_effect_with_their_status_and_a_summary_of_what_is_left()
    {
        var page = Page(Sections());
        var shell = page.ShellView!;

        Assert.Equal(["Identity", "Contact"], shell.NavRows.Select(r => r.Label.Text).ToArray());
        Assert.Equal(FormTexts.Current.SectionProgress(0, 1), shell.NavRows[0].Badge.Text);
        Assert.Equal(KlyColor.Warning, shell.NavRows[0].Badge.Color);
        Assert.Equal(FormTexts.Current.SectionOptional, shell.NavRows[1].Badge.Text);
        Assert.Equal(KlyColor.Neutral, shell.NavRows[1].Badge.Color);
        Assert.Equal(FormTexts.Current.LeftToComplete(1), shell.SummaryText.Text);
    }

    [Fact]
    public void Filling_a_required_field_completes_its_section_and_the_summary_says_everything_is_ready()
    {
        var page = Page(Sections());
        var shell = page.ShellView!;

        page.Form!.SetValue("name", "Ada");

        Assert.Equal(FormTexts.Current.SectionComplete, shell.NavRows[0].Badge.Text);
        Assert.Equal(KlyColor.Success, shell.NavRows[0].Badge.Color);
        Assert.Equal(FormTexts.Current.AllReady, shell.SummaryText.Text);
    }

    [Fact]
    public async Task A_section_in_error_is_flagged_after_a_failed_submit()
    {
        var page = Page(Sections());

        Assert.False(await page.SubmitAsync());

        Assert.Equal(FormTexts.Current.SectionInvalid, page.ShellView!.NavRows[0].Badge.Text);
        Assert.Equal(KlyColor.Error, page.ShellView.NavRows[0].Badge.Color);
    }

    [Fact]
    public void A_section_that_changed_has_a_reset_that_puts_back_only_that_section()
    {
        var page = Page(Sections(), new Dictionary<string, object?> { ["name"] = "Ada", ["email"] = "a@x" });
        var shell = page.ShellView!;
        Assert.False(shell.ResetOf("identity")!.IsVisible);

        page.Form!.SetValue("name", "Grace");
        page.Form.SetValue("email", "g@x");
        Assert.True(shell.ResetOf("identity")!.IsVisible);

        shell.ResetOf("identity")!.SendClicked();

        Assert.Equal("Ada", page.Form.GetValue("name"));
        Assert.Equal("g@x", page.Form.GetValue("email"));
        Assert.False(shell.ResetOf("identity")!.IsVisible);
    }

    [Fact]
    public void A_section_that_comes_into_effect_gets_a_card()
    {
        var page = Page(Sections());

        page.Form!.SetValue("name", "root");

        Assert.NotNull(page.ShellView!.CardOf("secret"));
        Assert.Equal(3, page.ShellView.Sections.Views.Count);
        Assert.Equal(["Identity", "Contact", "Secret"], page.ShellView.NavRows.Select(r => r.Label.Text).ToArray());
    }

    [Fact]
    public async Task The_page_submits_with_the_save_step_and_raises_Submitted()
    {
        var page = Page(Sections(), new Dictionary<string, object?> { ["name"] = "Ada" });
        var saved = false;
        IReadOnlyDictionary<string, object?>? output = null;
        page.OnSave = (_, _) =>
        {
            saved = true;
            return Task.FromResult(true);
        };
        page.Submitted += (_, e) => output = e.Output;

        Assert.True(await page.SubmitAsync());

        Assert.True(saved);
        Assert.Equal("Ada", output!["name"]);
    }

    [Fact]
    public void The_page_fields_have_the_same_controls_as_the_form()
    {
        var page = Page(Sections());

        var field = page.ShellView!.Views<FormFieldView>().First(v => v.Node.Key == "name");

        Assert.IsType<FormTextControl>(field.Control);
        Assert.Equal("Name", field.Field.Label);
        Assert.True(field.Field.Required);
    }
}
