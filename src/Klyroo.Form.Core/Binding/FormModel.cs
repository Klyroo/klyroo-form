using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Klyroo.Form;

/// <summary>Entry point of the typed binding: builds a <see cref="FormModel{TModel}"/> from a C# model type.</summary>
public static class FormModel
{
    /// <summary>Declares a form from the members of <typeparamref name="TModel"/>.</summary>
    /// <typeparam name="TModel">The model type. Its public properties and parameterless constructor are kept when an app is trimmed.</typeparam>
    /// <param name="configure">Declares the fields, and sets the title and the other settings of the schema.</param>
    /// <returns>The form: its schema, and the methods that move values between a model and a controller.</returns>
    /// <example><c>var form = FormModel.For&lt;Person&gt;(b =&gt; b.Field(p =&gt; p.Name, f =&gt; f.Required()).Field(p =&gt; p.Address.City));</c></example>
    public static FormModel<TModel> For<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>(Action<FormModelBuilder<TModel>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new FormModelBuilder<TModel>();
        configure(builder);
        return builder.Build();
    }
}

internal sealed record FormModelSchemaOptions(
    string FormKey,
    string? Title,
    string? Description,
    string? Eyebrow,
    int Columns,
    FormValidationMode ValidateMode,
    FormMessages? Messages,
    Func<FormController, Task<bool>>? BeforeSubmit,
    Func<FormController, Task>? OnSubmit);

/// <summary>A form declared from a model type: the schema to give a <see cref="FormController"/> or a component, and the typed moves between a model and the values of a controller. It is immutable and can be shared, and the same instance serves any number of controllers.</summary>
/// <typeparam name="TModel">The model type.</typeparam>
public sealed class FormModel<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel>
{
    private readonly IReadOnlyList<FormFieldBinding> _bindings;
    private readonly FormFieldBinding[] _writable;
    private readonly FormModelValidator<TModel>[] _validators;
    private readonly IReadOnlyDictionary<string, Func<object>> _creators;
    private readonly Func<TModel>? _factory;
    private readonly FormModelSchemaOptions _options;
    private readonly ConditionalWeakTable<FormController, HashSet<string>> _reported = new();
    private FormSchema? _schema;

    internal FormModel(
        IReadOnlyList<FormFieldSchema> fields,
        IReadOnlyList<FormFieldBinding> bindings,
        FormModelValidator<TModel>[] validators,
        IReadOnlyDictionary<string, Func<object>> creators,
        Func<TModel>? factory,
        FormModelSchemaOptions options)
    {
        Fields = fields;
        _bindings = bindings;
        _writable = bindings.Where(b => b.CanWrite).ToArray();
        _validators = validators;
        _creators = creators;
        _factory = factory;
        _options = options;
    }

    /// <summary>The fields in form order: the ones declared from members and the plain fields added with <see cref="FormModelBuilder{TModel}.Add(FormFieldSchema)"/>. Put them in the <see cref="FormStepSchema"/> or <see cref="FormPageSectionSchema"/> of a schema you build yourself, with <see cref="OnBeforeSubmit"/>, to lay the form out in steps or sections.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; }

    /// <summary>Gets the field declared for a member, to place it in a step or a section.</summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="member">The member, written as in <see cref="FormModelBuilder{TModel}.Field{TValue}"/>.</param>
    /// <returns>The field.</returns>
    /// <exception cref="ArgumentException">No field is declared for the member.</exception>
    public FormFieldSchema Field<TValue>(Expression<Func<TModel, TValue>> member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var key = FormModelPath.Resolve(member, typeof(TValue)).Key;
        return Fields.FirstOrDefault(f => f.Key == key) ?? throw new ArgumentException($"No field is declared for '{key}'.", nameof(member));
    }

    /// <summary>The schema of the form: <see cref="Fields"/> and the settings given to the builder (title, columns, validation mode, messages, hooks), with <see cref="OnBeforeSubmit"/> as the before-submit hook. The same instance is returned every time.</summary>
    public FormSchema Schema => _schema ??= new FormSchema
    {
        FormKey = _options.FormKey,
        Title = _options.Title,
        Description = _options.Description,
        Eyebrow = _options.Eyebrow,
        Columns = _options.Columns,
        ValidateMode = _options.ValidateMode,
        Messages = _options.Messages,
        Fields = Fields,
        OnBeforeSubmit = OnBeforeSubmit,
        OnSubmit = _options.OnSubmit,
    };

    /// <summary>The before-submit hook of the form, or <c>null</c> when nothing needs one. It builds the model from the values, runs the model checks (see <see cref="FormModelBuilder{TModel}.Validate"/>), puts what they report on the fields they name or on the form, and returns <c>false</c> if there is any error. Then it runs the hook given to <see cref="FormModelBuilder{TModel}.OnBeforeSubmit"/>. The errors are not blocking: they show until the user edits the field, and the checks run again at the next submit. Use it as <see cref="FormSchema.OnBeforeSubmit"/> of a schema you build yourself.</summary>
    public Func<FormController, Task<bool>>? OnBeforeSubmit => _validators.Length == 0 ? _options.BeforeSubmit : BeforeSubmitAsync;

    /// <summary>Creates a controller for <see cref="Schema"/>. With <paramref name="model"/> the controller starts from its values, which are also the baseline of dirty tracking. Without it, the controller starts from the values of a new model when a model can be created (the parameterless constructor or <see cref="FormModelBuilder{TModel}.Factory"/>), so initializers of the model are the defaults of the form.</summary>
    /// <param name="model">The model to edit, or <c>null</c> for a new one.</param>
    /// <returns>The controller. Dispose it when you are done.</returns>
    public FormController CreateController(TModel? model = default)
    {
        var source = model ?? (_factory is null ? default : _factory());
        return new FormController(Schema, source is null ? null : ToInput(source));
    }

    /// <summary>Gets the initial values of a model in the form the controller stores: the <c>Input</c> of <c>KlyFormEngine</c> or the second argument of <see cref="FormController"/>. Keys follow the fields, and a member that is not declared is not in it.</summary>
    /// <param name="model">The model to read.</param>
    /// <returns>The values by field key.</returns>
    public IReadOnlyDictionary<string, object?> ToInput(TModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var input = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var binding in _bindings) input[binding.Key] = binding.ToControl(model);
        return input;
    }

    /// <summary>Loads a model into a controller: it becomes the values and the baseline of dirty tracking, errors and touched state are cleared (see <see cref="FormController.Reset"/>).</summary>
    /// <param name="controller">The controller of this form.</param>
    /// <param name="model">The model to load.</param>
    public void Load(FormController controller, TModel model)
    {
        ArgumentNullException.ThrowIfNull(controller);
        controller.Reset(ToInput(model));
    }

    /// <summary>Writes the values of a controller into <paramref name="target"/>. Only the members of fields in effect are written: a hidden field, or one of a skipped step, leaves its member as it is, as does a member without a public setter. An empty value gives <c>null</c> for a nullable or reference member (an empty string for a string that is not declared nullable) and <c>default</c> for another value type.</summary>
    /// <param name="controller">The controller of this form.</param>
    /// <param name="target">The model to update.</param>
    /// <returns><paramref name="target"/>.</returns>
    public TModel Write(FormController controller, TModel target)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return Write(controller.BuildOutput(), target);
    }

    /// <summary>Writes values by field key, such as the dictionary <c>KlyFormEngine</c> gives <c>OnSubmit</c> and <c>OnSave</c>, into <paramref name="target"/>. See <see cref="Write(FormController, TModel)"/> for the rules.</summary>
    /// <param name="values">The values by field key. A key that is missing leaves its member as it is.</param>
    /// <param name="target">The model to update.</param>
    /// <returns><paramref name="target"/>.</returns>
    public TModel Write(IReadOnlyDictionary<string, object?> values, TModel target)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(target);
        foreach (var binding in _writable)
        {
            if (values.TryGetValue(binding.Key, out var raw)) binding.Write(target, raw, _creators);
        }
        return target;
    }

    /// <summary>Creates a new model from the values of a controller (see <see cref="Write(FormController, TModel)"/>).</summary>
    /// <param name="controller">The controller of this form.</param>
    /// <returns>The new model.</returns>
    /// <exception cref="InvalidOperationException">The model has no parameterless constructor and no <see cref="FormModelBuilder{TModel}.Factory"/>.</exception>
    public TModel Create(FormController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return Create(controller.BuildOutput());
    }

    /// <summary>Creates a new model from values by field key (see <see cref="Write(IReadOnlyDictionary{string, object?}, TModel)"/>).</summary>
    /// <param name="values">The values by field key.</param>
    /// <returns>The new model.</returns>
    /// <exception cref="InvalidOperationException">The model has no parameterless constructor and no <see cref="FormModelBuilder{TModel}.Factory"/>.</exception>
    public TModel Create(IReadOnlyDictionary<string, object?> values)
    {
        var model = _factory is null
            ? throw new InvalidOperationException($"{typeof(TModel).Name} has no parameterless constructor: set FormModelBuilder.Factory to say how a new one is created.")
            : _factory();
        return Write(values, model);
    }

    /// <summary>Submits a controller with a typed save step: validation and the schema hooks run, then <paramref name="save"/> gets the model, and the form stays dirty and editable when it returns <c>false</c> (see <see cref="FormController.SubmitAsync(Func{FormController, CancellationToken, Task{bool}}, CancellationToken)"/>).</summary>
    /// <param name="controller">The controller of this form.</param>
    /// <param name="save">Saves the model and returns <c>true</c> on success. To report a failure, call <see cref="FormController.SetError"/> or <see cref="FormController.SetFormError"/> and return <c>false</c>.</param>
    /// <param name="target">The model to update and pass to <paramref name="save"/>, or <c>null</c> to pass a new one.</param>
    /// <param name="cancellationToken">Passed to <paramref name="save"/>.</param>
    /// <returns><c>true</c> if the form was submitted and saved.</returns>
    public Task<bool> SubmitAsync(FormController controller, Func<TModel, CancellationToken, Task<bool>> save, TModel? target = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return controller.SubmitAsync(OnSave(save, target), cancellationToken);
    }

    /// <summary>Wraps a typed save step as the handler of <see cref="FormController.SubmitAsync(Func{FormController, CancellationToken, Task{bool}}, CancellationToken)"/>.</summary>
    /// <param name="save">Saves the model.</param>
    /// <param name="target">The model to update and pass to <paramref name="save"/>, or <c>null</c> to pass a new one.</param>
    /// <returns>The handler.</returns>
    public Func<FormController, CancellationToken, Task<bool>> OnSave(Func<TModel, CancellationToken, Task<bool>> save, TModel? target = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        return (controller, ct) => save(target is null ? Create(controller) : Write(controller, target), ct);
    }

    /// <summary>Wraps a typed save step as the <c>OnSave</c> parameter of <c>KlyFormEngine</c>, which passes the output dictionary.</summary>
    /// <param name="save">Saves the model.</param>
    /// <param name="target">The model to update and pass to <paramref name="save"/>, or <c>null</c> to pass a new one.</param>
    /// <returns>The handler.</returns>
    public Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>> OnSaveOutput(Func<TModel, CancellationToken, Task<bool>> save, TModel? target = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        return (values, ct) => save(target is null ? Create(values) : Write(values, target), ct);
    }

    private async Task<bool> BeforeSubmitAsync(FormController controller)
    {
        var reported = _reported.GetOrCreateValue(controller);
        foreach (var key in reported) controller.ClearError(key);
        reported.Clear();

        var errors = new FormModelErrors(controller.ActiveFields.Select(f => f.Key));
        var model = Create(controller);
        foreach (var validator in _validators) validator(model, errors);
        foreach (var (member, message) in errors.Items)
        {
            var key = member is null ? null : ResolveKey(member);
            if (key is null) controller.SetFormError(message, blocking: false);
            else controller.SetError(key, message, blocking: false);
            reported.Add(key ?? FormController.FormErrorKey);
        }
        if (errors.Items.Count > 0) return false;
        return _options.BeforeSubmit is null || await _options.BeforeSubmit(controller);
    }

    // A key, or a member name that exactly one bound field has.
    private string? ResolveKey(string member)
    {
        if (_bindings.Any(b => b.Key == member)) return member;
        var byName = _bindings.Where(b => b.Member.Name == member).ToList();
        return byName.Count == 1 ? byName[0].Key : null;
    }
}
