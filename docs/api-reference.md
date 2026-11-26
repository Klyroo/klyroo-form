# Klyroo.Form API reference

> Generated from the compiled assemblies and their XML documentation. Do not edit by hand.

## Klyroo.Form.Core

Classes (7) · Records (5) · Enums (5)

### Classes

#### `FormController`

`Klyroo.Form` · class

Runtime state of a `FormSchema`: field values, an initial snapshot for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time.

| Constructor | Description |
| :--- | :--- |
| `FormController(FormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)` | Creates a controller for `schema` and initializes the state. Values in `initialInput` win over default values, and the result becomes the baseline for dirty tracking. A stepped form starts on its first step that is not skipped. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ActiveFields` | `IReadOnlyList<FormFieldSchema>` | The stateful leaf fields in effect now, which are the ones validation checks and `FormController.BuildOutput` includes: the fields of `FormController.AllFields` that are visible, inside visible containers and visible tabs, in a visible section, and in the current step or a step that is not skipped. Stateless kinds are left out. |
| `AllFields` | `IReadOnlyList<FormFieldSchema>` | Every leaf field of the form: the fields of the steps, else of the sections, else of `FormSchema.Fields`, with groups, cards, objects and tabs flattened and ignored fields removed, whether they are visible or not. Repeatable lists are leaves; containers never are. |
| `CurrentStep` | `FormStepSchema?` | The current step, or `null` for a form without steps. |
| `CurrentStepIndex` | `int` | Zero-based index of the current step. It stays 0 for a form without steps. |
| `CurrentVisibleTopFields` | `IReadOnlyList<FormFieldSchema>` | Top-level fields to render now: those of the current step, of the visible sections, or of the schema, without hidden and ignored fields. |
| `DirtyPaths` | `IReadOnlyList<string>` | Keys whose current value differs from the initial snapshot, excluding `FormSchema.IgnoreDirtyPaths`. The snapshot is taken at construction, by `FormController.Reset` and after a successful submission. |
| `Errors` | `IReadOnlyDictionary<string, string>` | Errors to show: validation errors of touched fields, validation errors of touched cells of repeatable lists keyed by `FormController.CellPath` (for example `lines[0].sku`), plus every error set with `FormController.SetError`, by key. |
| `HasBlockingErrors` | `bool` | `true` if a field in effect (see `FormController.ActiveFields`) or a visible cell of one of its repeatable lists has a validation error, touched or not, or if any error set with `FormController.SetError` is blocking. Validation errors only exist once validation has run. |
| `InternalState` | `IReadOnlyDictionary<string, object?>` | Live, read-only view of all values by key, including keys of the initial input that match no field. |
| `IsDirty` | `bool` | `true` when `FormController.DirtyPaths` is not empty. |
| `IsFirstStep` | `bool` | `true` when every step before the current one is skipped by `FormStepSchema.SkipWhen`, and for a form without steps. |
| `IsLastStep` | `bool` | `true` when every step after the current one is skipped by `FormStepSchema.SkipWhen`, and for a form without steps. The components show the submit button instead of Next on such a step. |
| `IsStepped` | `bool` | `true` when the schema declares steps. |
| `IsSubmitting` | `bool` | `true` while `FormController.SubmitAsync` is running. |
| `Schema` | `FormSchema` | The schema this controller runs. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddArrayItem(FormFieldSchema arrayField)` | `void` | Appends a row to a repeatable list, filling each cell from the child field's default, then revalidates the list if it was touched and calls its `FormFieldSchema.Watch`. |
| `BuildOutput()` | `IReadOnlyDictionary<string, object?>` | Builds the dictionary to submit: every field in effect (see `FormController.ActiveFields`) except those with `FormFieldSchema.OmitFromOutput`, with `FormFieldSchema.TransformOutput` applied. Fields that are hidden, in a hidden container, tab or section, or in a skipped step are left out, and their values stay in the state. Lists are copied. |
| `static CellPath(string arrayKey, int index, string childKey)` | `string` | Builds the path that identifies a cell of a repeatable list in `FormController.Errors` and `FormController.GetError`: `arrayKey[index].childKey`. |
| `ClearError(string? path = null)` | `void` | Clears errors set with `FormController.SetError` and validation errors, for one field or for all. Clearing a repeatable list also clears the errors of its cells. |
| `CreateFieldOption(FormFieldSchema field, string label)` | `FormOptionItem?` | Creates an option from user text with `FormFieldSchema.OnCreateOption`, adds it to the field options and selects it, appending to the value for a field with `FormFieldSchema.Multiple`. |
| `GetArrayItemError(string arrayKey, int index, string childKey)` | `string?` | Gets the error to show for a cell of a repeatable list. |
| `GetArrayItems(string arrayKey)` | `IReadOnlyList<Dictionary<string, object?>>` | Gets the rows of a repeatable list. |
| `GetError(string path)` | `string?` | Gets the error to show for a field, or for a cell of a repeatable list by its `FormController.CellPath`. |
| `GetFieldOptions(FormFieldSchema field)` | `IReadOnlyList<FormOptionItem>` | Gets the options of a field: those from its options factory or its static options, followed by the options added with `FormController.CreateFieldOption`. |
| `GetInitialValue(string path)` | `object?` | Gets the value a field had in the initial snapshot. |
| `GetSectionStates()` | `IReadOnlyList<FormPageSectionState>` | Computes the completion state of each visible section from the current values and errors. Fields in hidden containers or tabs are not counted. |
| `GetValue(string path)` | `object?` | Gets the current value of a field. |
| `GetValue<T>(string path)` | `T` | Gets the current value converted to `T`. A value already of that type is returned as is; otherwise it is converted with the invariant culture. |
| `static IsContainer(FormFieldKind kind)` | `bool` | Tells whether a kind is a layout container whose children are fields of their own: `FormFieldKind.Group`, `FormFieldKind.Card`, `FormFieldKind.Object` and `FormFieldKind.Tabs`. A container never has a value, even without children. |
| `static IsEmptyValue(object? value)` | `bool` | Tells whether a value counts as empty for required checks: `null`, a blank string or an empty collection. `false` and `0` are not empty. |
| `IsFieldDisabled(FormFieldSchema field)` | `bool` | Tells whether a field is currently disabled, from `FormFieldSchema.Disabled` and `FormFieldSchema.DisabledWhen`. |
| `IsFieldRequired(FormFieldSchema field)` | `bool` | Tells whether a field is currently required, from `FormFieldSchema.Required` and `FormFieldSchema.RequiredWhen`. |
| `IsSectionVisible(FormPageSectionSchema section)` | `bool` | Tells whether a section is shown: its `FormPageSectionSchema.Condition` is absent or true. |
| `static IsStateless(FormFieldKind kind)` | `bool` | Tells whether a kind carries no value: `FormFieldKind.Info`, `FormFieldKind.Divider`, `FormFieldKind.Section` and `FormFieldKind.Button`. |
| `MarkArrayItemTouched(string arrayKey, int index, string childKey)` | `void` | Marks a cell of a repeatable list touched, which makes its validation error visible, and validates it unless the trigger of its child field is `FormValidationTrigger.Submit`. `KlyFormEngine` calls it when a cell loses focus. |
| `MarkTouched(string path)` | `void` | Marks a field touched, which makes its validation error visible, and validates it unless its trigger is `FormValidationTrigger.Submit`. |
| `NextStepAsync()` | `Task<bool>` | Moves to the next step that is not skipped. It validates the current step, then runs `FormSchema.OnBeforeNext`, and clears the touched state and validation errors of the new step. |
| `PreviousStepAsync()` | `Task<bool>` | Moves to the previous step that is not skipped, after `FormSchema.OnBeforePrevious`. Values are kept and the touched state and validation errors are cleared. |
| `RemoveArrayItem(string arrayKey, int index)` | `void` | Removes a row from a repeatable list, moving the errors and touched state of the following rows up, then revalidates the list if it was touched and calls its `FormFieldSchema.Watch`. Does nothing when the index is out of range. |
| `Reset(IReadOnlyDictionary<string, object?>? newInput = null)` | `void` | Clears errors and touched state, reloads the state from `newInput`, which also becomes the new dirty-tracking baseline, and returns to the first step that is not skipped. `FormFieldSchema.Watch` is not called. |
| `ResetField(string path)` | `void` | Restores a field to its initial value, clears its errors and touched state (and those of its cells for a repeatable list), and calls `FormFieldSchema.Watch` when the value changed. |
| `ResetSection(string sectionKey)` | `void` | Resets every field of a section with `FormController.ResetField`. Does nothing when the key matches no section. |
| `SetArrayItemValue(string arrayKey, int index, string childKey, object? value, bool markTouched = false)` | `void` | Sets one cell of a repeatable list. Does nothing when the index is out of range. The cell follows the `FormFieldSchema.Trigger` of its child field, like `FormController.SetValue` does for a field: it is validated with the child's rules once touched, unless the trigger is `FormValidationTrigger.Submit`, which clears its error instead. Then the child's `FormFieldSchema.Watch` is called with the cell value and the list's with the rows. |
| `SetError(string path, string message, bool blocking = false)` | `void` | Sets an error on a field, for example one returned by the server, and marks the field touched. The error is removed by the next `FormController.SetValue` on that key, by `FormController.ClearError` or by `FormController.Reset`. |
| `SetValue(string path, object? value, bool markTouched = false)` | `void` | Sets a value, clears any error set with `FormController.SetError` on the key (and the errors and touched state of the cells when it replaces the rows of a repeatable list), validates the field if it has been touched and its trigger is not `FormValidationTrigger.Submit` (with that trigger, the validation error is cleared instead), calls `FormFieldSchema.Watch` and raises `Changed`. |
| `ShouldRenderField(FormFieldSchema field)` | `bool` | Tells whether a field is shown: it is not ignored and its `FormFieldSchema.Condition` is absent or true. It does not look at the containers, tab, section or step that hold the field; `FormController.ActiveFields` does. |
| `SubmitAsync()` | `Task<bool>` | Validates every field in effect, runs `FormSchema.OnBeforeSubmit` then `FormSchema.OnSubmit`, and on success takes a new initial snapshot so the form is no longer dirty. `FormController.IsSubmitting` is true meanwhile. |
| `Validate(bool currentStepOnly = false)` | `bool` | Validates the fields in effect (see `FormController.ActiveFields`) according to `FormSchema.ValidateMode`, including each cell of their repeatable lists, and marks them touched so their errors show. Validation errors of fields that are not in effect, such as hidden ones, are removed. |

#### `FormFieldSchema`

`Klyroo.Form` · class

Declarative definition of one field, or of a layout element such as a section heading, a container or a repeatable list. Fields are immutable; build them with object initializers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `AddItemLabel` (init) | `string?` | Label of the add button of a repeatable list. The default is `Add item`. |
| `Clearable` (init) | `bool` | `true` to show a clear button once the field has a value, next to select lists, text-like inputs, textareas, passwords, number inputs and ratings. Clearing sets the value to `null`, or to an empty string for text-like inputs. |
| `Collapsible` (init) | `bool` | `true` to render a `FormFieldKind.Group`, `FormFieldKind.Card` or `FormFieldKind.Object` container as a disclosure (`details` and `summary`) the user can collapse, with `FormFieldSchema.Label` as its summary. Collapsing only hides the children: they are still validated and output. |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the field. A hidden field is skipped by validation and left out of `FormController.BuildOutput`, but its value stays in the state. A hidden container hides its children too, with the same effect on validation and output. On a child of a repeatable list it hides that column: its cells are neither rendered nor validated, and the column is left out of the rows of the output, while its values stay in the state. |
| `DefaultCollapsed` (init) | `bool` | `true` to start a `FormFieldSchema.Collapsible` container collapsed. Ignored when `FormFieldSchema.Collapsible` is `false`. |
| `DefaultFactory` (init) | `Func<FormController, object?>?` | Computes the initial value from the controller. It takes precedence over `FormFieldSchema.DefaultValue` and also fills the cells of a new row of a repeatable list. |
| `DefaultValue` (init) | `object?` | Initial value used when the initial input has no value for the key. Ignored when `FormFieldSchema.DefaultFactory` is set. |
| `Description` (init) | `string?` | Text shown under the label. For `FormFieldKind.Info` it is the fallback of `FormFieldSchema.DisplayText`. |
| `DirtyCheck` (init) | `bool` | `true` to show a per-field reset button in `KlyFormEngine` once the value differs from its initial value. |
| `Disabled` (init) | `bool` | `true` to render the control disabled; on a repeatable list it also disables its cells and the add and remove buttons. This only affects rendering: a disabled field is still validated and included in the output. |
| `DisabledReason` (init) | `string?` | Text shown under a disabled control to explain why it is disabled. |
| `DisabledWhen` (init) | `Func<FormController, bool>?` | Disables the field while it returns `true`. Same scope as `FormFieldSchema.Disabled`. |
| `DisplayKind` (init) | `string?` | Presentation of a `FormFieldKind.Info` block: `callout` (the default) draws a tinted box with a border, `text` renders plain text without a box. Other values render as `callout`. Case is ignored. |
| `DisplayText` (init) | `string?` | Text of a `FormFieldKind.Info` block. Falls back to `FormFieldSchema.Description`. |
| `DisplayTone` (init) | `string?` | Color of a `FormFieldKind.Info` block, from the Klyroo.UI theme: `neutral` (the default), `info`, `success`, `warning` or `error`. Other values render as `neutral`. Case is ignored. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>?` | Child fields: the content of a container, or the columns of each row of a repeatable list. A container without children renders nothing. |
| `Help` (init) | `string?` | Text shown under the control. |
| `Hint` (init) | `string?` | Short text shown next to the label. |
| `Ignore` (init) | `bool` | `true` to drop the field entirely: it has no state, is not rendered, validated or output, and its children are dropped with it. |
| `Key` (init) | `string` | Unique key of the field. It is the key of the value in the form state and in the output dictionary. Keys are flat strings: containers do not prefix the keys of their children. |
| `Kind` (init) | `FormFieldKind` | The kind of the field. The default is `FormFieldKind.Text`. |
| `Label` (init) | `string?` | Text of the label shown above the control. For `FormFieldKind.Info`, `FormFieldKind.Section` and containers it is the heading. |
| `Max` (init) | `double?` | Largest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `max` attribute of number and slider inputs, and the number of stars of a `FormFieldKind.Rating` field. |
| `MaxLength` (init) | `int?` | Largest accepted number of characters of a text value, enforced on validation. Also the `maxlength` attribute of text-like inputs, including textareas, passwords and one-time codes. |
| `Min` (init) | `double?` | Smallest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `min` attribute of number and slider inputs. |
| `Multiple` (init) | `bool` | `true` if a `FormFieldKind.Select` or `FormFieldKind.AutoComplete` field holds several values: it renders a list of checkboxes and the value is a `List<string>`. `FormController.CreateFieldOption` appends to that list. |
| `OmitFromOutput` (init) | `bool` | `true` to validate and store the field but leave it out of `FormController.BuildOutput`. |
| `OnClick` (init) | `Func<FormController, Task>?` | Runs when the user clicks a `FormFieldKind.Button` field. The button never submits the form, and is disabled like other fields by `FormFieldSchema.Disabled` and `FormFieldSchema.DisabledWhen`. |
| `OnCreateOption` (init) | `Func<string, FormOptionItem?>?` | Turns text typed by the user into a new option. On a `FormFieldKind.Select` or `FormFieldKind.AutoComplete` field the built-in components render a text input and a create button under the control, which call `FormController.CreateFieldOption`. |
| `Options` (init) | `IReadOnlyList<FormOptionItem>?` | Static options of a choice field. Ignored when `FormFieldSchema.OptionsFactory` is set. |
| `OptionsFactory` (init) | `Func<FormController, IReadOnlyList<FormOptionItem>>?` | Computes the options from the controller, for example to depend on another field. It takes precedence over `FormFieldSchema.Options`. |
| `PasswordRequirements` (init) | `IReadOnlyList<FormPasswordRequirement>?` | Rules a `FormFieldKind.Password` value must satisfy. They are checked on validation, in order, and the built-in components show them as a live checklist under the field. |
| `Placeholder` (init) | `string?` | Placeholder of the control. For `FormFieldKind.Select` and `FormFieldKind.AutoComplete` it is the text of the empty option. |
| `Required` (init) | `bool` | `true` if the field must have a value. An empty string, a whitespace string, `null` and an empty collection count as empty; `false` and `0` do not. |
| `RequiredMessage` (init) | `string?` | Error message of a failed required check. The default is `This field is required`. |
| `RequiredWhen` (init) | `Func<FormController, bool>?` | Makes the field required while it returns `true`. It is evaluated on every validation, in addition to `FormFieldSchema.Required`. |
| `Span` (init) | `int` | Number of columns the field occupies in the grid of `FormSchema.Columns` columns. Values are clamped between 1 and the column count, so the default of 12 fills a row. |
| `Step` (init) | `double?` | The `step` attribute of number and slider inputs. It is not validated. |
| `Tabs` (init) | `IReadOnlyList<FormTabItemSchema>?` | Tabs of a `FormFieldKind.Tabs` field. A tabs field without tabs renders nothing. |
| `TransformInput` (init) | `Func<object?, object?>?` | Converts a value from the initial input into the value held in the state. It does not apply to default values or to values set later. |
| `TransformOutput` (init) | `Func<object?, object?>?` | Converts the value in `FormController.BuildOutput`, for example to trim a string. The state keeps the untransformed value. |
| `Trigger` (init) | `FormValidationTrigger` | When the field is validated while the user edits it. The default is `FormValidationTrigger.Blur`. |
| `Validators` (init) | `IReadOnlyList<FormValidatorRule>?` | Custom validation rules, run in order after the built-in checks. The first failing rule sets the error. |
| `Watch` (init) | `Action<object?, FormController>?` | Called with the new value and the controller after the value of this field changes: after `FormController.SetValue`, after `FormController.ResetField` when it changes the value, and, for a repeatable list, after a row is added or removed or a cell changes. On a child of a repeatable list it is called with the cell value after `FormController.SetArrayItemValue`. It does not run for `FormController.Reset`, which loads a complete new state. |

#### `FormPageSectionSchema`

`Klyroo.Form` · class

One section of a sectioned form, declared in `FormSchema.Sections` and rendered by `KlyFormPage`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the section and leave it out of `FormController.GetSectionStates`. Its fields are skipped by validation and left out of `FormController.BuildOutput`, and their values stay in the state. |
| `Description` (init) | `string?` | Text shown under the title. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields of the section. |
| `Key` (init) | `string` | Unique key of the section. It is also the anchor id of the section in `KlyFormPage`. |
| `Label` (init) | `string` | Title of the section. |
| `Optional` (init) | `bool` | `true` if the section may stay empty: it then reports `FormPageSectionStatus.Optional` until a field has a value. |
| `Span` (init) | `int` | Number of columns, from 1 to 12, the section occupies in the 12-column grid of sections of `KlyFormPage`. Values outside the range are clamped. The default is 12, one section per row. |

#### `FormSchema`

`Klyroo.Form` · class

Declarative description of a form: its fields, optional steps or sections, validation mode and submit hooks. A schema is plain data and can be shared between controllers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Columns` (init) | `int` | Number of columns, from 1 to 12, of the grids that lay out the fields, including those of containers and tabs. Values outside the range are clamped. `FormFieldSchema.Span` is clamped to it. The default is 12. |
| `Description` (init) | `string?` | Text shown under the title. |
| `Eyebrow` (init) | `string?` | Small label shown above the title. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields of a single-block form. Ignored when `FormSchema.Steps` or `FormSchema.Sections` is not empty. |
| `FormKey` (init) | `string` | Identifier of the form, written to the `data-key` attribute of the rendered form. The default is `form`. |
| `IgnoreDirtyPaths` (init) | `IReadOnlyList<string>?` | Keys excluded from dirty tracking. A key also excludes the keys that start with it followed by a dot. |
| `OnBeforeNext` (init) | `Func<FormController, int, Task<bool>>?` | Runs after the current step validates and before moving forward, with the index of the current step. Return `false` to stay on the step. |
| `OnBeforePrevious` (init) | `Func<FormController, int, Task<bool>>?` | Runs before moving back, with the index of the current step. Return `false` to stay on the step. |
| `OnBeforeSubmit` (init) | `Func<FormController, Task<bool>>?` | Runs after validation succeeds and before `FormSchema.OnSubmit`. Return `false` to cancel the submission, for example after a failed server check. |
| `OnSubmit` (init) | `Func<FormController, Task>?` | Runs when the form is submitted and validation (and `FormSchema.OnBeforeSubmit`) succeeded. An exception it throws propagates to the caller of `FormController.SubmitAsync`. |
| `Sections` (init) | `IReadOnlyList<FormPageSectionSchema>` | Sections of a page form. Used when `FormSchema.Steps` is empty, and replaces `FormSchema.Fields`. |
| `ShowStepper` (init) | `bool` | `true` to show the list of steps above a stepped form. The default is `true`. |
| `Steps` (init) | `IReadOnlyList<FormStepSchema>` | Steps of a wizard. When not empty, they replace `FormSchema.Sections` and `FormSchema.Fields`. |
| `Title` (init) | `string?` | Title shown in the form header. |
| `ValidateMode` (init) | `FormValidationMode` | Which checks validation runs. The default is `FormValidationMode.All`. |

#### `FormService`

`Klyroo.Form` · class

Opens a form as an overlay and returns its result as a task. Register it with `AddKlyrooForm()` (scoped) and mount one `KlyFormProvider` in the layout so the form has somewhere to render.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ActiveForm` | `FormController?` | The controller of the open form, or `null` when none is open. |
| `ActiveMode` | `FormShellMode` | The shell mode of the open form. The default is `FormShellMode.Modal`. |
| `IsOpen` | `bool` | `true` while a form is open. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `CloseForm()` | `void` | Closes the open form without submitting and completes the task of `FormService.CreateFormAsync` with `FormSubmitOutcome.IsCompleted` set to `false`. Does nothing when no form is open. |
| `CreateFormAsync(FormSchema schema, IReadOnlyDictionary<string, object?>? input = null, FormShellMode mode = Modal)` | `Task<FormSubmitOutcome>` | Opens a form in the mounted `KlyFormProvider` and waits for its result. Opening a form while another is open replaces it, and the replaced form's task completes with `FormSubmitOutcome.IsCompleted` set to `false` and that form's output, as if it had been closed. |
| `SubmitActiveFormAsync()` | `Task<bool>` | Submits the open form with `FormController.SubmitAsync`. On success it closes the form and completes the task of `FormService.CreateFormAsync`. |

#### `FormStepSchema`

`Klyroo.Form` · class

One step of a multi-step form, declared in `FormSchema.Steps`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Optional text shown under the stepper while the step is current. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields of the step. |
| `Key` (init) | `string` | Unique key of the step. |
| `SkipWhen` (init) | `Func<FormController, bool>?` | Returns `true` to skip the step: the controller starts on the first step that is not skipped, `FormController.NextStepAsync` and `FormController.PreviousStepAsync` pass over it, the stepper does not list it, and its fields are skipped by validation and left out of `FormController.BuildOutput`. The current step is never treated as skipped. |
| `Title` (init) | `string` | Title shown in the stepper. |

#### `FormTabItemSchema`

`Klyroo.Form` · class

One tab of a `FormFieldKind.Tabs` field.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the tab. A hidden tab's fields are skipped by validation and left out of `FormController.BuildOutput`, and their values stay in the state. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields shown when the tab is selected. |
| `Key` (init) | `string` | Unique key of the tab among its siblings. |
| `Label` (init) | `string` | Text shown on the tab. |

### Records

#### `FormOptionItem`

`Klyroo.Form` · record

One choice of a `FormFieldKind.Select`, `FormFieldKind.AutoComplete`, `FormFieldKind.Radio` or `FormFieldKind.CheckboxGroup` field.

| Constructor | Description |
| :--- | :--- |
| `FormOptionItem(string Value, string Label, string? Description = null, string? Group = null, string? Icon = null, bool Disabled = false)` | One choice of a `FormFieldKind.Select`, `FormFieldKind.AutoComplete`, `FormFieldKind.Radio` or `FormFieldKind.CheckboxGroup` field. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Optional secondary text, shown under the label in radio and checkbox lists. A native select list cannot show it. |
| `Disabled` (init) | `bool` | `true` to show the option but prevent selecting it. |
| `Group` (init) | `string?` | Optional group name. Options with the same group are rendered together, in an `optgroup` in select lists and under a heading in radio and checkbox lists, in the order the groups first appear. |
| `Icon` (init) | `string?` | Optional icon name, in any form `KlyIcon` of Klyroo.UI accepts (`i-lucide-star`, `lucide:star` or a bare Lucide name), shown before the label in radio and checkbox lists. A native select list cannot show it. |
| `Label` (init) | `string` | The text shown to the user. |
| `Value` (init) | `string` | The value stored in the form when the option is selected. |

#### `FormPageSectionState`

`Klyroo.Form` · record

Completion state of one section, computed from the current values by `FormController.GetSectionStates`.

| Constructor | Description |
| :--- | :--- |
| `FormPageSectionState(string Key, string Label, string? Description, bool Optional, int RequiredCount, int MissingCount, bool Dirty, FormPageSectionStatus Status)` | Completion state of one section, computed from the current values by `FormController.GetSectionStates`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Description of the section. |
| `Dirty` (init) | `bool` | `true` when any field of the section differs from its initial value. |
| `Key` (init) | `string` | Key of the section. |
| `Label` (init) | `string` | Label of the section. |
| `MissingCount` (init) | `int` | Number of those required fields that are empty. |
| `Optional` (init) | `bool` | Whether the section is optional. |
| `RequiredCount` (init) | `int` | Number of required, visible fields in the section. |
| `Status` (init) | `FormPageSectionStatus` | Completion status of the section. |

#### `FormPasswordRequirement`

`Klyroo.Form` · record

A rule a `FormFieldKind.Password` value must satisfy. The first failing rule becomes the field error, using its `FormPasswordRequirement.Label` as the message.

| Constructor | Description |
| :--- | :--- |
| `FormPasswordRequirement(string Key, string Label, Func<string, bool> Validate)` | A rule a `FormFieldKind.Password` value must satisfy. The first failing rule becomes the field error, using its `FormPasswordRequirement.Label` as the message. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Key` (init) | `string` | Identifier of the rule. |
| `Label` (init) | `string` | Description of the rule, used as the field error when the rule fails. |
| `Validate` (init) | `Func<string, bool>` | Returns `true` when the password satisfies the rule. |

#### `FormSubmitOutcome`

`Klyroo.Form` · record

Result of an overlay opened with `FormService.CreateFormAsync`.

| Constructor | Description |
| :--- | :--- |
| `FormSubmitOutcome(bool IsCompleted, IReadOnlyDictionary<string, object?> FormData)` | Result of an overlay opened with `FormService.CreateFormAsync`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `FormData` (init) | `IReadOnlyDictionary<string, object?>` | The output of the form at that moment, as returned by `FormController.BuildOutput`. |
| `IsCompleted` (init) | `bool` | `true` when the form was submitted successfully, `false` when it was closed without submitting. |

#### `FormValidatorRule`

`Klyroo.Form` · record

A custom validation rule of a field. Rules run in order after the built-in checks and the first failing rule sets the field error.

| Constructor | Description |
| :--- | :--- |
| `FormValidatorRule(string Key, string Message, Func<object?, FormController, bool> Validate)` | A custom validation rule of a field. Rules run in order after the built-in checks and the first failing rule sets the field error. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Key` (init) | `string` | Identifier of the rule. |
| `Message` (init) | `string` | Error message set on the field when the rule fails. |
| `Validate` (init) | `Func<object?, FormController, bool>` | Receives the current field value and the controller, and returns `true` when the value is valid. It also runs for empty values of fields that are not required, so handle `null`. |

### Enums

#### `FormFieldKind`

`Klyroo.Form` · enum

Kind of a field in a `FormFieldSchema`. The kind decides how the field is stored, validated and rendered. `KlyFormEngine`, `KlyFormPage` and `KlyFormField` render every kind with a dedicated control.

| Value | Description |
| :--- | :--- |
| `Text` | Single-line text input. The value is a string. |
| `Password` | Masked text input with a show/hide toggle. Rules in `FormFieldSchema.PasswordRequirements` are checked on validation. |
| `Textarea` | Multi-line text input. The value is a string. |
| `Number` | Number input. The value is a `Double`, and `FormFieldSchema.Min` and `FormFieldSchema.Max` are enforced on validation. |
| `Checkbox` | Checkbox. The value is a `Boolean`. |
| `Switch` | Checkbox exposed to assistive technology as a switch (`role="switch"`). The value is a `Boolean`. |
| `Radio` | Radio group built from the field options. The value is the selected option value. |
| `CheckboxGroup` | Multiple choice rendered as a list of checkboxes built from the field options. The value is a `List<string>` of the checked option values. |
| `Select` | Native select list built from the field options, with an `optgroup` per `FormOptionItem.Group`. The value is the selected option value. With `FormFieldSchema.Multiple` it renders a list of checkboxes and the value is a `List<string>`. |
| `AutoComplete` | Text input that suggests the option labels through a `datalist`. The value is the value of the option whose label matches the text (ignoring case) or whose value equals it, otherwise the text itself; the typed text stays on screen until the input loses focus. With `FormFieldSchema.Multiple` it renders a list of checkboxes and the value is a `List<string>`. |
| `Date` | Date input (`type="date"`). The value is the string produced by the browser, in `yyyy-MM-dd` form. |
| `DateTime` | Date and time input (`type="datetime-local"`). The value is the string produced by the browser, in `yyyy-MM-ddTHH:mm` form. |
| `DateRange` | Range of two dates, rendered as a start and an end date input. The value is a `List<string>` of two items, start then end, in `yyyy-MM-dd` form, with an empty string for a missing date, or `null` when both are empty. A required range needs both dates, and validation rejects an end before the start. |
| `Time` | Time input (`type="time"`). The value is the string produced by the browser. |
| `PhoneNumber` | Phone number input (`type="tel"`, `autocomplete="tel"`). The value is a string; the format is not validated. |
| `Slider` | Range slider (`type="range"`) bounded by `FormFieldSchema.Min`, `FormFieldSchema.Max` and `FormFieldSchema.Step`. The value is a `Double`. While the value is `null` the slider shows `FormFieldSchema.Min` (or 0) and the state stays `null` until the user moves it. Bounds are only enforced on validation for `FormFieldKind.Number` fields. |
| `Rating` | Star rating from 1 to `FormFieldSchema.Max` (5 by default), rendered as a group of radio buttons. The value is an `Int32`. |
| `ColorPicker` | Color input (`type="color"`). The value is the string produced by the browser, in `#rrggbb` form. |
| `OneTimeCode` | One-time code input (`autocomplete="one-time-code"`, `inputmode="numeric"`, `maxlength` from `FormFieldSchema.MaxLength`). The value is a string. |
| `Tag` | List of free-form tags: Enter or a comma in the text input adds the typed tag, and each tag has a remove button. The value is a `List<string>` without blanks or duplicates. |
| `Hidden` | Hidden input. The value is stored and included in the output but nothing is shown to the user. |
| `Info` | Read-only text block showing `FormFieldSchema.Label` and `FormFieldSchema.DisplayText` (or `FormFieldSchema.Description`). It is stateless: it has no value, is never validated and never appears in the output. |
| `Divider` | Horizontal rule. Stateless, like `FormFieldKind.Info`. |
| `Section` | Heading made of `FormFieldSchema.Label` and `FormFieldSchema.Description`. Stateless, like `FormFieldKind.Info`. |
| `Button` | Button labelled by `FormFieldSchema.Label` that runs `FormFieldSchema.OnClick` and never submits the form. Stateless: it has no value, is never validated and never appears in the output. |
| `Group` | Container that lays out the fields in `FormFieldSchema.Fields` without a border. The child fields keep their own keys and are validated and output as top-level fields, unless the container is hidden. A container is layout only: without `FormFieldSchema.Fields` it renders nothing and has no value. |
| `Card` | Bordered container for the fields in `FormFieldSchema.Fields`. Children behave as in `FormFieldKind.Group`. |
| `Tabs` | Tabbed container driven by `FormFieldSchema.Tabs`. The fields of every visible tab are validated and output, whether or not the tab is selected; the fields of a hidden tab are not. Without tabs it renders nothing and has no value. |
| `Object` | Container with the same behavior as `FormFieldKind.Group`. Child values are not nested under the object key. |
| `ArrayList` | Repeatable list of rows, each row holding the fields declared in `FormFieldSchema.Fields`. The value is a `List<Dictionary<string, object?>>`. Rows are edited with `AddArrayItem`, `SetArrayItemValue` and `RemoveArrayItem`, each cell is rendered with the control of its child kind, and each cell is validated with the rules of its child field. |
| `ArrayTable` | Same value and behavior as `FormFieldKind.ArrayList`; `KlyFormEngine` renders both identically. |

#### `FormPageSectionStatus`

`Klyroo.Form` · enum

Completion status of a section in a sectioned form, computed by `FormController.GetSectionStates`.

| Value | Description |
| :--- | :--- |
| `Complete` | Every required field of the section has a value and no field has an error. |
| `Pending` | At least one required field of the section is empty. |
| `Invalid` | At least one field of the section has an error. This takes precedence over the other statuses. |
| `Optional` | The section is marked optional and none of its fields has a value. |

#### `FormShellMode`

`Klyroo.Form` · enum

Layout hint for the shell that hosts a form, used by `FormService` overlays and the `Shell` parameter of `KlyFormEngine`.

| Value | Description |
| :--- | :--- |
| `Inline` | Renders in the page flow. This is the default of `KlyFormEngine`. |
| `Modal` | Renders in a centered dialog. This is the default of `FormService.CreateFormAsync`. |
| `Drawer` | Side panel. `KlyFormProvider` renders it as a full-height panel docked to the end (right in left-to-right layouts) of the viewport. |
| `Fullscreen` | Full viewport. `KlyFormProvider` renders it over the whole viewport, without margins or rounded corners. |

#### `FormValidationMode`

`Klyroo.Form` · enum

Controls which checks a `FormController` runs when it validates, set through `FormSchema.ValidateMode`.

| Value | Description |
| :--- | :--- |
| `All` | Runs required checks, number bounds, password requirements and custom validators. |
| `RequiredOnly` | Runs required checks only. |
| `ValidatorsOnly` | Runs number bounds, password requirements and custom validators, and skips required checks. |
| `None` | Runs no validation. `FormController.Validate` returns `true` unless a blocking error set with `FormController.SetError` exists. |

#### `FormValidationTrigger`

`Klyroo.Form` · enum

Controls when a field is validated while the user edits it, set through `FormFieldSchema.Trigger`.

| Value | Description |
| :--- | :--- |
| `Blur` | Validates once the field has been touched, which `KlyFormEngine` does when the control loses focus. This is the default. |
| `Input` | Marks the field touched and validates it on every change. |
| `Submit` | Does not validate while editing. The field is validated by `FormController.Validate` and by submission, and a change clears its validation error without validating again. |

## Klyroo.Form

Components (4) · Static classes and extensions (1)

### Components

#### `KlyFormEngine`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a `FormSchema` as a form: header, optional stepper with the description of the current step, a field grid of `FormSchema.Columns` columns and the footer actions (Back, Reset, Cancel, Next, Submit). Pass either a `FormController` through `Form` or a schema through `Schema`. The component subscribes to `Changed` and re-renders on every change.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Form` | `FormController?` | The controller to render. Use it when you need to read or change the form from code. Takes precedence over `KlyFormEngine.Schema`. |
| `Input` | `IReadOnlyDictionary<string, object?>?` | Initial values by field key for the controller created from `KlyFormEngine.Schema`. Ignored when `KlyFormEngine.Form` is set. |
| `OnCancel` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with the current output when the user presses Cancel or the close button. When bound, the Cancel button is shown. |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with `FormController.BuildOutput` after the form is submitted successfully, that is after validation and the schema hooks passed. |
| `Schema` | `FormSchema?` | The schema to render when no `KlyFormEngine.Form` is given. The component creates and owns the controller, and creates a new one when the schema instance changes. |
| `Shell` | `FormShellMode` | How the form is presented. Anything other than `FormShellMode.Inline` adds a close button to the header when `KlyFormEngine.OnCancel` is bound. |
| `ShowReset` | `bool` | Shows a reset button in the footer while the form is dirty. The default is `true`. |
| `SubmitLabel` | `string` | Text of the submit button. The default is `Submit`. |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `EffectiveController` | `FormController?` | The controller in use: `KlyFormEngine.Form` if set, otherwise the one the component created from `KlyFormEngine.Schema`. |

#### `KlyFormPage`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a sectioned form as a page: a sidebar listing each section with its completion status, and one card per section laid out by `FormPageSectionSchema.Span`. Fields render with the same controls, labels, containers and repeatable lists as `KlyFormEngine`, and the page is a `form` element, so pressing Enter in a field submits it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Form` | `FormController` | The controller to render. Its schema must declare `FormSchema.Sections`. |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with `FormController.BuildOutput` after the form is submitted successfully. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormProvider`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Host of the overlay forms opened with `FormService`. Mount one instance in a layout, wrapping the page content, and call `FormService.CreateFormAsync` from anywhere in the same scope.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The content of the provider, usually the page body. Overlay forms render after it. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySchemaField`

`Klyroo.Form` · component · inherits `ComponentBase`

Renders one field of a `FormController` with the control of its `FormFieldKind`, its label, description, hint, help, error and per-field reset, the way `KlyFormEngine` and `KlyFormPage` render their fields. Containers, tabs and repeatable lists render their children too. Place it in a CSS grid of `FormSchema.Columns` columns, since the field sets its own `grid-column` span. It renders nothing when `FormController.ShouldRenderField` is `false`. It does not subscribe to `Changed`: render it from a component that re-renders on that event, so changes made elsewhere (by code or by another field) show up.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Field` | `FormFieldSchema` | The field to render, usually one of `FormController.CurrentVisibleTopFields`. Containers render their children with nested `KlySchemaField` components. |
| `Form` | `FormController` | The controller that holds the value of the field. |

### Static classes and extensions

#### `KlyrooFormServiceCollectionExtensions`

`Klyroo.Form` · service

Dependency injection registration for Klyroo.Form.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooForm(IServiceCollection services)` | `IServiceCollection` | Registers `FormService` as a scoped service and calls `AddKlyrooUi()`, because the components build on Klyroo.UI. In a Blazor Web App with interactive WebAssembly or Auto, call it in the server project and in the client project. |
