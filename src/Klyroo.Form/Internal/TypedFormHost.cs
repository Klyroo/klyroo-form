using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Klyroo.Form.Internal;

// What KlyFormEngine<TModel> and KlyFormPage<TModel> do with their Form, Schema and Model parameters and with a submit, so the two components do it once: pick the
// controller (the given one, or one made from the schema and the model), reload it when another model arrives, and run the submit lifecycle with OnSave and OnSubmit.
internal sealed class TypedFormHost<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
    private FormController<TModel>? _owned;
    private TModel? _loaded;

    public FormController<TModel>? Controller { get; private set; }

    // Called from OnParametersSet. A controller the host made goes away with the schema that made it; a new instance of the model reloads it.
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

    // Validates, writes into the model (when there is one), runs the schema hooks, OnSave and OnSubmit. True when the form was submitted.
    public async Task<bool> SubmitAsync(
        Func<TModel, CancellationToken, Task<bool>>? onSave,
        EventCallback<TModel> onSubmit,
        TModel? model,
        CancellationToken cancellationToken)
    {
        if (Controller is not { } form) return false;
        if (onSave is null && !onSubmit.HasDelegate) return await form.SubmitAsync().ConfigureAwait(false);
        TModel? submitted = default;
        var done = await form.SubmitAsync(
            async (value, ct) =>
            {
                submitted = value;
                return onSave is null || await onSave(value, ct).ConfigureAwait(false);
            },
            model,
            cancellationToken);
        if (done && onSubmit.HasDelegate) await onSubmit.InvokeAsync(submitted!);
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
