namespace Klyroo.Form;

/// <summary>One error to show, as listed by <see cref="FormController.GetErrorEntries"/>: a field, a cell of a repeatable list or the form as a whole.</summary>
/// <param name="Path">The key in <see cref="FormController.Errors"/>: a field key, a <see cref="FormController.CellPath"/> or the key given to <see cref="FormController.SetError"/>.</param>
/// <param name="Message">The error message.</param>
/// <param name="FieldKey">Key of the field that holds the error: the field itself, or the repeatable list for a cell. <c>null</c> for a form-level error.</param>
/// <param name="RowIndex">Zero-based row of the cell, or <c>null</c> when the error is not on a cell.</param>
/// <param name="ChildKey">Key of the child field of the cell, or <c>null</c> when the error is not on a cell.</param>
public sealed record FormErrorEntry(string Path, string Message, string? FieldKey, int? RowIndex, string? ChildKey)
{
    /// <summary><c>true</c> when the error belongs to the form as a whole and not to a field: its key matches no field of the schema.</summary>
    public bool IsFormLevel => FieldKey is null;
}

public sealed partial class FormController
{
    /// <summary>The key <see cref="SetFormError"/> uses for the form-level error. It matches no field, so the error counts as form-level.</summary>
    public const string FormErrorKey = "$form";

    /// <summary><c>true</c> after a submission, or a move to the next step, failed because of validation, a hook or a handler, until a submission succeeds, the step changes or <see cref="Reset"/> runs. The components list every field error in the error summary while it is set.</summary>
    public bool SubmitFailed { get; private set; }

    /// <summary>Sets an error on the form as a whole, for a failure that belongs to no field (a server rejection, a conflict). It is a <see cref="SetError"/> on <see cref="FormErrorKey"/>, so it shows in the error summary, and it clears with <see cref="ClearError"/> or <see cref="Reset"/>.</summary>
    /// <param name="message">The message to show.</param>
    /// <param name="blocking"><c>true</c> to make <see cref="Validate"/> and <see cref="SubmitAsync()"/> fail until the error is cleared. The default is <c>true</c>, since a form-level error usually means the data cannot be saved as it is; fields do not clear it when they change, so call <see cref="ClearError"/> when the user fixes the cause.</param>
    public void SetFormError(string message, bool blocking = true) => SetError(FormErrorKey, message, blocking);

    /// <summary>The messages of the form-level errors: those set with <see cref="SetError"/> on a key that matches no field of the schema, such as <see cref="FormErrorKey"/>.</summary>
    public IReadOnlyList<string> FormErrors =>
        _customErrors.Where(e => IsFormLevelKey(e.Key)).Select(e => e.Value.Message).ToList();

    // A key is a field key when it is a field of the schema, or a cell path (lines[0].sku) of a repeatable list field.
    internal bool IsFormLevelKey(string key) => !AllFieldKeys().Contains(BaseKey(key));

    internal static string BaseKey(string key)
    {
        var bracket = key.IndexOf('[', StringComparison.Ordinal);
        return bracket > 0 ? key[..bracket] : key;
    }

    private HashSet<string> AllFieldKeys() => AllFields.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);

    /// <summary>Lists the errors to show, in the order of the form: the errors of the fields in effect and of their cells, then the other errors set with <see cref="SetError"/>, form-level ones last. It is built from <see cref="Errors"/>, so errors of untouched fields are not listed.</summary>
    /// <returns>The entries, possibly empty.</returns>
    public IReadOnlyList<FormErrorEntry> GetErrorEntries()
    {
        var errors = Errors;
        var result = new List<FormErrorEntry>();
        if (errors.Count == 0) return result;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var fieldKeys = AllFieldKeys();

        foreach (var field in ActiveFields)
        {
            if (errors.TryGetValue(field.Key, out var message) && seen.Add(field.Key))
            {
                result.Add(new FormErrorEntry(field.Key, message, field.Key, null, null));
            }
            if (field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
            {
                var rows = GetArrayItems(field.Key).Count;
                var children = CellFields(field);
                for (var row = 0; row < rows; row++)
                {
                    foreach (var child in children)
                    {
                        var path = CellPath(field.Key, row, child.Key);
                        if (errors.TryGetValue(path, out var cellMessage) && seen.Add(path))
                        {
                            result.Add(new FormErrorEntry(path, cellMessage, field.Key, row, child.Key));
                        }
                    }
                }
            }
        }

        foreach (var (path, message) in errors)
        {
            if (seen.Contains(path)) continue;
            var owner = fieldKeys.Contains(BaseKey(path)) ? BaseKey(path) : null;
            result.Add(new FormErrorEntry(path, message, owner, null, null));
        }
        return result.OrderBy(e => e.IsFormLevel ? 1 : 0).ToList();
    }
}
