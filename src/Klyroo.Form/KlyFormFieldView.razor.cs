using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form;

/// <summary>
/// Renders one field of a typed form with the control of its kind, its label, description, hint, help, error and per-field reset, the way <c>KlyFormEngine&lt;TModel&gt;</c> and
/// <c>KlyFormPage&lt;TModel&gt;</c> render their fields, for a layout of your own. Containers, tabs and repeatable lists render their children too. Place it in a CSS grid, since the
/// field sets its own <c>grid-column</c> span. It renders nothing when the field is not visible (<see cref="FormController{TModel}.IsVisible"/>). It does not subscribe to
/// <see cref="FormController{TModel}.Changed"/>: render it from a component that re-renders on that event (<c>KlyFormEngine</c> does), so changes made elsewhere show up.
/// </summary>
/// <example>
/// <code>
/// &lt;KlyFormFieldView Field="@signupForm.Email" Form="controller" /&gt;
/// </code>
/// </example>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed partial class KlyFormFieldView<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
}
