using System.Globalization;

namespace Klyroo.Form;

// Converts the untyped value a control stored into the type the caller asks for, with the invariant culture and without reflection
// over the target type, so FormController.GetValue<T> works under NativeAOT and trimming. The formats are the ones the controls store:
// "yyyy-MM-dd" (Date), "yyyy-MM-ddTHH:mm" (DateTime), "HH:mm" (Time), double (Number, Slider), int (Rating).
internal static class FormValueConverter
{
    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "yyyy-MM-dd",
    ];

    private static readonly string[] TimeFormats = ["HH:mm", "H:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];

    public static bool TryConvert<T>(object? raw, out T? value)
    {
        if (raw is T direct)
        {
            value = direct;
            return true;
        }
        value = default;
        if (raw is null) return false;
        var target = typeof(T);
        var type = Nullable.GetUnderlyingType(target) ?? target;
        if (type != typeof(string) && raw is string text && string.IsNullOrWhiteSpace(text)) return false;
        if (TryConvertCore(raw, type, out var converted) && converted is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }

    // The same conversion for a target known only at run time (a model member). Type is compared, never inspected, so it stays trim and AOT safe.
    internal static bool TryConvert(object? raw, Type target, out object? value)
    {
        value = null;
        if (raw is null) return false;
        var type = Nullable.GetUnderlyingType(target) ?? target;
        if (type.IsInstanceOfType(raw)) { value = raw; return true; }
        if (type != typeof(string) && raw is string text && string.IsNullOrWhiteSpace(text)) return false;
        return TryConvertCore(raw, type, out value, lenientEnum: true);
    }

    // lenientEnum: a number that no member of an enum has still converts, as long as it fits the underlying type. A model can hold such a value (a flag
    // combination, a value from a newer version of a database), and the binding must give it back when the user did not change it.
    private static bool TryConvertCore(object raw, Type type, out object? result, bool lenientEnum = false)
    {
        result = null;
        if (type == typeof(string)) return Set(ToText(raw), out result);
        if (type == typeof(bool)) return TryBool(raw, out result);
        if (type == typeof(double)) return TryDouble(raw, out var d) && Set(d, out result);
        if (type == typeof(float)) return TryFloat(raw, out var f) && Set(f, out result);
        if (type == typeof(decimal)) return TryDecimal(raw, out var m) && Set(m, out result);
        if (IsIntegral(type)) return TryDecimal(raw, out var whole) && TryIntegral(whole, type, out result);
        if (type == typeof(Guid)) return TryGuid(raw, out result);
        if (type == typeof(DateOnly)) return TryDateOnly(raw, out result);
        if (type == typeof(TimeOnly)) return TryTimeOnly(raw, out result);
        if (type == typeof(DateTime)) return TryDateTime(raw, out result);
        if (type == typeof(DateTimeOffset)) return TryDateTimeOffset(raw, out result);
        if (type == typeof(TimeSpan)) return TryTimeSpan(raw, out result);
        if (type.IsEnum) return TryEnum(raw, type, lenientEnum, out result);
        if (type == typeof(List<string>) && raw is IEnumerable<string> strings) return Set(strings.ToList(), out result);
        if (type == typeof(string[]) && raw is IEnumerable<string> array) return Set(array.ToArray(), out result);
        if (raw is IConvertible && typeof(IConvertible).IsAssignableFrom(type))
        {
            try
            {
                return Set(Convert.ChangeType(raw, type, CultureInfo.InvariantCulture), out result);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                return false;
            }
        }
        return false;
    }

    private static bool Set(object? value, out object? result)
    {
        result = value;
        return value is not null;
    }

    internal static string ToInvariantText(object raw) => ToText(raw) ?? "";

    private static string? ToText(object raw) => raw switch
    {
        string s => s,
        bool b => b ? "true" : "false",
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly t => t.ToString(t.Millisecond != 0 ? "HH:mm:ss.FFF" : t.Second == 0 ? "HH:mm" : "HH:mm:ss", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString(dt.Millisecond != 0 ? "yyyy-MM-ddTHH:mm:ss.FFF" : dt.Second == 0 ? "yyyy-MM-ddTHH:mm" : "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => raw.ToString(),
    };

    private static bool TryBool(object raw, out object? result)
    {
        result = null;
        if (raw is not string s)
        {
            // A boxed number is a flag when it is exactly 1 or 0.
            return TryDecimal(raw, out var number) && (number == 1 || number == 0) && Set(number == 1, out result);
        }
        s = s.Trim();
        if (bool.TryParse(s, out var b)) return Set(b, out result);
        if (s is "1" or "on" or "On" or "ON") return Set(true, out result);
        if (s is "0" or "off" or "Off" or "OFF") return Set(false, out result);
        return false;
    }

    private static bool TryDouble(object raw, out double value)
    {
        switch (raw)
        {
            case double d: value = d; return !double.IsNaN(d);
            case float f: value = f; return !float.IsNaN(f);
            case string s: return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value);
            default:
                if (TryDecimal(raw, out var m))
                {
                    // Through the text: the conversion of a decimal to a double is not always the nearest double, the parsing of its exact text is.
                    value = double.Parse(m.ToString(CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
                    return true;
                }
                value = 0;
                return false;
        }
    }

    // A float is parsed from the text of what it is given, never from a double, so 0.1 stays 0.1f and a decimal or a long is rounded once.
    private static bool TryFloat(object raw, out float value)
    {
        value = 0;
        switch (raw)
        {
            case float f: value = f; return !float.IsNaN(f);
            case double d:
                if (double.IsNaN(d)) return false;
                value = (float)d;
                return double.IsInfinity(d) || float.IsFinite(value);
            case string s:
                return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && float.IsFinite(value);
            default:
                return TryDecimal(raw, out var m)
                    && float.TryParse(m.ToString(CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    && float.IsFinite(value);
        }
    }

    private static bool TryDecimal(object raw, out decimal value)
    {
        value = 0;
        try
        {
            switch (raw)
            {
                case decimal m: value = m; return true;
                case double d: return double.IsFinite(d) && FromShortestText(d.ToString("R", CultureInfo.InvariantCulture), d != 0, out value);
                case float f: return float.IsFinite(f) && FromShortestText(f.ToString("R", CultureInfo.InvariantCulture), f != 0, out value);
                case sbyte v: value = v; return true;
                case byte v: value = v; return true;
                case short v: value = v; return true;
                case ushort v: value = v; return true;
                case int v: value = v; return true;
                case uint v: value = v; return true;
                case long v: value = v; return true;
                case ulong v: value = v; return true;
                case string s: return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                default: return false;
            }
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    // The decimal that has the shortest text which reads back as the double or the float, with all its digits: the conversion operators round to 15 and 7 digits.
    // A number too small for a decimal is refused instead of becoming zero.
    private static bool FromShortestText(string text, bool nonZero, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && (value != 0 || !nonZero);

    // The decimal a number holds, or false when it is not a number or does not fit a decimal.
    internal static bool TryExactDecimal(object? raw, out decimal value)
    {
        value = 0;
        return raw is not null && TryDecimal(raw, out value);
    }

    // The value of a number the way a control of kind Number stores it: a double when the text of the number is the same for a double and for a decimal
    // (up to 15 digits, which is what a user types and what an input shows), and the decimal otherwise, so no digit of a long, an ulong or a decimal is lost.
    // A number that does not fit a decimal stays a double. NaN is no number.
    internal static object? NumberToControl(object value)
    {
        if (TryDecimal(value, out var m)) return DoubleIfExact(m);
        switch (value)
        {
            case double d: return double.IsNaN(d) ? null : d;
            case float f: return float.IsNaN(f) ? null : double.Parse(f.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            default: return null;
        }
    }

    private static object DoubleIfExact(decimal m)
    {
        var d = double.Parse(m.ToString(CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
        return d is > -1e28 and < 1e28 && (decimal)d == m ? d : m;
    }

    // What a Number input gives back: a double or a decimal as NumberToControl does, or null when the text is no number.
    internal static object? NumberFromInput(string text)
    {
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var m)
            && (m != 0 || !double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var tiny) || tiny == 0))
        {
            return DoubleIfExact(m);
        }
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && !double.IsNaN(d) ? d : null;
    }

    private static bool IsIntegral(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) || t == typeof(sbyte)
        || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort);

    // A number converts to an integer type only when it is whole and in range: 3.5 is not a valid int, it is not rounded to 4.
    private static bool TryIntegral(decimal d, Type t, out object? result)
    {
        result = null;
        if (d != decimal.Truncate(d)) return false;
        if (t == typeof(int)) return d is >= int.MinValue and <= int.MaxValue && Set((int)d, out result);
        if (t == typeof(long)) return d is >= long.MinValue and <= long.MaxValue && Set((long)d, out result);
        if (t == typeof(short)) return d is >= short.MinValue and <= short.MaxValue && Set((short)d, out result);
        if (t == typeof(byte)) return d is >= byte.MinValue and <= byte.MaxValue && Set((byte)d, out result);
        if (t == typeof(sbyte)) return d is >= sbyte.MinValue and <= sbyte.MaxValue && Set((sbyte)d, out result);
        if (t == typeof(uint)) return d is >= uint.MinValue and <= uint.MaxValue && Set((uint)d, out result);
        if (t == typeof(ulong)) return d is >= ulong.MinValue and <= ulong.MaxValue && Set((ulong)d, out result);
        if (t == typeof(ushort)) return d is >= ushort.MinValue and <= ushort.MaxValue && Set((ushort)d, out result);
        return false;
    }

    private static bool TryGuid(object raw, out object? result)
    {
        result = null;
        return raw switch
        {
            string s => Guid.TryParse(s.Trim(), out var g) && Set(g, out result),
            _ => false,
        };
    }

    private static bool TryDateOnly(object raw, out object? result)
    {
        result = null;
        switch (raw)
        {
            case DateTime dt: return Set(DateOnly.FromDateTime(dt), out result);
            case DateTimeOffset dto: return Set(DateOnly.FromDateTime(dto.DateTime), out result);
            case string s when TryParseDateTime(s, out var parsed): return Set(DateOnly.FromDateTime(parsed), out result);
            default: return false;
        }
    }

    private static bool TryTimeOnly(object raw, out object? result)
    {
        result = null;
        switch (raw)
        {
            case DateTime dt: return Set(TimeOnly.FromDateTime(dt), out result);
            case DateTimeOffset dto: return Set(TimeOnly.FromDateTime(dto.DateTime), out result);
            case TimeSpan ts: return ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1) && Set(TimeOnly.FromTimeSpan(ts), out result);
            case string s: return TimeOnly.TryParseExact(s.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) && Set(t, out result);
            default: return false;
        }
    }

    private static bool TryTimeSpan(object raw, out object? result)
    {
        result = null;
        switch (raw)
        {
            case TimeOnly t: return Set(t.ToTimeSpan(), out result);
            case string s: return TimeSpan.TryParse(s.Trim(), CultureInfo.InvariantCulture, out var ts) && Set(ts, out result);
            default: return false;
        }
    }

    private static bool TryDateTime(object raw, out object? result)
    {
        result = null;
        switch (raw)
        {
            case DateTimeOffset dto: return Set(dto.DateTime, out result);
            case DateOnly d: return Set(d.ToDateTime(TimeOnly.MinValue), out result);
            case string s: return TryParseDateTime(s, out var parsed) && Set(parsed, out result);
            default: return false;
        }
    }

    private static bool TryDateTimeOffset(object raw, out object? result)
    {
        result = null;
        switch (raw)
        {
            case DateTime dt:
                // A browser value has no zone: treat it as UTC rather than as the zone of the machine, which differs between server and client.
                return Set(dt.Kind == DateTimeKind.Unspecified ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)) : new DateTimeOffset(dt), out result);
            case DateOnly d:
                return Set(new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), out result);
            case string s:
                s = s.Trim();
                return (DateTimeOffset.TryParseExact(s, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exact) && Set(exact, out result))
                    || (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var any) && Set(any, out result));
            default:
                return false;
        }
    }

    private static bool TryParseDateTime(string s, out DateTime value)
    {
        s = s.Trim();
        return DateTime.TryParseExact(s, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
            || DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
    }

    private static bool TryEnum(object raw, Type type, bool lenient, out object? result)
    {
        result = null;
        if (raw.GetType() == type) return Set(raw, out result);
        if (raw is string s)
        {
            s = s.Trim();
            // "A, B" is a combination: it means something only for a [Flags] enum, otherwise Enum.TryParse would return a value no member has.
            if (s.Contains(',', StringComparison.Ordinal) && !type.IsDefined(typeof(FlagsAttribute), inherit: false)) return false;
            if (!Enum.TryParse(type, s, ignoreCase: true, out var parsed) || parsed is null) return false;
            // "7" parses to 7 even when no member has that value: accept a number only when it names a member.
            var numeric = s.Length > 0 && (char.IsDigit(s[0]) || s[0] is '-' or '+');
            return (!numeric || lenient || Enum.IsDefined(type, parsed)) && Set(parsed, out result);
        }
        if (TryDecimal(raw, out var d) && TryIntegral(d, Enum.GetUnderlyingType(type), out var number) && number is not null)
        {
            var value = Enum.ToObject(type, number);
            return (lenient || Enum.IsDefined(type, value)) && Set(value, out result);
        }
        return false;
    }

    // A value read from a draft (JSON gives strings, doubles, booleans and lists) in the type the controls of this field use.
    internal static object? NormalizeForField(FieldNode field, object? value)
    {
        if (value is null) return null;
        switch (field.Kind)
        {
            case FormFieldKind.ArrayList or FormFieldKind.ArrayTable:
                var rows = new List<Dictionary<string, object?>>();
                if (value is System.Collections.IEnumerable items and not string)
                {
                    foreach (var item in items)
                    {
                        if (item is not IEnumerable<KeyValuePair<string, object?>> map) continue;
                        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                        foreach (var (key, cell) in map)
                        {
                            var child = field.Fields?.FirstOrDefault(c => c.Key == key);
                            if (child?.Kind is FormFieldKind.File or FormFieldKind.Custom) continue;
                            row[key] = child is null ? cell : NormalizeForField(child, cell);
                        }
                        rows.Add(row);
                    }
                }
                return rows;
            case FormFieldKind.Custom or FormFieldKind.File:
                return value;
            case FormFieldKind.Number or FormFieldKind.Slider:
                return NumberToControl(value);
            case FormFieldKind.Rating:
                return TryConvert<int>(value, out var rating) ? rating : null;
            case FormFieldKind.Checkbox or FormFieldKind.Switch:
                return TryConvert<bool>(value, out var flag) ? flag : null;
            case FormFieldKind.CheckboxGroup or FormFieldKind.Tag or FormFieldKind.DateRange:
                return StringList(value);
            case FormFieldKind.Select or FormFieldKind.AutoComplete when field.Multiple:
                return StringList(value);
            default:
                return value is string ? value : ToInvariantText(value);
        }
    }

    private static List<string>? StringList(object value) => value switch
    {
        string s => s.Length == 0 ? [] : [s],
        System.Collections.IEnumerable items => items.Cast<object?>().Select(i => i is null ? "" : ToInvariantText(i)).ToList(),
        _ => null,
    };
}
