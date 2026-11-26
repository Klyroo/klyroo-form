using System.Reflection;
using System.Text.Json;

namespace Klyroo.Form;

// What a builder collects while a schema is declared and turns into nodes when it is built, once every convention is known. The pieces are not generic over
// the model, so builders of any depth can hold them in one list.
internal sealed class BuildScope(
    IReadOnlyList<Action<FormFieldConventionContext>> conventions,
    IReadOnlyList<Func<FormMemberInfo, int?>> orders)
{
    public IReadOnlyList<Action<FormFieldConventionContext>> Conventions { get; } = conventions;

    public IReadOnlyList<Func<FormMemberInfo, int?>> Orders { get; } = orders;

    // The bindings of the fields built in this scope, in declaration order. The schema of the model reads them as IFieldBinding of its model.
    public List<object> Bindings { get; } = [];

    // The typed handles of the fields built in this scope, by key.
    public Dictionary<string, object> Handles { get; } = new(StringComparer.Ordinal);

    public int? OrderOf(FormMemberInfo member) => Orders.Select(o => o(member)).FirstOrDefault(o => o is not null);

    // Builds the items of a list in the order of their declaration, or of their Display order when a convention gives one.
    public List<FieldNode> BuildAll(IEnumerable<PendingItem> items)
    {
        var sorted = items.Select((item, index) => (item, index, order: item.OrderOf(this) ?? 10000)).OrderBy(x => x.order).ThenBy(x => x.index).Select(x => x.item);
        return sorted.Select(i => i.Build(this)).ToList();
    }
}

internal abstract class PendingItem
{
    public abstract FieldNode Build(BuildScope scope);

    public virtual int? OrderOf(BuildScope scope) => null;
}

// A field bound to a member through typed accessors or through reflection. TOwner is the type the member belongs to (the model, or the item of a list), and
// TRoot the model whose controller the conditions of the field receive.
internal sealed class PendingField<TOwner, TRoot, TValue>(
    FormField<TOwner, TValue> handle,
    FormMemberInfo member,
    FormFieldBuilder<TRoot, TValue> builder,
    Func<FormCodec<TValue>, IFieldBinding<TOwner>> bind,
    Func<FormFieldKind, FormCodec<TValue>>? codecFactory = null) : PendingItem
{
    public override int? OrderOf(BuildScope scope) => scope.OrderOf(member);

    public override FieldNode Build(BuildScope scope)
    {
        var provisional = builder.ExplicitKind ?? FormCodec<TValue>.DefaultKind
            ?? throw new ArgumentException($"The type {member.Type.Name} of '{member.Key}' has no default kind of field: choose one with Kind(...) or one of the As... methods.", nameof(member));
        var settings = new FieldSettings(member.Key);
        FormValueMap.ApplyDefaults(member, provisional, clearOptional: false, settings);
        var context = new FormFieldConventionContext(member, settings);
        foreach (var convention in scope.Conventions) convention(context);
        // The kind decides how the value is stored, so it is final once the conventions and the declaration have chosen.
        if (builder.ExplicitKind is { } explicitKind) settings.Kind = explicitKind;
        var codec = codecFactory?.Invoke(settings.Kind) ?? FormCodec<TValue>.For(settings.Kind, nullableReference: false);
        settings.Codec = codec;
        foreach (var step in builder.Steps) step(settings);
        codec.NullableReference = member.Type == typeof(string) && FormNullability.IsNullableReference(member.Property, settings.NullWhenEmpty);
        settings.ControlCheck = raw => codec.TryFromControl(raw, out _) == ControlValue.Invalid ? $"\"{member.Key}\" expects {FormValueMap.Expectation(member.Type)}." : null;
        settings.JsonReader = codec.TryReadJsonControl;
        var node = settings.ToNode();
        handle.Node = node;
        handle.Codec = codec;
        handle.Binding = bind(codec);
        scope.Bindings.Add(handle.Binding);
        scope.Handles[member.Key] = handle;
        return node;
    }
}

// A field of AllMembers: a member known only at run time, with no typed handle until the schema is asked for one.
internal sealed class PendingReflected<TModel>(FormMemberInfo member) : PendingItem
{
    public override int? OrderOf(BuildScope scope) => scope.OrderOf(member);

    public override FieldNode Build(BuildScope scope)
    {
        var kind = FormValueMap.DefaultKind(member.Type)!.Value;
        var settings = new FieldSettings(member.Key);
        FormValueMap.ApplyDefaults(member, kind, clearOptional: false, settings);
        var context = new FormFieldConventionContext(member, settings);
        foreach (var convention in scope.Conventions) convention(context);
        var nullableReference = member.Type == typeof(string) && FormNullability.IsNullableReference(member.Property, settings.NullWhenEmpty);
        var node = settings.ToNode();
        scope.Bindings.Add(new ReflectedBinding<TModel>(member, node.Kind, nullableReference));
        return node;
    }
}

// An element that has no value, or that only holds other elements: a heading, an info block, a group, a card, tabs, a button.
internal sealed class PendingNode<TModel>(FormItem<TModel> handle, Func<BuildScope, FieldNode> build) : PendingItem
{
    public override FieldNode Build(BuildScope scope)
    {
        var node = build(scope);
        handle.Node = node;
        scope.Handles[handle.Key] = handle;
        return node;
    }
}

internal sealed class PendingStep(string key, string title, string? description, List<PendingItem> items, Func<FormEngine, bool>? skipWhen)
{
    public StepNode Build(BuildScope scope) => new()
    {
        Key = key,
        Title = title,
        Description = description,
        Fields = scope.BuildAll(items),
        SkipWhen = skipWhen,
    };
}

internal sealed class PendingSection(string key, string label, string? description, bool optional, int span, List<PendingItem> items, Func<FormEngine, bool>? condition)
{
    public SectionNode Build(BuildScope scope) => new()
    {
        Key = key,
        Label = label,
        Description = description,
        Optional = optional,
        Span = span,
        Fields = scope.BuildAll(items),
        Condition = condition,
    };
}

internal sealed class PendingTab(string key, string label, List<PendingItem> items, Func<FormEngine, bool>? condition)
{
    public TabNode Build(BuildScope scope) => new()
    {
        Key = key,
        Label = label,
        Fields = scope.BuildAll(items),
        Condition = condition,
    };
}

// A repeatable list: the field of the list itself, and the columns of its rows, which are built with their own bindings over the item type.
internal sealed class PendingList<TModel, TItem>(
    FormListField<TModel, TItem> handle,
    FormMemberInfo member,
    FormFieldBuilder<TModel, IReadOnlyList<TItem>> builder,
    ListRows<TItem> rows,
    List<PendingItem> columns,
    ListCodec<TItem> codec,
    Func<FormCodec<IReadOnlyList<TItem>>, IFieldBinding<TModel>> bind) : PendingItem
{
    public override int? OrderOf(BuildScope scope) => scope.OrderOf(member);

    public override FieldNode Build(BuildScope scope)
    {
        var settings = new FieldSettings(member.Key);
        FormValueMap.ApplyDefaults(member, FormFieldKind.ArrayList, clearOptional: false, settings);
        var context = new FormFieldConventionContext(member, settings);
        foreach (var convention in scope.Conventions) convention(context);
        settings.Kind = FormFieldKind.ArrayList;
        settings.Codec = codec;
        foreach (var step in builder.Steps) step(settings);
        var rowScope = new BuildScope(scope.Conventions, scope.Orders);
        settings.Fields = rowScope.BuildAll(columns);
        foreach (var column in rowScope.Bindings) rows.Columns.Add((IFieldBinding<TItem>)column);
        var node = settings.ToNode();
        handle.Node = node;
        handle.Codec = codec;
        handle.Binding = bind(codec);
        scope.Bindings.Add(handle.Binding);
        scope.Handles[member.Key] = handle;
        return node;
    }
}

// A field the form has and the model does not: a confirmation, an acceptance of the terms. It is read and written through the controller only.
internal sealed class PendingUnbound<TModel, TValue>(
    FormField<TModel, TValue> handle,
    FormFieldBuilder<TModel, TValue> builder) : PendingItem
{
    public override FieldNode Build(BuildScope scope)
    {
        var kind = builder.ExplicitKind ?? FormCodec<TValue>.DefaultKind
            ?? throw new ArgumentException($"The type {typeof(TValue).Name} of '{handle.Key}' has no default kind of field: choose one with Kind(...) or one of the As... methods.", nameof(handle));
        var settings = new FieldSettings(handle.Key);
        FormValueMap.ApplyDefaults(handle.Key, typeof(TValue), kind, clearOptional: false, settings);
        var codec = FormCodec<TValue>.For(kind, nullableReference: false);
        settings.Codec = codec;
        foreach (var step in builder.Steps) step(settings);
        codec = FormCodec<TValue>.For(settings.Kind, nullableReference: false);
        settings.ControlCheck = raw => codec.TryFromControl(raw, out _) == ControlValue.Invalid ? $"\"{handle.Key}\" expects {FormValueMap.Expectation(typeof(TValue))}." : null;
        settings.JsonReader = (JsonElement element, FieldNode field, IReadOnlyList<FormOptionItem> options, out object? control, out string? reason) =>
            codec.TryReadJsonControl(element, field, options, out control, out reason);
        var node = settings.ToNode();
        handle.Node = node;
        handle.Codec = codec;
        scope.Handles[handle.Key] = handle;
        return node;
    }
}
