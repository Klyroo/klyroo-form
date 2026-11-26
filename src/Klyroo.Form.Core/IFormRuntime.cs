using Klyroo.Form.Dynamic;
using System.Text.Json;

namespace Klyroo.Form;

/// <summary>What a form exposes to code that drives it without knowing its model type, such as an agent bridge: the form itself, its fields as <see cref="IFormFieldAccess"/> and the actions a person has on a form. <c>FormController&lt;TModel&gt;</c> and <c>DynamicFormController</c> implement it.</summary>
public interface IFormRuntime
{
    /// <summary>The identifier of the form, as set on the schema.</summary>
    string FormKey { get; }

    /// <summary>The title of the form, or <c>null</c>.</summary>
    string? Title { get; }

    /// <summary><c>true</c> when a value differs from the baseline taken at creation, by a reset or after a successful submission.</summary>
    bool IsDirty { get; }

    /// <summary><c>true</c> when the schema declares steps.</summary>
    bool IsStepped { get; }

    /// <summary>The key of the current step, or <c>null</c> for a form without steps.</summary>
    string? CurrentStepKey { get; }

    /// <summary>The fields a person fills in: every stateful leaf field of the form, whether it is visible now or not. Containers, stateless blocks and hidden inputs are not listed.</summary>
    IReadOnlyList<IFormFieldAccess> Fields { get; }

    /// <summary>Raised synchronously after every change of a value, an error, the step or the submission status.</summary>
    event Action? Changed;

    /// <summary>Validates the form and runs its submission hooks, like the submit button does.</summary>
    /// <returns><c>true</c> when the form was submitted.</returns>
    Task<bool> SubmitAsync();

    /// <summary>Puts every field back to the baseline value and clears errors.</summary>
    void Reset();

    /// <summary>Moves to the next step, after validating the current one.</summary>
    /// <returns><c>true</c> when the step changed.</returns>
    Task<bool> NextStepAsync();

    /// <summary>Moves to the previous step.</summary>
    /// <returns><c>true</c> when the step changed.</returns>
    Task<bool> PreviousStepAsync();
}

/// <summary>One field of an <see cref="IFormRuntime"/>, seen as text and JSON so that code that does not know the type of its value can read and write it.</summary>
public interface IFormFieldAccess
{
    /// <summary>The key of the field.</summary>
    string Key { get; }

    /// <summary>The kind of control the field uses.</summary>
    FormFieldKind Kind { get; }

    /// <summary>The label of the field, or its key when it has none.</summary>
    string Label { get; }

    /// <summary><c>true</c> for a password or a one-time code: <see cref="WriteValue"/> writes <c>null</c> for it.</summary>
    bool Sensitive { get; }

    /// <summary><c>true</c> when the field is not in effect now (its condition is false, it is in a hidden container, tab or section, or in a skipped step), so validation and the output ignore it.</summary>
    bool IsHidden { get; }

    /// <summary><c>true</c> when the field is in effect but belongs to a step other than the current one.</summary>
    bool IsOffPage { get; }

    /// <summary><c>true</c> when the field cannot be edited now.</summary>
    bool IsDisabled { get; }

    /// <summary><c>true</c> when the field must have a value now.</summary>
    bool IsRequired { get; }

    /// <summary>The options of a choice field, with their values as text; empty for any other field.</summary>
    IReadOnlyList<FormOptionItem> Options { get; }

    /// <summary>Writes the current value as JSON: a string, a number, a boolean, a list of strings or <c>null</c>. A sensitive field writes <c>null</c>.</summary>
    /// <param name="writer">Where to write the value.</param>
    void WriteValue(Utf8JsonWriter writer);

    /// <summary>Sets the value from JSON, the way a person would type it: numbers keep their exact digits, a choice is matched by value or by label, a date is read as <c>yyyy-MM-dd</c>. The value is checked against the field before it is stored.</summary>
    /// <param name="value">The JSON value to store.</param>
    /// <returns>Whether the value was accepted, and the reason when it was not.</returns>
    FormWriteResult TrySet(JsonElement value);

    /// <summary>Empties the field: <c>false</c> for a checkbox, an empty list for a multiple choice, no value for the others.</summary>
    /// <returns>Whether the field could be emptied, and the reason when it could not.</returns>
    FormWriteResult Clear();

    /// <summary>Flips a checkbox or a switch.</summary>
    /// <returns>Whether the field could be toggled, and the reason when it could not.</returns>
    FormWriteResult Toggle();

    /// <summary>Puts the field back to its baseline value and clears its error.</summary>
    void Reset();
}

/// <summary>The result of writing a value into a field through <see cref="IFormFieldAccess"/>.</summary>
/// <param name="Ok"><c>true</c> when the value was stored.</param>
/// <param name="Error">Why the value was refused, or <c>null</c> when it was stored.</param>
public readonly record struct FormWriteResult(bool Ok, string? Error = null)
{
    /// <summary>A write that stored its value.</summary>
    public static FormWriteResult Success => new(true);

    /// <summary>A write that was refused.</summary>
    /// <param name="error">Why it was refused.</param>
    /// <returns>The failed result.</returns>
    public static FormWriteResult Fail(string error) => new(false, error);
}
