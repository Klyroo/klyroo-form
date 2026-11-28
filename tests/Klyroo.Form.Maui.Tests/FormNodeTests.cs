using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormNodeTests
{
    private static DynamicFieldSchema Group(string key, FormFieldKind kind, params DynamicFieldSchema[] children) =>
        new() { Key = key, Kind = kind, Label = key, Fields = children };

    // ---- blocks of text

    [Fact]
    public void A_divider_is_a_separator_and_a_section_is_a_heading_with_its_description()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            new DynamicFieldSchema { Key = "d", Kind = FormFieldKind.Divider },
            new DynamicFieldSchema { Key = "h", Kind = FormFieldKind.Section, Label = "Billing", Description = "Where to send the invoice" }));

        Assert.Single(engine.ShellView!.Views<KlySeparator>());
        var heading = Assert.Single(engine.ShellView!.Views<FormHeadingView>());
        Assert.Equal("Billing", heading.Title.Text);
        Assert.Equal(SemanticHeadingLevel.Level3, SemanticProperties.GetHeadingLevel(heading.Title));
        Assert.Contains(heading.Views<Label>(), l => l.Text == "Where to send the invoice");
    }

    [Theory]
    [InlineData(FormInfoTone.Neutral)]
    [InlineData(FormInfoTone.Info)]
    [InlineData(FormInfoTone.Success)]
    [InlineData(FormInfoTone.Warning)]
    [InlineData(FormInfoTone.Error)]
    public void An_info_callout_has_the_color_of_its_tone_and_the_text_of_the_field(FormInfoTone tone)
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "i", Kind = FormFieldKind.Info, Label = "Heads up", DisplayText = "Read this", DisplayTone = tone }));

        var info = Assert.Single(engine.ShellView!.Views<FormInfoView>());

        Assert.Equal(tone, info.Tone);
        Assert.Equal("Heads up", info.Callout!.Title);
        Assert.Equal("Read this", info.Callout.Description);
        Assert.Equal(FormInfoView.ColorOf(tone), info.Callout.Color);
        Assert.Equal(KlyVariant.Subtle, info.Callout.Variant);
    }

    [Fact]
    public void An_info_takes_its_text_from_the_description_and_a_tone_out_of_range_is_neutral()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "i", Kind = FormFieldKind.Info, Description = "From the description", DisplayTone = (FormInfoTone)99 }));

        var info = Assert.Single(engine.ShellView!.Views<FormInfoView>());

        Assert.Equal(FormInfoTone.Neutral, info.Tone);
        Assert.Equal("From the description", info.Callout!.Description);
        Assert.Equal(KlyColor.Neutral, info.Callout.Color);
    }

    [Fact]
    public void A_plain_info_has_no_box()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "i", Kind = FormFieldKind.Info, Label = "Note", DisplayText = "Plain", DisplayKind = FormInfoKind.Text }));

        var info = Assert.Single(engine.ShellView!.Views<FormInfoView>());

        Assert.Null(info.Callout);
        Assert.Contains(info.Views<Label>(), l => l.Text == "Plain");
    }

    [Fact]
    public async Task A_button_field_runs_its_handler_and_never_submits_the_form()
    {
        var clicked = 0;
        var submitted = false;
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "b", Kind = FormFieldKind.Button, Label = "Check", Help = "Looks it up", OnClick = _ => { clicked++; return Task.CompletedTask; } }));
        engine.Submitted += (_, _) => submitted = true;
        var button = Assert.Single(engine.ShellView!.Views<FormButtonView>());

        Assert.Equal("Check", button.Button.Text);
        await button.ClickAsync();

        Assert.Equal(1, clicked);
        Assert.False(submitted);
        Assert.Contains(button.Views<Label>(), l => l.Text == "Looks it up");
    }

    [Fact]
    public async Task A_button_handler_that_throws_becomes_a_notice_in_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "b", Kind = FormFieldKind.Button, OnClick = _ => throw new InvalidOperationException("boom") }));
        var button = Assert.Single(engine.ShellView!.Views<FormButtonView>());

        await button.ClickAsync();

        Assert.Contains(FormTexts.Current.SomethingWentWrong, engine.EffectiveController!.FormErrors);
        Assert.True(engine.ShellView!.Summary.Box.IsVisible);
    }

    [Fact]
    public async Task A_button_is_disabled_with_the_form_and_does_nothing_then()
    {
        var clicked = 0;
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "b", Kind = FormFieldKind.Button, Disabled = true, OnClick = _ => { clicked++; return Task.CompletedTask; } }));
        var button = Assert.Single(engine.ShellView!.Views<FormButtonView>());

        await button.ClickAsync();

        Assert.False(button.Button.IsEnabled);
        Assert.Equal(0, clicked);
    }

    // ---- containers

    [Fact]
    public void A_group_lays_out_its_children_in_a_grid_and_a_card_draws_a_box_around_them()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            Group("g", FormFieldKind.Group, FormKit.Field("a"), FormKit.Field("b")),
            Group("c", FormFieldKind.Card, FormKit.Field("x"))));

        var containers = engine.ShellView!.Views<FormContainerView>().ToList();

        Assert.Equal(2, containers.Count);
        Assert.Null(containers[0].Card);
        Assert.Equal(2, containers[0].Grid.Views.Count);
        Assert.NotNull(containers[1].Card);
        Assert.Equal("g", containers[0].Views<Label>().First().Text);
    }

    [Fact]
    public void A_container_hides_children_that_are_not_in_effect()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            FormKit.Field("on"),
            Group("g", FormFieldKind.Object, new DynamicFieldSchema { Key = "inner", Condition = f => f.GetValue<string>("on") == "yes" })));
        var container = engine.ShellView!.Views<FormContainerView>().Single();
        Assert.Empty(container.Grid.Views);

        engine.Control<FormTextControl>("on").Input.Text = "yes";

        Assert.Single(container.Grid.Views);
    }

    [Fact]
    public void A_collapsible_container_opens_unless_it_starts_collapsed()
    {
        var engine = FormKit.Engine(FormKit.Schema(
            new DynamicFieldSchema { Key = "open", Kind = FormFieldKind.Group, Label = "Open", Collapsible = true, Fields = [FormKit.Field("a")] },
            new DynamicFieldSchema { Key = "shut", Kind = FormFieldKind.Group, Label = "Shut", Collapsible = true, DefaultCollapsed = true, Fields = [FormKit.Field("b")] }));

        var containers = engine.ShellView!.Views<FormContainerView>().ToList();

        Assert.True(containers[0].Collapsible!.Open);
        Assert.Equal("Open", containers[0].Collapsible!.Label);
        Assert.False(containers[1].Collapsible!.Open);
    }

    [Fact]
    public void A_container_without_children_renders_nothing()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "g", Kind = FormFieldKind.Group }, FormKit.Field("a")));

        Assert.Empty(engine.ShellView!.Views<FormContainerView>());
    }

    // ---- tabs

    private static DynamicFieldSchema Tabs() => new()
    {
        Key = "t",
        Kind = FormFieldKind.Tabs,
        Label = "Settings",
        Tabs =
        [
            new DynamicTabSchema { Key = "general", Label = "General", Fields = [FormKit.Field("name")] },
            new DynamicTabSchema { Key = "advanced", Label = "Advanced", Fields = [FormKit.Field("flag")], Condition = f => f.GetValue<string>("name") == "pro" },
        ],
    };

    [Fact]
    public void Tabs_show_the_visible_tabs_and_the_fields_of_the_selected_one()
    {
        var engine = FormKit.Engine(FormKit.Schema(Tabs()));
        var tabs = engine.ShellView!.Views<FormTabsView>().Single();

        Assert.Equal(["General"], tabs.Tabs.Items.Select(i => i.Label).ToArray());
        Assert.Equal("general", tabs.Active);
        Assert.Equal("Settings", tabs.Tabs.AccessibleName);
        Assert.Single(tabs.PanelOf("general")!.Views);

        engine.Control<FormTextControl>("name").Input.Text = "pro";

        Assert.Equal(["General", "Advanced"], tabs.Tabs.Items.Select(i => i.Label).ToArray());
    }

    [Fact]
    public void Choosing_a_tab_shows_its_fields_and_the_choice_survives_a_refresh()
    {
        var engine = FormKit.Engine(FormKit.Schema(Tabs()), new Dictionary<string, object?> { ["name"] = "pro" });
        var tabs = engine.ShellView!.Views<FormTabsView>().Single();

        tabs.Tabs.Value = "advanced";

        Assert.Equal("advanced", tabs.Active);
        Assert.Single(tabs.PanelOf("advanced")!.Views);
        engine.EffectiveController!.SetValue("flag", "x");
        Assert.Equal("advanced", tabs.Active);
    }

    [Fact]
    public void The_selected_tab_goes_back_to_the_first_when_it_is_no_longer_visible()
    {
        var engine = FormKit.Engine(FormKit.Schema(Tabs()), new Dictionary<string, object?> { ["name"] = "pro" });
        var tabs = engine.ShellView!.Views<FormTabsView>().Single();
        tabs.Tabs.Value = "advanced";

        engine.EffectiveController!.SetValue("name", "basic");

        Assert.Equal("general", tabs.Active);
    }

    // ---- repeatable lists

    private static DynamicFieldSchema Lines() => new()
    {
        Key = "lines",
        Kind = FormFieldKind.ArrayList,
        Label = "Lines",
        AddItemLabel = "Add a line",
        Fields = [new DynamicFieldSchema { Key = "sku", Label = "SKU", Required = true }, new DynamicFieldSchema { Key = "qty", Label = "Qty", Kind = FormFieldKind.Number }],
    };

    [Fact]
    public void An_empty_list_says_so_and_the_add_button_adds_a_row_with_a_cell_per_column()
    {
        var engine = FormKit.Engine(FormKit.Schema(Lines()));
        var list = engine.ShellView!.Views<FormArrayView>().Single();
        Assert.True(list.EmptyLabel.IsVisible);
        Assert.Equal(FormTexts.Current.NoItemsYet, list.EmptyLabel.Text);
        Assert.Equal("+ Add a line", list.AddButton.Text);

        list.AddButton.SendClicked();

        Assert.False(list.EmptyLabel.IsVisible);
        var row = Assert.Single(list.Rows);
        Assert.Equal(["sku", "qty"], row.Cells.Select(c => c.Node.Key).ToArray());
        Assert.Single(engine.EffectiveController!.GetArrayItems("lines"));
    }

    [Fact]
    public void A_cell_writes_into_its_row_and_follows_the_form()
    {
        var engine = FormKit.Engine(FormKit.Schema(Lines()));
        var list = engine.ShellView!.Views<FormArrayView>().Single();
        list.AddButton.SendClicked();
        list.AddButton.SendClicked();

        ((FormTextControl)list.Rows[1].Cells[0].Control).Input.Text = "ABC";

        Assert.Equal("ABC", engine.EffectiveController!.GetArrayItems("lines")[1]["sku"]);
        engine.EffectiveController.SetArrayItemValue("lines", 0, "sku", "ZZZ");
        Assert.Equal("ZZZ", ((FormTextControl)list.Rows[0].Cells[0].Control).Input.Text);
        Assert.Equal("ABC", ((FormTextControl)list.Rows[1].Cells[0].Control).Input.Text);
    }

    [Fact]
    public void The_remove_button_of_a_row_removes_that_row_and_says_which_one_it_is()
    {
        var engine = FormKit.Engine(FormKit.Schema(Lines()));
        var list = engine.ShellView!.Views<FormArrayView>().Single();
        list.AddButton.SendClicked();
        list.AddButton.SendClicked();
        engine.EffectiveController!.SetArrayItemValue("lines", 0, "sku", "first");
        engine.EffectiveController.SetArrayItemValue("lines", 1, "sku", "second");
        Assert.Equal(FormTexts.Current.RemoveItem(2), SemanticProperties.GetDescription(list.Rows[1].Remove));

        list.Rows[0].Remove.SendClicked();

        var row = Assert.Single(list.Rows);
        Assert.Equal("second", ((FormTextControl)row.Cells[0].Control).Input.Text);
    }

    [Fact]
    public async Task A_cell_with_an_error_shows_it_and_the_summary_names_the_cell()
    {
        var engine = FormKit.Engine(FormKit.Schema(Lines()));
        var list = engine.ShellView!.Views<FormArrayView>().Single();
        list.AddButton.SendClicked();

        Assert.False(await engine.SubmitAsync());

        Assert.Equal(FormMessages.Default.Required, list.Rows[0].Cells[0].Field.Error);
        Assert.Equal(FormTexts.Current.CellError("SKU", "Lines", 1, FormMessages.Default.Required), Assert.Single(engine.ShellView!.Summary.Lines).Label.Text);
    }

    [Fact]
    public void A_list_with_an_error_of_its_own_shows_it_and_a_disabled_list_cannot_add()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "l", Kind = FormFieldKind.ArrayTable, Label = "L", Disabled = true, DisabledReason = "Read only", Fields = [FormKit.Field("a")] }));
        var list = engine.ShellView!.Views<FormArrayView>().Single();

        engine.EffectiveController!.SetError("l", "Too many");

        Assert.False(list.AddButton.IsEnabled);
        Assert.True(list.ErrorLabel.IsVisible);
        Assert.Equal("Too many", list.ErrorLabel.Text);
    }

    [Fact]
    public void A_list_does_not_make_cells_for_hidden_or_container_columns()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema
        {
            Key = "l",
            Kind = FormFieldKind.ArrayList,
            Fields = [FormKit.Field("a"), new DynamicFieldSchema { Key = "h", Kind = FormFieldKind.Hidden }, new DynamicFieldSchema { Key = "i", Kind = FormFieldKind.Info }],
        }));
        var list = engine.ShellView!.Views<FormArrayView>().Single();

        list.AddButton.SendClicked();

        Assert.Equal(["a"], list.Rows[0].Cells.Select(c => c.Node.Key).ToArray());
    }

    // ---- per field extras

    [Fact]
    public void A_field_with_DirtyCheck_shows_a_reset_link_once_it_changed_and_puts_that_field_back()
    {
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema { Key = "a", DirtyCheck = true }, FormKit.Field("b")), new Dictionary<string, object?> { ["a"] = "orig", ["b"] = "other" });
        var view = engine.FieldView("a");
        Assert.False(view.ResetButton.IsVisible);

        engine.Control<FormTextControl>("a").Input.Text = "edited";
        engine.Control<FormTextControl>("b").Input.Text = "edited too";
        Assert.True(view.ResetButton.IsVisible);

        view.ResetButton.SendClicked();

        Assert.Equal("orig", engine.EffectiveController!.GetValue("a"));
        Assert.Equal("edited too", engine.EffectiveController.GetValue("b"));
        Assert.False(view.ResetButton.IsVisible);
    }

    [Fact]
    public async Task A_field_being_checked_asynchronously_shows_the_pending_line()
    {
        var gate = new TaskCompletionSource<string?>();
        var engine = FormKit.Engine(FormKit.Schema(new DynamicFieldSchema
        {
            Key = "u",
            Trigger = FormValidationTrigger.Input,
            AsyncValidators = [new DynamicAsyncValidatorRule("unique", (_, _, _) => new ValueTask<string?>(gate.Task)) { Debounce = TimeSpan.Zero }],
        }));
        var view = engine.FieldView("u");
        Assert.False(view.IsPending);

        engine.Control<FormTextControl>("u").Input.Text = "ada";
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.True(view.IsPending);

        gate.SetResult("Taken");
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.False(view.IsPending);
        Assert.Equal("Taken", view.Field.Error);
    }

    // ---- ids and focus

    [Fact]
    public void The_control_of_a_field_carries_the_id_of_the_field_for_tests_and_automation()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("first.name")));

        var id = engine.Id("first.name");

        Assert.EndsWith("first-name", id);
        Assert.Equal(id, engine.ControlOf("first.name").Root.AutomationId);
    }

    [Fact]
    public void The_ids_of_two_forms_with_the_same_keys_do_not_collide()
    {
        var one = FormKit.Engine(FormKit.Schema(FormKit.Field("a")));
        var two = FormKit.Engine(FormKit.Schema(FormKit.Field("a")));

        Assert.NotEqual(one.Id("a"), two.Id("a"));
    }

    [Fact]
    public void The_context_focuses_a_control_by_id_and_knows_when_there_is_none()
    {
        var engine = FormKit.Engine(FormKit.Schema(FormKit.Field("a")));
        var context = engine.ShellView!.Context;

        Assert.False(context.Focus("no-such-id"));
        Assert.False(context.Focus(engine.Id("a")));
    }
}
