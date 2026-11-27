using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormShellTests
{
    [Fact]
    public void Nothing_is_shown_without_a_schema_or_a_controller()
    {
        var engine = new KlyDynamicFormEngine();

        Assert.Null(engine.Content);
        Assert.Null(engine.EffectiveController);
    }

    [Fact]
    public void A_schema_makes_a_controller_and_a_shell_with_the_header_of_the_schema()
    {
        var engine = new KlyDynamicFormEngine
        {
            Schema = new DynamicFormSchema { Title = "Sign up", Description = "Two minutes", Eyebrow = "Account", Fields = [FormKit.Field("name")] },
        };

        var shell = engine.ShellView!;
        Assert.NotNull(engine.EffectiveController);
        Assert.Equal("Sign up", shell.TitleLabel.Text);
        Assert.Equal("Two minutes", shell.DescriptionLabel.Text);
        Assert.Equal("Account", shell.EyebrowLabel.Text);
        Assert.True(shell.TitleLabel.IsVisible);
    }

    [Fact]
    public void The_header_is_hidden_when_the_schema_has_no_title_description_or_steps()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));

        Assert.False(engine.ShellView!.HeaderLine.IsVisible);
    }

    [Fact]
    public void A_given_controller_wins_over_the_schema_and_stays_the_callers_after_dispose()
    {
        var controller = new DynamicFormController(FormKit.Schema(FormKit.Field("a")));
        var engine = new KlyDynamicFormEngine { Schema = FormKit.Schema(FormKit.Field("b")), Form = controller };

        Assert.Same(controller, engine.EffectiveController);
        Assert.NotNull(engine.FieldView("a"));
        engine.Dispose();
        controller.SetValue("a", "x");
        Assert.Equal("x", controller.GetValue("a"));
    }

    [Fact]
    public void A_new_schema_instance_makes_a_new_controller_and_keeps_the_view_for_the_same_one()
    {
        var first = FormKit.Schema(FormKit.Field("a"));
        var engine = new KlyDynamicFormEngine { Schema = first };
        var controller = engine.EffectiveController;
        var shell = engine.ShellView;

        engine.Input = new Dictionary<string, object?> { ["a"] = "reloads the controller of the schema" };
        Assert.Same(controller, engine.EffectiveController);
        Assert.Equal("reloads the controller of the schema", engine.EffectiveController!.GetValue("a"));
        Assert.Same(shell, engine.ShellView);

        engine.Schema = FormKit.Schema(FormKit.Field("b"));
        Assert.NotSame(controller, engine.EffectiveController);
        Assert.NotSame(shell, engine.ShellView);
    }

    [Fact]
    public void The_initial_values_of_Input_start_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")), new Dictionary<string, object?> { ["name"] = "Ada" });

        Assert.Equal("Ada", engine.Control<FormTextControl>("name").Input.Text);
        Assert.False(engine.EffectiveController!.IsDirty);
    }

    [Fact]
    public void Typing_in_a_field_writes_into_the_controller_and_makes_the_form_dirty()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        var input = engine.Control<FormTextControl>("name").Input;

        input.Text = "Ada";

        Assert.Equal("Ada", engine.EffectiveController!.GetValue("name"));
        Assert.True(engine.EffectiveController.IsDirty);
        Assert.True(engine.ShellView!.ResetButton.IsVisible);
    }

    [Fact]
    public void A_value_set_from_code_shows_in_the_control_without_echoing_back()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        var changes = 0;
        engine.EffectiveController!.Changed += () => changes++;

        engine.EffectiveController.SetValue("name", "Grace");

        Assert.Equal("Grace", engine.Control<FormTextControl>("name").Input.Text);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void The_reset_button_puts_the_values_back_and_hides_itself()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")), new Dictionary<string, object?> { ["name"] = "Ada" });
        var input = engine.Control<FormTextControl>("name").Input;
        input.Text = "Grace";
        Assert.True(engine.ShellView!.ResetButton.IsVisible);

        engine.ShellView.ResetButton.SendClicked();

        Assert.Equal("Ada", input.Text);
        Assert.False(engine.ShellView.ResetButton.IsVisible);
    }

    [Fact]
    public void ShowReset_off_never_shows_the_reset_button()
    {
        var engine = new KlyDynamicFormEngine { Schema = FormKit.Schema(FormKit.Field("name")), ShowReset = false };
        engine.Control<FormTextControl>("name").Input.Text = "x";

        Assert.False(engine.ShellView!.ResetButton.IsVisible);
    }

    [Fact]
    public async Task The_submit_button_says_what_SubmitLabel_says_and_Saving_while_it_saves()
    {
        var gate = new TaskCompletionSource<bool>();
        var engine = new KlyDynamicFormEngine { Schema = FormKit.Schema(FormKit.Field("name")), SubmitLabel = "Send", OnSave = (_, _) => gate.Task };
        var shell = engine.ShellView!;
        Assert.Equal("Send", shell.SubmitButton.Text);

        var submit = engine.SubmitAsync();

        Assert.Equal(FormTexts.Current.Saving, shell.SubmitButton.Text);
        Assert.False(shell.SubmitButton.IsEnabled);
        gate.SetResult(true);
        Assert.True(await submit);
        Assert.Equal("Send", shell.SubmitButton.Text);
        Assert.True(shell.SubmitButton.IsEnabled);
    }

    [Fact]
    public async Task A_valid_submit_runs_OnSave_with_the_output_then_raises_Submitted_once_and_leaves_the_form_clean()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        IReadOnlyDictionary<string, object?>? saved = null;
        var submitted = new List<IReadOnlyDictionary<string, object?>>();
        engine.OnSave = (output, _) =>
        {
            saved = output;
            return Task.FromResult(true);
        };
        engine.Submitted += (_, e) => submitted.Add(e.Output);
        engine.Control<FormTextControl>("name").Input.Text = "Ada";

        Assert.True(await engine.SubmitAsync());

        Assert.Equal("Ada", saved!["name"]);
        Assert.Single(submitted);
        Assert.Equal("Ada", submitted[0]["name"]);
        Assert.False(engine.EffectiveController!.IsDirty);
        Assert.False(engine.EffectiveController.IsSubmitting);
    }

    [Fact]
    public async Task A_save_that_returns_false_keeps_the_form_dirty_and_raises_nothing()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        engine.OnSave = (_, _) => Task.FromResult(false);
        var raised = false;
        engine.Submitted += (_, _) => raised = true;
        engine.Control<FormTextControl>("name").Input.Text = "Ada";

        Assert.False(await engine.SubmitAsync());

        Assert.False(raised);
        Assert.True(engine.EffectiveController!.IsDirty);
    }

    [Fact]
    public async Task A_save_that_throws_becomes_a_form_error_and_not_an_exception()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        engine.OnSave = (_, _) => throw new InvalidOperationException("boom");

        Assert.False(await engine.SubmitAsync());

        Assert.Contains(FormTexts.Current.SomethingWentWrong, engine.EffectiveController!.FormErrors);
        Assert.True(engine.ShellView!.Summary.Box.IsVisible);
    }

    [Fact]
    public async Task A_failed_attempt_clears_the_something_went_wrong_notice_of_the_previous_one()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        var fail = true;
        engine.OnSave = (_, _) => fail ? throw new InvalidOperationException("boom") : Task.FromResult(true);

        await engine.SubmitAsync();
        fail = false;
        Assert.True(await engine.SubmitAsync());

        Assert.Empty(engine.EffectiveController!.FormErrors);
    }

    [Fact]
    public async Task A_submit_that_fails_validation_shows_the_errors_and_the_summary_lists_them()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "name", Label = "Name", Required = true }));

        Assert.False(await engine.SubmitAsync());

        var summary = engine.ShellView!.Summary;
        Assert.True(summary.Box.IsVisible);
        Assert.Equal(FormTexts.Current.ErrorsToFix(1), summary.Title);
        Assert.Equal(FormTexts.Current.FieldError("Name", FormMessages.Default.Required), Assert.Single(summary.Lines).Label.Text);
        Assert.Equal(FormMessages.Default.Required, engine.FieldView("name").Field.Error);
    }

    [Fact]
    public async Task Cancel_and_the_close_button_show_only_while_someone_listens_to_Cancelled()
    {
        var engine = new KlyDynamicFormEngine { Schema = FormKit.Schema(FormKit.Field("name")), Shell = FormShellMode.Modal };
        var shell = engine.ShellView!;
        Assert.False(shell.CancelButton.IsVisible);
        Assert.False(shell.CloseButton.IsVisible);

        var outputs = new List<IReadOnlyDictionary<string, object?>>();
        EventHandler<DynamicFormOutputEventArgs> handler = (_, e) => outputs.Add(e.Output);
        engine.Cancelled += handler;
        Assert.True(shell.CancelButton.IsVisible);
        Assert.True(shell.CloseButton.IsVisible);

        engine.Control<FormTextControl>("name").Input.Text = "draft";
        await shell.CancelAsync();
        Assert.Equal("draft", Assert.Single(outputs)["name"]);

        engine.Cancelled -= handler;
        Assert.False(shell.CancelButton.IsVisible);
    }

    [Fact]
    public void An_inline_form_has_no_close_button_even_with_a_listener()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("name")));
        engine.Cancelled += (_, _) => { };

        Assert.True(engine.ShellView!.CancelButton.IsVisible);
        Assert.False(engine.ShellView.CloseButton.IsVisible);
    }

    [Fact]
    public void Fields_not_in_effect_are_not_shown_and_come_back_when_their_condition_holds()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            FormKit.Field("kind"),
            new DynamicFieldSchema { Key = "other", Label = "Other", Condition = f => f.GetValue<string>("kind") == "other" }));
        var shell = engine.ShellView!;
        Assert.DoesNotContain(shell.Views<FormFieldView>(), v => v.Node.Key == "other");

        engine.Control<FormTextControl>("kind").Input.Text = "other";

        Assert.Contains(shell.Views<FormFieldView>(), v => v.Node.Key == "other");
    }

    [Fact]
    public void A_field_keeps_its_view_while_the_form_lives()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("a"), FormKit.Field("b")));
        var view = engine.FieldView("a");

        engine.EffectiveController!.SetValue("b", "x");
        engine.EffectiveController.SetValue("a", "y");

        Assert.Same(view, engine.FieldView("a"));
    }

    [Fact]
    public void Hidden_fields_have_no_view_but_keep_their_value()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("a"), new DynamicFieldSchema { Key = "token", Kind = FormFieldKind.Hidden, DefaultValue = "abc" }));

        Assert.DoesNotContain(engine.ShellView!.Views<FormFieldView>(), v => v.Node.Key == "token");
        Assert.Equal("abc", engine.EffectiveController!.GetValue("token"));
    }

    [Fact]
    public void The_grid_lays_fields_out_by_span_in_the_columns_of_the_schema()
    {
        var engine = FormKit.Engine(new DynamicFormSchema
        {
            Columns = 12,
            Fields =
            [
                new DynamicFieldSchema { Key = "a", Span = 6 },
                new DynamicFieldSchema { Key = "b", Span = 6 },
                new DynamicFieldSchema { Key = "c", Span = 12 },
                new DynamicFieldSchema { Key = "d", Span = 8 },
            ],
        });

        var placement = engine.ShellView!.Grid.Placement;

        Assert.Equal([(0, 0, 6), (0, 6, 6), (1, 0, 12), (2, 0, 8)], placement.Select(p => (p.Row, p.Column, p.ColumnSpan)).ToArray());
        Assert.Equal(12, engine.ShellView.Grid.ColumnDefinitions.Count);
    }

    [Fact]
    public void A_span_is_clamped_to_the_columns_of_the_schema()
    {
        var engine = FormKit.Engine(new DynamicFormSchema { Columns = 2, Fields = [new DynamicFieldSchema { Key = "a", Span = 9 }, new DynamicFieldSchema { Key = "b", Span = 1 }] });

        var placement = engine.ShellView!.Grid.Placement;

        Assert.Equal(2, engine.ShellView.Grid.ColumnDefinitions.Count);
        Assert.Equal([(0, 0, 2), (1, 0, 1)], placement.Select(p => (p.Row, p.Column, p.ColumnSpan)).ToArray());
    }

    [Theory]
    [InlineData(300, true)]
    [InlineData(479, true)]
    [InlineData(480, false)]
    [InlineData(900, false)]
    public void A_narrow_form_puts_every_field_on_its_own_row_and_a_wide_one_keeps_the_columns(double width, bool compact)
    {
        var grid = new FormGrid(12, 480);
        grid.SetItems([(new BoxView(), 6), (new BoxView(), 6), (new BoxView(), 4)]);

        ((IView)grid).Arrange(new Rect(0, 0, width, 400));

        Assert.Equal(compact, grid.Compact);
        Assert.Equal(compact ? new[] { 0, 1, 2 } : new[] { 0, 0, 1 }, grid.Placement.Select(p => p.Row).ToArray());
        Assert.Equal(compact ? 1 : 12, grid.ColumnDefinitions.Count);
        Assert.All(grid.Placement, p => Assert.True(p.ColumnSpan <= grid.ColumnDefinitions.Count));
    }

    [Fact]
    public void A_form_that_grows_wide_again_goes_back_to_its_columns()
    {
        var grid = new FormGrid(12, 480);
        grid.SetItems([(new BoxView(), 6), (new BoxView(), 6)]);

        ((IView)grid).Arrange(new Rect(0, 0, 300, 400));
        Assert.True(grid.Compact);
        ((IView)grid).Arrange(new Rect(0, 0, 800, 400));

        Assert.False(grid.Compact);
        Assert.Equal([0, 0], grid.Placement.Select(p => p.Row).ToArray());
    }

    [Fact]
    public void A_grid_with_a_compact_width_of_zero_never_goes_compact()
    {
        var grid = new FormGrid(12, 0);
        grid.SetItems([(new BoxView(), 6), (new BoxView(), 6)]);

        ((IView)grid).Arrange(new Rect(0, 0, 100, 400));

        Assert.False(grid.Compact);
    }

    [Fact]
    public void The_same_views_with_the_same_spans_do_not_rebuild_the_grid()
    {
        var grid = new FormGrid(12, 480);
        var views = new[] { new BoxView(), new BoxView() };
        grid.SetItems([(views[0], 6), (views[1], 6)]);
        var first = grid.Children.ToList();

        grid.SetItems([(views[0], 6), (views[1], 6)]);

        Assert.Equal(first, grid.Children.ToList());
        grid.SetItems([(views[0], 12), (views[1], 6)]);
        Assert.Equal(12, grid.Placement[0].ColumnSpan);
    }

    [Fact]
    public void The_compact_width_zero_keeps_the_columns_whatever_the_width()
    {
        var engine = new KlyDynamicFormEngine { CompactWidth = 0, Schema = FormKit.Schema(FormKit.Field("a")) };

        Assert.Equal(0, engine.ShellView!.Grid.CompactWidth);
    }

    [Fact]
    public void Setting_properties_before_the_schema_builds_the_shell_once_the_schema_arrives()
    {
        var engine = new KlyDynamicFormEngine { SubmitLabel = "Go", Shell = FormShellMode.Drawer };
        Assert.Null(engine.ShellView);

        engine.Schema = FormKit.Schema(FormKit.Field("a"));

        Assert.Equal("Go", engine.ShellView!.SubmitButton.Text);
        Assert.Equal(FormShellMode.Drawer, engine.ShellView.Mode);
    }

    [Fact]
    public void Removing_the_schema_clears_the_view()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("a")));

        engine.Schema = null;

        Assert.Null(engine.Content);
        Assert.Null(engine.EffectiveController);
    }
}
