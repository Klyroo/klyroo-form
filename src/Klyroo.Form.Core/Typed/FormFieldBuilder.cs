namespace Klyroo.Form;

/// <summary>Configures one field of a typed form: its label, rules, conditions and defaults. Methods return the builder, so a field reads as one chain: <c>f.Required().MaxLength(120)</c>. Settings that only make sense for some kinds of value (<c>AsTextarea</c>, <c>Range</c>, <c>Options</c>) are extension methods that only exist for the types they fit, so a wrong kind does not compile.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="TValue">The type of the value of the field.</typeparam>
public sealed class FormFieldBuilder<TModel, TValue>
{
    private readonly ICodecSource<TValue> _field;

    internal FormFieldBuilder(ICodecSource<TValue> field) => _field = field;

    // The settings, applied in order once the kind of the field is known.
    internal List<Action<FieldSettings>> Steps { get; } = [];

    // The kind a kind-specific method chose, which wins over the one the type of the value gives.
    internal FormFieldKind? ExplicitKind { get; private set; }

    internal FormCodec<TValue> Codec => _field.Codec;

    internal FormFieldBuilder<TModel, TValue> Add(Action<FieldSettings> step)
    {
        Steps.Add(step);
        return this;
    }

    internal FormFieldBuilder<TModel, TValue> SetKind(FormFieldKind kind, Action<FieldSettings>? more = null)
    {
        ExplicitKind = kind;
        Steps.Add(s =>
        {
            s.Kind = kind;
            more?.Invoke(s);
        });
        return this;
    }

    internal static FormController<TModel> Controller(FormEngine engine) => (FormController<TModel>)engine.Owner!;

    /// <summary>Chooses the control by its kind, for a type that has no default one or to replace the default. Prefer the kind-specific methods (<c>AsTextarea</c>, <c>AsSwitch</c>), which exist only for the types they fit.</summary>
    /// <param name="kind">The kind of control.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Kind(FormFieldKind kind) => SetKind(kind);

    /// <summary>Sets the text of the label shown above the control. The default is the name of the member, split into words.</summary>
    /// <param name="label">The label.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Label(string? label) => Add(s => s.Label = label);

    /// <summary>Sets the text shown under the label.</summary>
    /// <param name="description">The description.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Description(string? description) => Add(s => s.Description = description);

    /// <summary>Sets the short text shown next to the label.</summary>
    /// <param name="hint">The hint.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Hint(string? hint) => Add(s => s.Hint = hint);

    /// <summary>Sets the text shown under the control.</summary>
    /// <param name="help">The help text.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Help(string? help) => Add(s => s.Help = help);

    /// <summary>Sets the placeholder of the control. For a choice field it is the text of the empty option.</summary>
    /// <param name="placeholder">The placeholder.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Placeholder(string? placeholder) => Add(s => s.Placeholder = placeholder);

    /// <summary>Sets the number of columns the field occupies in the grid of the form. Values are clamped between 1 and the column count.</summary>
    /// <param name="span">The number of columns.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Span(int span) => Add(s => s.Span = span);

    /// <summary>Makes the field required, or not.</summary>
    /// <param name="required"><c>true</c> to require a value.</param>
    /// <param name="message">The error shown when the value is missing; the localized message of the form when <c>null</c>.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Required(bool required = true, string? message = null) => Add(s =>
    {
        s.Required = required;
        if (message is not null) s.RequiredMessage = message;
    });

    /// <summary>Makes the field required while <paramref name="when"/> returns <c>true</c>. It is evaluated on every validation.</summary>
    /// <param name="when">The condition, given the controller of the form.</param>
    /// <param name="message">The error shown when the value is missing; the localized message of the form when <c>null</c>.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Required(Func<FormController<TModel>, bool> when, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(when);
        return Add(s =>
        {
            s.RequiredWhen = e => when(Controller(e));
            if (message is not null) s.RequiredMessage = message;
        });
    }

    /// <summary>Renders the control disabled. This only affects rendering: a disabled field is still validated and written to the model.</summary>
    /// <param name="disabled"><c>true</c> to disable the field.</param>
    /// <param name="reason">Text shown under the control to say why it is disabled.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Disabled(bool disabled = true, string? reason = null) => Add(s =>
    {
        s.Disabled = disabled;
        if (reason is not null) s.DisabledReason = reason;
    });

    /// <summary>Disables the field while <paramref name="when"/> returns <c>true</c>.</summary>
    /// <param name="when">The condition, given the controller of the form.</param>
    /// <param name="reason">Text shown under the control to say why it is disabled.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> DisabledWhen(Func<FormController<TModel>, bool> when, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(when);
        return Add(s =>
        {
            s.DisabledWhen = e => when(Controller(e));
            if (reason is not null) s.DisabledReason = reason;
        });
    }

    /// <summary>Shows the field only while <paramref name="when"/> returns <c>true</c>. A hidden field is skipped by validation and left out of the model write, and its value stays in the controller.</summary>
    /// <param name="when">The condition, given the controller of the form.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> VisibleWhen(Func<FormController<TModel>, bool> when)
    {
        ArgumentNullException.ThrowIfNull(when);
        return Add(s => s.Condition = e => when(Controller(e)));
    }

    /// <summary>Names the fields this field's rules read, so a change of one of them validates this field again once it was touched. A confirmation field depends on the password it repeats.</summary>
    /// <param name="items">The fields this one depends on.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> DependsOn(params FormItem<TModel>[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var keys = items.Select(i => i.Key).ToArray();
        return Add(s => s.DependsOn = [.. s.DependsOn ?? [], .. keys]);
    }

    /// <summary>Sets the value the field starts with when the model does not give one.</summary>
    /// <param name="value">The initial value.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Default(TValue value) => Add(s => s.DefaultValue = ((FormCodec<TValue>)s.Codec!).ToControl(value));

    /// <summary>Computes the value the field starts with from the controller. It also fills the cells of a new row of a repeatable list.</summary>
    /// <param name="factory">Computes the value, given the controller of the form.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Default(Func<FormController<TModel>, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.DefaultFactory = e => codec.ToControl(factory(Controller(e)));
        });
    }

    /// <summary>Converts a value from the model into the value held by the form when the form is created or reset. It does not apply to default values or to values set later.</summary>
    /// <param name="transform">The conversion.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> TransformInput(Func<TValue, TValue> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.TransformInput = raw => codec.ToControl(transform(codec.FromControlOrDefault(raw)!));
        });
    }

    /// <summary>Converts the value when the model is written, for example to trim a string. The form keeps the untransformed value.</summary>
    /// <param name="transform">The conversion.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> TransformOutput(Func<TValue, TValue> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.TransformOutput = raw => codec.ToControl(transform(codec.FromControlOrDefault(raw)!));
        });
    }

    /// <summary>Calls <paramref name="onChange"/> with the new value after the value of the field changes by <c>Set</c> or by the user, and after a reset of the field that changes it. It does not run when a whole new state is loaded.</summary>
    /// <param name="onChange">Receives the new value and the controller.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> OnChange(Action<TValue, FormController<TModel>> onChange)
    {
        ArgumentNullException.ThrowIfNull(onChange);
        return Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.Watch = (raw, e) => onChange(codec.FromControlOrDefault(raw)!, Controller(e));
        });
    }

    /// <summary>Adds a rule: while <paramref name="isValid"/> returns <c>false</c> the field shows <paramref name="message"/>. Rules run in order after the built-in checks, also for an empty value of a field that is not required, and the first failing one sets the error.</summary>
    /// <param name="isValid">Receives the value (the default of the type while the text is not a valid value) and returns <c>true</c> when it is valid.</param>
    /// <param name="message">The error message.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Rule(Func<TValue, bool> isValid, string message)
    {
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(message);
        return AddRule(message, (value, _) => isValid(value));
    }

    /// <summary>Adds a rule that also reads the controller, for example to compare the value with another field.</summary>
    /// <param name="isValid">Receives the value and the controller, and returns <c>true</c> when the value is valid.</param>
    /// <param name="message">The error message.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Rule(Func<TValue, FormController<TModel>, bool> isValid, string message)
    {
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(message);
        return AddRule(message, isValid);
    }

    private FormFieldBuilder<TModel, TValue> AddRule(string message, Func<TValue, FormController<TModel>, bool> isValid) => Add(s =>
    {
        var key = "rule" + ((s.Validators?.Count ?? 0) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var rule = new ValidatorRule(key, message, (raw, e) => isValid(Codec.FromControlOrDefault(raw)!, Controller(e)));
        s.Validators = [.. s.Validators ?? [], rule];
    });

    /// <summary>Adds an asynchronous rule, for a check that needs I/O such as "is this username free". It runs in the background once the user left the field, and again as they edit it after <paramref name="debounce"/>; submission waits for it. It does not run for an empty value or for a value that already fails a synchronous rule.</summary>
    /// <param name="validate">Receives the value, the controller and a token that is cancelled when the value changes. Returns the error message, or <c>null</c> when the value is valid.</param>
    /// <param name="debounce">How long the value must stay unchanged before a background run starts. The default is 300 milliseconds.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> RuleAsync(Func<TValue, FormController<TModel>, CancellationToken, ValueTask<string?>> validate, TimeSpan? debounce = null)
    {
        ArgumentNullException.ThrowIfNull(validate);
        return Add(s =>
        {
            var key = "async" + ((s.AsyncValidators?.Count ?? 0) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var rule = new AsyncValidatorRule(key, (raw, e, ct) => validate(Codec.FromControlOrDefault(raw)!, Controller(e), ct))
            {
                Debounce = debounce ?? TimeSpan.FromMilliseconds(300),
            };
            s.AsyncValidators = [.. s.AsyncValidators ?? [], rule];
        });
    }

    /// <summary>Chooses when the field is validated while the user edits it. The default is <see cref="FormValidationTrigger.Blur"/>.</summary>
    /// <param name="trigger">The trigger.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Trigger(FormValidationTrigger trigger) => Add(s => s.Trigger = trigger);

    /// <summary>Shows a clear button once the field has a value.</summary>
    /// <param name="clearable"><c>true</c> to show the button.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> Clearable(bool clearable = true) => Add(s => s.Clearable = clearable);

    /// <summary>Shows a per-field reset button once the value differs from its initial value.</summary>
    /// <param name="dirtyCheck"><c>true</c> to show the button.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> DirtyCheck(bool dirtyCheck = true) => Add(s => s.DirtyCheck = dirtyCheck);

    /// <summary>Validates and stores the field but leaves it out of the model write, so the member keeps the value it has.</summary>
    /// <param name="omit"><c>true</c> to leave the field out.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> OmitFromOutput(bool omit = true) => Add(s => s.OmitFromOutput = omit);

    /// <summary>Decides what an empty input becomes for a string member: <c>null</c> when <c>true</c>, an empty string when <c>false</c>. By default the nullable annotation of the member decides.</summary>
    /// <param name="nullWhenEmpty"><c>true</c> for <c>null</c>, <c>false</c> for an empty string.</param>
    /// <returns>The builder.</returns>
    public FormFieldBuilder<TModel, TValue> NullWhenEmpty(bool nullWhenEmpty = true) => Add(s => s.NullWhenEmpty = nullWhenEmpty);
}
