namespace Klyroo.Form.Dynamic;

/// <summary>A custom validation rule of a field. Rules run in order after the built-in checks and the first failing rule sets the field error.</summary>
/// <param name="Key">Identifier of the rule.</param>
/// <param name="Message">Error message set on the field when the rule fails.</param>
/// <param name="Validate">Receives the current field value and the controller, and returns <c>true</c> when the value is valid. It also runs for empty values of fields that are not required, so handle <c>null</c>.</param>
public sealed record DynamicValidatorRule(
    string Key,
    string Message,
    Func<object?, DynamicFormController, bool> Validate)
{
    /// <summary>Computes the error message when the rule fails, from the <see cref="DynamicFormController.Messages"/> of the controller, so the message follows the language and the culture of the moment of validation instead of the ones of the moment the rule was created. When it is <c>null</c> or returns <c>null</c>, <see cref="Message"/> is used.</summary>
    public Func<FormMessages, string?>? LocalizedMessage { get; init; }
}

/// <summary>
/// An asynchronous validation rule of a field, for a check that needs I/O such as "is this username free". It runs in the background once the user has left the field
/// (and again, debounced, as they edit it), a newer run cancels an older one, and <see cref="DynamicFormController.SubmitAsync()"/> and <see cref="DynamicFormController.NextStepAsync"/>
/// wait for it. It does not run for an empty value or for a value that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list.
/// </summary>
/// <param name="Key">Identifier of the rule.</param>
/// <param name="Validate">Receives the current field value, the controller and a token that is cancelled when the value changes or a newer run starts. Returns the error message, or <c>null</c> when the value is valid. An exception it throws in a background run is swallowed and the value stays unchecked until submission, where the exception propagates to the caller.</param>
public sealed record DynamicAsyncValidatorRule(
    string Key,
    Func<object?, DynamicFormController, CancellationToken, ValueTask<string?>> Validate)
{
    /// <summary>How long the value must stay unchanged before a background run starts, so typing does not call the service on every key. The default is 300 milliseconds. Submission runs the rule at once.</summary>
    public TimeSpan Debounce { get; init; } = TimeSpan.FromMilliseconds(300);
}

/// <summary>One tab of a <see cref="FormFieldKind.Tabs"/> field.</summary>
public sealed class DynamicTabSchema
{
    /// <summary>Unique key of the tab among its siblings.</summary>
    public required string Key { get; init; }
    /// <summary>Text shown on the tab.</summary>
    public required string Label { get; init; }
    /// <summary>Fields shown when the tab is selected.</summary>
    public IReadOnlyList<DynamicFieldSchema> Fields { get; init; } = [];
    /// <summary>Returns <c>false</c> to hide the tab. A hidden tab's fields are skipped by validation and left out of <see cref="DynamicFormController.BuildOutput"/>, and their values stay in the state.</summary>
    public Func<DynamicFormController, bool>? Condition { get; init; }
}

/// <summary>Declarative definition of one field, or of a layout element such as a section heading, a container or a repeatable list. Fields are immutable; build them with object initializers.</summary>
public sealed class DynamicFieldSchema
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
    public Func<DynamicFormController, object?>? DefaultFactory { get; init; }

    /// <summary><c>true</c> if the field must have a value. An empty string, a whitespace string, <c>null</c> and an empty collection count as empty; <c>false</c> and <c>0</c> do not.</summary>
    public bool Required { get; init; }
    /// <summary>Makes the field required while it returns <c>true</c>. It is evaluated on every validation, in addition to <see cref="Required"/>.</summary>
    public Func<DynamicFormController, bool>? RequiredWhen { get; init; }
    /// <summary>Error message of a failed required check. The default is <c>This field is required</c>.</summary>
    public string? RequiredMessage { get; init; }
    /// <summary>When the field is validated while the user edits it. The default is <see cref="FormValidationTrigger.Blur"/>.</summary>
    public FormValidationTrigger Trigger { get; init; } = FormValidationTrigger.Blur;
    /// <summary>Keys of the fields whose value this field's rules read, for example a confirmation field that compares itself with <c>password</c>. Once this field has been touched, a change of any of those fields validates it again, even when it shows no error yet, so a mismatch appears (and disappears) as soon as the other field changes. Without it, a touched field that currently shows an error is still validated again after any other field changes, which clears stale errors but cannot raise a new one. Ignored with <see cref="FormValidationTrigger.Submit"/>. On a child of a repeatable list it applies to the cells of that column.</summary>
    public IReadOnlyList<string>? DependsOn { get; init; }

    /// <summary><c>true</c> to render the control disabled; on a repeatable list it also disables its cells and the add and remove buttons. This only affects rendering: a disabled field is still validated and included in the output.</summary>
    public bool Disabled { get; init; }
    /// <summary>Disables the field while it returns <c>true</c>. Same scope as <see cref="Disabled"/>.</summary>
    public Func<DynamicFormController, bool>? DisabledWhen { get; init; }
    /// <summary>Text shown under a disabled control to explain why it is disabled.</summary>
    public string? DisabledReason { get; init; }

    /// <summary>Returns <c>false</c> to hide the field. A hidden field is skipped by validation and left out of <see cref="DynamicFormController.BuildOutput"/>, but its value stays in the state. A hidden container hides its children too, with the same effect on validation and output. On a child of a repeatable list it hides that column: its cells are neither rendered nor validated, and the column is left out of the rows of the output, while its values stay in the state.</summary>
    public Func<DynamicFormController, bool>? Condition { get; init; }
    /// <summary><c>true</c> to drop the field entirely: it has no state, is not rendered, validated or output, and its children are dropped with it.</summary>
    public bool Ignore { get; init; }
    /// <summary><c>true</c> to validate and store the field but leave it out of <see cref="DynamicFormController.BuildOutput"/>.</summary>
    public bool OmitFromOutput { get; init; }
    /// <summary><c>true</c> to show a per-field reset button in <c>KlyFormEngine</c> once the value differs from its initial value.</summary>
    public bool DirtyCheck { get; init; }
    /// <summary><c>true</c> to render a <see cref="FormFieldKind.Group"/>, <see cref="FormFieldKind.Card"/> or <see cref="FormFieldKind.Object"/> container as a disclosure (<c>details</c> and <c>summary</c>) the user can collapse, with <see cref="Label"/> as its summary. Collapsing only hides the children: they are still validated and output.</summary>
    public bool Collapsible { get; init; }
    /// <summary><c>true</c> to start a <see cref="Collapsible"/> container collapsed. Ignored when <see cref="Collapsible"/> is <c>false</c>.</summary>
    public bool DefaultCollapsed { get; init; }
    /// <summary>Number of columns the field occupies in the grid of <see cref="DynamicFormSchema.Columns"/> columns. Values are clamped between 1 and the column count, so the default of 12 fills a row.</summary>
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
    /// <summary><c>true</c> if a <see cref="FormFieldKind.File"/> field takes several files (its value is a <c>List&lt;FormFileValue&gt;</c>), or if a <see cref="FormFieldKind.Select"/> or <see cref="FormFieldKind.AutoComplete"/> field holds several values: it renders a list of checkboxes and the value is a <c>List&lt;string&gt;</c>. <see cref="DynamicFormController.CreateFieldOption"/> appends to that list.</summary>
    public bool Multiple { get; init; }
    /// <summary><c>true</c> to show a clear button once the field has a value, next to select lists, text-like inputs, textareas, passwords, number inputs and ratings. Clearing sets the value to <c>null</c>, or to an empty string for text-like inputs.</summary>
    public bool Clearable { get; init; }
    /// <summary>Static options of a choice field. Ignored when <see cref="OptionsFactory"/> is set.</summary>
    public IReadOnlyList<FormOptionItem>? Options { get; init; }
    /// <summary>Computes the options from the controller, for example to depend on another field. It takes precedence over <see cref="Options"/>.</summary>
    public Func<DynamicFormController, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; init; }
    /// <summary>Turns text typed by the user into a new option. On a <see cref="FormFieldKind.Select"/> or <see cref="FormFieldKind.AutoComplete"/> field the built-in components render a text input and a create button under the control, which call <see cref="DynamicFormController.CreateFieldOption"/>.</summary>
    public Func<string, FormOptionItem?>? OnCreateOption { get; init; }
    /// <summary>Rules a <see cref="FormFieldKind.Password"/> value must satisfy. They are checked on validation, in order, and the built-in components show them as a live checklist under the field.</summary>
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; init; }
    /// <summary>Custom validation rules, run in order after the built-in checks. The first failing rule sets the error.</summary>
    public IReadOnlyList<DynamicValidatorRule>? Validators { get; init; }
    /// <summary>Asynchronous rules, run after the synchronous checks pass and the value is not empty. They run in the background after the field is touched and while it is edited (see <see cref="DynamicAsyncValidatorRule.Debounce"/>), and <see cref="DynamicFormController.SubmitAsync()"/>, <see cref="DynamicFormController.NextStepAsync"/> and <see cref="DynamicFormController.ValidateAsync"/> wait for them. <see cref="DynamicFormController.IsValidating"/> tells whether one is running, which the components show next to the field.</summary>
    public IReadOnlyList<DynamicAsyncValidatorRule>? AsyncValidators { get; init; }

    // Transforms & watchers
    /// <summary>Converts a value from the initial input into the value held in the state. It does not apply to default values or to values set later.</summary>
    public Func<object?, object?>? TransformInput { get; init; }
    /// <summary>Converts the value in <see cref="DynamicFormController.BuildOutput"/>, for example to trim a string. The state keeps the untransformed value.</summary>
    public Func<object?, object?>? TransformOutput { get; init; }
    /// <summary>Called with the new value and the controller after the value of this field changes: after <see cref="DynamicFormController.SetValue"/>, after <see cref="DynamicFormController.ResetField"/> when it changes the value, and, for a repeatable list, after a row is added or removed or a cell changes. On a child of a repeatable list it is called with the cell value after <see cref="DynamicFormController.SetArrayItemValue"/>. It does not run for <see cref="DynamicFormController.Reset"/>, which loads a complete new state.</summary>
    public Action<object?, DynamicFormController>? Watch { get; init; }
    /// <summary>Runs when the user clicks a <see cref="FormFieldKind.Button"/> field. The button never submits the form, and is disabled like other fields by <see cref="Disabled"/> and <see cref="DisabledWhen"/>.</summary>
    public Func<DynamicFormController, Task>? OnClick { get; init; }

    // Display / Info block
    /// <summary>Presentation of a <see cref="FormFieldKind.Info"/> block: <see cref="FormInfoKind.Callout"/> (the default when null) draws a tinted box with a border, <see cref="FormInfoKind.Text"/> renders plain text without a box.</summary>
    public FormInfoKind? DisplayKind { get; init; }
    /// <summary>Text of a <see cref="FormFieldKind.Info"/> block. Falls back to <see cref="Description"/>.</summary>
    public string? DisplayText { get; init; }
    /// <summary>Color of a <see cref="FormFieldKind.Info"/> block, from the Klyroo.UI theme: <see cref="FormInfoTone.Neutral"/> when null.</summary>
    public FormInfoTone? DisplayTone { get; init; }

    // Container / Array children
    /// <summary>Child fields: the content of a container, or the columns of each row of a repeatable list. A container without children renders nothing.</summary>
    public IReadOnlyList<DynamicFieldSchema>? Fields { get; init; }
    /// <summary>Tabs of a <see cref="FormFieldKind.Tabs"/> field. A tabs field without tabs renders nothing.</summary>
    public IReadOnlyList<DynamicTabSchema>? Tabs { get; init; }
    /// <summary>Label of the add button of a repeatable list. The default is <c>Add item</c>.</summary>
    public string? AddItemLabel { get; init; }
}

/// <summary>One step of a multi-step form, declared in <see cref="DynamicFormSchema.Steps"/>.</summary>
public sealed class DynamicStepSchema
{
    /// <summary>Unique key of the step.</summary>
    public required string Key { get; init; }
    /// <summary>Title shown in the stepper.</summary>
    public required string Title { get; init; }
    /// <summary>Optional text shown under the stepper while the step is current.</summary>
    public string? Description { get; init; }
    /// <summary>Fields of the step.</summary>
    public IReadOnlyList<DynamicFieldSchema> Fields { get; init; } = [];
    /// <summary>Returns <c>true</c> to skip the step: the controller starts on the first step that is not skipped, <see cref="DynamicFormController.NextStepAsync"/> and <see cref="DynamicFormController.PreviousStepAsync"/> pass over it, the stepper does not list it, and its fields are skipped by validation and left out of <see cref="DynamicFormController.BuildOutput"/>. The current step is never treated as skipped.</summary>
    public Func<DynamicFormController, bool>? SkipWhen { get; init; }
}

/// <summary>One section of a sectioned form, declared in <see cref="DynamicFormSchema.Sections"/> and rendered by <c>KlyFormPage</c>.</summary>
public sealed class DynamicSectionSchema
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
    /// <summary>Returns <c>false</c> to hide the section and leave it out of <see cref="DynamicFormController.GetSectionStates"/>. Its fields are skipped by validation and left out of <see cref="DynamicFormController.BuildOutput"/>, and their values stay in the state.</summary>
    public Func<DynamicFormController, bool>? Condition { get; init; }
    /// <summary>Fields of the section.</summary>
    public IReadOnlyList<DynamicFieldSchema> Fields { get; init; } = [];
}

/// <summary>Declarative description of a form: its fields, optional steps or sections, validation mode and submit hooks. A schema is plain data and can be shared between controllers.</summary>
public sealed class DynamicFormSchema
{
    /// <summary>Identifier of the form, written to the <c>data-key</c> attribute of the rendered form. The default is <c>form</c>.</summary>
    public string FormKey { get; init; } = "form";
    /// <summary>Title shown in the form header.</summary>
    public string? Title { get; init; }
    /// <summary>Text shown under the title.</summary>
    public string? Description { get; init; }
    /// <summary>Small label shown above the title.</summary>
    public string? Eyebrow { get; init; }
    /// <summary>Number of columns, from 1 to 12, of the grids that lay out the fields, including those of containers and tabs. Values outside the range are clamped. <see cref="DynamicFieldSchema.Span"/> is clamped to it. The default is 12.</summary>
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
    public IReadOnlyList<DynamicFieldSchema> Fields { get; init; } = [];
    /// <summary>Steps of a wizard. When not empty, they replace <see cref="Sections"/> and <see cref="Fields"/>.</summary>
    public IReadOnlyList<DynamicStepSchema> Steps { get; init; } = [];
    /// <summary>Sections of a page form. Used when <see cref="Steps"/> is empty, and replaces <see cref="Fields"/>.</summary>
    public IReadOnlyList<DynamicSectionSchema> Sections { get; init; } = [];

    /// <summary>Runs after validation succeeds and before <see cref="OnSubmit"/>. Return <c>false</c> to cancel the submission, for example after a failed server check.</summary>
    public Func<DynamicFormController, Task<bool>>? OnBeforeSubmit { get; init; }
    /// <summary>Runs when the form is submitted and validation (and <see cref="OnBeforeSubmit"/>) succeeded. An exception it throws propagates to the caller of <see cref="DynamicFormController.SubmitAsync()"/>.</summary>
    public Func<DynamicFormController, Task>? OnSubmit { get; init; }
    /// <summary>Runs after the current step validates and before moving forward, with the index of the current step. Return <c>false</c> to stay on the step.</summary>
    public Func<DynamicFormController, int, Task<bool>>? OnBeforeNext { get; init; }
    /// <summary>Runs before moving back, with the index of the current step. Return <c>false</c> to stay on the step.</summary>
    public Func<DynamicFormController, int, Task<bool>>? OnBeforePrevious { get; init; }
}
