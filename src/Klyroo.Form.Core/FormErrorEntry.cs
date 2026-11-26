using Klyroo.Form.Dynamic;
namespace Klyroo.Form;

/// <summary>One error to show, as listed by <see cref="DynamicFormController.GetErrorEntries"/>: a field, a cell of a repeatable list or the form as a whole.</summary>
/// <param name="Path">The key in <see cref="DynamicFormController.Errors"/>: a field key, a <see cref="DynamicFormController.CellPath"/> or the key given to <see cref="DynamicFormController.SetError"/>.</param>
/// <param name="Message">The error message.</param>
/// <param name="FieldKey">Key of the field that holds the error: the field itself, or the repeatable list for a cell. <c>null</c> for a form-level error.</param>
/// <param name="RowIndex">Zero-based row of the cell, or <c>null</c> when the error is not on a cell.</param>
/// <param name="ChildKey">Key of the child field of the cell, or <c>null</c> when the error is not on a cell.</param>
public sealed record FormErrorEntry(string Path, string Message, string? FieldKey, int? RowIndex, string? ChildKey)
{
    /// <summary><c>true</c> when the error belongs to the form as a whole and not to a field: its key matches no field of the schema.</summary>
    public bool IsFormLevel => FieldKey is null;
}
