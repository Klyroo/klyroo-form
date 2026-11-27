namespace Klyroo.Form.Maui;

/// <summary>
/// Runs what a submit or a Next calls (an <c>OnSave</c> handler, an asynchronous rule, a hook) so that an exception ends up as a message in the form instead of an unhandled exception
/// on the UI thread, which ends the app and loses everything the user typed.
/// </summary>
internal static class FormRun
{
    /// <summary>False when the action reported a failure (or threw), true when it succeeded.</summary>
    public static async Task<bool> RunAsync(Func<Task<bool>> action, FormEngine form, FormTexts texts, CancellationToken cancellationToken)
    {
        // The notice of a previous failed attempt must not outlive the next attempt.
        if (form.FormErrors.Contains(texts.SomethingWentWrong)) form.ClearError(FormEngine.FormErrorKey);
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The view went away while the action ran: nobody is left to tell.
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"Klyroo.Form: submitting form '{form.Schema.FormKey}' failed: {ex}");
            form.SetFormError(texts.SomethingWentWrong, blocking: false);
            return false;
        }
    }
}

/// <summary>What the form shells share: the context of the form, the submit lifecycle, the error summary and the focus after a failed submit.</summary>
internal abstract class FormShellBase : VerticalStackLayout, IDisposable
{
    private readonly CancellationTokenSource _disposed = new();
    private Func<CancellationToken, Task<bool>> _submit;

    protected FormShellBase(FormContext context, Func<CancellationToken, Task<bool>> submit)
    {
        Context = context;
        _submit = submit;
        Spacing = 16;
        HorizontalOptions = LayoutOptions.Fill;
        Summary = new KlyFormErrorSummary { Form = (IFormRuntime)context.Engine.Owner! };
        Summary.FocusEntry = FocusEntry;
        AutomationId = context.Engine.IdPrefix + "form";
    }

    public FormContext Context { get; }

    public FormEngine Engine => Context.Engine;

    public KlyFormErrorSummary Summary { get; }

    protected CancellationToken Token => _disposed.Token;

    /// <summary>Changes what a submit calls.</summary>
    public void SetSubmit(Func<CancellationToken, Task<bool>> submit) => _submit = submit;

    /// <summary>Shows the state of the form in every view.</summary>
    public abstract void Refresh();

    /// <summary>
    /// Validates, saves and raises the events of the component that holds the form, as the submit button does. Returns true when the form was submitted. When it was not, the focus goes to the first error.
    /// </summary>
    public virtual async Task<bool> SubmitAsync()
    {
        var form = Engine;
        if (form.IsSubmitting) return false;
        var ok = await FormRun.RunAsync(() => _submit(_disposed.Token), form, FormTexts.Current, _disposed.Token);
        if (!ok) RequestFocus();
        return ok;
    }

    /// <summary>Moves the focus to the first field in error, else to the summary.</summary>
    protected void RequestFocus()
    {
        var entries = Engine.GetErrorEntries();
        if (entries.Count > 0) FocusEntry(entries[0]);
    }

    private bool FocusEntry(FormErrorEntry entry)
    {
        if (entry.IsFormLevel)
        {
            _ = FormUi.RevealAsync(Summary);
            return false;
        }
        return Context.Focus(FormUi.TargetId(Engine, entry));
    }

    public void Dispose()
    {
        _disposed.Cancel();
        _disposed.Dispose();
        Context.Detach();
    }
}
