namespace Klyroo.Form;

// Everything a control needs to edit one field or one cell of a list: the value and how to change it, the error, the state flags and the ids that tie the
// control to its label, description, help and error. The renderer builds one per control, for typed and dynamic forms alike; the contexts of the public custom
// renderers (FormControlContext of a TValue, DynamicControlContext) wrap it.
internal sealed class ControlBinding(
    FormEngine engine,
    FieldNode field,
    string id,
    object? value,
    Func<object?> current,
    Action<object?> set,
    Action<object?> commit,
    Action blur,
    string? error,
    bool disabled,
    bool required,
    bool validating,
    string? labelledBy,
    string? describedBy,
    string slot,
    bool isCell)
{
    public FormEngine Engine { get; } = engine;
    public FieldNode Field { get; } = field;
    public string Id { get; } = id;
    public object? Value { get; } = value;
    public string? Error { get; } = error;
    public bool Disabled { get; } = disabled;
    public bool Required { get; } = required;
    public bool Validating { get; } = validating;
    public string? LabelledBy { get; } = labelledBy;
    public string? DescribedBy { get; } = describedBy;
    public string Slot { get; } = slot;
    public bool IsCell { get; } = isCell;

    public object? Current() => current();

    public void Set(object? value) => set(value);

    public void Commit(object? value) => commit(value);

    public void Blur() => blur();
}
