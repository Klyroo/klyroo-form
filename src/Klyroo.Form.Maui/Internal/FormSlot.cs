namespace Klyroo.Form.Maui;

/// <summary>
/// One field or one cell of a repeatable list, as a control needs it: the live value and the way to change it, the error and the state flags. It lives as long as the view of the field,
/// where the web renderer builds a new binding on every render; <see cref="Binding"/> makes that snapshot for the custom renderers.
/// </summary>
internal sealed class FormSlot
{
    private readonly Func<object?> _value;
    private readonly Action<object?> _set;
    private readonly Action<object?> _commit;
    private readonly Action _blur;
    private readonly Func<string?> _error;
    private readonly Func<bool> _disabled;
    private readonly Func<bool> _required;
    private readonly Func<bool> _validating;
    private readonly string? _labelledBy;
    private int _writing;

    public FormSlot(
        FormEngine engine,
        FieldNode field,
        string id,
        bool isCell,
        Func<object?> value,
        Action<object?> set,
        Action<object?> commit,
        Action blur,
        Func<string?> error,
        Func<bool> disabled,
        Func<bool> required,
        Func<bool> validating)
    {
        Engine = engine;
        Field = field;
        Id = id;
        IsCell = isCell;
        _value = value;
        _set = set;
        _commit = commit;
        _blur = blur;
        _error = error;
        _disabled = disabled;
        _required = required;
        _validating = validating;
        _labelledBy = null;
    }

    public FormEngine Engine { get; }

    public FieldNode Field { get; }

    public string Id { get; }

    public bool IsCell { get; }

    /// <summary>Called when the user presses the return key in a single-line input: the form submits, or goes to the next step.</summary>
    public Action? Enter { get; set; }

    /// <summary>The live value of the field.</summary>
    public object? Value => _value();

    public string? Error => _error();

    public bool Disabled => _disabled();

    public bool Required => _required();

    public bool Validating => _validating();

    /// <summary>True while the control itself is writing into the form: the view does not push that change back into the control.</summary>
    public bool IsWriting => _writing > 0;

    /// <summary>Changes the value while the user edits.</summary>
    public void Set(object? value) => Write(() => _set(value));

    /// <summary>Changes the value and marks the field touched.</summary>
    public void Commit(object? value) => Write(() => _commit(value));

    /// <summary>Tells the form the control lost focus.</summary>
    public void Blur() => Write(_blur);

    /// <summary>The snapshot a custom renderer works with.</summary>
    public ControlBinding Binding()
    {
        var error = Error;
        var describedBy = error is null ? null : Id + "-error";
        return new ControlBinding(Engine, Field, Id, Value, _value, Set, Commit, Blur, error, Disabled, Required, Validating, _labelledBy, describedBy, IsCell ? "form-array-input" : "form-control", IsCell);
    }

    private void Write(Action action)
    {
        _writing++;
        try
        {
            action();
        }
        finally
        {
            _writing--;
        }
    }
}
