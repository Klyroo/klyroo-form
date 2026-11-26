using Klyroo.Form.Dynamic;
namespace Klyroo.Form;

/// <summary>
/// Host of the overlay forms opened with <see cref="FormService"/> (typed) and <see cref="DynamicFormService"/> (runtime). Mount one instance in a layout, wrapping the page
/// content, and call <see cref="FormService.CreateFormAsync{TModel}"/> or <see cref="DynamicFormService.CreateFormAsync"/> from anywhere in the same scope.
/// </summary>
public partial class KlyFormProvider
{
}
