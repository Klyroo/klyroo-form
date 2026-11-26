namespace Klyroo.Form;

/// <summary>
/// What a custom control of a typed form needs besides its value: the error, the state flags and the ids that tie the control to its label, description, help and
/// error, and the string boundary for raw inputs (<see cref="Text"/> and <see cref="SetText"/>). The value itself, typed as the member of the model, is on
/// <see cref="FormControlContext{TValue}"/>, which is what a renderer receives. A context is a snapshot of one render: the components build a new one after the form changes.
/// </summary>
public abstract class FormControlContext
{
    private protected FormControlContext(ControlBinding binding) => Binding = binding;

    private protected ControlBinding Binding { get; }

    /// <summary>The <c>id</c> the control must use, so that the label points at it. The ids of the description, help and error elements are this id followed by <c>-description</c>, <c>-help</c> and <c>-error</c>.</summary>
    public string Id => Binding.Id;

    /// <summary>The error to show, or <c>null</c>. The error text itself is rendered by the component around the control.</summary>
    public string? Error => Binding.Error;

    /// <summary><c>true</c> when the field is disabled, which includes every field while a submission runs. Render the control disabled.</summary>
    public bool Disabled => Binding.Disabled;

    /// <summary><c>true</c> when the field is required. Set <c>aria-required</c> on the control.</summary>
    public bool Required => Binding.Required;

    /// <summary><c>true</c> while an asynchronous rule of the field runs.</summary>
    public bool Validating => Binding.Validating;

    /// <summary>The ids of the elements that label a control made of several inputs (<c>aria-labelledby</c>), or <c>null</c> when a <c>label</c> element points at <see cref="Id"/>.</summary>
    public string? LabelledBy => Binding.LabelledBy;

    /// <summary>The ids of the description, help and error elements to put in <c>aria-describedby</c>, space separated, or <c>null</c>.</summary>
    public string? DescribedBy => Binding.DescribedBy;

    /// <summary>The id of the error element. It exists only while <see cref="Error"/> is not <c>null</c>.</summary>
    public string ErrorId => Binding.Id + "-error";

    /// <summary>Tells the form the control lost focus: the field is marked touched and validated, which is what makes its error show.</summary>
    public void Blur() => Binding.Blur();

    /// <summary>The current value as the text an input shows: the text the user typed when it is not a value yet (<c>abc</c> in a number field), else the value in invariant form (dates as <c>yyyy-MM-dd</c>, lists comma separated). <c>null</c> when the field is empty.</summary>
    public string? Text => Binding.Current() switch
    {
        null => null,
        string text => text,
        IEnumerable<string> list => string.Join(',', list),
        var other => FormValueConverter.ToInvariantText(other),
    };

    /// <summary>Changes the value from the text of an input, while the user is editing. Blank text empties the field. Text that is a value of the type becomes that value; text that is not stays as it is, so the field shows its error instead of a zero.</summary>
    /// <param name="text">The text of the input.</param>
    public abstract void SetText(string? text);
}

/// <summary>
/// Everything a custom control needs to edit one field of a typed form: the value as the type of the member, and how to change it. A renderer is a
/// <c>RenderFragment&lt;FormControlContext&lt;TValue&gt;&gt;</c>, given in a <c>KlyFieldTemplate</c> for one field or registered for every form with
/// <c>KlyrooFormOptions.CustomField&lt;TValue&gt;</c>. A control that keeps the value as text (a raw HTML input) reads <see cref="FormControlContext.Text"/> and calls <see cref="FormControlContext.SetText"/>.
/// </summary>
/// <example>
/// <code>
/// // A number input wired to an int field, labelled and described like the built-in ones.
/// RenderFragment&lt;FormControlContext&lt;int&gt;&gt; renderer = ctx => builder =>
/// {
///     builder.OpenElement(0, "input");
///     builder.AddAttribute(1, "id", ctx.Id);
///     builder.AddAttribute(2, "value", ctx.Text);
///     builder.AddAttribute(3, "aria-describedby", ctx.DescribedBy);
///     builder.AddAttribute(4, "oninput", EventCallback.Factory.Create&lt;ChangeEventArgs&gt;(this, e => ctx.SetText(e.Value?.ToString())));
///     builder.AddAttribute(5, "onblur", EventCallback.Factory.Create(this, ctx.Blur));
///     builder.CloseElement();
/// };
/// </code>
/// </example>
/// <typeparam name="TValue">The type of the value of the field.</typeparam>
public sealed class FormControlContext<TValue> : FormControlContext
{
    private readonly FormCodec<TValue> _codec;

    internal FormControlContext(ControlBinding binding, FormCodec<TValue> codec)
        : base(binding)
    {
        _codec = codec;
        Value = codec.FromControlOrDefault(binding.Value);
    }

    /// <summary>The value when this context was built, or the default of the type while the field is empty or its text is not a value yet (<c>0</c> for an <c>int</c>, <c>null</c> for a string). Use <see cref="Current"/> for the live value inside an event handler.</summary>
    public TValue? Value { get; }

    /// <summary>Gets the live value, which may be newer than <see cref="Value"/>.</summary>
    /// <returns>The current value, or the default of the type while the field is empty or its text is not a value yet.</returns>
    public TValue? Current() => _codec.FromControlOrDefault(Binding.Current());

    /// <summary>Changes the value while the user is editing, for example on every keystroke. The field is validated only once touched, and per its trigger.</summary>
    /// <param name="value">The new value.</param>
    public void Set(TValue value) => Binding.Set(_codec.ToControl(value));

    /// <summary>Changes the value and marks the field touched, for a change that is complete in one gesture: a click, a selection, a removal. The error shows at once.</summary>
    /// <param name="value">The new value.</param>
    public void Commit(TValue value) => Binding.Commit(_codec.ToControl(value));

    /// <inheritdoc/>
    public override void SetText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Binding.Set(null);
        }
        else if (Binding.Field.Kind is FormFieldKind.Number or FormFieldKind.Slider)
        {
            // A double, or a decimal when the digits do not fit a double; text that is no number stays, and the field reports it.
            Binding.Set(FormValueConverter.NumberFromInput(text) ?? text);
        }
        else if (_codec.TryParse(text, out var value) && value is not null)
        {
            Binding.Set(_codec.ToControl(value));
        }
        else
        {
            Binding.Set(text);
        }
    }
}
