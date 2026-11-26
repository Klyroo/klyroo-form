using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Klyroo.Form;

internal static class FormModelTrim
{
    // What the binding reads and writes on a model: its public properties (getters, setters, init accessors) and its parameterless constructor.
    public const DynamicallyAccessedMemberTypes Members =
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
}

/// <summary>Declares the fields of a form from the members of a model type. Create the form with <see cref="FormModel.For{TModel}(Action{FormModelBuilder{TModel}})"/>.</summary>
/// <typeparam name="TModel">The model type.</typeparam>
public sealed class FormModelBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
    private sealed class Pending(FormMemberInfo member, FormFieldKind kind, Action<FormFieldSettings>[] explicitSteps)
    {
        public FormMemberInfo Member { get; } = member;
        public FormFieldKind Kind { get; } = kind;
        public Action<FormFieldSettings>[] Explicit { get; } = explicitSteps;
    }

    // Model fields and plain fields in the order they were declared.
    private readonly List<object> _items = [];
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);
    private readonly List<FormFieldConvention> _conventions = [];
    private readonly List<FormModelValidator<TModel>> _validators = [];
    private readonly List<Func<FormMemberInfo, int?>> _orders = [];
    private readonly Dictionary<string, Func<object>> _creators = new(StringComparer.Ordinal);
    private Func<TModel>? _factory;
    private Func<FormController, Task<bool>>? _beforeSubmit;
    private Func<FormController, Task>? _onSubmit;

    internal FormModelBuilder()
    {
    }

    /// <summary>The <see cref="FormSchema.FormKey"/> of the schema. The default is <c>form</c>.</summary>
    public string FormKey { get; set; } = "form";

    /// <summary>The <see cref="FormSchema.Title"/> of the schema.</summary>
    public string? Title { get; set; }

    /// <summary>The <see cref="FormSchema.Description"/> of the schema.</summary>
    public string? Description { get; set; }

    /// <summary>The <see cref="FormSchema.Eyebrow"/> of the schema.</summary>
    public string? Eyebrow { get; set; }

    /// <summary>The <see cref="FormSchema.Columns"/> of the schema. The default is 12.</summary>
    public int Columns { get; set; } = 12;

    /// <summary>The <see cref="FormSchema.ValidateMode"/> of the schema.</summary>
    public FormValidationMode ValidateMode { get; set; } = FormValidationMode.All;

    /// <summary>The <see cref="FormSchema.Messages"/> of the schema, or <c>null</c> for the messages of the locale.</summary>
    public FormMessages? Messages { get; set; }

    /// <summary>Declares a field for a member. The key is the member name, or the names down to it joined with dots for a nested member (<c>p =&gt; p.Address.City</c> gives <c>Address.City</c>). The kind, label and bounds come from the member type, see <see cref="FormFieldBuilder{TValue}"/> to change them.</summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="member">A member access such as <c>p =&gt; p.Name</c>, or a chain of them. Nothing else is accepted, and the expression is never compiled.</param>
    /// <param name="configure">Adjusts the field.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException">The expression is not a chain of property accesses, or the member type has no default kind and none was given.</exception>
    public FormModelBuilder<TModel> Field<TValue>(Expression<Func<TModel, TValue>> member, Action<FormFieldBuilder<TValue>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        var path = FormModelPath.Resolve(member, typeof(TValue));
        var field = new FormFieldBuilder<TValue>();
        configure?.Invoke(field);
        AddMember(path, field.Steps);
        return this;
    }

    /// <summary>Declares the fields of the members of a nested object, with the name of the object as the prefix of their keys. The object is created with its parameterless constructor when it is <c>null</c> and a value must be written, which is also what keeps the type safe to trim.</summary>
    /// <typeparam name="TNested">The type of the nested object.</typeparam>
    /// <param name="member">The member that holds the object, such as <c>p =&gt; p.Address</c>.</param>
    /// <param name="configure">Declares the fields of the nested members.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> Nested<[DynamicallyAccessedMembers(FormModelTrim.Members)] TNested>(Expression<Func<TModel, TNested>> member, Action<FormNestedBuilder<TModel, TNested>> configure)
        where TNested : class
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(configure);
        var prefix = FormModelPath.Resolve(member, typeof(TNested));
        RegisterCreator(prefix, () => Activator.CreateInstance(typeof(TNested))!);
        configure(new FormNestedBuilder<TModel, TNested>(this, prefix));
        return this;
    }

    /// <summary>Declares a field for every public readable and writable member of the model that is not declared yet, when its type has a default kind (string, numbers, <see cref="bool"/>, enums, dates, times, <see cref="Guid"/> and lists of strings). Members of other types are skipped. Call it after the explicit <c>Field</c> calls to keep their order, or alone for a form that is described by attributes.</summary>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> AllMembers()
    {
        foreach (var property in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null || property.SetMethod is null) continue;
            if (_keys.Contains(property.Name) || FormValueMap.DefaultKind(property.PropertyType) is null) continue;
            AddMember(new FormModelPath([property]), []);
        }
        return this;
    }

    /// <summary>Adds a field that is not bound to a member, at this position of the form. Its value is in the output of the controller but never reaches the model.</summary>
    /// <param name="field">The field.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> Add(FormFieldSchema field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!_keys.Add(field.Key)) throw new ArgumentException($"A field with the key '{field.Key}' is already declared.", nameof(field));
        _items.Add(field);
        return this;
    }

    /// <summary>Adds a convention that adjusts every field declared from a member (see <see cref="FormFieldConvention"/>).</summary>
    /// <param name="convention">The convention.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> Convention(FormFieldConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        _conventions.Add(convention);
        return this;
    }

    /// <summary>Adds a check of the whole model, run at submit after the field rules passed. The model it receives is built from the values of the form, see <see cref="Factory"/>.</summary>
    /// <param name="validator">The check.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> Validate(FormModelValidator<TModel> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validators.Add(validator);
        return this;
    }

    /// <summary>Gives the position of a member in the form, or <c>null</c> to keep the order of declaration. Fields are sorted by it (stable), and fields without a position, plain fields included, count as 10000.</summary>
    /// <param name="order">Returns the position of a member.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> Order(Func<FormMemberInfo, int?> order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _orders.Add(order);
        return this;
    }

    /// <summary>Sets how a new model is created from the form values (<see cref="FormModel{TModel}.Create(FormController)"/>, the typed submit and the model checks). The default is the parameterless constructor of the model. A record with required constructor parameters needs this.</summary>
    /// <param name="factory">Creates an empty model.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> Factory(Func<TModel> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        return this;
    }

    /// <summary>Sets <see cref="FormSchema.OnBeforeSubmit"/>. It runs after the model checks passed.</summary>
    /// <param name="hook">The hook.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> OnBeforeSubmit(Func<FormController, Task<bool>> hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _beforeSubmit = hook;
        return this;
    }

    /// <summary>Sets <see cref="FormSchema.OnSubmit"/>.</summary>
    /// <param name="hook">The hook.</param>
    /// <returns>This builder.</returns>
    public FormModelBuilder<TModel> OnSubmit(Func<FormController, Task> hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _onSubmit = hook;
        return this;
    }

    internal void AddMember(FormModelPath path, List<Action<FormFieldSettings>> explicitSteps)
    {
        var info = new FormMemberInfo(path.Key, path.Properties, typeof(TModel));
        var kind = FormValueMap.DefaultKind(info.Type);
        var explicitKind = ExplicitKind(explicitSteps);
        if (kind is null && explicitKind is null)
        {
            throw new ArgumentException($"The type {info.Type.Name} of '{info.Key}' has no default kind of field: call Kind(...) to choose one, or declare the field with a FormFieldSchema.", nameof(path));
        }
        if (!_keys.Add(info.Key)) throw new ArgumentException($"A field with the key '{info.Key}' is already declared.", nameof(path));
        _items.Add(new Pending(info, kind ?? explicitKind!.Value, [.. explicitSteps]));
    }

    internal void RegisterCreator(FormModelPath prefix, Func<object> create) => _creators[prefix.Key] = create;

    // The kind the explicit settings choose, found by running them on a scratch copy.
    private static FormFieldKind? ExplicitKind(List<Action<FormFieldSettings>> steps)
    {
        if (steps.Count == 0) return null;
        var probe = new FormFieldSettings("probe") { Kind = (FormFieldKind)(-1) };
        foreach (var step in steps) step(probe);
        return probe.Kind == (FormFieldKind)(-1) ? null : probe.Kind;
    }

    internal FormModel<TModel> Build()
    {
        var fields = new List<(FormFieldSchema Schema, FormFieldBinding? Binding, int Order)>();
        foreach (var item in _items)
        {
            if (item is FormFieldSchema plain)
            {
                fields.Add((plain, null, 10000));
                continue;
            }
            var pending = (Pending)item;
            var settings = new FormFieldSettings(pending.Member.Key);
            var nullableReference = pending.Member.Type == typeof(string) && IsNullableReference(pending.Member.Property);
            FormValueMap.ApplyDefaults(pending.Member, pending.Kind, clearOptional: false, settings);
            foreach (var convention in _conventions) convention(pending.Member, settings);
            foreach (var step in pending.Explicit) step(settings);
            var schema = settings.ToSchema();
            var order = _orders.Select(o => o(pending.Member)).FirstOrDefault(o => o is not null) ?? 10000;
            fields.Add((schema, new FormFieldBinding(pending.Member, schema.Kind, nullableReference), order));
        }
        var sorted = fields.Select((f, i) => (f, i)).OrderBy(x => x.f.Order).ThenBy(x => x.i).Select(x => x.f).ToList();
        return new FormModel<TModel>(
            sorted.Select(f => f.Schema).ToList(),
            sorted.Where(f => f.Binding is not null).Select(f => f.Binding!).ToList(),
            [.. _validators],
            new Dictionary<string, Func<object>>(_creators, StringComparer.Ordinal),
            _factory ?? DefaultFactory(),
            new FormModelSchemaOptions(FormKey, Title, Description, Eyebrow, Columns, ValidateMode, Messages, _beforeSubmit, _onSubmit));
    }

    private static bool IsNullableReference(PropertyInfo property) =>
        new NullabilityInfoContext().Create(property).WriteState == NullabilityState.Nullable;

    private static Func<TModel>? DefaultFactory() =>
        typeof(TModel).IsValueType || typeof(TModel).GetConstructor(Type.EmptyTypes) is not null
            ? () => Activator.CreateInstance<TModel>()
            : null;
}

/// <summary>Declares the fields of the members of a nested object. Obtained from <see cref="FormModelBuilder{TModel}.Nested{TNested}"/>.</summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TNested">The type of the nested object.</typeparam>
public sealed class FormNestedBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, [DynamicallyAccessedMembers(FormModelTrim.Members)] TNested>
{
    private readonly FormModelBuilder<TModel> _owner;
    private readonly FormModelPath _prefix;

    internal FormNestedBuilder(FormModelBuilder<TModel> owner, FormModelPath prefix)
    {
        _owner = owner;
        _prefix = prefix;
    }

    /// <summary>Declares a field for a member of the nested object. The key is the key of the nested object and the member name, joined with a dot.</summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="member">A member access such as <c>a =&gt; a.City</c>, or a chain of them.</param>
    /// <param name="configure">Adjusts the field.</param>
    /// <returns>This builder.</returns>
    public FormNestedBuilder<TModel, TNested> Field<TValue>(Expression<Func<TNested, TValue>> member, Action<FormFieldBuilder<TValue>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        var path = _prefix.Append(FormModelPath.Resolve(member, typeof(TValue)));
        var field = new FormFieldBuilder<TValue>();
        configure?.Invoke(field);
        _owner.AddMember(path, field.Steps);
        return this;
    }

    /// <summary>Declares the fields of a member of the nested object that is an object itself.</summary>
    /// <typeparam name="TInner">The type of the inner object.</typeparam>
    /// <param name="member">The member that holds the object.</param>
    /// <param name="configure">Declares the fields of the inner members.</param>
    /// <returns>This builder.</returns>
    public FormNestedBuilder<TModel, TNested> Nested<[DynamicallyAccessedMembers(FormModelTrim.Members)] TInner>(Expression<Func<TNested, TInner>> member, Action<FormNestedBuilder<TModel, TInner>> configure)
        where TInner : class
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(configure);
        var prefix = _prefix.Append(FormModelPath.Resolve(member, typeof(TInner)));
        _owner.RegisterCreator(prefix, () => Activator.CreateInstance(typeof(TInner))!);
        configure(new FormNestedBuilder<TModel, TInner>(_owner, prefix));
        return this;
    }
}

internal sealed class FormModelPath(IReadOnlyList<PropertyInfo> properties)
{
    public IReadOnlyList<PropertyInfo> Properties { get; } = properties;

    public string Key => string.Join('.', Properties.Select(p => p.Name));

    public FormModelPath Append(FormModelPath tail) => new([.. Properties, .. tail.Properties]);

    // A member access, or a chain of them, from the parameter of the lambda. The expression is read, never compiled.
    public static FormModelPath Resolve(LambdaExpression lambda, Type expected)
    {
        var chain = new List<PropertyInfo>();
        var node = lambda.Body;
        while (node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary) node = unary.Operand;
        while (node is MemberExpression member)
        {
            if (member.Member is not PropertyInfo property)
            {
                throw new ArgumentException($"'{member.Member.Name}' is a field: only properties can be bound.", nameof(lambda));
            }
            chain.Insert(0, property);
            node = member.Expression;
        }
        if (node is not ParameterExpression || chain.Count == 0)
        {
            throw new ArgumentException("Use a member access such as p => p.Name, or a chain such as p => p.Address.City.", nameof(lambda));
        }
        if (chain[^1].PropertyType != expected)
        {
            throw new ArgumentException($"'{chain[^1].Name}' is a {chain[^1].PropertyType.Name}, not a {expected.Name}.", nameof(lambda));
        }
        for (var i = 0; i < chain.Count - 1; i++)
        {
            if (chain[i].PropertyType.IsValueType)
            {
                throw new ArgumentException($"'{chain[i].Name}' is a struct: only members of a class can be bound through it.", nameof(lambda));
            }
        }
        return new FormModelPath(chain);
    }
}
