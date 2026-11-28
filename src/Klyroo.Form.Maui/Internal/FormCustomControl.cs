namespace Klyroo.Form.Maui;

/// <summary>
/// The control of a field that an app draws itself (a <see cref="KlyFieldTemplate{TModel, TValue}"/>, a renderer of <see cref="FormCustomFields"/> or the <c>FieldTemplates</c> of a dynamic form).
/// The template runs once, and again when the form changes the field outside the control (a reset, another field's watcher) or its state changes (disabled, required, the error shown),
/// since the context it receives is a snapshot. A change the control makes through its own context does not rebuild it, so typing in it is never interrupted.
/// </summary>
internal sealed class FormCustomControl : FormControl
{
    private readonly Func<FormSlot, View?> _build;
    private readonly ContentView _host = new();
    private object? _lastValue;
    private string? _lastError;
    private bool _lastDisabled;
    private bool _lastRequired;
    private bool _built;

    public FormCustomControl(FormSlot slot, Func<FormSlot, View?> build)
        : base(slot)
    {
        _build = build;
        Init(_host);
    }

    protected override bool CanClear => false;

    /// <summary>The view the template made, for the tests.</summary>
    public View? Content => _host.Content;

    /// <summary>How many times the template ran, for the tests.</summary>
    public int Builds { get; private set; }

    protected override void Show()
    {
        var value = Slot.Value;
        var changed = !_built
            || (!Slot.IsWriting && !SameValue(value, _lastValue))
            || Slot.Disabled != _lastDisabled
            || Slot.Required != _lastRequired
            || (!Slot.IsWriting && Slot.Error != _lastError);
        _lastValue = value;
        _lastError = Slot.Error;
        _lastDisabled = Slot.Disabled;
        _lastRequired = Slot.Required;
        if (!changed) return;
        _built = true;
        Builds++;
        _host.Content = _build(Slot);
    }

    protected override void ApplyEnabled(bool enabled)
    {
        // The template decides what disabled looks like: it receives it in its context.
    }

    public override bool Focus() => _host.Content is { } content && FormUi.Descendants(content).OfType<View>().Any(v => v is Entry or Editor or Picker or DatePicker or TimePicker or SearchBar or Slider && v.IsEnabled && v.Focus());

    private static bool SameValue(object? a, object? b)
    {
        if (Equals(a, b)) return true;
        if (a is string || b is string) return false;
        return a is System.Collections.IEnumerable x && b is System.Collections.IEnumerable y && x.Cast<object?>().SequenceEqual(y.Cast<object?>());
    }
}

/// <summary>A notice in place of a custom control that has no renderer.</summary>
internal static class FormMissingRenderer
{
    public static View Notice(FieldNode field)
    {
        var label = FormUi.NewLabel(12, KlyTheme.Resolve(KlyColor.Warning).Text);
        label.Text = $"No renderer is registered for the custom field \"{field.CustomKey ?? field.Key}\"{(field.ValueType is { } type ? $" of {type.Name}" : "")}.";
        return label;
    }
}
