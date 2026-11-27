namespace Klyroo.Form.Maui;

/// <summary>The view of one node of the form: a field, a block of text, a container, tabs or a repeatable list. It is made once, and <see cref="Refresh"/> shows the state of the form in it again.</summary>
internal abstract class FormNodeView : ContentView
{
    protected FormNodeView(FormContext context, FieldNode node)
    {
        Context = context;
        Node = node;
        HorizontalOptions = LayoutOptions.Fill;
    }

    public FormContext Context { get; }

    public FieldNode Node { get; }

    /// <summary>How many columns of the grid of the form the node spans.</summary>
    public int Span => Node.Span;

    public abstract void Refresh();
}

/// <summary>Makes the view of a node, by its kind.</summary>
internal static class FormNodes
{
    public static FormNodeView? Create(FormContext context, FieldNode node) => node.Kind switch
    {
        FormFieldKind.Hidden => null,
        FormFieldKind.Divider => new FormDividerView(context, node),
        FormFieldKind.Section => new FormHeadingView(context, node),
        FormFieldKind.Info => new FormInfoView(context, node),
        FormFieldKind.Button => new FormButtonView(context, node),
        FormFieldKind.Group or FormFieldKind.Card or FormFieldKind.Object => node.Fields is null ? null : new FormContainerView(context, node),
        FormFieldKind.Tabs => node.Tabs is { Count: > 0 } ? new FormTabsView(context, node) : null,
        FormFieldKind.ArrayList or FormFieldKind.ArrayTable => new FormArrayView(context, node),
        _ => new FormFieldView(context, node, slot: null),
    };
}

/// <summary>A grid of the views of the nodes that are in effect, in the order of the schema, refreshed together.</summary>
internal sealed class FormNodeGrid
{
    private readonly FormContext _context;

    public FormNodeGrid(FormContext context)
    {
        _context = context;
        Grid = new FormGrid(context.Columns, context.CompactWidth);
    }

    public FormGrid Grid { get; }

    /// <summary>Shows the nodes: the ones the form renders, and nothing for the rest.</summary>
    public void Update(IEnumerable<FieldNode> nodes)
    {
        var views = new List<FormNodeView>();
        foreach (var node in nodes)
        {
            if (_context.ViewOf(node) is { } view) views.Add(view);
        }
        Grid.SetItems([.. views.Select(v => ((View)v, v.Span))]);
        foreach (var view in views) view.Refresh();
    }

    /// <summary>The nodes of a list that the form renders now.</summary>
    public IEnumerable<FieldNode> InEffect(IEnumerable<FieldNode> nodes) => nodes.Where(_context.Engine.ShouldRenderField);
}

/// <summary>A horizontal rule.</summary>
internal sealed class FormDividerView : FormNodeView
{
    public FormDividerView(FormContext context, FieldNode node)
        : base(context, node)
    {
        Content = new KlySeparator { Margin = new Thickness(0, 4) };
    }

    public override void Refresh()
    {
    }
}

/// <summary>A heading with an optional description, between fields.</summary>
internal sealed class FormHeadingView : FormNodeView
{
    public FormHeadingView(FormContext context, FieldNode node)
        : base(context, node)
    {
        var title = FormUi.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
        title.Text = node.Label;
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level3);
        var description = FormUi.NewLabel(12, KlyTheme.TextMuted);
        FormUi.Show(description, node.Description);
        Title = title;
        Content = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(0, 8, 0, 0), Children = { title, description } };
    }

    public Label Title { get; }

    public override void Refresh()
    {
    }
}

/// <summary>A read-only block of text: a callout in the color of its tone, or plain text.</summary>
internal sealed class FormInfoView : FormNodeView
{
    public FormInfoView(FormContext context, FieldNode node)
        : base(context, node)
    {
        // A value cast from outside the enum counts as neutral.
        var tone = node.DisplayTone switch
        {
            FormInfoTone.Info => FormInfoTone.Info,
            FormInfoTone.Success => FormInfoTone.Success,
            FormInfoTone.Warning => FormInfoTone.Warning,
            FormInfoTone.Error => FormInfoTone.Error,
            _ => FormInfoTone.Neutral,
        };
        Tone = tone;
        var color = ColorOf(tone);
        var text = node.DisplayText ?? node.Description;
        if (node.DisplayKind == FormInfoKind.Text)
        {
            var textColor = tone == FormInfoTone.Neutral ? KlyTheme.TextMuted : KlyTheme.Resolve(color).Text;
            var title = FormUi.NewLabel(12, tone == FormInfoTone.Neutral ? KlyTheme.TextHighlighted : KlyTheme.Resolve(color).Text, FontAttributes.Bold);
            FormUi.Show(title, node.Label);
            var body = FormUi.NewLabel(12, textColor);
            FormUi.Show(body, text);
            Content = new VerticalStackLayout { Spacing = 2, Children = { title, body } };
            Callout = null;
        }
        else
        {
            Callout = new KlyAlert { Title = node.Label, Description = text, Color = color, Variant = KlyVariant.Subtle };
            Content = Callout;
        }
    }

    public FormInfoTone Tone { get; }

    /// <summary>The box of a callout; null for plain text.</summary>
    public KlyAlert? Callout { get; }

    public static KlyColor ColorOf(FormInfoTone tone) => tone switch
    {
        FormInfoTone.Info => KlyColor.Info,
        FormInfoTone.Success => KlyColor.Success,
        FormInfoTone.Warning => KlyColor.Warning,
        FormInfoTone.Error => KlyColor.Error,
        _ => KlyColor.Neutral,
    };

    public override void Refresh()
    {
    }
}

/// <summary>A button that runs the handler of the field and never submits the form.</summary>
internal sealed class FormButtonView : FormNodeView
{
    private readonly KlyButton _button;

    public FormButtonView(FormContext context, FieldNode node)
        : base(context, node)
    {
        _button = new KlyButton { Text = node.Label ?? node.Key, Variant = ButtonVariant.Outline, Color = KlyColor.Neutral, HorizontalOptions = LayoutOptions.Start, AutomationId = FormUi.Id(context.Engine, node.Key) };
        _button.Clicked += async (_, _) => await ClickAsync();
        var help = FormUi.NewLabel(12, KlyTheme.TextMuted);
        FormUi.Show(help, node.Help);
        Content = new VerticalStackLayout { Spacing = 4, Children = { _button, help } };
        Button = _button;
    }

    public KlyButton Button { get; }

    /// <summary>Runs the handler of the field, as a tap on the button does.</summary>
    public async Task ClickAsync()
    {
        if (!_button.IsEnabled || Node.OnClick is null) return;
        await Node.OnClick(Context.Engine);
    }

    public override void Refresh() => _button.IsEnabled = !Context.Engine.IsFieldDisabled(Node);
}

/// <summary>A group, a card or an object: a titled box of fields, which can collapse.</summary>
internal sealed class FormContainerView : FormNodeView
{
    private readonly FormNodeGrid _grid;

    public FormContainerView(FormContext context, FieldNode node)
        : base(context, node)
    {
        _grid = new FormNodeGrid(context);
        var body = new VerticalStackLayout { Spacing = 12 };
        if (!node.Collapsible && !string.IsNullOrEmpty(node.Label))
        {
            var title = FormUi.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
            title.Text = node.Label;
            SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level4);
            var description = FormUi.NewLabel(12, KlyTheme.TextMuted);
            FormUi.Show(description, node.Description);
            body.Add(new VerticalStackLayout { Spacing = 2, Children = { title, description } });
        }
        else if (node.Collapsible && !string.IsNullOrEmpty(node.Description))
        {
            var description = FormUi.NewLabel(12, KlyTheme.TextMuted);
            description.Text = node.Description;
            body.Add(description);
        }
        body.Add(_grid.Grid);

        View content = body;
        if (node.Collapsible)
        {
            Collapsible = new KlyCollapsible { Label = node.Label ?? node.Key, Open = !node.DefaultCollapsed, Content = body };
            content = Collapsible;
        }
        if (node.Kind == FormFieldKind.Card)
        {
            Card = new KlyCard { Content = content, Padding = new Thickness(16) };
            content = Card;
        }
        Content = content;
    }

    public KlyCollapsible? Collapsible { get; }

    public KlyCard? Card { get; }

    public FormGrid Grid => _grid.Grid;

    public override void Refresh() => _grid.Update(_grid.InEffect(Node.Fields ?? []));
}

/// <summary>Tabs: one panel of fields per visible tab, and the one shown is remembered.</summary>
internal sealed class FormTabsView : FormNodeView
{
    private readonly KlyTabs _tabs = new();
    private readonly Dictionary<string, FormNodeGrid> _panels = new(StringComparer.Ordinal);
    private string? _active;
    private List<string> _keys = [];

    public FormTabsView(FormContext context, FieldNode node)
        : base(context, node)
    {
        _tabs.AccessibleName = node.Label;
        _tabs.AutomationId = FormUi.Id(context.Engine, node.Key);
        _tabs.ValueChanged += (_, e) =>
        {
            if (e.Value is null || e.Value == _active) return;
            _active = e.Value;
            Refresh();
        };
        Content = _tabs;
    }

    public KlyTabs Tabs => _tabs;

    /// <summary>The key of the tab that is shown.</summary>
    public string? Active => _active;

    public FormGrid? PanelOf(string key) => _panels.TryGetValue(key, out var panel) ? panel.Grid : null;

    public override void Refresh()
    {
        var visible = Node.Tabs!.Where(t => t.Condition is null || t.Condition(Context.Engine)).ToList();
        IsVisible = visible.Count > 0;
        if (visible.Count == 0) return;

        var keys = visible.Select(t => t.Key).ToList();
        if (_active is null || !keys.Contains(_active, StringComparer.Ordinal)) _active = keys[0];
        if (!keys.SequenceEqual(_keys, StringComparer.Ordinal) || _tabs.Items.Count != keys.Count)
        {
            _keys = keys;
            _tabs.Items = [.. visible.Select(t => new KlyTabItem(t.Label) { Value = t.Key, Content = Panel(t).Grid })];
        }
        if (_tabs.Value != _active) _tabs.Value = _active;
        var activeTab = visible.First(t => t.Key == _active);
        var panel = Panel(activeTab);
        panel.Update(panel.InEffect(activeTab.Fields));
    }

    private FormNodeGrid Panel(TabNode tab)
    {
        if (!_panels.TryGetValue(tab.Key, out var panel)) _panels[tab.Key] = panel = new FormNodeGrid(Context);
        return panel;
    }
}
