using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form;

/// <summary>
/// Draws the control of one field of a typed form with your markup. Put it inside <c>KlyFormEngine&lt;TModel&gt;</c> (or <c>KlyFormPage&lt;TModel&gt;</c>), name the field with its handle, and read the
/// <see cref="FormControlContext{TValue}"/> it gives (<c>Value</c>, <c>Set</c>, <c>Commit</c>, <c>Id</c>, <c>Error</c>, <c>Disabled</c>, <c>Blur</c>): the component still draws the label,
/// description, help and error around your control. The template replaces the control of any field, so a field you declared with <c>AsCustom()</c> needs one, and a built-in kind can be
/// swapped for your own. <typeparamref name="TModel"/> and <typeparamref name="TValue"/> are inferred from <c>Field</c>. It renders nothing itself.
/// </summary>
/// <example>
/// <code>
/// &lt;KlyFormEngine Schema="schema" Model="signup"&gt;
///     &lt;KlyFieldTemplate Field="@rating" Context="c"&gt;
///         &lt;input type="range" id="@c.Id" min="1" max="5" value="@c.Value" @oninput="@(e =&gt; c.Set(int.Parse(e.Value!.ToString()!)))" @onblur="c.Blur" /&gt;
///     &lt;/KlyFieldTemplate&gt;
/// &lt;/KlyFormEngine&gt;
/// </code>
/// </example>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="TValue">The type of the value of the field.</typeparam>
public sealed partial class KlyFieldTemplate<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, TValue>
{
}
