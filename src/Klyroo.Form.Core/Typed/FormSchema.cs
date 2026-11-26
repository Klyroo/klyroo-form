using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Klyroo.Form;

/// <summary>Entry point of the typed forms: declares a <see cref="FormSchema{TModel}"/> from a C# model type.</summary>
public static class FormSchema
{
    /// <summary>Declares a form from the members of <typeparamref name="TModel"/>.</summary>
    /// <typeparam name="TModel">The model type, a class or a record. Its public properties and parameterless constructor are kept when an app is trimmed. A struct is refused: a form writes members into the object it is given, and a struct would be copied on the way, so the caller would never see the values.</typeparam>
    /// <param name="configure">Declares the fields, and sets the title and the other settings of the form.</param>
    /// <returns>The schema: immutable, and shared by every controller of the form.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="TModel"/> is a struct, or a member cannot be bound.</exception>
    /// <exception cref="InvalidOperationException">The form has model checks (<c>Validate</c>, <c>UseDataAnnotations</c>) and the model has no parameterless constructor and no <c>Factory</c>.</exception>
    /// <example><code>
    /// var schema = FormSchema.For&lt;Signup&gt;(s =&gt;
    /// {
    ///     var company = s.Field(m =&gt; m.IsCompany);
    ///     s.Field(m =&gt; m.CompanyName, f =&gt; f.Required().VisibleWhen(c =&gt; c.Get(company)));
    ///     s.Field(m =&gt; m.Seats, f =&gt; f.Range(1, 500));
    /// });
    /// </code></example>
    public static FormSchema<TModel> For<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>(Action<FormSchemaBuilder<TModel>> configure)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (typeof(TModel).IsValueType)
        {
            throw new ArgumentException($"{typeof(TModel).Name} is a struct: a form writes the values into the object it is given, and a struct is copied on the way. Use a class or a record.", nameof(TModel));
        }
        var builder = new FormSchemaBuilder<TModel>();
        configure(builder);
        return builder.Build();
    }
}

/// <summary>The declaration of a typed form: its fields, steps or sections, rules and hooks, bound to the members of <typeparamref name="TModel"/>. A schema is immutable and holds no values: share one between every controller of the form, and read a field of a controller with the handle <c>Field</c> returns.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class FormSchema<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
    private readonly Dictionary<string, object> _handles;
    private readonly Dictionary<string, FieldNode> _nodes;
    private readonly BuilderState<TModel> _state;

    private FormSchema(BuilderState<TModel> state, FormDefinition definition, List<IFieldBinding<TModel>> bindings, Dictionary<string, object> handles, Func<TModel>? factory)
    {
        _state = state;
        Definition = definition;
        Bindings = bindings;
        _handles = handles;
        Factory = factory;
        _nodes = [];
        foreach (var node in definition.AllNodes()) _nodes[node.Key] = node;
        Writable = bindings.Where(b => b.CanWrite).ToList();
    }

    internal FormDefinition Definition { get; }

    internal IReadOnlyList<IFieldBinding<TModel>> Bindings { get; }

    internal IReadOnlyList<IFieldBinding<TModel>> Writable { get; }

    internal Func<TModel>? Factory { get; }

    internal BuilderState<TModel> State => _state;

    /// <summary>The identifier of the form.</summary>
    public string FormKey => Definition.FormKey;

    /// <summary>The title of the form, or <c>null</c>.</summary>
    public string? Title => Definition.Title;

    /// <summary>Gets the handle of a field declared with a member of the model, to read and write it through a controller from code that did not keep the handle <c>Field</c> returned.</summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="member">The member, as in <c>m =&gt; m.Email</c>.</param>
    /// <returns>The handle declared for it.</returns>
    /// <exception cref="ArgumentException">No field is declared for the member, or its type is not <typeparamref name="TValue"/>.</exception>
    public FormField<TModel, TValue> Field<TValue>(Expression<Func<TModel, TValue>> member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var key = FormModelPath.Resolve(member, typeof(TValue)).Key;
        if (_handles.TryGetValue(key, out var handle))
        {
            return handle as FormField<TModel, TValue> ?? throw new ArgumentException($"'{key}' is not a field of {typeof(TValue).Name}.", nameof(member));
        }
        // A field of AllMembers has a type known only at run time: it gets its typed view here, the first time it is asked for.
        if (Bindings.OfType<ReflectedBinding<TModel>>().FirstOrDefault(b => b.Key == key) is { } reflected && reflected.Member.Type == typeof(TValue))
        {
            lock (_handles)
            {
                if (_handles.TryGetValue(key, out var existing)) return (FormField<TModel, TValue>)existing;
                var codec = FormCodec<TValue>.For(reflected.Kind, reflected.NullableReference);
                var view = new FormField<TModel, TValue>(key) { Node = _nodes[key], Codec = codec, Binding = reflected };
                _handles[key] = view;
                return view;
            }
        }
        throw new ArgumentException($"No field is declared for '{key}'.", nameof(member));
    }

    /// <summary>Gets the handle of a repeatable list declared with a collection member of the model.</summary>
    /// <typeparam name="TItem">The type of a row.</typeparam>
    /// <param name="member">The collection member, as in <c>m =&gt; m.Lines</c>.</param>
    /// <returns>The handle declared for it.</returns>
    /// <exception cref="ArgumentException">No list is declared for the member.</exception>
    public FormListField<TModel, TItem> List<TItem>(Expression<Func<TModel, IEnumerable<TItem>>> member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var key = FormModelPath.Resolve(member, null).Key;
        return _handles.TryGetValue(key, out var handle) && handle is FormListField<TModel, TItem> list
            ? list
            : throw new ArgumentException($"No list of {typeof(TItem).Name} is declared for '{key}'.", nameof(member));
    }

    /// <summary>Gets the handle of an element by its key: a field (its member names joined with dots) or a layout element (the key it was declared with).</summary>
    /// <param name="key">The key.</param>
    /// <returns>The handle declared for it.</returns>
    /// <exception cref="ArgumentException">No element has this key.</exception>
    public FormItem<TModel> Item(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _handles.TryGetValue(key, out var handle) && handle is FormItem<TModel> item
            ? item
            : throw new ArgumentException($"No element is declared with the key '{key}'.", nameof(key));
    }

    internal bool TryFindNode(string key, [NotNullWhen(true)] out FieldNode? node) => _nodes.TryGetValue(key, out node);

    internal static FormSchema<TModel> Build(BuilderState<TModel> state, List<PendingItem> items)
    {
        var scope = new BuildScope(state.Conventions, state.Orders);
        var fields = scope.BuildAll(items);
        var steps = state.Steps.Select(s => s.Build(scope)).ToList();
        var sections = state.Sections.Select(s => s.Build(scope)).ToList();
        var factory = state.Factory ?? DefaultFactory();
        if (factory is null && state.Validators.Count > 0)
        {
            throw new InvalidOperationException($"{typeof(TModel).Name} has no parameterless constructor and the form has model checks (Validate or UseDataAnnotations), which run on a model built from the values: call Factory(...) to say how one is created.");
        }
        var definition = new FormDefinition
        {
            FormKey = state.FormKey,
            Title = state.Title,
            Description = state.Description,
            Eyebrow = state.Eyebrow,
            Columns = state.Columns,
            ShowStepper = state.ShowStepper,
            ValidateMode = state.ValidateMode,
            Messages = state.Messages,
            IgnoreDirtyPaths = state.IgnoreDirty.Count == 0 ? null : state.IgnoreDirty,
            Fields = fields,
            Steps = steps,
            Sections = sections,
            OnBeforeNext = state.BeforeNext is { } next ? (e, i) => next((FormController<TModel>)e.Owner!, i) : null,
            OnBeforePrevious = state.BeforePrevious is { } previous ? (e, i) => previous((FormController<TModel>)e.Owner!, i) : null,
        };
        var handles = new Dictionary<string, object>(scope.Handles, StringComparer.Ordinal);
        return new FormSchema<TModel>(state, definition, scope.Bindings.Cast<IFieldBinding<TModel>>().ToList(), handles, factory);
    }

    private static Func<TModel>? DefaultFactory() =>
        typeof(TModel).GetConstructor(Type.EmptyTypes) is not null ? () => Activator.CreateInstance<TModel>() : null;

    // The values of a model as the controls store them. With skipDefaulted, a field that declares a default is left out, so the default is used instead of the
    // zero value of a model that was only created to hold something.
    internal Dictionary<string, object?> ToInput(TModel model, bool skipDefaulted = false)
    {
        var input = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var binding in Bindings)
        {
            if (skipDefaulted && _nodes.TryGetValue(binding.Key, out var node) && (node.DefaultValue is not null || node.DefaultFactory is not null)) continue;
            input[binding.Key] = binding.ReadControl(model);
        }
        return input;
    }

    // Everything that can fail on the values or the shape of the model happens here, before the model is changed.
    internal FormWritePlan PlanWrite(IReadOnlyDictionary<string, object?> values, TModel target)
    {
        var plan = new FormWritePlan();
        foreach (var binding in Writable)
        {
            if (values.TryGetValue(binding.Key, out var raw)) binding.Plan(target, raw, _state.Creators, plan);
        }
        return plan;
    }

    internal TModel Create(IReadOnlyDictionary<string, object?> values)
    {
        var model = Factory is null
            ? throw new InvalidOperationException($"{typeof(TModel).Name} has no parameterless constructor: call Factory(...) on the schema builder to say how a new one is created.")
            : Factory();
        PlanWrite(values, model).Apply();
        return model;
    }
}
