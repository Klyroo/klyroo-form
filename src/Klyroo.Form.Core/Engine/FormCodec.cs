using System.Text.Json;

namespace Klyroo.Form;

// How the values of one field type cross the three boundaries a form has: the control (what the engine stores and the inputs edit), invariant text (hidden
// inputs, and later a static post) and JSON (drafts, agents). A codec is chosen once per field, by the type of the value, never by reflection over it.
internal abstract class FormCodec<TValue>
{
    public abstract FormFieldKind Kind { get; }

    // Whether an empty text is null (a string member that accepts it) or an empty string. Only a string field cares.
    public virtual bool NullableReference { get; set; }

    // The value as the control of the field stores it.
    public abstract object? ToControl(TValue? value);

    // The value a control stored as a TValue. Empty gives the default (and says so), Invalid gives the default too, and a write refuses it instead of storing it.
    public abstract ControlValue TryFromControl(object? raw, out TValue? value);

    // Invariant text of a value, null for no value. Lists are comma separated.
    public abstract string? Format(TValue? value);

    // Reads invariant text. Blank text is no value and gives the default; false means the text is not a TValue.
    public abstract bool TryParse(string? text, out TValue? value);

    public TValue? FromControlOrDefault(object? raw)
    {
        TryFromControl(raw, out var value);
        return value;
    }

    public void WriteJson(Utf8JsonWriter writer, TValue? value) => FormDraft.WriteValue(writer, ToControl(value));

    // The control value the JSON a model or a person gave stands for, checked against the field (kind, bounds, options, exact numbers) and then against the type.
    // This is what IFormFieldAccess.TrySet stores, so a number keeps every digit it had.
    public bool TryReadJsonControl(JsonElement element, FieldNode field, IReadOnlyList<FormOptionItem> options, out object? control, out string? reason)
    {
        if (!FormJsonCoercion.TryCoerce(field, options, element, out control, out reason)) return false;
        // The kind rules read a number as a double. A typed field keeps the digits the JSON had: the decimal (or the double, when it says the same) of its text.
        if (field.Kind is FormFieldKind.Number or FormFieldKind.Slider && control is double && NumberText(element) is { } text
            && FormValueConverter.NumberFromInput(text) is { } exact)
        {
            control = exact;
        }
        if (TryFromControl(control, out _) == ControlValue.Invalid)
        {
            reason = $"\"{field.Key}\" expects {FormValueMap.Expectation(typeof(TValue))}.";
            return false;
        }
        return true;
    }

    private static string? NumberText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.String => element.GetString()?.Trim(),
        _ => null,
    };

    // The JSON a model or a person gave, as the value of the field.
    public bool TryReadJson(JsonElement element, FieldNode field, IReadOnlyList<FormOptionItem> options, out TValue? value, out string? reason)
    {
        value = default;
        if (!TryReadJsonControl(element, field, options, out var control, out reason)) return false;
        TryFromControl(control, out value);
        return true;
    }

    // The kind a TValue gets when the declaration does not choose one, or null when no control fits it.
    public static FormFieldKind? DefaultKind => FormValueMap.DefaultKind(typeof(TValue));

    public static FormCodec<TValue> For(FormFieldKind kind, bool nullableReference) => new ScalarCodec<TValue>(kind, nullableReference);
}

// A single value, or a list of strings or files: the conversions of FormValueMap, which compares types and never inspects them.
internal sealed class ScalarCodec<TValue>(FormFieldKind kind, bool nullableReference) : FormCodec<TValue>
{
    private bool _nullableReference = nullableReference;

    public override FormFieldKind Kind => kind;

    public override bool NullableReference
    {
        get => _nullableReference;
        set => _nullableReference = value;
    }

    public override object? ToControl(TValue? value) => FormValueMap.ToControl(value, kind);

    public override ControlValue TryFromControl(object? raw, out TValue? value)
    {
        var state = FormValueMap.TryFromControl(raw, typeof(TValue), _nullableReference, out var converted);
        value = converted is null ? default : (TValue)converted;
        return state;
    }

    public override string? Format(TValue? value) => value is null ? null : FormatText(value);

    public override bool TryParse(string? text, out TValue? value) => TryFromControl(text, out value) != ControlValue.Invalid;

    internal static string FormatText(object value) =>
        value is IEnumerable<string> list ? string.Join(',', list) : FormValueConverter.ToInvariantText(value);
}

// A multiple choice: the control holds the option values as text, in the order they were chosen.
internal sealed class MultiCodec<TElement>(FormCodec<TElement> element) : FormCodec<IReadOnlyList<TElement>>
{
    public override FormFieldKind Kind => FormFieldKind.Select;

    public override object? ToControl(IReadOnlyList<TElement>? value) =>
        value is null ? null : value.Select(e => element.Format(e) ?? "").ToList();

    public override ControlValue TryFromControl(object? raw, out IReadOnlyList<TElement>? value)
    {
        value = [];
        if (raw is null) return ControlValue.Empty;
        if (raw is not IEnumerable<string> texts) return ControlValue.Invalid;
        var items = new List<TElement>();
        foreach (var text in texts)
        {
            if (!element.TryParse(text, out var item) || item is null && !string.IsNullOrWhiteSpace(text)) return ControlValue.Invalid;
            if (item is not null) items.Add(item);
        }
        value = items;
        return items.Count == 0 ? ControlValue.Empty : ControlValue.Value;
    }

    public override string? Format(IReadOnlyList<TElement>? value) => value is null ? null : string.Join(',', value.Select(e => element.Format(e)));

    public override bool TryParse(string? text, out IReadOnlyList<TElement>? value) =>
        TryFromControl(string.IsNullOrEmpty(text) ? [] : text.Split(','), out value) != ControlValue.Invalid;

}
