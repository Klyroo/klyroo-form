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

    // Sets the member from the value of the control. An object on the way that is null is created with the creator registered for it by Nested.
    public void Write(object model, object? raw, IReadOnlyDictionary<string, Func<object>> creators)
    {
        if (!CanWrite) return;
        var value = FormValueMap.FromControl(raw, Member.Type, nullableReference);
        var holder = model;
        var prefix = "";
        for (var i = 0; i < Member.Path.Count - 1; i++)
        {
            var property = Member.Path[i];
            prefix = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            var next = property.GetValue(holder);
            if (next is null)
            {
                if (!creators.TryGetValue(prefix, out var create) || property.SetMethod is not { IsPublic: true })
                {
                    throw new InvalidOperationException($"'{prefix}' is null and cannot be created: declare its fields with Nested(...), or give it a value in the constructor of the model.");
                }
                next = create();
                property.SetValue(holder, next);
            }
            holder = next;
        }
        var leaf = Member.Property;
        // A zone-less control value keeps the offset the member had.
        if (value is DateTimeOffset written && leaf.GetValue(holder) is DateTimeOffset existing)
        {
            value = new DateTimeOffset(written.DateTime, existing.Offset);
        }
        leaf.SetValue(holder, value);
    }
}
