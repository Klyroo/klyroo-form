using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Klyroo.Form;

// What every builder of one schema shares: the keys taken, the conventions, the checks and the hooks.
internal sealed class BuilderState<TModel>
{
    public HashSet<string> Keys { get; } = new(StringComparer.Ordinal);

    public List<Action<FormFieldConventionContext>> Conventions { get; } = [];

    public List<Func<FormMemberInfo, int?>> Orders { get; } = [];

    public List<Action<TModel, FormModelErrors<TModel>>> Validators { get; } = [];

    public Dictionary<string, Func<object>> Creators { get; } = new(StringComparer.Ordinal);

    public Func<TModel>? Factory { get; set; }

    public List<PendingStep> Steps { get; } = [];

    public List<PendingSection> Sections { get; } = [];

    public string FormKey { get; set; } = "form";

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Eyebrow { get; set; }

    public int Columns { get; set; } = 12;

    public bool ShowStepper { get; set; } = true;

    public FormValidationMode ValidateMode { get; set; } = FormValidationMode.All;

    public FormMessages? Messages { get; set; }

    public List<string> IgnoreDirty { get; } = [];

    public Func<TModel, FormController<TModel>, Task<bool>>? BeforeSubmit { get; set; }

    public Func<TModel, FormController<TModel>, Task>? OnSubmit { get; set; }

    public Func<FormController<TModel>, int, Task<bool>>? BeforeNext { get; set; }

    public Func<FormController<TModel>, int, Task<bool>>? BeforePrevious { get; set; }

    public void Claim(string key)
    {
        if (!Keys.Add(key)) throw new ArgumentException($"A field or element with the key '{key}' is already declared.", nameof(key));
    }
}

/// <summary>Declares the fields and layout of one block of a typed form: the form itself, a step, a section, a group, a card or a tab. Every method returns the handle of what it declared, so a later condition can point at an earlier field through a captured local. The fields are read with <see cref="FormController{TModel}"/>.</summary>
/// <typeparam name="TModel">The model the form is declared for. A class or a record; its public properties and parameterless constructor are kept when an app is trimmed.</typeparam>
/// <typeparam name="THolder">The type whose members this block declares: the model itself, or the type of a group.</typeparam>
public abstract class FormScopeBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, [DynamicallyAccessedMembers(FormModelTrim.Members)] THolder>
{
    private readonly HolderPath<TModel, THolder> _holder;
    private readonly IReadOnlyList<PropertyInfo> _prefix;

    internal FormScopeBuilder(BuilderState<TModel> state, HolderPath<TModel, THolder> holder, IReadOnlyList<PropertyInfo> prefix, List<PendingItem> items)
    {
        State = state;
        _holder = holder;
        _prefix = prefix;
        Items = items;
    }

    internal BuilderState<TModel> State { get; }

    internal List<PendingItem> Items { get; }

    private string PrefixKey => string.Join('.', _prefix.Select(p => p.Name));

    private string KeyOf(string name) => _prefix.Count == 0 ? name : PrefixKey + "." + name;

    /// <summary>Declares a field bound to a member of the model.</summary>
    /// <typeparam name="TValue">The type of the member, which decides the control the field gets by default: a text input for a string, a number input for a number, a checkbox for a boolean, a select list for an enum, a date input for a date.</typeparam>
    /// <param name="member">The member, as in <c>m =&gt; m.Email</c>. A chain such as <c>m =&gt; m.Address.City</c> is accepted, and is written to the model only when <see cref="Group{TNested}"/> declared the object on the way.</param>
    /// <param name="configure">Sets the label, rules and conditions of the field.</param>
    /// <returns>The handle of the field.</returns>
    /// <exception cref="ArgumentException">The member is not a property, its type has no default kind of control (choose one with <c>Kind</c>), or a field with its key is already declared.</exception>
    public FormField<TModel, TValue> Field<TValue>(Expression<Func<THolder, TValue>> member, Action<FormFieldBuilder<TModel, TValue>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        var path = FormModelPath.Resolve(member, typeof(TValue));
        var properties = new List<PropertyInfo>(_prefix);
        properties.AddRange(path.Properties);
        var info = new FormMemberInfo(string.Join('.', properties.Select(p => p.Name)), properties, typeof(TModel));
        State.Claim(info.Key);
        var handle = new FormField<TModel, TValue>(info.Key);
        var builder = new FormFieldBuilder<TModel, TValue>(handle);
        configure?.Invoke(builder);
        Func<FormCodec<TValue>, IFieldBinding<TModel>> bind;
        if (path.Properties.Count == 1)
        {
            var property = path.Properties[0];
            var get = Accessors.Getter<THolder, TValue>(property);
            var set = Accessors.Setter<THolder, TValue>(property);
            bind = codec => new TypedBinding<TModel, THolder, TValue>(_holder, info, get, set, codec);
        }
        else
        {
            bind = codec => new ReflectedBinding<TModel>(info, codec.Kind, codec.NullableReference);
        }
        Items.Add(new PendingField<TModel, TModel, TValue>(handle, info, builder, bind));
        return handle;
    }

    /// <summary>Declares a field that the form has and the model does not, such as a confirmation or the acceptance of the terms. It is read and written through the controller like any field, validated and part of the draft, and left out of the model.</summary>
    /// <typeparam name="TValue">The type of the value, which decides the control the field gets by default.</typeparam>
    /// <param name="key">The key of the field, unique in the form.</param>
    /// <param name="configure">Sets the label, rules and conditions of the field.</param>
    /// <returns>The handle of the field.</returns>
    public FormField<TModel, TValue> Field<TValue>(string key, Action<FormFieldBuilder<TModel, TValue>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        State.Claim(key);
        var handle = new FormField<TModel, TValue>(key);
        var builder = new FormFieldBuilder<TModel, TValue>(handle);
        configure?.Invoke(builder);
        Items.Add(new PendingUnbound<TModel, TValue>(handle, builder));
        return handle;
    }

    /// <summary>Declares a field for a collection member as a multiple choice: the user ticks several options and the member holds their values.</summary>
    /// <typeparam name="TElement">The type of one choice.</typeparam>
    /// <param name="member">The collection member: a <c>List</c>, an array, or <c>IList</c>, <c>ICollection</c>, <c>IReadOnlyList</c>, <c>IReadOnlyCollection</c> or <c>IEnumerable</c> of the element type.</param>
    /// <param name="options">The choices.</param>
    /// <param name="configure">Sets the label, rules and conditions of the field.</param>
    /// <returns>The handle of the field, whose value is the list of the chosen values.</returns>
    public FormField<TModel, IReadOnlyList<TElement>> MultiSelect<TElement>(
        Expression<Func<THolder, IEnumerable<TElement>>> member,
        IEnumerable<FormOption<TElement>> options,
        Action<FormFieldBuilder<TModel, IReadOnlyList<TElement>>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(options);
        var path = FormModelPath.Resolve(member, null);
        var info = MemberOf(path);
        State.Claim(info.Key);
        var handle = new FormField<TModel, IReadOnlyList<TElement>>(info.Key);
        var builder = new FormFieldBuilder<TModel, IReadOnlyList<TElement>>(handle);
        builder.SetKind(FormFieldKind.Select, s => s.Multiple = true);
        configure?.Invoke(builder);
        var list = options.ToList();
        builder.Add(s =>
        {
            var element = FormCodec<TElement>.For(FormFieldKind.Select, nullableReference: false);
            s.Options = list.Select(o => FormFieldBuilderExtensions.Item(element.Format(o.Value), o)).ToList();
        });
        var make = CollectionShape.Maker<TElement>(info.Type, info.Key);
        Items.Add(new PendingField<TModel, TModel, IReadOnlyList<TElement>>(handle, info, builder, codec => ListBinding(info, path, codec, make), _ => new MultiCodec<TElement>(FormCodec<TElement>.For(FormFieldKind.Select, nullableReference: false))));
        return handle;
    }

    /// <summary>Declares a repeatable list of rows for a collection member. Each row is an item of the collection; its columns are declared in <paramref name="columns"/>.</summary>
    /// <typeparam name="TItem">The type of a row: a class or a record with a parameterless constructor, unless <paramref name="itemFactory"/> says how to create one.</typeparam>
    /// <param name="member">The collection member: a <c>List</c>, an array, or <c>IList</c>, <c>ICollection</c>, <c>IReadOnlyList</c>, <c>IReadOnlyCollection</c> or <c>IEnumerable</c> of the item type.</param>
    /// <param name="columns">Declares the columns of a row.</param>
    /// <param name="addLabel">The text of the add button. The default is <c>Add item</c>.</param>
    /// <param name="configure">Sets the label and conditions of the list itself.</param>
    /// <param name="itemFactory">Creates a new item when the model is written. The default is the parameterless constructor.</param>
    /// <returns>The handle of the list, whose value is the list of its items.</returns>
    public FormListField<TModel, TItem> List<[DynamicallyAccessedMembers(FormModelTrim.Members)] TItem>(
        Expression<Func<THolder, IEnumerable<TItem>>> member,
        Action<FormRowBuilder<TModel, TItem>> columns,
        string? addLabel = null,
        Action<FormFieldBuilder<TModel, IReadOnlyList<TItem>>>? configure = null,
        Func<TItem>? itemFactory = null)
        where TItem : class
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(columns);
        var path = FormModelPath.Resolve(member, null);
        var info = MemberOf(path);
        State.Claim(info.Key);
        var handle = new FormListField<TModel, TItem>(info.Key);
        var rows = new ListRows<TItem> { Factory = itemFactory ?? DefaultFactory<TItem>() };
        handle.Rows = rows;
        var builder = new FormFieldBuilder<TModel, IReadOnlyList<TItem>>(handle);
        builder.SetKind(FormFieldKind.ArrayList, s =>
        {
            if (addLabel is not null) s.AddItemLabel = addLabel;
        });
        configure?.Invoke(builder);
        var rowBuilder = new FormRowBuilder<TModel, TItem>(State);
        columns(rowBuilder);
        var make = CollectionShape.Maker<TItem>(info.Type, info.Key);
        var codec = new ListCodec<TItem>(rows);
        Items.Add(new PendingList<TModel, TItem>(handle, info, builder, rows, rowBuilder.Items, codec, c => ListBinding(info, path, c, make)));
        return handle;
    }

    /// <summary>Declares a group of fields for an object member of the model, laid out together. The fields are declared on the type of the member and keep the model-wide keys (<c>Address.City</c>). A null object is created when the model is written.</summary>
    /// <typeparam name="TNested">The type of the member: a class with a parameterless constructor.</typeparam>
    /// <param name="member">The object member, as in <c>m =&gt; m.Address</c>.</param>
    /// <param name="configure">Declares the fields of the group.</param>
    /// <param name="label">The heading of the group.</param>
    /// <param name="description">The text under the heading.</param>
    /// <param name="card"><c>true</c> to draw a border around the group.</param>
    /// <param name="collapsible"><c>true</c> to let the user collapse the group.</param>
    /// <returns>The handle of the group.</returns>
    public FormItem<TModel> Group<[DynamicallyAccessedMembers(FormModelTrim.Members)] TNested>(
        Expression<Func<THolder, TNested>> member,
        Action<FormGroupBuilder<TModel, TNested>> configure,
        string? label = null,
        string? description = null,
        bool card = false,
        bool collapsible = false)
        where TNested : class
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(configure);
        var path = FormModelPath.Resolve(member, typeof(TNested));
        if (path.Properties.Count != 1)
        {
            throw new ArgumentException("A group takes a direct member of the model, such as m => m.Address. Declare a nested group inside it.", nameof(member));
        }
        var property = path.Properties[0];
        var info = MemberOf(path);
        State.Claim(info.Key);
        State.Creators[info.Key] = static () => Activator.CreateInstance(typeof(TNested))!;
        var nested = new NestedHolder<TModel, THolder, TNested>(
            _holder,
            Accessors.Getter<THolder, TNested?>(property),
            Accessors.Setter<THolder, TNested>(property),
            static () => Activator.CreateInstance<TNested>(),
            info.Key);
        var items = new List<PendingItem>();
        configure(new FormGroupBuilder<TModel, TNested>(State, nested, info.Path, items));
        var handle = new FormItem<TModel>(info.Key);
        Items.Add(new PendingNode<TModel>(handle, scope => new FieldNode
        {
            Key = info.Key,
            Kind = card ? FormFieldKind.Card : FormFieldKind.Group,
            Label = label,
            Description = description,
            Collapsible = collapsible,
            Fields = scope.BuildAll(items),
        }));
        return handle;
    }

    /// <summary>Declares a bordered container for fields of the same model, to lay them out together.</summary>
    /// <param name="key">A key for the card, unique in the form.</param>
    /// <param name="label">The heading of the card.</param>
    /// <param name="configure">Declares the fields of the card.</param>
    /// <param name="description">The text under the heading.</param>
    /// <param name="collapsible"><c>true</c> to let the user collapse the card.</param>
    /// <param name="visibleWhen">Hides the card, and its fields with it, while it returns <c>false</c>.</param>
    /// <returns>The handle of the card.</returns>
    public FormItem<TModel> Card(
        string key,
        string label,
        Action<FormGroupBuilder<TModel, THolder>> configure,
        string? description = null,
        bool collapsible = false,
        Func<FormController<TModel>, bool>? visibleWhen = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(configure);
        State.Claim(key);
        var items = new List<PendingItem>();
        configure(new FormGroupBuilder<TModel, THolder>(State, _holder, _prefix, items));
        var handle = new FormItem<TModel>(key);
        Items.Add(new PendingNode<TModel>(handle, scope => new FieldNode
        {
            Key = key,
            Kind = FormFieldKind.Card,
            Label = label,
            Description = description,
            Collapsible = collapsible,
            Condition = Visible(visibleWhen),
            Fields = scope.BuildAll(items),
        }));
        return handle;
    }

    /// <summary>Declares a tabbed container for fields of the same model. The fields of every visible tab are validated and written to the model, whether or not the tab is selected.</summary>
    /// <param name="key">A key for the tabs, unique in the form.</param>
    /// <param name="configure">Declares the tabs.</param>
    /// <returns>The handle of the tabs.</returns>
    public FormItem<TModel> Tabs(string key, Action<FormTabsBuilder<TModel, THolder>> configure)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(configure);
        State.Claim(key);
        var tabs = new FormTabsBuilder<TModel, THolder>(State, _holder, _prefix);
        configure(tabs);
        var handle = new FormItem<TModel>(key);
        Items.Add(new PendingNode<TModel>(handle, scope => new FieldNode
        {
            Key = key,
            Kind = FormFieldKind.Tabs,
            Tabs = tabs.Tabs.Select(t => t.Build(scope)).ToList(),
        }));
        return handle;
    }

    /// <summary>Declares a heading made of a label and a description.</summary>
    /// <param name="key">A key for the heading, unique in the form.</param>
    /// <param name="label">The heading.</param>
    /// <param name="description">The text under the heading.</param>
    /// <param name="visibleWhen">Hides the heading while it returns <c>false</c>.</param>
    /// <returns>The handle of the heading.</returns>
    public FormItem<TModel> Heading(string key, string label, string? description = null, Func<FormController<TModel>, bool>? visibleWhen = null) =>
        Layout(key, new FieldNode { Key = key, Kind = FormFieldKind.Section, Label = label, Description = description, Condition = Visible(visibleWhen) });

    /// <summary>Declares a read-only block of text.</summary>
    /// <param name="key">A key for the block, unique in the form.</param>
    /// <param name="text">The text.</param>
    /// <param name="title">A heading for the block.</param>
    /// <param name="tone">The color from the Klyroo.UI theme: <c>neutral</c> (the default), <c>info</c>, <c>success</c>, <c>warning</c> or <c>error</c>.</param>
    /// <param name="visibleWhen">Hides the block while it returns <c>false</c>.</param>
    /// <returns>The handle of the block.</returns>
    public FormItem<TModel> Info(string key, string text, string? title = null, string? tone = null, Func<FormController<TModel>, bool>? visibleWhen = null) =>
        Layout(key, new FieldNode { Key = key, Kind = FormFieldKind.Info, Label = title, DisplayText = text, DisplayTone = tone, Condition = Visible(visibleWhen) });

    /// <summary>Declares a horizontal rule.</summary>
    /// <param name="key">A key for the rule, unique in the form.</param>
    /// <returns>The handle of the rule.</returns>
    public FormItem<TModel> Divider(string key) => Layout(key, new FieldNode { Key = key, Kind = FormFieldKind.Divider });

    /// <summary>Declares a button that runs <paramref name="onClick"/> and never submits the form.</summary>
    /// <param name="key">A key for the button, unique in the form.</param>
    /// <param name="label">The text of the button.</param>
    /// <param name="onClick">Runs when the user clicks the button.</param>
    /// <param name="visibleWhen">Hides the button while it returns <c>false</c>.</param>
    /// <returns>The handle of the button.</returns>
    public FormItem<TModel> Button(string key, string label, Func<FormController<TModel>, Task> onClick, Func<FormController<TModel>, bool>? visibleWhen = null)
    {
        ArgumentNullException.ThrowIfNull(onClick);
        return Layout(key, new FieldNode { Key = key, Kind = FormFieldKind.Button, Label = label, OnClick = e => onClick((FormController<TModel>)e.Owner!), Condition = Visible(visibleWhen) });
    }

    private FormItem<TModel> Layout(string key, FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(key);
        State.Claim(key);
        var handle = new FormItem<TModel>(key);
        Items.Add(new PendingNode<TModel>(handle, _ => node));
        return handle;
    }

    private static Func<FormEngine, bool>? Visible(Func<FormController<TModel>, bool>? when) =>
        when is null ? null : e => when((FormController<TModel>)e.Owner!);

    private FormMemberInfo MemberOf(FormModelPath path)
    {
        var properties = new List<PropertyInfo>(_prefix);
        properties.AddRange(path.Properties);
        return new FormMemberInfo(string.Join('.', properties.Select(p => p.Name)), properties, typeof(TModel));
    }

    private IFieldBinding<TModel> ListBinding<TItem>(FormMemberInfo info, FormModelPath path, FormCodec<IReadOnlyList<TItem>> codec, Func<IReadOnlyList<TItem>, object> make)
    {
        // A collection is read and written through the property, as the shape of the member decides: the typed delegates only fit the member type itself.
        var property = info.Property;
        if (path.Properties.Count == 1)
        {
            return new TypedBinding<TModel, THolder, IReadOnlyList<TItem>>(
                _holder,
                info,
                holder => property.GetValue(holder) is IEnumerable<TItem> items ? items.ToList() : [],
                property.SetMethod is { IsPublic: true } ? (holder, items) => property.SetValue(holder, make(items)) : null,
                codec);
        }
        return new ReflectedBinding<TModel>(info, FormFieldKind.ArrayList, nullableReference: false);
    }

    private static Func<TItem>? DefaultFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TItem>()
        where TItem : class =>
        typeof(TItem).GetConstructor(Type.EmptyTypes) is not null ? static () => Activator.CreateInstance<TItem>() : null;
}

/// <summary>Declares a typed form from the members of a model: the entry point of <see cref="FormSchema.For{TModel}"/>. It is also the builder of a step or a section, through <see cref="Step"/> and <see cref="Section"/>.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class FormSchemaBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel> : FormScopeBuilder<TModel, TModel>
{
    internal FormSchemaBuilder()
        : base(new BuilderState<TModel>(), new RootHolder<TModel>(), [], [])
    {
    }

    /// <summary>The identifier of the form, written to the <c>data-key</c> attribute of the rendered form. The default is <c>form</c>.</summary>
    public string FormKey
    {
        get => State.FormKey;
        set => State.FormKey = value;
    }

    /// <summary>The title shown in the form header.</summary>
    public string? Title
    {
        get => State.Title;
        set => State.Title = value;
    }

    /// <summary>The text shown under the title.</summary>
    public string? Description
    {
        get => State.Description;
        set => State.Description = value;
    }

    /// <summary>The small label shown above the title.</summary>
    public string? Eyebrow
    {
        get => State.Eyebrow;
        set => State.Eyebrow = value;
    }

    /// <summary>The number of columns, from 1 to 12, of the grids that lay out the fields. The default is 12.</summary>
    public int Columns
    {
        get => State.Columns;
        set => State.Columns = value;
    }

    /// <summary><c>true</c> to show the list of steps above a stepped form. The default is <c>true</c>.</summary>
    public bool ShowStepper
    {
        get => State.ShowStepper;
        set => State.ShowStepper = value;
    }

    /// <summary>Which checks validation runs. The default is <see cref="FormValidationMode.All"/>.</summary>
    public FormValidationMode ValidateMode
    {
        get => State.ValidateMode;
        set => State.ValidateMode = value;
    }

    /// <summary>The built-in validation messages of the form. When <c>null</c> the controller uses the English messages, or those of the current locale when a Klyroo.Form component renders it.</summary>
    public FormMessages? Messages
    {
        get => State.Messages;
        set => State.Messages = value;
    }

    /// <summary>Declares a step of a wizard. When the form has steps, the fields and sections declared outside a step are ignored.</summary>
    /// <param name="key">The key of the step, unique among the steps.</param>
    /// <param name="title">The title shown in the stepper.</param>
    /// <param name="configure">Declares the fields of the step.</param>
    /// <param name="description">Text shown under the stepper while the step is current.</param>
    /// <param name="skipWhen">Returns <c>true</c> to skip the step: it is passed over by Next and Back, and its fields are skipped by validation and the model write.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> Step(
        string key,
        string title,
        Action<FormGroupBuilder<TModel, TModel>> configure,
        string? description = null,
        Func<FormController<TModel>, bool>? skipWhen = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(configure);
        var items = new List<PendingItem>();
        configure(new FormGroupBuilder<TModel, TModel>(State, new RootHolder<TModel>(), [], items));
        State.Steps.Add(new PendingStep(key, title, description, items, skipWhen is null ? null : e => skipWhen((FormController<TModel>)e.Owner!)));
        return this;
    }

    /// <summary>Declares a section of a page form. Sections replace the fields declared outside a section, and are reported by <see cref="FormController{TModel}.GetSectionStates"/>.</summary>
    /// <param name="key">The key of the section, which is also its anchor in the page.</param>
    /// <param name="label">The title of the section.</param>
    /// <param name="configure">Declares the fields of the section.</param>
    /// <param name="description">The text under the title.</param>
    /// <param name="optional"><c>true</c> if the section may stay empty.</param>
    /// <param name="span">The number of columns, from 1 to 12, the section occupies in the grid of sections.</param>
    /// <param name="visibleWhen">Hides the section while it returns <c>false</c>.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> Section(
        string key,
        string label,
        Action<FormGroupBuilder<TModel, TModel>> configure,
        string? description = null,
        bool optional = false,
        int span = 12,
        Func<FormController<TModel>, bool>? visibleWhen = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(configure);
        var items = new List<PendingItem>();
        configure(new FormGroupBuilder<TModel, TModel>(State, new RootHolder<TModel>(), [], items));
        State.Sections.Add(new PendingSection(key, label, description, optional, span, items, visibleWhen is null ? null : e => visibleWhen((FormController<TModel>)e.Owner!)));
        return this;
    }

    /// <summary>Declares a field for every public readable and writable member of the model that has a default control and no field yet, with the label and settings the type gives. Use it together with a convention such as <c>UseDataAnnotations()</c> that reads the rest from the model.</summary>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> AllMembers()
    {
        foreach (var property in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null || property.SetMethod is null) continue;
            if (State.Keys.Contains(property.Name) || FormValueMap.DefaultKind(property.PropertyType) is null) continue;
            State.Claim(property.Name);
            Items.Add(new PendingReflected<TModel>(new FormMemberInfo(property.Name, [property], typeof(TModel))));
        }
        return this;
    }

    /// <summary>Adds a convention that adjusts every field declared from a member, after its defaults and before its explicit settings.</summary>
    /// <param name="convention">The convention.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> Convention(FormFieldConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        State.Conventions.Add(context => convention(context));
        return this;
    }

    /// <summary>Orders the fields: a field the function gives a number to is placed by that number, the others keep their order of declaration after them. The first function that answers decides.</summary>
    /// <param name="order">Gives the position of a member, or <c>null</c> for none.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> Order(Func<FormMemberInfo, int?> order)
    {
        ArgumentNullException.ThrowIfNull(order);
        State.Orders.Add(order);
        return this;
    }

    /// <summary>Says how a new model is created when the form needs one and the model type has no parameterless constructor.</summary>
    /// <param name="factory">Creates a new model.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> Factory(Func<TModel> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        State.Factory = factory;
        return this;
    }

    /// <summary>Adds a check of the whole model, run on submit after the field rules passed, on the model written from the values. The errors it reports go to the field they name, or to the form.</summary>
    /// <param name="validate">Receives the model and where to report what is wrong.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> Validate(Action<TModel, FormModelErrors<TModel>> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);
        State.Validators.Add(validate);
        return this;
    }

    /// <summary>Runs after validation and the model checks succeed, and before the save step. Return <c>false</c> to cancel the submission, for example after a failed server check.</summary>
    /// <param name="hook">Receives the model and the controller.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> OnBeforeSubmit(Func<TModel, FormController<TModel>, Task<bool>> hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        State.BeforeSubmit = hook;
        return this;
    }

    /// <summary>Runs when the form is submitted and validation and <see cref="OnBeforeSubmit"/> succeeded, before the save step. An exception it throws propagates to the caller of <c>SubmitAsync</c>.</summary>
    /// <param name="hook">Receives the model and the controller.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> OnSubmit(Func<TModel, FormController<TModel>, Task> hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        State.OnSubmit = hook;
        return this;
    }

    /// <summary>Runs after the current step validates and before moving forward. Return <c>false</c> to stay on the step.</summary>
    /// <param name="hook">Receives the controller and the index of the current step.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> OnBeforeNext(Func<FormController<TModel>, int, Task<bool>> hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        State.BeforeNext = hook;
        return this;
    }

    /// <summary>Runs before moving back. Return <c>false</c> to stay on the step.</summary>
    /// <param name="hook">Receives the controller and the index of the current step.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> OnBeforePrevious(Func<FormController<TModel>, int, Task<bool>> hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        State.BeforePrevious = hook;
        return this;
    }

    /// <summary>Excludes fields from dirty tracking, for example a search box that is not part of the data.</summary>
    /// <param name="items">The fields to exclude.</param>
    /// <returns>The builder.</returns>
    public FormSchemaBuilder<TModel> IgnoreDirty(params FormItem<TModel>[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        State.IgnoreDirty.AddRange(items.Select(i => i.Key));
        return this;
    }

    internal FormSchema<TModel> Build() => FormSchema<TModel>.Build(State, Items);
}

/// <summary>Declares the fields of a group, a card, a tab, a step or a section of a typed form. See <see cref="FormScopeBuilder{TModel, THolder}"/> for what it declares.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="THolder">The type whose members the block declares: the model, or the type of a group.</typeparam>
public sealed class FormGroupBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, [DynamicallyAccessedMembers(FormModelTrim.Members)] THolder> : FormScopeBuilder<TModel, THolder>
{
    internal FormGroupBuilder(BuilderState<TModel> state, HolderPath<TModel, THolder> holder, IReadOnlyList<PropertyInfo> prefix, List<PendingItem> items)
        : base(state, holder, prefix, items)
    {
    }
}

/// <summary>Declares the tabs of a tabbed container of a typed form.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="THolder">The type whose members the tabs declare.</typeparam>
public sealed class FormTabsBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, [DynamicallyAccessedMembers(FormModelTrim.Members)] THolder>
{
    private readonly BuilderState<TModel> _state;
    private readonly HolderPath<TModel, THolder> _holder;
    private readonly IReadOnlyList<PropertyInfo> _prefix;

    internal FormTabsBuilder(BuilderState<TModel> state, HolderPath<TModel, THolder> holder, IReadOnlyList<PropertyInfo> prefix)
    {
        _state = state;
        _holder = holder;
        _prefix = prefix;
    }

    internal List<PendingTab> Tabs { get; } = [];

    /// <summary>Declares a tab.</summary>
    /// <param name="key">The key of the tab, unique among its siblings.</param>
    /// <param name="label">The text shown on the tab.</param>
    /// <param name="configure">Declares the fields of the tab.</param>
    /// <param name="visibleWhen">Hides the tab, and its fields with it, while it returns <c>false</c>.</param>
    /// <returns>The builder.</returns>
    public FormTabsBuilder<TModel, THolder> Tab(string key, string label, Action<FormGroupBuilder<TModel, THolder>> configure, Func<FormController<TModel>, bool>? visibleWhen = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(configure);
        var items = new List<PendingItem>();
        configure(new FormGroupBuilder<TModel, THolder>(_state, _holder, _prefix, items));
        Tabs.Add(new PendingTab(key, label, items, visibleWhen is null ? null : e => visibleWhen((FormController<TModel>)e.Owner!)));
        return this;
    }
}

/// <summary>Declares the columns of a row of a repeatable list of a typed form. A column is a field of the item type; its handle reads and writes the cell of a <see cref="FormRow{TItem}"/>.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="TItem">The type of a row.</typeparam>
public sealed class FormRowBuilder<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel, [DynamicallyAccessedMembers(FormModelTrim.Members)] TItem>
{
    private readonly BuilderState<TModel> _state;
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    internal FormRowBuilder(BuilderState<TModel> state) => _state = state;

    internal List<PendingItem> Items { get; } = [];

    /// <summary>Declares a column bound to a member of the item type. The conditions of the column receive the controller of the whole form.</summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="member">The member, as in <c>l =&gt; l.Quantity</c>.</param>
    /// <param name="configure">Sets the label, rules and conditions of the column.</param>
    /// <returns>The handle of the column, to read a cell with <see cref="FormRow{TItem}.Get{TValue}"/>.</returns>
    public FormField<TItem, TValue> Field<TValue>(Expression<Func<TItem, TValue>> member, Action<FormFieldBuilder<TModel, TValue>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        var path = FormModelPath.Resolve(member, typeof(TValue));
        var info = new FormMemberInfo(path.Key, path.Properties, typeof(TItem));
        if (!_keys.Add(info.Key)) throw new ArgumentException($"A column with the key '{info.Key}' is already declared.", nameof(member));
        var handle = new FormField<TItem, TValue>(info.Key);
        var builder = new FormFieldBuilder<TModel, TValue>(handle);
        configure?.Invoke(builder);
        Func<FormCodec<TValue>, IFieldBinding<TItem>> bind;
        if (path.Properties.Count == 1)
        {
            var property = path.Properties[0];
            var get = Accessors.Getter<TItem, TValue>(property);
            var set = Accessors.Setter<TItem, TValue>(property);
            bind = codec => new TypedBinding<TItem, TItem, TValue>(new RootHolder<TItem>(), info, get, set, codec);
        }
        else
        {
            bind = codec => new ReflectedBinding<TItem>(info, codec.Kind, codec.NullableReference);
        }
        Items.Add(new PendingField<TItem, TModel, TValue>(handle, info, builder, bind));
        return handle;
    }
}
