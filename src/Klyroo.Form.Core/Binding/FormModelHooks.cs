namespace Klyroo.Form;

/// <summary>Adjusts the settings of a field declared from a model member. Conventions run after the defaults the member type gives and before the settings written in the <c>Field</c> call, so an explicit setting always wins. <c>UseDataAnnotations()</c> is one.</summary>
/// <param name="member">The member the field is declared from.</param>
/// <param name="settings">The settings of the field, to change.</param>
public delegate void FormFieldConvention(FormMemberInfo member, FormFieldSettings settings);

/// <summary>A check of the whole model, run when the form is submitted, after the field rules passed.</summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <param name="model">The model built from the values of the form.</param>
/// <param name="errors">Where to report what is wrong.</param>
public delegate void FormModelValidator<in TModel>(TModel model, FormModelErrors errors);

/// <summary>Collects the errors of a <see cref="FormModelValidator{TModel}"/>. An error names the member it belongs to, or none for the form as a whole.</summary>
public sealed class FormModelErrors
{
    private readonly HashSet<string> _active;

    internal FormModelErrors(IEnumerable<string> activeKeys) => _active = new HashSet<string>(activeKeys, StringComparer.Ordinal);

    internal List<(string? Member, string Message)> Items { get; } = [];

    /// <summary>Tells whether the field with this key is in effect: not hidden by a condition, in a hidden container or section, or in a skipped step. A check should leave the other fields alone, because their values are not the user's.</summary>
    /// <param name="key">The key of the field.</param>
    /// <returns><c>true</c> when the field is in effect.</returns>
    public bool IsActive(string key) => _active.Contains(key);

    /// <summary>Reports an error on the form as a whole.</summary>
    /// <param name="message">The message to show.</param>
    public void Add(string message) => Items.Add((null, message));

    /// <summary>Reports an error on a member. <paramref name="member"/> is a field key (<c>Address.City</c>) or a member name that only one field has (<c>City</c>). A name that matches no field makes the error a form-level one.</summary>
    /// <param name="member">The key or name of the member.</param>
    /// <param name="message">The message to show.</param>
    public void Add(string member, string message) => Items.Add((member, message));
}
