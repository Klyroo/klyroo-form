using System.Globalization;
using System.Text.Json;

namespace Klyroo.Form;

internal sealed record RawNumber(string Text, double Approximate);

internal static class FormJsonArgs
{
    public static object? Unwrap(object? raw, out string? error)
    {
        error = null;
        if (raw is not JsonElement element) return raw;
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
                return UnwrapNumber(element);
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    var inner = Unwrap(item, out error);
                    if (error is not null) return null;
                    list.Add(inner);
                }
                return list;
            default:
                error = "An object is not a valid value.";
                return null;
        }
    }

    private static object UnwrapNumber(JsonElement element)
    {
        var text = element.GetRawText();
        var hasExponent = text.AsSpan().IndexOfAny('e', 'E') >= 0;
        var isIntegerToken = !hasExponent && text.AsSpan().IndexOf('.') < 0;
        if (isIntegerToken && element.TryGetInt64(out var whole)) return whole;
        // A decimal rounds beyond 28 digits and cannot hold an exponent token as sent, so it is only used when it reproduces the token exactly.
        if (!hasExponent && element.TryGetDecimal(out var exact) && exact.ToString(CultureInfo.InvariantCulture) == text) return exact;
        return new RawNumber(text, element.TryGetDouble(out var approx) ? approx : double.PositiveInfinity);
    }

    public static bool IsNumber(object? value) =>
        value is RawNumber or double or float or decimal or int or long or short or byte or sbyte or uint or ulong or ushort;

    public static string NumberText(object value) => value switch
    {
        RawNumber raw => raw.Text,
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        float f => f.ToString("R", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    public static bool TryDouble(object? value, out double number)
    {
        number = 0;
        switch (value)
        {
            case string s:
                return double.TryParse(s.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture, out number) && double.IsFinite(number);
            case RawNumber raw:
                number = raw.Approximate;
                return double.IsFinite(number);
            case null or bool:
                return false;
            default:
                if (!IsNumber(value)) return false;
                number = value is decimal m ? (double)m : Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsFinite(number);
        }
    }

    public static bool TryInt(object? arg, out int result)
    {
        result = 0;
        switch (arg)
        {
            case int i:
                result = i;
                return true;
            case long l when l is >= int.MinValue and <= int.MaxValue:
                result = (int)l;
                return true;
            case decimal m when m == decimal.Truncate(m) && m is >= int.MinValue and <= int.MaxValue:
                result = (int)m;
                return true;
            case double d when d == Math.Floor(d) && d is >= int.MinValue and <= int.MaxValue:
                result = (int)d;
                return true;
            case string s:
                return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            default:
                return false;
        }
    }

    // Text compared without case, accents or repeated blanks, to match an option by its label.
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var formD = text.Trim().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(formD.Length);
        var prevSpace = false;
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(char.ToLowerInvariant(ch));
                prevSpace = false;
            }
        }
        return sb.ToString().Trim();
    }
}
