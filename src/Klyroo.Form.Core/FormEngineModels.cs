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
    /// <summary>Does not validate while editing. The field is validated by <see cref="FormController.Validate"/> and by submission.</summary>
    Submit,
}

/// <summary>Layout hint for the shell that hosts a form, used by <see cref="FormService"/> overlays and the <c>Shell</c> parameter of <c>KlyFormEngine</c>.</summary>
public enum FormShellMode
{
    /// <summary>Renders in the page flow. This is the default of <c>KlyFormEngine</c>.</summary>
    Inline,
    /// <summary>Renders in a centered dialog. This is the default of <see cref="FormService.CreateFormAsync"/>.</summary>
    Modal,
    /// <summary>Drawer layout. <c>KlyFormProvider</c> currently renders it in the same centered dialog as <see cref="Modal"/> and only sets a <c>data-mode</c> attribute.</summary>
    Drawer,
    /// <summary>Fullscreen layout. <c>KlyFormProvider</c> currently renders it in the same centered dialog as <see cref="Modal"/> and only sets a <c>data-mode</c> attribute.</summary>
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

/// <summary>Kind of a field in a <see cref="FormFieldSchema"/>. The kind decides how the field is stored, validated and rendered; kinds without a dedicated control in <c>KlyFormEngine</c> are rendered as a text input.</summary>
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
    /// <summary>Rendered by <c>KlyFormEngine</c> exactly like <see cref="Checkbox"/>. The value is a <see cref="bool"/>.</summary>
    Switch,
    /// <summary>Radio group built from the field options. The value is the selected option value.</summary>
    Radio,
    /// <summary>Multiple choice from a list of options. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    CheckboxGroup,
    /// <summary>Native select list built from the field options. The value is the selected option value.</summary>
    Select,
    /// <summary>Rendered by <c>KlyFormEngine</c> exactly like <see cref="Select"/>, as a native select list.</summary>
    AutoComplete,
    /// <summary>Date input (<c>type="date"</c>). The value is the string produced by the browser, in <c>yyyy-MM-dd</c> form.</summary>
    Date,
    /// <summary>Date and time value. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    DateTime,
    /// <summary>Range of two dates. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    DateRange,
    /// <summary>Time input (<c>type="time"</c>). The value is the string produced by the browser.</summary>
    Time,
    /// <summary>Phone number. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    PhoneNumber,
    /// <summary>Range slider (<c>type="range"</c>) bounded by <see cref="FormFieldSchema.Min"/>, <see cref="FormFieldSchema.Max"/> and <see cref="FormFieldSchema.Step"/>. The value is a <see cref="double"/>. Bounds are only enforced on validation for <see cref="Number"/> fields.</summary>
    Slider,
    /// <summary>Rating value. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    Rating,
    /// <summary>Color value. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    ColorPicker,
    /// <summary>One-time code. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    OneTimeCode,
    /// <summary>List of free-form tags. <c>KlyFormEngine</c> has no dedicated control for it and renders a text input.</summary>
    Tag,
    /// <summary>Hidden input. The value is stored and included in the output but nothing is shown to the user.</summary>
    Hidden,
    /// <summary>Read-only text block showing <see cref="FormFieldSchema.Label"/> and <see cref="FormFieldSchema.DisplayText"/> (or <see cref="FormFieldSchema.Description"/>). It is stateless: it has no value, is never validated and never appears in the output.</summary>
    Info,
    /// <summary>Horizontal rule. Stateless, like <see cref="Info"/>.</summary>
    Divider,
    /// <summary>Heading made of <see cref="FormFieldSchema.Label"/> and <see cref="FormFieldSchema.Description"/>. Stateless, like <see cref="Info"/>.</summary>
    Section,
    /// <summary>Stateless placeholder: it has no value, is never validated and never appears in the output. <c>KlyFormEngine</c> has no dedicated rendering for it, so do not use it to add a button.</summary>
    Button,
    /// <summary>Container that lays out the fields in <see cref="FormFieldSchema.Fields"/> without a border. The child fields keep their own keys and are validated and output as top-level fields.</summary>
    Group,
    /// <summary>Bordered container for the fields in <see cref="FormFieldSchema.Fields"/>. Children behave as in <see cref="Group"/>.</summary>
    Card,
    /// <summary>Tabbed container driven by <see cref="FormFieldSchema.Tabs"/>. The fields of every tab are validated and output, whether or not the tab is selected.</summary>
    Tabs,
    /// <summary>Container with the same behavior as <see cref="Group"/>. Child values are not nested under the object key.</summary>
    Object,
    /// <summary>Repeatable list of rows, each row holding the fields declared in <see cref="FormFieldSchema.Fields"/>. The value is a <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>. Rows are edited with <c>AddArrayItem</c>, <c>SetArrayItemValue</c> and <c>RemoveArrayItem</c>, and <c>KlyFormEngine</c> renders each cell as a text input.</summary>
    ArrayList,
    /// <summary>Same value and behavior as <see cref="ArrayList"/>; <c>KlyFormEngine</c> renders both identically.</summary>
    ArrayTable,
}

/// <summary>One choice of a <see cref="FormFieldKind.Select"/>, <see cref="FormFieldKind.AutoComplete"/> or <see cref="FormFieldKind.Radio"/> field.</summary>
/// <param name="Value">The value stored in the form when the option is selected.</param>
/// <param name="Label">The text shown to the user.</param>
/// <param name="Description">Optional secondary text. Not rendered by the built-in components.</param>
/// <param name="Group">Optional group name. Not rendered by the built-in components.</param>
/// <param name="Icon">Optional icon name. Not rendered by the built-in components.</param>
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

/// <summary>One tab of a <see cref="FormFieldKind.Tabs"/> field.</summary>
public sealed class FormTabItemSchema
{
    /// <summary>Unique key of the tab among its siblings.</summary>
    public required string Key { get; init; }
    /// <summary>Text shown on the tab.</summary>
    public required string Label { get; init; }
    /// <summary>Fields shown when the tab is selected.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    /// <summary>Returns <c>false</c> to hide the tab. A hidden tab's fields are still validated and output.</summary>
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

    /// <summary><c>true</c> to render the control disabled. This only affects rendering: a disabled field is still validated and included in the output.</summary>
    public bool Disabled { get; init; }
    /// <summary>Disables the field while it returns <c>true</c>. Same scope as <see cref="Disabled"/>.</summary>
    public Func<FormController, bool>? DisabledWhen { get; init; }
    /// <summary>Text shown under a disabled control to explain why it is disabled.</summary>
    public string? DisabledReason { get; init; }

    /// <summary>Returns <c>false</c> to hide the field. A hidden field is skipped by validation and left out of <see cref="FormController.BuildOutput"/>, but its value stays in the state. A container's condition hides the container only: put the condition on the children as well to exclude them from validation and output.</summary>
    public Func<FormController, bool>? Condition { get; init; }
    /// <summary><c>true</c> to drop the field entirely: it has no state, is not rendered, validated or output, and its children are dropped with it.</summary>
    public bool Ignore { get; init; }
    /// <summary><c>true</c> to validate and store the field but leave it out of <see cref="FormController.BuildOutput"/>.</summary>
    public bool OmitFromOutput { get; init; }
    /// <summary><c>true</c> to show a per-field reset button in <c>KlyFormEngine</c> once the value differs from its initial value.</summary>
    public bool DirtyCheck { get; init; }
    /// <summary>Reserved. The built-in components do not use it.</summary>
    public bool Collapsible { get; init; }
    /// <summary>Reserved. The built-in components do not use it.</summary>
    public bool DefaultCollapsed { get; init; }
    /// <summary>Number of columns, from 1 to 12, the field occupies in the 12-column grid. Values outside the range are clamped. The default is 12.</summary>
    public int Span { get; init; } = 12;

    // Bounds & rules
    /// <summary>Smallest accepted value of a <see cref="FormFieldKind.Number"/> field, enforced on validation. Also the <c>min</c> attribute of number and slider inputs.</summary>
    public double? Min { get; init; }
    /// <summary>Largest accepted value of a <see cref="FormFieldKind.Number"/> field, enforced on validation. Also the <c>max</c> attribute of number and slider inputs.</summary>
    public double? Max { get; init; }
    /// <summary>The <c>step</c> attribute of number and slider inputs. It is not validated.</summary>
    public double? Step { get; init; }
    /// <summary>Largest accepted number of characters of a text value, enforced on validation. Also the <c>maxlength</c> attribute of text-like inputs.</summary>
    public int? MaxLength { get; init; }
    /// <summary><c>true</c> if the field holds several values. Only <see cref="FormController.CreateFieldOption"/> reads it: the built-in components render no multi-select control.</summary>
    public bool Multiple { get; init; }
    /// <summary><c>true</c> to show a clear button next to select lists and text-like inputs once they have a value.</summary>
    public bool Clearable { get; init; }
    /// <summary>Static options of a choice field. Ignored when <see cref="OptionsFactory"/> is set.</summary>
    public IReadOnlyList<FormOptionItem>? Options { get; init; }
    /// <summary>Computes the options from the controller, for example to depend on another field. It takes precedence over <see cref="Options"/>.</summary>
    public Func<FormController, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; init; }
    /// <summary>Turns text typed by the user into a new option. Only <see cref="FormController.CreateFieldOption"/> calls it; the built-in components render no control that does.</summary>
    public Func<string, FormOptionItem?>? OnCreateOption { get; init; }
    /// <summary>Rules a <see cref="FormFieldKind.Password"/> value must satisfy. They are checked on validation, in order; the built-in components do not render them as a checklist.</summary>
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; init; }
    /// <summary>Custom validation rules, run in order after the built-in checks. The first failing rule sets the error.</summary>
    public IReadOnlyList<FormValidatorRule>? Validators { get; init; }

    // Transforms & watchers
    /// <summary>Converts a value from the initial input into the value held in the state. It does not apply to default values or to values set later.</summary>
    public Func<object?, object?>? TransformInput { get; init; }
    /// <summary>Converts the value in <see cref="FormController.BuildOutput"/>, for example to trim a string. The state keeps the untransformed value.</summary>
    public Func<object?, object?>? TransformOutput { get; init; }
    /// <summary>Called with the new value and the controller after <see cref="FormController.SetValue"/> changes this field. It does not run for repeatable-list edits or resets.</summary>
    public Action<object?, FormController>? Watch { get; init; }

    // Display / Info block
    /// <summary>Reserved. The built-in components do not use it.</summary>
    public string? DisplayKind { get; init; }
    /// <summary>Text of a <see cref="FormFieldKind.Info"/> block. Falls back to <see cref="Description"/>.</summary>
    public string? DisplayText { get; init; }
    /// <summary>Reserved. The built-in components do not use it.</summary>
    public string? DisplayTone { get; init; }

    // Container / Array children
    /// <summary>Child fields: the content of a container, or the columns of each row of a repeatable list.</summary>
    public IReadOnlyList<FormFieldSchema>? Fields { get; init; }
    /// <summary>Tabs of a <see cref="FormFieldKind.Tabs"/> field.</summary>
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
    /// <summary>Optional description. Not rendered by the built-in components.</summary>
    public string? Description { get; init; }
    /// <summary>Reserved. Neither the controller nor the built-in components use it.</summary>
    public string? Root { get; init; }
    /// <summary>Fields of the step.</summary>
    public IReadOnlyList<FormFieldSchema> Fields { get; init; } = [];
    /// <summary>Returns <c>true</c> to skip the step: <see cref="FormController.NextStepAsync"/> and <see cref="FormController.PreviousStepAsync"/> pass over it, and the stepper does not list it. Its fields are still validated and output on submit.</summary>
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
    /// <summary>Reserved. <c>KlyFormPage</c> does not use it.</summary>
    public int Span { get; init; } = 12;
    /// <summary>Returns <c>false</c> to hide the section and leave it out of <see cref="FormController.GetSectionStates"/>. Its fields are still validated and output.</summary>
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
    /// <summary>Reserved. The built-in components always lay fields out on a 12-column grid.</summary>
    public int Columns { get; init; } = 12;
    /// <summary><c>true</c> to show the list of steps above a stepped form. The default is <c>true</c>.</summary>
    public bool ShowStepper { get; init; } = true;
    /// <summary>Which checks validation runs. The default is <see cref="FormValidationMode.All"/>.</summary>
    public FormValidationMode ValidateMode { get; init; } = FormValidationMode.All;
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
    /// <summary>Runs when the form is submitted and validation (and <see cref="OnBeforeSubmit"/>) succeeded. An exception it throws propagates to the caller of <see cref="FormController.SubmitAsync"/>.</summary>
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
public sealed class FormController
{
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _initialSnapshot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _validationErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Message, bool Blocking)> _customErrors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _touchedPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<FormOptionItem>> _createdOptions = new(StringComparer.Ordinal);

    /// <summary>Creates a controller for <paramref name="schema"/> and initializes the state. Values in <paramref name="initialInput"/> win over default values, and the result becomes the baseline for dirty tracking.</summary>
    /// <param name="schema">The schema to run.</param>
    /// <param name="initialInput">Initial values by field key. Keys that match no field are kept in <see cref="InternalState"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <c>null</c>.</exception>
    public FormController(FormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        InitializeState(initialInput);
    }

    /// <summary>Raised synchronously, on the calling thread, after every change of state, error, step or submission status. Components subscribe to it to re-render.</summary>
    public event Action? Changed;

    /// <summary>The schema this controller runs.</summary>
    public FormSchema Schema { get; }
    /// <summary>Zero-based index of the current step. It stays 0 for a form without steps.</summary>
    public int CurrentStepIndex { get; private set; }
    /// <summary><c>true</c> while <see cref="SubmitAsync"/> is running.</summary>
    public bool IsSubmitting { get; private set; }
    /// <summary><c>true</c> when the schema declares steps.</summary>
    public bool IsStepped => Schema.Steps.Count > 0;
    /// <summary>The current step, or <c>null</c> for a form without steps.</summary>
    public FormStepSchema? CurrentStep => IsStepped && CurrentStepIndex < Schema.Steps.Count ? Schema.Steps[CurrentStepIndex] : null;
    /// <summary><c>true</c> on the first step, and for a form without steps.</summary>
    public bool IsFirstStep => CurrentStepIndex <= 0;
    /// <summary><c>true</c> on the last declared step, and for a form without steps. A later step that <see cref="FormStepSchema.SkipWhen"/> skips does not change this.</summary>
    public bool IsLastStep => !IsStepped || CurrentStepIndex >= Schema.Steps.Count - 1;

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

    /// <summary>Errors to show: validation errors of touched fields plus every error set with <see cref="SetError"/>, by key.</summary>
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
            foreach (var (path, (msg, _)) in _customErrors)
            {
                result[path] = msg;
            }
            return result;
        }
    }

    /// <summary><c>true</c> if any validation error exists, touched or not, or any error set with <see cref="SetError"/> as blocking. Validation errors only exist once validation has run.</summary>
    public bool HasBlockingErrors =>
        _validationErrors.Count > 0 || _customErrors.Values.Any(e => e.Blocking);

    /// <summary>Every leaf field of the form: the fields of the steps, else of the sections, else of <see cref="FormSchema.Fields"/>, with groups, cards, objects and tabs flattened and ignored fields removed. Repeatable lists are leaves.</summary>
    public IReadOnlyList<FormFieldSchema> AllFields
    {
        get
        {
            if (IsStepped) return Schema.Steps.SelectMany(s => FlattenFields(s.Fields)).ToList();
            if (Schema.Sections.Count > 0) return Schema.Sections.SelectMany(s => FlattenFields(s.Fields)).ToList();
            return FlattenFields(Schema.Fields).ToList();
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

    /// <summary>Gets the current value converted to <typeparamref name="T"/>. A value already of that type is returned as is; otherwise it is converted with the invariant culture.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The converted value, or <c>default</c> when the value is missing or cannot be converted.</returns>
    public T? GetValue<T>(string path)
    {
        var raw = GetValue(path);
        if (raw is T typed) return typed;
        if (raw is null) return default;
        try
        {
            return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>Gets the value a field had in the initial snapshot.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The initial value, or <c>null</c>.</returns>
    public object? GetInitialValue(string path) =>
        _initialSnapshot.TryGetValue(path, out var val) ? val : null;

    /// <summary>Sets a value, clears any error set with <see cref="SetError"/> on the key, validates the field if it has been touched and its trigger is not <see cref="FormValidationTrigger.Submit"/>, calls <see cref="FormFieldSchema.Watch"/> and raises <see cref="Changed"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to mark the field touched, so its error is shown. Fields with the <see cref="FormValidationTrigger.Input"/> trigger are always marked.</param>
    public void SetValue(string path, object? value, bool markTouched = false)
    {
        _state[path] = value;
        _customErrors.Remove(path);

        var field = FindField(path);
        if (markTouched || field?.Trigger == FormValidationTrigger.Input)
        {
            _touchedPaths.Add(path);
        }

        if (_touchedPaths.Contains(path) && field is not null && field.Trigger != FormValidationTrigger.Submit)
        {
            ValidateField(field);
        }

        field?.Watch?.Invoke(value, this);
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
        }
        Changed?.Invoke();
    }

    /// <summary>Restores a field to its initial value and clears its errors and touched state.</summary>
    /// <param name="path">Key of the field.</param>
    public void ResetField(string path)
    {
        _initialSnapshot.TryGetValue(path, out var init);
        _state[path] = CloneValue(init);
        _validationErrors.Remove(path);
        _customErrors.Remove(path);
        _touchedPaths.Remove(path);
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
    /// <param name="blocking"><c>true</c> to make <see cref="Validate"/> and <see cref="SubmitAsync"/> fail until the error is cleared.</param>
    public void SetError(string path, string message, bool blocking = false)
    {
        _customErrors[path] = (message, blocking);
        _touchedPaths.Add(path);
        Changed?.Invoke();
    }

    /// <summary>Clears errors set with <see cref="SetError"/> and validation errors, for one field or for all.</summary>
    /// <param name="path">Key of the field, or <c>null</c> to clear every error.</param>
    public void ClearError(string? path = null)
    {
        if (path is null)
        {
            _customErrors.Clear();
            _validationErrors.Clear();
        }
        else
        {
            _customErrors.Remove(path);
            _validationErrors.Remove(path);
        }
        Changed?.Invoke();
    }

    /// <summary>Gets the error to show for a field.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The message, or <c>null</c> when there is none or the field has not been touched.</returns>
    public string? GetError(string path) =>
        Errors.TryGetValue(path, out var err) ? err : null;

    /// <summary>Tells whether a field is shown: it is not ignored and its <see cref="FormFieldSchema.Condition"/> is absent or true.</summary>
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

    /// <summary>Tells whether a field is currently disabled, from <see cref="FormFieldSchema.Disabled"/> and <see cref="FormFieldSchema.DisabledWhen"/>.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is disabled.</returns>
    public bool IsFieldDisabled(FormFieldSchema field) =>
        field.Disabled || (field.DisabledWhen is not null && field.DisabledWhen(this));

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

    /// <summary>Appends a row to a repeatable list, filling each cell from the child field's default.</summary>
    /// <param name="arrayField">The list field.</param>
    public void AddArrayItem(FormFieldSchema arrayField)
    {
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
        Changed?.Invoke();
    }

    /// <summary>Removes a row from a repeatable list. Does nothing when the index is out of range.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    public void RemoveArrayItem(string arrayKey, int index)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
            Changed?.Invoke();
        }
    }

    /// <summary>Sets one cell of a repeatable list. Does nothing when the index is out of range. Cells are not validated and do not call <see cref="FormFieldSchema.Watch"/>.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <param name="value">The new value.</param>
    public void SetArrayItemValue(string arrayKey, int index, string childKey, object? value)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list[index][childKey] = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Validates the fields according to <see cref="FormSchema.ValidateMode"/> and marks them touched so their errors show. Hidden and stateless fields are skipped, and cells of repeatable lists are not checked.</summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the fields of the current step.</param>
    /// <returns><c>true</c> if none of the validated fields has an error.</returns>
    public bool Validate(bool currentStepOnly = false)
    {
        if (Schema.ValidateMode == FormValidationMode.None)
        {
            _validationErrors.Clear();
            Changed?.Invoke();
            return !_customErrors.Values.Any(e => e.Blocking);
        }

        var fieldsToValidate = currentStepOnly && CurrentStep is not null
            ? FlattenFields(CurrentStep.Fields).ToList()
            : AllFields;

        foreach (var field in fieldsToValidate)
        {
            _touchedPaths.Add(field.Key);
            ValidateField(field);
        }

        Changed?.Invoke();
        var hasStepErrors = fieldsToValidate.Any(f =>
            _validationErrors.ContainsKey(f.Key)
            || (_customErrors.TryGetValue(f.Key, out var ce) && ce.Blocking));
        return !hasStepErrors;
    }

    private void ValidateField(FormFieldSchema field)
    {
        _validationErrors.Remove(field.Key);
        if (!ShouldRenderField(field) || IsStateless(field.Kind)) return;
        if (Schema.ValidateMode == FormValidationMode.None) return;

        var val = GetValue(field.Key);
        var isEmpty = IsEmptyValue(val);

        if (Schema.ValidateMode is FormValidationMode.All or FormValidationMode.RequiredOnly)
        {
            if (IsFieldRequired(field) && IsMissingValue(field, val))
            {
                _validationErrors[field.Key] = field.RequiredMessage ?? "This field is required";
                return;
            }
        }

        if (Schema.ValidateMode == FormValidationMode.RequiredOnly) return;

        // Number bounds
        if (field.Kind == FormFieldKind.Number && !isEmpty && TryConvertDouble(val, out var num))
        {
            if (field.Min is { } min && num < min)
            {
                _validationErrors[field.Key] = $"Value must be at least {min.ToString(CultureInfo.InvariantCulture)}";
                return;
            }
            if (field.Max is { } max && num > max)
            {
                _validationErrors[field.Key] = $"Value must be at most {max.ToString(CultureInfo.InvariantCulture)}";
                return;
            }
        }

        // Text length
        if (field.MaxLength is { } maxLength && val is string text && text.Length > maxLength)
        {
            _validationErrors[field.Key] = $"Must be at most {maxLength.ToString(CultureInfo.InvariantCulture)} characters";
            return;
        }

        // Password requirements
        if (field.Kind == FormFieldKind.Password && !isEmpty && field.PasswordRequirements is { Count: > 0 })
        {
            var str = val?.ToString() ?? "";
            foreach (var req in field.PasswordRequirements)
            {
                if (!req.Validate(str))
                {
                    _validationErrors[field.Key] = req.Label;
                    return;
                }
            }
        }

        // Custom validators
        if (field.Validators is { Count: > 0 })
        {
            foreach (var rule in field.Validators)
            {
                if (!rule.Validate(val, this))
                {
                    _validationErrors[field.Key] = rule.Message;
                    return;
                }
            }
        }
    }

    /// <summary>Builds the dictionary to submit: every visible, stateful field except those with <see cref="FormFieldSchema.OmitFromOutput"/>, with <see cref="FormFieldSchema.TransformOutput"/> applied. Lists are copied.</summary>
    /// <returns>The values by field key.</returns>
    public IReadOnlyDictionary<string, object?> BuildOutput()
    {
        var output = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in AllFields)
        {
            if (!ShouldRenderField(field) || IsStateless(field.Kind) || field.OmitFromOutput)
            {
                continue;
            }
            var val = GetValue(field.Key);
            if (field.TransformOutput is not null)
            {
                val = field.TransformOutput(val);
            }
            output[field.Key] = CloneValue(val);
        }
        return output;
    }

    /// <summary>Moves to the next step that is not skipped. It validates the current step, then runs <see cref="FormSchema.OnBeforeNext"/>, and clears the touched state and validation errors of the new step.</summary>
    /// <returns><c>true</c> if the step changed. <c>false</c> when the form has no steps, is on its last step, is submitting, fails validation or a hook returns <c>false</c>.</returns>
    public async Task<bool> NextStepAsync()
    {
        if (!IsStepped || IsLastStep || IsSubmitting) return false;
        if (!Validate(currentStepOnly: true)) return false;

        if (Schema.OnBeforeNext is not null)
        {
            var ok = await Schema.OnBeforeNext(this, CurrentStepIndex);
            if (!ok) return false;
        }

        var nextIdx = CurrentStepIndex + 1;
        while (nextIdx < Schema.Steps.Count && Schema.Steps[nextIdx].SkipWhen?.Invoke(this) == true)
        {
            nextIdx++;
        }
        if (nextIdx >= Schema.Steps.Count) return false;

        CurrentStepIndex = nextIdx;
        _validationErrors.Clear();
        _touchedPaths.Clear();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Moves to the previous step that is not skipped, after <see cref="FormSchema.OnBeforePrevious"/>. Values are kept and the touched state and validation errors are cleared.</summary>
    /// <returns><c>true</c> if the step changed.</returns>
    public async Task<bool> PreviousStepAsync()
    {
        if (!IsStepped || IsFirstStep || IsSubmitting) return false;
        if (Schema.OnBeforePrevious is not null)
        {
            var ok = await Schema.OnBeforePrevious(this, CurrentStepIndex);
            if (!ok) return false;
        }

        var prevIdx = CurrentStepIndex - 1;
        while (prevIdx >= 0 && Schema.Steps[prevIdx].SkipWhen?.Invoke(this) == true)
        {
            prevIdx--;
        }
        if (prevIdx < 0) return false;

        CurrentStepIndex = prevIdx;
        _validationErrors.Clear();
        _touchedPaths.Clear();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Validates every field, runs <see cref="FormSchema.OnBeforeSubmit"/> then <see cref="FormSchema.OnSubmit"/>, and on success takes a new initial snapshot so the form is no longer dirty. <see cref="IsSubmitting"/> is true meanwhile.</summary>
    /// <returns><c>true</c> if the form was submitted. <c>false</c> if a submission is already running, validation fails or <see cref="FormSchema.OnBeforeSubmit"/> returns <c>false</c>.</returns>
    public async Task<bool> SubmitAsync()
    {
        if (IsSubmitting) return false;
        if (!Validate(currentStepOnly: false)) return false;

        IsSubmitting = true;
        Changed?.Invoke();
        try
        {
            if (Schema.OnBeforeSubmit is not null)
            {
                var allowed = await Schema.OnBeforeSubmit(this);
                if (!allowed) return false;
            }

            if (Schema.OnSubmit is not null)
            {
                await Schema.OnSubmit(this);
            }

            // Rebaseline dirty snapshot on success
            _initialSnapshot.Clear();
            foreach (var (k, v) in _state)
            {
                _initialSnapshot[k] = CloneValue(v);
            }
            return true;
        }
        finally
        {
            IsSubmitting = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Clears errors and touched state, returns to the first step and reloads the state from <paramref name="newInput"/>, which also becomes the new dirty-tracking baseline.</summary>
    /// <param name="newInput">New initial values, or <c>null</c> to start from the defaults.</param>
    public void Reset(IReadOnlyDictionary<string, object?>? newInput = null)
    {
        _validationErrors.Clear();
        _customErrors.Clear();
        _touchedPaths.Clear();
        CurrentStepIndex = 0;
        InitializeState(newInput);
        Changed?.Invoke();
    }

    /// <summary>Computes the completion state of each visible section from the current values and errors.</summary>
    /// <returns>One state per visible section, in schema order.</returns>
    public IReadOnlyList<FormPageSectionState> GetSectionStates()
    {
        var result = new List<FormPageSectionState>();
        var dirtySet = DirtyPaths.ToHashSet(StringComparer.Ordinal);
        foreach (var section in Schema.Sections.Where(IsSectionVisible))
        {
            var leaves = FlattenFields(section.Fields).Where(f => ShouldRenderField(f) && !IsStateless(f.Kind)).ToList();
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
                if (_validationErrors.ContainsKey(leaf.Key) || _customErrors.ContainsKey(leaf.Key))
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

    private FormFieldSchema? FindField(string key) =>
        AllFields.FirstOrDefault(f => f.Key == key);

    private IEnumerable<FormFieldSchema> FlattenFields(IEnumerable<FormFieldSchema> fields)
    {
        foreach (var field in fields)
        {
            if (field.Ignore) continue;
            if (field.Kind is FormFieldKind.Group or FormFieldKind.Card or FormFieldKind.Object && field.Fields is not null)
            {
                foreach (var child in FlattenFields(field.Fields))
                {
                    yield return child;
                }
            }
            else if (field.Kind == FormFieldKind.Tabs && field.Tabs is not null)
            {
                foreach (var tab in field.Tabs)
                {
                    foreach (var child in FlattenFields(tab.Fields))
                    {
                        yield return child;
                    }
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

    // A required checkbox or switch is a consent: it is satisfied only when checked.
    private static bool IsMissingValue(FormFieldSchema field, object? value) =>
        IsEmptyValue(value) || (field.Kind is FormFieldKind.Checkbox or FormFieldKind.Switch && value is not true);

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

    /// <summary>Opens a form in the mounted <c>KlyFormProvider</c> and waits for its result. Opening a form while another is open replaces it, and the replaced form's task never completes.</summary>
    /// <param name="schema">The form to show.</param>
    /// <param name="input">Initial values by field key.</param>
    /// <param name="mode">How to present the form.</param>
    /// <returns>A task that completes with <see cref="FormSubmitOutcome.IsCompleted"/> set to <c>true</c> after a successful submit, or <c>false</c> after <see cref="CloseForm"/>.</returns>
    public Task<FormSubmitOutcome> CreateFormAsync(
        FormSchema schema,
        IReadOnlyDictionary<string, object?>? input = null,
        FormShellMode mode = FormShellMode.Modal)
    {
        ActiveForm = new FormController(schema, input);
        ActiveMode = mode;
        _tcs = new TaskCompletionSource<FormSubmitOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        Changed?.Invoke();
        return _tcs.Task;
    }

    /// <summary>Submits the open form with <see cref="FormController.SubmitAsync"/>. On success it closes the form and completes the task of <see cref="CreateFormAsync"/>.</summary>
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
    }

    /// <summary>Closes the open form without submitting and completes the task of <see cref="CreateFormAsync"/> with <see cref="FormSubmitOutcome.IsCompleted"/> set to <c>false</c>. Does nothing when no form is open.</summary>
    public void CloseForm()
    {
        if (ActiveForm is null) return;
        var data = ActiveForm.BuildOutput();
        var tcs = _tcs;
        ActiveForm = null;
        _tcs = null;
        Changed?.Invoke();
        tcs?.TrySetResult(new FormSubmitOutcome(false, data));
    }
}
