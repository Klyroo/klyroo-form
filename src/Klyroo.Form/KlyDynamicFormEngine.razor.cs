namespace Klyroo.Form.Dynamic;

/// <summary>
/// Renders a <see cref="DynamicFormSchema"/> as a form: header, optional stepper with the description of the current step, a field grid of <see cref="DynamicFormSchema.Columns"/> columns and the footer actions
/// (Back, Reset, Cancel, Next, Submit). Pass either a <see cref="DynamicFormController"/> through <c>Form</c> or a schema through
/// <c>Schema</c>. The component subscribes to <see cref="DynamicFormController.Changed"/> and re-renders on every change.
/// </summary>
public partial class KlyDynamicFormEngine
{
}
