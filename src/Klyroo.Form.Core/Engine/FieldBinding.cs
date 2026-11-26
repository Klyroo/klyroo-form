using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Klyroo.Form;

// A field bound to a member of a model: reads the member into the value of the control and plans the write of the value back.
internal interface IFieldBinding<TModel>
{
    string Key { get; }

    FormMemberInfo Member { get; }

    bool CanWrite { get; }

    // The control value of the member, or null when an object on the way to it is null.
    object? ReadControl(TModel model);

    // Works out the assignment of the member from the value of the control and adds it to the plan, without touching the model. An object on the way that is
    // null is created (the creators of Group, or the one a holder carries) and attached when the plan is applied.
    void Plan(TModel model, object? raw, IReadOnlyDictionary<string, Func<object>> creators, FormWritePlan plan);
}

// The object that holds a member: the model itself, or a nested object reached through typed delegates.
internal abstract class HolderPath<TModel, THolder>
{
    // The holder, or null when an object on the way is null.
    public abstract THolder? Read(TModel model);

    // The holder, with the objects on the way that are null created and planned.
    public abstract THolder Resolve(TModel model, FormWritePlan plan);
}

internal sealed class RootHolder<TModel> : HolderPath<TModel, TModel>
{
    public override TModel? Read(TModel model) => model;

    public override TModel Resolve(TModel model, FormWritePlan plan) => model;
}

internal sealed class NestedHolder<TModel, TParent, TNested>(
    HolderPath<TModel, TParent> parent,
    Func<TParent, TNested?> get,
    Action<TParent, TNested>? set,
    Func<TNested> create,
    string prefix) : HolderPath<TModel, TNested>
{
    public string Prefix => prefix;

    public override TNested? Read(TModel model)
    {
        var holder = parent.Read(model);
        return holder is null ? default : get(holder);
    }

    public override TNested Resolve(TModel model, FormWritePlan plan)
    {
        if (plan.Created.TryGetValue(prefix, out var planned)) return (TNested)planned;
        var holder = parent.Resolve(model, plan);
        var next = get(holder);
        if (next is not null) return next;
        if (set is null)
        {
            throw new InvalidOperationException($"'{prefix}' is null and cannot be created: it has no public setter. Give it a value in the constructor of the model.");
        }
        next = create();
        plan.Created[prefix] = next!;
        plan.Add(holder, set, next, default!);
        return next;
    }
}

// Accessors built from the properties of a model: a typed delegate when the runtime can make one, reflection otherwise (a property declared on an interface).
internal static class Accessors
{
    public static Func<THolder, TValue> Getter<THolder, TValue>(PropertyInfo property)
    {
        if (property.GetMethod is { IsStatic: false } method)
        {
            try
            {
                return method.CreateDelegate<Func<THolder, TValue>>();
            }
            catch (ArgumentException)
            {
                // The shapes do not match: fall back to reflection.
            }
        }
        return holder => (TValue)property.GetValue(holder)!;
    }

    public static Action<THolder, TValue>? Setter<THolder, TValue>(PropertyInfo property)
    {
        if (property.SetMethod is not { IsPublic: true, IsStatic: false } method) return null;
        try
        {
            return method.CreateDelegate<Action<THolder, TValue>>();
        }
        catch (ArgumentException)
        {
            return (holder, value) => property.SetValue(holder, value);
        }
    }
}

internal sealed class TypedBinding<TModel, THolder, TValue>(
    HolderPath<TModel, THolder> holder,
    FormMemberInfo member,
    Func<THolder, TValue> get,
    Action<THolder, TValue>? set,
    FormCodec<TValue> codec) : IFieldBinding<TModel>
{
    public string Key => member.Key;

    public FormMemberInfo Member => member;

    public bool CanWrite => set is not null;

    public FormCodec<TValue> Codec => codec;

    public object? ReadControl(TModel model)
    {
        var h = holder.Read(model);
        return h is null ? null : codec.ToControl(get(h));
    }

    public void Plan(TModel model, object? raw, IReadOnlyDictionary<string, Func<object>> creators, FormWritePlan plan)
    {
        if (set is null) return;
        if (codec.TryFromControl(raw, out var value) == ControlValue.Invalid)
        {
            throw new InvalidOperationException($"The value of '{Key}' cannot be converted to {member.UnderlyingType.Name}.");
        }
        var h = holder.Resolve(model, plan);
        var current = get(h);
        // Text that is what the control shows for the value the member has now means the user did not change it: the member keeps the value, with the
        // precision and the kind that the text of the control cannot hold.
        if (BindingRules.IsUntouched(current, raw, member.UnderlyingType, codec.ToControl(current))) return;
        plan.Add(h, set, (TValue)BindingRules.Adjust(value, current)!, current);
    }
}

// A field of a member known only at run time, bound through reflection: the fields of AllMembers, and chains of members written without Group.
internal sealed class ReflectedBinding<TModel>(FormMemberInfo member, FormFieldKind kind, bool nullableReference) : IFieldBinding<TModel>
{
    public string Key => member.Key;

    public FormMemberInfo Member => member;

    public bool CanWrite => member.Property.SetMethod is { IsPublic: true };

    public FormFieldKind Kind => kind;

    public bool NullableReference => nullableReference;

    // The value of the member, or null when an object on the way to it is null.
    public object? Read(TModel model)
    {
        object? current = model;
        foreach (var property in member.Path)
        {
            if (current is null) return null;
            current = property.GetValue(current);
        }
        return current;
    }

    public object? ReadControl(TModel model) => FormValueMap.ToControl(Read(model), kind);

    public void Plan(TModel model, object? raw, IReadOnlyDictionary<string, Func<object>> creators, FormWritePlan plan)
    {
        if (!CanWrite) return;
        if (FormValueMap.TryFromControl(raw, member.Type, nullableReference, out var value) == ControlValue.Invalid)
        {
            throw new InvalidOperationException($"The value of '{Key}' cannot be converted to {member.UnderlyingType.Name}.");
        }
        object holder = model!;
        var prefix = "";
        for (var i = 0; i < member.Path.Count - 1; i++)
        {
            var property = member.Path[i];
            prefix = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            var next = plan.Created.TryGetValue(prefix, out var planned) ? planned : property.GetValue(holder);
            if (next is null)
            {
                if (!creators.TryGetValue(prefix, out var create) || property.SetMethod is not { IsPublic: true })
                {
                    throw new InvalidOperationException($"'{prefix}' is null and cannot be created: declare its fields with Group(...), or give it a value in the constructor of the model.");
                }
                next = create();
                plan.Created[prefix] = next;
                plan.Add<object, object?>(holder, (h, v) => property.SetValue(h, v), next, null);
            }
            holder = next;
        }
        var leaf = member.Property;
        var current = leaf.GetValue(holder);
        if (BindingRules.IsUntouched(current, raw, member.UnderlyingType, FormValueMap.ToControl(current, kind))) return;
        plan.Add<object, object?>(holder, (h, v) => leaf.SetValue(h, v), BindingRules.Adjust(value, current), current);
    }
}

internal static class BindingRules
{
    // The types whose text in a control cannot say everything about the value: an enum value that has no name, ticks, a kind.
    private static bool IsLossy(Type type) =>
        type.IsEnum || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeOnly);

    public static bool IsUntouched(object? current, object? raw, Type underlying, object? shownControl) =>
        current is not null && raw is string text && IsLossy(underlying) && shownControl is string shown && string.Equals(text, shown, StringComparison.Ordinal);

    // A zone-less control value keeps the offset the member had, and a typed date and time keeps the kind of the value it replaces.
    public static object? Adjust(object? value, object? current)
    {
        if (value is DateTimeOffset written && current is DateTimeOffset existing) return new DateTimeOffset(written.DateTime, existing.Offset);
        if (value is DateTime { Kind: DateTimeKind.Unspecified } typed && current is DateTime before) return DateTime.SpecifyKind(typed, before.Kind);
        return value;
    }
}
