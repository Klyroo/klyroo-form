using System.Reflection;

namespace Klyroo.Form;

// A field bound to a member: reads the member into the value of the control and writes the value back.
internal sealed class FormFieldBinding(FormMemberInfo member, FormFieldKind kind, bool nullableReference)
{
    public FormMemberInfo Member { get; } = member;

    public string Key => Member.Key;

    public bool CanWrite => Member.Property.SetMethod is { IsPublic: true };

    // The value of the member, or null when an object on the way to it is null.
    public object? Read(object model)
    {
        object? current = model;
        foreach (var property in Member.Path)
        {
            if (current is null) return null;
            current = property.GetValue(current);
        }
        return current;
    }

    public object? ToControl(object model) => FormValueMap.ToControl(Read(model), kind);

    // Works out the assignment of the member from the value of the control and adds it to the plan, without touching the model. An object on the way that is
    // null is created with the creator registered for it by Nested, and attached when the plan is applied.
    public void Plan(object model, object? raw, IReadOnlyDictionary<string, Func<object>> creators, FormWritePlan plan)
    {
        if (!CanWrite) return;
        if (FormValueMap.TryFromControl(raw, Member.Type, nullableReference, out var value) == ControlValue.Invalid)
        {
            throw new InvalidOperationException($"The value of '{Key}' cannot be converted to {Member.UnderlyingType.Name}.");
        }
        var holder = model;
        var prefix = "";
        for (var i = 0; i < Member.Path.Count - 1; i++)
        {
            var property = Member.Path[i];
            prefix = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            var next = plan.Created.TryGetValue(prefix, out var planned) ? planned : property.GetValue(holder);
            if (next is null)
            {
                if (!creators.TryGetValue(prefix, out var create) || property.SetMethod is not { IsPublic: true })
                {
                    throw new InvalidOperationException($"'{prefix}' is null and cannot be created: declare its fields with Nested(...), or give it a value in the constructor of the model.");
                }
                next = create();
                plan.Created[prefix] = next;
                plan.Add(holder, property, next);
            }
            holder = next;
        }
        var leaf = Member.Property;
        var current = leaf.GetValue(holder);
        // Text that is what the control shows for the value the member has now means the user did not change it: the member keeps the value, with the
        // precision and the kind that the text of the control cannot hold.
        if (IsUntouched(current, raw)) return;
        // A zone-less control value keeps the offset the member had.
        if (value is DateTimeOffset written && current is DateTimeOffset existing)
        {
            value = new DateTimeOffset(written.DateTime, existing.Offset);
        }
        // A typed date and time has no zone either: it keeps the kind of the value it replaces, and is unspecified when there was none.
        else if (value is DateTime { Kind: DateTimeKind.Unspecified } typed && current is DateTime before)
        {
            value = DateTime.SpecifyKind(typed, before.Kind);
        }
        plan.Add(holder, leaf, value);
    }

    private bool IsUntouched(object? current, object? raw) =>
        current is not null && raw is string text && IsLossy(Member.UnderlyingType)
        && FormValueMap.ToControl(current, kind) is string shown && string.Equals(text, shown, StringComparison.Ordinal);

    // The types whose text in a control cannot say everything about the value: an enum value that has no name, ticks, a kind.
    private static bool IsLossy(Type type) =>
        type.IsEnum || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeOnly);
}
