using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Klyroo.Form;

/// <summary>
/// Renders a sectioned typed form as a page: a sidebar listing each section with its completion status, and one card per
/// section laid out by its span. Fields render with the same controls, labels, containers and
/// repeatable lists as <c>KlyFormEngine&lt;TModel&gt;</c>, and the page is a <c>form</c> element, so pressing Enter in a field submits it.
/// <typeparamref name="TModel"/> is inferred from <c>Form</c>, <c>Schema</c> or <c>Model</c>.
/// </summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
[CascadingTypeParameter(nameof(TModel))]
public sealed partial class KlyFormPage<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
}
