namespace Klyroo.Form.Maui;

/// <summary>
/// A list of choices shown all at once: radio buttons, or checkboxes for a checkbox group and for a multiple select. Options with a group name go under their own legend,
/// one control per group, and the groups act as one: choosing in a radio group empties the others.
/// </summary>
internal sealed class FormChoiceControl : FormControl
{
    private readonly bool _multiple;
    private readonly VerticalStackLayout _stack = new() { Spacing = 12 };
    private readonly List<ChoiceGroup> _groups = [];
    private IReadOnlyList<FormOptionItem> _options = [];
    private bool _built;

    public FormChoiceControl(FormSlot slot, bool multiple)
        : base(slot)
    {
        _multiple = multiple;
        Init(_stack);
    }

    protected override bool CanClear => false;

    /// <summary>The control of every group of options, for the tests.</summary>
    public IReadOnlyList<View> Controls => [.. _groups.Select(g => g.Control)];

    protected override void Show()
    {
        var options = Slot.Engine.GetFieldOptions(Slot.Field);
        if (!_built || !options.SequenceEqual(_options)) Rebuild(options);
        ShowValue();
    }

    protected override void ApplyEnabled(bool enabled)
    {
        foreach (var group in _groups) group.Control.IsEnabled = enabled;
    }

    public override bool Focus() => false;

    private void Rebuild(IReadOnlyList<FormOptionItem> options)
    {
        _built = true;
        _options = options;
        _groups.Clear();
        _stack.Children.Clear();
        foreach (var (name, items) in GroupOptions(options))
        {
            var group = _multiple ? NewCheckboxes(name, items) : NewRadios(name, items);
            _groups.Add(group);
            _stack.Add(group.Control);
        }
        ApplyEnabled(!Slot.Disabled);
    }

    private ChoiceGroup NewRadios(string? name, List<FormOptionItem> items)
    {
        var radios = new KlyRadioGroup
        {
            Legend = name,
            Items = [.. items.Select(o => new KlyRadioItem(o.Value, o.Label) { Description = o.Description, Disabled = o.Disabled })],
        };
        var group = new ChoiceGroup(radios, items);
        radios.ValueChanged += (_, e) =>
        {
            if (Busy || e.NewValue is null) return;
            // The other groups let go of their choice: one value for the whole field.
            foreach (var other in _groups.Where(g => !ReferenceEquals(g, group)))
            {
                if (other.Control is KlyRadioGroup { Value: not null } otherRadios) otherRadios.Value = null;
            }
            Slot.Commit(e.NewValue.ToString());
        };
        return group;
    }

    private ChoiceGroup NewCheckboxes(string? name, List<FormOptionItem> items)
    {
        var boxes = new KlyCheckboxGroup
        {
            Legend = name,
            Items = [.. items.Select(o => new KlyCheckboxItem(o.Value, o.Label) { Description = o.Description, Disabled = o.Disabled })],
        };
        var group = new ChoiceGroup(boxes, items);
        boxes.ValueChanged += (_, e) =>
        {
            if (Busy) return;
            var before = (e.OldValue ?? []).Select(v => v.ToString() ?? "").ToList();
            var after = (e.NewValue ?? []).Select(v => v.ToString() ?? "").ToList();
            var list = FormUi.AsStringList(Slot.Value);
            foreach (var value in before.Except(after, StringComparer.Ordinal)) list.RemoveAll(v => v == value);
            foreach (var value in after.Except(before, StringComparer.Ordinal))
            {
                list.RemoveAll(v => v == value);
                list.Add(value);
            }
            Slot.Commit(list);
        };
        return group;
    }

    private void ShowValue()
    {
        if (_multiple)
        {
            var selected = FormUi.AsStringList(Slot.Value);
            foreach (var group in _groups)
            {
                var mine = group.Items.Where(o => selected.Contains(o.Value, StringComparer.Ordinal)).Select(o => (object)o.Value).ToList();
                var boxes = (KlyCheckboxGroup)group.Control;
                var current = (boxes.Value ?? []).Select(v => v.ToString()).ToList();
                if (!current.SequenceEqual(mine.Select(v => v.ToString()))) boxes.Value = mine;
            }
            return;
        }
        var single = Slot.Value?.ToString();
        foreach (var group in _groups)
        {
            var radios = (KlyRadioGroup)group.Control;
            var wanted = single is not null && group.Items.Any(o => o.Value == single) ? single : null;
            if (!Equals(radios.Value?.ToString(), wanted)) radios.Value = wanted;
        }
    }

    /// <summary>Options in the order their groups first appear, the ungrouped ones first when they come first.</summary>
    internal static List<(string? Group, List<FormOptionItem> Items)> GroupOptions(IReadOnlyList<FormOptionItem> options)
    {
        var groups = new List<(string? Group, List<FormOptionItem> Items)>();
        foreach (var option in options)
        {
            var group = string.IsNullOrEmpty(option.Group) ? null : option.Group;
            var existing = groups.FindIndex(g => g.Group == group);
            if (existing < 0) groups.Add((group, [option]));
            else groups[existing].Items.Add(option);
        }
        if (groups.Count == 0) groups.Add((null, []));
        return groups;
    }

    private sealed record ChoiceGroup(View Control, List<FormOptionItem> Items);
}

/// <summary>The text of an option in a picker: the label is what shows, the value is what the form stores.</summary>
internal sealed class FormChoice(string? value, string label)
{
    public string? Value { get; } = value;

    public string Label { get; } = label;

    public override string ToString() => Label;
}

/// <summary>
/// A choice among options in a list: the native picker. Options that are grouped or disabled need the menu of Klyroo.UI.Maui instead, which draws the group headings
/// and the disabled rows, so the field uses it for them.
/// </summary>
internal sealed class FormSelectControl : FormControl
{
    private readonly KlySelect? _picker;
    private readonly KlySelectMenu? _menu;
    private readonly FormChoice _empty;
    private IReadOnlyList<FormOptionItem>? _options;
    private List<FormChoice> _choices = [];

    public FormSelectControl(FormSlot slot)
        : base(slot)
    {
        var placeholder = slot.Field.Placeholder ?? FormTexts.Current.SelectPlaceholder;
        _empty = new FormChoice(null, placeholder);
        var options = slot.Engine.GetFieldOptions(slot.Field);
        if (options.Any(o => !string.IsNullOrEmpty(o.Group) || o.Disabled))
        {
            _menu = new KlySelectMenu { Placeholder = placeholder };
            _menu.ValueChanged += (_, e) =>
            {
                if (Busy) return;
                var chosen = e.NewValue is { Count: > 0 } values ? values[0]?.ToString() : null;
                Slot.Commit(string.IsNullOrEmpty(chosen) ? null : chosen);
            };
            Init(_menu);
        }
        else
        {
            _picker = new KlySelect { Placeholder = placeholder };
            _picker.SelectedItemChanged += (_, _) =>
            {
                if (Busy) return;
                var chosen = (_picker.SelectedItem as FormChoice)?.Value;
                Slot.Commit(string.IsNullOrEmpty(chosen) ? null : chosen);
            };
            Init(_picker);
            HookBlur(_picker);
        }
    }

    public KlySelect? Picker => _picker;

    public KlySelectMenu? Menu => _menu;

    protected override bool HasValue => !string.IsNullOrEmpty(Slot.Value?.ToString());

    protected override void Show()
    {
        var options = Slot.Engine.GetFieldOptions(Slot.Field);
        var value = Slot.Value?.ToString();
        if (_options is null || !options.SequenceEqual(_options))
        {
            _options = options;
            if (_menu is not null)
            {
                _menu.Items = options.Select(o => new KlySelectItem(o.Value, o.Label) { Disabled = o.Disabled, Group = string.IsNullOrEmpty(o.Group) ? null : o.Group, Icon = o.Icon }).ToList();
            }
            else
            {
                _choices = [_empty, .. options.Select(o => new FormChoice(o.Value, o.Label))];
                _picker!.ItemsSource = _choices;
            }
        }
        if (_menu is not null)
        {
            var current = _menu.Value is { Count: > 0 } values ? values[0]?.ToString() : null;
            if (current != (string.IsNullOrEmpty(value) ? null : value)) _menu.Value = string.IsNullOrEmpty(value) ? [] : [value];
            return;
        }
        var shown = _picker!.SelectedItem as FormChoice;
        var shownValue = string.IsNullOrEmpty(shown?.Value) ? null : shown.Value;
        var wanted = string.IsNullOrEmpty(value) ? null : value;
        if (shownValue != wanted) _picker.SelectedItem = wanted is null ? null : _choices.FirstOrDefault(c => c.Value == wanted);
    }
}

/// <summary>A text field that suggests the labels of the options. The value is the option whose label matches the text, or the text itself.</summary>
internal sealed class FormAutoCompleteControl : FormControl
{
    private readonly KlyInputMenu _menu = new() { Strict = false };
    private IReadOnlyList<FormOptionItem> _options = [];

    public FormAutoCompleteControl(FormSlot slot)
        : base(slot)
    {
        _menu.Placeholder = slot.Field.Placeholder;
        _menu.ValueChanged += (_, e) =>
        {
            if (Busy) return;
            Slot.Commit(Resolve(_options, e.NewValue?.ToString()));
        };
        Init(_menu);
        HookBlur(_menu);
    }

    protected override object? Cleared => null;

    protected override bool HasValue => !string.IsNullOrEmpty(Slot.Value?.ToString());

    public KlyInputMenu Menu => _menu;

    protected override void Show()
    {
        var options = Slot.Engine.GetFieldOptions(Slot.Field);
        if (!options.SequenceEqual(_options))
        {
            _options = options;
            _menu.Items = options.Where(o => !o.Disabled).Select(o => new KlySelectItem(o.Label, o.Label)).ToList();
        }
        var value = Slot.Value?.ToString();
        var shown = string.IsNullOrEmpty(value) ? null : _options.FirstOrDefault(o => o.Value == value)?.Label ?? value;
        // What the user typed that already stands for this value stays on screen.
        if (!string.Equals(Resolve(_options, _menu.Value?.ToString()), string.IsNullOrEmpty(value) ? null : value, StringComparison.Ordinal)) _menu.Value = shown;
    }

    /// <summary>The value of the option whose label is the text (ignoring case) or whose value is the text; the text itself otherwise.</summary>
    internal static string? Resolve(IReadOnlyList<FormOptionItem> options, string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = options.FirstOrDefault(o => !o.Disabled && string.Equals(o.Label, text, StringComparison.OrdinalIgnoreCase))
            ?? options.FirstOrDefault(o => !o.Disabled && string.Equals(o.Value, text, StringComparison.Ordinal));
        return match?.Value ?? text;
    }
}
