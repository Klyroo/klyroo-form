namespace Klyroo.Form;

// Grid helpers shared by the components: the Columns of the schema set the column count and every span is clamped to it.
internal static class FormLayout
{
    public static int Columns(FormDefinition schema) => Math.Clamp(schema.Columns, 1, 12);

    public static string GridStyle(int columns) => $"grid-template-columns: repeat({columns}, minmax(0, 1fr))";

    public static string SpanStyle(int span, int columns)
    {
        var s = Math.Clamp(span, 1, columns);
        return $"grid-column: span {s} / span {s}";
    }

    // Ids are built from field keys; anything that is not a letter, digit, dash or underscore becomes a dash.
    public static string Id(string key)
    {
        Span<char> buffer = key.Length <= 128 ? stackalloc char[key.Length] : new char[key.Length];
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            buffer[i] = char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '-';
        }
        return "kly-ff-" + new string(buffer);
    }

    // The id of the error summary of a form.
    public static string SummaryId(string formKey) => Id("summary-" + formKey);

    // The id of the control to focus for an error: the field, the cell of a list, or the summary when the error belongs to the form.
    public static string TargetId(FormEngine form, FormErrorEntry entry) =>
        entry.FieldKey is null ? SummaryId(form.Schema.FormKey)
        : entry.RowIndex is { } row && entry.ChildKey is { } child ? Id($"{entry.FieldKey}-{row}-{child}")
        : Id(entry.FieldKey);

    // The id to focus after a failed submit or Next: the first field in error, else the summary, else nothing.
    public static string? FirstErrorTargetId(FormEngine form)
    {
        var entries = form.GetErrorEntries();
        return entries.Count == 0 ? null : TargetId(form, entries[0]);
    }
}
