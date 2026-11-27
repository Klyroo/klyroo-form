using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// Shows one field of a <see cref="DynamicFormController"/> with the control of its <see cref="FormFieldKind"/>, its label, description, hint, help, error and per-field reset, the way
/// <see cref="KlyDynamicFormEngine"/> and <see cref="KlyDynamicFormPage"/> show their fields, for a layout of your own. Containers, tabs and repeatable lists show their children too. Place it in a
/// layout that gives it the full width: the view takes no column of its own. It shows nothing while <see cref="DynamicFormController.ShouldRenderField"/> is <c>false</c>, and it follows the controller by itself.
/// </summary>
public sealed class KlyDynamicFieldView : ContentView, IDisposable
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(DynamicFormController), typeof(KlyDynamicFieldView), null, propertyChanged: (b, _, _) => ((KlyDynamicFieldView)b).Reconfigure());
    /// <summary>Identifies <see cref="Field"/>.</summary>
    public static readonly BindableProperty FieldProperty = BindableProperty.Create(nameof(Field), typeof(DynamicFieldSchema), typeof(KlyDynamicFieldView), null, propertyChanged: (b, _, _) => ((KlyDynamicFieldView)b).Reconfigure());
    /// <summary>Identifies <see cref="Key"/>.</summary>
    public static readonly BindableProperty KeyProperty = BindableProperty.Create(nameof(Key), typeof(string), typeof(KlyDynamicFieldView), null, propertyChanged: (b, _, _) => ((KlyDynamicFieldView)b).Reconfigure());
    /// <summary>Identifies <see cref="FieldTemplates"/>.</summary>
    public static readonly BindableProperty FieldTemplatesProperty = BindableProperty.Create(nameof(FieldTemplates), typeof(IReadOnlyDictionary<string, Func<DynamicControlContext, View>>), typeof(KlyDynamicFieldView), null, propertyChanged: (b, _, _) => ((KlyDynamicFieldView)b).Reconfigure());

    private readonly FieldViewHost _host;

    /// <summary>Creates a field view.</summary>
    public KlyDynamicFieldView()
    {
        _host = new FieldViewHost(this);
        Loaded += (_, _) => _host.Resume();
        Unloaded += (_, _) => _host.Suspend();
    }

    /// <summary>The controller that holds the value of the field.</summary>
    public DynamicFormController? Form { get => (DynamicFormController?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The field to show, usually one of <see cref="DynamicFormController.CurrentVisibleTopFields"/>. Containers show their children too.</summary>
    public DynamicFieldSchema? Field { get => (DynamicFieldSchema?)GetValue(FieldProperty); set => SetValue(FieldProperty, value); }

    /// <summary>The key of the field to show, as an alternative to <see cref="Field"/>: the field of <see cref="Form"/> with this key. <see cref="Field"/> wins when both are set. Nothing shows when the form has no field with this key.</summary>
    public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

    /// <summary>Builders of the <see cref="FormFieldKind.Custom"/> fields by <see cref="DynamicFieldSchema.CustomKey"/>. They win over the ones registered with <see cref="FormCustomFields"/>, and reach the fields inside containers, tabs and lists.</summary>
    public IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? FieldTemplates { get => (IReadOnlyDictionary<string, Func<DynamicControlContext, View>>?)GetValue(FieldTemplatesProperty); set => SetValue(FieldTemplatesProperty, value); }

    private DynamicFieldSchema? Target => Field ?? (Key is null ? null : Form?.AllFields.FirstOrDefault(f => f.Key == Key));

    /// <summary>The view of the field, or <c>null</c> while there is nothing to show.</summary>
    internal FormNodeView? NodeView => _host.NodeView;

    /// <summary>Stops listening to the controller.</summary>
    public void Dispose() => _host.Dispose();

    private void Reconfigure()
    {
        if (_host is null) return;
        if (Form is { } form && Target is { } target) _host.Bind(form.Engine, form.NodeOf(target), 480, FieldTemplates);
        else _host.Bind(null, null);
    }
}
