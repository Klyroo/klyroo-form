using System.Linq.Expressions;

namespace Klyroo.Form;

/// <summary>Collects the errors of a model check declared with <c>Validate</c>. An error names the field it belongs to, or none for the form as a whole.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class FormModelErrors<TModel>
{
    private readonly HashSet<string> _active;

    internal FormModelErrors(IEnumerable<string> activeKeys) => _active = new HashSet<string>(activeKeys, StringComparer.Ordinal);

    internal List<(string? Member, string Message)> Items { get; } = [];

    /// <summary>Tells whether a field is in effect: not hidden by a condition, in a hidden container or section, or in a skipped step. A check should leave the other fields alone, because their values are not the user's.</summary>
    /// <param name="item">The field.</param>
    /// <returns><c>true</c> when the field is in effect.</returns>
    public bool IsActive(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _active.Contains(item.Key);
    }

    /// <summary>Tells whether the field with this key is in effect. See <see cref="IsActive(FormItem{TModel})"/>.</summary>
    /// <param name="key">The key of the field.</param>
    /// <returns><c>true</c> when the field is in effect.</returns>
    public bool IsActive(string key) => _active.Contains(key);

    /// <summary>Reports an error on the form as a whole.</summary>
    /// <param name="message">The message to show.</param>
    public void Add(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Items.Add((null, message));
    }

    /// <summary>Reports an error on a member of the model.</summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="member">The member, as in <c>m =&gt; m.End</c>. A member that has no field makes the error a form-level one.</param>
    /// <param name="message">The message to show.</param>
    public void Add<TValue>(Expression<Func<TModel, TValue>> member, string message)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(message);
        Items.Add((FormModelPath.Resolve(member, null).Key, message));
    }

    /// <summary>Reports an error on a field or an element of the form.</summary>
    /// <param name="item">The field.</param>
    /// <param name="message">The message to show.</param>
    public void Add(FormItem<TModel> item, string message)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(message);
        Items.Add((item.Key, message));
    }

    /// <summary>Reports an error on a member by its key (<c>Address.City</c>) or by a member name that only one field has (<c>City</c>). A name that matches no field makes the error a form-level one. This is the form that fits a name read from text, such as the member names of a validation result.</summary>
    /// <param name="member">The key or name of the member.</param>
    /// <param name="message">The message to show.</param>
    public void Add(string member, string message)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(message);
        Items.Add((member, message));
    }
}
