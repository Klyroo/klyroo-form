using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Klyroo.Form;

/// <summary>Result of an overlay opened with <see cref="FormService.CreateFormAsync{TModel}"/>.</summary>
/// <typeparam name="TModel">The model of the form.</typeparam>
/// <param name="IsCompleted"><c>true</c> when the form was submitted successfully, <c>false</c> when it was closed without submitting.</param>
/// <param name="Model">When completed, the model that was submitted: the one you passed, with the values written into it, or the new one the form created. Otherwise the model you passed (unchanged), or <c>null</c> when you passed none.</param>
public sealed record FormSubmitOutcome<TModel>(bool IsCompleted, TModel? Model);

/// <summary>
/// Opens a typed form as an overlay and returns its result as a task. Register it with <c>AddKlyrooForm()</c> (scoped) and mount one <c>KlyFormProvider</c> in the layout so the form has somewhere to render.
/// The runtime counterpart, for a form known only at run time, is <see cref="Dynamic.DynamicFormService"/>.
/// </summary>
public sealed class FormService
{
    /// <summary>Raised when a form is opened, submitted or closed. <c>KlyFormProvider</c> subscribes to it to re-render.</summary>
    public event Action? Changed;

    internal FormOverlay? Active { get; private set; }

    /// <summary><c>true</c> while a form is open.</summary>
    public bool IsOpen => Active is not null;

    /// <summary>Opens a form in the mounted <c>KlyFormProvider</c> and waits for its result. Opening a form while another is open replaces it, and the replaced form's task completes with <see cref="FormSubmitOutcome{TModel}.IsCompleted"/> set to <c>false</c>, as if it had been closed.</summary>
    /// <typeparam name="TModel">The model of the form.</typeparam>
    /// <param name="schema">The form to show.</param>
    /// <param name="model">The model to edit: its values start the form, and a submit writes the values back into it. Without one a submit creates a new model.</param>
    /// <param name="mode">How to present the form.</param>
    /// <returns>A task that completes with <see cref="FormSubmitOutcome{TModel}.IsCompleted"/> set to <c>true</c> after a successful submit, or <c>false</c> after <see cref="CloseForm"/> or when another form replaces it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <c>null</c>.</exception>
    public Task<FormSubmitOutcome<TModel>> CreateFormAsync<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>(
        FormSchema<TModel> schema,
        TModel? model = default,
        FormShellMode mode = FormShellMode.Modal)
    {
        ArgumentNullException.ThrowIfNull(schema);
        var replaced = Active;
        var overlay = new FormOverlay<TModel>(this, schema, model, mode);
        Active = overlay;
        Changed?.Invoke();
        replaced?.Abandon();
        return overlay.Outcome;
    }

    /// <summary>Submits the open form with <see cref="FormController{TModel}.SubmitAsync()"/>. On success it closes the form and completes the task of <see cref="CreateFormAsync{TModel}"/>.</summary>
    /// <returns><c>true</c> if the form was submitted; <c>false</c> if no form is open or submission failed.</returns>
    public Task<bool> SubmitActiveFormAsync() => Active?.SubmitAsync() ?? Task.FromResult(false);

    /// <summary>Closes the open form without submitting and completes the task of <see cref="CreateFormAsync{TModel}"/> with <see cref="FormSubmitOutcome{TModel}.IsCompleted"/> set to <c>false</c>. Does nothing when no form is open.</summary>
    public void CloseForm() => Active?.Close();

    internal void Release(FormOverlay overlay)
    {
        if (!ReferenceEquals(Active, overlay)) return;
        Active = null;
        Changed?.Invoke();
    }
}

// The form an overlay shows, whatever its model: what the provider needs to render it and to follow its state.
internal abstract class FormOverlay
{
    public abstract FormEngine Engine { get; }

    public abstract string? Title { get; }

    public abstract FormShellMode Mode { get; }

    public abstract RenderFragment Body();

    public abstract Task<bool> SubmitAsync();

    public abstract void Close();

    // Another form took its place: it ends as if it had been closed, and the service has already moved on.
    public abstract void Abandon();
}

internal sealed class FormOverlay<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel> : FormOverlay
{
    private readonly FormService _service;
    private readonly FormController<TModel> _controller;
    private readonly TModel? _model;
    private readonly TaskCompletionSource<FormSubmitOutcome<TModel>> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FormOverlay(FormService service, FormSchema<TModel> schema, TModel? model, FormShellMode mode)
    {
        _service = service;
        _model = model;
        Mode = mode;
        _controller = new FormController<TModel>(schema, model);
    }

    public Task<FormSubmitOutcome<TModel>> Outcome => _result.Task;

    public override FormEngine Engine => _controller.Engine;

    public override string? Title => _controller.Title;

    public override FormShellMode Mode { get; }

    public override RenderFragment Body() => builder =>
    {
        // The same wrapper as the runtime overlay: a fullscreen form does not stretch over the whole viewport.
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", Mode == FormShellMode.Fullscreen ? "mx-auto w-full max-w-3xl" : "w-full");
        builder.OpenComponent<KlyFormEngine<TModel>>(2);
        builder.SetKey(this);
        builder.AddComponentParameter(3, nameof(KlyFormEngine<TModel>.Form), _controller);
        builder.AddComponentParameter(4, nameof(KlyFormEngine<TModel>.Model), _model);
        builder.AddComponentParameter(5, nameof(KlyFormEngine<TModel>.Shell), Mode);
        builder.AddComponentParameter(6, nameof(KlyFormEngine<TModel>.OnSubmit), EventCallback.Factory.Create<TModel>(this, Complete));
        builder.AddComponentParameter(7, nameof(KlyFormEngine<TModel>.OnCancel), EventCallback.Factory.Create(this, Close));
        builder.CloseComponent();
        builder.CloseElement();
    };

    public override async Task<bool> SubmitAsync()
    {
        TModel? submitted = default;
        var ok = await _controller.SubmitAsync((value, _) =>
        {
            submitted = value;
            return Task.FromResult(true);
        }, _model);
        if (ok) Complete(submitted!);
        return ok;
    }

    public void Complete(TModel model)
    {
        _service.Release(this);
        _result.TrySetResult(new FormSubmitOutcome<TModel>(true, model));
        _controller.Dispose();
    }

    public override void Close()
    {
        _service.Release(this);
        _result.TrySetResult(new FormSubmitOutcome<TModel>(false, _model));
        _controller.Dispose();
    }

    public override void Abandon()
    {
        _result.TrySetResult(new FormSubmitOutcome<TModel>(false, _model));
        _controller.Dispose();
    }
}
