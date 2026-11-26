namespace Klyroo.Form;

/// <summary>
/// The built-in texts of <see cref="KlyFormEngine"/>, <see cref="KlyFormPage"/>, <see cref="KlyFormProvider"/>, <see cref="KlySchemaField"/>, <see cref="KlyFormErrorSummary"/> and
/// <see cref="KlyFormNavigationGuard"/>: button labels, accessible names, empty states, counters and the built-in validation messages. There is one set per locale of Klyroo.UI
/// (<see cref="FormLocales"/>), and a component shows the set of its current locale, the one its <c>Messages</c> come from. Change a set with <see cref="KlyrooFormOptions.Texts"/>
/// in <c>AddKlyrooForm(f =&gt; ...)</c>.
/// </summary>
/// <remarks>
/// A text that contains a number or a name is a function, so a language can choose its own plural forms and word order. Build a variant with a <c>with</c> expression. A new instance
/// holds the English texts. Text you put in the schema (labels, help, placeholders, <see cref="FormFieldSchema.RequiredMessage"/>) and the messages of your own validators are never translated.
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddKlyrooForm(f =&gt; f.Texts("fr", t =&gt; t with
/// {
///     Submit = "Envoyer",
///     Messages = t.Messages with { Required = "Merci de renseigner ce champ" },
/// }));
/// </code>
/// </example>
public sealed record FormTexts
{
    /// <summary>Creates the texts with their English defaults.</summary>
    public FormTexts()
    {
    }

    /// <summary>The built-in validation messages the components give to the controller they render (see <see cref="FormController.Messages"/>), unless the schema or the code chose its own.</summary>
    public FormMessages Messages { get; init; } = FormMessages.Default;

    /// <summary>The accessible name of the close button of an overlay form (<c>"Close form"</c>).</summary>
    public string CloseForm { get; init; } = "Close form";

    /// <summary>The Back button of a stepped form (<c>"Back"</c>).</summary>
    public string Back { get; init; } = "Back";

    /// <summary>The Next button of a stepped form (<c>"Next"</c>).</summary>
    public string Next { get; init; } = "Next";

    /// <summary>The Reset button of a form, of a section and of a field (<c>"Reset"</c>).</summary>
    public string Reset { get; init; } = "Reset";

    /// <summary>The Cancel button of a form (<c>"Cancel"</c>).</summary>
    public string Cancel { get; init; } = "Cancel";

    /// <summary>The submit button when <c>SubmitLabel</c> is not set (<c>"Submit"</c>).</summary>
    public string Submit { get; init; } = "Submit";

    /// <summary>The submit button while the form is submitting (<c>"Saving…"</c>).</summary>
    public string Saving { get; init; } = "Saving…";

    /// <summary>The heading and the accessible name of the section list of <c>KlyFormPage</c> (<c>"Form sections"</c>).</summary>
    public string FormSections { get; init; } = "Form sections";

    /// <summary>The summary of <c>KlyFormPage</c> when no section is pending or invalid (<c>"Everything is ready."</c>).</summary>
    public string AllReady { get; init; } = "Everything is ready.";

    /// <summary>The summary of <c>KlyFormPage</c>, given the number of sections left (<c>"2 left to complete."</c>).</summary>
    public Func<int, string> LeftToComplete { get; init; } = n => $"{n} left to complete.";

    /// <summary>The badge of a complete section (<c>"Completed"</c>).</summary>
    public string SectionComplete { get; init; } = "Completed";

    /// <summary>The badge of a section with an error (<c>"To fix"</c>).</summary>
    public string SectionInvalid { get; init; } = "To fix";

    /// <summary>The badge of an optional, empty section (<c>"Optional"</c>).</summary>
    public string SectionOptional { get; init; } = "Optional";

    /// <summary>The badge of a pending section, given the required fields done and the total (<c>"1 of 3"</c>).</summary>
    public Func<int, int, string> SectionProgress { get; init; } = (done, total) => $"{done} of {total}";

    /// <summary>The title of the error summary, given the number of errors (<c>"There are 2 errors to fix"</c>).</summary>
    public Func<int, string> ErrorsToFix { get; init; } = n => n == 1 ? "There is 1 error to fix" : $"There are {n} errors to fix";

    /// <summary>An entry of the error summary, given the field label and the message (<c>"Email: This field is required"</c>).</summary>
    public Func<string, string, string> FieldError { get; init; } = (label, message) => $"{label}: {message}";

    /// <summary>An entry of the error summary for a cell of a repeatable list, given the column label, the list label, the one-based row and the message (<c>"SKU (Lines, 2): This field is required"</c>).</summary>
    public Func<string, string, int, string, string> CellError { get; init; } = (column, list, row, message) => $"{column} ({list}, {row}): {message}";

    /// <summary>The pending state of a field being validated asynchronously (<c>"Checking…"</c>).</summary>
    public string Checking { get; init; } = "Checking…";

    /// <summary>The accessible name of the button that reveals a password (<c>"Show password"</c>).</summary>
    public string ShowPassword { get; init; } = "Show password";

    /// <summary>The accessible name of the button that hides a revealed password (<c>"Hide password"</c>).</summary>
    public string HidePassword { get; init; } = "Hide password";

    /// <summary>The visible text of the button that reveals a password (<c>"Show"</c>).</summary>
    public string Show { get; init; } = "Show";

    /// <summary>The visible text of the button that hides a password (<c>"Hide"</c>).</summary>
    public string Hide { get; init; } = "Hide";

    /// <summary>The accessible name of the password checklist (<c>"Password requirements"</c>).</summary>
    public string PasswordRequirements { get; init; } = "Password requirements";

    /// <summary>The hidden marker after a satisfied password requirement (<c>"(met)"</c>).</summary>
    public string RequirementMet { get; init; } = "(met)";

    /// <summary>The hidden marker after an unsatisfied password requirement (<c>"(not met)"</c>).</summary>
    public string RequirementNotMet { get; init; } = "(not met)";

    /// <summary>The accessible name of the clear button of a control (<c>"Clear"</c>).</summary>
    public string Clear { get; init; } = "Clear";

    /// <summary>The accessible name of the first input of a date range (<c>"Start date"</c>).</summary>
    public string StartDate { get; init; } = "Start date";

    /// <summary>The accessible name of the second input of a date range (<c>"End date"</c>).</summary>
    public string EndDate { get; init; } = "End date";

    /// <summary>The accessible name of a star, given its number and the number of stars (<c>"3 of 5"</c>).</summary>
    public Func<int, int, string> RatingStar { get; init; } = (star, count) => $"{star} of {count}";

    /// <summary>The empty option of a select list without a <c>Placeholder</c> (<c>"Select…"</c>).</summary>
    public string SelectPlaceholder { get; init; } = "Select…";

    /// <summary>The placeholder of a tag input without a <c>Placeholder</c> (<c>"Add a tag and press Enter"</c>).</summary>
    public string TagPlaceholder { get; init; } = "Add a tag and press Enter";

    /// <summary>The accessible name of the tag list of a field without a label (<c>"Tags"</c>).</summary>
    public string Tags { get; init; } = "Tags";

    /// <summary>The accessible name of the button that removes a tag, given the tag (<c>"Remove news"</c>).</summary>
    public Func<string, string> RemoveTag { get; init; } = tag => $"Remove {tag}";

    /// <summary>The placeholder of the input that creates an option (<c>"New option"</c>).</summary>
    public string NewOption { get; init; } = "New option";

    /// <summary>The accessible name of that input, given the field label (<c>"New option for Country"</c>).</summary>
    public Func<string, string> NewOptionFor { get; init; } = label => $"New option for {label}";

    /// <summary>The button that creates an option (<c>"Create"</c>).</summary>
    public string Create { get; init; } = "Create";

    /// <summary>The add button of a repeatable list without an <c>AddItemLabel</c> (<c>"Add item"</c>).</summary>
    public string AddItem { get; init; } = "Add item";

    /// <summary>The text of an empty repeatable list (<c>"No items yet"</c>).</summary>
    public string NoItemsYet { get; init; } = "No items yet";

    /// <summary>The accessible name of the button that removes a row, given its one-based number (<c>"Remove item 2"</c>).</summary>
    public Func<int, string> RemoveItem { get; init; } = n => $"Remove item {n}";

    /// <summary>The accessible name of an overlay form without a title (<c>"Form"</c>).</summary>
    public string FormTitle { get; init; } = "Form";

    /// <summary>The question of the default confirmation of <c>KlyFormNavigationGuard</c> (<c>"You have unsaved changes. Leave this page and discard them?"</c>).</summary>
    public string UnsavedChanges { get; init; } = "You have unsaved changes. Leave this page and discard them?";

    /// <summary>The button of a file field that takes one file (<c>"Choose file"</c>).</summary>
    public string ChooseFile { get; init; } = "Choose file";

    /// <summary>The button of a file field with <c>Multiple</c> (<c>"Choose files"</c>).</summary>
    public string ChooseFiles { get; init; } = "Choose files";

    /// <summary>The text of a file field without a file (<c>"No file chosen"</c>).</summary>
    public string NoFileChosen { get; init; } = "No file chosen";

    /// <summary>The accessible name of the button that removes a file, given its name (<c>"Remove report.pdf"</c>).</summary>
    public Func<string, string> RemoveFile { get; init; } = name => $"Remove {name}";

    /// <summary>The form-level error shown when saving, an asynchronous rule or a handler throws (<c>"Something went wrong. Try again."</c>).</summary>
    public string SomethingWentWrong { get; init; } = "Something went wrong. Try again.";
}
