using Klyroo.Form.Dynamic;
using System.Globalization;

namespace Klyroo.Form;

/// <summary>Controls which checks a <see cref="DynamicFormController"/> runs when it validates, set through <see cref="DynamicFormSchema.ValidateMode"/>.</summary>
public enum FormValidationMode
{
    /// <summary>Runs required checks, number bounds, password requirements and custom validators.</summary>
    All,
    /// <summary>Runs required checks only.</summary>
    RequiredOnly,
    /// <summary>Runs number bounds, password requirements and custom validators, and skips required checks.</summary>
    ValidatorsOnly,
    /// <summary>Runs no rule. <see cref="DynamicFormController.Validate"/> returns <c>true</c> unless a blocking error set with <see cref="DynamicFormController.SetError"/> exists or, in a typed form, a control holds something that is not a value of the type of its member (<see cref="FormMessages.InvalidValue"/>), which no write could use.</summary>
    None,
}

/// <summary>Controls when a field is validated while the user edits it, set through <see cref="DynamicFieldSchema.Trigger"/>.</summary>
public enum FormValidationTrigger
{
    /// <summary>Validates once the field has been touched, which <c>KlyFormEngine</c> does when the control loses focus. This is the default.</summary>
    Blur,
    /// <summary>Marks the field touched and validates it on every change.</summary>
    Input,
    /// <summary>Does not validate while editing. The field is validated by <see cref="DynamicFormController.Validate"/> and by submission, and a change clears its validation error without validating again.</summary>
    Submit,
}

/// <summary>Layout hint for the shell that hosts a form, used by <see cref="DynamicFormService"/> overlays and the <c>Shell</c> parameter of <c>KlyFormEngine</c>.</summary>
public enum FormShellMode
{
    /// <summary>Renders in the page flow. This is the default of <c>KlyFormEngine</c>.</summary>
    Inline,
    /// <summary>Renders in a centered dialog. This is the default of <see cref="DynamicFormService.CreateFormAsync"/>.</summary>
    Modal,
    /// <summary>Side panel. <c>KlyFormProvider</c> renders it as a full-height panel docked to the end (right in left-to-right layouts) of the viewport.</summary>
    Drawer,
    /// <summary>Full viewport. <c>KlyFormProvider</c> renders it over the whole viewport, without margins or rounded corners.</summary>
    Fullscreen,
}

/// <summary>Completion status of a section in a sectioned form, computed by <see cref="DynamicFormController.GetSectionStates"/>.</summary>
public enum FormPageSectionStatus
{
    /// <summary>Every required field of the section has a value and no field has an error.</summary>
    Complete,
    /// <summary>At least one required field of the section is empty.</summary>
    Pending,
    /// <summary>At least one field of the section has an error. This takes precedence over the other statuses.</summary>
    Invalid,
    /// <summary>The section is marked optional and none of its fields has a value.</summary>
    Optional,
}

/// <summary>Kind of a field in a <see cref="DynamicFieldSchema"/>. The kind decides how the field is stored, validated and rendered. <c>KlyFormEngine</c>, <c>KlyFormPage</c> and <c>KlyFormField</c> render every kind with a dedicated control.</summary>
public enum FormFieldKind
{
    /// <summary>Single-line text input. The value is a string.</summary>
    Text,
    /// <summary>Masked text input with a show/hide toggle. Rules in <see cref="DynamicFieldSchema.PasswordRequirements"/> are checked on validation.</summary>
    Password,
    /// <summary>Multi-line text input. The value is a string.</summary>
    Textarea,
    /// <summary>Number input. The value is a <see cref="double"/>, and <see cref="DynamicFieldSchema.Min"/> and <see cref="DynamicFieldSchema.Max"/> are enforced on validation.</summary>
    Number,
    /// <summary>Checkbox. The value is a <see cref="bool"/>.</summary>
    Checkbox,
    /// <summary>Checkbox exposed to assistive technology as a switch (<c>role="switch"</c>). The value is a <see cref="bool"/>.</summary>
    Switch,
    /// <summary>Radio group built from the field options. The value is the selected option value.</summary>
    Radio,
    /// <summary>Multiple choice rendered as a list of checkboxes built from the field options. The value is a <c>List&lt;string&gt;</c> of the checked option values.</summary>
    CheckboxGroup,
    /// <summary>Native select list built from the field options, with an <c>optgroup</c> per <see cref="FormOptionItem.Group"/>. The value is the selected option value. With <see cref="DynamicFieldSchema.Multiple"/> it renders a list of checkboxes and the value is a <c>List&lt;string&gt;</c>.</summary>
    Select,
    /// <summary>Text input that suggests the option labels through a <c>datalist</c>. The value is the value of the option whose label matches the text (ignoring case) or whose value equals it, otherwise the text itself; the typed text stays on screen until the input loses focus. With <see cref="DynamicFieldSchema.Multiple"/> it renders a list of checkboxes and the value is a <c>List&lt;string&gt;</c>.</summary>
    AutoComplete,
    /// <summary>Date input (<c>type="date"</c>). The value is the string produced by the browser, in <c>yyyy-MM-dd</c> form.</summary>
    Date,
    /// <summary>Date and time input (<c>type="datetime-local"</c>). The value is the string produced by the browser, in <c>yyyy-MM-ddTHH:mm</c> form.</summary>
    DateTime,
    /// <summary>Range of two dates, rendered as a start and an end date input. The value is a <c>List&lt;string&gt;</c> of two items, start then end, in <c>yyyy-MM-dd</c> form, with an empty string for a missing date, or <c>null</c> when both are empty. A required range needs both dates, and validation rejects an end before the start.</summary>
    DateRange,
    /// <summary>Time input (<c>type="time"</c>). The value is the string produced by the browser.</summary>
    Time,
    /// <summary>Phone number input (<c>type="tel"</c>, <c>autocomplete="tel"</c>). The value is a string; the format is not validated.</summary>
    PhoneNumber,
    /// <summary>Range slider (<c>type="range"</c>) bounded by <see cref="DynamicFieldSchema.Min"/>, <see cref="DynamicFieldSchema.Max"/> and <see cref="DynamicFieldSchema.Step"/>. The value is a <see cref="double"/>. While the value is <c>null</c> the slider shows <see cref="DynamicFieldSchema.Min"/> (or 0) and the state stays <c>null</c> until the user moves it. Bounds are only enforced on validation for <see cref="Number"/> fields.</summary>
    Slider,
    /// <summary>Star rating from 1 to <see cref="DynamicFieldSchema.Max"/> (5 by default), rendered as a group of radio buttons. The value is an <see cref="int"/>.</summary>
    Rating,
    /// <summary>Color input (<c>type="color"</c>). The value is the string produced by the browser, in <c>#rrggbb</c> form.</summary>
    ColorPicker,
    /// <summary>One-time code input (<c>autocomplete="one-time-code"</c>, <c>inputmode="numeric"</c>, <c>maxlength</c> from <see cref="DynamicFieldSchema.MaxLength"/>). The value is a string.</summary>
    OneTimeCode,
    /// <summary>List of free-form tags: Enter or a comma in the text input adds the typed tag, and each tag has a remove button. The value is a <c>List&lt;string&gt;</c> without blanks or duplicates.</summary>
    Tag,
    /// <summary>Hidden input. The value is stored and included in the output but nothing is shown to the user.</summary>
    Hidden,
    /// <summary>Read-only text block showing <see cref="DynamicFieldSchema.Label"/> and <see cref="DynamicFieldSchema.DisplayText"/> (or <see cref="DynamicFieldSchema.Description"/>). It is stateless: it has no value, is never validated and never appears in the output.</summary>
    Info,
    /// <summary>Horizontal rule. Stateless, like <see cref="Info"/>.</summary>
    Divider,
    /// <summary>Heading made of <see cref="DynamicFieldSchema.Label"/> and <see cref="DynamicFieldSchema.Description"/>. Stateless, like <see cref="Info"/>.</summary>
    Section,
    /// <summary>Button labelled by <see cref="DynamicFieldSchema.Label"/> that runs <see cref="DynamicFieldSchema.OnClick"/> and never submits the form. Stateless: it has no value, is never validated and never appears in the output.</summary>
    Button,
    /// <summary>Container that lays out the fields in <see cref="DynamicFieldSchema.Fields"/> without a border. The child fields keep their own keys and are validated and output as top-level fields, unless the container is hidden. A container is layout only: without <see cref="DynamicFieldSchema.Fields"/> it renders nothing and has no value.</summary>
    Group,
    /// <summary>Bordered container for the fields in <see cref="DynamicFieldSchema.Fields"/>. Children behave as in <see cref="Group"/>.</summary>
    Card,
    /// <summary>Tabbed container driven by <see cref="DynamicFieldSchema.Tabs"/>. The fields of every visible tab are validated and output, whether or not the tab is selected; the fields of a hidden tab are not. Without tabs it renders nothing and has no value.</summary>
    Tabs,
    /// <summary>Container with the same behavior as <see cref="Group"/>. Child values are not nested under the object key.</summary>
    Object,
    /// <summary>Repeatable list of rows, each row holding the fields declared in <see cref="DynamicFieldSchema.Fields"/>. The value is a <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>. Rows are edited with <c>AddArrayItem</c>, <c>SetArrayItemValue</c> and <c>RemoveArrayItem</c>, each cell is rendered with the control of its child kind, and each cell is validated with the rules of its child field.</summary>
    ArrayList,
    /// <summary>Same value and behavior as <see cref="ArrayList"/>; <c>KlyFormEngine</c> renders both identically.</summary>
    ArrayTable,
    /// <summary>A control you provide. <see cref="DynamicFieldSchema.CustomKey"/> names its renderer, a <c>RenderFragment&lt;FormControlContext&gt;</c> registered with <c>KlyrooFormOptions.CustomField</c> or passed in the <c>FieldTemplates</c> parameter of the components. The field is stateful like any input: it holds any value you store, is validated with <see cref="DynamicFieldSchema.Required"/> and <see cref="DynamicFieldSchema.Validators"/>, and is part of the output. The components render its label, description, help and error around your control.</summary>
    Custom,
    /// <summary>File input rendered with the Blazor <c>InputFile</c>. The value is a <see cref="FormFileValue"/> (or <c>null</c>), or a <c>List&lt;FormFileValue&gt;</c> with <see cref="DynamicFieldSchema.Multiple"/>. <see cref="DynamicFieldSchema.Accept"/>, <see cref="DynamicFieldSchema.MaxFileSize"/> and the number of files are checked on validation. A file is never part of a draft (<see cref="DynamicFormController.ExportState"/>), since its content cannot be restored.</summary>
    File,
}

/// <summary>The color of an info block (<see cref="FormFieldKind.Info"/>), from the Klyroo.UI theme.</summary>
public enum FormInfoTone
{
    /// <summary>The muted default.</summary>
    Neutral,
    /// <summary>The info color.</summary>
    Info,
    /// <summary>The success color.</summary>
    Success,
    /// <summary>The warning color.</summary>
    Warning,
    /// <summary>The error color.</summary>
    Error,
}

/// <summary>How an info block (<see cref="FormFieldKind.Info"/>) is drawn.</summary>
public enum FormInfoKind
{
    /// <summary>A tinted box with a border (the default).</summary>
    Callout,
    /// <summary>Plain text without a box.</summary>
    Text,
}

/// <summary>One choice of a <see cref="FormFieldKind.Select"/>, <see cref="FormFieldKind.AutoComplete"/>, <see cref="FormFieldKind.Radio"/> or <see cref="FormFieldKind.CheckboxGroup"/> field.</summary>
/// <param name="Value">The value stored in the form when the option is selected.</param>
/// <param name="Label">The text shown to the user.</param>
/// <param name="Description">Optional secondary text, shown under the label in radio and checkbox lists. A native select list cannot show it.</param>
/// <param name="Group">Optional group name. Options with the same group are rendered together, in an <c>optgroup</c> in select lists and under a heading in radio and checkbox lists, in the order the groups first appear.</param>
/// <param name="Icon">Optional icon name, in any form <c>KlyIcon</c> of Klyroo.UI accepts (<c>i-lucide-star</c>, <c>lucide:star</c> or a bare Lucide name), shown before the label in radio and checkbox lists. A native select list cannot show it.</param>
/// <param name="Disabled"><c>true</c> to show the option but prevent selecting it.</param>
public sealed record FormOptionItem(
    string Value,
    string Label,
    string? Description = null,
    string? Group = null,
    string? Icon = null,
    bool Disabled = false);

/// <summary>A rule a <see cref="FormFieldKind.Password"/> value must satisfy. The first failing rule becomes the field error, using its <see cref="Label"/> as the message.</summary>
/// <param name="Key">Identifier of the rule.</param>
/// <param name="Label">Description of the rule, used as the field error when the rule fails.</param>
/// <param name="Validate">Returns <c>true</c> when the password satisfies the rule.</param>
public sealed record FormPasswordRequirement(
    string Key,
    string Label,
    Func<string, bool> Validate);
