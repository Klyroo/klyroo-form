using System.Globalization;
using System.Text.Json;

namespace Klyroo.Form;

/// <summary>
/// A snapshot of what the user has typed, produced by <see cref="FormController.ExportState"/> and loaded back with <see cref="FormController.RestoreDraft"/>,
/// for example to keep a long form across a page reload. It holds values only: the baseline that dirty tracking compares against stays with the controller.
/// <see cref="ToJson"/> and <see cref="FromJson"/> write and read it without reflection, so it works under NativeAOT and trimming.
/// </summary>
/// <param name="Values">The values by key, in the CLR types the controls use (strings, <see cref="double"/>, <see cref="bool"/>, <c>List&lt;string&gt;</c>, rows as <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>).</param>
/// <param name="StepIndex">Zero-based index of the step the user was on. 0 for a form without steps.</param>
public sealed record FormDraft(IReadOnlyDictionary<string, object?> Values, int StepIndex = 0)
{
    private const int Version = 1;

    /// <summary>Writes the draft as JSON. Values of other types than the ones of <see cref="Values"/> are written as text.</summary>
    /// <returns>A JSON document, for example to store in the browser.</returns>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", Version);
            writer.WriteNumber("step", StepIndex);
            writer.WritePropertyName("values");
            WriteValue(writer, Values);
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads a draft written by <see cref="ToJson"/>. Numbers come back as <see cref="double"/>, or as <see cref="decimal"/> when their digits do not fit a double (a large <see cref="long"/>); <see cref="FormController.RestoreDraft"/> converts each value to the type its field uses.</summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>The draft.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException"><paramref name="json"/> is not a draft written by <see cref="ToJson"/>.</exception>
    public static FormDraft FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("The JSON is not a form draft: it has no \"values\" object.");
            }
            var step = root.TryGetProperty("step", out var s) && s.TryGetInt32(out var i) ? i : 0;
            return new FormDraft((Dictionary<string, object?>)ReadValue(values)!, step);
        }
        catch (JsonException ex)
        {
            throw new FormatException("The text is not valid JSON.", ex);
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int or long or short or byte or sbyte or uint or ulong or ushort or float or double or decimal:
                if (value is double d && !double.IsFinite(d)) writer.WriteNullValue();
                else if (value is float f && !float.IsFinite(f)) writer.WriteNullValue();
                else writer.WriteRawValue(Convert.ToString(value, CultureInfo.InvariantCulture)!);
                break;
            case IEnumerable<KeyValuePair<string, object?>> map:
                writer.WriteStartObject();
                foreach (var (key, item) in map)
                {
                    writer.WritePropertyName(key);
                    WriteValue(writer, item);
                }
                writer.WriteEndObject();
                break;
            case System.Collections.IEnumerable items:
                writer.WriteStartArray();
                foreach (var item in items) WriteValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(FormValueConverter.ToInvariantText(value));
                break;
        }
    }

    private static object? ReadValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String: return element.GetString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Number: return FormValueConverter.NumberFromInput(element.GetRawText()) ?? element.GetDouble();
            case JsonValueKind.Object:
                var map = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject()) map[property.Name] = ReadValue(property.Value);
                return map;
            case JsonValueKind.Array:
                var items = element.EnumerateArray().Select(ReadValue).ToList();
                if (items.Count > 0 && items.All(i => i is Dictionary<string, object?>))
                {
                    return items.Cast<Dictionary<string, object?>>().ToList();
                }
                return items.All(i => i is string) ? items.Cast<string>().ToList() : items;
            default: return null;
        }
    }
}
