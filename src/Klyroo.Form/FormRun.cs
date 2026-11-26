using Klyroo.Form.Dynamic;
using Microsoft.Extensions.Logging;

namespace Klyroo.Form;

// Runs what a submit or a Next click calls (an OnSave handler, an asynchronous rule, a hook, an OnSubmit callback) so that an exception ends up
// as a message in the form instead of an unhandled exception, which on Blazor Server ends the circuit and loses everything the user typed.
internal static class FormRun
{
    // Returns false when the action reported a failure (or threw), true when it succeeded.
    public static async Task<bool> RunAsync(Func<Task<bool>> action, DynamicFormController form, ILogger logger, FormTexts texts, CancellationToken cancellationToken)
    {
        // The notice of a previous failed attempt must not outlive the next attempt.
        if (form.FormErrors.Contains(texts.SomethingWentWrong)) form.ClearError(DynamicFormController.FormErrorKey);
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The component was disposed while the action ran: nobody is left to tell.
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Klyroo.Form: submitting form '{FormKey}' failed.", form.Schema.FormKey);
            form.SetFormError(texts.SomethingWentWrong, blocking: false);
            return false;
        }
    }
}
