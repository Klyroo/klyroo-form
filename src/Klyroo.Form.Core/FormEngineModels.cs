using System.Globalization;

namespace Klyroo.Form;

/// <summary>Controls which checks a <see cref="FormController"/> runs when it validates, set through <see cref="FormSchema.ValidateMode"/>.</summary>
public enum FormValidationMode
{
    /// <summary>Runs required checks, number bounds, password requirements and custom validators.</summary>
    All,
    /// <summary>Runs required checks only.</summary>
    RequiredOnly,
    /// <summary>Runs number bounds, password requirements and custom validators, and skips required checks.</summary>
    ValidatorsOnly,
    /// <summary>Runs no validation. <see cref="FormController.Validate"/> returns <c>true</c> unless a blocking error set with <see cref="FormController.SetError"/> exists.</summary>
    None,
}

/// <summary>Controls when a field is validated while the user edits it, set through <see cref="FormFieldSchema.Trigger"/>.</summary>
public enum FormValidationTrigger
{
    /// <summary>Validates once the field has been touched, which <c>KlyFormEngine</c> does when the control loses focus. This is the default.</summary>
    Blur,
    /// <summary>Marks the field touched and validates it on every change.</summary>
    Input,
    /// <summary>Does not validate while editing. The field is validated by <see cref="FormController.Validate"/> and by submission, and a change clears its validation error without validating again.</summary>
    Submit,
}

/// <summary>Layout hint for the shell that hosts a form, used by <see cref="FormService"/> overlays and the <c>Shell</c> parameter of <c>KlyFormEngine</c>.</summary>
public enum FormShellMode
{
    /// <summary>Renders in the page flow. This is the default of <c>KlyFormEngine</c>.</summary>
    Inline,
    /// <summary>Renders in a centered dialog. This is the default of <see cref="FormService.CreateFormAsync"/>.</summary>
    Modal,
    /// <summary>Side panel. <c>KlyFormProvider</c> renders it as a full-height panel docked to the end (right in left-to-right layouts) of the viewport.</summary>
    Drawer,
    /// <summary>Full viewport. <c>KlyFormProvider</c> renders it over the whole viewport, without margins or rounded corners.</summary>
    Fullscreen,
}

/// <summary>Completion status of a section in a sectioned form, computed by <see cref="FormController.GetSectionStates"/>.</summary>
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

/// <summary>Kind of a field in a <see cref="FormFieldSchema"/>. The kind decides how the field is stored, validated and rendered. <c>KlyFormEngine</c>, <c>KlyFormPage</c> and <c>KlyFormField</c> render every kind with a dedicated control.</summary>
public enum FormFieldKind
{
    /// <summary>Single-line text input. The value is a string.</summary>
    Text,
    /// <summary>Masked text input with a show/hide toggle. Rules in <see cref="FormFieldSchema.PasswordRequirements"/> are checked on validation.</summary>
    Password,
    /// <summary>Multi-line text input. The value is a string.</summary>
    Textarea,
    /// <summary>Number input. The value is a <see cref="double"/>, and <see cref="FormFieldSchema.Min"/> and <see cref="FormFieldSchema.Max"/> are enforced on validation.</summary>
    Number,
    /// <summary>Checkbox. The value is a <see cref="bool"/>.</summary>
    Checkbox,
    /// <summary>Checkbox exposed to assistive technology as a switch (<c>role="switch"</c>). The value is a <see cref="bool"/>.</summary>
    Switch,
    /// <summary>Radio group built from the field options. The value is the selected option value.</summary>
    Radio,
    /// <summary>Multiple choice rendered as a list of checkboxes built from the field options. The value is a <c>List&lt;string&gt;</c> of the checked option values.</summary>
    CheckboxGroup,
    /// <summary>Native select list built from the field options, with an <c>optgroup</c> per <see cref="FormOptionItem.Group"/>. The value is the selected option value. With <see cref="FormFieldSchema.Multiple"/> it renders a list of checkboxes and the value is a <c>List&lt;string&gt;</c>.</summary>
    Select,
    /// <summary>Text input that suggests the option labels through a <c>datalist</c>. The value is the value of the option whose label matches the text (ignoring case) or whose value equals it, otherwise the text itself; the typed text stays on screen until the input loses focus. With <see cref="FormFieldSchema.Multiple"/> it renders a list of checkboxes and the value is a <c>List&lt;string&gt;</c>.</summary>
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
    /// <summary>Range slider (<c>type="range"</c>) bounded by <see cref="FormFieldSchema.Min"/>, <see cref="FormFieldSchema.Max"/> and <see cref="FormFieldSchema.Step"/>. The value is a <see cref="double"/>. While the value is <c>null</c> the slider shows <see cref="FormFieldSchema.Min"/> (or 0) and the state stays <c>null</c> until the user moves it. Bounds are only enforced on validation for <see cref="Number"/> fields.</summary>
    Slider,
    /// <summary>Star rating from 1 to <see cref="FormFieldSchema.Max"/> (5 by default), rendered as a group of radio buttons. The value is an <see cref="int"/>.</summary>
    Rating,
    /// <summary>Color input (<c>type="color"</c>). The value is the string produced by the browser, in <c>#rrggbb</c> form.</summary>
    ColorPicker,
    /// <summary>One-time code input (<c>autocomplete="one-time-code"</c>, <c>inputmode="numeric"</c>, <c>maxlength</c> from <see cref="FormFieldSchema.MaxLength"/>). The value is a string.</summary>
    OneTimeCode,
    /// <summary>List of free-form tags: Enter or a comma in the text input adds the typed tag, and each tag has a remove button. The value is a <c>List&lt;string&gt;</c> without blanks or duplicates.</summary>
    Tag,
    /// <summary>Hidden input. The value is stored and included in the output but nothing is shown to the user.</summary>
    Hidden,
    /// <summary>Read-only text block showing <see cref="FormFieldSchema.Label"/> and <see cref="FormFieldSchema.DisplayText"/> (or <see cref="FormFieldSchema.Description"/>). It is stateless: it has no value, is never validated and never appears in the output.</summary>
    Info,
    /// <summary>Horizontal rule. Stateless, like <see cref="Info"/>.</summary>
    Divider,
    /// <summary>Heading made of <see cref="FormFieldSchema.Label"/> and <see cref="FormFieldSchema.Description"/>. Stateless, like <see cref="Info"/>.</summary>
    Section,
    /// <summary>Button labelled by <see cref="FormFieldSchema.Label"/> that runs <see cref="FormFieldSchema.OnClick"/> and never submits the form. Stateless: it has no value, is never validated and never appears in the output.</summary>
    Button,
    /// <summary>Container that lays out the fields in <see cref="FormFieldSchema.Fields"/> without a border. The child fields keep their own keys and are validated and output as top-level fields, unless the container is hidden. A container is layout only: without <see cref="FormFieldSchema.Fields"/> it renders nothing and has no value.</summary>
    Group,
    /// <summary>Bordered container for the fields in <see cref="FormFieldSchema.Fields"/>. Children behave as in <see cref="Group"/>.</summary>
    Card,
    /// <summary>Tabbed container driven by <see cref="FormFieldSchema.Tabs"/>. The fields of every visible tab are validated and output, whether or not the tab is selected; the fields of a hidden tab are not. Without tabs it renders nothing and has no value.</summary>
    Tabs,
    /// <summary>Container with the same behavior as <see cref="Group"/>. Child values are not nested under the object key.</summary>
    Object,
    /// <summary>Repeatable list of rows, each row holding the fields declared in <see cref="FormFieldSchema.Fields"/>. The value is a <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>. Rows are edited with <c>AddArrayItem</c>, <c>SetArrayItemValue</c> and <c>RemoveArrayItem</c>, each cell is rendered with the control of its child kind, and each cell is validated with the rules of its child field.</summary>
    ArrayList,
    /// <summary>Same value and behavior as <see cref="ArrayList"/>; <c>KlyFormEngine</c> renders both identically.</summary>
    ArrayTable,
    /// <summary>A control you provide. <see cref="FormFieldSchema.CustomKey"/> names its renderer, a <c>RenderFragment&lt;FormControlContext&gt;</c> registered with <c>KlyrooFormOptions.CustomField</c> or passed in the <c>FieldTemplates</c> parameter of the components. The field is stateful like any input: it holds any value you store, is validated with <see cref="FormFieldSchema.Required"/> and <see cref="FormFieldSchema.Validators"/>, and is part of the output. The components render its label, description, help and error around your control.</summary>
    Custom,
    /// <summary>File input rendered with the Blazor <c>InputFile</c>. The value is a <see cref="FormFileValue"/> (or <c>null</c>), or a <c>List&lt;FormFileValue&gt;</c> with <see cref="FormFieldSchema.Multiple"/>. <see cref="FormFieldSchema.Accept"/>, <see cref="FormFieldSchema.MaxFileSize"/> and the number of files are checked on validation. A file is never part of a draft (<see cref="FormController.ExportState"/>), since its content cannot be restored.</summary>
    File,
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

/// <summary>A custom validation rule of a field. Rules run in order after the built-in checks and the first failing rule sets the field error.</summary>
/// <param name="Key">Identifier of the rule.</param>
/// <param name="Message">Error message set on the field when the rule fails.</param>
/// <param name="Validate">Receives the current field value and the controller, and returns <c>true</c> when the value is valid. It also runs for empty values of fields that are not required, so handle <c>null</c>.</param>
public sealed record FormValidatorRule(
    string Key,
    string Message,
    Func<object?, FormController, bool> Validate);

/// <summary>
/// An asynchronous validation rule of a field, for a check that needs I/O such as "is this username free". It runs in the background once the user has left the field
/// (and again, debounced, as they edit it), a newer run cancels an older one, and <see cref="FormController.SubmitAsync()"/> and <see cref="FormController.NextStepAsync"/>
/// wait for it. It does not run for an empty value or for a value that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list.
/// </summary>
/// <param name="Key">Identifier of the rule.</param>
/// <param name="Validate">Receives the current field value, the controller and a token that is cancelled when the value changes or a newer run starts. Returns the error message, or <c>null</c> when the value is valid. An exception it throws in a background run is swallowed and the value stays unchecked until submission, where the exception propagates to the caller.</param>
public sealed record FormAsyncValidatorRule(
    string Key,
    Func<object?, FormController, CancellationToken, ValueTask<string?>> Validate)
{
    /// <summary>How long the value must stay unchanged before a background run starts, so typing does not call the service on every key. The default is 300 milliseconds. Submission runs the rule at once.</summary>
    public TimeSpan Debounce { get; init; } = TimeSpan.FromMilliseconds(300);
}

/// <summary>One tab of a <see cref="FormFieldKind.Tabs"/> field.</summary>
public sealed class FormTabItemSchema
{
    /// <summary>Unique key of the tab among its siblings.</summary>
    public required string Key { get; init; }
    /// <summary>Text shown on the tab.</summary>
    public required string Label { get; init; }
    /// <summary>Fields shown when the tab is selected.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    /// <summary>Returns <c>false</c> to hide the tab. A hidden tab's fields are skipped by validation and left out of <see cref="FormController.BuildOutput"/>, and their values stay in the state.</summary>
    public Func<FormController, bool>? Condition { get; init; }
}

/// <summary>Declarative definition of one field, or of a layout element such as a section heading, a container or a repeatable list. Fields are immutable; build them with object initializers.</summary>
public sealed class FormFieldSchema
{
    /// <summary>Unique key of the field. It is the key of the value in the form state and in the output dictionary. Keys are flat strings: containers do not prefix the keys of their children.</summary>
    public required string Key { get; init; }
    /// <summary>The kind of the field. The default is <see cref="FormFieldKind.Text"/>.</summary>
    public FormFieldKind Kind { get; init; } = FormFieldKind.Text;
    /// <summary>Text of the label shown above the control. For <see cref="FormFieldKind.Info"/>, <see cref="FormFieldKind.Section"/> and containers it is the heading.</summary>
    public string? Label { get; init; }
    /// <summary>Text shown under the label. For <see cref="FormFieldKind.Info"/> it is the fallback of <see cref="DisplayText"/>.</summary>
    public string? Description { get; init; }
    /// <summary>Short text shown next to the label.</summary>
    public string? Hint { get; init; }
    /// <summary>Text shown under the control.</summary>
    public string? Help { get; init; }
    /// <summary>Placeholder of the control. For <see cref="FormFieldKind.Select"/> and <see cref="FormFieldKind.AutoComplete"/> it is the text of the empty option.</summary>
    public string? Placeholder { get; init; }
    /// <summary>Initial value used when the initial input has no value for the key. Ignored when <see cref="DefaultFactory"/> is set.</summary>
    public object? DefaultValue { get; init; }
    /// <summary>Computes the initial value from the controller. It takes precedence over <see cref="DefaultValue"/> and also fills the cells of a new row of a repeatable list.</summary>
    public Func<FormController, object?>? DefaultFactory { get; init; }

    /// <summary><c>true</c> if the field must have a value. An empty string, a whitespace string, <c>null</c> and an empty collection count as empty; <c>false</c> and <c>0</c> do not.</summary>
    public bool Required { get; init; }
    /// <summary>Makes the field required while it returns <c>true</c>. It is evaluated on every validation, in addition to <see cref="Required"/>.</summary>
    public Func<FormController, bool>? RequiredWhen { get; init; }
    /// <summary>Error message of a failed required check. The default is <c>This field is required</c>.</summary>
    public string? RequiredMessage { get; init; }
    /// <summary>When the field is validated while the user edits it. The default is <see cref="FormValidationTrigger.Blur"/>.</summary>
    public FormValidationTrigger Trigger { get; init; } = FormValidationTrigger.Blur;
    /// <summary>Keys of the fields whose value this field's rules read, for example a confirmation field that compares itself with <c>password</c>. Once this field has been touched, a change of any of those fields validates it again, even when it shows no error yet, so a mismatch appears (and disappears) as soon as the other field changes. Without it, a touched field that currently shows an error is still validated again after any other field changes, which clears stale errors but cannot raise a new one. Ignored with <see cref="FormValidationTrigger.Submit"/>. On a child of a repeatable list it applies to the cells of that column.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary><c>true</c> to render the control disabled; on a repeatable list it also disables its cells and the add and remove buttons. This only affects rendering: a disabled field is still validated and included in the output.</summary>
    public bool Disabled { get; init; }
    /// <summary>Disables the field while it returns <c>true</c>. Same scope as <see cref="Disabled"/>.</summary>
    public Func<FormController, bool>? DisabledWhen { get; init; }
    /// <summary>Text shown under a disabled control to explain why it is disabled.</summary>
    public string? DisabledReason { get; init; }

    /// <summary>Returns <c>false</c> to hide the field. A hidden field is skipped by validation and left out of <see cref="FormController.BuildOutput"/>, but its value stays in the state. A hidden container hides its children too, with the same effect on validation and output. On a child of a repeatable list it hides that column: its cells are neither rendered nor validated, and the column is left out of the rows of the output, while its values stay in the state.</summary>
    public Func<FormController, bool>? Condition { get; init; }
    /// <summary><c>true</c> to drop the field entirely: it has no state, is not rendered, validated or output, and its children are dropped with it.</summary>
    public bool Ignore { get; init; }
    /// <summary><c>true</c> to validate and store the field but leave it out of <see cref="FormController.BuildOutput"/>.</summary>
    public bool OmitFromOutput { get; init; }
    /// <summary><c>true</c> to show a per-field reset button in <c>KlyFormEngine</c> once the value differs from its initial value.</summary>
    public bool DirtyCheck { get; init; }
    /// <summary><c>true</c> to render a <see cref="FormFieldKind.Group"/>, <see cref="FormFieldKind.Card"/> or <see cref="FormFieldKind.Object"/> container as a disclosure (<c>details</c> and <c>summary</c>) the user can collapse, with <see cref="Label"/> as its summary. Collapsing only hides the children: they are still validated and output.</summary>
    public bool Collapsible { get; init; }
    /// <summary><c>true</c> to start a <see cref="Collapsible"/> container collapsed. Ignored when <see cref="Collapsible"/> is <c>false</c>.</summary>
    public bool DefaultCollapsed { get; init; }
    /// <summary>Number of columns the field occupies in the grid of <see cref="FormSchema.Columns"/> columns. Values are clamped between 1 and the column count, so the default of 12 fills a row.</summary>
    public int Span { get; init; } = 12;

    // Bounds & rules
    /// <summary>Smallest accepted value of a <see cref="FormFieldKind.Number"/> field, enforced on validation. Also the <c>min</c> attribute of number and slider inputs.</summary>
    public double? Min { get; init; }
    /// <summary>Largest accepted value of a <see cref="FormFieldKind.Number"/> field, enforced on validation. Also the <c>max</c> attribute of number and slider inputs, and the number of stars of a <see cref="FormFieldKind.Rating"/> field.</summary>
    public double? Max { get; init; }
    /// <summary>The <c>step</c> attribute of number and slider inputs. It is not validated.</summary>
    public double? Step { get; init; }
    /// <summary>The accepted files of a <see cref="FormFieldKind.File"/> field, in the syntax of the HTML <c>accept</c> attribute: comma-separated extensions (<c>.png</c>), content types (<c>image/png</c>) and wildcards (<c>image/*</c>). It is the <c>accept</c> attribute of the input, which only filters the picker, so validation checks it too (see <see cref="FormFileValue.Matches"/>).</summary>
    public string? Accept { get; init; }
    /// <summary>The largest accepted size in bytes of each file of a <see cref="FormFieldKind.File"/> field. The components read a chosen file into memory when it is chosen, so this is also the most memory one file can take (times the number of files with <see cref="Multiple"/>, per field, for as long as the value is held). It is enforced on validation and when the content is read or opened. <c>null</c> means <see cref="FormFileValue.DefaultMaxSize"/> (10 MiB); set a larger value on purpose if you accept larger files.</summary>
    public long? MaxFileSize { get; init; }
    /// <summary>Names the renderer of a <see cref="FormFieldKind.Custom"/> field: the key it was registered under with <c>KlyrooFormOptions.CustomField</c>, or the key in the <c>FieldTemplates</c> parameter. Keys are case sensitive. A custom field without a registered renderer shows a notice instead of a control.</summary>
    public string? CustomKey { get; init; }
    /// <summary>Largest accepted number of characters of a text value, enforced on validation. Also the <c>maxlength</c> attribute of text-like inputs, including textareas, passwords and one-time codes.</summary>
    public int? MaxLength { get; init; }
    /// <summary><c>true</c> if a <see cref="FormFieldKind.File"/> field takes several files (its value is a <c>List&lt;FormFileValue&gt;</c>), or if a <see cref="FormFieldKind.Select"/> or <see cref="FormFieldKind.AutoComplete"/> field holds several values: it renders a list of checkboxes and the value is a <c>List&lt;string&gt;</c>. <see cref="FormController.CreateFieldOption"/> appends to that list.</summary>
    public bool Multiple { get; init; }
    /// <summary><c>true</c> to show a clear button once the field has a value, next to select lists, text-like inputs, textareas, passwords, number inputs and ratings. Clearing sets the value to <c>null</c>, or to an empty string for text-like inputs.</summary>
    public bool Clearable { get; init; }
    /// <summary>Static options of a choice field. Ignored when <see cref="OptionsFactory"/> is set.</summary>
    public IReadOnlyList<FormOptionItem>? Options { get; init; }
    /// <summary>Computes the options from the controller, for example to depend on another field. It takes precedence over <see cref="Options"/>.</summary>
    public Func<FormController, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; init; }
    /// <summary>Turns text typed by the user into a new option. On a <see cref="FormFieldKind.Select"/> or <see cref="FormFieldKind.AutoComplete"/> field the built-in components render a text input and a create button under the control, which call <see cref="FormController.CreateFieldOption"/>.</summary>
    public Func<string, FormOptionItem?>? OnCreateOption { get; init; }
    /// <summary>Rules a <see cref="FormFieldKind.Password"/> value must satisfy. They are checked on validation, in order, and the built-in components show them as a live checklist under the field.</summary>
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; init; }
    /// <summary>Custom validation rules, run in order after the built-in checks. The first failing rule sets the error.</summary>
    public IReadOnlyList<FormValidatorRule>? Validators { get; init; }
    /// <summary>Asynchronous rules, run after the synchronous checks pass and the value is not empty. They run in the background after the field is touched and while it is edited (see <see cref="FormAsyncValidatorRule.Debounce"/>), and <see cref="FormController.SubmitAsync()"/>, <see cref="FormController.NextStepAsync"/> and <see cref="FormController.ValidateAsync"/> wait for them. <see cref="FormController.IsValidating"/> tells whether one is running, which the components show next to the field.</summary>
    public IReadOnlyList<FormAsyncValidatorRule>? AsyncValidators { get; init; }

    // Transforms & watchers
    /// <summary>Converts a value from the initial input into the value held in the state. It does not apply to default values or to values set later.</summary>
    public Func<object?, object?>? TransformInput { get; init; }
    /// <summary>Converts the value in <see cref="FormController.BuildOutput"/>, for example to trim a string. The state keeps the untransformed value.</summary>
    public Func<object?, object?>? TransformOutput { get; init; }
    /// <summary>Called with the new value and the controller after the value of this field changes: after <see cref="FormController.SetValue"/>, after <see cref="FormController.ResetField"/> when it changes the value, and, for a repeatable list, after a row is added or removed or a cell changes. On a child of a repeatable list it is called with the cell value after <see cref="FormController.SetArrayItemValue"/>. It does not run for <see cref="FormController.Reset"/>, which loads a complete new state.</summary>
    public Action<object?, FormController>? Watch { get; init; }
    /// <summary>Runs when the user clicks a <see cref="FormFieldKind.Button"/> field. The button never submits the form, and is disabled like other fields by <see cref="Disabled"/> and <see cref="DisabledWhen"/>.</summary>
    public Func<FormController, Task>? OnClick { get; init; }

    // Display / Info block
    /// <summary>Presentation of a <see cref="FormFieldKind.Info"/> block: <c>callout</c> (the default) draws a tinted box with a border, <c>text</c> renders plain text without a box. Other values render as <c>callout</c>. Case is ignored.</summary>
    public string? DisplayKind { get; init; }
    /// <summary>Text of a <see cref="FormFieldKind.Info"/> block. Falls back to <see cref="Description"/>.</summary>
    public string? DisplayText { get; init; }
    /// <summary>Color of a <see cref="FormFieldKind.Info"/> block, from the Klyroo.UI theme: <c>neutral</c> (the default), <c>info</c>, <c>success</c>, <c>warning</c> or <c>error</c>. Other values render as <c>neutral</c>. Case is ignored.</summary>
    public string? DisplayTone { get; init; }

    // Container / Array children
    /// <summary>Child fields: the content of a container, or the columns of each row of a repeatable list. A container without children renders nothing.</summary>
    public IReadOnlyList<FormFieldSchema>? Fields { get; init; }
    /// <summary>Tabs of a <see cref="FormFieldKind.Tabs"/> field. A tabs field without tabs renders nothing.</summary>
    public IReadOnlyList<FormTabItemSchema>? Tabs { get; init; }
    /// <summary>Label of the add button of a repeatable list. The default is <c>Add item</c>.</summary>
    public string? AddItemLabel { get; init; }
}

/// <summary>One step of a multi-step form, declared in <see cref="FormSchema.Steps"/>.</summary>
public sealed class FormStepSchema
{
    /// <summary>Unique key of the step.</summary>
    public required string Key { get; init; }
    /// <summary>Title shown in the stepper.</summary>
    public required string Title { get; init; }
    /// <summary>Optional text shown under the stepper while the step is current.</summary>
    public string? Description { get; init; }
    /// <summary>Fields of the step.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    /// <summary>Returns <c>true</c> to skip the step: the controller starts on the first step that is not skipped, <see cref="FormController.NextStepAsync"/> and <see cref="FormController.PreviousStepAsync"/> pass over it, the stepper does not list it, and its fields are skipped by validation and left out of <see cref="FormController.BuildOutput"/>. The current step is never treated as skipped.</summary>
    public Func<FormController, bool>? SkipWhen { get; init; }
}

/// <summary>One section of a sectioned form, declared in <see cref="FormSchema.Sections"/> and rendered by <c>KlyFormPage</c>.</summary>
public sealed class FormPageSectionSchema
{
    /// <summary>Unique key of the section. It is also the anchor id of the section in <c>KlyFormPage</c>.</summary>
    public required string Key { get; init; }
    /// <summary>Title of the section.</summary>
    public required string Label { get; init; }
    /// <summary>Text shown under the title.</summary>
    public string? Description { get; init; }
    /// <summary><c>true</c> if the section may stay empty: it then reports <see cref="FormPageSectionStatus.Optional"/> until a field has a value.</summary>
    public bool Optional { get; init; }
    /// <summary>Number of columns, from 1 to 12, the section occupies in the 12-column grid of sections of <c>KlyFormPage</c>. Values outside the range are clamped. The default is 12, one section per row.</summary>
    public int Span { get; init; } = 12;
    /// <summary>Returns <c>false</c> to hide the section and leave it out of <see cref="FormController.GetSectionStates"/>. Its fields are skipped by validation and left out of <see cref="FormController.BuildOutput"/>, and their values stay in the state.</summary>
    public Func<FormController, bool>? Condition { get; init; }
    /// <summary>Fields of the section.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
}

/// <summary>Completion state of one section, computed from the current values by <see cref="FormController.GetSectionStates"/>.</summary>
/// <param name="Key">Key of the section.</param>
/// <param name="Label">Label of the section.</param>
/// <param name="Description">Description of the section.</param>
/// <param name="Optional">Whether the section is optional.</param>
/// <param name="RequiredCount">Number of required, visible fields in the section.</param>
/// <param name="MissingCount">Number of those required fields that are empty.</param>
/// <param name="Dirty"><c>true</c> when any field of the section differs from its initial value.</param>
/// <param name="Status">Completion status of the section.</param>
public sealed record FormPageSectionState(
    string Key,
    string Label,
    string? Description,
    bool Optional,
    int RequiredCount,
    int MissingCount,
    bool Dirty,
    FormPageSectionStatus Status);

/// <summary>Declarative description of a form: its fields, optional steps or sections, validation mode and submit hooks. A schema is plain data and can be shared between controllers.</summary>
public sealed class FormSchema
{
    /// <summary>Identifier of the form, written to the <c>data-key</c> attribute of the rendered form. The default is <c>form</c>.</summary>
    public string FormKey { get; init; } = "form";
    /// <summary>Title shown in the form header.</summary>
    public string? Title { get; init; }
    /// <summary>Text shown under the title.</summary>
    public string? Description { get; init; }
    /// <summary>Small label shown above the title.</summary>
    public string? Eyebrow { get; init; }
    /// <summary>Number of columns, from 1 to 12, of the grids that lay out the fields, including those of containers and tabs. Values outside the range are clamped. <see cref="FormFieldSchema.Span"/> is clamped to it. The default is 12.</summary>
    public int Columns { get; init; } = 12;
    /// <summary><c>true</c> to show the list of steps above a stepped form. The default is <c>true</c>.</summary>
    public bool ShowStepper { get; init; } = true;
    /// <summary>Which checks validation runs. The default is <see cref="FormValidationMode.All"/>.</summary>
    public FormValidationMode ValidateMode { get; init; } = FormValidationMode.All;
    /// <summary>The built-in validation messages of this form. When <c>null</c> (the default) the controller uses the English messages, or those of the current locale when a Klyroo.Form component renders it. Set it to fix the language of the form whatever the locale is.</summary>
    public FormMessages? Messages { get; init; }
    /// <summary>Keys excluded from dirty tracking. A key also excludes the keys that start with it followed by a dot.</summary>
    public IReadOnlyList<string>? IgnoreDirtyPaths { get; init; }
    /// <summary>Fields of a single-block form. Ignored when <see cref="Steps"/> or <see cref="Sections"/> is not empty.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    /// <summary>Steps of a wizard. When not empty, they replace <see cref="Sections"/> and <see cref="Fields"/>.</summary>
    public IReadOnlyList<FormStepSchema> Steps { get; init; } = [];
    /// <summary>Sections of a page form. Used when <see cref="Steps"/> is empty, and replaces <see cref="Fields"/>.</summary>
    public IReadOnlyList<FormPageSectionSchema> Sections { get; init; } = [];

    /// <summary>Runs after validation succeeds and before <see cref="OnSubmit"/>. Return <c>false</c> to cancel the submission, for example after a failed server check.</summary>
    public Func<FormController, Task<bool>>? OnBeforeSubmit { get; init; }
    /// <summary>Runs when the form is submitted and validation (and <see cref="OnBeforeSubmit"/>) succeeded. An exception it throws propagates to the caller of <see cref="FormController.SubmitAsync()"/>.</summary>
    public Func<FormController, Task>? OnSubmit { get; init; }
    /// <summary>Runs after the current step validates and before moving forward, with the index of the current step. Return <c>false</c> to stay on the step.</summary>
    public Func<FormController, int, Task<bool>>? OnBeforeNext { get; init; }
    /// <summary>Runs before moving back, with the index of the current step. Return <c>false</c> to stay on the step.</summary>
    public Func<FormController, int, Task<bool>>? OnBeforePrevious { get; init; }
}

/// <summary>Result of an overlay opened with <see cref="FormService.CreateFormAsync"/>.</summary>
/// <param name="IsCompleted"><c>true</c> when the form was submitted successfully, <c>false</c> when it was closed without submitting.</param>
/// <param name="FormData">The output of the form at that moment, as returned by <see cref="FormController.BuildOutput"/>.</param>
public sealed record FormSubmitOutcome(
    bool IsCompleted,
    IReadOnlyDictionary<string, object?> FormData);

/// <summary>Runtime state of a <see cref="FormSchema"/>: field values, an initial snapshot for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time.</summary>
/// <remarks>Dispose it when it is no longer used: it cancels the asynchronous validations still waiting or running. A controller you pass to a component stays yours to dispose; one the component creates from a schema is disposed with the component.</remarks>
public sealed partial class FormController : IDisposable
{
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _initialSnapshot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _validationErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Message, bool Blocking)> _customErrors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _touchedPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<CellKey, string> _cellErrors = [];
    private readonly HashSet<CellKey> _touchedCells = [];
    private readonly Dictionary<string, List<FormOptionItem>> _createdOptions = new(StringComparer.Ordinal);
    private bool _navigating;
    private readonly Dictionary<string, (object? Value, string? Message)> _asyncResults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _pendingValidations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BackgroundRun> _backgroundRuns = new(StringComparer.Ordinal);

    private readonly record struct CellKey(string Array, int Row, string Child)
    {
        public override string ToString() => CellPath(Array, Row, Child);
    }

    /// <summary>Creates a controller for <paramref name="schema"/> and initializes the state. Values in <paramref name="initialInput"/> win over default values, and the result becomes the baseline for dirty tracking. A stepped form starts on its first step that is not skipped.</summary>
    /// <param name="schema">The schema to run.</param>
    /// <param name="initialInput">Initial values by field key. Keys that match no field are kept in <see cref="InternalState"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <c>null</c>.</exception>
    public FormController(FormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        InitializeState(initialInput);
        CurrentStepIndex = FirstReachableStepIndex();
    }

    /// <summary>Raised synchronously, on the calling thread, after every change of state, error, step or submission status. Components subscribe to it to re-render.</summary>
    public event Action? Changed;

    /// <summary>The schema this controller runs.</summary>
    public FormSchema Schema { get; }

    private FormMessages? _messages;
    private bool _messagesExplicit;

    /// <summary>
    /// The built-in validation messages in use: the ones set here, else <see cref="FormSchema.Messages"/>, else the ones a Klyroo.Form component gave for its locale, else English.
    /// Setting it fixes the language of this controller: components no longer replace it. Errors already computed are computed again with the new messages.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public FormMessages Messages
    {
        get => _messages ?? Schema.Messages ?? FormMessages.Default;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _messagesExplicit = true;
            ApplyMessages(value);
        }
    }

    // What a component does for its locale: only when neither the schema nor the code chose the language.
    internal void UseLocaleMessages(FormMessages messages)
    {
        if (_messagesExplicit || Schema.Messages is not null) return;
        ApplyMessages(messages);
    }

    private void ApplyMessages(FormMessages messages)
    {
        if (ReferenceEquals(Messages, messages)) return;
        _messages = messages;
        foreach (var field in ActiveFields.Where(f => _validationErrors.ContainsKey(f.Key)).ToList())
        {
            ValidateFieldCore(field);
        }
        foreach (var cell in _cellErrors.Keys.ToList())
        {
            var arrayField = ActiveFields.FirstOrDefault(f => f.Key == cell.Array);
            var child = arrayField?.Fields?.FirstOrDefault(c => c.Key == cell.Child);
            if (arrayField is not null && child is not null) ValidateCell(arrayField, child, cell.Row);
        }
        Changed?.Invoke();
    }
    /// <summary>Zero-based index of the current step. It stays 0 for a form without steps.</summary>
    public int CurrentStepIndex { get; private set; }
    /// <summary><c>true</c> while <see cref="SubmitAsync()"/> is running.</summary>
    public bool IsSubmitting { get; private set; }
    /// <summary><c>true</c> when the schema declares steps.</summary>
    public bool IsStepped => Schema.Steps.Count > 0;
    /// <summary>The current step, or <c>null</c> for a form without steps.</summary>
    public FormStepSchema? CurrentStep => IsStepped && CurrentStepIndex < Schema.Steps.Count ? Schema.Steps[CurrentStepIndex] : null;
    /// <summary><c>true</c> when every step before the current one is skipped by <see cref="FormStepSchema.SkipWhen"/>, and for a form without steps.</summary>
    public bool IsFirstStep
    {
        get
        {
            if (!IsStepped) return true;
            for (var i = 0; i < CurrentStepIndex && i < Schema.Steps.Count; i++)
            {
                if (!IsStepSkipped(i)) return false;
            }
            return true;
        }
    }
    /// <summary><c>true</c> when every step after the current one is skipped by <see cref="FormStepSchema.SkipWhen"/>, and for a form without steps. The components show the submit button instead of Next on such a step.</summary>
    public bool IsLastStep
    {
        get
        {
            if (!IsStepped) return true;
            for (var i = CurrentStepIndex + 1; i < Schema.Steps.Count; i++)
            {
                if (!IsStepSkipped(i)) return false;
            }
            return true;
        }
    }

    /// <summary>Live, read-only view of all values by key, including keys of the initial input that match no field.</summary>
    public IReadOnlyDictionary<string, object?> InternalState => _state;

    /// <summary>Keys whose current value differs from the initial snapshot, excluding <see cref="FormSchema.IgnoreDirtyPaths"/>. The snapshot is taken at construction, by <see cref="Reset"/> and after a successful submission.</summary>
    public IReadOnlyList<string> DirtyPaths
    {
        get
        {
            var ignored = Schema.IgnoreDirtyPaths ?? [];
            var allKeys = _state.Keys.Union(_initialSnapshot.Keys, StringComparer.Ordinal);
            var dirty = new List<string>();
            foreach (var key in allKeys)
            {
                if (ignored.Any(ig => key.Equals(ig, StringComparison.Ordinal) || key.StartsWith(ig + ".", StringComparison.Ordinal)))
                {
                    continue;
                }
                _state.TryGetValue(key, out var cur);
                _initialSnapshot.TryGetValue(key, out var init);
                if (!ValuesEqual(cur, init))
                {
                    dirty.Add(key);
                }
            }
            return dirty;
        }
    }

    /// <summary><c>true</c> when <see cref="DirtyPaths"/> is not empty.</summary>
    public bool IsDirty => DirtyPaths.Count > 0;

    /// <summary>Errors to show: validation errors of touched fields, validation errors of touched cells of repeatable lists keyed by <see cref="CellPath"/> (for example <c>lines[0].sku</c>), plus every error set with <see cref="SetError"/>, by key.</summary>
    public IReadOnlyDictionary<string, string> Errors
    {
        get
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (path, msg) in _validationErrors)
            {
                if (_touchedPaths.Contains(path))
                {
                    result[path] = msg;
                }
            }
            if (_cellErrors.Count > 0)
            {
                var columns = ActiveCellColumns();
                foreach (var (cell, msg) in _cellErrors)
                {
                    if (_touchedCells.Contains(cell) && IsCellInEffect(columns, cell))
                    {
                        result[cell.ToString()] = msg;
                    }
                }
            }
            foreach (var (path, (msg, _)) in _customErrors)
            {
                result[path] = msg;
            }
            return result;
        }
    }

    /// <summary><c>true</c> if a field in effect (see <see cref="ActiveFields"/>) or a visible cell of one of its repeatable lists has a validation error, touched or not, or if any error set with <see cref="SetError"/> is blocking. Validation errors only exist once validation has run.</summary>
    public bool HasBlockingErrors
    {
        get
        {
            if (_customErrors.Values.Any(e => e.Blocking)) return true;
            if (_validationErrors.Count == 0 && _cellErrors.Count == 0) return false;
            var activeKeys = ActiveFields.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
            if (_validationErrors.Keys.Any(activeKeys.Contains)) return true;
            var columns = ActiveCellColumns();
            return _cellErrors.Keys.Any(c => IsCellInEffect(columns, c));
        }
    }

    /// <summary>Every leaf field of the form: the fields of the steps, else of the sections, else of <see cref="FormSchema.Fields"/>, with groups, cards, objects and tabs flattened and ignored fields removed, whether they are visible or not. Repeatable lists are leaves; containers never are.</summary>
    public IReadOnlyList<FormFieldSchema> AllFields
    {
        get
        {
            if (IsStepped) return Schema.Steps.SelectMany(s => FlattenFields(s.Fields)).ToList();
            if (Schema.Sections.Count > 0) return Schema.Sections.SelectMany(s => FlattenFields(s.Fields)).ToList();
            return FlattenFields(Schema.Fields).ToList();
        }
    }

    /// <summary>The stateful leaf fields in effect now, which are the ones validation checks and <see cref="BuildOutput"/> includes: the fields of <see cref="AllFields"/> that are visible, inside visible containers and visible tabs, in a visible section, and in the current step or a step that is not skipped. Stateless kinds are left out.</summary>
    public IReadOnlyList<FormFieldSchema> ActiveFields
    {
        get
        {
            var result = new List<FormFieldSchema>();
            if (IsStepped)
            {
                for (var i = 0; i < Schema.Steps.Count; i++)
                {
                    if (i == CurrentStepIndex || !IsStepSkipped(i)) CollectActive(Schema.Steps[i].Fields, result);
                }
            }
            else if (Schema.Sections.Count > 0)
            {
                foreach (var section in Schema.Sections)
                {
                    if (IsSectionVisible(section)) CollectActive(section.Fields, result);
                }
            }
            else
            {
                CollectActive(Schema.Fields, result);
            }
            return result;
        }
    }

    /// <summary>Top-level fields to render now: those of the current step, of the visible sections, or of the schema, without hidden and ignored fields.</summary>
    public IReadOnlyList<FormFieldSchema> CurrentVisibleTopFields
    {
        get
        {
            var source = IsStepped ? (CurrentStep?.Fields ?? [])
                : Schema.Sections.Count > 0 ? Schema.Sections.Where(IsSectionVisible).SelectMany(s => s.Fields).ToList()
                : Schema.Fields;
            return source.Where(ShouldRenderField).ToList();
        }
    }

    /// <summary>Gets the current value of a field.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The value, or <c>null</c> when the key has no value.</returns>
    public object? GetValue(string path) =>
        _state.TryGetValue(path, out var val) ? val : null;

    /// <summary>Gets the current value converted to <typeparamref name="T"/>. A value already of that type is returned as is. Otherwise it is converted with the invariant culture, in the formats the controls store: <c>T</c> may be a <see cref="Nullable{T}"/>, a number (a whole number only becomes an integer type: 3.5 is not an <see cref="int"/>), <see cref="bool"/>, <see cref="Guid"/>, an enum (by name, ignoring case, or by the number of a member), <see cref="DateOnly"/> (<c>yyyy-MM-dd</c>), <see cref="TimeOnly"/> (<c>HH:mm</c>), <see cref="DateTime"/> (<c>yyyy-MM-ddTHH:mm</c>), <see cref="DateTimeOffset"/> (a value without a zone counts as UTC), <see cref="TimeSpan"/>, a string, or a list of strings.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The converted value, or <c>default</c> when the value is missing, blank or cannot be converted. Use <see cref="TryGetValue{T}"/> to tell those cases apart from a real <c>default</c>.</returns>
    public T? GetValue<T>(string path) =>
        FormValueConverter.TryConvert<T>(GetValue(path), out var value) ? value : default;

    /// <summary>Tries to get the current value converted to <typeparamref name="T"/>, with the conversions of <see cref="GetValue{T}"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="value">The converted value, or <c>default</c> when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> when the field has a value that converts to <typeparamref name="T"/>. <c>false</c> when the key has no value, the value is <c>null</c> or (except for a string target) blank, or it cannot be converted.</returns>
    public bool TryGetValue<T>(string path, out T? value) =>
        FormValueConverter.TryConvert(GetValue(path), out value);

    /// <summary>Gets the value a field had in the initial snapshot.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The initial value, or <c>null</c>.</returns>
    public object? GetInitialValue(string path) =>
        _initialSnapshot.TryGetValue(path, out var val) ? val : null;

    /// <summary>Sets a value, clears any error set with <see cref="SetError"/> on the key (and the errors and touched state of the cells when it replaces the rows of a repeatable list), validates the field if it has been touched and its trigger is not <see cref="FormValidationTrigger.Submit"/> (with that trigger, the validation error is cleared instead), calls <see cref="FormFieldSchema.Watch"/>, validates again the other touched fields that depend on this one (see <see cref="FormFieldSchema.DependsOn"/>) or show an error, and raises <see cref="Changed"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to mark the field touched, so its error is shown. Fields with the <see cref="FormValidationTrigger.Input"/> trigger are always marked.</param>
    public void SetValue(string path, object? value, bool markTouched = false)
    {
        _state[path] = value;
        _customErrors.Remove(path);
        RemoveCellState(path);

        var field = FindField(path);
        if (markTouched || field?.Trigger == FormValidationTrigger.Input)
        {
            _touchedPaths.Add(path);
        }

        if (field?.Trigger == FormValidationTrigger.Submit)
        {
            _validationErrors.Remove(path);
        }
        else if (_touchedPaths.Contains(path) && field is not null)
        {
            ValidateField(field);
        }

        field?.Watch?.Invoke(value, this);
        RevalidateDependents(path);
        if (field is not null && field.AsyncValidators is { Count: > 0 })
        {
            if (field.Trigger != FormValidationTrigger.Submit && _touchedPaths.Contains(path)) ScheduleAsyncValidation(field);
            else CancelPendingValidation(path);
        }
        Changed?.Invoke();
    }

    /// <summary>Marks a field touched, which makes its validation error visible, and validates it unless its trigger is <see cref="FormValidationTrigger.Submit"/>.</summary>
    /// <param name="path">Key of the field.</param>
    public void MarkTouched(string path)
    {
        _touchedPaths.Add(path);
        var field = FindField(path);
        if (field is not null && field.Trigger != FormValidationTrigger.Submit)
        {
            ValidateField(field);
            if (field.AsyncValidators is { Count: > 0 }) ScheduleAsyncValidation(field);
        }
        Changed?.Invoke();
    }

    /// <summary>Restores a field to its initial value, clears its errors and touched state (and those of its cells for a repeatable list), and calls <see cref="FormFieldSchema.Watch"/> when the value changed.</summary>
    /// <param name="path">Key of the field.</param>
    public void ResetField(string path)
    {
        _state.TryGetValue(path, out var before);
        _initialSnapshot.TryGetValue(path, out var init);
        var restored = CloneValue(init);
        _state[path] = restored;
        _validationErrors.Remove(path);
        _customErrors.Remove(path);
        _touchedPaths.Remove(path);
        RemoveCellState(path);
        CancelPendingValidation(path);
        _asyncResults.Remove(path);
        if (!ValuesEqual(before, restored))
        {
            FindField(path)?.Watch?.Invoke(restored, this);
            RevalidateDependents(path);
        }
        Changed?.Invoke();
    }

    /// <summary>Resets every field of a section with <see cref="ResetField"/>. Does nothing when the key matches no section.</summary>
    /// <param name="sectionKey">Key of the section.</param>
    public void ResetSection(string sectionKey)
    {
        var section = Schema.Sections.FirstOrDefault(s => s.Key == sectionKey);
        if (section is null) return;
        foreach (var field in FlattenFields(section.Fields))
        {
            ResetField(field.Key);
        }
        Changed?.Invoke();
    }

    /// <summary>Sets an error on a field, for example one returned by the server, and marks the field touched. The error is removed by the next <see cref="SetValue"/> on that key, by <see cref="ClearError"/> or by <see cref="Reset"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="message">The message to show.</param>
    /// <param name="blocking"><c>true</c> to make <see cref="Validate"/> and <see cref="SubmitAsync()"/> fail until the error is cleared.</param>
    public void SetError(string path, string message, bool blocking = false)
    {
        _customErrors[path] = (message, blocking);
        _touchedPaths.Add(path);
        Changed?.Invoke();
    }

    /// <summary>Clears errors set with <see cref="SetError"/> and validation errors, for one field or for all. Clearing a repeatable list also clears the errors of its cells.</summary>
    /// <param name="path">Key of the field, or <c>null</c> to clear every error.</param>
    public void ClearError(string? path = null)
    {
        if (path is null)
        {
            _customErrors.Clear();
            _validationErrors.Clear();
            _cellErrors.Clear();
        }
        else
        {
            _customErrors.Remove(path);
            _validationErrors.Remove(path);
            foreach (var cell in _cellErrors.Keys.Where(c => c.Array == path).ToList())
            {
                _cellErrors.Remove(cell);
            }
        }
        Changed?.Invoke();
    }

    /// <summary>Gets the error to show for a field, or for a cell of a repeatable list by its <see cref="CellPath"/>.</summary>
    /// <param name="path">Key of the field, or path of a cell.</param>
    /// <returns>The message, or <c>null</c> when there is none or the field has not been touched.</returns>
    public string? GetError(string path) =>
        Errors.TryGetValue(path, out var err) ? err : null;

    /// <summary>Tells whether a field is shown: it is not ignored and its <see cref="FormFieldSchema.Condition"/> is absent or true. It does not look at the containers, tab, section or step that hold the field; <see cref="ActiveFields"/> does.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is visible.</returns>
    public bool ShouldRenderField(FormFieldSchema field) =>
        !field.Ignore && (field.Condition is null || field.Condition(this));

    /// <summary>Tells whether a section is shown: its <see cref="FormPageSectionSchema.Condition"/> is absent or true.</summary>
    /// <param name="section">The section to test.</param>
    /// <returns><c>true</c> if the section is visible.</returns>
    public bool IsSectionVisible(FormPageSectionSchema section) =>
        section.Condition is null || section.Condition(this);

    /// <summary>Tells whether a field is currently required, from <see cref="FormFieldSchema.Required"/> and <see cref="FormFieldSchema.RequiredWhen"/>.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is required.</returns>
    public bool IsFieldRequired(FormFieldSchema field) =>
        field.Required || (field.RequiredWhen is not null && field.RequiredWhen(this));

    /// <summary>Tells whether a field is currently disabled, from <see cref="FormFieldSchema.Disabled"/>, <see cref="FormFieldSchema.DisabledWhen"/> and <see cref="IsSubmitting"/>: every field is disabled while a submission runs, so the user cannot edit values that are being validated or saved. The components render the controls disabled; <see cref="SetValue"/> from code still works.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is disabled.</returns>
    public bool IsFieldDisabled(FormFieldSchema field) =>
        IsSubmitting || field.Disabled || (field.DisabledWhen is not null && field.DisabledWhen(this));

    /// <summary>Gets the options of a field: those from its options factory or its static options, followed by the options added with <see cref="CreateFieldOption"/>.</summary>
    /// <param name="field">The choice field.</param>
    /// <returns>The options, possibly empty.</returns>
    public IReadOnlyList<FormOptionItem> GetFieldOptions(FormFieldSchema field)
    {
        var baseOpts = field.OptionsFactory is not null
            ? field.OptionsFactory(this)
            : (field.Options ?? []);
        if (_createdOptions.TryGetValue(field.Key, out var extra) && extra.Count > 0)
        {
            return baseOpts.Concat(extra).ToList();
        }
        return baseOpts;
    }

    /// <summary>Creates an option from user text with <see cref="FormFieldSchema.OnCreateOption"/>, adds it to the field options and selects it, appending to the value for a field with <see cref="FormFieldSchema.Multiple"/>.</summary>
    /// <param name="field">The choice field.</param>
    /// <param name="label">The text typed by the user, trimmed before use.</param>
    /// <returns>The created option, or <c>null</c> when the text is blank, the field has no <see cref="FormFieldSchema.OnCreateOption"/> or it returned <c>null</c>.</returns>
    public FormOptionItem? CreateFieldOption(FormFieldSchema field, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || field.OnCreateOption is null) return null;
        var created = field.OnCreateOption(label.Trim());
        if (created is null) return null;
        if (!_createdOptions.TryGetValue(field.Key, out var list))
        {
            list = [];
            _createdOptions[field.Key] = list;
        }
        if (list.All(o => o.Value != created.Value))
        {
            list.Add(created);
        }
        if (field.Multiple)
        {
            var cur = GetValue<IReadOnlyList<string>>(field.Key)?.ToList() ?? [];
            if (!cur.Contains(created.Value, StringComparer.Ordinal))
            {
                cur.Add(created.Value);
                SetValue(field.Key, cur, markTouched: true);
            }
        }
        else
        {
            SetValue(field.Key, created.Value, markTouched: true);
        }
        return created;
    }

    // Array item management
    /// <summary>Gets the rows of a repeatable list.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <returns>The rows, each a dictionary of cell values by child key. Empty when the key has no list.</returns>
    public IReadOnlyList<Dictionary<string, object?>> GetArrayItems(string arrayKey)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list)
        {
            return list;
        }
        return [];
    }

    /// <summary>Appends a row to a repeatable list, filling each cell from the child field's default, then revalidates the list if it was touched and calls its <see cref="FormFieldSchema.Watch"/>.</summary>
    /// <param name="arrayField">The list field.</param>
    public void AddArrayItem(FormFieldSchema arrayField)
    {
        ArgumentNullException.ThrowIfNull(arrayField);
        if (!_state.TryGetValue(arrayField.Key, out var val) || val is not List<Dictionary<string, object?>> list)
        {
            list = [];
            _state[arrayField.Key] = list;
        }
        var item = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (arrayField.Fields is not null)
        {
            foreach (var child in arrayField.Fields)
            {
                item[child.Key] = child.DefaultFactory is not null ? child.DefaultFactory(this) : child.DefaultValue;
            }
        }
        list.Add(item);
        AfterListChanged(arrayField, list);
        Changed?.Invoke();
    }

    /// <summary>Removes a row from a repeatable list, moving the errors and touched state of the following rows up, then revalidates the list if it was touched and calls its <see cref="FormFieldSchema.Watch"/>. Does nothing when the index is out of range.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    public void RemoveArrayItem(string arrayKey, int index)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
            ShiftCellState(arrayKey, index);
            if (FindField(arrayKey) is { } arrayField)
            {
                AfterListChanged(arrayField, list);
            }
            Changed?.Invoke();
        }
    }

    /// <summary>Sets one cell of a repeatable list. Does nothing when the index is out of range. The cell follows the <see cref="FormFieldSchema.Trigger"/> of its child field, like <see cref="SetValue"/> does for a field: it is validated with the child's rules once touched, unless the trigger is <see cref="FormValidationTrigger.Submit"/>, which clears its error instead. Then the child's <see cref="FormFieldSchema.Watch"/> is called with the cell value and the list's with the rows.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to mark the cell touched, so its error is shown. Children with the <see cref="FormValidationTrigger.Input"/> trigger are always marked.</param>
    public void SetArrayItemValue(string arrayKey, int index, string childKey, object? value, bool markTouched = false)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list[index][childKey] = value;
            var arrayField = FindField(arrayKey);
            var child = arrayField?.Fields?.FirstOrDefault(f => f.Key == childKey);
            if (arrayField is not null && child is not null)
            {
                var cell = new CellKey(arrayKey, index, childKey);
                if (markTouched || child.Trigger == FormValidationTrigger.Input)
                {
                    _touchedCells.Add(cell);
                }
                if (child.Trigger == FormValidationTrigger.Submit)
                {
                    _cellErrors.Remove(cell);
                }
                else if (_touchedCells.Contains(cell))
                {
                    ValidateCell(arrayField, child, index);
                }
                child.Watch?.Invoke(value, this);
                AfterListChanged(arrayField, list);
            }
            Changed?.Invoke();
        }
    }

    /// <summary>Marks a cell of a repeatable list touched, which makes its validation error visible, and validates it unless the trigger of its child field is <see cref="FormValidationTrigger.Submit"/>. <c>KlyFormEngine</c> calls it when a cell loses focus.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    public void MarkArrayItemTouched(string arrayKey, int index, string childKey)
    {
        var arrayField = FindField(arrayKey);
        var child = arrayField?.Fields?.FirstOrDefault(f => f.Key == childKey);
        if (arrayField is null || child is null || index < 0 || index >= GetArrayItems(arrayKey).Count) return;
        _touchedCells.Add(new CellKey(arrayKey, index, childKey));
        if (child.Trigger != FormValidationTrigger.Submit)
        {
            ValidateCell(arrayField, child, index);
        }
        Changed?.Invoke();
    }

    /// <summary>Gets the error to show for a cell of a repeatable list.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <returns>The message, or <c>null</c> when the cell has no error or has not been touched.</returns>
    public string? GetArrayItemError(string arrayKey, int index, string childKey)
    {
        var cell = new CellKey(arrayKey, index, childKey);
        return _touchedCells.Contains(cell) && _cellErrors.TryGetValue(cell, out var err) && IsCellInEffect(ActiveCellColumns(), cell) ? err : null;
    }

    /// <summary>Builds the path that identifies a cell of a repeatable list in <see cref="Errors"/> and <see cref="GetError"/>: <c>arrayKey[index].childKey</c>.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <returns>The path, for example <c>lines[0].sku</c>.</returns>
    public static string CellPath(string arrayKey, int index, string childKey) =>
        string.Create(CultureInfo.InvariantCulture, $"{arrayKey}[{index}].{childKey}");

    /// <summary>Validates the fields in effect (see <see cref="ActiveFields"/>) according to <see cref="FormSchema.ValidateMode"/>, including each cell of their repeatable lists, and marks them touched so their errors show. Validation errors of fields that are not in effect, such as hidden ones, are removed.</summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the fields of the current step.</param>
    /// <returns><c>true</c> if none of the validated fields and cells has an error and no blocking error set with <see cref="SetError"/> applies. A blocking error applies when it is on a validated field or cell, or when it is a form-level error (its key matches no field, see <see cref="SetFormError"/>), whichever step is validated.</returns>
    public bool Validate(bool currentStepOnly = false)
    {
        if (Schema.ValidateMode == FormValidationMode.None)
        {
            _validationErrors.Clear();
            _cellErrors.Clear();
            Changed?.Invoke();
            return !_customErrors.Values.Any(e => e.Blocking);
        }

        var active = ActiveFields;
        var activeKeys = active.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var key in _validationErrors.Keys.Where(k => !activeKeys.Contains(k)).ToList())
        {
            _validationErrors.Remove(key);
        }
        foreach (var cell in _cellErrors.Keys.Where(c => !activeKeys.Contains(c.Array)).ToList())
        {
            _cellErrors.Remove(cell);
        }

        IReadOnlyList<FormFieldSchema> fieldsToValidate = active;
        if (currentStepOnly && CurrentStep is not null)
        {
            var stepFields = new List<FormFieldSchema>();
            CollectActive(CurrentStep.Fields, stepFields);
            fieldsToValidate = stepFields;
        }

        foreach (var field in fieldsToValidate)
        {
            _touchedPaths.Add(field.Key);
            ValidateFieldCore(field);
            if (field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
            {
                ValidateArrayCells(field);
            }
        }

        Changed?.Invoke();
        var scopeKeys = fieldsToValidate.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        var hasErrors = fieldsToValidate.Any(f =>
            _validationErrors.ContainsKey(f.Key)
            || _cellErrors.Keys.Any(c => c.Array == f.Key))
            || _customErrors.Any(e => e.Value.Blocking && (IsFormLevelKey(e.Key) || scopeKeys.Contains(BaseKey(e.Key))));
        return !hasErrors;
    }

    private void ValidateField(FormFieldSchema field)
    {
        _validationErrors.Remove(field.Key);
        if (!ActiveFields.Contains(field)) return;
        ValidateFieldCore(field);
    }

    private void ValidateFieldCore(FormFieldSchema field)
    {
        _validationErrors.Remove(field.Key);
        if (CheckValue(field, GetValue(field.Key)) is { } error)
        {
            _validationErrors[field.Key] = error;
        }
    }

    private void ValidateArrayCells(FormFieldSchema arrayField)
    {
        foreach (var cell in _cellErrors.Keys.Where(c => c.Array == arrayField.Key).ToList())
        {
            _cellErrors.Remove(cell);
        }
        var rows = GetArrayItems(arrayField.Key);
        var children = CellFields(arrayField);
        for (var i = 0; i < rows.Count; i++)
        {
            foreach (var child in children)
            {
                var cell = new CellKey(arrayField.Key, i, child.Key);
                _touchedCells.Add(cell);
                if (CheckValue(child, rows[i].GetValueOrDefault(child.Key), withAsync: false) is { } error)
                {
                    _cellErrors[cell] = error;
                }
            }
        }
    }

    private void ValidateCell(FormFieldSchema arrayField, FormFieldSchema child, int index)
    {
        var cell = new CellKey(arrayField.Key, index, child.Key);
        _cellErrors.Remove(cell);
        if (!ActiveFields.Contains(arrayField) || !CellFields(arrayField).Contains(child)) return;
        var rows = GetArrayItems(arrayField.Key);
        if (index < 0 || index >= rows.Count) return;
        if (CheckValue(child, rows[index].GetValueOrDefault(child.Key), withAsync: false) is { } error)
        {
            _cellErrors[cell] = error;
        }
    }

    // After a value changes, validate again the touched fields that show an error (so stale errors clear and HasBlockingErrors is right)
    // and the touched fields that declare the changed key in DependsOn (so a new mismatch shows). The changed field itself is left alone.
    private void RevalidateDependents(string changedKey)
    {
        if (Schema.ValidateMode == FormValidationMode.None) return;
        var hasCellErrors = _cellErrors.Count > 0;
        foreach (var field in ActiveFields)
        {
            var isList = field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable;
            if (field.Key != changedKey && field.Trigger != FormValidationTrigger.Submit && _touchedPaths.Contains(field.Key)
                && (_validationErrors.ContainsKey(field.Key) || DependsOnKey(field, changedKey)))
            {
                ValidateFieldCore(field);
            }
            if (!isList) continue;
            var rows = GetArrayItems(field.Key);
            foreach (var child in CellFields(field))
            {
                if (child.Trigger == FormValidationTrigger.Submit) continue;
                var dependent = DependsOnKey(child, changedKey);
                if (!dependent && !hasCellErrors) continue;
                for (var row = 0; row < rows.Count; row++)
                {
                    var cell = new CellKey(field.Key, row, child.Key);
                    if (_touchedCells.Contains(cell) && (dependent || _cellErrors.ContainsKey(cell)))
                    {
                        ValidateCell(field, child, row);
                    }
                }
            }
        }
    }

    private static bool DependsOnKey(FormFieldSchema field, string key) =>
        field.DependsOn is { Count: > 0 } deps && deps.Contains(key, StringComparer.Ordinal);

    // The visible columns of every repeatable list in effect, by list key.
    private Dictionary<string, HashSet<string>> ActiveCellColumns() =>
        ActiveFields.Where(f => f.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
            .GroupBy(f => f.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.SelectMany(CellFields).Select(c => c.Key).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);

    private bool IsCellInEffect(Dictionary<string, HashSet<string>> columns, CellKey cell) =>
        columns.TryGetValue(cell.Array, out var children) && children.Contains(cell.Child)
        && cell.Row >= 0 && cell.Row < GetArrayItems(cell.Array).Count;

    // The children of a repeatable list that are columns now: visible, stateful leaves.
    private List<FormFieldSchema> CellFields(FormFieldSchema arrayField) =>
        (arrayField.Fields ?? []).Where(c => ShouldRenderField(c) && !IsStateless(c.Kind) && !IsContainer(c.Kind)).ToList();

    private void AfterListChanged(FormFieldSchema arrayField, List<Dictionary<string, object?>> list)
    {
        if (arrayField.Trigger == FormValidationTrigger.Submit)
        {
            _validationErrors.Remove(arrayField.Key);
        }
        else if (_touchedPaths.Contains(arrayField.Key))
        {
            ValidateField(arrayField);
        }
        arrayField.Watch?.Invoke(list, this);
        RevalidateDependents(arrayField.Key);
    }

    private void RemoveCellState(string arrayKey)
    {
        _cellErrors.Keys.Where(c => c.Array == arrayKey).ToList().ForEach(c => _cellErrors.Remove(c));
        _touchedCells.RemoveWhere(c => c.Array == arrayKey);
    }

    private void ShiftCellState(string arrayKey, int removedRow)
    {
        var errors = _cellErrors.Where(e => e.Key.Array == arrayKey).ToList();
        var touched = _touchedCells.Where(c => c.Array == arrayKey).ToList();
        RemoveCellState(arrayKey);
        foreach (var (cell, msg) in errors)
        {
            if (cell.Row != removedRow) _cellErrors[cell.Row > removedRow ? cell with { Row = cell.Row - 1 } : cell] = msg;
        }
        foreach (var cell in touched)
        {
            if (cell.Row != removedRow) _touchedCells.Add(cell.Row > removedRow ? cell with { Row = cell.Row - 1 } : cell);
        }
    }

    // The first failing check of a value against the rules of a field, or null. The result of an asynchronous rule counts once it is known for this value.
    private string? CheckValue(FormFieldSchema field, object? val, bool withAsync = true) =>
        CheckValueSync(field, val) ?? (withAsync ? CachedAsyncError(field, val) : null);

    private string? CheckValueSync(FormFieldSchema field, object? val)
    {
        if (Schema.ValidateMode == FormValidationMode.None) return null;
        var isEmpty = IsEmptyValue(val);

        if (Schema.ValidateMode is FormValidationMode.All or FormValidationMode.RequiredOnly
            && IsFieldRequired(field) && IsMissingValue(field, val))
        {
            return field.RequiredMessage ?? Messages.Required;
        }

        if (Schema.ValidateMode == FormValidationMode.RequiredOnly) return null;

        // Number bounds
        if (field.Kind == FormFieldKind.Number && !isEmpty && TryConvertDouble(val, out var num))
        {
            if (field.Min is { } min && num < min)
            {
                return Messages.MinValue(min);
            }
            if (field.Max is { } max && num > max)
            {
                return Messages.MaxValue(max);
            }
        }

        // Text length
        if (field.MaxLength is { } maxLength && val is string text && text.Length > maxLength)
        {
            return Messages.MaxLength(maxLength);
        }

        // Date range order
        if (field.Kind == FormFieldKind.DateRange && val is IReadOnlyList<string> { Count: 2 } range
            && DateOnly.TryParseExact(range[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            && DateOnly.TryParseExact(range[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            && end < start)
        {
            return Messages.DateRangeOrder;
        }

        // Password requirements
        if (field.Kind == FormFieldKind.Password && !isEmpty && field.PasswordRequirements is { Count: > 0 })
        {
            var str = val?.ToString() ?? "";
            foreach (var req in field.PasswordRequirements)
            {
                if (!req.Validate(str))
                {
                    return req.Label;
                }
            }
        }

        // Files
        if (field.Kind == FormFieldKind.File && !isEmpty && CheckFiles(field, val) is { } fileError)
        {
            return fileError;
        }

        // Custom validators
        if (field.Validators is { Count: > 0 })
        {
            foreach (var rule in field.Validators)
            {
                if (!rule.Validate(val, this))
                {
                    return rule.Message;
                }
            }
        }
        return null;
    }

    private string? CheckFiles(FormFieldSchema field, object? val)
    {
        var files = val switch
        {
            FormFileValue single => [single],
            IEnumerable<FormFileValue> many => many.ToList(),
            _ => new List<FormFileValue>(),
        };
        if (!field.Multiple && files.Count > 1) return Messages.SingleFileOnly;
        foreach (var file in files)
        {
            if (!file.Matches(field.Accept)) return Messages.FileTypeNotAllowed(file.Name, field.Accept!);
            var max = field.MaxFileSize ?? FormFileValue.DefaultMaxSize;
            if (file.Size > max) return Messages.FileTooLarge(file.Name, Messages.FileSize(max));
        }
        return null;
    }

    /// <summary>Builds the dictionary to submit: every field in effect (see <see cref="ActiveFields"/>) except those with <see cref="FormFieldSchema.OmitFromOutput"/>, with <see cref="FormFieldSchema.TransformOutput"/> applied. Fields that are hidden, in a hidden container, tab or section, or in a skipped step are left out, and their values stay in the state. Lists are copied.</summary>
    /// <returns>The values by field key.</returns>
    public IReadOnlyDictionary<string, object?> BuildOutput()
    {
        var output = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in ActiveFields)
        {
            if (field.OmitFromOutput)
            {
                continue;
            }
            var val = CloneValue(GetValue(field.Key));
            if (field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable
                && field.Fields is { Count: > 0 } columns
                && val is List<Dictionary<string, object?>> rows)
            {
                // Hidden and omitted columns leave the rows, like hidden and omitted fields leave the output.
                var dropped = columns
                    .Where(c => c.OmitFromOutput || c.Condition?.Invoke(this) == false)
                    .Select(c => c.Key)
                    .ToHashSet(StringComparer.Ordinal);
                if (dropped.Count > 0)
                {
                    foreach (var row in rows)
                    {
                        foreach (var key in dropped) row.Remove(key);
                    }
                }
            }
            if (field.TransformOutput is not null)
            {
                val = field.TransformOutput(val);
            }
            output[field.Key] = val;
        }
        return output;
    }

    /// <summary>Moves to the next step that is not skipped. It validates the current step, then runs <see cref="FormSchema.OnBeforeNext"/>, and clears the touched state and validation errors of the new step.</summary>
    /// <returns><c>true</c> if the step changed. <c>false</c> when the form has no steps, is on its last step, is submitting or already changing step, fails validation or a hook returns <c>false</c>.</returns>
    public async Task<bool> NextStepAsync()
    {
        if (!IsStepped || IsLastStep || IsSubmitting || _navigating) return false;
        _navigating = true;
        try
        {
            var valid = HasAsyncWork(currentStepOnly: true)
                ? await ValidateAsync(currentStepOnly: true)
                : Validate(currentStepOnly: true);
            if (!valid)
            {
                SubmitFailed = true;
                SubmitFailureCount++;
                Changed?.Invoke();
                return false;
            }

            if (Schema.OnBeforeNext is not null)
            {
                var ok = await Schema.OnBeforeNext(this, CurrentStepIndex);
                if (!ok) return false;
            }

            var nextIdx = CurrentStepIndex + 1;
            while (nextIdx < Schema.Steps.Count && IsStepSkipped(nextIdx))
            {
                nextIdx++;
            }
            if (nextIdx >= Schema.Steps.Count) return false;

            CurrentStepIndex = nextIdx;
            ClearValidationState();
            SubmitFailed = false;
            Changed?.Invoke();
            return true;
        }
        finally
        {
            _navigating = false;
        }
    }

    /// <summary>Moves to the previous step that is not skipped, after <see cref="FormSchema.OnBeforePrevious"/>. Values are kept and the touched state and validation errors are cleared.</summary>
    /// <returns><c>true</c> if the step changed. <c>false</c> when the form has no steps, is on its first step, is submitting or already changing step, or a hook returns <c>false</c>.</returns>
    public async Task<bool> PreviousStepAsync()
    {
        if (!IsStepped || IsFirstStep || IsSubmitting || _navigating) return false;
        _navigating = true;
        try
        {
            if (Schema.OnBeforePrevious is not null)
            {
                var ok = await Schema.OnBeforePrevious(this, CurrentStepIndex);
                if (!ok) return false;
            }

            var prevIdx = CurrentStepIndex - 1;
            while (prevIdx >= 0 && IsStepSkipped(prevIdx))
            {
                prevIdx--;
            }
            if (prevIdx < 0) return false;

            CurrentStepIndex = prevIdx;
            ClearValidationState();
            SubmitFailed = false;
            Changed?.Invoke();
            return true;
        }
        finally
        {
            _navigating = false;
        }
    }

    /// <summary>Validates every field in effect, runs <see cref="FormSchema.OnBeforeSubmit"/> then <see cref="FormSchema.OnSubmit"/>, and on success takes a new initial snapshot so the form is no longer dirty. <see cref="IsSubmitting"/> is true meanwhile. Use <see cref="SubmitAsync(Func{FormController, CancellationToken, Task{bool}}, CancellationToken)"/> to run your own save step inside the same lifecycle.</summary>
    /// <returns><c>true</c> if the form was submitted. <c>false</c> if a submission is already running, validation fails or <see cref="FormSchema.OnBeforeSubmit"/> returns <c>false</c>.</returns>
    public Task<bool> SubmitAsync() => SubmitCoreAsync(null, CancellationToken.None);

    /// <summary>
    /// Submits like <see cref="SubmitAsync()"/> and runs <paramref name="handler"/>, your save step, after <see cref="FormSchema.OnSubmit"/>, while <see cref="IsSubmitting"/> is still <c>true</c>.
    /// The dirty snapshot is retaken, and the form stops being dirty, only when every step succeeded, so a handler that returns <c>false</c> or throws leaves the user's edits
    /// dirty and the form editable again. <see cref="IsSubmitting"/> goes back to <c>false</c> in every case, and a second call while one runs returns <c>false</c> at once.
    /// </summary>
    /// <param name="handler">Receives this controller (read <see cref="BuildOutput"/> from it) and <paramref name="cancellationToken"/>, and returns <c>true</c> when the save succeeded. To report a failure, return <c>false</c> after calling <see cref="SetError"/> or <see cref="SetFormError"/>. An exception it throws propagates to the caller.</param>
    /// <param name="cancellationToken">Passed to <paramref name="handler"/> and to the asynchronous validators.</param>
    /// <returns><c>true</c> if the form was submitted and saved. <c>false</c> if a submission is already running, validation fails, <see cref="FormSchema.OnBeforeSubmit"/> or <paramref name="handler"/> returns <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <c>null</c>.</exception>
    /// <example><c>var saved = await form.SubmitAsync(async (f, ct) => (await api.SaveAsync(f.BuildOutput(), ct)).Ok);</c></example>
    public Task<bool> SubmitAsync(Func<FormController, CancellationToken, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubmitCoreAsync(handler, cancellationToken);
    }

    private async Task<bool> SubmitCoreAsync(Func<FormController, CancellationToken, Task<bool>>? handler, CancellationToken cancellationToken)
    {
        if (IsSubmitting || _navigating) return false;
        // Waiting for an asynchronous rule is part of the submission: a second submit during that wait must be refused too.
        var waits = HasAsyncWork(currentStepOnly: false);
        if (waits)
        {
            IsSubmitting = true;
            Changed?.Invoke();
        }
        var succeeded = false;
        try
        {
            var valid = waits ? await ValidateAsync(currentStepOnly: false, cancellationToken) : Validate(currentStepOnly: false);
            if (!valid) return false;

            if (!IsSubmitting)
            {
                IsSubmitting = true;
                Changed?.Invoke();
            }

            if (Schema.OnBeforeSubmit is not null)
            {
                var allowed = await Schema.OnBeforeSubmit(this);
                if (!allowed) return false;
            }

            if (Schema.OnSubmit is not null)
            {
                await Schema.OnSubmit(this);
            }

            var submitted = CloneState();
            if (handler is not null && !await handler(this, cancellationToken))
            {
                return false;
            }

            // Rebaseline dirty snapshot on success, with what the handler was given and not with what the state became since.
            RetakeSnapshot(submitted);
            succeeded = true;
            return true;
        }
        finally
        {
            IsSubmitting = false;
            SubmitFailed = !succeeded;
            if (!succeeded) SubmitFailureCount++;
            Changed?.Invoke();
        }
    }

    private Dictionary<string, object?> CloneState()
    {
        var copy = new Dictionary<string, object?>(_state.Count, StringComparer.Ordinal);
        foreach (var (k, v) in _state)
        {
            copy[k] = CloneValue(v);
        }
        return copy;
    }

    private void RetakeSnapshot(Dictionary<string, object?> values)
    {
        _initialSnapshot.Clear();
        foreach (var (k, v) in values)
        {
            _initialSnapshot[k] = v;
        }
    }

    /// <summary>Clears errors and touched state, reloads the state from <paramref name="newInput"/>, which also becomes the new dirty-tracking baseline, and returns to the first step that is not skipped. <see cref="FormFieldSchema.Watch"/> is not called.</summary>
    /// <param name="newInput">New initial values, or <c>null</c> to start from the defaults.</param>
    public void Reset(IReadOnlyDictionary<string, object?>? newInput = null)
    {
        ClearValidationState();
        _customErrors.Clear();
        SubmitFailed = false;
        SubmitFailureCount = 0;
        CancelAllPendingValidations();
        _asyncResults.Clear();
        CurrentStepIndex = 0;
        InitializeState(newInput);
        CurrentStepIndex = FirstReachableStepIndex();
        Changed?.Invoke();
    }

    /// <summary>Computes the completion state of each visible section from the current values and errors. Fields in hidden containers or tabs are not counted.</summary>
    /// <returns>One state per visible section, in schema order.</returns>
    public IReadOnlyList<FormPageSectionState> GetSectionStates()
    {
        var result = new List<FormPageSectionState>();
        var dirtySet = DirtyPaths.ToHashSet(StringComparer.Ordinal);
        var columns = ActiveCellColumns();
        foreach (var section in Schema.Sections.Where(IsSectionVisible))
        {
            var leaves = new List<FormFieldSchema>();
            CollectActive(section.Fields, leaves);
            var reqCount = 0;
            var missingCount = 0;
            var hasError = false;
            var isDirty = false;
            var hasProvided = false;

            foreach (var leaf in leaves)
            {
                var val = GetValue(leaf.Key);
                var empty = IsEmptyValue(val);
                if (!empty) hasProvided = true;
                if (IsFieldRequired(leaf))
                {
                    reqCount++;
                    if (IsMissingValue(leaf, val)) missingCount++;
                }
                if (_validationErrors.ContainsKey(leaf.Key) || _customErrors.ContainsKey(leaf.Key)
                    || _cellErrors.Keys.Any(c => c.Array == leaf.Key && IsCellInEffect(columns, c)))
                {
                    hasError = true;
                }
                if (dirtySet.Contains(leaf.Key))
                {
                    isDirty = true;
                }
            }

            var status = hasError ? FormPageSectionStatus.Invalid
                : missingCount > 0 ? FormPageSectionStatus.Pending
                : (section.Optional && !hasProvided) ? FormPageSectionStatus.Optional
                : FormPageSectionStatus.Complete;

            result.Add(new FormPageSectionState(
                section.Key,
                section.Label,
                section.Description,
                section.Optional,
                reqCount,
                missingCount,
                isDirty,
                status));
        }
        return result;
    }

    private void InitializeState(IReadOnlyDictionary<string, object?>? input)
    {
        _state.Clear();
        _initialSnapshot.Clear();

        if (input is not null)
        {
            foreach (var (k, v) in input)
            {
                _state[k] = CloneValue(v);
            }
        }

        foreach (var field in AllFields)
        {
            if (field.Ignore || IsStateless(field.Kind)) continue;
            if (_state.TryGetValue(field.Key, out var existing) && existing is not null)
            {
                if (field.TransformInput is not null)
                {
                    _state[field.Key] = field.TransformInput(existing);
                }
            }
            else
            {
                var def = field.DefaultFactory is not null ? field.DefaultFactory(this) : field.DefaultValue;
                if (def is null && field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
                {
                    def = new List<Dictionary<string, object?>>();
                }
                _state[field.Key] = CloneValue(def);
            }
        }

        foreach (var (k, v) in _state)
        {
            _initialSnapshot[k] = CloneValue(v);
        }
    }

    private void ClearValidationState()
    {
        _validationErrors.Clear();
        _touchedPaths.Clear();
        _cellErrors.Clear();
        _touchedCells.Clear();
    }

    private bool IsStepSkipped(int index) =>
        Schema.Steps[index].SkipWhen?.Invoke(this) == true;

    private int FirstReachableStepIndex()
    {
        for (var i = 0; i < Schema.Steps.Count; i++)
        {
            if (!IsStepSkipped(i)) return i;
        }
        return 0;
    }

    private FormFieldSchema? FindField(string key) =>
        AllFields.FirstOrDefault(f => f.Key == key);

    private void CollectActive(IEnumerable<FormFieldSchema> fields, List<FormFieldSchema> result)
    {
        foreach (var field in fields)
        {
            if (!ShouldRenderField(field)) continue;
            if (field.Kind == FormFieldKind.Tabs)
            {
                foreach (var tab in field.Tabs ?? [])
                {
                    if (tab.Condition is null || tab.Condition(this)) CollectActive(tab.Fields, result);
                }
            }
            else if (IsContainer(field.Kind))
            {
                CollectActive(field.Fields ?? [], result);
            }
            else if (!IsStateless(field.Kind))
            {
                result.Add(field);
            }
        }
    }

    private static IEnumerable<FormFieldSchema> FlattenFields(IEnumerable<FormFieldSchema> fields)
    {
        foreach (var field in fields)
        {
            if (field.Ignore) continue;
            if (field.Kind == FormFieldKind.Tabs)
            {
                foreach (var tab in field.Tabs ?? [])
                {
                    foreach (var child in FlattenFields(tab.Fields))
                    {
                        yield return child;
                    }
                }
            }
            else if (IsContainer(field.Kind))
            {
                foreach (var child in FlattenFields(field.Fields ?? []))
                {
                    yield return child;
                }
            }
            else
            {
                yield return field;
            }
        }
    }

    /// <summary>Tells whether a kind carries no value: <see cref="FormFieldKind.Info"/>, <see cref="FormFieldKind.Divider"/>, <see cref="FormFieldKind.Section"/> and <see cref="FormFieldKind.Button"/>.</summary>
    /// <param name="kind">The kind to test.</param>
    /// <returns><c>true</c> for a stateless kind.</returns>
    public static bool IsStateless(FormFieldKind kind) =>
        kind is FormFieldKind.Info or FormFieldKind.Divider or FormFieldKind.Section or FormFieldKind.Button;

    /// <summary>Tells whether a kind is a layout container whose children are fields of their own: <see cref="FormFieldKind.Group"/>, <see cref="FormFieldKind.Card"/>, <see cref="FormFieldKind.Object"/> and <see cref="FormFieldKind.Tabs"/>. A container never has a value, even without children.</summary>
    /// <param name="kind">The kind to test.</param>
    /// <returns><c>true</c> for a container kind.</returns>
    public static bool IsContainer(FormFieldKind kind) =>
        kind is FormFieldKind.Group or FormFieldKind.Card or FormFieldKind.Object or FormFieldKind.Tabs;

    /// <summary>Tells whether a value counts as empty for required checks: <c>null</c>, a blank string or an empty collection. <c>false</c> and <c>0</c> are not empty.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><c>true</c> if the value is empty.</returns>
    public static bool IsEmptyValue(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        System.Collections.ICollection c => c.Count == 0,
        IEnumerable<object?> e => !e.Any(),
        _ => false,
    };

    // A required checkbox or switch is a consent: it is satisfied only when checked. A required date range needs both dates.
    private static bool IsMissingValue(FormFieldSchema field, object? value) =>
        IsEmptyValue(value)
        || (field.Kind is FormFieldKind.Checkbox or FormFieldKind.Switch && value is not true)
        || (field.Kind == FormFieldKind.DateRange && (value is not IReadOnlyList<string> { Count: 2 } range || range.Any(string.IsNullOrWhiteSpace)));

    private static bool TryConvertDouble(object? val, out double d)
    {
        switch (val)
        {
            case double dbl:
                d = dbl;
                return true;
            case int i:
                d = i;
                return true;
            case long l:
                d = l;
                return true;
            case decimal m:
                d = (double)m;
                return true;
            default:
                return double.TryParse(val?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }
    }

    private static object? CloneValue(object? value)
    {
        if (value is List<Dictionary<string, object?>> dictList)
        {
            return dictList.Select(d => new Dictionary<string, object?>(d, StringComparer.Ordinal)).ToList();
        }
        if (value is IReadOnlyList<string> strList)
        {
            return strList.ToList();
        }
        if (value is IReadOnlyList<FormFileValue> fileList)
        {
            return fileList.ToList();
        }
        return value;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a is IReadOnlyList<string> la && b is IReadOnlyList<string> lb)
        {
            return la.SequenceEqual(lb, StringComparer.Ordinal);
        }
        if (a is IReadOnlyList<FormFileValue> fa && b is IReadOnlyList<FormFileValue> fb)
        {
            return fa.SequenceEqual(fb);
        }
        if (a is List<Dictionary<string, object?>> da && b is List<Dictionary<string, object?>> db)
        {
            if (da.Count != db.Count) return false;
            for (var i = 0; i < da.Count; i++)
            {
                if (da[i].Count != db[i].Count) return false;
                foreach (var (k, va) in da[i])
                {
                    if (!db[i].TryGetValue(k, out var vb) || !Equals(va, vb)) return false;
                }
            }
            return true;
        }
        return Equals(a, b);
    }
}

/// <summary>Opens a form as an overlay and returns its result as a task. Register it with <c>AddKlyrooForm()</c> (scoped) and mount one <c>KlyFormProvider</c> in the layout so the form has somewhere to render.</summary>
public sealed class FormService
{
    /// <summary>Raised when a form is opened, submitted or closed. <c>KlyFormProvider</c> subscribes to it to re-render.</summary>
    public event Action? Changed;

    /// <summary>The controller of the open form, or <c>null</c> when none is open.</summary>
    public FormController? ActiveForm { get; private set; }
    /// <summary>The shell mode of the open form. The default is <see cref="FormShellMode.Modal"/>.</summary>
    public FormShellMode ActiveMode { get; private set; } = FormShellMode.Modal;
    private TaskCompletionSource<FormSubmitOutcome>? _tcs;

    /// <summary><c>true</c> while a form is open.</summary>
    public bool IsOpen => ActiveForm is not null;

    /// <summary>Opens a form in the mounted <c>KlyFormProvider</c> and waits for its result. Opening a form while another is open replaces it, and the replaced form's task completes with <see cref="FormSubmitOutcome.IsCompleted"/> set to <c>false</c> and that form's output, as if it had been closed.</summary>
    /// <param name="schema">The form to show.</param>
    /// <param name="input">Initial values by field key.</param>
    /// <param name="mode">How to present the form.</param>
    /// <returns>A task that completes with <see cref="FormSubmitOutcome.IsCompleted"/> set to <c>true</c> after a successful submit, or <c>false</c> after <see cref="CloseForm"/> or when another form replaces it.</returns>
    public Task<FormSubmitOutcome> CreateFormAsync(
        FormSchema schema,
        IReadOnlyDictionary<string, object?>? input = null,
        FormShellMode mode = FormShellMode.Modal)
    {
        var replaced = ActiveForm;
        var replacedTcs = _tcs;
        ActiveForm = new FormController(schema, input);
        ActiveMode = mode;
        _tcs = new TaskCompletionSource<FormSubmitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        Changed?.Invoke();
        if (replaced is not null)
        {
            replacedTcs?.TrySetResult(new FormSubmitOutcome(false, replaced.BuildOutput()));
            replaced.Dispose();
        }
        return _tcs.Task;
    }

    /// <summary>Submits the open form with <see cref="FormController.SubmitAsync()"/>. On success it closes the form and completes the task of <see cref="CreateFormAsync"/>.</summary>
    /// <returns><c>true</c> if the form was submitted; <c>false</c> if no form is open or submission failed.</returns>
    public async Task<bool> SubmitActiveFormAsync()
    {
        if (ActiveForm is not { } form) return false;
        var ok = await form.SubmitAsync();
        if (ok) CompleteSubmittedForm(form);
        return ok;
    }

    // Called by KlyFormProvider once KlyFormEngine has already submitted the controller, so the schema hooks run once.
    internal void CompleteSubmittedForm(FormController form)
    {
        if (!ReferenceEquals(form, ActiveForm)) return;
        var data = form.BuildOutput();
        var tcs = _tcs;
        ActiveForm = null;
        _tcs = null;
        Changed?.Invoke();
        tcs?.TrySetResult(new FormSubmitOutcome(true, data));
        form.Dispose();
    }

    /// <summary>Closes the open form without submitting and completes the task of <see cref="CreateFormAsync"/> with <see cref="FormSubmitOutcome.IsCompleted"/> set to <c>false</c>. Does nothing when no form is open.</summary>
    public void CloseForm()
    {
        if (ActiveForm is null) return;
        var closing = ActiveForm;
        var data = closing.BuildOutput();
        var tcs = _tcs;
        ActiveForm = null;
        _tcs = null;
        Changed?.Invoke();
        tcs?.TrySetResult(new FormSubmitOutcome(false, data));
        closing.Dispose();
    }
}
