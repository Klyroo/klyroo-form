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
| `FormController(FormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)` | Creates a controller for `schema` and initializes the state. Values in `initialInput` win over default values, and the result becomes the baseline for dirty tracking. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `AllFields` | `IReadOnlyList<FormFieldSchema>` | Every leaf field of the form: the fields of the steps, else of the sections, else of `FormSchema.Fields`, with groups, cards, objects and tabs flattened and ignored fields removed. Repeatable lists are leaves. |
| `CurrentStep` | `FormStepSchema?` | The current step, or `null` for a form without steps. |
| `CurrentStepIndex` | `int` | Zero-based index of the current step. It stays 0 for a form without steps. |
| `CurrentVisibleTopFields` | `IReadOnlyList<FormFieldSchema>` | Top-level fields to render now: those of the current step, of the visible sections, or of the schema, without hidden and ignored fields. |
| `DirtyPaths` | `IReadOnlyList<string>` | Keys whose current value differs from the initial snapshot, excluding `FormSchema.IgnoreDirtyPaths`. The snapshot is taken at construction, by `FormController.Reset` and after a successful submission. |
| `Errors` | `IReadOnlyDictionary<string, string>` | Errors to show: validation errors of touched fields plus every error set with `FormController.SetError`, by key. |
| `HasBlockingErrors` | `bool` | `true` if any validation error exists, touched or not, or any error set with `FormController.SetError` as blocking. Validation errors only exist once validation has run. |
| `InternalState` | `IReadOnlyDictionary<string, object?>` | Live, read-only view of all values by key, including keys of the initial input that match no field. |
| `IsDirty` | `bool` | `true` when `FormController.DirtyPaths` is not empty. |
| `IsFirstStep` | `bool` | `true` on the first step, and for a form without steps. |
| `IsLastStep` | `bool` | `true` on the last declared step, and for a form without steps. A later step that `FormStepSchema.SkipWhen` skips does not change this. |
| `IsStepped` | `bool` | `true` when the schema declares steps. |
| `IsSubmitting` | `bool` | `true` while `FormController.SubmitAsync` is running. |
| `Schema` | `FormSchema` | The schema this controller runs. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddArrayItem(FormFieldSchema arrayField)` | `void` | Appends a row to a repeatable list, filling each cell from the child field's default. |
| `BuildOutput()` | `IReadOnlyDictionary<string, object?>` | Builds the dictionary to submit: every visible, stateful field except those with `FormFieldSchema.OmitFromOutput`, with `FormFieldSchema.TransformOutput` applied. Lists are copied. |
| `ClearError(string? path = null)` | `void` | Clears errors set with `FormController.SetError` and validation errors, for one field or for all. |
| `CreateFieldOption(FormFieldSchema field, string label)` | `FormOptionItem?` | Creates an option from user text with `FormFieldSchema.OnCreateOption`, adds it to the field options and selects it, appending to the value for a field with `FormFieldSchema.Multiple`. |
| `GetArrayItems(string arrayKey)` | `IReadOnlyList<Dictionary<string, object?>>` | Gets the rows of a repeatable list. |
| `GetError(string path)` | `string?` | Gets the error to show for a field. |
| `GetFieldOptions(FormFieldSchema field)` | `IReadOnlyList<FormOptionItem>` | Gets the options of a field: those from its options factory or its static options, followed by the options added with `FormController.CreateFieldOption`. |
| `GetInitialValue(string path)` | `object?` | Gets the value a field had in the initial snapshot. |
| `GetSectionStates()` | `IReadOnlyList<FormPageSectionState>` | Computes the completion state of each visible section from the current values and errors. |
| `GetValue(string path)` | `object?` | Gets the current value of a field. |
| `GetValue<T>(string path)` | `T` | Gets the current value converted to `T`. A value already of that type is returned as is; otherwise it is converted with the invariant culture. |
| `static IsEmptyValue(object? value)` | `bool` | Tells whether a value counts as empty for required checks: `null`, a blank string or an empty collection. `false` and `0` are not empty. |
| `IsFieldDisabled(FormFieldSchema field)` | `bool` | Tells whether a field is currently disabled, from `FormFieldSchema.Disabled` and `FormFieldSchema.DisabledWhen`. |
| `IsFieldRequired(FormFieldSchema field)` | `bool` | Tells whether a field is currently required, from `FormFieldSchema.Required` and `FormFieldSchema.RequiredWhen`. |
| `IsSectionVisible(FormPageSectionSchema section)` | `bool` | Tells whether a section is shown: its `FormPageSectionSchema.Condition` is absent or true. |
| `static IsStateless(FormFieldKind kind)` | `bool` | Tells whether a kind carries no value: `FormFieldKind.Info`, `FormFieldKind.Divider`, `FormFieldKind.Section` and `FormFieldKind.Button`. |
| `MarkTouched(string path)` | `void` | Marks a field touched, which makes its validation error visible, and validates it unless its trigger is `FormValidationTrigger.Submit`. |
| `NextStepAsync()` | `Task<bool>` | Moves to the next step that is not skipped. It validates the current step, then runs `FormSchema.OnBeforeNext`, and clears the touched state and validation errors of the new step. |
| `PreviousStepAsync()` | `Task<bool>` | Moves to the previous step that is not skipped, after `FormSchema.OnBeforePrevious`. Values are kept and the touched state and validation errors are cleared. |
| `RemoveArrayItem(string arrayKey, int index)` | `void` | Removes a row from a repeatable list. Does nothing when the index is out of range. |
| `Reset(IReadOnlyDictionary<string, object?>? newInput = null)` | `void` | Clears errors and touched state, returns to the first step and reloads the state from `newInput`, which also becomes the new dirty-tracking baseline. |
| `ResetField(string path)` | `void` | Restores a field to its initial value and clears its errors and touched state. |
| `ResetSection(string sectionKey)` | `void` | Resets every field of a section with `FormController.ResetField`. Does nothing when the key matches no section. |
| `SetArrayItemValue(string arrayKey, int index, string childKey, object? value)` | `void` | Sets one cell of a repeatable list. Does nothing when the index is out of range. Cells are not validated and do not call `FormFieldSchema.Watch`. |
| `SetError(string path, string message, bool blocking = false)` | `void` | Sets an error on a field, for example one returned by the server, and marks the field touched. The error is removed by the next `FormController.SetValue` on that key, by `FormController.ClearError` or by `FormController.Reset`. |
| `SetValue(string path, object? value, bool markTouched = false)` | `void` | Sets a value, clears any error set with `FormController.SetError` on the key, validates the field if it has been touched and its trigger is not `FormValidationTrigger.Submit`, calls `FormFieldSchema.Watch` and raises `Changed`. |
| `ShouldRenderField(FormFieldSchema field)` | `bool` | Tells whether a field is shown: it is not ignored and its `FormFieldSchema.Condition` is absent or true. |
| `SubmitAsync()` | `Task<bool>` | Validates every field, runs `FormSchema.OnBeforeSubmit` then `FormSchema.OnSubmit`, and on success takes a new initial snapshot so the form is no longer dirty. `FormController.IsSubmitting` is true meanwhile. |
| `Validate(bool currentStepOnly = false)` | `bool` | Validates the fields according to `FormSchema.ValidateMode` and marks them touched so their errors show. Hidden and stateless fields are skipped, and cells of repeatable lists are not checked. |

#### `FormFieldSchema`

`Klyroo.Form` · class

Declarative definition of one field, or of a layout element such as a section heading, a container or a repeatable list. Fields are immutable; build them with object initializers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `AddItemLabel` (init) | `string?` | Label of the add button of a repeatable list. The default is `Add item`. |
| `Clearable` (init) | `bool` | `true` to show a clear button next to select lists and text-like inputs once they have a value. |
| `Collapsible` (init) | `bool` | Reserved. The built-in components do not use it. |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the field. A hidden field is skipped by validation and left out of `FormController.BuildOutput`, but its value stays in the state. A container's condition hides the container only: put the condition on the children as well to exclude them from validation and output. |
| `DefaultCollapsed` (init) | `bool` | Reserved. The built-in components do not use it. |
| `DefaultFactory` (init) | `Func<FormController, object?>?` | Computes the initial value from the controller. It takes precedence over `FormFieldSchema.DefaultValue` and also fills the cells of a new row of a repeatable list. |
| `DefaultValue` (init) | `object?` | Initial value used when the initial input has no value for the key. Ignored when `FormFieldSchema.DefaultFactory` is set. |
| `Description` (init) | `string?` | Text shown under the label. For `FormFieldKind.Info` it is the fallback of `FormFieldSchema.DisplayText`. |
| `DirtyCheck` (init) | `bool` | `true` to show a per-field reset button in `KlyFormEngine` once the value differs from its initial value. |
| `Disabled` (init) | `bool` | `true` to render the control disabled. This only affects rendering: a disabled field is still validated and included in the output. |
| `DisabledReason` (init) | `string?` | Text shown under a disabled control to explain why it is disabled. |
| `DisabledWhen` (init) | `Func<FormController, bool>?` | Disables the field while it returns `true`. Same scope as `FormFieldSchema.Disabled`. |
| `DisplayKind` (init) | `string?` | Reserved. The built-in components do not use it. |
| `DisplayText` (init) | `string?` | Text of a `FormFieldKind.Info` block. Falls back to `FormFieldSchema.Description`. |
| `DisplayTone` (init) | `string?` | Reserved. The built-in components do not use it. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>?` | Child fields: the content of a container, or the columns of each row of a repeatable list. |
| `Help` (init) | `string?` | Text shown under the control. |
| `Hint` (init) | `string?` | Short text shown next to the label. |
| `Ignore` (init) | `bool` | `true` to drop the field entirely: it has no state, is not rendered, validated or output, and its children are dropped with it. |
| `Key` (init) | `string` | Unique key of the field. It is the key of the value in the form state and in the output dictionary. Keys are flat strings: containers do not prefix the keys of their children. |
| `Kind` (init) | `FormFieldKind` | The kind of the field. The default is `FormFieldKind.Text`. |
| `Label` (init) | `string?` | Text of the label shown above the control. For `FormFieldKind.Info`, `FormFieldKind.Section` and containers it is the heading. |
| `Max` (init) | `double?` | Largest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `max` attribute of number and slider inputs. |
| `MaxLength` (init) | `int?` | The `maxlength` attribute of text-like inputs. The controller does not validate it; add a `FormValidatorRule` to enforce a length. |
| `Min` (init) | `double?` | Smallest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `min` attribute of number and slider inputs. |
| `Multiple` (init) | `bool` | `true` if the field holds several values. Only `FormController.CreateFieldOption` reads it: the built-in components render no multi-select control. |
| `OmitFromOutput` (init) | `bool` | `true` to validate and store the field but leave it out of `FormController.BuildOutput`. |
| `OnCreateOption` (init) | `Func<string, FormOptionItem?>?` | Turns text typed by the user into a new option. Only `FormController.CreateFieldOption` calls it; the built-in components render no control that does. |
| `Options` (init) | `IReadOnlyList<FormOptionItem>?` | Static options of a choice field. Ignored when `FormFieldSchema.OptionsFactory` is set. |
| `OptionsFactory` (init) | `Func<FormController, IReadOnlyList<FormOptionItem>>?` | Computes the options from the controller, for example to depend on another field. It takes precedence over `FormFieldSchema.Options`. |
| `PasswordRequirements` (init) | `IReadOnlyList<FormPasswordRequirement>?` | Rules a `FormFieldKind.Password` value must satisfy. They are checked on validation, in order; the built-in components do not render them as a checklist. |
| `Placeholder` (init) | `string?` | Placeholder of the control. For `FormFieldKind.Select` and `FormFieldKind.AutoComplete` it is the text of the empty option. |
| `Required` (init) | `bool` | `true` if the field must have a value. An empty string, a whitespace string, `null` and an empty collection count as empty; `false` and `0` do not. |
| `RequiredMessage` (init) | `string?` | Error message of a failed required check. The default is `This field is required`. |
| `RequiredWhen` (init) | `Func<FormController, bool>?` | Makes the field required while it returns `true`. It is evaluated on every validation, in addition to `FormFieldSchema.Required`. |
| `Span` (init) | `int` | Number of columns, from 1 to 12, the field occupies in the 12-column grid. Values outside the range are clamped. The default is 12. |
| `Step` (init) | `double?` | The `step` attribute of number and slider inputs. It is not validated. |
| `Tabs` (init) | `IReadOnlyList<FormTabItemSchema>?` | Tabs of a `FormFieldKind.Tabs` field. |
| `TransformInput` (init) | `Func<object?, object?>?` | Converts a value from the initial input into the value held in the state. It does not apply to default values or to values set later. |
| `TransformOutput` (init) | `Func<object?, object?>?` | Converts the value in `FormController.BuildOutput`, for example to trim a string. The state keeps the untransformed value. |
| `Trigger` (init) | `FormValidationTrigger` | When the field is validated while the user edits it. The default is `FormValidationTrigger.Blur`. |
| `Validators` (init) | `IReadOnlyList<FormValidatorRule>?` | Custom validation rules, run in order after the built-in checks. The first failing rule sets the error. |
| `Watch` (init) | `Action<object?, FormController>?` | Called with the new value and the controller after `FormController.SetValue` changes this field. It does not run for repeatable-list edits or resets. |

#### `FormPageSectionSchema`

`Klyroo.Form` · class

One section of a sectioned form, declared in `FormSchema.Sections` and rendered by `KlyFormPage`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the section and leave it out of `FormController.GetSectionStates`. Its fields are still validated and output. |
| `Description` (init) | `string?` | Text shown under the title. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields of the section. |
| `Key` (init) | `string` | Unique key of the section. It is also the anchor id of the section in `KlyFormPage`. |
| `Label` (init) | `string` | Title of the section. |
| `Optional` (init) | `bool` | `true` if the section may stay empty: it then reports `FormPageSectionStatus.Optional` until a field has a value. |
| `Span` (init) | `int` | Reserved. `KlyFormPage` does not use it. |

#### `FormSchema`

`Klyroo.Form` · class

Declarative description of a form: its fields, optional steps or sections, validation mode and submit hooks. A schema is plain data and can be shared between controllers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Columns` (init) | `int` | Reserved. The built-in components always lay fields out on a 12-column grid. |
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
| `CreateFormAsync(FormSchema schema, IReadOnlyDictionary<string, object?>? input = null, FormShellMode mode = Modal)` | `Task<FormSubmitOutcome>` | Opens a form in the mounted `KlyFormProvider` and waits for its result. Opening a form while another is open replaces it, and the replaced form's task never completes. |
| `SubmitActiveFormAsync()` | `Task<bool>` | Submits the open form with `FormController.SubmitAsync`. On success it closes the form and completes the task of `FormService.CreateFormAsync`. |

#### `FormStepSchema`

`Klyroo.Form` · class

One step of a multi-step form, declared in `FormSchema.Steps`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Optional description. Not rendered by the built-in components. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields of the step. |
| `Key` (init) | `string` | Unique key of the step. |
| `Root` (init) | `string?` | Reserved. Neither the controller nor the built-in components use it. |
| `SkipWhen` (init) | `Func<FormController, bool>?` | Returns `true` to skip the step: `FormController.NextStepAsync` and `FormController.PreviousStepAsync` pass over it, and the stepper does not list it. Its fields are still validated and output on submit. |
| `Title` (init) | `string` | Title shown in the stepper. |

#### `FormTabItemSchema`

`Klyroo.Form` · class

One tab of a `FormFieldKind.Tabs` field.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the tab. A hidden tab's fields are still validated and output. |
| `Fields` (init) | `IReadOnlyList<FormFieldSchema>` | Fields shown when the tab is selected. |
| `Key` (init) | `string` | Unique key of the tab among its siblings. |
| `Label` (init) | `string` | Text shown on the tab. |

### Records

#### `FormOptionItem`

`Klyroo.Form` · record

One choice of a `FormFieldKind.Select`, `FormFieldKind.AutoComplete` or `FormFieldKind.Radio` field.

| Constructor | Description |
| :--- | :--- |
| `FormOptionItem(string Value, string Label, string? Description = null, string? Group = null, string? Icon = null, bool Disabled = false)` | One choice of a `FormFieldKind.Select`, `FormFieldKind.AutoComplete` or `FormFieldKind.Radio` field. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Optional secondary text. Not rendered by the built-in components. |
| `Disabled` (init) | `bool` | `true` to show the option but prevent selecting it. |
| `Group` (init) | `string?` | Optional group name. Not rendered by the built-in components. |
| `Icon` (init) | `string?` | Optional icon name. Not rendered by the built-in components. |
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

Kind of a field in a `FormFieldSchema`. The kind decides how the field is stored, validated and rendered; kinds without a dedicated control in `KlyFormEngine` are rendered as a text input.

| Value | Description |
| :--- | :--- |
| `Text` | Single-line text input. The value is a string. |
| `Password` | Masked text input with a show/hide toggle. Rules in `FormFieldSchema.PasswordRequirements` are checked on validation. |
| `Textarea` | Multi-line text input. The value is a string. |
| `Number` | Number input. The value is a `Double`, and `FormFieldSchema.Min` and `FormFieldSchema.Max` are enforced on validation. |
| `Checkbox` | Checkbox. The value is a `Boolean`. |
| `Switch` | Rendered by `KlyFormEngine` exactly like `FormFieldKind.Checkbox`. The value is a `Boolean`. |
| `Radio` | Radio group built from the field options. The value is the selected option value. |
| `CheckboxGroup` | Multiple choice from a list of options. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `Select` | Native select list built from the field options. The value is the selected option value. |
| `AutoComplete` | Rendered by `KlyFormEngine` exactly like `FormFieldKind.Select`, as a native select list. |
| `Date` | Date input (`type="date"`). The value is the string produced by the browser, in `yyyy-MM-dd` form. |
| `DateTime` | Date and time value. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `DateRange` | Range of two dates. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `Time` | Time input (`type="time"`). The value is the string produced by the browser. |
| `PhoneNumber` | Phone number. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `Slider` | Range slider (`type="range"`) bounded by `FormFieldSchema.Min`, `FormFieldSchema.Max` and `FormFieldSchema.Step`. The value is a `Double`. Bounds are only enforced on validation for `FormFieldKind.Number` fields. |
| `Rating` | Rating value. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `ColorPicker` | Color value. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `OneTimeCode` | One-time code. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `Tag` | List of free-form tags. `KlyFormEngine` has no dedicated control for it and renders a text input. |
| `Hidden` | Hidden input. The value is stored and included in the output but nothing is shown to the user. |
| `Info` | Read-only text block showing `FormFieldSchema.Label` and `FormFieldSchema.DisplayText` (or `FormFieldSchema.Description`). It is stateless: it has no value, is never validated and never appears in the output. |
| `Divider` | Horizontal rule. Stateless, like `FormFieldKind.Info`. |
| `Section` | Heading made of `FormFieldSchema.Label` and `FormFieldSchema.Description`. Stateless, like `FormFieldKind.Info`. |
| `Button` | Stateless placeholder: it has no value, is never validated and never appears in the output. `KlyFormEngine` has no dedicated rendering for it, so do not use it to add a button. |
| `Group` | Container that lays out the fields in `FormFieldSchema.Fields` without a border. The child fields keep their own keys and are validated and output as top-level fields. |
| `Card` | Bordered container for the fields in `FormFieldSchema.Fields`. Children behave as in `FormFieldKind.Group`. |
| `Tabs` | Tabbed container driven by `FormFieldSchema.Tabs`. The fields of every tab are validated and output, whether or not the tab is selected. |
| `Object` | Container with the same behavior as `FormFieldKind.Group`. Child values are not nested under the object key. |
| `ArrayList` | Repeatable list of rows, each row holding the fields declared in `FormFieldSchema.Fields`. The value is a `List<Dictionary<string, object?>>`. Rows are edited with `AddArrayItem`, `SetArrayItemValue` and `RemoveArrayItem`, and `KlyFormEngine` renders each cell as a text input. |
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
| `Drawer` | Drawer layout. `KlyFormProvider` currently renders it in the same centered dialog as `FormShellMode.Modal` and only sets a `data-mode` attribute. |
| `Fullscreen` | Fullscreen layout. `KlyFormProvider` currently renders it in the same centered dialog as `FormShellMode.Modal` and only sets a `data-mode` attribute. |

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
| `Submit` | Does not validate while editing. The field is validated by `FormController.Validate` and by submission. |

## Klyroo.Form

Components (3) · Static classes and extensions (1)

### Components

#### `KlyFormEngine`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a `FormSchema` as a form: header, optional stepper, a 12-column field grid and the footer actions (Back, Reset, Cancel, Next, Submit). Pass either a `FormController` through `Form` or a schema through `Schema`. The component subscribes to `Changed` and re-renders on every change.

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

Renders a sectioned form as a page: a sidebar listing each section with its completion status, and one card per section. Every field is rendered as a text input, whatever its `FormFieldKind`; use `KlyFormEngine` when you need the typed controls.

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

### Static classes and extensions

#### `KlyrooFormServiceCollectionExtensions`

`Klyroo.Form` · service

Dependency injection registration for Klyroo.Form.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooForm(IServiceCollection services)` | `IServiceCollection` | Registers `FormService` as a scoped service and calls `AddKlyrooUi()`, because the components build on Klyroo.UI. In a Blazor Web App with interactive WebAssembly or Auto, call it in the server project and in the client project. |
