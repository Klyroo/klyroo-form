using System.ComponentModel;

namespace Klyroo.Form.Internal;

/// <summary>Renders one field of a form: the label, the control of its kind, the description, help and error. It is the one renderer of typed and dynamic forms, and it takes its parameters from <c>KlyFormFieldView&lt;TModel&gt;</c>, <c>KlyDynamicFieldView</c> and the form components, which are the API; do not use it directly.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class FieldNodeView
{
}
