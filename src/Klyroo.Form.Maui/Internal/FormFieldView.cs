namespace Klyroo.Form.Maui;

/// <summary>
/// A field with the control of its kind: the label with its required mark, the hint, the description, the control, the pending check, the way to create an option, why the field is disabled, the
/// help and the error. A cell of a repeatable list is a smaller field with a label and an error only.
/// </summary>
internal sealed class FormFieldView : FormNodeView
{
    private readonly FormSlot _slot;
    private readonly FormControl _control;
    private readonly KlyFormField _field;
    private readonly VerticalStackLayout _root = new() { Spacing = 6 };
    private readonly HorizontalStackLayout _pending = new() { Spacing = 8, IsVisible = false };
    private readonly Label _pendingText = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _disabledReason = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly KlyButton _reset = new() { Variant = ButtonVariant.Link, Size = KlySize.Xs, HorizontalOptions = LayoutOptions.End, IsVisible = false };
    private readonly Grid? _create;
    private readonly KlyInput? _createInput;
    private readonly KlyButton? _createButton;

    public FormFieldView(FormContext context, FieldNode node, FormSlot? slot)
        : base(context, node)
    {
        var engine = context.Engine;
        _slot = slot ?? NewSlot(engine, node);
        _slot.Enter = () => Context.Enter?.Invoke();
        IsCell = _slot.IsCell;
        _control = FormControlFactory.Create(context, _slot);

        _field = new KlyFormField { Content = _control.Root };
        if (IsCell) _field.Size = KlySize.Sm;
        else
        {
            _field.Hint = node.Hint;
            _field.Description = node.Description;
            _field.Help = node.Help;
        }
        _field.AutomationId = _slot.Id + "-field";
        _root.Add(_field);

        if (!IsCell)
        {
            var spinner = new ActivityIndicator { IsRunning = true, WidthRequest = 14, HeightRequest = 14, VerticalOptions = LayoutOptions.Center };
            _pending.Add(spinner);
            _pending.Add(_pendingText);
            _root.Add(_pending);

            if (node.OnCreateOption is not null && node.Kind is FormFieldKind.Select or FormFieldKind.AutoComplete)
            {
                _createInput = new KlyInput { ReturnType = ReturnType.Done };
                _createButton = new KlyButton { Variant = ButtonVariant.Soft, Color = KlyColor.Neutral, VerticalOptions = LayoutOptions.Center };
                _create = new Grid { ColumnSpacing = 6, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
                _create.Add(_createInput, 0);
                _create.Add(_createButton, 1);
                _createInput.TextChanged += (_, _) => UpdateCreate();
                _createInput.Completed += (_, _) => CreateOption();
                _createButton.Clicked += (_, _) => CreateOption();
                _root.Add(_create);
            }

            _root.Add(_disabledReason);
            _reset.Clicked += (_, _) => Context.Engine.ResetField(Node.Key);
            _root.Add(_reset);
        }

        Content = _root;
        context.RegisterTarget(_slot.Id, _control.Focus, this);
    }

    public bool IsCell { get; }

    public FormSlot Slot => _slot;

    public FormControl Control => _control;

    public KlyFormField Field => _field;

    public KlyButton ResetButton => _reset;

    public KlyInput? CreateInput => _createInput;

    public KlyButton? CreateButton => _createButton;

    public bool IsPending => _pending.IsVisible;

    public Label DisabledReason => _disabledReason;

    /// <summary>The label is the text of the checkbox and of the switch, which have no label above them.</summary>
    private static bool HasInlineLabel(FormFieldKind kind) => kind is FormFieldKind.Checkbox or FormFieldKind.Switch;

    public override void Refresh()
    {
        var texts = FormTexts.Current;
        var showLabel = !string.IsNullOrEmpty(Node.Label) && !HasInlineLabel(Node.Kind);
        var required = _slot.Required;
        var error = _slot.Error;
        _control.Refresh();

        _field.Label = showLabel ? Node.Label : null;
        _field.Required = showLabel && required;
        _field.Error = error;
        if (IsCell) return;

        _pending.IsVisible = _slot.Validating;
        _pendingText.Text = texts.Checking;
        SemanticProperties.SetDescription(_pending, texts.Checking);

        var disabled = _slot.Disabled;
        _disabledReason.IsVisible = disabled && !string.IsNullOrEmpty(Node.DisabledReason);
        _disabledReason.Text = _disabledReason.IsVisible ? "🔒 " + Node.DisabledReason : null;

        _reset.Text = texts.Reset;
        _reset.IsVisible = Node.DirtyCheck && Context.Engine.DirtyPaths.Contains(Node.Key);
        if (_create is not null)
        {
            _createInput!.Placeholder = texts.NewOption;
            _createInput.AccessibleName = texts.NewOptionFor(Node.Label ?? Node.Key);
            _createButton!.Text = texts.Create;
            _createInput.IsEnabled = !disabled;
            UpdateCreate();
        }
    }

    private void UpdateCreate()
    {
        if (_createButton is null) return;
        _createButton.IsEnabled = !_slot.Disabled && !string.IsNullOrWhiteSpace(_createInput!.Text);
    }

    /// <summary>Asks the schema to make the option from the typed text, as the Create button does, and selects it.</summary>
    internal void CreateOption()
    {
        if (_createInput is null || _slot.Disabled) return;
        if (Context.Engine.CreateFieldOption(Node, _createInput.Text ?? "") is not null) _createInput.Text = "";
    }

    private static FormSlot NewSlot(FormEngine engine, FieldNode node) => new(
        engine,
        node,
        FormUi.Id(engine, node.Key),
        isCell: false,
        value: () => engine.GetValue(node.Key),
        set: v => engine.SetValue(node.Key, v),
        commit: v => engine.SetValue(node.Key, v, markTouched: true),
        blur: () => engine.MarkTouched(node.Key),
        error: () => engine.GetError(node.Key),
        disabled: () => engine.IsFieldDisabled(node),
        required: () => engine.IsFieldRequired(node),
        validating: () => engine.IsValidating(node.Key));
}

/// <summary>A repeatable list of rows: a titled box with an Add button, and a row per item with the controls of its columns and a button that removes it.</summary>
internal sealed class FormArrayView : FormNodeView
{
    private readonly KlyCard _card = new() { Padding = new Thickness(14) };
    private readonly Label _label = FormUi.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _hint = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly KlyButton _reset = new() { Variant = ButtonVariant.Link, Size = KlySize.Xs, IsVisible = false };
    private readonly KlyButton _add = new() { Variant = ButtonVariant.Soft, Color = KlyColor.Neutral, Size = KlySize.Sm };
    private readonly Label _description = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _empty = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly VerticalStackLayout _rows = new() { Spacing = 8 };
    private readonly Label _disabledReason = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _help = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _error = FormUi.NewLabel(12, KlyTheme.Resolve(KlyColor.Error).Text, FontAttributes.Bold);
    private readonly List<RowView> _items = [];
    private IReadOnlyList<FieldNode> _cells = [];
    private int _count = -1;

    public FormArrayView(FormContext context, FieldNode node)
        : base(context, node)
    {
        _label.Text = node.Label ?? node.Key;
        _hint.Text = node.Hint;
        _hint.IsVisible = !string.IsNullOrEmpty(node.Hint);
        FormUi.Show(_description, node.Description);
        FormUi.Show(_help, node.Help);
        _label.VerticalOptions = LayoutOptions.Center;
        _add.AutomationId = FormUi.Id(context.Engine, node.Key) + "-add";
        _add.Clicked += (_, _) => context.Engine.AddArrayItem(node);
        _reset.Clicked += (_, _) => context.Engine.ResetField(node.Key);

        var actions = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center, Children = { _hint, _reset, _add } };
        var header = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        header.Add(_label, 0);
        header.Add(actions, 1);

        _card.Content = new VerticalStackLayout { Spacing = 10, Children = { header, _description, _empty, _rows, _disabledReason, _help, _error } };
        Content = _card;
        SemanticProperties.SetDescription(_card, node.Label ?? node.Key);
    }

    public KlyButton AddButton => _add;

    public KlyButton ResetButton => _reset;

    public KlyCard Card => _card;

    public Label EmptyLabel => _empty;

    public Label ErrorLabel => _error;

    /// <summary>The row views, for the tests.</summary>
    public IReadOnlyList<(Grid Row, IReadOnlyList<FormFieldView> Cells, FormIconButton Remove)> Rows => [.. _items.Select(i => ((Grid)i.Layout, (IReadOnlyList<FormFieldView>)i.Cells, i.Remove))];

    public override void Refresh()
    {
        var engine = Context.Engine;
        var texts = FormTexts.Current;
        var field = Node;
        var items = engine.GetArrayItems(field.Key);
        var error = engine.GetError(field.Key);
        var required = engine.IsFieldRequired(field);
        var disabled = engine.IsFieldDisabled(field);

        _label.FormattedText = null;
        _label.Text = (field.Label ?? field.Key) + (required ? " *" : "");
        _card.SetStroke(error is null ? KlyTheme.Border : KlyTheme.Resolve(KlyColor.Error).Fill);
        _reset.Text = texts.Reset;
        _reset.IsVisible = field.DirtyCheck && engine.DirtyPaths.Contains(field.Key);
        _add.Text = "+ " + (field.AddItemLabel ?? texts.AddItem);
        _add.IsEnabled = !disabled;
        _empty.Text = texts.NoItemsYet;
        _empty.HorizontalTextAlignment = TextAlignment.Center;
        _empty.IsVisible = items.Count == 0;
        _disabledReason.IsVisible = disabled && !string.IsNullOrEmpty(field.DisabledReason);
        _disabledReason.Text = _disabledReason.IsVisible ? "🔒 " + field.DisabledReason : null;
        FormUi.Show(_error, error);

        var cells = (field.Fields ?? []).Where(c => c.Kind != FormFieldKind.Hidden && engine.ShouldRenderField(c) && !FormEngine.IsStateless(c.Kind) && !FormEngine.IsContainer(c.Kind)).ToList();
        if (items.Count != _count || !cells.SequenceEqual(_cells)) Rebuild(items.Count, cells, disabled);
        foreach (var row in _items)
        {
            row.Remove.SetDescription(texts.RemoveItem(row.Index + 1));
            row.Remove.IsEnabled = !disabled;
            foreach (var cell in row.Cells) cell.Refresh();
        }
    }

    private void Rebuild(int count, IReadOnlyList<FieldNode> cells, bool disabled)
    {
        _count = count;
        _cells = cells;
        _items.Clear();
        _rows.Children.Clear();
        var engine = Context.Engine;
        var field = Node;
        for (var i = 0; i < count; i++)
        {
            var index = i;
            var grid = new FormGrid(Context.Columns, Context.CompactWidth, spacing: 8);
            var views = new List<FormFieldView>();
            var placed = new List<(View, int)>();
            foreach (var child in cells)
            {
                var cellId = FormUi.Id(engine, $"{field.Key}-{index}-{child.Key}");
                var slot = new FormSlot(
                    engine,
                    child,
                    cellId,
                    isCell: true,
                    value: () => CellValue(engine, field.Key, index, child.Key),
                    set: v => engine.SetArrayItemValue(field.Key, index, child.Key, v),
                    commit: v => engine.SetArrayItemValue(field.Key, index, child.Key, v, markTouched: true),
                    blur: () => engine.MarkArrayItemTouched(field.Key, index, child.Key),
                    error: () => engine.GetArrayItemError(field.Key, index, child.Key),
                    disabled: () => engine.IsFieldDisabled(field) || engine.IsFieldDisabled(child),
                    required: () => engine.IsFieldRequired(child),
                    validating: () => false);
                var cell = new FormFieldView(Context, child, slot);
                views.Add(cell);
                placed.Add((cell, child.Span));
            }
            grid.SetItems(placed);

            var remove = new FormIconButton(KlyIconNames.Close, FormTexts.Current.RemoveItem(index + 1), KlyTheme.Resolve(KlyColor.Error).Text) { VerticalOptions = LayoutOptions.Start };
            remove.Clicked += (_, _) => engine.RemoveArrayItem(field.Key, index);
            var row = new Grid { ColumnSpacing = 8, Padding = new Thickness(8), ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            row.Add(grid, 0);
            row.Add(remove, 1);
            var frame = new Border { StrokeThickness = 1, Content = row };
            frame.SetRadius(FormThemeExtensions.RadiusOf(1));
            frame.SetStroke(KlyTheme.Border);
            _rows.Add(frame);
            _items.Add(new RowView(index, row, views, remove));
        }
    }

    private static object? CellValue(FormEngine engine, string arrayKey, int index, string childKey)
    {
        var rows = engine.GetArrayItems(arrayKey);
        return index < rows.Count ? rows[index].GetValueOrDefault(childKey) : null;
    }

    private sealed record RowView(int Index, Layout Layout, List<FormFieldView> Cells, FormIconButton Remove);
}
