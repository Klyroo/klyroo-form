using System.Text.RegularExpressions;

namespace Klyroo.Form;

/// <summary>Adjusts the settings of a field declared from a model member. Conventions run after the defaults the type of the member gives and before the settings written in the <c>Field</c> call, so an explicit setting always wins. <c>UseDataAnnotations()</c> is one.</summary>
/// <param name="field">The field being declared, with what to read about its member and the setters to change it.</param>
public delegate void FormFieldConvention(FormFieldConventionContext field);

/// <summary>The member a field is declared from and the metadata a <see cref="FormFieldConvention"/> may set on it: label, description, placeholder, kind, requirement, length, range, pattern and options.</summary>
public sealed class FormFieldConventionContext
{
    internal FormFieldConventionContext(FormMemberInfo member, FieldSettings settings)
    {
        Member = member;
        Settings = settings;
    }

    internal FieldSettings Settings { get; }

    /// <summary>The member the field is declared from.</summary>
    public FormMemberInfo Member { get; }

    /// <summary>The kind of control the field has so far.</summary>
    public FormFieldKind Kind => Settings.Kind;

    /// <summary>The label of the field so far.</summary>
    public string? Label => Settings.Label;

    /// <summary>Sets the kind of control.</summary>
    /// <param name="kind">The kind.</param>
    public void SetKind(FormFieldKind kind) => Settings.Kind = kind;

    /// <summary>Sets the label.</summary>
    /// <param name="label">The label.</param>
    public void SetLabel(string? label) => Settings.Label = label;

    /// <summary>Sets the description shown under the label.</summary>
    /// <param name="description">The description.</param>
    public void SetDescription(string? description) => Settings.Description = description;

    /// <summary>Sets the placeholder of the control.</summary>
    /// <param name="placeholder">The placeholder.</param>
    public void SetPlaceholder(string? placeholder) => Settings.Placeholder = placeholder;

    /// <summary>Disables the control. A disabled field is still validated and written to the model.</summary>
    /// <param name="disabled"><c>true</c> to disable the field.</param>
    public void SetDisabled(bool disabled) => Settings.Disabled = disabled;

    /// <summary>Makes the field required.</summary>
    /// <param name="required"><c>true</c> to require a value.</param>
    /// <param name="message">The error shown when the value is missing, or <c>null</c> for the localized message of the form.</param>
    public void SetRequired(bool required, string? message = null)
    {
        Settings.Required = required;
        if (message is not null) Settings.RequiredMessage = message;
    }

    /// <summary>Limits the number of characters of a text value.</summary>
    /// <param name="maxLength">The largest accepted number of characters.</param>
    public void SetMaxLength(int maxLength) => Settings.MaxLength = maxLength;

    /// <summary>Bounds a number.</summary>
    /// <param name="min">The smallest accepted value.</param>
    /// <param name="max">The largest accepted value.</param>
    public void SetRange(double min, double max)
    {
        Settings.Min = min;
        Settings.Max = max;
    }

    /// <summary>Requires a text value to match a regular expression. An empty value is the business of the required check.</summary>
    /// <param name="pattern">The regular expression.</param>
    /// <param name="message">The error shown when the text does not match.</param>
    public void SetPattern(string pattern, string message)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(message);
        var regex = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Settings.Validators =
        [
            .. Settings.Validators ?? [],
            new ValidatorRule("pattern", message, (raw, _) => raw is not string { Length: > 0 } text || regex.IsMatch(text)),
        ];
    }

    /// <summary>Sets the options of a choice field, as the text of their values and their labels.</summary>
    /// <param name="options">The options.</param>
    public void SetOptions(IReadOnlyList<FormOptionItem> options) => Settings.Options = options;

    /// <summary>Makes the field depend on other fields of the same form by key, so a change of one of them validates it again once it was touched.</summary>
    /// <param name="keys">The keys of the fields.</param>
    public void DependsOn(params string[] keys) => Settings.DependsOn = [.. Settings.DependsOn ?? [], .. keys];
}
