using System.Globalization;
using System.Text.Json;

namespace Klyroo.Form;

internal static class FormJsonCoercion
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd"];

    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss",
    ];

    private static readonly string[] TimeFormats = ["HH:mm", "HH:mm:ss"];

    public static object? EmptyValue(FieldNode field) => field.Kind switch
    {
        FormFieldKind.Checkbox or FormFieldKind.Switch => false,
        FormFieldKind.Tag or FormFieldKind.CheckboxGroup => new List<string>(),
        FormFieldKind.Select or FormFieldKind.AutoComplete when field.Multiple => new List<string>(),
        _ => null,
    };

    public static bool TryCoerce(
        FieldNode field,
        IReadOnlyList<FormOptionItem> options,
        System.Text.Json.JsonElement raw,
        out object? value,
        out string? error)
    {
        value = null;
        error = null;
        var arg = FormJsonArgs.Unwrap(raw, out var unwrapError);
        if (unwrapError is not null)
        {
            error = unwrapError;
            return false;
        }

        switch (field.Kind)
        {
            case FormFieldKind.Checkbox:
            case FormFieldKind.Switch:
                if (TryBool(arg, out var flag))
                {
                    value = flag;
                    return true;
                }
                error = $"\"{field.Key}\" expects a boolean (true or false), got {Describe(arg)}.";
                return false;

            case FormFieldKind.Number:
            case FormFieldKind.Slider:
            case FormFieldKind.Rating:
                return TryNumber(field, arg, out value, out error);

            case FormFieldKind.Date:
                return TryDate(arg, out value, out error);

            case FormFieldKind.DateTime:
                return TryDateTime(arg, out value, out error);

            case FormFieldKind.Time:
                return TryTime(arg, out value, out error);

            case FormFieldKind.DateRange:
                return TryDateRange(arg, out value, out error);

            case FormFieldKind.ColorPicker:
                return TryColor(arg, out value, out error);

            case FormFieldKind.CheckboxGroup:
                return TryChoices(field, options, arg, multiple: true, out value, out error);

            case FormFieldKind.Select or FormFieldKind.AutoComplete when field.Multiple:
                return TryChoices(field, options, arg, multiple: true, out value, out error);

            case FormFieldKind.AutoComplete:
                // Like the built-in input: text that names an option stores its value, any other text is kept as typed.
                if (TryChoices(field, options, arg, multiple: false, out value, out error)) return true;
                if (arg is not string) return false;
                error = null;
                return TryText(field, arg, out value, out error);

            case FormFieldKind.Select or FormFieldKind.Radio:
                return TryChoices(field, options, arg, multiple: false, out value, out error);

            case FormFieldKind.Tag:
                return TryTags(arg, out value, out error);

            case FormFieldKind.ArrayList or FormFieldKind.ArrayTable:
                error = $"\"{field.Key}\" is a repeatable list, which setValue cannot edit.";
                return false;

            default:
                if (FormEngine.IsContainer(field.Kind))
                {
                    error = $"\"{field.Key}\" is a container and has no value.";
                    return false;
                }
                return TryText(field, arg, out value, out error);
        }
    }

    private static string Describe(object? arg) => arg switch
    {
        null => "no value",
        string text => $"\"{text}\"",
        System.Collections.IEnumerable and not string => "a list",
        _ when FormJsonArgs.IsNumber(arg) => FormJsonArgs.NumberText(arg!),
        _ => Convert.ToString(arg, CultureInfo.InvariantCulture) ?? "an unsupported value",
    };

    private static bool TryText(FieldNode field, object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null) return true;
        string? text = arg switch
        {
            string s => s,
            bool or System.Collections.IEnumerable => null,
            _ when FormJsonArgs.IsNumber(arg) => FormJsonArgs.NumberText(arg),
            _ => null,
        };
        if (text is null)
        {
            error = $"\"{field.Key}\" expects text, got {Describe(arg)}.";
            return false;
        }
        if (field.MaxLength is { } max && text.Length > max)
        {
            error = $"\"{field.Key}\" accepts at most {max.ToString(CultureInfo.InvariantCulture)} characters, got {text.Length.ToString(CultureInfo.InvariantCulture)}.";
            return false;
        }
        value = text;
        return true;
    }

    private static bool TryBool(object? arg, out bool result)
    {
        result = false;
        switch (arg)
        {
            case bool b:
                result = b;
                return true;
            case string s:
                switch (s.Trim().ToLowerInvariant())
                {
                    case "true" or "yes" or "on" or "1":
                        result = true;
                        return true;
                    case "false" or "no" or "off" or "0":
                        return true;
                }
                return false;
            default:
                if (arg is not null && FormJsonArgs.IsNumber(arg) && FormJsonArgs.TryDouble(arg, out var number) && number is 0 or 1)
                {
                    result = number == 1;
                    return true;
                }
                return false;
        }
    }

    private static bool TryNumber(FieldNode field, object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null || arg is string { Length: 0 }) return true;
        if (!FormJsonArgs.TryDouble(arg, out var number))
        {
            error = $"\"{field.Key}\" expects a finite number (invariant format such as 12.5), got {Describe(arg)}.";
            return false;
        }
        if (field.Kind == FormFieldKind.Rating)
        {
            var maxRating = field.Max ?? 5;
            var minRating = field.Min ?? 0;
            if (number != Math.Floor(number) || number < minRating || number > maxRating)
            {
                error = $"\"{field.Key}\" expects a whole number from {minRating.ToString(CultureInfo.InvariantCulture)} to {maxRating.ToString(CultureInfo.InvariantCulture)}, got {Describe(arg)}.";
                return false;
            }
        }
        else
        {
            // The bounds are doubles, the number is not: compare the exact number the JSON had, so 100.0000000000000001 is above a Max of 100.
            var exact = arg switch { RawNumber raw => raw.Text, string text => text.Trim(), _ => arg };
            if (field.Min is { } min && (FormValueConverter.TryCompareToBound(exact, min, out var belowMin) ? belowMin < 0 : number < min))
            {
                error = $"\"{field.Key}\" must be at least {min.ToString(CultureInfo.InvariantCulture)}, got {Describe(arg)}.";
                return false;
            }
            if (field.Max is { } max && (FormValueConverter.TryCompareToBound(exact, max, out var aboveMax) ? aboveMax > 0 : number > max))
            {
                error = $"\"{field.Key}\" must be at most {max.ToString(CultureInfo.InvariantCulture)}, got {Describe(arg)}.";
                return false;
            }
        }
        value = number;
        return true;
    }

    private static bool TryDate(object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null || arg is string { Length: 0 }) return true;
        if (arg is string text)
        {
            var trimmed = text.Trim();
            if (DateOnly.TryParseExact(trimmed, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                value = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;
            }
            // A full timestamp is accepted for its date, since models often send one.
            if (DateTime.TryParseExact(trimmed, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp))
            {
                value = stamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;
            }
        }
        error = $"Expected a date written yyyy-MM-dd (for example 2026-03-27), got {Describe(arg)}.";
        return false;
    }

    private static bool TryDateTime(object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null || arg is string { Length: 0 }) return true;
        if (arg is string text)
        {
            if (DateTime.TryParseExact(text.Trim(), DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var stamp))
            {
                value = stamp.ToString(stamp.Second == 0 && stamp.Millisecond == 0 ? "yyyy-MM-ddTHH:mm" : "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                return true;
            }
        }
        error = $"Expected a local date and time written yyyy-MM-ddTHH:mm (for example 2026-03-27T14:30), without a time zone, got {Describe(arg)}.";
        return false;
    }

    private static bool TryTime(object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null || arg is string { Length: 0 }) return true;
        if (arg is string text && TimeOnly.TryParseExact(text.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            value = time.Second == 0 ? time.ToString("HH:mm", CultureInfo.InvariantCulture) : time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            return true;
        }
        error = $"Expected a time written HH:mm (for example 14:30), got {Describe(arg)}.";
        return false;
    }

    private static bool TryDateRange(object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null) return true;
        if (arg is not List<object?> { Count: 2 } parts)
        {
            error = $"Expected a list of two dates [start, end] written yyyy-MM-dd, got {Describe(arg)}.";
            return false;
        }
        var days = new List<DateOnly>();
        foreach (var part in parts)
        {
            if (part is not string text || !DateOnly.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                error = $"Expected a date written yyyy-MM-dd in the range, got {Describe(part)}.";
                return false;
            }
            days.Add(day);
        }
        if (days[1] < days[0])
        {
            error = "The end date must not be before the start date.";
            return false;
        }
        value = days.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
        return true;
    }

    private static bool TryColor(object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null || arg is string { Length: 0 }) return true;
        if (arg is string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length == 7 && trimmed[0] == '#' && trimmed.Skip(1).All(Uri.IsHexDigit))
            {
                value = trimmed.ToLowerInvariant();
                return true;
            }
        }
        error = $"Expected a color written #rrggbb (for example #1a2b3c), got {Describe(arg)}.";
        return false;
    }

    private static bool TryTags(object? arg, out object? value, out string? error)
    {
        value = null;
        error = null;
        if (arg is null) return true;
        var items = arg is List<object?> list ? list : [arg];
        var tags = new List<string>();
        foreach (var item in items)
        {
            if (item is not string text)
            {
                error = $"Expected a list of text tags, got {Describe(item)} in the list.";
                return false;
            }
            var tag = text.Trim();
            if (tag.Length > 0 && !tags.Contains(tag, StringComparer.Ordinal)) tags.Add(tag);
        }
        value = tags;
        return true;
    }

    // Single or multiple choice: every value must be an enabled option, given by its value or by its label.
    private static bool TryChoices(
        FieldNode field,
        IReadOnlyList<FormOptionItem> options,
        object? arg,
        bool multiple,
        out object? value,
        out string? error)
    {
        value = null;
        error = null;
        if (arg is null || arg is string { Length: 0 })
        {
            value = multiple ? new List<string>() : null;
            return true;
        }

        IReadOnlyList<object?> items;
        if (arg is List<object?> list)
        {
            if (!multiple)
            {
                error = $"\"{field.Key}\" accepts one value, got a list.";
                return false;
            }
            items = list;
        }
        else
        {
            items = [arg];
        }

        var chosen = new List<string>();
        foreach (var item in items)
        {
            var text = item switch
            {
                string s => s,
                bool => null,
                not null when FormJsonArgs.IsNumber(item) => FormJsonArgs.NumberText(item),
                _ => null,
            };
            FormOptionItem? option = null;
            string? matchError = null;
            if (text is null || !TryMatchOption(options, text, out option, out matchError))
            {
                error = matchError ?? $"\"{field.Key}\" expects an option value or label, got {Describe(item)}. Use query to list the options.";
                return false;
            }
            if (!chosen.Contains(option!.Value, StringComparer.Ordinal)) chosen.Add(option.Value);
        }

        value = multiple ? chosen : chosen.Count > 0 ? chosen[0] : null;
        return true;
    }

    private static bool TryMatchOption(IReadOnlyList<FormOptionItem> options, string text, out FormOptionItem? option, out string? error)
    {
        error = null;
        option = options.FirstOrDefault(o => string.Equals(o.Value, text, StringComparison.Ordinal));
        if (option is null)
        {
            var normalized = FormJsonArgs.Normalize(text);
            var byLabel = normalized.Length == 0
                ? []
                : options.Where(o => FormJsonArgs.Normalize(o.Label) == normalized).ToList();
            if (byLabel.Count > 1)
            {
                error = $"\"{text}\" matches several options by label ({string.Join(", ", byLabel.Select(o => o.Value))}). Use the option value.";
                return false;
            }
            option = byLabel.FirstOrDefault();
        }
        if (option is null)
        {
            error = $"\"{text}\" is not one of the options. Use query to list them.";
            return false;
        }
        if (option.Disabled)
        {
            error = $"The option \"{option.Label}\" is disabled.";
            return false;
        }
        return true;
    }
}
