using System.Globalization;

namespace Klyroo.Form;

/// <summary>
/// The built-in validation messages of <see cref="FormController"/>, which has no UI and so no locale of its own: the required, number bounds, text length,
/// date range and file errors. A new instance holds the English messages. Build a variant with a <c>with</c> expression, give it to the controller with
/// <see cref="FormSchema.Messages"/> or <see cref="FormController.Messages"/>, or pick one of the twelve built-in sets (<see cref="FormMessageLocales"/>).
/// The Blazor components do it for you: they give the controller the messages of the current Klyroo.UI locale, see <c>FormTexts</c>.
/// </summary>
/// <remarks>
/// A message that contains a number or a name is a function, so a language can choose its own word order and plural forms. <see cref="FormFieldSchema.RequiredMessage"/>
/// and the messages of your own validators are never translated. Messages already computed when the set changes are recomputed from the new one.
/// </remarks>
/// <example>
/// <code>
/// var form = new FormController(schema) { Messages = FormMessageLocales.Fr };
/// var custom = FormMessages.Default with { Required = "Please fill this in" };
/// </code>
/// </example>
public sealed record FormMessages
{
    /// <summary>Creates the messages with their English defaults.</summary>
    public FormMessages()
    {
    }

    /// <summary>The English messages, shared and read-only.</summary>
    public static FormMessages Default { get; } = new();

    /// <summary>The error of a required field that has no value (<c>"This field is required"</c>). <see cref="FormFieldSchema.RequiredMessage"/> wins over it.</summary>
    public string Required { get; init; } = "This field is required";

    /// <summary>The error of a <see cref="FormFieldKind.Number"/> below <see cref="FormFieldSchema.Min"/>, given the bound (<c>"Value must be at least 3"</c>).</summary>
    public Func<double, string> MinValue { get; init; } = n => $"Value must be at least {n.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>The error of a <see cref="FormFieldKind.Number"/> above <see cref="FormFieldSchema.Max"/>, given the bound (<c>"Value must be at most 10"</c>).</summary>
    public Func<double, string> MaxValue { get; init; } = n => $"Value must be at most {n.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>The error of a text longer than <see cref="FormFieldSchema.MaxLength"/>, given the limit (<c>"Must be at most 20 characters"</c>).</summary>
    public Func<int, string> MaxLength { get; init; } = n => $"Must be at most {n.ToString(CultureInfo.InvariantCulture)} characters";

    /// <summary>The error of a <see cref="FormFieldKind.DateRange"/> whose end is before its start (<c>"The end date must not be before the start date"</c>).</summary>
    public string DateRangeOrder { get; init; } = "The end date must not be before the start date";

    /// <summary>The error of a <c>FormFieldKind.File</c> file above <c>FormFieldSchema.MaxFileSize</c>, given the file name and the limit as text (<c>"report.pdf is larger than 5 MB"</c>).</summary>
    public Func<string, string, string> FileTooLarge { get; init; } = (name, size) => $"{name} is larger than {size}";

    /// <summary>The error of a <c>FormFieldKind.File</c> file that does not match <c>FormFieldSchema.Accept</c>, given the file name and the accepted list (<c>"photo.gif is not an accepted type (.png,.jpg)"</c>).</summary>
    public Func<string, string, string> FileTypeNotAllowed { get; init; } = (name, accept) => $"{name} is not an accepted type ({accept})";

    /// <summary>The error of a <c>FormFieldKind.File</c> field without <see cref="FormFieldSchema.Multiple"/> that holds several files (<c>"Choose one file only"</c>).</summary>
    public string SingleFileOnly { get; init; } = "Choose one file only";
}
