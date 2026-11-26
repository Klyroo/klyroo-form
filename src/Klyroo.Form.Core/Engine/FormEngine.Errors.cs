namespace Klyroo.Form;


internal sealed partial class FormEngine
{
    internal const string FormErrorKey = "$form";

    internal bool SubmitFailed { get; private set; }

    internal int SubmitFailureCount { get; private set; }

    internal void SetFormError(string message, bool blocking = true) => SetError(FormErrorKey, message, blocking);

    internal IReadOnlyList<string> FormErrors =>
        _customErrors.Where(e => IsFormLevelKey(e.Key)).Select(e => e.Value.Message).ToList();

    // A key is a field key when it is a field of the schema, or a cell path (lines[0].sku) of a repeatable list field.
    internal bool IsFormLevelKey(string key) => !AllFieldKeys().Contains(BaseKey(key));

    internal static string BaseKey(string key)
    {
        var bracket = key.IndexOf('[', StringComparison.Ordinal);
        return bracket > 0 ? key[..bracket] : key;
    }

    private HashSet<string> AllFieldKeys() => AllFields.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);

    internal IReadOnlyList<FormErrorEntry> GetErrorEntries()
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
