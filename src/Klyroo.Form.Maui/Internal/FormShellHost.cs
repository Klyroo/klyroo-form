using System.Diagnostics.CodeAnalysis;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// What the form views do with an engine: build the shell for it, show its state again after every change (once for a burst of changes, on the UI thread), stop listening while the view is
/// out of the page, and let go of it when the view is disposed.
/// </summary>
internal sealed class FormShellHost : IDisposable
{
    private readonly ContentView _owner;
    private readonly Func<FormContext, FormShellBase> _create;
    private FormEngine? _engine;
    private FormContext? _context;
    private double _compactWidth;
    private IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? _templates;
    private bool _listening;
    private bool _pending;

    public FormShellHost(ContentView owner, Func<FormContext, FormShellBase> create)
    {
        _owner = owner;
        _create = create;
    }

    public FormShellBase? Shell { get; private set; }

    public FormContext? Context => _context;

    /// <summary>
    /// Shows an engine, or nothing. The shell is built again when the engine, the renderers of the custom fields or the compact width change; otherwise nothing is built.
    /// Returns true when a shell was built, so the owner gives it its settings.
    /// </summary>
    public bool Bind(FormEngine? engine, double compactWidth, IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? templates)
    {
        if (ReferenceEquals(engine, _engine) && compactWidth == _compactWidth && ReferenceEquals(templates, _templates)) return false;
        Unbind();
        _engine = engine;
        _compactWidth = compactWidth;
        _templates = templates;
        if (engine is null)
        {
            _owner.Content = null;
            return false;
        }
        _context = new FormContext(engine, compactWidth, templates);
        _context.Rebuilt += OnRebuilt;
        Shell = _create(_context);
        _owner.Content = Shell;
        Listen();
        return true;
    }

    /// <summary>Shows the state of the engine in the shell, now when the caller is on the UI thread, at once otherwise.</summary>
    public void Refresh()
    {
        _pending = false;
        Shell?.Refresh();
    }

    /// <summary>Starts listening to the engine again, and shows what changed while the view was away.</summary>
    public void Resume()
    {
        if (_engine is null || _listening) return;
        Listen();
        Refresh();
    }

    private void Listen()
    {
        if (_engine is null || _listening) return;
        _listening = true;
        _engine.Changed += OnChanged;
    }

    /// <summary>Stops listening to the engine, for a view that left the page.</summary>
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

    private void Unbind()
    {
        Suspend();
        if (_context is not null)
        {
            _context.Rebuilt -= OnRebuilt;
            _context.Detach();
        }
        Shell?.Dispose();
        Shell = null;
        _context = null;
        _engine = null;
    }

    private void OnChanged()
    {
        if (_pending) return;
        _pending = true;
        FormUi.Post(_owner, Refresh);
    }

    private void OnRebuilt() => FormUi.Post(_owner, Refresh);
}

/// <summary>
/// What a typed form view does with its <c>Form</c>, <c>Schema</c> and <c>Model</c> properties and with a submit: pick the controller (the given one, or one made from the schema and the model),
/// reload it when another model arrives, and run the submit lifecycle with the save step and the submitted callback.
/// </summary>
internal sealed class FormModelHost<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
    private FormController<TModel>? _owned;
    private TModel? _loaded;

    public FormController<TModel>? Controller { get; private set; }

    /// <summary>A controller the host made goes away with the schema that made it; a new instance of the model reloads it.</summary>
    public void Update(FormController<TModel>? form, FormSchema<TModel>? schema, TModel? model)
    {
        if (form is not null)
        {
            DisposeOwned();
            Controller = form;
            return;
        }
        if (schema is null)
        {
            DisposeOwned();
            Controller = null;
            return;
        }
        if (_owned is null || !ReferenceEquals(_owned.Schema, schema))
        {
            DisposeOwned();
            _owned = new FormController<TModel>(schema, model);
            _loaded = model;
        }
        else if (model is not null && !ReferenceEquals(_loaded, model))
        {
            _owned.Reset(model);
            _loaded = model;
        }
        else if (model is null && _loaded is not null)
        {
            // The model went away: a submit now creates a new one, so the form shows the state of a new one.
            _owned.ResetToNewModel();
            _loaded = default;
        }
        Controller = _owned;
    }

    /// <summary>Validates, writes into the model (when there is one), runs the schema hooks, the save step and the submitted callback. True when the form was submitted.</summary>
    public async Task<bool> SubmitAsync(Func<TModel, CancellationToken, Task<bool>>? onSave, Action<TModel>? onSubmitted, TModel? model, CancellationToken cancellationToken)
    {
        if (Controller is not { } form) return false;
        if (onSave is null && onSubmitted is null) return await form.SubmitAsync();
        TModel? submitted = default;
        var done = await form.SubmitAsync(
            async (value, ct) =>
            {
                submitted = value;
                return onSave is null || await onSave(value, ct);
            },
            model,
            cancellationToken);
        if (done && onSubmitted is not null) onSubmitted(submitted!);
        return done;
    }

    public void Dispose()
    {
        if (_owned is not null) DisposeOwned();
        else Controller?.Engine.CancelPendingValidations();
    }

    private void DisposeOwned()
    {
        _owned?.Dispose();
        _owned = null;
        _loaded = default;
    }
}
