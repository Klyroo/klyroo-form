namespace Klyroo.Form.Dynamic;

/// <summary>Result of an overlay opened with <see cref="DynamicFormService.CreateFormAsync"/>.</summary>
/// <param name="IsCompleted"><c>true</c> when the form was submitted successfully, <c>false</c> when it was closed without submitting.</param>
/// <param name="FormData">The output of the form at that moment, as returned by <see cref="DynamicFormController.BuildOutput"/>.</param>
public sealed record DynamicFormOutcome(
    bool IsCompleted,
    IReadOnlyDictionary<string, object?> FormData);

/// <summary>Opens a form as an overlay and returns its result as a task. Register it with <c>AddKlyrooForm()</c> (scoped) and mount one <c>KlyFormProvider</c> in the layout so the form has somewhere to render.</summary>
public sealed class DynamicFormService
{
    /// <summary>Raised when a form is opened, submitted or closed. <c>KlyFormProvider</c> subscribes to it to re-render.</summary>
    public event Action? Changed;

    /// <summary>The controller of the open form, or <c>null</c> when none is open.</summary>
    public DynamicFormController? ActiveForm { get; private set; }
    /// <summary>The shell mode of the open form. The default is <see cref="FormShellMode.Modal"/>.</summary>
    public FormShellMode ActiveMode { get; private set; } = FormShellMode.Modal;
    private TaskCompletionSource<DynamicFormOutcome>? _tcs;

    /// <summary><c>true</c> while a form is open.</summary>
    public bool IsOpen => ActiveForm is not null;

    /// <summary>Opens a form in the mounted <c>KlyFormProvider</c> and waits for its result. Opening a form while another is open replaces it, and the replaced form's task completes with <see cref="DynamicFormOutcome.IsCompleted"/> set to <c>false</c> and that form's output, as if it had been closed.</summary>
    /// <param name="schema">The form to show.</param>
    /// <param name="input">Initial values by field key.</param>
    /// <param name="mode">How to present the form.</param>
    /// <returns>A task that completes with <see cref="DynamicFormOutcome.IsCompleted"/> set to <c>true</c> after a successful submit, or <c>false</c> after <see cref="CloseForm"/> or when another form replaces it.</returns>
    public Task<DynamicFormOutcome> CreateFormAsync(
        DynamicFormSchema schema,
        IReadOnlyDictionary<string, object?>? input = null,
        FormShellMode mode = FormShellMode.Modal)
    {
        var replaced = ActiveForm;
        var replacedTcs = _tcs;
        ActiveForm = new DynamicFormController(schema, input);
        ActiveMode = mode;
        _tcs = new TaskCompletionSource<DynamicFormOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        Changed?.Invoke();
        if (replaced is not null)
        {
            replacedTcs?.TrySetResult(new DynamicFormOutcome(false, replaced.BuildOutput()));
            replaced.Dispose();
        }
        return _tcs.Task;
    }

    /// <summary>Submits the open form with <see cref="DynamicFormController.SubmitAsync()"/>. On success it closes the form and completes the task of <see cref="CreateFormAsync"/>.</summary>
    /// <returns><c>true</c> if the form was submitted; <c>false</c> if no form is open or submission failed.</returns>
    public async Task<bool> SubmitActiveFormAsync()
    {
        if (ActiveForm is not { } form) return false;
        var ok = await form.SubmitAsync();
        if (ok) CompleteSubmittedForm(form);
        return ok;
    }

    // Called by KlyFormProvider once KlyFormEngine has already submitted the controller, so the schema hooks run once.
    internal void CompleteSubmittedForm(DynamicFormController form)
    {
        if (!ReferenceEquals(form, ActiveForm)) return;
        var data = form.BuildOutput();
        var tcs = _tcs;
        ActiveForm = null;
        _tcs = null;
        Changed?.Invoke();
        tcs?.TrySetResult(new DynamicFormOutcome(true, data));
        form.Dispose();
    }

    /// <summary>Closes the open form without submitting and completes the task of <see cref="CreateFormAsync"/> with <see cref="DynamicFormOutcome.IsCompleted"/> set to <c>false</c>. Does nothing when no form is open.</summary>
    public void CloseForm()
    {
        if (ActiveForm is null) return;
        var closing = ActiveForm;
        var data = closing.BuildOutput();
        var tcs = _tcs;
        ActiveForm = null;
        _tcs = null;
        Changed?.Invoke();
        tcs?.TrySetResult(new DynamicFormOutcome(false, data));
        closing.Dispose();
    }
}

