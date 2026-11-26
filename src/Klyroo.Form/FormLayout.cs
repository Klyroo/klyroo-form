namespace Klyroo.Form;

// Grid helpers shared by the components: FormSchema.Columns sets the column count and every span is clamped to it.
internal static class FormLayout
{
    public static int Columns(FormSchema schema) => Math.Clamp(schema.Columns, 1, 12);

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
}
