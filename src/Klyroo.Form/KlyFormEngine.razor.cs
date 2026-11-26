namespace Klyroo.Form;

/// <summary>
/// Renders a <see cref="FormSchema"/> as a form: header, optional stepper with the description of the current step, a field grid of <see cref="FormSchema.Columns"/> columns and the footer actions
/// (Back, Reset, Cancel, Next, Submit). Pass either a <see cref="FormController"/> through <c>Form</c> or a schema through
/// <c>Schema</c>. The component subscribes to <see cref="FormController.Changed"/> and re-renders on every change.
/// </summary>
public partial class KlyFormEngine
{
}
