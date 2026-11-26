# Klyroo.Form API reference

> Generated from the compiled assemblies and their XML documentation. Do not edit by hand.

## Klyroo.Form.Core

Static classes and extensions (1) · Classes (9) · Records (9) · Enums (5)

### Static classes and extensions

#### `FormMessageLocales`

`Klyroo.Form` · service

The built-in `FormMessages`, one set per locale of Klyroo.UI and keyed by the same codes, usable without Blazor: `new FormController(schema) { Messages = FormMessageLocales.Fr }`. English and French are reviewed by a native speaker, the other sets are drafts waiting for review. The Blazor components pick the set of their current locale and let you change it with `KlyrooFormOptions.Texts`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Ar` | `FormMessages` | العربية (ar). Needs review. |
| `static De` | `FormMessages` | Deutsch (de). Needs review. |
| `static En` | `FormMessages` | English. |
| `static Es` | `FormMessages` | Español (es). Needs review. |
| `static Fr` | `FormMessages` | Français (fr). Reviewed. |
| `static It` | `FormMessages` | Italiano (it). Needs review. |
| `static Ja` | `FormMessages` | 日本語 (ja). Needs review. |
| `static Nl` | `FormMessages` | Nederlands (nl). Needs review. |
| `static Pt` | `FormMessages` | Português (pt). Needs review. |
| `static PtBr` | `FormMessages` | Português (Brasil) (pt-BR). Needs review. |
| `static ReviewedCodes` | `IReadOnlyList<string>` | The codes whose messages were reviewed by a native speaker. The other sets are drafts waiting for review. |
| `static Ru` | `FormMessages` | Русский (ru). Needs review. |
| `static Tables` | `IReadOnlyDictionary<string, FormMessages>` | Every set by locale code: `en`, `fr`, `de`, `es`, `it`, `pt`, `pt-BR`, `nl`, `ja`, `zh-Hans`, `ar` and `ru`. |
| `static ZhCn` | `FormMessages` | 简体中文 (zh-Hans). Needs review. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Resolve(string code)` | `FormMessages` | Picks the set for a culture name with the rules Klyroo.UI uses for its own texts: the exact code ("pt-BR"), then its parent cultures ("pt-PT" gets "pt", "zh-CN" gets "zh-Hans"), then English. A name the runtime has no data for (invariant globalization) is walked by trimming its last "-" segment. |

### Classes

#### `FormControlContext`

`Klyroo.Form` · class

Everything a control needs to edit one field: the value and how to change it, the error, the state flags and the ids that tie the control to its label, description, help and error. `KlyFormEngine` and `KlyFormPage` build one for each control they render, and pass it to the renderer of a `FormFieldKind.Custom` field. The same type serves a field and a cell of a repeatable list. A context is a snapshot of one render: build a new one (the components do) after the form changes.

| Constructor | Description |
| :--- | :--- |
| `FormControlContext(FormController form, FormFieldSchema field, string id)` | Creates the context of a field of `form`, with the state, error and ids the built-in controls use. `FormControlContext.Set`, `FormControlContext.Commit` and `FormControlContext.Blur` act on the field through the controller. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `DescribedBy` | `string?` | The ids of the description, help and error elements to put in `aria-describedby`, space separated, or `null`. |
| `Disabled` | `bool` | `true` when the field is disabled (`FormFieldSchema.Disabled` or `FormFieldSchema.DisabledWhen`, or its list is). Render the control disabled. |
| `Error` | `string?` | The error to show, or `null`. The error text itself is rendered by the component around the control. |
| `ErrorId` | `string` | The id of the error element. It exists only while `FormControlContext.Error` is not `null`. |
| `Field` | `FormFieldSchema` | The field being edited. For a cell of a repeatable list, the child field of the column. |
| `Form` | `FormController` | The controller that holds the value. |
| `Id` | `string` | The `id` the control must use, so that the label points at it. |
| `IsCell` | `bool` | `true` when the control is a cell of a repeatable list. |
| `LabelledBy` | `string?` | The ids of the elements that label a control made of several inputs (`aria-labelledby`), or `null` when a `label` element points at `FormControlContext.Id`. |
| `Required` | `bool` | `true` when the field is required (`FormFieldSchema.Required` or `FormFieldSchema.RequiredWhen`). Set `aria-required` on the control. |
| `Slot` | `string` | The `data-slot` the built-in controls carry (`form-control`, or `form-array-input` in a cell of a repeatable list). |
| `Validating` | `bool` | `true` while an asynchronous rule of the field runs (see `FormController.IsValidating`). |
| `Value` | `object?` | The value when this context was built. Use `FormControlContext.Current` for the live value inside an event handler. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Blur()` | `void` | Tells the form the control lost focus: the field is marked touched and validated, which is what makes its error show. |
| `Commit(object? value)` | `void` | Changes the value and marks the field touched, for a change that is complete in one gesture: a click, a selection, a removal. The error shows at once. |
| `Current()` | `object?` | Gets the live value, which may be newer than `FormControlContext.Value`. In a cell, the value of the cell. |
| `Current<T>()` | `T` | Gets the live value converted to `T` like `FormController.GetValue`` does. |
| `Set(object? value)` | `void` | Changes the value while the user is editing, for example on every keystroke. The field is validated as `FormController.SetValue` does: only once touched, and per its trigger. |

#### `FormController`

`Klyroo.Form` · class

Runtime state of a `FormSchema`: field values, an initial snapshot for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time.

| Constructor | Description |
| :--- | :--- |
| `FormController(FormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)` | Creates a controller for `schema` and initializes the state. Values in `initialInput` win over default values, and the result becomes the baseline for dirty tracking. A stepped form starts on its first step that is not skipped. |

| Field | Type | Description |
| :--- | :--- | :--- |
| `const FormErrorKey = "$form"` | `string` | The key `FormController.SetFormError` uses for the form-level error. It matches no field, so the error counts as form-level. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ActiveFields` | `IReadOnlyList<FormFieldSchema>` | The stateful leaf fields in effect now, which are the ones validation checks and `FormController.BuildOutput` includes: the fields of `FormController.AllFields` that are visible, inside visible containers and visible tabs, in a visible section, and in the current step or a step that is not skipped. Stateless kinds are left out. |
| `AllFields` | `IReadOnlyList<FormFieldSchema>` | Every leaf field of the form: the fields of the steps, else of the sections, else of `FormSchema.Fields`, with groups, cards, objects and tabs flattened and ignored fields removed, whether they are visible or not. Repeatable lists are leaves; containers never are. |
| `CurrentStep` | `FormStepSchema?` | The current step, or `null` for a form without steps. |
| `CurrentStepIndex` | `int` | Zero-based index of the current step. It stays 0 for a form without steps. |
| `CurrentVisibleTopFields` | `IReadOnlyList<FormFieldSchema>` | Top-level fields to render now: those of the current step, of the visible sections, or of the schema, without hidden and ignored fields. |
| `DirtyPaths` | `IReadOnlyList<string>` | Keys whose current value differs from the initial snapshot, excluding `FormSchema.IgnoreDirtyPaths`. The snapshot is taken at construction, by `FormController.Reset` and after a successful submission. |
| `Errors` | `IReadOnlyDictionary<string, string>` | Errors to show: validation errors of touched fields, validation errors of touched cells of repeatable lists keyed by `FormController.CellPath` (for example `lines[0].sku`), plus every error set with `FormController.SetError`, by key. |
| `FormErrors` | `IReadOnlyList<string>` | The messages of the form-level errors: those set with `FormController.SetError` on a key that matches no field of the schema, such as `FormController.FormErrorKey`. |
| `HasBlockingErrors` | `bool` | `true` if a field in effect (see `FormController.ActiveFields`) or a visible cell of one of its repeatable lists has a validation error, touched or not, or if any error set with `FormController.SetError` is blocking. Validation errors only exist once validation has run. |
| `HasPendingValidation` | `bool` | `true` while any asynchronous rule (see `FormFieldSchema.AsyncValidators`) is waiting for its debounce or running. |
| `InternalState` | `IReadOnlyDictionary<string, object?>` | Live, read-only view of all values by key, including keys of the initial input that match no field. |
| `IsDirty` | `bool` | `true` when `FormController.DirtyPaths` is not empty. |
| `IsFirstStep` | `bool` | `true` when every step before the current one is skipped by `FormStepSchema.SkipWhen`, and for a form without steps. |
| `IsLastStep` | `bool` | `true` when every step after the current one is skipped by `FormStepSchema.SkipWhen`, and for a form without steps. The components show the submit button instead of Next on such a step. |
| `IsStepped` | `bool` | `true` when the schema declares steps. |
| `IsSubmitting` | `bool` | `true` while `FormController.SubmitAsync` is running. |
| `Messages` (get, set) | `FormMessages` | The built-in validation messages in use: the ones set here, else `FormSchema.Messages`, else the ones a Klyroo.Form component gave for its locale, else English. Setting it fixes the language of this controller: components no longer replace it. Errors already computed are computed again with the new messages. |
| `Schema` | `FormSchema` | The schema this controller runs. |
| `SubmitFailed` | `bool` | `true` after a submission, or a move to the next step, failed because of validation, a hook or a handler, until a submission succeeds, the step changes or `FormController.Reset` runs. The components list every field error in the error summary while it is set. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddArrayItem(FormFieldSchema arrayField)` | `void` | Appends a row to a repeatable list, filling each cell from the child field's default, then revalidates the list if it was touched and calls its `FormFieldSchema.Watch`. |
| `BuildOutput()` | `IReadOnlyDictionary<string, object?>` | Builds the dictionary to submit: every field in effect (see `FormController.ActiveFields`) except those with `FormFieldSchema.OmitFromOutput`, with `FormFieldSchema.TransformOutput` applied. Fields that are hidden, in a hidden container, tab or section, or in a skipped step are left out, and their values stay in the state. Lists are copied. |
| `static CellPath(string arrayKey, int index, string childKey)` | `string` | Builds the path that identifies a cell of a repeatable list in `FormController.Errors` and `FormController.GetError`: `arrayKey[index].childKey`. |
| `ClearError(string? path = null)` | `void` | Clears errors set with `FormController.SetError` and validation errors, for one field or for all. Clearing a repeatable list also clears the errors of its cells. |
| `CreateFieldOption(FormFieldSchema field, string label)` | `FormOptionItem?` | Creates an option from user text with `FormFieldSchema.OnCreateOption`, adds it to the field options and selects it, appending to the value for a field with `FormFieldSchema.Multiple`. |
| `ExportState(bool includeSensitive = false)` | `FormDraft` | Takes a copy of the values the user has typed and of the current step, to keep them across a reload or a navigation: store `FormDraft.ToJson` and give the draft back to `FormController.RestoreDraft` on a controller built from the same initial input. |
| `GetArrayItemError(string arrayKey, int index, string childKey)` | `string?` | Gets the error to show for a cell of a repeatable list. |
| `GetArrayItems(string arrayKey)` | `IReadOnlyList<Dictionary<string, object?>>` | Gets the rows of a repeatable list. |
| `GetError(string path)` | `string?` | Gets the error to show for a field, or for a cell of a repeatable list by its `FormController.CellPath`. |
| `GetErrorEntries()` | `IReadOnlyList<FormErrorEntry>` | Lists the errors to show, in the order of the form: the errors of the fields in effect and of their cells, then the other errors set with `FormController.SetError`, form-level ones last. It is built from `FormController.Errors`, so errors of untouched fields are not listed. |
| `GetFieldOptions(FormFieldSchema field)` | `IReadOnlyList<FormOptionItem>` | Gets the options of a field: those from its options factory or its static options, followed by the options added with `FormController.CreateFieldOption`. |
| `GetInitialValue(string path)` | `object?` | Gets the value a field had in the initial snapshot. |
| `GetSectionStates()` | `IReadOnlyList<FormPageSectionState>` | Computes the completion state of each visible section from the current values and errors. Fields in hidden containers or tabs are not counted. |
| `GetValue(string path)` | `object?` | Gets the current value of a field. |
| `GetValue<T>(string path)` | `T` | Gets the current value converted to `T`. A value already of that type is returned as is. Otherwise it is converted with the invariant culture, in the formats the controls store: `T` may be a `Nullable`, a number (a whole number only becomes an integer type: 3.5 is not an `Int32`), `Boolean`, `Guid`, an enum (by name, ignoring case, or by the number of a member), `DateOnly` (`yyyy-MM-dd`), `TimeOnly` (`HH:mm`), `DateTime` (`yyyy-MM-ddTHH:mm`), `DateTimeOffset` (a value without a zone counts as UTC), `TimeSpan`, a string, or a list of strings. |
| `static IsContainer(FormFieldKind kind)` | `bool` | Tells whether a kind is a layout container whose children are fields of their own: `FormFieldKind.Group`, `FormFieldKind.Card`, `FormFieldKind.Object` and `FormFieldKind.Tabs`. A container never has a value, even without children. |
| `static IsEmptyValue(object? value)` | `bool` | Tells whether a value counts as empty for required checks: `null`, a blank string or an empty collection. `false` and `0` are not empty. |
| `IsFieldDisabled(FormFieldSchema field)` | `bool` | Tells whether a field is currently disabled, from `FormFieldSchema.Disabled` and `FormFieldSchema.DisabledWhen`. |
| `IsFieldRequired(FormFieldSchema field)` | `bool` | Tells whether a field is currently required, from `FormFieldSchema.Required` and `FormFieldSchema.RequiredWhen`. |
| `IsSectionVisible(FormPageSectionSchema section)` | `bool` | Tells whether a section is shown: its `FormPageSectionSchema.Condition` is absent or true. |
| `static IsStateless(FormFieldKind kind)` | `bool` | Tells whether a kind carries no value: `FormFieldKind.Info`, `FormFieldKind.Divider`, `FormFieldKind.Section` and `FormFieldKind.Button`. |
| `IsValidating(string path)` | `bool` | Tells whether an asynchronous rule of a field is waiting for its debounce or running, which the components show as a pending state next to the field. |
| `MarkArrayItemTouched(string arrayKey, int index, string childKey)` | `void` | Marks a cell of a repeatable list touched, which makes its validation error visible, and validates it unless the trigger of its child field is `FormValidationTrigger.Submit`. `KlyFormEngine` calls it when a cell loses focus. |
| `MarkTouched(string path)` | `void` | Marks a field touched, which makes its validation error visible, and validates it unless its trigger is `FormValidationTrigger.Submit`. |
| `NextStepAsync()` | `Task<bool>` | Moves to the next step that is not skipped. It validates the current step, then runs `FormSchema.OnBeforeNext`, and clears the touched state and validation errors of the new step. |
| `PreviousStepAsync()` | `Task<bool>` | Moves to the previous step that is not skipped, after `FormSchema.OnBeforePrevious`. Values are kept and the touched state and validation errors are cleared. |
| `RemoveArrayItem(string arrayKey, int index)` | `void` | Removes a row from a repeatable list, moving the errors and touched state of the following rows up, then revalidates the list if it was touched and calls its `FormFieldSchema.Watch`. Does nothing when the index is out of range. |
| `Reset(IReadOnlyDictionary<string, object?>? newInput = null)` | `void` | Clears errors and touched state, reloads the state from `newInput`, which also becomes the new dirty-tracking baseline, and returns to the first step that is not skipped. `FormFieldSchema.Watch` is not called. |
| `ResetField(string path)` | `void` | Restores a field to its initial value, clears its errors and touched state (and those of its cells for a repeatable list), and calls `FormFieldSchema.Watch` when the value changed. |
| `ResetSection(string sectionKey)` | `void` | Resets every field of a section with `FormController.ResetField`. Does nothing when the key matches no section. |
| `RestoreDraft(FormDraft draft)` | `void` | Puts the values of a draft back into the form, as if the user had typed them. The baseline of dirty tracking is not touched, so the restored fields differ from it: `FormController.IsDirty` and `FormController.DirtyPaths` report the draft as unsaved changes, which is what lets a navigation guard protect it. This is what separates it from `FormController.Reset`, which makes the loaded values the new baseline. Errors and touched state are cleared and `FormFieldSchema.Watch` is not called, like `FormController.Reset`. Keys of the draft that match no value of the form are ignored, and a field the draft does not mention keeps its value (the sensitive fields `FormController.ExportState` leaves out among them). |
| `SetArrayItemValue(string arrayKey, int index, string childKey, object? value, bool markTouched = false)` | `void` | Sets one cell of a repeatable list. Does nothing when the index is out of range. The cell follows the `FormFieldSchema.Trigger` of its child field, like `FormController.SetValue` does for a field: it is validated with the child's rules once touched, unless the trigger is `FormValidationTrigger.Submit`, which clears its error instead. Then the child's `FormFieldSchema.Watch` is called with the cell value and the list's with the rows. |
| `SetError(string path, string message, bool blocking = false)` | `void` | Sets an error on a field, for example one returned by the server, and marks the field touched. The error is removed by the next `FormController.SetValue` on that key, by `FormController.ClearError` or by `FormController.Reset`. |
| `SetFormError(string message, bool blocking = true)` | `void` | Sets an error on the form as a whole, for a failure that belongs to no field (a server rejection, a conflict). It is a `FormController.SetError` on `FormController.FormErrorKey`, so it shows in the error summary, and it clears with `FormController.ClearError` or `FormController.Reset`. |
| `SetValue(string path, object? value, bool markTouched = false)` | `void` | Sets a value, clears any error set with `FormController.SetError` on the key (and the errors and touched state of the cells when it replaces the rows of a repeatable list), validates the field if it has been touched and its trigger is not `FormValidationTrigger.Submit` (with that trigger, the validation error is cleared instead), calls `FormFieldSchema.Watch`, validates again the other touched fields that depend on this one (see `FormFieldSchema.DependsOn`) or show an error, and raises `Changed`. |
| `ShouldRenderField(FormFieldSchema field)` | `bool` | Tells whether a field is shown: it is not ignored and its `FormFieldSchema.Condition` is absent or true. It does not look at the containers, tab, section or step that hold the field; `FormController.ActiveFields` does. |
| `SubmitAsync()` | `Task<bool>` | Validates every field in effect, runs `FormSchema.OnBeforeSubmit` then `FormSchema.OnSubmit`, and on success takes a new initial snapshot so the form is no longer dirty. `FormController.IsSubmitting` is true meanwhile. Use `FormController.SubmitAsync` to run your own save step inside the same lifecycle. |
| `SubmitAsync(Func<FormController, CancellationToken, Task<bool>> handler, CancellationToken cancellationToken = null)` | `Task<bool>` | Submits like `FormController.SubmitAsync` and runs `handler`, your save step, after `FormSchema.OnSubmit`, while `FormController.IsSubmitting` is still `true`. The dirty snapshot is retaken, and the form stops being dirty, only when every step succeeded, so a handler that returns `false` or throws leaves the user's edits dirty and the form editable again. `FormController.IsSubmitting` goes back to `false` in every case, and a second call while one runs returns `false` at once. |
| `TryGetValue<T>(string path, out T value)` | `bool` | Tries to get the current value converted to `T`, with the conversions of `FormController.GetValue``. |
| `Validate(bool currentStepOnly = false)` | `bool` | Validates the fields in effect (see `FormController.ActiveFields`) according to `FormSchema.ValidateMode`, including each cell of their repeatable lists, and marks them touched so their errors show. Validation errors of fields that are not in effect, such as hidden ones, are removed. |
| `ValidateAsync(bool currentStepOnly = false, CancellationToken cancellationToken = null)` | `Task<bool>` | Validates like `FormController.Validate`, then runs the asynchronous rules (see `FormFieldSchema.AsyncValidators`) of the validated fields that have a non-empty value, pass their synchronous checks and have no result yet for the current value, in parallel across fields, and validates again with their results. `FormController.SubmitAsync` and `FormController.NextStepAsync` call it. |

#### `FormFieldSchema`

`Klyroo.Form` · class

Declarative definition of one field, or of a layout element such as a section heading, a container or a repeatable list. Fields are immutable; build them with object initializers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Accept` (init) | `string?` | The accepted files of a `FormFieldKind.File` field, in the syntax of the HTML `accept` attribute: comma-separated extensions (`.png`), content types (`image/png`) and wildcards (`image/*`). It is the `accept` attribute of the input, which only filters the picker, so validation checks it too (see `FormFileValue.Matches`). |
| `AddItemLabel` (init) | `string?` | Label of the add button of a repeatable list. The default is `Add item`. |
| `AsyncValidators` (init) | `IReadOnlyList<FormAsyncValidatorRule>?` | Asynchronous rules, run after the synchronous checks pass and the value is not empty. They run in the background after the field is touched and while it is edited (see `FormAsyncValidatorRule.Debounce`), and `FormController.SubmitAsync`, `FormController.NextStepAsync` and `FormController.ValidateAsync` wait for them. `FormController.IsValidating` tells whether one is running, which the components show next to the field. |
| `Clearable` (init) | `bool` | `true` to show a clear button once the field has a value, next to select lists, text-like inputs, textareas, passwords, number inputs and ratings. Clearing sets the value to `null`, or to an empty string for text-like inputs. |
| `Collapsible` (init) | `bool` | `true` to render a `FormFieldKind.Group`, `FormFieldKind.Card` or `FormFieldKind.Object` container as a disclosure (`details` and `summary`) the user can collapse, with `FormFieldSchema.Label` as its summary. Collapsing only hides the children: they are still validated and output. |
| `Condition` (init) | `Func<FormController, bool>?` | Returns `false` to hide the field. A hidden field is skipped by validation and left out of `FormController.BuildOutput`, but its value stays in the state. A hidden container hides its children too, with the same effect on validation and output. On a child of a repeatable list it hides that column: its cells are neither rendered nor validated, and the column is left out of the rows of the output, while its values stay in the state. |
| `CustomKey` (init) | `string?` | Names the renderer of a `FormFieldKind.Custom` field: the key it was registered under with `KlyrooFormOptions.CustomField`, or the key in the `FieldTemplates` parameter. Keys are case sensitive. A custom field without a registered renderer shows a notice instead of a control. |
| `DefaultCollapsed` (init) | `bool` | `true` to start a `FormFieldSchema.Collapsible` container collapsed. Ignored when `FormFieldSchema.Collapsible` is `false`. |
| `DefaultFactory` (init) | `Func<FormController, object?>?` | Computes the initial value from the controller. It takes precedence over `FormFieldSchema.DefaultValue` and also fills the cells of a new row of a repeatable list. |
| `DefaultValue` (init) | `object?` | Initial value used when the initial input has no value for the key. Ignored when `FormFieldSchema.DefaultFactory` is set. |
| `DependsOn` (init) | `IReadOnlyList<string>?` | Keys of the fields whose value this field's rules read, for example a confirmation field that compares itself with `password`. Once this field has been touched, a change of any of those fields validates it again, even when it shows no error yet, so a mismatch appears (and disappears) as soon as the other field changes. Without it, a touched field that currently shows an error is still validated again after any other field changes, which clears stale errors but cannot raise a new one. Ignored with `FormValidationTrigger.Submit`. On a child of a repeatable list it applies to the cells of that column. |
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
| `MaxFileSize` (init) | `long?` | The largest accepted size in bytes of each file of a `FormFieldKind.File` field, enforced on validation. `null` means no limit. |
| `MaxLength` (init) | `int?` | Largest accepted number of characters of a text value, enforced on validation. Also the `maxlength` attribute of text-like inputs, including textareas, passwords and one-time codes. |
| `Min` (init) | `double?` | Smallest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `min` attribute of number and slider inputs. |
| `Multiple` (init) | `bool` | `true` if a `FormFieldKind.File` field takes several files (its value is a `List<FormFileValue>`), or if a `FormFieldKind.Select` or `FormFieldKind.AutoComplete` field holds several values: it renders a list of checkboxes and the value is a `List<string>`. `FormController.CreateFieldOption` appends to that list. |
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

#### `FormFileValue`

`Klyroo.Form` · class

A file chosen in a `FormFieldKind.File` field, without any UI type: the name, size and content type the user's device reported, and a way to open the content. The value of a file field is a `FormFileValue` (or `null`), or a `List<FormFileValue>` with `FormFieldSchema.Multiple`. The controller checks `FormFieldSchema.Accept`, `FormFieldSchema.MaxFileSize` and `FormFieldSchema.Multiple` on it, so a test can build one by hand with `FormFileValue.FromBytes`.

| Constructor | Description |
| :--- | :--- |
| `FormFileValue(string name, long size, string contentType, Func<CancellationToken, ValueTask<Stream>> open, DateTimeOffset? lastModified = null)` | Creates a file value. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ContentType` | `string` | The content type, or an empty string when the device did not report one. |
| `LastModified` | `DateTimeOffset?` | When the file was last modified, if the device reported it. |
| `Name` | `string` | The file name, as reported by the device. |
| `Size` | `long` | The size in bytes, as reported by the device. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Equals(FormFileValue? other)` | `bool` | Two file values are equal when they have the same name, size, content type and modification time, which is how a field tells that the user chose the file that was already there. |
| `Equals(object? obj)` | `bool` |  |
| `static FormatSize(long bytes)` | `string` | Formats a size for a message, in the invariant culture: `512 B`, `1.5 KB`, `5 MB`. |
| `static FromBytes(string name, ReadOnlySpan<byte> content, string contentType = "application/octet-stream")` | `FormFileValue` | Builds a file value over bytes in memory, for tests and for files your code produces. |
| `GetHashCode()` | `int` |  |
| `Matches(string? accept)` | `bool` | Tells whether the file matches an accept list in the syntax of the HTML `accept` attribute: comma-separated extensions (`.png`), content types (`image/png`) and wildcards (`image/*`), compared ignoring case. |
| `OpenReadAsync(CancellationToken cancellationToken = null)` | `ValueTask<Stream>` | Opens a new stream over the content. Dispose it when done. |
| `ToString()` | `string` |  |

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
| `Messages` (init) | `FormMessages?` | The built-in validation messages of this form. When `null` (the default) the controller uses the English messages, or those of the current locale when a Klyroo.Form component renders it. Set it to fix the language of the form whatever the locale is. |
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

#### `FormAsyncValidatorRule`

`Klyroo.Form` · record

An asynchronous validation rule of a field, for a check that needs I/O such as "is this username free". It runs in the background once the user has left the field (and again, debounced, as they edit it), a newer run cancels an older one, and `FormController.SubmitAsync` and `FormController.NextStepAsync` wait for it. It does not run for an empty value or for a value that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list.

| Constructor | Description |
| :--- | :--- |
| `FormAsyncValidatorRule(string Key, Func<object?, FormController, CancellationToken, ValueTask<string?>> Validate)` | An asynchronous validation rule of a field, for a check that needs I/O such as "is this username free". It runs in the background once the user has left the field (and again, debounced, as they edit it), a newer run cancels an older one, and `FormController.SubmitAsync` and `FormController.NextStepAsync` wait for it. It does not run for an empty value or for a value that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Debounce` (init) | `TimeSpan` | How long the value must stay unchanged before a background run starts, so typing does not call the service on every key. The default is 300 milliseconds. Submission runs the rule at once. |
| `Key` (init) | `string` | Identifier of the rule. |
| `Validate` (init) | `Func<object?, FormController, CancellationToken, ValueTask<string?>>` | Receives the current field value, the controller and a token that is cancelled when the value changes or a newer run starts. Returns the error message, or `null` when the value is valid. An exception it throws in a background run is swallowed and the value stays unchecked until submission, where the exception propagates to the caller. |

#### `FormDraft`

`Klyroo.Form` · record

A snapshot of what the user has typed, produced by `FormController.ExportState` and loaded back with `FormController.RestoreDraft`, for example to keep a long form across a page reload. It holds values only: the baseline that dirty tracking compares against stays with the controller. `FormDraft.ToJson` and `FormDraft.FromJson` write and read it without reflection, so it works under NativeAOT and trimming.

| Constructor | Description |
| :--- | :--- |
| `FormDraft(IReadOnlyDictionary<string, object?> Values, int StepIndex = 0)` | A snapshot of what the user has typed, produced by `FormController.ExportState` and loaded back with `FormController.RestoreDraft`, for example to keep a long form across a page reload. It holds values only: the baseline that dirty tracking compares against stays with the controller. `FormDraft.ToJson` and `FormDraft.FromJson` write and read it without reflection, so it works under NativeAOT and trimming. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `StepIndex` (init) | `int` | Zero-based index of the step the user was on. 0 for a form without steps. |
| `Values` (init) | `IReadOnlyDictionary<string, object?>` | The values by key, in the CLR types the controls use (strings, `Double`, `Boolean`, `List<string>`, rows as `List<Dictionary<string, object?>>`). |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static FromJson(string json)` | `FormDraft` | Reads a draft written by `FormDraft.ToJson`. Numbers come back as `Double`; `FormController.RestoreDraft` converts each value to the type its field uses. |
| `ToJson()` | `string` | Writes the draft as JSON. Values of other types than the ones of `FormDraft.Values` are written as text. |

#### `FormErrorEntry`

`Klyroo.Form` · record

One error to show, as listed by `FormController.GetErrorEntries`: a field, a cell of a repeatable list or the form as a whole.

| Constructor | Description |
| :--- | :--- |
| `FormErrorEntry(string Path, string Message, string? FieldKey, int? RowIndex, string? ChildKey)` | One error to show, as listed by `FormController.GetErrorEntries`: a field, a cell of a repeatable list or the form as a whole. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ChildKey` (init) | `string?` | Key of the child field of the cell, or `null` when the error is not on a cell. |
| `FieldKey` (init) | `string?` | Key of the field that holds the error: the field itself, or the repeatable list for a cell. `null` for a form-level error. |
| `IsFormLevel` | `bool` | `true` when the error belongs to the form as a whole and not to a field: its key matches no field of the schema. |
| `Message` (init) | `string` | The error message. |
| `Path` (init) | `string` | The key in `FormController.Errors`: a field key, a `FormController.CellPath` or the key given to `FormController.SetError`. |
| `RowIndex` (init) | `int?` | Zero-based row of the cell, or `null` when the error is not on a cell. |

#### `FormMessages`

`Klyroo.Form` · record

The built-in validation messages of `FormController`, which has no UI and so no locale of its own: the required, number bounds, text length, date range and file errors. A new instance holds the English messages. Build a variant with a `with` expression, give it to the controller with `FormSchema.Messages` or `FormController.Messages`, or pick one of the twelve built-in sets (`FormMessageLocales`). The Blazor components do it for you: they give the controller the messages of the current Klyroo.UI locale, see `FormTexts`.

| Constructor | Description |
| :--- | :--- |
| `FormMessages()` | Creates the messages with their English defaults. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `DateRangeOrder` (init) | `string` | The error of a `FormFieldKind.DateRange` whose end is before its start (`"The end date must not be before the start date"`). |
| `static Default` | `FormMessages` | The English messages, shared and read-only. |
| `FileTooLarge` (init) | `Func<string, string, string>` | The error of a `FormFieldKind.File` file above `FormFieldSchema.MaxFileSize`, given the file name and the limit as text (`"report.pdf is larger than 5 MB"`). |
| `FileTypeNotAllowed` (init) | `Func<string, string, string>` | The error of a `FormFieldKind.File` file that does not match `FormFieldSchema.Accept`, given the file name and the accepted list (`"photo.gif is not an accepted type (.png,.jpg)"`). |
| `MaxLength` (init) | `Func<int, string>` | The error of a text longer than `FormFieldSchema.MaxLength`, given the limit (`"Must be at most 20 characters"`). |
| `MaxValue` (init) | `Func<double, string>` | The error of a `FormFieldKind.Number` above `FormFieldSchema.Max`, given the bound (`"Value must be at most 10"`). |
| `MinValue` (init) | `Func<double, string>` | The error of a `FormFieldKind.Number` below `FormFieldSchema.Min`, given the bound (`"Value must be at least 3"`). |
| `Required` (init) | `string` | The error of a required field that has no value (`"This field is required"`). `FormFieldSchema.RequiredMessage` wins over it. |
| `SingleFileOnly` (init) | `string` | The error of a `FormFieldKind.File` field without `FormFieldSchema.Multiple` that holds several files (`"Choose one file only"`). |

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
| `Custom` | A control you provide. `FormFieldSchema.CustomKey` names its renderer, a `RenderFragment<FormControlContext>` registered with `KlyrooFormOptions.CustomField` or passed in the `FieldTemplates` parameter of the components. The field is stateful like any input: it holds any value you store, is validated with `FormFieldSchema.Required` and `FormFieldSchema.Validators`, and is part of the output. The components render its label, description, help and error around your control. |
| `File` | File input rendered with the Blazor `InputFile`. The value is a `FormFileValue` (or `null`), or a `List<FormFileValue>` with `FormFieldSchema.Multiple`. `FormFieldSchema.Accept`, `FormFieldSchema.MaxFileSize` and the number of files are checked on validation. A file is never part of a draft (`FormController.ExportState`), since its content cannot be restored. |

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

Components (6) · Static classes and extensions (2) · Classes (1) · Records (1)

### Components

#### `KlyFormEngine`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a `FormSchema` as a form: header, optional stepper with the description of the current step, a field grid of `FormSchema.Columns` columns and the footer actions (Back, Reset, Cancel, Next, Submit). Pass either a `FormController` through `Form` or a schema through `Schema`. The component subscribes to `Changed` and re-renders on every change.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `FieldTemplates` | `IReadOnlyDictionary<string, RenderFragment<FormControlContext>>?` | Renderers of the `FormFieldKind.Custom` fields by `FormFieldSchema.CustomKey`, each a `RenderFragment<FormControlContext>` that draws the control. They win over the ones registered with `KlyrooFormOptions.CustomField`. |
| `Form` | `FormController?` | The controller to render. Use it when you need to read or change the form from code. Takes precedence over `KlyFormEngine.Schema`. |
| `Input` | `IReadOnlyDictionary<string, object?>?` | Initial values by field key for the controller created from `KlyFormEngine.Schema`. Ignored when `KlyFormEngine.Form` is set. |
| `OnCancel` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with the current output when the user presses Cancel or the close button. When bound, the Cancel button is shown. |
| `OnSave` | `Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>>?` | Your save step: called with `FormController.BuildOutput` after validation and the schema hooks passed, while the form is still submitting (the button shows its saving state, a second submit is refused, Cancel is disabled). Return `true` when the save succeeded; the form is then clean and `KlyFormEngine.OnSubmit` is raised. Return `false` to keep the user's edits dirty and the form open, after reporting the reason with `FormController.SetError` or `FormController.SetFormError`. The token is cancelled when the component is disposed. It runs through `FormController.SubmitAsync`. |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with `FormController.BuildOutput` after the form is submitted successfully, that is after validation, the schema hooks and `KlyFormEngine.OnSave` passed. At that point the submission is over: `FormController.IsSubmitting` is `false` and the form is clean. Do the save itself in `KlyFormEngine.OnSave`. |
| `Schema` | `FormSchema?` | The schema to render when no `KlyFormEngine.Form` is given. The component creates and owns the controller, and creates a new one when the schema instance changes. |
| `Shell` | `FormShellMode` | How the form is presented. Anything other than `FormShellMode.Inline` adds a close button to the header when `KlyFormEngine.OnCancel` is bound. |
| `ShowReset` | `bool` | Shows a reset button in the footer while the form is dirty. The default is `true`. |
| `SubmitLabel` | `string?` | Text of the submit button. The default is the `Submit` text of the current locale (`FormTexts.Submit`). |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `EffectiveController` | `FormController?` | The controller in use: `KlyFormEngine.Form` if set, otherwise the one the component created from `KlyFormEngine.Schema`. |

#### `KlyFormErrorSummary`

`Klyroo.Form` · component · inherits `KlyComponentBase`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Form` | `FormController` | The controller whose errors to list. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormNavigationGuard`

`Klyroo.Form` · component · inherits `KlyComponentBase`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ConfirmExternalNavigation` | `bool` | Asks the browser to confirm leaving the site, reloading or closing the tab while the form is dirty. The browser shows its own text; it cannot be changed. The default is `true`. |
| `ConfirmLeave` | `Func<LocationChangingContext, Task<bool>>?` | Called when the user navigates inside the app while the form is dirty. Return `true` to let the navigation go ahead and `false` to stay, for example after asking in a `KlyModal`. Without it the guard asks with `window.confirm` and `KlyFormNavigationGuard.Message`. |
| `Disabled` | `bool` | Turns the guard off, for example while the page saves and navigates on its own. The default is `false`. |
| `Form` | `FormController` | The controller to protect. Navigation is blocked while `FormController.IsDirty` is `true`, so the guard stops once a submission succeeds and the snapshot is retaken. |
| `Message` | `string?` | The question of the default confirmation. The default is the `UnsavedChanges` text of the current locale (`FormTexts.UnsavedChanges`). |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormPage`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a sectioned form as a page: a sidebar listing each section with its completion status, and one card per section laid out by `FormPageSectionSchema.Span`. Fields render with the same controls, labels, containers and repeatable lists as `KlyFormEngine`, and the page is a `form` element, so pressing Enter in a field submits it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `FieldTemplates` | `IReadOnlyDictionary<string, RenderFragment<FormControlContext>>?` | Renderers of the `FormFieldKind.Custom` fields by `FormFieldSchema.CustomKey`, each a `RenderFragment<FormControlContext>` that draws the control. They win over the ones registered with `KlyrooFormOptions.CustomField`. |
| `Form` | `FormController` | The controller to render. Its schema must declare `FormSchema.Sections`. |
| `OnSave` | `Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>>?` | Your save step: called with `FormController.BuildOutput` after validation and the schema hooks passed, while the form is still submitting. Return `true` when the save succeeded; the form is then clean and `KlyFormPage.OnSubmit` is raised. Return `false` to keep the edits dirty, after reporting the reason with `FormController.SetError` or `FormController.SetFormError`. The token is cancelled when the component is disposed. |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with `FormController.BuildOutput` after the form is submitted successfully, that is after validation, the schema hooks and `KlyFormPage.OnSave` passed. The submission is over at that point and the form is clean. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormProvider`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Host of the overlay forms opened with `FormService`. Mount one instance in a layout, wrapping the page content, and call `FormService.CreateFormAsync` from anywhere in the same scope.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The content of the provider, usually the page body. Overlay forms render after it. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlySchemaField`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders one field of a `FormController` with the control of its `FormFieldKind`, its label, description, hint, help, error and per-field reset, the way `KlyFormEngine` and `KlyFormPage` render their fields. Containers, tabs and repeatable lists render their children too. Place it in a CSS grid of `FormSchema.Columns` columns, since the field sets its own `grid-column` span. It renders nothing when `FormController.ShouldRenderField` is `false`. It does not subscribe to `Changed`: render it from a component that re-renders on that event, so changes made elsewhere (by code or by another field) show up.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Field` | `FormFieldSchema` | The field to render, usually one of `FormController.CurrentVisibleTopFields`. Containers render their children with nested `KlySchemaField` components. |
| `FieldTemplates` | `IReadOnlyDictionary<string, RenderFragment<FormControlContext>>?` | Renderers of the `FormFieldKind.Custom` fields by `FormFieldSchema.CustomKey`. They win over the ones registered with `KlyrooFormOptions.CustomField`, and reach the fields inside containers, tabs and lists. |
| `Form` | `FormController` | The controller that holds the value of the field. |

Also accepts the parameters of `KlyComponentBase`.

### Static classes and extensions

#### `FormLocales`

`Klyroo.Form` · service

The built-in texts of Klyroo.Form, one set per locale of `KlyLocales.All` and keyed by the same codes. A component picks the set of its current `KlyLocale.Code` with `KlyLocales.ResolveCode`, so "pt-PT" gets "pt" and an unknown language gets English. Change a set with `KlyrooFormOptions.Texts`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `static Ar` | `FormTexts` | العربية (ar). Needs review. |
| `static De` | `FormTexts` | Deutsch (de). Needs review. |
| `static En` | `FormTexts` | English. |
| `static Es` | `FormTexts` | Español (es). Needs review. |
| `static Fr` | `FormTexts` | Français (fr). Reviewed. |
| `static It` | `FormTexts` | Italiano (it). Needs review. |
| `static Ja` | `FormTexts` | 日本語 (ja). Needs review. |
| `static Nl` | `FormTexts` | Nederlands (nl). Needs review. |
| `static Pt` | `FormTexts` | Português (pt). Needs review. |
| `static PtBr` | `FormTexts` | Português (Brasil) (pt-BR). Needs review. |
| `static ReviewedCodes` | `IReadOnlyList<string>` | The codes whose texts were reviewed by a native speaker. The other sets are drafts waiting for review. |
| `static Ru` | `FormTexts` | Русский (ru). Needs review. |
| `static Tables` | `IReadOnlyDictionary<string, FormTexts>` | Every set by locale code: the codes of `KlyLocales.All`. |
| `static ZhCn` | `FormTexts` | 简体中文 (zh-Hans). Needs review. |

#### `KlyrooFormServiceCollectionExtensions`

`Klyroo.Form` · service

Dependency injection registration for Klyroo.Form.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static AddKlyrooForm(IServiceCollection services, Action<KlyrooFormOptions>? configure = null)` | `IServiceCollection` | Registers `FormService` as a scoped service and calls `AddKlyrooUi()`, because the components build on Klyroo.UI. In a Blazor Web App with interactive WebAssembly or Auto, call it in the server project and in the client project. |

### Classes

#### `KlyrooFormOptions`

`Klyroo.Form` · class

Configuration of Klyroo.Form: the built-in texts of each locale. It is an extension of `KlyrooOptions` (`KlyrooOptions.Extension``). Configure it with `services.AddKlyrooForm(f => ...)`, in code shared by the server and the WebAssembly client.

| Constructor | Description |
| :--- | :--- |
| `KlyrooFormOptions()` | Creates the options with the built-in texts of `FormLocales`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `CustomField(string key, RenderFragment<FormControlContext> renderer)` | `KlyrooFormOptions` | Registers the renderer of `FormFieldKind.Custom` fields whose `FormFieldSchema.CustomKey` is `key`, for every form of the app. The renderer draws the control from a `FormControlContext`; the components draw the label, description, help and error around it. The `FieldTemplates` parameter of a component wins over it for that component. Registering a key again replaces its renderer. |
| `Texts(string localeCode, Func<FormTexts, FormTexts> change)` | `KlyrooFormOptions` | Changes the texts of a locale, like `KlyLocales.Extend` does for Klyroo.UI: `change` receives the set the locale resolves to and returns the one to use, usually `t => t with { ... }`. `localeCode` is a code of `KlyLocales.All` ("fr"), or the code of a locale you added ("en-AU", "tlh"): a locale without a set of its own starts from the set `KlyLocales.ResolveCode` picks for it, then its changes apply. The built-in validation messages are part of the set (`FormTexts.Messages`). The texts become read-only the first time a component resolves one. |

### Records

#### `FormTexts`

`Klyroo.Form` · record

The built-in texts of `KlyFormEngine`, `KlyFormPage`, `KlyFormProvider`, `KlySchemaField`, `KlyFormErrorSummary` and `KlyFormNavigationGuard`: button labels, accessible names, empty states, counters and the built-in validation messages. There is one set per locale of Klyroo.UI (`FormLocales`), and a component shows the set of its current locale, the one its `Messages` come from. Change a set with `KlyrooFormOptions.Texts` in `AddKlyrooForm(f => ...)`.

| Constructor | Description |
| :--- | :--- |
| `FormTexts()` | Creates the texts with their English defaults. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `AddItem` (init) | `string` | The add button of a repeatable list without an `AddItemLabel` (`"Add item"`). |
| `AllReady` (init) | `string` | The summary of `KlyFormPage` when no section is pending or invalid (`"Everything is ready."`). |
| `Back` (init) | `string` | The Back button of a stepped form (`"Back"`). |
| `Cancel` (init) | `string` | The Cancel button of a form (`"Cancel"`). |
| `CellError` (init) | `Func<string, string, int, string, string>` | An entry of the error summary for a cell of a repeatable list, given the column label, the list label, the one-based row and the message (`"SKU (Lines, 2): This field is required"`). |
| `Checking` (init) | `string` | The pending state of a field being validated asynchronously (`"Checking…"`). |
| `ChooseFile` (init) | `string` | The button of a file field that takes one file (`"Choose file"`). |
| `ChooseFiles` (init) | `string` | The button of a file field with `Multiple` (`"Choose files"`). |
| `Clear` (init) | `string` | The accessible name of the clear button of a control (`"Clear"`). |
| `CloseForm` (init) | `string` | The accessible name of the close button of an overlay form (`"Close form"`). |
| `Create` (init) | `string` | The button that creates an option (`"Create"`). |
| `EndDate` (init) | `string` | The accessible name of the second input of a date range (`"End date"`). |
| `ErrorsToFix` (init) | `Func<int, string>` | The title of the error summary, given the number of errors (`"There are 2 errors to fix"`). |
| `FieldError` (init) | `Func<string, string, string>` | An entry of the error summary, given the field label and the message (`"Email: This field is required"`). |
| `FormSections` (init) | `string` | The heading and the accessible name of the section list of `KlyFormPage` (`"Form sections"`). |
| `FormTitle` (init) | `string` | The accessible name of an overlay form without a title (`"Form"`). |
| `Hide` (init) | `string` | The visible text of the button that hides a password (`"Hide"`). |
| `HidePassword` (init) | `string` | The accessible name of the button that hides a revealed password (`"Hide password"`). |
| `LeftToComplete` (init) | `Func<int, string>` | The summary of `KlyFormPage`, given the number of sections left (`"2 left to complete."`). |
| `Messages` (init) | `FormMessages` | The built-in validation messages the components give to the controller they render (see `FormController.Messages`), unless the schema or the code chose its own. |
| `NewOption` (init) | `string` | The placeholder of the input that creates an option (`"New option"`). |
| `NewOptionFor` (init) | `Func<string, string>` | The accessible name of that input, given the field label (`"New option for Country"`). |
| `Next` (init) | `string` | The Next button of a stepped form (`"Next"`). |
| `NoFileChosen` (init) | `string` | The text of a file field without a file (`"No file chosen"`). |
| `NoItemsYet` (init) | `string` | The text of an empty repeatable list (`"No items yet"`). |
| `PasswordRequirements` (init) | `string` | The accessible name of the password checklist (`"Password requirements"`). |
| `RatingStar` (init) | `Func<int, int, string>` | The accessible name of a star, given its number and the number of stars (`"3 of 5"`). |
| `RemoveFile` (init) | `Func<string, string>` | The accessible name of the button that removes a file, given its name (`"Remove report.pdf"`). |
| `RemoveItem` (init) | `Func<int, string>` | The accessible name of the button that removes a row, given its one-based number (`"Remove item 2"`). |
| `RemoveTag` (init) | `Func<string, string>` | The accessible name of the button that removes a tag, given the tag (`"Remove news"`). |
| `RequirementMet` (init) | `string` | The hidden marker after a satisfied password requirement (`"(met)"`). |
| `RequirementNotMet` (init) | `string` | The hidden marker after an unsatisfied password requirement (`"(not met)"`). |
| `Reset` (init) | `string` | The Reset button of a form, of a section and of a field (`"Reset"`). |
| `Saving` (init) | `string` | The submit button while the form is submitting (`"Saving…"`). |
| `SectionComplete` (init) | `string` | The badge of a complete section (`"Completed"`). |
| `SectionInvalid` (init) | `string` | The badge of a section with an error (`"To fix"`). |
| `SectionOptional` (init) | `string` | The badge of an optional, empty section (`"Optional"`). |
| `SectionProgress` (init) | `Func<int, int, string>` | The badge of a pending section, given the required fields done and the total (`"1 of 3"`). |
| `SelectPlaceholder` (init) | `string` | The empty option of a select list without a `Placeholder` (`"Select…"`). |
| `Show` (init) | `string` | The visible text of the button that reveals a password (`"Show"`). |
| `ShowPassword` (init) | `string` | The accessible name of the button that reveals a password (`"Show password"`). |
| `StartDate` (init) | `string` | The accessible name of the first input of a date range (`"Start date"`). |
| `Submit` (init) | `string` | The submit button when `SubmitLabel` is not set (`"Submit"`). |
| `TagPlaceholder` (init) | `string` | The placeholder of a tag input without a `Placeholder` (`"Add a tag and press Enter"`). |
| `Tags` (init) | `string` | The accessible name of the tag list of a field without a label (`"Tags"`). |
| `UnsavedChanges` (init) | `string` | The question of the default confirmation of `KlyFormNavigationGuard` (`"You have unsaved changes. Leave this page and discard them?"`). |
