using System.Globalization;
using System.Text;

namespace Klyroo.Form;

internal enum ControlValue
{
    Value,
    Empty,
    Invalid,
}

// What a member type means for a field: its kind, its settings and how its values move between the model and the controller.
// Types are only compared, never inspected with reflection, so everything here is trim and AOT safe.
internal static class FormValueMap
{
    // The kind a member type gets by default, or null when the engine has no control for it.
    public static FormFieldKind? DefaultKind(Type member)
    {
        var type = Nullable.GetUnderlyingType(member) ?? member;
        if (type == typeof(string) || type == typeof(Guid) || type == typeof(char)) return FormFieldKind.Text;
        if (type == typeof(bool)) return FormFieldKind.Checkbox;
        if (IsNumber(type)) return FormFieldKind.Number;
        if (type.IsEnum) return type.IsDefined(typeof(FlagsAttribute), inherit: false) ? null : FormFieldKind.Select;
        if (type == typeof(DateOnly)) return FormFieldKind.Date;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return FormFieldKind.DateTime;
        if (type == typeof(TimeOnly) || type == typeof(TimeSpan)) return FormFieldKind.Time;
        if (IsStringList(type)) return FormFieldKind.Tag;
        return null;
    }

    public static bool IsNumber(Type type) => IsInteger(type) || type == typeof(double) || type == typeof(float) || type == typeof(decimal);

    public static bool IsInteger(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte)
        || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort);

    public static bool IsStringList(Type type) =>
        type == typeof(string[]) || type == typeof(List<string>) || type == typeof(IList<string>) || type == typeof(ICollection<string>)
        || type == typeof(IEnumerable<string>) || type == typeof(IReadOnlyList<string>) || type == typeof(IReadOnlyCollection<string>);

    // The settings a member type gives to its field, before conventions and the explicit settings.
    public static void ApplyDefaults(FormMemberInfo member, FormFieldKind kind, bool clearOptional, FormFieldSettings s)
    {
        var type = member.UnderlyingType;
        s.Kind = kind;
        s.Label = Humanize(member.Name);
        if (kind == FormFieldKind.Select && type.IsEnum)
        {
            s.Options = Enum.GetNames(type).Select(n => new FormOptionItem(n, Humanize(n))).ToList();
            s.Clearable = member.IsNullableValueType || clearOptional;
        }
        if (kind == FormFieldKind.Number)
        {
            if (IsInteger(type))
            {
                s.Step = 1;
                (s.Min, s.Max) = Bounds(type);
                s.Validators = [WholeRule(), RangeRule(type)];
            }
            else if (type == typeof(decimal))
            {
                s.Step = 0.01;
                s.Validators = [RangeRule(type)];
            }
            else if (type == typeof(float))
            {
                s.Validators = [RangeRule(type)];
            }
        }
        if (type == typeof(Guid))
        {
            s.Validators =
            [
                new FormValidatorRule("guid", "Enter a valid identifier", (v, c) => v is not string t || t.Length == 0 || Guid.TryParse(t.Trim(), out _))
                {
                    LocalizedMessage = m => m.InvalidIdentifier,
                },
            ];
        }
    }

    // Min and Max are doubles, which cannot tell long.MaxValue from the next number: the exact checks are rules, with the message of the language of the form.
    private static FormValidatorRule WholeRule() =>
        new("whole", "Enter a whole number", (v, _) => FormController.IsEmptyValue(v) || !FormValueConverter.TryExactDecimal(v, out var m) || m == decimal.Truncate(m))
        {
            LocalizedMessage = m => m.WholeNumber,
        };

    private static FormValidatorRule RangeRule(Type type)
    {
        var (min, max) = BoundTexts(type);
        return new FormValidatorRule("range", $"Enter a number between {min} and {max}", (v, _) => FormController.IsEmptyValue(v) || FormValueConverter.TryConvert(v, type, out object? _))
        {
            LocalizedMessage = m => m.NumberOutOfRange(min, max),
        };
    }

    private static (string Min, string Max) BoundTexts(Type type)
    {
        var c = CultureInfo.InvariantCulture;
        return type == typeof(byte) ? (byte.MinValue.ToString(c), byte.MaxValue.ToString(c))
            : type == typeof(sbyte) ? (sbyte.MinValue.ToString(c), sbyte.MaxValue.ToString(c))
            : type == typeof(short) ? (short.MinValue.ToString(c), short.MaxValue.ToString(c))
            : type == typeof(ushort) ? (ushort.MinValue.ToString(c), ushort.MaxValue.ToString(c))
            : type == typeof(int) ? (int.MinValue.ToString(c), int.MaxValue.ToString(c))
            : type == typeof(uint) ? (uint.MinValue.ToString(c), uint.MaxValue.ToString(c))
            : type == typeof(long) ? (long.MinValue.ToString(c), long.MaxValue.ToString(c))
            : type == typeof(ulong) ? (ulong.MinValue.ToString(c), ulong.MaxValue.ToString(c))
            : type == typeof(float) ? (float.MinValue.ToString("R", c), float.MaxValue.ToString("R", c))
            : (decimal.MinValue.ToString(c), decimal.MaxValue.ToString(c));
    }

    private static (double Min, double Max) Bounds(Type type) =>
        type == typeof(byte) ? (byte.MinValue, byte.MaxValue)
        : type == typeof(sbyte) ? (sbyte.MinValue, sbyte.MaxValue)
        : type == typeof(short) ? (short.MinValue, short.MaxValue)
        : type == typeof(ushort) ? (ushort.MinValue, ushort.MaxValue)
        : type == typeof(int) ? (int.MinValue, int.MaxValue)
        : type == typeof(uint) ? (uint.MinValue, uint.MaxValue)
        : type == typeof(long) ? (long.MinValue, long.MaxValue)
        : (0, ulong.MaxValue);

    // "FirstName" -> "First name", "HTMLContent" -> "HTML content".
    public static string Humanize(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])
                || (char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1]))))
            {
                sb.Append(' ');
            }
            sb.Append(c);
        }
        var words = sb.ToString().Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            var acronym = w.Length > 1 && w.All(char.IsUpper);
            if (!acronym) w = w.ToLowerInvariant();
            if (i == 0 && w.Length > 0) w = char.ToUpperInvariant(w[0]) + w[1..];
            words[i] = w;
        }
        return string.Join(' ', words);
    }

    // The model value as the control of the field stores it.
    public static object? ToControl(object? value, FormFieldKind kind)
    {
        if (value is null) return null;
        switch (kind)
        {
            case FormFieldKind.Number or FormFieldKind.Slider:
                return FormValueConverter.NumberToControl(value);
            case FormFieldKind.Rating:
                return FormValueConverter.TryConvert<int>(value, out var rating) ? rating : null;
            case FormFieldKind.Checkbox or FormFieldKind.Switch:
                return FormValueConverter.TryConvert<bool>(value, out var flag) ? flag : null;
            case FormFieldKind.Date:
                return value switch
                {
                    DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    _ => FormValueConverter.ToInvariantText(value),
                };
            case FormFieldKind.DateTime:
                return value switch
                {
                    DateTimeOffset dto => FormValueConverter.ToInvariantText(dto.DateTime),
                    DateOnly d => d.ToString("yyyy-MM-dd'T'00:00", CultureInfo.InvariantCulture),
                    _ => FormValueConverter.ToInvariantText(value),
                };
        }
        return value switch
        {
            string s => s,
            Enum e => e.ToString(),
            IEnumerable<string> list => list.ToList(),
            _ => FormValueConverter.ToInvariantText(value),
        };
    }

    // The value a control stored as a value of the member type. A missing or blank value gives null for a reference or nullable type
    // and the default for the others, an empty string for a string that is not nullable.
    public static object? FromControl(object? raw, Type member, bool nullableReference)
    {
        TryFromControl(raw, member, nullableReference, out var value);
        return value;
    }

    // Like FromControl, and says what happened: the value converted, the value was empty (the default is given), or there is a value that is not one of the member type
    // (the default is given too, and a write refuses it instead of storing it).
    public static ControlValue TryFromControl(object? raw, Type member, bool nullableReference, out object? value)
    {
        if (FormValueConverter.TryConvert(raw, member, out value))
        {
            if (value is string { Length: 0 } && nullableReference) value = null;
            return ControlValue.Value;
        }
        value = Default(member, nullableReference);
        return raw is null || raw is string text && string.IsNullOrWhiteSpace(text) ? ControlValue.Empty : ControlValue.Invalid;
    }

    public static object? Default(Type member, bool nullableReference)
    {
        if (member == typeof(string)) return nullableReference ? null : "";
        if (!member.IsValueType) return IsStringList(member) ? EmptyList(member) : null;
        if (Nullable.GetUnderlyingType(member) is not null) return null;
        if (member.IsEnum) return Enum.ToObject(member, 0);
        if (member == typeof(bool)) return false;
        if (member == typeof(int)) return 0;
        if (member == typeof(long)) return 0L;
        if (member == typeof(short)) return (short)0;
        if (member == typeof(byte)) return (byte)0;
        if (member == typeof(sbyte)) return (sbyte)0;
        if (member == typeof(uint)) return 0u;
        if (member == typeof(ulong)) return 0UL;
        if (member == typeof(ushort)) return (ushort)0;
        if (member == typeof(double)) return 0d;
        if (member == typeof(float)) return 0f;
        if (member == typeof(decimal)) return 0m;
        if (member == typeof(Guid)) return Guid.Empty;
        if (member == typeof(DateOnly)) return default(DateOnly);
        if (member == typeof(DateTime)) return default(DateTime);
        if (member == typeof(DateTimeOffset)) return default(DateTimeOffset);
        if (member == typeof(TimeOnly)) return default(TimeOnly);
        if (member == typeof(TimeSpan)) return TimeSpan.Zero;
        if (member == typeof(char)) return '\0';
        return null;
    }

    private static object EmptyList(Type member) => member == typeof(string[]) ? Array.Empty<string>() : new List<string>();
}
