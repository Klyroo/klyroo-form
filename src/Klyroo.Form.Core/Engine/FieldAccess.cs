using System.Text.Json;

namespace Klyroo.Form;

// A field of a form as IFormFieldAccess, over the engine: both controllers hand these out.
internal sealed class FieldAccess(FormEngine engine, FieldNode node) : IFormFieldAccess
{
    public static IReadOnlyList<IFormFieldAccess> For(FormEngine engine) =>
        engine.AllFields
            .Where(f => !FormEngine.IsStateless(f.Kind) && f.Kind != FormFieldKind.Hidden)
            .Select(f => (IFormFieldAccess)new FieldAccess(engine, f))
            .ToList();

    public string Key => node.Key;

    public FormFieldKind Kind => node.Kind;

    public string Label => node.Label ?? node.Key;

    public bool Sensitive => node.Kind is FormFieldKind.Password or FormFieldKind.OneTimeCode;

    public bool IsHidden => !engine.ActiveFields.Contains(node);

    public bool IsOffPage => engine.IsStepped && engine.CurrentStep is { } step && !Leaves(step.Fields).Contains(node);

    public bool IsDisabled => engine.IsFieldDisabled(node);

    public bool IsRequired => engine.IsFieldRequired(node);

    public IReadOnlyList<FormOptionItem> Options => engine.GetFieldOptions(node);

    public void WriteValue(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Sensitive) writer.WriteNullValue();
        else FormDraft.WriteValue(writer, engine.GetValue(node.Key));
    }

    public FormWriteResult TrySet(JsonElement value)
    {
        var options = engine.GetFieldOptions(node);
        // A typed field reads the JSON through its codec, which also checks the value against the type; a dynamic field only has the kind rules.
        var read = node.JsonReader is { } reader
            ? reader(value, node, options, out var control, out var reason)
            : FormJsonCoercion.TryCoerce(node, options, value, out control, out reason);
        if (!read) return FormWriteResult.Fail(reason ?? "the value does not fit the field.");
        engine.SetValue(node.Key, control, markTouched: true);
        return FormWriteResult.Success;
    }

    public FormWriteResult Clear()
    {
        if (FormEngine.IsContainer(node.Kind) || node.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
        {
            return FormWriteResult.Fail($"\"{node.Key}\" has no value to clear.");
        }
        engine.SetValue(node.Key, FormJsonCoercion.EmptyValue(node), markTouched: true);
        return FormWriteResult.Success;
    }

    public FormWriteResult Toggle()
    {
        if (node.Kind is not (FormFieldKind.Checkbox or FormFieldKind.Switch))
        {
            return FormWriteResult.Fail($"\"{node.Key}\" is not a checkbox or a switch.");
        }
        engine.SetValue(node.Key, !engine.GetValue<bool>(node.Key), markTouched: true);
        return FormWriteResult.Success;
    }

    public void Reset() => engine.ResetField(node.Key);

    private static IEnumerable<FieldNode> Leaves(IEnumerable<FieldNode> fields)
    {
        foreach (var f in fields)
        {
            if (f.Kind == FormFieldKind.Tabs)
            {
                foreach (var tab in f.Tabs ?? []) foreach (var child in Leaves(tab.Fields)) yield return child;
            }
            else if (FormEngine.IsContainer(f.Kind))
            {
                foreach (var child in Leaves(f.Fields ?? [])) yield return child;
            }
            else
            {
                yield return f;
            }
        }
    }
}
