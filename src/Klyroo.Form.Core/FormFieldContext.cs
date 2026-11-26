namespace Klyroo.Form;

/// <summary>
/// Everything a control needs to edit one field: the value and how to change it, the error, the state flags and the ids that tie the control to its label, description, help and error.
/// <c>KlyFormEngine</c> and <c>KlyFormPage</c> build one for each control they render, and pass it to the renderer of a <see cref="FormFieldKind.Custom"/> field. The same type
/// serves a field and a cell of a repeatable list. A context is a snapshot of one render: build a new one (the components do) after the form changes.
/// </summary>
/// <example>
/// <code>
/// // A custom control: an input wired to the field, labelled and described like the built-in ones.
/// RenderFragment&lt;FormFieldContext&gt; renderer = ctx => builder =>
/// {
///     builder.OpenElement(0, "input");
///     builder.AddAttribute(1, "id", ctx.Id);
///     builder.AddAttribute(2, "value", ctx.Value?.ToString());
///     builder.AddAttribute(3, "aria-describedby", ctx.DescribedBy);
///     builder.AddAttribute(4, "aria-invalid", ctx.Error is null ? null : "true");
///     builder.AddAttribute(5, "oninput", EventCallback.Factory.Create&lt;ChangeEventArgs&gt;(this, e => ctx.Set(e.Value)));
///     builder.AddAttribute(6, "onblur", EventCallback.Factory.Create(this, ctx.Blur));
///     builder.CloseElement();
/// };
/// </code>
/// </example>
public sealed class FormFieldContext
{
    private readonly Func<object?> _current;
    private readonly Action<object?> _set;
    private readonly Action<object?> _commit;
    private readonly Action _blur;

    /// <summary>Creates the context of a field of <paramref name="form"/>, with the state, error and ids the built-in controls use. <see cref="Set"/>, <see cref="Commit"/> and <see cref="Blur"/> act on the field through the controller.</summary>
    /// <param name="form">The controller that holds the value.</param>
    /// <param name="field">The field.</param>
    /// <param name="id">The id of the control. The ids of the description, help and error elements are this id followed by <c>-description</c>, <c>-help</c> and <c>-error</c>.</param>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public FormFieldContext(FormController form, FormFieldSchema field, string id)
        : this(
            form,
            field,
            id,
            form?.GetValue(field.Key),
            () => form!.GetValue(field.Key),
            v => form!.SetValue(field.Key, v),
            v => form!.SetValue(field.Key, v, markTouched: true),
            () => form!.MarkTouched(field.Key),
            form?.GetError(field.Key),
            form?.IsFieldDisabled(field) ?? false,
            form?.IsFieldRequired(field) ?? false,
            form?.IsValidating(field.Key) ?? false,
            null,
            DescribedByFor(field, id, form?.GetError(field.Key) is not null),
            "form-control",
            isCell: false)
    {
    }

    internal FormFieldContext(
        FormController form,
        FormFieldSchema field,
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
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(id);
        Form = form;
        Field = field;
        Id = id;
        Value = value;
        _current = current;
        _set = set;
        _commit = commit;
        _blur = blur;
        Error = error;
        Disabled = disabled;
        Required = required;
        Validating = validating;
        LabelledBy = labelledBy;
        DescribedBy = describedBy;
        Slot = slot;
        IsCell = isCell;
    }

    /// <summary>The controller that holds the value.</summary>
    public FormController Form { get; }

    /// <summary>The field being edited. For a cell of a repeatable list, the child field of the column.</summary>
    public FormFieldSchema Field { get; }

    /// <summary>The <c>id</c> the control must use, so that the label points at it.</summary>
    public string Id { get; }

    /// <summary>The value when this context was built. Use <see cref="Current"/> for the live value inside an event handler.</summary>
    public object? Value { get; }

    /// <summary>The error to show, or <c>null</c>. The error text itself is rendered by the component around the control.</summary>
    public string? Error { get; }

    /// <summary><c>true</c> when the field is disabled (<see cref="FormFieldSchema.Disabled"/> or <see cref="FormFieldSchema.DisabledWhen"/>, or its list is). Render the control disabled.</summary>
    public bool Disabled { get; }

    /// <summary><c>true</c> when the field is required (<see cref="FormFieldSchema.Required"/> or <see cref="FormFieldSchema.RequiredWhen"/>). Set <c>aria-required</c> on the control.</summary>
    public bool Required { get; }

    /// <summary><c>true</c> while an asynchronous rule of the field runs (see <see cref="FormController.IsValidating"/>).</summary>
    public bool Validating { get; }

    /// <summary>The ids of the elements that label a control made of several inputs (<c>aria-labelledby</c>), or <c>null</c> when a <c>label</c> element points at <see cref="Id"/>.</summary>
    public string? LabelledBy { get; }

    /// <summary>The ids of the description, help and error elements to put in <c>aria-describedby</c>, space separated, or <c>null</c>.</summary>
    public string? DescribedBy { get; }

    /// <summary>The id of the error element. It exists only while <see cref="Error"/> is not <c>null</c>.</summary>
    public string ErrorId => Id + "-error";

    /// <summary>The <c>data-slot</c> the built-in controls carry (<c>form-control</c>, or <c>form-array-input</c> in a cell of a repeatable list).</summary>
    public string Slot { get; }

    /// <summary><c>true</c> when the control is a cell of a repeatable list.</summary>
    public bool IsCell { get; }

    /// <summary>Gets the live value, which may be newer than <see cref="Value"/>. In a cell, the value of the cell.</summary>
    /// <returns>The current value.</returns>
    public object? Current() => _current();

    /// <summary>Gets the live value converted to <typeparamref name="T"/> like <see cref="FormController.GetValue{T}"/> does.</summary>
    /// <typeparam name="T">The wanted type.</typeparam>
    /// <returns>The converted value, or <c>default</c>.</returns>
    public T? Current<T>() => FormValueConverter.TryConvert<T>(_current(), out var value) ? value : default;

    /// <summary>Changes the value while the user is editing, for example on every keystroke. The field is validated as <see cref="FormController.SetValue"/> does: only once touched, and per its trigger.</summary>
    /// <param name="value">The new value.</param>
    public void Set(object? value) => _set(value);

    /// <summary>Changes the value and marks the field touched, for a change that is complete in one gesture: a click, a selection, a removal. The error shows at once.</summary>
    /// <param name="value">The new value.</param>
    public void Commit(object? value) => _commit(value);

    /// <summary>Tells the form the control lost focus: the field is marked touched and validated, which is what makes its error show.</summary>
    public void Blur() => _blur();

    // Ids of the description, help and error elements that exist, in the order the built-in controls list them.
    internal static string? DescribedByFor(FormFieldSchema field, string id, bool hasError)
    {
        var ids = new List<string>(3);
        if (!string.IsNullOrEmpty(field.Description)) ids.Add(id + "-description");
        if (!string.IsNullOrEmpty(field.Help)) ids.Add(id + "-help");
        if (hasError) ids.Add(id + "-error");
        return ids.Count == 0 ? null : string.Join(' ', ids);
    }
}
