using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form.Maui;

/// <summary>
/// Shows one field of a typed form with the control of its kind, its label, description, hint, help, error and per-field reset, the way <see cref="KlyFormEngine{TModel}"/> and
/// <see cref="KlyFormPage{TModel}"/> show their fields, for a layout of your own. Containers, tabs and repeatable lists show their children too. Place it in a layout that gives it the full width:
/// the view takes no column of its own. It shows nothing while the field is not in effect (<see cref="FormController{TModel}.IsVisible"/>), and it follows the controller by itself.
/// </summary>
/// <example>
/// <code>
/// layout.Add(new KlyFormFieldView&lt;Signup&gt; { Field = schema.Email, Form = controller });
/// </code>
/// </example>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class KlyFormFieldView<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel> : ContentView, IDisposable
{
    /// <summary>Identifies <see cref="Field"/>.</summary>
    public static readonly BindableProperty FieldProperty = BindableProperty.Create(nameof(Field), typeof(FormItem<TModel>), typeof(KlyFormFieldView<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormFieldView<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(FormController<TModel>), typeof(KlyFormFieldView<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormFieldView<TModel>)b).Reconfigure());

    private readonly FieldViewHost _host;

    /// <summary>Creates a field view.</summary>
    public KlyFormFieldView()
    {
        _host = new FieldViewHost(this);
        ParentChanged += (_, _) => Reconfigure();
        Loaded += (_, _) => Rebind();
        Unloaded += (_, _) => _host.Suspend();
    }

    /// <summary>The field or layout element to show: the handle the schema gave. <typeparamref name="TModel"/> is inferred from it.</summary>
    public FormItem<TModel>? Field { get => (FormItem<TModel>?)GetValue(FieldProperty); set => SetValue(FieldProperty, value); }

    /// <summary>The controller that holds the value of the field. When it is not set, the one of the enclosing <see cref="KlyFormEngine{TModel}"/> or <see cref="KlyFormPage{TModel}"/> is used.</summary>
    public FormController<TModel>? Form { get => (FormController<TModel>?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The controller in use: <see cref="Form"/>, or the one of the enclosing form view.</summary>
    public FormController<TModel>? EffectiveController => Form ?? FormUi.FindAncestor<IFormControllerHost<TModel>>(this)?.EffectiveController;

    /// <summary>The view of the field, or <c>null</c> while there is nothing to show.</summary>
    internal FormNodeView? NodeView => _host.NodeView;

    /// <summary>Stops listening to the controller.</summary>
    public void Dispose() => _host.Dispose();

    /// <summary>Looks for the controller again, as when the view enters a page: an enclosing form view can be attached after the view was made. Also resumes listening to it.</summary>
    internal void Rebind()
    {
        Reconfigure();
        _host.Resume();
    }

    private void Reconfigure()
    {
        if (_host is null) return;
        if (EffectiveController is { } controller && Field is { } field) _host.Bind(controller.Engine, field.Node);
        else _host.Bind(null, null);
    }
}

/// <summary>What the views of a single field do: the context and the view of the node, kept as long as the engine and the node stay the same, refreshed on every change of the engine.</summary>
internal sealed class FieldViewHost : IDisposable
{
    private readonly ContentView _owner;
    private FormEngine? _engine;
    private FieldNode? _node;
    private IReadOnlyDictionary<string, Func<Klyroo.Form.Dynamic.DynamicControlContext, View>>? _templates;
    private FormContext? _context;
    private bool _listening;
    private bool _pending;

    public FieldViewHost(ContentView owner) => _owner = owner;

    public FormNodeView? NodeView { get; private set; }

    public void Bind(FormEngine? engine, FieldNode? node, double compactWidth = 480, IReadOnlyDictionary<string, Func<Klyroo.Form.Dynamic.DynamicControlContext, View>>? templates = null)
    {
        if (ReferenceEquals(engine, _engine) && ReferenceEquals(node, _node) && ReferenceEquals(templates, _templates)) return;
        Unbind();
        _engine = engine;
        _node = node;
        _templates = templates;
        if (engine is null || node is null)
        {
            _owner.Content = null;
            return;
        }
        _context = new FormContext(engine, compactWidth, templates);
        _context.Rebuilt += OnRebuilt;
        NodeView = _context.ViewOf(node);
        _owner.Content = NodeView;
        engine.Changed += OnChanged;
        _listening = true;
        Refresh();
    }

    public void Resume()
    {
        if (_engine is null || _listening) return;
        _engine.Changed += OnChanged;
        _listening = true;
        Refresh();
    }

    public void Suspend()
    {
        if (_engine is not null && _listening) _engine.Changed -= OnChanged;
        _listening = false;
    }

    public void Dispose()
    {
        Unbind();
        _owner.Content = null;
    }

    private void Refresh()
    {
        _pending = false;
        if (_engine is null || _node is null) return;
        if (_context!.ViewOf(_node) is { } view && !ReferenceEquals(view, NodeView))
        {
            NodeView = view;
            _owner.Content = view;
        }
        var inEffect = _engine.ShouldRenderField(_node);
        _owner.IsVisible = inEffect;
        if (inEffect) NodeView?.Refresh();
    }

    private void Unbind()
    {
        Suspend();
        if (_context is not null)
        {
            _context.Rebuilt -= OnRebuilt;
            _context.Detach();
        }
        _context = null;
        NodeView = null;
        _engine = null;
        _node = null;
        _templates = null;
    }

    private void OnChanged()
    {
        if (_pending) return;
        _pending = true;
        FormUi.Post(_owner, Refresh);
    }

    private void OnRebuilt() => FormUi.Post(_owner, Refresh);
}
