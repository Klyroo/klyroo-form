using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Klyroo.Form;

/// <summary>
/// Renders a <see cref="FormSchema{TModel}"/> as a form: header, optional stepper with the description of the current step, a field grid of columns and the footer actions
/// (Back, Reset, Cancel, Next, Submit). Pass either a <see cref="FormController{TModel}"/> through <c>Form</c> or a schema through <c>Schema</c>, with the <c>Model</c> to edit.
/// <typeparamref name="TModel"/> is inferred from <c>Form</c>, <c>Schema</c> or <c>Model</c>. The component subscribes to <see cref="FormController{TModel}.Changed"/> and re-renders on every change, and it
/// gives the controller to the components inside it (<c>KlyFieldTemplate</c>, <c>KlyFormFieldView</c>).
/// </summary>
/// <example>
/// <code>
/// &lt;KlyFormEngine Schema="schema" Model="signup" OnSave="SaveAsync" /&gt;
/// </code>
/// </example>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
[CascadingTypeParameter(nameof(TModel))]
public sealed partial class KlyFormEngine<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
}
