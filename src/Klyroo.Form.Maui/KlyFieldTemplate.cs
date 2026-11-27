using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form.Maui;

/// <summary>The part of <see cref="KlyFieldTemplate{TModel, TValue}"/> that does not depend on the types, so a form view can hold templates of any field.</summary>
public abstract class KlyFieldTemplate : IDisposable
{
    internal KlyFieldTemplate()
    {
    }

    /// <summary>The key of the field the template draws the control of.</summary>
    public abstract string FieldKey { get; }

    /// <summary>Attaches the template to the controller of the form view it was added to, or detaches it with <c>null</c>.</summary>
    internal abstract void Attach(object? controller);

    /// <summary>Detaches the template from its form: the field gets the control of its kind again.</summary>
    public abstract void Dispose();
}

/// <summary>
/// Draws the control of one field of a typed form with a view of your own. Add it to the <see cref="KlyFormEngine{TModel}.Templates"/> (or those of <see cref="KlyFormPage{TModel}"/>), or set its <see cref="Form"/>
/// for a layout built with <see cref="KlyFormFieldView{TModel}"/>, and name the field with its handle. The view is built from the <see cref="FormControlContext{TValue}"/> of the field
/// (<c>Value</c>, <c>Set</c>, <c>Commit</c>, <c>Id</c>, <c>Error</c>, <c>Disabled</c>, <c>Blur</c>): the form view still draws the label, description, help and error around your control.
/// The template replaces the control of any field, so a field you declared with <c>AsCustom()</c> needs one, and a built-in kind can be swapped for your own.
/// </summary>
/// <remarks>
/// The context is a snapshot. The view is built once, and built again when the form changes the field outside of your control (a reset, a watcher of another field) or when the field becomes
/// disabled, required, or gets another error; a change your control makes through its own context does not rebuild it, so typing in it is never interrupted. Read the live value with <c>Current()</c>.
/// </remarks>
/// <example>
/// <code>
/// var rating = new KlyFieldTemplate&lt;Signup, int&gt;(schema.Rating, ctx =>
/// {
///     var slider = new Slider { Minimum = 1, Maximum = 5, Value = ctx.Value, IsEnabled = !ctx.Disabled };
///     slider.ValueChanged += (_, e) => ctx.Commit((int)Math.Round(e.NewValue));
///     return slider;
/// });
/// engine.Templates.Add(rating);
/// </code>
/// </example>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="TValue">The type of the value of the field.</typeparam>
public sealed class KlyFieldTemplate<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, TValue> : KlyFieldTemplate
{
    private readonly FormField<TModel, TValue> _field;
    private Func<FormControlContext<TValue>, View> _content;
    private FormController<TModel>? _form;
    private FormTemplateRegistry? _registry;

    /// <summary>Creates a template.</summary>
    /// <param name="field">The field whose control this template draws: the handle the schema gave. <typeparamref name="TModel"/> and <typeparamref name="TValue"/> are inferred from it.</param>
    /// <param name="content">Builds the control from the context of the field: its value typed as the member of the model, how to change it, and the id and state the control needs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> or <paramref name="content"/> is <c>null</c>.</exception>
    public KlyFieldTemplate(FormField<TModel, TValue> field, Func<FormControlContext<TValue>, View> content)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(content);
        _field = field;
        _content = content;
    }

    /// <summary>The field whose control this template draws.</summary>
    public FormField<TModel, TValue> Field => _field;

    /// <inheritdoc/>
    public override string FieldKey => _field.Key;

    /// <summary>Builds the control from the context of the field. Setting it makes the form show the new control.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public Func<FormControlContext<TValue>, View> Content
    {
        get => _content;
        set
        {
            _content = value ?? throw new ArgumentNullException(nameof(value));
            Register();
        }
    }

    /// <summary>The controller of the form. A template added to a form view takes the controller of that view; set it yourself when the field is drawn by a <see cref="KlyFormFieldView{TModel}"/> of a layout of your own.</summary>
    public FormController<TModel>? Form
    {
        get => _form;
        set
        {
            if (ReferenceEquals(_form, value)) return;
            Unregister();
            _form = value;
            Register();
        }
    }

    /// <inheritdoc/>
    internal override void Attach(object? controller)
    {
        if (controller is not null && controller is not FormController<TModel>)
        {
            throw new InvalidOperationException($"A template of a field of {typeof(TModel).Name} cannot be added to a form of another model.");
        }
        Form = (FormController<TModel>?)controller;
    }

    /// <inheritdoc/>
    public override void Dispose() => Form = null;

    private void Register()
    {
        if (_form is null) return;
        _registry = FormTemplateRegistry.For(_form.Engine);
        // The view of a field reads the content when it builds the control, so a template whose content changed builds the new one.
        _registry.Set(_field.Key, this, typeof(TValue), context => _content((FormControlContext<TValue>)context));
    }

    private void Unregister()
    {
        _registry?.Remove(_field.Key, this);
        _registry = null;
    }
}
