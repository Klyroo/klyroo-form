namespace Klyroo.Form;

/// <summary>Adjusts one field declared from a model member. Every call is applied after the defaults of the member type and after the conventions (<c>UseDataAnnotations()</c>), so it wins over both. Calls return the builder.</summary>
/// <typeparam name="TValue">The type of the member.</typeparam>
public sealed class FormFieldBuilder<TValue>
{
    internal List<Action<FormFieldSettings>> Steps { get; } = [];

    private FormFieldBuilder<TValue> Add(Action<FormFieldSettings> step)
    {
        Steps.Add(step);
        return this;
    }

    /// <summary>Uses another kind of control than the one the member type gets (a <c>Textarea</c> or a <c>Password</c> for a string, a <c>Radio</c> or a <c>Select</c> for an enum, a <c>Slider</c> for a number).</summary>
    /// <param name="kind">The kind to use.</param>
    public FormFieldBuilder<TValue> Kind(FormFieldKind kind) => Add(s => s.Kind = kind);

    /// <summary>Sets <see cref="FormFieldSchema.Label"/>. The default is the member name in words (<c>FirstName</c> gives <c>First name</c>).</summary>
    /// <param name="label">The label.</param>
    public FormFieldBuilder<TValue> Label(string? label) => Add(s => s.Label = label);

    /// <summary>Sets <see cref="FormFieldSchema.Description"/>.</summary>
    /// <param name="description">The description.</param>
    public FormFieldBuilder<TValue> Description(string? description) => Add(s => s.Description = description);

    /// <summary>Sets <see cref="FormFieldSchema.Hint"/>.</summary>
    /// <param name="hint">The hint.</param>
    public FormFieldBuilder<TValue> Hint(string? hint) => Add(s => s.Hint = hint);

    /// <summary>Sets <see cref="FormFieldSchema.Placeholder"/>.</summary>
    /// <param name="placeholder">The placeholder.</param>
    public FormFieldBuilder<TValue> Placeholder(string? placeholder) => Add(s => s.Placeholder = placeholder);

    /// <summary>Sets <see cref="FormFieldSchema.Span"/>, the width in columns.</summary>
    /// <param name="span">The number of columns.</param>
    public FormFieldBuilder<TValue> Span(int span) => Add(s => s.Span = span);

    /// <summary>Sets <see cref="FormFieldSchema.Required"/>.</summary>
    /// <param name="required"><c>true</c> to require a value.</param>
    /// <param name="message">The message to show instead of the built-in one.</param>
    public FormFieldBuilder<TValue> Required(bool required = true, string? message = null) => Add(s =>
    {
        s.Required = required;
        if (message is not null) s.RequiredMessage = message;
    });

    /// <summary>Sets <see cref="FormFieldSchema.Disabled"/>.</summary>
    /// <param name="disabled"><c>true</c> to disable the control.</param>
    public FormFieldBuilder<TValue> Disabled(bool disabled = true) => Add(s => s.Disabled = disabled);

    /// <summary>Sets <see cref="FormFieldSchema.Options"/> for a choice kind, in place of the options an enum member gets by itself.</summary>
    /// <param name="options">The options. For an enum, the value of an option is the name of a member.</param>
    public FormFieldBuilder<TValue> Options(params FormOptionItem[] options) => Add(s => s.Options = options);

    /// <summary>Sets <see cref="FormFieldSchema.Min"/> and <see cref="FormFieldSchema.Max"/> of a number.</summary>
    /// <param name="min">The lowest value, or <c>null</c> for no bound.</param>
    /// <param name="max">The highest value, or <c>null</c> for no bound.</param>
    public FormFieldBuilder<TValue> Range(double? min, double? max) => Add(s =>
    {
        s.Min = min;
        s.Max = max;
    });

    /// <summary>Sets <see cref="FormFieldSchema.MaxLength"/> of a text.</summary>
    /// <param name="maxLength">The number of characters.</param>
    public FormFieldBuilder<TValue> MaxLength(int maxLength) => Add(s => s.MaxLength = maxLength);

    /// <summary>Adds a rule that gets the value as the member type. The value is <c>default</c> when the field is empty, so a rule on a nullable member sees <c>null</c>.</summary>
    /// <param name="key">The key of the rule.</param>
    /// <param name="message">The message to show when the rule fails.</param>
    /// <param name="isValid">Returns <c>true</c> when the value is valid.</param>
    public FormFieldBuilder<TValue> Rule(string key, string message, Func<TValue?, bool> isValid) => Add(s =>
    {
        var type = typeof(TValue);
        var rule = new FormValidatorRule(key, message, (raw, _) => isValid((TValue?)FormValueMap.FromControl(raw, type, nullableReference: false)));
        s.Validators = [.. s.Validators ?? [], rule];
    });

    /// <summary>Chooses what an empty input becomes for a <see cref="string"/> member: <c>null</c> (<paramref name="nullWhenEmpty"/> <c>true</c>) or an empty string. By default the nullable annotation of the member decides (<c>string?</c> gives <c>null</c>). The annotation is read at run time, and an app trimmed with <c>System.Reflection.NullabilityInfoContext.IsSupported</c> off has none: every string then gets an empty string unless you say otherwise here.</summary>
    /// <param name="nullWhenEmpty"><c>true</c> to write <c>null</c>, <c>false</c> to write an empty string.</param>
    public FormFieldBuilder<TValue> NullWhenEmpty(bool nullWhenEmpty = true) => Add(s => s.NullWhenEmpty = nullWhenEmpty);

    /// <summary>Shows the field only while <paramref name="condition"/> returns <c>true</c> (<see cref="FormFieldSchema.Condition"/>). A hidden field is not validated and its member is left as it is.</summary>
    /// <param name="condition">Receives the controller.</param>
    public FormFieldBuilder<TValue> Condition(Func<FormController, bool> condition) => Add(s => s.Condition = condition);

    /// <summary>Changes any setting of the field. Use it for what the other methods do not cover: <see cref="FormFieldSettings.DependsOn"/>, <see cref="FormFieldSettings.AsyncValidators"/>, <see cref="FormFieldSettings.Trigger"/>, <see cref="FormFieldSettings.RequiredWhen"/> and the rest of <see cref="FormFieldSchema"/>.</summary>
    /// <param name="configure">Changes the settings.</param>
    public FormFieldBuilder<TValue> Configure(Action<FormFieldSettings> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return Add(configure);
    }
}
