namespace Klyroo.Form.Dynamic;

/// <summary>
/// Renders one field of a <see cref="DynamicFormController"/> with the control of its <see cref="FormFieldKind"/>, its label, description,
/// hint, help, error and per-field reset, the way <c>KlyDynamicFormEngine</c> and <c>KlyDynamicFormPage</c> render their fields. Containers, tabs
/// and repeatable lists render their children too. Place it in a CSS grid of <see cref="DynamicFormSchema.Columns"/> columns, since the
/// field sets its own <c>grid-column</c> span. It renders nothing when <see cref="DynamicFormController.ShouldRenderField"/> is <c>false</c>.
/// It does not subscribe to <see cref="DynamicFormController.Changed"/>: render it from a component that re-renders on that event, so
/// changes made elsewhere (by code or by another field) show up.
/// </summary>
public sealed partial class KlyDynamicFieldView
{
}
