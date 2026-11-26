# Klyroo.Form API reference

> Generated from the compiled assemblies and their XML documentation. Do not edit by hand.

## Klyroo.Form.Core

Static classes and extensions (3) · Classes (27) · Records (9) · Structs (2) · Interfaces (2) · Enums (7) · Delegates (1)

### Static classes and extensions

#### `FormFieldBuilderExtensions`

`Klyroo.Form` · service

The settings of a typed field that only fit some kinds of value. Each method is constrained on the type of the value, so declaring a text area for a number, or a range for a string, does not compile.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Accept<TModel>(FormFieldBuilder<TModel, FormFileValue?> field, string accept)` | `FormFieldBuilder<TModel, FormFileValue?>` | Sets the accepted files of a file field, in the syntax of the HTML `accept` attribute: extensions (`.png`), content types (`image/png`) and wildcards (`image/*`). Validation checks it too. |
| `static Accept<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, string accept)` | `FormFieldBuilder<TModel, TValue>` | Sets the accepted files of a field that takes several files. See `FormFieldBuilderExtensions.Accept``. |
| `static AsAutoComplete<TModel, TValue>(FormFieldBuilder<TModel, TValue> field)` | `FormFieldBuilder<TModel, TValue>` | Renders a choice field as a text input that suggests the option labels. |
| `static AsColor<TModel, TString>(FormFieldBuilder<TModel, TString> field)` | `FormFieldBuilder<TModel, TString>` | Renders a text field as a color input. The value is `#rrggbb`. |
| `static AsOneTimeCode<TModel, TString>(FormFieldBuilder<TModel, TString> field, int length = 6)` | `FormFieldBuilder<TModel, TString>` | Renders a text field as a one-time code input. |
| `static AsPassword<TModel, TString>(FormFieldBuilder<TModel, TString> field, params FormPasswordRequirement[] requirements)` | `FormFieldBuilder<TModel, TString>` | Renders a text field as a masked password input with a show/hide toggle. |
| `static AsPhone<TModel, TString>(FormFieldBuilder<TModel, TString> field)` | `FormFieldBuilder<TModel, TString>` | Renders a text field as a phone number input. The format is not validated. |
| `static AsRadio<TModel, TValue>(FormFieldBuilder<TModel, TValue> field)` | `FormFieldBuilder<TModel, TValue>` | Renders a choice field as a radio group. |
| `static AsRating<TModel>(FormFieldBuilder<TModel, int> field, int stars = 5)` | `FormFieldBuilder<TModel, int>` | Renders a whole number as a star rating from 1 to `stars`. |
| `static AsRating<TModel>(FormFieldBuilder<TModel, int?> field, int stars = 5)` | `FormFieldBuilder<TModel, int?>` | Renders an optional whole number as a star rating from 1 to `stars`. |
| `static AsSlider<TModel, TValue>(FormFieldBuilder<TModel, TValue> field)` | `FormFieldBuilder<TModel, TValue>` | Renders a number as a range slider, bounded by `FormFieldBuilderExtensions.Range``. |
| `static AsSlider<TModel, TValue>(FormFieldBuilder<TModel, TValue?> field)` | `FormFieldBuilder<TModel, TValue?>` | Renders an optional number as a range slider. The slider shows its minimum while the value is empty. |
| `static AsSwitch<TModel>(FormFieldBuilder<TModel, bool> field)` | `FormFieldBuilder<TModel, bool>` | Renders a boolean as a switch instead of a checkbox. |
| `static AsSwitch<TModel>(FormFieldBuilder<TModel, bool?> field)` | `FormFieldBuilder<TModel, bool?>` | Renders an optional boolean as a switch instead of a checkbox. |
| `static AsTextarea<TModel, TString>(FormFieldBuilder<TModel, TString> field)` | `FormFieldBuilder<TModel, TString>` | Renders a text field as a multi-line text area. |
| `static MaxFileSize<TModel>(FormFieldBuilder<TModel, FormFileValue?> field, long bytes)` | `FormFieldBuilder<TModel, FormFileValue?>` | Sets the largest accepted size in bytes of each file of a file field. Files are read into memory when chosen, so this is also the most memory one file takes. |
| `static MaxFileSize<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, long bytes)` | `FormFieldBuilder<TModel, TValue>` | Sets the largest accepted size in bytes of each file of a field that takes several files. |
| `static MaxLength<TModel, TString>(FormFieldBuilder<TModel, TString> field, int maxLength)` | `FormFieldBuilder<TModel, TString>` | Limits the number of characters of a text value. It is checked on validation and is the `maxlength` of the input. |
| `static OnCreateOption<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, Func<string, FormOption<TValue>?> create)` | `FormFieldBuilder<TModel, TValue>` | Turns text typed by the user into a new option, which the components offer as a create button under the control. |
| `static Options<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, IEnumerable<FormOption<TValue>> options)` | `FormFieldBuilder<TModel, TValue>` | Gives a field its options and renders it as a select list, unless `AsRadio` or `AsAutoComplete` chose another control. The values are stored in the model as `TValue`; the control only sees their invariant text. |
| `static Options<TModel, TValue>(FormFieldBuilder<TModel, TValue?> field, IEnumerable<FormOption<TValue>> options)` | `FormFieldBuilder<TModel, TValue?>` | Gives an optional field its options. See `FormFieldBuilderExtensions.Options``. |
| `static Options<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, Func<FormController<TModel>, IReadOnlyList<FormOption<TValue>>> options)` | `FormFieldBuilder<TModel, TValue>` | Computes the options from the controller, for example to depend on another field. The options are computed again on every render. |
| `static Options<TModel, TValue>(FormFieldBuilder<TModel, TValue?> field, Func<FormController<TModel>, IReadOnlyList<FormOption<TValue>>> options)` | `FormFieldBuilder<TModel, TValue?>` | Computes the options of an optional field from the controller. See `FormFieldBuilderExtensions.Options``. |
| `static Range<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, TValue min, TValue max)` | `FormFieldBuilder<TModel, TValue>` | Bounds a number. The bounds are checked on validation, and are the `min` and `max` of the input. The control compares them as `Double`, and the type of the member adds its own exact limits. |
| `static Range<TModel, TValue>(FormFieldBuilder<TModel, TValue?> field, TValue min, TValue max)` | `FormFieldBuilder<TModel, TValue?>` | Bounds an optional number. See `FormFieldBuilderExtensions.Range``. |
| `static StepBy<TModel, TValue>(FormFieldBuilder<TModel, TValue> field, TValue step)` | `FormFieldBuilder<TModel, TValue>` | Sets the `step` of a number input. |
| `static StepBy<TModel, TValue>(FormFieldBuilder<TModel, TValue?> field, TValue step)` | `FormFieldBuilder<TModel, TValue?>` | Sets the `step` of an optional number input. |

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

#### `FormSchema`

`Klyroo.Form` · service

Entry point of the typed forms: declares a `FormSchema` from a C# model type.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static For<TModel>(Action<FormSchemaBuilder<TModel>> configure)` | `FormSchema<TModel>` | Declares a form from the members of `TModel`. |

### Classes

#### `FormControlContext`

`Klyroo.Form` · class

What a custom control of a typed form needs besides its value: the error, the state flags and the ids that tie the control to its label, description, help and error, and the string boundary for raw inputs (`FormControlContext.Text` and `FormControlContext.SetText`). The value itself, typed as the member of the model, is on `FormControlContext`, which is what a renderer receives. A context is a snapshot of one render: the components build a new one after the form changes.

| Property | Type | Description |
| :--- | :--- | :--- |
| `DescribedBy` | `string?` | The ids of the description, help and error elements to put in `aria-describedby`, space separated, or `null`. |
| `Disabled` | `bool` | `true` when the field is disabled, which includes every field while a submission runs. Render the control disabled. |
| `Error` | `string?` | The error to show, or `null`. The error text itself is rendered by the component around the control. |
| `ErrorId` | `string` | The id of the error element. It exists only while `FormControlContext.Error` is not `null`. |
| `Id` | `string` | The `id` the control must use, so that the label points at it. The ids of the description, help and error elements are this id followed by `-description`, `-help` and `-error`. |
| `LabelledBy` | `string?` | The ids of the elements that label a control made of several inputs (`aria-labelledby`), or `null` when a `label` element points at `FormControlContext.Id`. |
| `Required` | `bool` | `true` when the field is required. Set `aria-required` on the control. |
| `Text` | `string?` | The current value as the text an input shows: the text the user typed when it is not a value yet (`abc` in a number field), else the value in invariant form (dates as `yyyy-MM-dd`, lists comma separated). `null` when the field is empty. |
| `Validating` | `bool` | `true` while an asynchronous rule of the field runs. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Blur()` | `void` | Tells the form the control lost focus: the field is marked touched and validated, which is what makes its error show. |
| `SetText(string? text)` | `void` | Changes the value from the text of an input, while the user is editing. Blank text empties the field. Text that is a value of the type becomes that value; text that is not stays as it is, so the field shows its error instead of a zero. |

#### `FormControlContext<TValue>`

`Klyroo.Form` · class · inherits `FormControlContext`

Everything a custom control needs to edit one field of a typed form: the value as the type of the member, and how to change it. A renderer is a `RenderFragment<FormControlContext<TValue>>`, given in a `KlyFieldTemplate` for one field or registered for every form with `KlyrooFormOptions.CustomField<TValue>`. A control that keeps the value as text (a raw HTML input) reads `FormControlContext.Text` and calls `FormControlContext.SetText`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Value` | `TValue` | The value when this context was built, or the default of the type while the field is empty or its text is not a value yet (`0` for an `int`, `null` for a string). Use `FormControlContext.Current` for the live value inside an event handler. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Commit(TValue value)` | `void` | Changes the value and marks the field touched, for a change that is complete in one gesture: a click, a selection, a removal. The error shows at once. |
| `Current()` | `TValue` | Gets the live value, which may be newer than `FormControlContext.Value`. |
| `Set(TValue value)` | `void` | Changes the value while the user is editing, for example on every keystroke. The field is validated only once touched, and per its trigger. |
| `SetText(string? text)` | `void` | Changes the value from the text of an input, while the user is editing. Blank text empties the field. Text that is a value of the type becomes that value; text that is not stays as it is, so the field shows its error instead of a zero. |

#### `FormController<TModel>`

`Klyroo.Form` · class

Runtime state of a `FormSchema`: the values of the fields, a baseline for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. Values are read and written through the handles the schema gave (`Get`, `Set`), typed as the members of the model, and move to and from the model with `FormController.CreateModel` and `FormController.ApplyTo`. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time.

| Constructor | Description |
| :--- | :--- |
| `FormController<TModel>(FormSchema<TModel> schema, TModel model = null)` | Creates a controller for `schema`, with the values of `model` as its baseline. Without a model the form starts from the default of each field (the `Default` of its declaration, else the default of the type). |

| Property | Type | Description |
| :--- | :--- | :--- |
| `CurrentStepIndex` | `int` | Zero-based index of the current step. It stays 0 for a form without steps. |
| `CurrentStepKey` | `string?` | The key of the current step, or `null` for a form without steps. |
| `Errors` | `IReadOnlyDictionary<string, string>` | The errors to show, by field key: validation errors of touched fields and cells, plus every error set with `FormController.SetError`. |
| `Fields` | `IReadOnlyList<IFormFieldAccess>` | The fields of the form as `IFormFieldAccess` views, for an agent or any code that works with values as JSON. |
| `FormErrors` | `IReadOnlyList<string>` | The messages of the errors that belong to the form as a whole. |
| `FormKey` | `string` | The identifier of the form. |
| `HasBlockingErrors` | `bool` | `true` if a field in effect has a validation error, touched or not, or if an error set with `FormController.SetError` is blocking. |
| `HasPendingValidation` | `bool` | `true` while any asynchronous rule is waiting for its debounce or running. |
| `IsDirty` | `bool` | `true` when a value differs from the baseline taken at creation, by `FormController.Reset` or after a successful submission. |
| `IsFirstStep` | `bool` | `true` when every step before the current one is skipped, and for a form without steps. |
| `IsLastStep` | `bool` | `true` when every step after the current one is skipped, and for a form without steps. |
| `IsStepped` | `bool` | `true` when the schema declares steps. |
| `IsSubmitting` | `bool` | `true` while a submission runs. |
| `Messages` (get, set) | `FormMessages` | The built-in validation messages in use: the ones set here, else the ones of the schema, else the ones a Klyroo.Form component gave for its locale, else English. Setting it fixes the language of this controller. |
| `Schema` | `FormSchema<TModel>` | The schema this controller runs. |
| `SubmitFailed` | `bool` | `true` after a submission or a move to the next step failed, until a submission succeeds, the step changes or `FormController.Reset` runs. |
| `SubmitFailureCount` | `int` | How many times a submission or a move to the next step failed since the controller was created or last reset. |
| `Title` | `string?` | The title of the form, or `null`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddRow<TItem>(FormListField<TModel, TItem> list, TItem seed = null)` | `void` | Appends a row to a repeatable list. Each cell starts from the default of its column, or from `seed` when one is given. |
| `ApplyTo(TModel target)` | `TModel` | Writes the values of the fields in effect into `target`. It is all or nothing: every value is converted and every object on the way is checked before the first member is set, and a setter that throws puts back the members already set. A member that nothing in the form changed keeps its value, with the precision and the kind the text of a control cannot hold. A repeatable list is written into the items its rows were loaded from (see `FormController.Rows``), so members that are not columns survive; a new item is created only for an added row, and the collection is left alone when no row was added or removed. |
| `CancelPendingValidations()` | `void` | Cancels the asynchronous validations that are waiting for their debounce or running, without changing any value or error. |
| `ClearErrors(FormItem<TModel>? item = null)` | `void` | Clears the errors of one field, or every error when no field is given. |
| `CreateModel()` | `TModel` | Builds a new model from the values of the fields in effect. Fields that are hidden, or in a skipped step, keep the default of their member. The items of a repeatable list are new items: they are never the items of a model the form was loaded from, which this method does not change. Use `FormController.ApplyTo` to keep them. |
| `Dispose()` | `void` | Cancels the pending asynchronous validations and stops starting new ones. The values and errors stay readable. |
| `ExportDraft(bool includeSensitive = false)` | `FormDraft` | Takes a snapshot of what the user has typed, for example to keep a long form across a page reload. Passwords and one-time codes are left out unless `includeSensitive` is set, and files never are part of a draft. |
| `Get<TValue>(FormField<TModel, TValue> field)` | `TValue` | Gets the current value of a field as its type. |
| `GetError(FormItem<TModel> item)` | `string?` | Gets the error to show for a field. |
| `GetOptions(FormItem<TModel> item)` | `IReadOnlyList<FormOptionItem>` | Gets the options of a choice field, with their values as invariant text, as the controls see them. |
| `GetSectionStates()` | `IReadOnlyList<FormPageSectionState>` | Computes the completion state of each visible section from the current values and errors. |
| `IsDisabled(FormItem<TModel> item)` | `bool` | Tells whether a field is disabled now, which includes every field while a submission runs. |
| `IsFieldDirty(FormItem<TModel> item)` | `bool` | Tells whether the value of a field differs from its baseline. |
| `IsRequired(FormItem<TModel> item)` | `bool` | Tells whether a field is required now. |
| `IsValidating(FormItem<TModel> item)` | `bool` | Tells whether an asynchronous rule of a field is waiting for its debounce or running. |
| `IsVisible(FormItem<TModel> item)` | `bool` | Tells whether a field is shown now: its condition is absent or true. It does not look at the container, step or section that holds it. |
| `NextStepAsync()` | `Task<bool>` | Moves to the next step that is not skipped, after validating the current one and running the `OnBeforeNext` hook. |
| `PreviousStepAsync()` | `Task<bool>` | Moves to the previous step that is not skipped, after the `OnBeforePrevious` hook. |
| `RemoveRow<TItem>(FormListField<TModel, TItem> list, int index)` | `void` | Removes a row from a repeatable list, moving the errors of the following rows up. |
| `Reset()` | `void` | Puts every value back to the baseline, clears errors and touched state, and goes back to the first step. |
| `Reset(TModel model)` | `void` | Loads a model: its values become the new baseline, errors and touched state are cleared and the form goes back to its first step. |
| `ResetField(FormItem<TModel> item)` | `void` | Restores a field to its baseline value and clears its errors and touched state. |
| `ResetSection(string sectionKey)` | `void` | Resets every field of a section. |
| `RestoreDraft(FormDraft draft)` | `void` | Puts the values of a draft back into the form, as if the user had typed them. The baseline of dirty tracking is not touched, so the restored fields count as unsaved changes. A value of the draft that is not a value of its field (`abc` for a date) is kept as the text it is and reported by `FormController.Validate` and `FormController.SubmitAsync` as an error of the field. The rows of a restored list come from no item of a model, so a write creates new items for them. |
| `Rows<TItem>(FormListField<TModel, TItem> list)` | `IReadOnlyList<FormRow<TItem>>` | Gets the rows of a repeatable list. |
| `Set<TValue>(FormField<TModel, TValue> field, TValue value, bool markTouched = false)` | `void` | Sets the value of a field, validating it when it was touched and running its change hooks. |
| `SetError(FormItem<TModel> item, string message, bool blocking = false)` | `void` | Sets an error on a field, for example after a server check. It stays until the field changes or `FormController.ClearErrors` runs. |
| `SetFormError(string message, bool blocking = true)` | `void` | Sets an error on the form as a whole. |
| `SubmitAsync()` | `Task<bool>` | Validates every field in effect, runs the model checks, `OnBeforeSubmit` and `OnSubmit`, and on success takes a new baseline so the form is no longer dirty. The model is created from the values for the checks and hooks and then dropped; use the overload with a save step to get it. |
| `SubmitAsync(Func<TModel, CancellationToken, Task<bool>> save, TModel target = null, CancellationToken cancellationToken = null)` | `Task<bool>` | Submits the form with your save step inside the same lifecycle. The steps are: validate (waiting for asynchronous rules); write the values into `target`, or create a new model when there is none; run the model checks and `OnBeforeSubmit` and `OnSubmit` on that model; call `save`. When a step fails, or `save` returns `false` or throws, the members the write changed get their previous values again (a list or a multiple choice holds its previous items, and a row gets its previous cells). Members the form did not write are not touched, and nothing that `OnBeforeSubmit`, `OnSubmit` or `save` did is undone, neither on the model nor outside it: `OnSubmit` runs before `save`, so its side effects stay when the save fails. When all succeed the baseline is retaken, so the form is no longer dirty. |
| `Touch(FormItem<TModel> item)` | `void` | Marks a field touched, which makes its validation error visible, and validates it. |
| `TryGet<TValue>(FormField<TModel, TValue> field, out TValue value)` | `bool` | Tries to get the current value of a field. |
| `Validate(bool currentStepOnly = false)` | `bool` | Validates every field in effect: its rules, and whether its control holds a value of the type of the member (a control can hold text that is not a value yet, from a draft or a custom control; that is an error of the field with the message `FormMessages.InvalidValue`, whatever the validation mode). |
| `ValidateAsync(bool currentStepOnly = false, CancellationToken cancellationToken = null)` | `Task<bool>` | Validates every field in effect, waiting for the asynchronous rules. |

#### `FormDraft`

`Klyroo.Form` · class

A snapshot of what the user has typed, produced by `DynamicFormController.ExportState` and loaded back with `DynamicFormController.RestoreDraft`, for example to keep a long form across a page reload. It holds values only: the baseline that dirty tracking compares against stays with the controller. `FormDraft.ToJson` and `FormDraft.FromJson` write and read it without reflection, so it works under NativeAOT and trimming.

| Property | Type | Description |
| :--- | :--- | :--- |
| `StepIndex` | `int` | Zero-based index of the step the user was on. 0 for a form without steps. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static FromJson(string json)` | `FormDraft` | Reads a draft written by `FormDraft.ToJson`. Numbers come back as `Double`, or as `Decimal` when their digits do not fit a double (a large `Int64`); `DynamicFormController.RestoreDraft` converts each value to the type its field uses. |
| `ToJson()` | `string` | Writes the draft as JSON. Values the controls do not use are written as text. |

#### `FormField<TModel, TValue>`

`Klyroo.Form` · class · inherits `FormItem<TModel>`

A field of a typed form: a handle on one member of the model, with the type of its value. Declare it with `Field`, then read and write it with `FormController`. A handle is immutable and can be used by every controller of the schema.

#### `FormFieldBuilder<TModel, TValue>`

`Klyroo.Form` · class

Configures one field of a typed form: its label, rules, conditions and defaults. Methods return the builder, so a field reads as one chain: `f.Required().MaxLength(120)`. Settings that only make sense for some kinds of value (`AsTextarea`, `Range`, `Options`) are extension methods that only exist for the types they fit, so a wrong kind does not compile.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AsCustom(string? key = null)` | `FormFieldBuilder<TModel, TValue>` | Makes the control one you provide, for any type of value: a `KlyFieldTemplate` of the field, or a renderer registered with `CustomField<TValue>` under `key`, draws it. The field is stateful like any input: it holds the value you store, is validated with its rules and is part of the model. |
| `Clearable(bool clearable = true)` | `FormFieldBuilder<TModel, TValue>` | Shows a clear button once the field has a value. |
| `Default(TValue value)` | `FormFieldBuilder<TModel, TValue>` | Sets the value the field starts with when the model does not give one. |
| `Default(Func<FormController<TModel>, TValue> factory)` | `FormFieldBuilder<TModel, TValue>` | Computes the value the field starts with from the controller. It also fills the cells of a new row of a repeatable list. |
| `DependsOn(params FormItem<TModel>[] items)` | `FormFieldBuilder<TModel, TValue>` | Names the fields this field's rules read, so a change of one of them validates this field again once it was touched. A confirmation field depends on the password it repeats. |
| `Description(string? description)` | `FormFieldBuilder<TModel, TValue>` | Sets the text shown under the label. |
| `DirtyCheck(bool dirtyCheck = true)` | `FormFieldBuilder<TModel, TValue>` | Shows a per-field reset button once the value differs from its initial value. |
| `Disabled(bool disabled = true, string? reason = null)` | `FormFieldBuilder<TModel, TValue>` | Renders the control disabled. This only affects rendering: a disabled field is still validated and written to the model. |
| `DisabledWhen(Func<FormController<TModel>, bool> when, string? reason = null)` | `FormFieldBuilder<TModel, TValue>` | Disables the field while `when` returns `true`. |
| `Help(string? help)` | `FormFieldBuilder<TModel, TValue>` | Sets the text shown under the control. |
| `Hint(string? hint)` | `FormFieldBuilder<TModel, TValue>` | Sets the short text shown next to the label. |
| `Kind(FormFieldKind kind)` | `FormFieldBuilder<TModel, TValue>` | Chooses the control by its kind, for a type that has no default one or to replace the default. Prefer the kind-specific methods (`AsTextarea`, `AsSwitch`), which exist only for the types they fit. |
| `Label(string? label)` | `FormFieldBuilder<TModel, TValue>` | Sets the text of the label shown above the control. The default is the name of the member, split into words. |
| `NullWhenEmpty(bool nullWhenEmpty = true)` | `FormFieldBuilder<TModel, TValue>` | Decides what an empty input becomes for a string member: `null` when `true`, an empty string when `false`. By default the nullable annotation of the member decides. |
| `OmitFromOutput(bool omit = true)` | `FormFieldBuilder<TModel, TValue>` | Validates and stores the field but leaves it out of the model write, so the member keeps the value it has. |
| `OnChange(Action<TValue, FormController<TModel>> onChange)` | `FormFieldBuilder<TModel, TValue>` | Calls `onChange` with the new value after the value of the field changes by `Set` or by the user, and after a reset of the field that changes it. It does not run when a whole new state is loaded. |
| `Placeholder(string? placeholder)` | `FormFieldBuilder<TModel, TValue>` | Sets the placeholder of the control. For a choice field it is the text of the empty option. |
| `Required(bool required = true, string? message = null)` | `FormFieldBuilder<TModel, TValue>` | Makes the field required, or not. |
| `Required(Func<FormController<TModel>, bool> when, string? message = null)` | `FormFieldBuilder<TModel, TValue>` | Makes the field required while `when` returns `true`. It is evaluated on every validation. |
| `Rule(Func<TValue, bool> isValid, string message)` | `FormFieldBuilder<TModel, TValue>` | Adds a rule: while `isValid` returns `false` the field shows `message`. Rules run in order after the built-in checks, also for an empty value of a field that is not required, and the first failing one sets the error. |
| `Rule(Func<TValue, FormController<TModel>, bool> isValid, string message)` | `FormFieldBuilder<TModel, TValue>` | Adds a rule that also reads the controller, for example to compare the value with another field. |
| `RuleAsync(Func<TValue, FormController<TModel>, CancellationToken, ValueTask<string?>> validate, TimeSpan? debounce = null)` | `FormFieldBuilder<TModel, TValue>` | Adds an asynchronous rule, for a check that needs I/O such as "is this username free". It runs in the background once the user left the field, and again as they edit it after `debounce`; submission waits for it. It does not run for an empty value or for a value that already fails a synchronous rule. |
| `Span(int span)` | `FormFieldBuilder<TModel, TValue>` | Sets the number of columns the field occupies in the grid of the form. Values are clamped between 1 and the column count. |
| `TransformInput(Func<TValue, TValue> transform)` | `FormFieldBuilder<TModel, TValue>` | Converts a value from the model into the value held by the form when the form is created or reset. It does not apply to default values or to values set later. |
| `TransformOutput(Func<TValue, TValue> transform)` | `FormFieldBuilder<TModel, TValue>` | Converts the value when the model is written, for example to trim a string. The form keeps the untransformed value. |
| `Trigger(FormValidationTrigger trigger)` | `FormFieldBuilder<TModel, TValue>` | Chooses when the field is validated while the user edits it. The default is `FormValidationTrigger.Blur`. |
| `VisibleWhen(Func<FormController<TModel>, bool> when)` | `FormFieldBuilder<TModel, TValue>` | Shows the field only while `when` returns `true`. A hidden field is skipped by validation and left out of the model write, and its value stays in the controller. |

#### `FormFieldConventionContext`

`Klyroo.Form` · class

The member a field is declared from and the metadata a `FormFieldConvention` may set on it: label, description, placeholder, kind, requirement, length, range, pattern and options.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Kind` | `FormFieldKind` | The kind of control the field has so far. |
| `Label` | `string?` | The label of the field so far. |
| `Member` | `FormMemberInfo` | The member the field is declared from. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `DependsOn(params string[] keys)` | `void` | Makes the field depend on other fields of the same form by key, so a change of one of them validates it again once it was touched. |
| `SetDescription(string? description)` | `void` | Sets the description shown under the label. |
| `SetDisabled(bool disabled)` | `void` | Disables the control. A disabled field is still validated and written to the model. |
| `SetKind(FormFieldKind kind)` | `void` | Sets the kind of control. |
| `SetLabel(string? label)` | `void` | Sets the label. |
| `SetMaxLength(int maxLength)` | `void` | Limits the number of characters of a text value. |
| `SetOptions(IReadOnlyList<FormOptionItem> options)` | `void` | Sets the options of a choice field, as the text of their values and their labels. |
| `SetPattern(string pattern, string message)` | `void` | Requires a text value to match a regular expression. An empty value is the business of the required check. |
| `SetPlaceholder(string? placeholder)` | `void` | Sets the placeholder of the control. |
| `SetRange(double min, double max)` | `void` | Bounds a number. |
| `SetRequired(bool required, string? message = null)` | `void` | Makes the field required. |

#### `FormFileValue`

`Klyroo.Form` · class

A file chosen in a `FormFieldKind.File` field, without any UI type: the name, size and content type the user's device reported, and a way to open the content. The value of a file field is a `FormFileValue` (or `null`), or a `List<FormFileValue>` with `DynamicFieldSchema.Multiple`. The controller checks `DynamicFieldSchema.Accept`, `DynamicFieldSchema.MaxFileSize` and `DynamicFieldSchema.Multiple` on it, so a test can build one by hand with `FormFileValue.FromBytes`.

| Constructor | Description |
| :--- | :--- |
| `FormFileValue(string name, long size, string contentType, Func<CancellationToken, ValueTask<Stream>> open, DateTimeOffset? lastModified = null)` | Creates a file value. |

| Field | Type | Description |
| :--- | :--- | :--- |
| `const DefaultMaxSize = 10485760` | `long` | The largest file a `FormFieldKind.File` field accepts, and keeps in memory, when `DynamicFieldSchema.MaxFileSize` is not set: 10 MiB. |

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
| `static FormatSize(long bytes, string decimalSeparator = ".", IReadOnlyList<string>? units = null)` | `string` | Formats a size for a message: `512 B`, `1.5 KB`, `5 MB`. The default is the invariant culture and English-style units; a locale passes its own decimal separator and units (French: `1,5 Mo`). |
| `static FromBytes(string name, ReadOnlySpan<byte> content, string contentType = "application/octet-stream")` | `FormFileValue` | Builds a file value over bytes in memory, for tests and for files your code produces. |
| `GetHashCode()` | `int` |  |
| `Matches(string? accept)` | `bool` | Tells whether the file matches an accept list in the syntax of the HTML `accept` attribute: comma-separated extensions (`.png`), content types (`image/png`) and wildcards (`image/*`), compared ignoring case. |
| `OpenReadAsync(CancellationToken cancellationToken = null)` | `ValueTask<Stream>` | Opens a new stream over the content. Dispose it when done. |
| `ToString()` | `string` |  |

#### `FormGroupBuilder<TModel, THolder>`

`Klyroo.Form` · class · inherits `FormScopeBuilder<TModel, THolder>`

Declares the fields of a group, a card, a tab, a step or a section of a typed form. See `FormScopeBuilder` for what it declares.

#### `FormItem<TModel>`

`Klyroo.Form` · class

An element of a typed form that a condition can point at: a field, or a layout element such as a group, a card, a section heading or an info block. It is what `DependsOn` takes, and what `FormSchemaBuilder` methods return.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | The key of the element in the form: the names of the members from the model down to it, joined with dots (`Address.City`) for a field, or the key the element was declared with for a layout element. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `ToString()` | `string` |  |

#### `FormListField<TModel, TItem>`

`Klyroo.Form` · class · inherits `FormField<TModel, IReadOnlyList<TItem>>`

A repeatable list of rows of a typed form: a handle on a collection member of the model. A row is a `TItem`; its columns are the fields declared in the `List` call, as `FormField` of the item.

#### `FormMemberInfo`

`Klyroo.Form` · class

Describes the member of a model a field is declared from. A convention (`FormFieldConventionContext`) receives it to decide the settings of the field, and `Klyroo.Form.DataAnnotations` reads the attributes of `FormMemberInfo.Property`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsNullableValueType` | `bool` | `true` when the member is a `Nullable`. |
| `Key` | `string` | The key of the field: the names of the members from the model down to this one, joined with dots (`Address.City`). It is the key of the value in the controller and in `FormController.CreateModel`. |
| `ModelType` | `Type` | The type of the model the form is declared for. |
| `Name` | `string` | The name of the member itself (`City` for `Address.City`). |
| `Path` | `IReadOnlyList<PropertyInfo>` | The members from the model to this one. The last one is `FormMemberInfo.Property`. |
| `Property` | `PropertyInfo` | The member the field reads and writes. |
| `Type` | `Type` | The declared type of the member, `Nullable` included. |
| `UnderlyingType` | `Type` | The type of the member without `Nullable`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `SiblingKey(string memberName)` | `string` | The key a sibling member of this one has: same parent, another member name. `Address.City` and `Zip` give `Address.Zip`. |

#### `FormModelErrors<TModel>`

`Klyroo.Form` · class

Collects the errors of a model check declared with `Validate`. An error names the field it belongs to, or none for the form as a whole.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Add(string message)` | `void` | Reports an error on the form as a whole. |
| `Add<TValue>(Expression<Func<TModel, TValue>> member, string message)` | `void` | Reports an error on a member of the model. |
| `Add(FormItem<TModel> item, string message)` | `void` | Reports an error on a field or an element of the form. |
| `Add(string member, string message)` | `void` | Reports an error on a member by its key (`Address.City`) or by a member name that only one field has (`City`). A name that matches no field makes the error a form-level one. This is the form that fits a name read from text, such as the member names of a validation result. |
| `IsActive(FormItem<TModel> item)` | `bool` | Tells whether a field is in effect: not hidden by a condition, in a hidden container or section, or in a skipped step. A check should leave the other fields alone, because their values are not the user's. |
| `IsActive(string key)` | `bool` | Tells whether the field with this key is in effect. See `FormModelErrors.IsActive`. |

#### `FormRow<TItem>`

`Klyroo.Form` · class

A row of a repeatable list of a typed form, read and written through a `FormController`. Its columns are the fields of the item type declared in the `List` call.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Index` | `int` | The zero-based position of the row in the list. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Get<TValue>(FormField<TItem, TValue> column)` | `TValue` | Reads the value of a column of this row. |
| `GetError<TValue>(FormField<TItem, TValue> column)` | `string?` | Gets the error to show for a column of this row. |
| `Set<TValue>(FormField<TItem, TValue> column, TValue value, bool markTouched = false)` | `void` | Sets the value of a column of this row, revalidating the cell when it was touched. |
| `TryGet<TValue>(FormField<TItem, TValue> column, out TValue value)` | `bool` | Tells whether a column of this row has a value that is a valid `TValue`. |

#### `FormRowBuilder<TModel, TItem>`

`Klyroo.Form` · class

Declares the columns of a row of a repeatable list of a typed form. A column is a field of the item type; its handle reads and writes the cell of a `FormRow`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Field<TValue>(Expression<Func<TItem, TValue>> member, Action<FormFieldBuilder<TModel, TValue>>? configure = null)` | `FormField<TItem, TValue>` | Declares a column bound to a member of the item type. The conditions of the column receive the controller of the whole form. |

#### `FormSchema<TModel>`

`Klyroo.Form` · class

The declaration of a typed form: its fields, steps or sections, rules and hooks, bound to the members of `TModel`. A schema is immutable and holds no values: share one between every controller of the form, and read a field of a controller with the handle `Field` returns.

| Property | Type | Description |
| :--- | :--- | :--- |
| `FormKey` | `string` | The identifier of the form. |
| `Title` | `string?` | The title of the form, or `null`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Field<TValue>(Expression<Func<TModel, TValue>> member)` | `FormField<TModel, TValue>` | Gets the handle of a field declared with a member of the model, to read and write it through a controller from code that did not keep the handle `Field` returned. For a list use `FormSchema.List``, and for a multiple choice `FormSchema.MultiSelect``. |
| `Item(string key)` | `FormItem<TModel>` | Gets the handle of an element by its key: a field (its member names joined with dots) or a layout element (the key it was declared with). |
| `List<TItem>(Expression<Func<TModel, IEnumerable<TItem>>> member)` | `FormListField<TModel, TItem>` | Gets the handle of a repeatable list declared with a collection member of the model. |
| `MultiSelect<TElement>(Expression<Func<TModel, IEnumerable<TElement>>> member)` | `FormField<TModel, IReadOnlyList<TElement>>` | Gets the handle of a multiple choice declared with a collection member of the model. |

#### `FormSchemaBuilder<TModel>`

`Klyroo.Form` · class · inherits `FormScopeBuilder<TModel, TModel>`

Declares a typed form from the members of a model: the entry point of `FormSchema.For``. It is also the builder of a step or a section, through `FormSchemaBuilder.Step` and `FormSchemaBuilder.Section`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Columns` (get, set) | `int` | The number of columns, from 1 to 12, of the grids that lay out the fields. The default is 12. |
| `Description` (get, set) | `string?` | The text shown under the title. |
| `Eyebrow` (get, set) | `string?` | The small label shown above the title. |
| `FormKey` (get, set) | `string` | The identifier of the form, written to the `data-key` attribute of the rendered form. The default is `form`. |
| `Messages` (get, set) | `FormMessages?` | The built-in validation messages of the form. When `null` the controller uses the English messages, or those of the current locale when a Klyroo.Form component renders it. |
| `ShowStepper` (get, set) | `bool` | `true` to show the list of steps above a stepped form. The default is `true`. |
| `Title` (get, set) | `string?` | The title shown in the form header. |
| `ValidateMode` (get, set) | `FormValidationMode` | Which checks validation runs. The default is `FormValidationMode.All`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AllMembers()` | `FormSchemaBuilder<TModel>` | Declares a field for every public readable and writable member of the model that has a default control and no field yet, with the label and settings the type gives. Use it together with a convention such as `UseDataAnnotations()` that reads the rest from the model. |
| `Convention(FormFieldConvention convention)` | `FormSchemaBuilder<TModel>` | Adds a convention that adjusts every field declared from a member, after its defaults and before its explicit settings. |
| `Factory(Func<TModel> factory)` | `FormSchemaBuilder<TModel>` | Says how a new model is created when the form needs one and the model type has no parameterless constructor. |
| `IgnoreDirty(params FormItem<TModel>[] items)` | `FormSchemaBuilder<TModel>` | Excludes fields from dirty tracking, for example a search box that is not part of the data. |
| `OnBeforeNext(Func<FormController<TModel>, int, Task<bool>> hook)` | `FormSchemaBuilder<TModel>` | Runs after the current step validates and before moving forward. Return `false` to stay on the step. |
| `OnBeforePrevious(Func<FormController<TModel>, int, Task<bool>> hook)` | `FormSchemaBuilder<TModel>` | Runs before moving back. Return `false` to stay on the step. |
| `OnBeforeSubmit(Func<TModel, FormController<TModel>, Task<bool>> hook)` | `FormSchemaBuilder<TModel>` | Runs after validation and the model checks succeed, and before the save step. Return `false` to cancel the submission, for example after a failed server check. |
| `OnSubmit(Func<TModel, FormController<TModel>, Task> hook)` | `FormSchemaBuilder<TModel>` | Runs when the form is submitted and validation and `FormSchemaBuilder.OnBeforeSubmit` succeeded, before the save step. An exception it throws propagates to the caller of `SubmitAsync`. |
| `Order(Func<FormMemberInfo, int?> order)` | `FormSchemaBuilder<TModel>` | Orders the fields: a field the function gives a number to is placed by that number, the others keep their order of declaration after them. The first function that answers decides. |
| `Section(string key, string label, Action<FormGroupBuilder<TModel, TModel>> configure, string? description = null, bool optional = false, int span = 12, Func<FormController<TModel>, bool>? visibleWhen = null)` | `FormSchemaBuilder<TModel>` | Declares a section of a page form. Sections replace the fields declared outside a section, and are reported by `FormController.GetSectionStates`. |
| `Step(string key, string title, Action<FormGroupBuilder<TModel, TModel>> configure, string? description = null, Func<FormController<TModel>, bool>? skipWhen = null)` | `FormSchemaBuilder<TModel>` | Declares a step of a wizard. When the form has steps, the fields and sections declared outside a step are ignored. |
| `Validate(Action<TModel, FormModelErrors<TModel>> validate)` | `FormSchemaBuilder<TModel>` | Adds a check of the whole model, run on submit after the field rules passed, on the model written from the values. The errors it reports go to the field they name, or to the form. |

#### `FormScopeBuilder<TModel, THolder>`

`Klyroo.Form` · class

Declares the fields and layout of one block of a typed form: the form itself, a step, a section, a group, a card or a tab. Every method returns the handle of what it declared, so a later condition can point at an earlier field through a captured local. The fields are read with `FormController`.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Button(string key, string label, Func<FormController<TModel>, Task> onClick, Func<FormController<TModel>, bool>? visibleWhen = null)` | `FormItem<TModel>` | Declares a button that runs `onClick` and never submits the form. |
| `Card(string key, string label, Action<FormGroupBuilder<TModel, THolder>> configure, string? description = null, bool collapsible = false, Func<FormController<TModel>, bool>? visibleWhen = null)` | `FormItem<TModel>` | Declares a bordered container for fields of the same model, to lay them out together. |
| `Divider(string key)` | `FormItem<TModel>` | Declares a horizontal rule. |
| `Field<TValue>(Expression<Func<THolder, TValue>> member, Action<FormFieldBuilder<TModel, TValue>>? configure = null)` | `FormField<TModel, TValue>` | Declares a field bound to a member of the model. |
| `Field<TValue>(string key, Action<FormFieldBuilder<TModel, TValue>>? configure = null)` | `FormField<TModel, TValue>` | Declares a field that the form has and the model does not, such as a confirmation or the acceptance of the terms. It is read and written through the controller like any field, validated and part of the draft, and left out of the model. |
| `Group<TNested>(Expression<Func<THolder, TNested>> member, Action<FormGroupBuilder<TModel, TNested>> configure, string? label = null, string? description = null, bool card = false, bool collapsible = false)` | `FormItem<TModel>` | Declares a group of fields for an object member of the model, laid out together. The fields are declared on the type of the member and keep the model-wide keys (`Address.City`). A null object is created when the model is written. |
| `Heading(string key, string label, string? description = null, Func<FormController<TModel>, bool>? visibleWhen = null)` | `FormItem<TModel>` | Declares a heading made of a label and a description. |
| `Info(string key, string text, string? title = null, FormInfoTone? tone = null, Func<FormController<TModel>, bool>? visibleWhen = null)` | `FormItem<TModel>` | Declares a read-only block of text. |
| `List<TItem>(Expression<Func<THolder, IEnumerable<TItem>>> member, Action<FormRowBuilder<TModel, TItem>> columns, string? addLabel = null, Action<FormFieldBuilder<TModel, IReadOnlyList<TItem>>>? configure = null, Func<TItem>? itemFactory = null)` | `FormListField<TModel, TItem>` | Declares a repeatable list of rows for a collection member. Each row is an item of the collection; its columns are declared in `columns`. |
| `MultiSelect<TElement>(Expression<Func<THolder, IEnumerable<TElement>>> member, IEnumerable<FormOption<TElement>> options, Action<FormFieldBuilder<TModel, IReadOnlyList<TElement>>>? configure = null)` | `FormField<TModel, IReadOnlyList<TElement>>` | Declares a field for a collection member as a multiple choice: the user ticks several options and the member holds their values. |
| `Tabs(string key, Action<FormTabsBuilder<TModel, THolder>> configure)` | `FormItem<TModel>` | Declares a tabbed container for fields of the same model. The fields of every visible tab are validated and written to the model, whether or not the tab is selected. |

#### `FormTabsBuilder<TModel, THolder>`

`Klyroo.Form` · class

Declares the tabs of a tabbed container of a typed form.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Tab(string key, string label, Action<FormGroupBuilder<TModel, THolder>> configure, Func<FormController<TModel>, bool>? visibleWhen = null)` | `FormTabsBuilder<TModel, THolder>` | Declares a tab. |

#### `DynamicControlContext`

`Klyroo.Form.Dynamic` · class

Everything a control needs to edit one field: the value and how to change it, the error, the state flags and the ids that tie the control to its label, description, help and error. `KlyFormEngine` and `KlyFormPage` build one for each control they render, and pass it to the renderer of a `FormFieldKind.Custom` field. The same type serves a field and a cell of a repeatable list. A context is a snapshot of one render: build a new one (the components do) after the form changes.

| Constructor | Description |
| :--- | :--- |
| `DynamicControlContext(DynamicFormController form, DynamicFieldSchema field, string id)` | Creates the context of a field of `form`, with the state, error and ids the built-in controls use. `DynamicControlContext.Set`, `DynamicControlContext.Commit` and `DynamicControlContext.Blur` act on the field through the controller. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `DescribedBy` | `string?` | The ids of the description, help and error elements to put in `aria-describedby`, space separated, or `null`. |
| `Disabled` | `bool` | `true` when the field is disabled (`DynamicFieldSchema.Disabled` or `DynamicFieldSchema.DisabledWhen`, or its list is). Render the control disabled. |
| `Error` | `string?` | The error to show, or `null`. The error text itself is rendered by the component around the control. |
| `ErrorId` | `string` | The id of the error element. It exists only while `DynamicControlContext.Error` is not `null`. |
| `Field` | `DynamicFieldSchema` | The field being edited. For a cell of a repeatable list, the child field of the column. |
| `Form` | `DynamicFormController` | The controller that holds the value. |
| `Id` | `string` | The `id` the control must use, so that the label points at it. |
| `IsCell` | `bool` | `true` when the control is a cell of a repeatable list. |
| `LabelledBy` | `string?` | The ids of the elements that label a control made of several inputs (`aria-labelledby`), or `null` when a `label` element points at `DynamicControlContext.Id`. |
| `Required` | `bool` | `true` when the field is required (`DynamicFieldSchema.Required` or `DynamicFieldSchema.RequiredWhen`). Set `aria-required` on the control. |
| `Slot` | `string` | The `data-slot` the built-in controls carry (`form-control`, or `form-array-input` in a cell of a repeatable list). |
| `Validating` | `bool` | `true` while an asynchronous rule of the field runs (see `DynamicFormController.IsValidating`). |
| `Value` | `object?` | The value when this context was built. Use `DynamicControlContext.Current` for the live value inside an event handler. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Blur()` | `void` | Tells the form the control lost focus: the field is marked touched and validated, which is what makes its error show. |
| `Commit(object? value)` | `void` | Changes the value and marks the field touched, for a change that is complete in one gesture: a click, a selection, a removal. The error shows at once. |
| `Current()` | `object?` | Gets the live value, which may be newer than `DynamicControlContext.Value`. In a cell, the value of the cell. |
| `Current<T>()` | `T` | Gets the live value converted to `T` like `DynamicFormController.GetValue`` does. |
| `Set(object? value)` | `void` | Changes the value while the user is editing, for example on every keystroke. The field is validated as `DynamicFormController.SetValue` does: only once touched, and per its trigger. |

#### `DynamicFieldSchema`

`Klyroo.Form.Dynamic` · class

Declarative definition of one field, or of a layout element such as a section heading, a container or a repeatable list. Fields are immutable; build them with object initializers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Accept` (init) | `string?` | The accepted files of a `FormFieldKind.File` field, in the syntax of the HTML `accept` attribute: comma-separated extensions (`.png`), content types (`image/png`) and wildcards (`image/*`). It is the `accept` attribute of the input, which only filters the picker, so validation checks it too (see `FormFileValue.Matches`). |
| `AddItemLabel` (init) | `string?` | Label of the add button of a repeatable list. The default is `Add item`. |
| `AsyncValidators` (init) | `IReadOnlyList<DynamicAsyncValidatorRule>?` | Asynchronous rules, run after the synchronous checks pass and the value is not empty. They run in the background after the field is touched and while it is edited (see `DynamicAsyncValidatorRule.Debounce`), and `DynamicFormController.SubmitAsync`, `DynamicFormController.NextStepAsync` and `DynamicFormController.ValidateAsync` wait for them. `DynamicFormController.IsValidating` tells whether one is running, which the components show next to the field. |
| `Clearable` (init) | `bool` | `true` to show a clear button once the field has a value, next to select lists, text-like inputs, textareas, passwords, number inputs and ratings. Clearing sets the value to `null`, or to an empty string for text-like inputs. |
| `Collapsible` (init) | `bool` | `true` to render a `FormFieldKind.Group`, `FormFieldKind.Card` or `FormFieldKind.Object` container as a disclosure (`details` and `summary`) the user can collapse, with `DynamicFieldSchema.Label` as its summary. Collapsing only hides the children: they are still validated and output. |
| `Condition` (init) | `Func<DynamicFormController, bool>?` | Returns `false` to hide the field. A hidden field is skipped by validation and left out of `DynamicFormController.BuildOutput`, but its value stays in the state. A hidden container hides its children too, with the same effect on validation and output. On a child of a repeatable list it hides that column: its cells are neither rendered nor validated, and the column is left out of the rows of the output, while its values stay in the state. |
| `CustomKey` (init) | `string?` | Names the renderer of a `FormFieldKind.Custom` field: the key it was registered under with `KlyrooFormOptions.CustomField`, or the key in the `FieldTemplates` parameter. Keys are case sensitive. A custom field without a registered renderer shows a notice instead of a control. |
| `DefaultCollapsed` (init) | `bool` | `true` to start a `DynamicFieldSchema.Collapsible` container collapsed. Ignored when `DynamicFieldSchema.Collapsible` is `false`. |
| `DefaultFactory` (init) | `Func<DynamicFormController, object?>?` | Computes the initial value from the controller. It takes precedence over `DynamicFieldSchema.DefaultValue` and also fills the cells of a new row of a repeatable list. |
| `DefaultValue` (init) | `object?` | Initial value used when the initial input has no value for the key. Ignored when `DynamicFieldSchema.DefaultFactory` is set. |
| `DependsOn` (init) | `IReadOnlyList<string>?` | Keys of the fields whose value this field's rules read, for example a confirmation field that compares itself with `password`. Once this field has been touched, a change of any of those fields validates it again, even when it shows no error yet, so a mismatch appears (and disappears) as soon as the other field changes. Without it, a touched field that currently shows an error is still validated again after any other field changes, which clears stale errors but cannot raise a new one. Ignored with `FormValidationTrigger.Submit`. On a child of a repeatable list it applies to the cells of that column. |
| `Description` (init) | `string?` | Text shown under the label. For `FormFieldKind.Info` it is the fallback of `DynamicFieldSchema.DisplayText`. |
| `DirtyCheck` (init) | `bool` | `true` to show a per-field reset button in `KlyFormEngine` once the value differs from its initial value. |
| `Disabled` (init) | `bool` | `true` to render the control disabled; on a repeatable list it also disables its cells and the add and remove buttons. This only affects rendering: a disabled field is still validated and included in the output. |
| `DisabledReason` (init) | `string?` | Text shown under a disabled control to explain why it is disabled. |
| `DisabledWhen` (init) | `Func<DynamicFormController, bool>?` | Disables the field while it returns `true`. Same scope as `DynamicFieldSchema.Disabled`. |
| `DisplayKind` (init) | `FormInfoKind?` | Presentation of a `FormFieldKind.Info` block: `FormInfoKind.Callout` (the default when null) draws a tinted box with a border, `FormInfoKind.Text` renders plain text without a box. |
| `DisplayText` (init) | `string?` | Text of a `FormFieldKind.Info` block. Falls back to `DynamicFieldSchema.Description`. |
| `DisplayTone` (init) | `FormInfoTone?` | Color of a `FormFieldKind.Info` block, from the Klyroo.UI theme: `FormInfoTone.Neutral` when null. |
| `Fields` (init) | `IReadOnlyList<DynamicFieldSchema>?` | Child fields: the content of a container, or the columns of each row of a repeatable list. A container without children renders nothing. |
| `Help` (init) | `string?` | Text shown under the control. |
| `Hint` (init) | `string?` | Short text shown next to the label. |
| `Ignore` (init) | `bool` | `true` to drop the field entirely: it has no state, is not rendered, validated or output, and its children are dropped with it. |
| `Key` (init) | `string` | Unique key of the field. It is the key of the value in the form state and in the output dictionary. Keys are flat strings: containers do not prefix the keys of their children. |
| `Kind` (init) | `FormFieldKind` | The kind of the field. The default is `FormFieldKind.Text`. |
| `Label` (init) | `string?` | Text of the label shown above the control. For `FormFieldKind.Info`, `FormFieldKind.Section` and containers it is the heading. |
| `Max` (init) | `double?` | Largest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `max` attribute of number and slider inputs, and the number of stars of a `FormFieldKind.Rating` field. |
| `MaxFileSize` (init) | `long?` | The largest accepted size in bytes of each file of a `FormFieldKind.File` field. The components read a chosen file into memory when it is chosen, so this is also the most memory one file can take (times the number of files with `DynamicFieldSchema.Multiple`, per field, for as long as the value is held). It is enforced on validation and when the content is read or opened. `null` means `FormFileValue.DefaultMaxSize` (10 MiB); set a larger value on purpose if you accept larger files. |
| `MaxLength` (init) | `int?` | Largest accepted number of characters of a text value, enforced on validation. Also the `maxlength` attribute of text-like inputs, including textareas, passwords and one-time codes. |
| `Min` (init) | `double?` | Smallest accepted value of a `FormFieldKind.Number` field, enforced on validation. Also the `min` attribute of number and slider inputs. |
| `Multiple` (init) | `bool` | `true` if a `FormFieldKind.File` field takes several files (its value is a `List<FormFileValue>`), or if a `FormFieldKind.Select` or `FormFieldKind.AutoComplete` field holds several values: it renders a list of checkboxes and the value is a `List<string>`. `DynamicFormController.CreateFieldOption` appends to that list. |
| `OmitFromOutput` (init) | `bool` | `true` to validate and store the field but leave it out of `DynamicFormController.BuildOutput`. |
| `OnClick` (init) | `Func<DynamicFormController, Task>?` | Runs when the user clicks a `FormFieldKind.Button` field. The button never submits the form, and is disabled like other fields by `DynamicFieldSchema.Disabled` and `DynamicFieldSchema.DisabledWhen`. |
| `OnCreateOption` (init) | `Func<string, FormOptionItem?>?` | Turns text typed by the user into a new option. On a `FormFieldKind.Select` or `FormFieldKind.AutoComplete` field the built-in components render a text input and a create button under the control, which call `DynamicFormController.CreateFieldOption`. |
| `Options` (init) | `IReadOnlyList<FormOptionItem>?` | Static options of a choice field. Ignored when `DynamicFieldSchema.OptionsFactory` is set. |
| `OptionsFactory` (init) | `Func<DynamicFormController, IReadOnlyList<FormOptionItem>>?` | Computes the options from the controller, for example to depend on another field. It takes precedence over `DynamicFieldSchema.Options`. |
| `PasswordRequirements` (init) | `IReadOnlyList<FormPasswordRequirement>?` | Rules a `FormFieldKind.Password` value must satisfy. They are checked on validation, in order, and the built-in components show them as a live checklist under the field. |
| `Placeholder` (init) | `string?` | Placeholder of the control. For `FormFieldKind.Select` and `FormFieldKind.AutoComplete` it is the text of the empty option. |
| `Required` (init) | `bool` | `true` if the field must have a value. An empty string, a whitespace string, `null` and an empty collection count as empty; `false` and `0` do not. |
| `RequiredMessage` (init) | `string?` | Error message of a failed required check. The default is `This field is required`. |
| `RequiredWhen` (init) | `Func<DynamicFormController, bool>?` | Makes the field required while it returns `true`. It is evaluated on every validation, in addition to `DynamicFieldSchema.Required`. |
| `Span` (init) | `int` | Number of columns the field occupies in the grid of `DynamicFormSchema.Columns` columns. Values are clamped between 1 and the column count, so the default of 12 fills a row. |
| `Step` (init) | `double?` | The `step` attribute of number and slider inputs. It is not validated. |
| `Tabs` (init) | `IReadOnlyList<DynamicTabSchema>?` | Tabs of a `FormFieldKind.Tabs` field. A tabs field without tabs renders nothing. |
| `TransformInput` (init) | `Func<object?, object?>?` | Converts a value from the initial input into the value held in the state. It does not apply to default values or to values set later. |
| `TransformOutput` (init) | `Func<object?, object?>?` | Converts the value in `DynamicFormController.BuildOutput`, for example to trim a string. The state keeps the untransformed value. |
| `Trigger` (init) | `FormValidationTrigger` | When the field is validated while the user edits it. The default is `FormValidationTrigger.Blur`. |
| `Validators` (init) | `IReadOnlyList<DynamicValidatorRule>?` | Custom validation rules, run in order after the built-in checks. The first failing rule sets the error. |
| `Watch` (init) | `Action<object?, DynamicFormController>?` | Called with the new value and the controller after the value of this field changes: after `DynamicFormController.SetValue`, after `DynamicFormController.ResetField` when it changes the value, and, for a repeatable list, after a row is added or removed or a cell changes. On a child of a repeatable list it is called with the cell value after `DynamicFormController.SetArrayItemValue`. It does not run for `DynamicFormController.Reset`, which loads a complete new state. |

#### `DynamicFormController`

`Klyroo.Form.Dynamic` · class

Runtime state of a `DynamicFormSchema`: field values, an initial snapshot for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time. Values are `Object`, keyed by field key; `Klyroo.Form` offers `FormController<TModel>` for a form bound to a C# model.

| Constructor | Description |
| :--- | :--- |
| `DynamicFormController(DynamicFormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)` | Creates a controller for `schema` and initializes the state. Values in `initialInput` win over default values, and the result becomes the baseline for dirty tracking. A stepped form starts on its first step that is not skipped. |

| Field | Type | Description |
| :--- | :--- | :--- |
| `const FormErrorKey = "$form"` | `string` | The key `DynamicFormController.SetFormError` uses for the form-level error. It matches no field, so the error counts as form-level. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ActiveFields` | `IReadOnlyList<DynamicFieldSchema>` | The stateful leaf fields in effect now, which are the ones validation checks and `DynamicFormController.BuildOutput` includes: the fields of `DynamicFormController.AllFields` that are visible, inside visible containers and visible tabs, in a visible section, and in the current step or a step that is not skipped. Stateless kinds are left out. |
| `AllFields` | `IReadOnlyList<DynamicFieldSchema>` | Every leaf field of the form: the fields of the steps, else of the sections, else of `DynamicFormSchema.Fields`, with groups, cards, objects and tabs flattened and ignored fields removed, whether they are visible or not. Repeatable lists are leaves; containers never are. |
| `CurrentStep` | `DynamicStepSchema?` | The current step, or `null` for a form without steps. |
| `CurrentStepIndex` | `int` | Zero-based index of the current step. It stays 0 for a form without steps. |
| `CurrentStepKey` | `string?` | The key of the current step, or `null` for a form without steps. |
| `CurrentVisibleTopFields` | `IReadOnlyList<DynamicFieldSchema>` | Top-level fields to render now: those of the current step, of the visible sections, or of the schema, without hidden and ignored fields. |
| `DirtyPaths` | `IReadOnlyList<string>` | Keys whose current value differs from the initial snapshot, excluding `DynamicFormSchema.IgnoreDirtyPaths`. The snapshot is taken at construction, by `DynamicFormController.Reset` and after a successful submission. |
| `Errors` | `IReadOnlyDictionary<string, string>` | Errors to show: validation errors of touched fields, validation errors of touched cells of repeatable lists keyed by `DynamicFormController.CellPath` (for example `lines[0].sku`), plus every error set with `DynamicFormController.SetError`, by key. |
| `Fields` | `IReadOnlyList<IFormFieldAccess>` | The fields of the form as `IFormFieldAccess` views, for an agent or any code that works with values as JSON. Containers, stateless blocks and `FormFieldKind.Hidden` fields are not listed. |
| `FormErrors` | `IReadOnlyList<string>` | The messages of the form-level errors: those set with `DynamicFormController.SetError` on a key that matches no field of the schema, such as `DynamicFormController.FormErrorKey`. |
| `FormKey` | `string` | The identifier of the form: `DynamicFormSchema.FormKey`. |
| `HasBlockingErrors` | `bool` | `true` if a field in effect (see `DynamicFormController.ActiveFields`) or a visible cell of one of its repeatable lists has a validation error, touched or not, or if any error set with `DynamicFormController.SetError` is blocking. Validation errors only exist once validation has run. |
| `HasPendingValidation` | `bool` | `true` while any asynchronous rule (see `DynamicFieldSchema.AsyncValidators`) is waiting for its debounce or running. |
| `InternalState` | `IReadOnlyDictionary<string, object?>` | Live, read-only view of all values by key, including keys of the initial input that match no field. |
| `IsDirty` | `bool` | `true` when `DynamicFormController.DirtyPaths` is not empty. |
| `IsFirstStep` | `bool` | `true` when every step before the current one is skipped by `DynamicStepSchema.SkipWhen`, and for a form without steps. |
| `IsLastStep` | `bool` | `true` when every step after the current one is skipped by `DynamicStepSchema.SkipWhen`, and for a form without steps. The components show the submit button instead of Next on such a step. |
| `IsStepped` | `bool` | `true` when the schema declares steps. |
| `IsSubmitting` | `bool` | `true` while `DynamicFormController.SubmitAsync` is running. |
| `Messages` (get, set) | `FormMessages` | The built-in validation messages in use: the ones set here, else `DynamicFormSchema.Messages`, else the ones a Klyroo.Form component gave for its locale, else English. Setting it fixes the language of this controller: components no longer replace it. Errors already computed are computed again with the new messages. |
| `Schema` | `DynamicFormSchema` | The schema this controller runs. |
| `SubmitFailed` | `bool` | `true` after a submission, or a move to the next step, failed because of validation, a hook or a handler, until a submission succeeds, the step changes or `DynamicFormController.Reset` runs. The components list every field error in the error summary while it is set. |
| `SubmitFailureCount` | `int` | How many times a submission or a move to the next step failed since the controller was created or last reset. The error summary announces once per increase instead of on every edit. |
| `Title` | `string?` | The title of the form: `DynamicFormSchema.Title`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `AddArrayItem(DynamicFieldSchema arrayField)` | `void` | Appends a row to a repeatable list, filling each cell from the child field's default, then revalidates the list if it was touched and calls its `DynamicFieldSchema.Watch`. |
| `BuildOutput()` | `IReadOnlyDictionary<string, object?>` | Builds the dictionary to submit: every field in effect (see `DynamicFormController.ActiveFields`) except those with `DynamicFieldSchema.OmitFromOutput`, with `DynamicFieldSchema.TransformOutput` applied. Fields that are hidden, in a hidden container, tab or section, or in a skipped step are left out, and their values stay in the state. Lists are copied. |
| `CancelPendingValidations()` | `void` | Cancels the asynchronous validations that are waiting for their debounce or running, without changing any value or error. A component calls it when it goes away. |
| `static CellPath(string arrayKey, int index, string childKey)` | `string` | Builds the path that identifies a cell of a repeatable list in `DynamicFormController.Errors` and `DynamicFormController.GetError`: `arrayKey[index].childKey`. |
| `ClearError(string? path = null)` | `void` | Clears errors set with `DynamicFormController.SetError` and validation errors, for one field or for all. Clearing a repeatable list also clears the errors of its cells. |
| `CreateFieldOption(DynamicFieldSchema field, string label)` | `FormOptionItem?` | Creates an option from user text with `DynamicFieldSchema.OnCreateOption`, adds it to the field options and selects it, appending to the value for a field with `DynamicFieldSchema.Multiple`. |
| `Dispose()` | `void` | Cancels the pending asynchronous validations and stops starting new ones. The values and errors stay readable. |
| `ExportState(bool includeSensitive = false)` | `FormDraft` | Takes a copy of the values the user has typed and of the current step, to keep them across a reload or a navigation: store `FormDraft.ToJson` and give the draft back to `DynamicFormController.RestoreDraft` on a controller built from the same initial input. |
| `GetArrayItemError(string arrayKey, int index, string childKey)` | `string?` | Gets the error to show for a cell of a repeatable list. |
| `GetArrayItems(string arrayKey)` | `IReadOnlyList<Dictionary<string, object?>>` | Gets the rows of a repeatable list. |
| `GetError(string path)` | `string?` | Gets the error to show for a field, or for a cell of a repeatable list by its `DynamicFormController.CellPath`. |
| `GetErrorEntries()` | `IReadOnlyList<FormErrorEntry>` | Lists the errors to show, in the order of the form: the errors of the fields in effect and of their cells, then the other errors set with `DynamicFormController.SetError`, form-level ones last. It is built from `DynamicFormController.Errors`, so errors of untouched fields are not listed. |
| `GetFieldOptions(DynamicFieldSchema field)` | `IReadOnlyList<FormOptionItem>` | Gets the options of a field: those from its options factory or its static options, followed by the options added with `DynamicFormController.CreateFieldOption`. |
| `GetInitialValue(string path)` | `object?` | Gets the value a field had in the initial snapshot. |
| `GetSectionStates()` | `IReadOnlyList<FormPageSectionState>` | Computes the completion state of each visible section from the current values and errors. Fields in hidden containers or tabs are not counted. |
| `GetValue(string path)` | `object?` | Gets the current value of a field. |
| `GetValue<T>(string path)` | `T` | Gets the current value converted to `T`. A value already of that type is returned as is. Otherwise it is converted with the invariant culture, in the formats the controls store: `T` may be a `Nullable`, a number (a whole number only becomes an integer type: 3.5 is not an `Int32`), `Boolean`, `Guid`, an enum (by name, ignoring case, or by the number of a member), `DateOnly` (`yyyy-MM-dd`), `TimeOnly` (`HH:mm`), `DateTime` (`yyyy-MM-ddTHH:mm`), `DateTimeOffset` (a value without a zone counts as UTC), `TimeSpan`, a string, or a list of strings. |
| `static IsContainer(FormFieldKind kind)` | `bool` | Tells whether a kind is a layout container whose children are fields of their own: `FormFieldKind.Group`, `FormFieldKind.Card`, `FormFieldKind.Object` and `FormFieldKind.Tabs`. A container never has a value, even without children. |
| `static IsEmptyValue(object? value)` | `bool` | Tells whether a value counts as empty for required checks: `null`, a blank string or an empty collection. `false` and `0` are not empty. |
| `IsFieldDisabled(DynamicFieldSchema field)` | `bool` | Tells whether a field is currently disabled, from `DynamicFieldSchema.Disabled`, `DynamicFieldSchema.DisabledWhen` and `DynamicFormController.IsSubmitting`: every field is disabled while a submission runs, so the user cannot edit values that are being validated or saved. The components render the controls disabled; `DynamicFormController.SetValue` from code still works. |
| `IsFieldRequired(DynamicFieldSchema field)` | `bool` | Tells whether a field is currently required, from `DynamicFieldSchema.Required` and `DynamicFieldSchema.RequiredWhen`. |
| `IsSectionVisible(DynamicSectionSchema section)` | `bool` | Tells whether a section is shown: its `DynamicSectionSchema.Condition` is absent or true. |
| `static IsStateless(FormFieldKind kind)` | `bool` | Tells whether a kind carries no value: `FormFieldKind.Info`, `FormFieldKind.Divider`, `FormFieldKind.Section` and `FormFieldKind.Button`. |
| `IsValidating(string path)` | `bool` | Tells whether an asynchronous rule of a field is waiting for its debounce or running, which the components show as a pending state next to the field. |
| `MarkArrayItemTouched(string arrayKey, int index, string childKey)` | `void` | Marks a cell of a repeatable list touched, which makes its validation error visible, and validates it unless the trigger of its child field is `FormValidationTrigger.Submit`. `KlyFormEngine` calls it when a cell loses focus. |
| `MarkTouched(string path)` | `void` | Marks a field touched, which makes its validation error visible, and validates it unless its trigger is `FormValidationTrigger.Submit`. |
| `NextStepAsync()` | `Task<bool>` | Moves to the next step that is not skipped. It validates the current step, then runs `DynamicFormSchema.OnBeforeNext`, and clears the touched state and validation errors of the new step. |
| `PreviousStepAsync()` | `Task<bool>` | Moves to the previous step that is not skipped, after `DynamicFormSchema.OnBeforePrevious`. Values are kept and the touched state and validation errors are cleared. |
| `RemoveArrayItem(string arrayKey, int index)` | `void` | Removes a row from a repeatable list, moving the errors and touched state of the following rows up, then revalidates the list if it was touched and calls its `DynamicFieldSchema.Watch`. Does nothing when the index is out of range. |
| `Reset(IReadOnlyDictionary<string, object?>? newInput = null)` | `void` | Clears errors and touched state, reloads the state from `newInput`, which also becomes the new dirty-tracking baseline, and returns to the first step that is not skipped. `DynamicFieldSchema.Watch` is not called. |
| `ResetField(string path)` | `void` | Restores a field to its initial value, clears its errors and touched state (and those of its cells for a repeatable list), and calls `DynamicFieldSchema.Watch` when the value changed. |
| `ResetSection(string sectionKey)` | `void` | Resets every field of a section with `DynamicFormController.ResetField`. Does nothing when the key matches no section. |
| `RestoreDraft(FormDraft draft)` | `void` | Puts the values of a draft back into the form, as if the user had typed them. The baseline of dirty tracking is not touched, so the restored fields differ from it: `DynamicFormController.IsDirty` and `DynamicFormController.DirtyPaths` report the draft as unsaved changes, which is what lets a navigation guard protect it. This is what separates it from `DynamicFormController.Reset`, which makes the loaded values the new baseline. Errors and touched state are cleared and `DynamicFieldSchema.Watch` is not called, like `DynamicFormController.Reset`. Keys of the draft that match no value of the form are ignored, as are `FormFieldKind.File` and `FormFieldKind.Custom` fields (and such columns of a list), and a field the draft does not mention keeps its value (the sensitive fields `DynamicFormController.ExportState` leaves out among them). |
| `SetArrayItemValue(string arrayKey, int index, string childKey, object? value, bool markTouched = false)` | `void` | Sets one cell of a repeatable list. Does nothing when the index is out of range. The cell follows the `DynamicFieldSchema.Trigger` of its child field, like `DynamicFormController.SetValue` does for a field: it is validated with the child's rules once touched, unless the trigger is `FormValidationTrigger.Submit`, which clears its error instead. Then the child's `DynamicFieldSchema.Watch` is called with the cell value and the list's with the rows. |
| `SetError(string path, string message, bool blocking = false)` | `void` | Sets an error on a field, for example one returned by the server, and marks the field touched. The error is removed by the next `DynamicFormController.SetValue` on that key, by `DynamicFormController.ClearError` or by `DynamicFormController.Reset`. |
| `SetFormError(string message, bool blocking = true)` | `void` | Sets an error on the form as a whole, for a failure that belongs to no field (a server rejection, a conflict). It is a `DynamicFormController.SetError` on `DynamicFormController.FormErrorKey`, so it shows in the error summary, and it clears with `DynamicFormController.ClearError` or `DynamicFormController.Reset`. |
| `SetValue(string path, object? value, bool markTouched = false)` | `void` | Sets a value, clears any error set with `DynamicFormController.SetError` on the key (and the errors and touched state of the cells when it replaces the rows of a repeatable list), validates the field if it has been touched and its trigger is not `FormValidationTrigger.Submit` (with that trigger, the validation error is cleared instead), calls `DynamicFieldSchema.Watch`, validates again the other touched fields that depend on this one (see `DynamicFieldSchema.DependsOn`) or show an error, and raises `Changed`. |
| `ShouldRenderField(DynamicFieldSchema field)` | `bool` | Tells whether a field is shown: it is not ignored and its `DynamicFieldSchema.Condition` is absent or true. It does not look at the containers, tab, section or step that hold the field; `DynamicFormController.ActiveFields` does. |
| `SubmitAsync()` | `Task<bool>` | Validates every field in effect, runs `DynamicFormSchema.OnBeforeSubmit` then `DynamicFormSchema.OnSubmit`, and on success takes a new initial snapshot so the form is no longer dirty. `DynamicFormController.IsSubmitting` is true meanwhile. Use `DynamicFormController.SubmitAsync` to run your own save step inside the same lifecycle. |
| `SubmitAsync(Func<DynamicFormController, CancellationToken, Task<bool>> handler, CancellationToken cancellationToken = null)` | `Task<bool>` | Submits like `DynamicFormController.SubmitAsync` and runs `handler`, your save step, after `DynamicFormSchema.OnSubmit`, while `DynamicFormController.IsSubmitting` is still `true`. The dirty snapshot is retaken, and the form stops being dirty, only when every step succeeded, so a handler that returns `false` or throws leaves the user's edits dirty and the form editable again. `DynamicFormController.IsSubmitting` goes back to `false` in every case, and a second call while one runs returns `false` at once. |
| `TryGetValue<T>(string path, out T value)` | `bool` | Tries to get the current value converted to `T`, with the conversions of `DynamicFormController.GetValue``. |
| `Validate(bool currentStepOnly = false)` | `bool` | Validates the fields in effect (see `DynamicFormController.ActiveFields`) according to `DynamicFormSchema.ValidateMode`, including each cell of their repeatable lists, and marks them touched so their errors show. Validation errors of fields that are not in effect, such as hidden ones, are removed. |
| `ValidateAsync(bool currentStepOnly = false, CancellationToken cancellationToken = null)` | `Task<bool>` | Validates like `DynamicFormController.Validate`, then runs the asynchronous rules (see `DynamicFieldSchema.AsyncValidators`) of the validated fields that have a non-empty value, pass their synchronous checks and have no result yet for the current value, in parallel across fields, and validates again with their results. `DynamicFormController.SubmitAsync` and `DynamicFormController.NextStepAsync` call it. |

#### `DynamicFormSchema`

`Klyroo.Form.Dynamic` · class

Declarative description of a form: its fields, optional steps or sections, validation mode and submit hooks. A schema is plain data and can be shared between controllers.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Columns` (init) | `int` | Number of columns, from 1 to 12, of the grids that lay out the fields, including those of containers and tabs. Values outside the range are clamped. `DynamicFieldSchema.Span` is clamped to it. The default is 12. |
| `Description` (init) | `string?` | Text shown under the title. |
| `Eyebrow` (init) | `string?` | Small label shown above the title. |
| `Fields` (init) | `IReadOnlyList<DynamicFieldSchema>` | Fields of a single-block form. Ignored when `DynamicFormSchema.Steps` or `DynamicFormSchema.Sections` is not empty. |
| `FormKey` (init) | `string` | Identifier of the form, written to the `data-key` attribute of the rendered form. The default is `form`. |
| `IgnoreDirtyPaths` (init) | `IReadOnlyList<string>?` | Keys excluded from dirty tracking. A key also excludes the keys that start with it followed by a dot. |
| `Messages` (init) | `FormMessages?` | The built-in validation messages of this form. When `null` (the default) the controller uses the English messages, or those of the current locale when a Klyroo.Form component renders it. Set it to fix the language of the form whatever the locale is. |
| `OnBeforeNext` (init) | `Func<DynamicFormController, int, Task<bool>>?` | Runs after the current step validates and before moving forward, with the index of the current step. Return `false` to stay on the step. |
| `OnBeforePrevious` (init) | `Func<DynamicFormController, int, Task<bool>>?` | Runs before moving back, with the index of the current step. Return `false` to stay on the step. |
| `OnBeforeSubmit` (init) | `Func<DynamicFormController, Task<bool>>?` | Runs after validation succeeds and before `DynamicFormSchema.OnSubmit`. Return `false` to cancel the submission, for example after a failed server check. |
| `OnSubmit` (init) | `Func<DynamicFormController, Task>?` | Runs when the form is submitted and validation (and `DynamicFormSchema.OnBeforeSubmit`) succeeded. An exception it throws propagates to the caller of `DynamicFormController.SubmitAsync`. |
| `Sections` (init) | `IReadOnlyList<DynamicSectionSchema>` | Sections of a page form. Used when `DynamicFormSchema.Steps` is empty, and replaces `DynamicFormSchema.Fields`. |
| `ShowStepper` (init) | `bool` | `true` to show the list of steps above a stepped form. The default is `true`. |
| `Steps` (init) | `IReadOnlyList<DynamicStepSchema>` | Steps of a wizard. When not empty, they replace `DynamicFormSchema.Sections` and `DynamicFormSchema.Fields`. |
| `Title` (init) | `string?` | Title shown in the form header. |
| `ValidateMode` (init) | `FormValidationMode` | Which checks validation runs. The default is `FormValidationMode.All`. |

#### `DynamicFormService`

`Klyroo.Form.Dynamic` · class

Opens a form as an overlay and returns its result as a task. Register it with `AddKlyrooForm()` (scoped) and mount one `KlyFormProvider` in the layout so the form has somewhere to render.

| Property | Type | Description |
| :--- | :--- | :--- |
| `ActiveForm` | `DynamicFormController?` | The controller of the open form, or `null` when none is open. |
| `ActiveMode` | `FormShellMode` | The shell mode of the open form. The default is `FormShellMode.Modal`. |
| `IsOpen` | `bool` | `true` while a form is open. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `CloseForm()` | `void` | Closes the open form without submitting and completes the task of `DynamicFormService.CreateFormAsync` with `DynamicFormOutcome.IsCompleted` set to `false`. Does nothing when no form is open. |
| `CreateFormAsync(DynamicFormSchema schema, IReadOnlyDictionary<string, object?>? input = null, FormShellMode mode = Modal)` | `Task<DynamicFormOutcome>` | Opens a form in the mounted `KlyFormProvider` and waits for its result. Opening a form while another is open replaces it, and the replaced form's task completes with `DynamicFormOutcome.IsCompleted` set to `false` and that form's output, as if it had been closed. |
| `SubmitActiveFormAsync()` | `Task<bool>` | Submits the open form with `DynamicFormController.SubmitAsync`. On success it closes the form and completes the task of `DynamicFormService.CreateFormAsync`. |

#### `DynamicSectionSchema`

`Klyroo.Form.Dynamic` · class

One section of a sectioned form, declared in `DynamicFormSchema.Sections` and rendered by `KlyFormPage`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Condition` (init) | `Func<DynamicFormController, bool>?` | Returns `false` to hide the section and leave it out of `DynamicFormController.GetSectionStates`. Its fields are skipped by validation and left out of `DynamicFormController.BuildOutput`, and their values stay in the state. |
| `Description` (init) | `string?` | Text shown under the title. |
| `Fields` (init) | `IReadOnlyList<DynamicFieldSchema>` | Fields of the section. |
| `Key` (init) | `string` | Unique key of the section. It is also the anchor id of the section in `KlyFormPage`. |
| `Label` (init) | `string` | Title of the section. |
| `Optional` (init) | `bool` | `true` if the section may stay empty: it then reports `FormPageSectionStatus.Optional` until a field has a value. |
| `Span` (init) | `int` | Number of columns, from 1 to 12, the section occupies in the 12-column grid of sections of `KlyFormPage`. Values outside the range are clamped. The default is 12, one section per row. |

#### `DynamicStepSchema`

`Klyroo.Form.Dynamic` · class

One step of a multi-step form, declared in `DynamicFormSchema.Steps`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Optional text shown under the stepper while the step is current. |
| `Fields` (init) | `IReadOnlyList<DynamicFieldSchema>` | Fields of the step. |
| `Key` (init) | `string` | Unique key of the step. |
| `SkipWhen` (init) | `Func<DynamicFormController, bool>?` | Returns `true` to skip the step: the controller starts on the first step that is not skipped, `DynamicFormController.NextStepAsync` and `DynamicFormController.PreviousStepAsync` pass over it, the stepper does not list it, and its fields are skipped by validation and left out of `DynamicFormController.BuildOutput`. The current step is never treated as skipped. |
| `Title` (init) | `string` | Title shown in the stepper. |

#### `DynamicTabSchema`

`Klyroo.Form.Dynamic` · class

One tab of a `FormFieldKind.Tabs` field.

| Property | Type | Description |
| :--- | :--- | :--- |
| `Condition` (init) | `Func<DynamicFormController, bool>?` | Returns `false` to hide the tab. A hidden tab's fields are skipped by validation and left out of `DynamicFormController.BuildOutput`, and their values stay in the state. |
| `Fields` (init) | `IReadOnlyList<DynamicFieldSchema>` | Fields shown when the tab is selected. |
| `Key` (init) | `string` | Unique key of the tab among its siblings. |
| `Label` (init) | `string` | Text shown on the tab. |

### Records

#### `FormErrorEntry`

`Klyroo.Form` · record

One error to show, as listed by `DynamicFormController.GetErrorEntries`: a field, a cell of a repeatable list or the form as a whole.

| Constructor | Description |
| :--- | :--- |
| `FormErrorEntry(string Path, string Message, string? FieldKey, int? RowIndex, string? ChildKey)` | One error to show, as listed by `DynamicFormController.GetErrorEntries`: a field, a cell of a repeatable list or the form as a whole. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `ChildKey` (init) | `string?` | Key of the child field of the cell, or `null` when the error is not on a cell. |
| `FieldKey` (init) | `string?` | Key of the field that holds the error: the field itself, or the repeatable list for a cell. `null` for a form-level error. |
| `IsFormLevel` | `bool` | `true` when the error belongs to the form as a whole and not to a field: its key matches no field of the schema. |
| `Message` (init) | `string` | The error message. |
| `Path` (init) | `string` | The key in `DynamicFormController.Errors`: a field key, a `DynamicFormController.CellPath` or the key given to `DynamicFormController.SetError`. |
| `RowIndex` (init) | `int?` | Zero-based row of the cell, or `null` when the error is not on a cell. |

#### `FormMessages`

`Klyroo.Form` · record

The built-in validation messages of `DynamicFormController`, which has no UI and so no locale of its own: the required, number bounds, text length, date range and file errors, and the errors the typed model binding adds for integers, numbers out of range, identifiers and values that are not of the type of the member. A new instance holds the English messages. Build a variant with a `with` expression, give it to the controller with `DynamicFormSchema.Messages` or `DynamicFormController.Messages`, or pick one of the twelve built-in sets (`FormMessageLocales`). The Blazor components do it for you: they give the controller the messages of the current Klyroo.UI locale, see `FormTexts`.

| Constructor | Description |
| :--- | :--- |
| `FormMessages()` | Creates the messages with their English defaults. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `DateRangeOrder` (init) | `string` | The error of a `FormFieldKind.DateRange` whose end is before its start (`"The end date must not be before the start date"`). |
| `static Default` | `FormMessages` | The English messages, shared and read-only. |
| `FileSize` (init) | `Func<long, string>` | Formats a size for a message and for the file list, with the decimal separator and the unit names of the language (`"1.5 MB"`, `"1,5 Mo"`). The default is `FormFileValue.FormatSize`. |
| `FileTooLarge` (init) | `Func<string, string, string>` | The error of a `FormFieldKind.File` file above `DynamicFieldSchema.MaxFileSize`, given the file name and the limit as text (`"report.pdf is larger than 5 MB"`). |
| `FileTypeNotAllowed` (init) | `Func<string, string, string>` | The error of a `FormFieldKind.File` file that does not match `DynamicFieldSchema.Accept`, given the file name and the accepted list (`"photo.gif is not an accepted type (.png,.jpg)"`). |
| `InvalidIdentifier` (init) | `string` | The error of a `Guid` member of a bound model whose text is not an identifier (`"Enter a valid identifier"`). Added by the typed schema, see `FormSchema.For`. |
| `InvalidValue` (init) | `string` | The error of a field of a typed form whose control holds something that is not a value of the type of the member: text in a date field (from a draft, or a custom control that writes free text), two texts in a date range (`"Enter a valid value"`). The messages above keep their own wording for numbers and identifiers. |
| `MaxLength` (init) | `Func<int, string>` | The error of a text longer than `DynamicFieldSchema.MaxLength`, given the limit (`"Must be at most 20 characters"`). |
| `MaxValue` (init) | `Func<double, string>` | The error of a `FormFieldKind.Number` above `DynamicFieldSchema.Max`, given the bound (`"Value must be at most 10"`). |
| `MinValue` (init) | `Func<double, string>` | The error of a `FormFieldKind.Number` below `DynamicFieldSchema.Min`, given the bound (`"Value must be at least 3"`). |
| `NumberOutOfRange` (init) | `Func<string, string, string>` | The error of a numeric member of a bound model whose value does not fit the type of the member, given the lowest and the highest value of the type as exact invariant text (`"Enter a number between -128 and 127"`). Added by the typed schema, see `FormSchema.For`. |
| `Required` (init) | `string` | The error of a required field that has no value (`"This field is required"`). `DynamicFieldSchema.RequiredMessage` wins over it. |
| `SingleFileOnly` (init) | `string` | The error of a `FormFieldKind.File` field without `DynamicFieldSchema.Multiple` that holds several files (`"Choose one file only"`). |
| `WholeNumber` (init) | `string` | The error of an integer member of a bound model whose value is not a whole number (`"Enter a whole number"`). Added by the typed schema, see `FormSchema.For`. |

#### `FormOption<TValue>`

`Klyroo.Form` · record

One choice of a choice field of a typed form: the value stored in the model, and the text shown to the user.

| Constructor | Description |
| :--- | :--- |
| `FormOption<TValue>(TValue Value, string Label, string? Description = null, string? Group = null, string? Icon = null, bool Disabled = false)` | One choice of a choice field of a typed form: the value stored in the model, and the text shown to the user. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Description` (init) | `string?` | Optional secondary text, shown under the label in radio and checkbox lists. |
| `Disabled` (init) | `bool` | `true` to show the option but prevent selecting it. |
| `Group` (init) | `string?` | Optional group name: options with the same group are rendered together. |
| `Icon` (init) | `string?` | Optional icon name, shown before the label in radio and checkbox lists. |
| `Label` (init) | `string` | The text shown to the user. |
| `Value` (init) | `TValue` | The value stored in the model when the option is selected. |

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

Completion state of one section, computed from the current values by `DynamicFormController.GetSectionStates`.

| Constructor | Description |
| :--- | :--- |
| `FormPageSectionState(string Key, string Label, string? Description, bool Optional, int RequiredCount, int MissingCount, bool Dirty, FormPageSectionStatus Status)` | Completion state of one section, computed from the current values by `DynamicFormController.GetSectionStates`. |

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

#### `DynamicAsyncValidatorRule`

`Klyroo.Form.Dynamic` · record

An asynchronous validation rule of a field, for a check that needs I/O such as "is this username free". It runs in the background once the user has left the field (and again, debounced, as they edit it), a newer run cancels an older one, and `DynamicFormController.SubmitAsync` and `DynamicFormController.NextStepAsync` wait for it. It does not run for an empty value or for a value that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list.

| Constructor | Description |
| :--- | :--- |
| `DynamicAsyncValidatorRule(string Key, Func<object?, DynamicFormController, CancellationToken, ValueTask<string?>> Validate)` | An asynchronous validation rule of a field, for a check that needs I/O such as "is this username free". It runs in the background once the user has left the field (and again, debounced, as they edit it), a newer run cancels an older one, and `DynamicFormController.SubmitAsync` and `DynamicFormController.NextStepAsync` wait for it. It does not run for an empty value or for a value that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Debounce` (init) | `TimeSpan` | How long the value must stay unchanged before a background run starts, so typing does not call the service on every key. The default is 300 milliseconds. Submission runs the rule at once. |
| `Key` (init) | `string` | Identifier of the rule. |
| `Validate` (init) | `Func<object?, DynamicFormController, CancellationToken, ValueTask<string?>>` | Receives the current field value, the controller and a token that is cancelled when the value changes or a newer run starts. Returns the error message, or `null` when the value is valid. An exception it throws in a background run is swallowed and the value stays unchecked until submission, where the exception propagates to the caller. |

#### `DynamicFormOutcome`

`Klyroo.Form.Dynamic` · record

Result of an overlay opened with `DynamicFormService.CreateFormAsync`.

| Constructor | Description |
| :--- | :--- |
| `DynamicFormOutcome(bool IsCompleted, IReadOnlyDictionary<string, object?> FormData)` | Result of an overlay opened with `DynamicFormService.CreateFormAsync`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `FormData` (init) | `IReadOnlyDictionary<string, object?>` | The output of the form at that moment, as returned by `DynamicFormController.BuildOutput`. |
| `IsCompleted` (init) | `bool` | `true` when the form was submitted successfully, `false` when it was closed without submitting. |

#### `DynamicValidatorRule`

`Klyroo.Form.Dynamic` · record

A custom validation rule of a field. Rules run in order after the built-in checks and the first failing rule sets the field error.

| Constructor | Description |
| :--- | :--- |
| `DynamicValidatorRule(string Key, string Message, Func<object?, DynamicFormController, bool> Validate)` | A custom validation rule of a field. Rules run in order after the built-in checks and the first failing rule sets the field error. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Key` (init) | `string` | Identifier of the rule. |
| `LocalizedMessage` (init) | `Func<FormMessages, string?>?` | Computes the error message when the rule fails, from the `DynamicFormController.Messages` of the controller, so the message follows the language and the culture of the moment of validation instead of the ones of the moment the rule was created. When it is `null` or returns `null`, `DynamicValidatorRule.Message` is used. |
| `Message` (init) | `string` | Error message set on the field when the rule fails. |
| `Validate` (init) | `Func<object?, DynamicFormController, bool>` | Receives the current field value and the controller, and returns `true` when the value is valid. It also runs for empty values of fields that are not required, so handle `null`. |

### Structs

#### `FormDateRange`

`Klyroo.Form` · struct

A range of two dates, the value of a `FormFieldKind.DateRange` field. Either end can be missing while the user is still choosing, and validation rejects an end before the start.

| Constructor | Description |
| :--- | :--- |
| `FormDateRange(DateOnly? Start, DateOnly? End)` | A range of two dates, the value of a `FormFieldKind.DateRange` field. Either end can be missing while the user is still choosing, and validation rejects an end before the start. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `End` (init) | `DateOnly?` | The last day, or `null` when none is chosen. |
| `Start` (init) | `DateOnly?` | The first day, or `null` when none is chosen. |

#### `FormWriteResult`

`Klyroo.Form` · struct

The result of writing a value into a field through `IFormFieldAccess`.

| Constructor | Description |
| :--- | :--- |
| `FormWriteResult(bool Ok, string? Error = null)` | The result of writing a value into a field through `IFormFieldAccess`. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `Error` (init) | `string?` | Why the value was refused, or `null` when it was stored. |
| `Ok` (init) | `bool` | `true` when the value was stored. |
| `static Success` | `FormWriteResult` | A write that stored its value. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static Fail(string error)` | `FormWriteResult` | A write that was refused. |

### Interfaces

#### `IFormFieldAccess`

`Klyroo.Form` · interface

One field of an `IFormRuntime`, seen as text and JSON so that code that does not know the type of its value can read and write it.

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsDisabled` | `bool` | `true` when the field cannot be edited now. |
| `IsHidden` | `bool` | `true` when the field is not in effect now (its condition is false, it is in a hidden container, tab or section, or in a skipped step), so validation and the output ignore it. |
| `IsOffPage` | `bool` | `true` when the field is in effect but belongs to a step other than the current one. |
| `IsRequired` | `bool` | `true` when the field must have a value now. |
| `Key` | `string` | The key of the field. |
| `Kind` | `FormFieldKind` | The kind of control the field uses. |
| `Label` | `string` | The label of the field, or its key when it has none. |
| `Options` | `IReadOnlyList<FormOptionItem>` | The options of a choice field, with their values as text; empty for any other field. |
| `Sensitive` | `bool` | `true` for a password or a one-time code: `IFormFieldAccess.WriteValue` writes `null` for it. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `Clear()` | `FormWriteResult` | Empties the field: `false` for a checkbox, an empty list for a multiple choice, no value for the others. |
| `Reset()` | `void` | Puts the field back to its baseline value and clears its error. |
| `Toggle()` | `FormWriteResult` | Flips a checkbox or a switch. |
| `TrySet(JsonElement value)` | `FormWriteResult` | Sets the value from JSON, the way a person would type it: numbers keep their exact digits, a choice is matched by value or by label, a date is read as `yyyy-MM-dd`. The value is checked against the field before it is stored. |
| `WriteValue(Utf8JsonWriter writer)` | `void` | Writes the current value as JSON: a string, a number, a boolean, a list of strings or `null`. A sensitive field writes `null`. |

#### `IFormRuntime`

`Klyroo.Form` · interface

What a form exposes to code that drives it without knowing its model type, such as an agent bridge: the form itself, its fields as `IFormFieldAccess` and the actions a person has on a form. `FormController<TModel>` and `DynamicFormController` implement it.

| Property | Type | Description |
| :--- | :--- | :--- |
| `CurrentStepKey` | `string?` | The key of the current step, or `null` for a form without steps. |
| `Fields` | `IReadOnlyList<IFormFieldAccess>` | The fields a person fills in: every stateful leaf field of the form, whether it is visible now or not. Containers, stateless blocks and hidden inputs are not listed. |
| `FormKey` | `string` | The identifier of the form, as set on the schema. |
| `IsDirty` | `bool` | `true` when a value differs from the baseline taken at creation, by a reset or after a successful submission. |
| `IsStepped` | `bool` | `true` when the schema declares steps. |
| `Title` | `string?` | The title of the form, or `null`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `NextStepAsync()` | `Task<bool>` | Moves to the next step, after validating the current one. |
| `PreviousStepAsync()` | `Task<bool>` | Moves to the previous step. |
| `Reset()` | `void` | Puts every field back to the baseline value and clears errors. |
| `SubmitAsync()` | `Task<bool>` | Validates the form and runs its submission hooks, like the submit button does. |

### Enums

#### `FormFieldKind`

`Klyroo.Form` · enum

Kind of a field in a `DynamicFieldSchema`. The kind decides how the field is stored, validated and rendered. `KlyFormEngine`, `KlyFormPage` and `KlyFormField` render every kind with a dedicated control.

| Value | Description |
| :--- | :--- |
| `Text` | Single-line text input. The value is a string. |
| `Password` | Masked text input with a show/hide toggle. Rules in `DynamicFieldSchema.PasswordRequirements` are checked on validation. |
| `Textarea` | Multi-line text input. The value is a string. |
| `Number` | Number input. The value is a `Double`, and `DynamicFieldSchema.Min` and `DynamicFieldSchema.Max` are enforced on validation. |
| `Checkbox` | Checkbox. The value is a `Boolean`. |
| `Switch` | Checkbox exposed to assistive technology as a switch (`role="switch"`). The value is a `Boolean`. |
| `Radio` | Radio group built from the field options. The value is the selected option value. |
| `CheckboxGroup` | Multiple choice rendered as a list of checkboxes built from the field options. The value is a `List<string>` of the checked option values. |
| `Select` | Native select list built from the field options, with an `optgroup` per `FormOptionItem.Group`. The value is the selected option value. With `DynamicFieldSchema.Multiple` it renders a list of checkboxes and the value is a `List<string>`. |
| `AutoComplete` | Text input that suggests the option labels through a `datalist`. The value is the value of the option whose label matches the text (ignoring case) or whose value equals it, otherwise the text itself; the typed text stays on screen until the input loses focus. With `DynamicFieldSchema.Multiple` it renders a list of checkboxes and the value is a `List<string>`. |
| `Date` | Date input (`type="date"`). The value is the string produced by the browser, in `yyyy-MM-dd` form. |
| `DateTime` | Date and time input (`type="datetime-local"`). The value is the string produced by the browser, in `yyyy-MM-ddTHH:mm` form. |
| `DateRange` | Range of two dates, rendered as a start and an end date input. The value is a `List<string>` of two items, start then end, in `yyyy-MM-dd` form, with an empty string for a missing date, or `null` when both are empty. A required range needs both dates, and validation rejects an end before the start. |
| `Time` | Time input (`type="time"`). The value is the string produced by the browser. |
| `PhoneNumber` | Phone number input (`type="tel"`, `autocomplete="tel"`). The value is a string; the format is not validated. |
| `Slider` | Range slider (`type="range"`) bounded by `DynamicFieldSchema.Min`, `DynamicFieldSchema.Max` and `DynamicFieldSchema.Step`. The value is a `Double`. While the value is `null` the slider shows `DynamicFieldSchema.Min` (or 0) and the state stays `null` until the user moves it. Bounds are only enforced on validation for `FormFieldKind.Number` fields. |
| `Rating` | Star rating from 1 to `DynamicFieldSchema.Max` (5 by default), rendered as a group of radio buttons. The value is an `Int32`. |
| `ColorPicker` | Color input (`type="color"`). The value is the string produced by the browser, in `#rrggbb` form. |
| `OneTimeCode` | One-time code input (`autocomplete="one-time-code"`, `inputmode="numeric"`, `maxlength` from `DynamicFieldSchema.MaxLength`). The value is a string. |
| `Tag` | List of free-form tags: Enter or a comma in the text input adds the typed tag, and each tag has a remove button. The value is a `List<string>` without blanks or duplicates. |
| `Hidden` | Hidden input. The value is stored and included in the output but nothing is shown to the user. |
| `Info` | Read-only text block showing `DynamicFieldSchema.Label` and `DynamicFieldSchema.DisplayText` (or `DynamicFieldSchema.Description`). It is stateless: it has no value, is never validated and never appears in the output. |
| `Divider` | Horizontal rule. Stateless, like `FormFieldKind.Info`. |
| `Section` | Heading made of `DynamicFieldSchema.Label` and `DynamicFieldSchema.Description`. Stateless, like `FormFieldKind.Info`. |
| `Button` | Button labelled by `DynamicFieldSchema.Label` that runs `DynamicFieldSchema.OnClick` and never submits the form. Stateless: it has no value, is never validated and never appears in the output. |
| `Group` | Container that lays out the fields in `DynamicFieldSchema.Fields` without a border. The child fields keep their own keys and are validated and output as top-level fields, unless the container is hidden. A container is layout only: without `DynamicFieldSchema.Fields` it renders nothing and has no value. |
| `Card` | Bordered container for the fields in `DynamicFieldSchema.Fields`. Children behave as in `FormFieldKind.Group`. |
| `Tabs` | Tabbed container driven by `DynamicFieldSchema.Tabs`. The fields of every visible tab are validated and output, whether or not the tab is selected; the fields of a hidden tab are not. Without tabs it renders nothing and has no value. |
| `Object` | Container with the same behavior as `FormFieldKind.Group`. Child values are not nested under the object key. |
| `ArrayList` | Repeatable list of rows, each row holding the fields declared in `DynamicFieldSchema.Fields`. The value is a `List<Dictionary<string, object?>>`. Rows are edited with `AddArrayItem`, `SetArrayItemValue` and `RemoveArrayItem`, each cell is rendered with the control of its child kind, and each cell is validated with the rules of its child field. |
| `ArrayTable` | Same value and behavior as `FormFieldKind.ArrayList`; `KlyFormEngine` renders both identically. |
| `Custom` | A control you provide. `DynamicFieldSchema.CustomKey` names its renderer, a `RenderFragment<FormControlContext>` registered with `KlyrooFormOptions.CustomField` or passed in the `FieldTemplates` parameter of the components. The field is stateful like any input: it holds any value you store, is validated with `DynamicFieldSchema.Required` and `DynamicFieldSchema.Validators`, and is part of the output. The components render its label, description, help and error around your control. |
| `File` | File input rendered with the Blazor `InputFile`. The value is a `FormFileValue` (or `null`), or a `List<FormFileValue>` with `DynamicFieldSchema.Multiple`. `DynamicFieldSchema.Accept`, `DynamicFieldSchema.MaxFileSize` and the number of files are checked on validation. A file is never part of a draft (`DynamicFormController.ExportState`), since its content cannot be restored. |

#### `FormInfoKind`

`Klyroo.Form` · enum

How an info block (`FormFieldKind.Info`) is drawn.

| Value | Description |
| :--- | :--- |
| `Callout` | A tinted box with a border (the default). |
| `Text` | Plain text without a box. |

#### `FormInfoTone`

`Klyroo.Form` · enum

The color of an info block (`FormFieldKind.Info`), from the Klyroo.UI theme.

| Value | Description |
| :--- | :--- |
| `Neutral` | The muted default. |
| `Info` | The info color. |
| `Success` | The success color. |
| `Warning` | The warning color. |
| `Error` | The error color. |

#### `FormPageSectionStatus`

`Klyroo.Form` · enum

Completion status of a section in a sectioned form, computed by `DynamicFormController.GetSectionStates`.

| Value | Description |
| :--- | :--- |
| `Complete` | Every required field of the section has a value and no field has an error. |
| `Pending` | At least one required field of the section is empty. |
| `Invalid` | At least one field of the section has an error. This takes precedence over the other statuses. |
| `Optional` | The section is marked optional and none of its fields has a value. |

#### `FormShellMode`

`Klyroo.Form` · enum

Layout hint for the shell that hosts a form, used by `DynamicFormService` overlays and the `Shell` parameter of `KlyFormEngine`.

| Value | Description |
| :--- | :--- |
| `Inline` | Renders in the page flow. This is the default of `KlyFormEngine`. |
| `Modal` | Renders in a centered dialog. This is the default of `DynamicFormService.CreateFormAsync`. |
| `Drawer` | Side panel. `KlyFormProvider` renders it as a full-height panel docked to the end (right in left-to-right layouts) of the viewport. |
| `Fullscreen` | Full viewport. `KlyFormProvider` renders it over the whole viewport, without margins or rounded corners. |

#### `FormValidationMode`

`Klyroo.Form` · enum

Controls which checks a `DynamicFormController` runs when it validates, set through `DynamicFormSchema.ValidateMode`.

| Value | Description |
| :--- | :--- |
| `All` | Runs required checks, number bounds, password requirements and custom validators. |
| `RequiredOnly` | Runs required checks only. |
| `ValidatorsOnly` | Runs number bounds, password requirements and custom validators, and skips required checks. |
| `None` | Runs no rule. `DynamicFormController.Validate` returns `true` unless a blocking error set with `DynamicFormController.SetError` exists or, in a typed form, a control holds something that is not a value of the type of its member (`FormMessages.InvalidValue`), which no write could use. |

#### `FormValidationTrigger`

`Klyroo.Form` · enum

Controls when a field is validated while the user edits it, set through `DynamicFieldSchema.Trigger`.

| Value | Description |
| :--- | :--- |
| `Blur` | Validates once the field has been touched, which `KlyFormEngine` does when the control loses focus. This is the default. |
| `Input` | Marks the field touched and validates it on every change. |
| `Submit` | Does not validate while editing. The field is validated by `DynamicFormController.Validate` and by submission, and a change clears its validation error without validating again. |

### Delegates

#### `FormFieldConvention`

`Klyroo.Form` · delegate

Adjusts the settings of a field declared from a model member. Conventions run after the defaults the type of the member gives and before the settings written in the `Field` call, so an explicit setting always wins. `UseDataAnnotations()` is one.

## Klyroo.Form

Components (13) · Static classes and extensions (2) · Classes (2) · Records (2)

### Components

#### `KlyFieldTemplate<TModel, TValue>`

`Klyroo.Form` · component · inherits `ComponentBase`

Draws the control of one field of a typed form with your markup. Put it inside `KlyFormEngine<TModel>` (or `KlyFormPage<TModel>`), name the field with its handle, and read the `FormControlContext` it gives (`Value`, `Set`, `Commit`, `Id`, `Error`, `Disabled`, `Blur`): the component still draws the label, description, help and error around your control. The template replaces the control of any field, so a field you declared with `AsCustom()` needs one, and a built-in kind can be swapped for your own. `TModel` and `TValue` are inferred from `Field`. It renders nothing itself.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment<FormControlContext<TValue>>` | Draws the control from the context of the field: its value typed as the member of the model, how to change it, and the ids and state the control needs. |
| `Field` | `FormField<TModel, TValue>` | The field whose control this template draws: the handle the schema gave. `TModel` and `TValue` are inferred from it. |
| `Form` | `FormController<TModel>?` | The controller of the form. When it is not set, the one of the enclosing `KlyFormEngine` or `KlyFormPage` is used. |

#### `KlyFormEngine<TModel>`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a `FormSchema` as a form: header, optional stepper with the description of the current step, a field grid of columns and the footer actions (Back, Reset, Cancel, Next, Submit). Pass either a `FormController` through `Form` or a schema through `Schema`, with the `Model` to edit. `TModel` is inferred from `Form`, `Schema` or `Model`. The component subscribes to `Changed` and re-renders on every change, and it gives the controller to the components inside it (`KlyFieldTemplate`, `KlyFormFieldView`).

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Templates of the fields (`KlyFieldTemplate`). They draw nothing themselves: they hand their control to the field they name. |
| `Form` | `FormController<TModel>?` | The controller to render. Use it when you need to read or change the form from code. Takes precedence over `KlyFormEngine.Schema`. |
| `Model` | `TModel` | The model to edit. Its values are the form's starting values, and a submit writes the values back into it (all or nothing, put back when a step fails), so members the form does not show keep their value. Without a model every submit creates a new `TModel`. A different instance reloads the form, dropping unsaved edits. It is only read by the controller the component creates: with `KlyFormEngine.Form`, it is only the target of a submit. |
| `OnCancel` | `EventCallback` | Called when the user presses Cancel or the close button. When bound, the Cancel button is shown. |
| `OnSave` | `Func<TModel, CancellationToken, Task<bool>>?` | Your save step: called with the model after validation and the schema hooks passed (the `KlyFormEngine.Model` with the values written into it, or a new one), while the form is still submitting (the button shows its saving state, a second submit is refused, Cancel is disabled). Return `true` when the save succeeded; the form is then clean and `KlyFormEngine.OnSubmit` is raised. Return `false` to keep the edits dirty and the form open, after reporting the reason with `FormController.SetError` or `FormController.SetFormError`; the model gets its previous values back. The token is cancelled when the component is disposed. It runs through `FormController.SubmitAsync`. |
| `OnSubmit` | `EventCallback<TModel>` | Called with the model after the form is submitted successfully, that is after validation, the schema hooks and `KlyFormEngine.OnSave` passed. At that point the submission is over: `FormController.IsSubmitting` is `false` and the form is clean. Do the save itself in `KlyFormEngine.OnSave`. |
| `Schema` | `FormSchema<TModel>?` | The schema to render when no `KlyFormEngine.Form` is given. The component creates and owns the controller, and creates a new one when the schema instance changes. |
| `Shell` | `FormShellMode` | How the form is presented. Anything other than `FormShellMode.Inline` adds a close button to the header when `KlyFormEngine.OnCancel` is bound. |
| `ShowReset` | `bool` | Shows a reset button in the footer while the form is dirty. The default is `true`. |
| `SubmitLabel` | `string?` | Text of the submit button. The default is the `Submit` text of the current locale (`FormTexts.Submit`). |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `EffectiveController` | `FormController<TModel>?` | The controller in use: `KlyFormEngine.Form` if set, otherwise the one the component created from `KlyFormEngine.Schema`, or `null` when neither is set. |

#### `KlyFormErrorSummary`

`Klyroo.Form` · component · inherits `KlyComponentBase`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Form` | `IFormRuntime` | The controller whose errors to list: a `FormController` or a `DynamicFormController`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormFieldView<TModel>`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders one field of a typed form with the control of its kind, its label, description, hint, help, error and per-field reset, the way `KlyFormEngine<TModel>` and `KlyFormPage<TModel>` render their fields, for a layout of your own. Containers, tabs and repeatable lists render their children too. Place it in a CSS grid, since the field sets its own `grid-column` span. It renders nothing when the field is not visible (`FormController.IsVisible`). It does not subscribe to `Changed`: render it from a component that re-renders on that event (`KlyFormEngine` does), so changes made elsewhere show up.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Field` | `FormItem<TModel>` | The field or layout element to render: the handle the schema gave. `TModel` is inferred from it. |
| `Form` | `FormController<TModel>?` | The controller that holds the value of the field. When it is not set, the one of the enclosing `KlyFormEngine` or `KlyFormPage` is used, or the one of a `CascadingValue`. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormNavigationGuard`

`Klyroo.Form` · component · inherits `KlyComponentBase`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ConfirmExternalNavigation` | `bool` | Asks the browser to confirm leaving the site, reloading or closing the tab while the form is dirty. The browser shows its own text; it cannot be changed. The default is `true`. |
| `ConfirmLeave` | `Func<LocationChangingContext, Task<bool>>?` | Called when the user navigates inside the app while the form is dirty. Return `true` to let the navigation go ahead and `false` to stay, for example after asking in a `KlyModal`. Without it the guard asks with `window.confirm` and `KlyFormNavigationGuard.Message`. |
| `Disabled` | `bool` | Turns the guard off, for example while the page saves and navigates on its own. The default is `false`. |
| `Form` | `IFormRuntime` | The controller to protect: a `FormController` or a `DynamicFormController`. Navigation is blocked while `IFormRuntime.IsDirty` is `true`, so the guard stops once a submission succeeds and the snapshot is retaken. |
| `Message` | `string?` | The question of the default confirmation. The default is the `UnsavedChanges` text of the current locale (`FormTexts.UnsavedChanges`). |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyFormPage<TModel>`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Renders a sectioned typed form as a page: a sidebar listing each section with its completion status, and one card per section laid out by its span. Fields render with the same controls, labels, containers and repeatable lists as `KlyFormEngine<TModel>`, and the page is a `form` element, so pressing Enter in a field submits it. `TModel` is inferred from `Form`, `Schema` or `Model`.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | Templates of the fields (`KlyFieldTemplate`). They draw nothing themselves: they hand their control to the field they name. |
| `Form` | `FormController<TModel>?` | The controller to render. Its schema must declare sections (`Section`). Takes precedence over `KlyFormPage.Schema`. |
| `Model` | `TModel` | The model to edit. Its values are the form's starting values, and a submit writes the values back into it (all or nothing, put back when a step fails), so members the form does not show keep their value. Without a model every submit creates a new `TModel`. A different instance reloads the form, dropping unsaved edits. |
| `OnSave` | `Func<TModel, CancellationToken, Task<bool>>?` | Your save step: called with the model after validation and the schema hooks passed, while the form is still submitting. Return `true` when the save succeeded; the form is then clean and `KlyFormPage.OnSubmit` is raised. Return `false` to keep the edits dirty, after reporting the reason with `FormController.SetError` or `FormController.SetFormError`. The token is cancelled when the component is disposed. |
| `OnSubmit` | `EventCallback<TModel>` | Called with the model after the form is submitted successfully, that is after validation, the schema hooks and `KlyFormPage.OnSave` passed. The submission is over at that point and the form is clean. |
| `Schema` | `FormSchema<TModel>?` | The schema to render when no `KlyFormPage.Form` is given. It must declare sections. The component creates and owns the controller, and creates a new one when the schema instance changes. |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `EffectiveController` | `FormController<TModel>?` | The controller in use: `KlyFormPage.Form` if set, otherwise the one the component created from `KlyFormPage.Schema`, or `null` when neither is set. |

#### `KlyFormProvider`

`Klyroo.Form` · component · inherits `KlyComponentBase`

Host of the overlay forms opened with `FormService` (typed) and `DynamicFormService` (runtime). Mount one instance in a layout, wrapping the page content, and call `FormService.CreateFormAsync`` or `DynamicFormService.CreateFormAsync` from anywhere in the same scope.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | The content of the provider, usually the page body. Overlay forms render after it. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDynamicFieldView`

`Klyroo.Form.Dynamic` · component · inherits `KlyComponentBase`

Renders one field of a `DynamicFormController` with the control of its `FormFieldKind`, its label, description, hint, help, error and per-field reset, the way `KlyDynamicFormEngine` and `KlyDynamicFormPage` render their fields. Containers, tabs and repeatable lists render their children too. Place it in a CSS grid of `DynamicFormSchema.Columns` columns, since the field sets its own `grid-column` span. It renders nothing when `DynamicFormController.ShouldRenderField` is `false`. It does not subscribe to `Changed`: render it from a component that re-renders on that event, so changes made elsewhere (by code or by another field) show up.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Field` | `DynamicFieldSchema?` | The field to render, usually one of `DynamicFormController.CurrentVisibleTopFields`. Containers render their children too. |
| `FieldTemplates` | `IReadOnlyDictionary<string, RenderFragment<DynamicControlContext>>?` | Renderers of the `FormFieldKind.Custom` fields by `DynamicFieldSchema.CustomKey`. They win over the ones registered with `KlyrooFormOptions.CustomField`, and reach the fields inside containers, tabs and lists. |
| `Form` | `DynamicFormController` | The controller that holds the value of the field. |
| `Key` | `string?` | The key of the field to render, as an alternative to `KlyDynamicFieldView.Field`: the field of `KlyDynamicFieldView.Form` with this key. `KlyDynamicFieldView.Field` wins when both are set. Nothing renders when the form has no field with this key. |

Also accepts the parameters of `KlyComponentBase`.

#### `KlyDynamicFormEngine`

`Klyroo.Form.Dynamic` · component · inherits `KlyComponentBase`

Renders a `DynamicFormSchema` as a form: header, optional stepper with the description of the current step, a field grid of `DynamicFormSchema.Columns` columns and the footer actions (Back, Reset, Cancel, Next, Submit). Pass either a `DynamicFormController` through `Form` or a schema through `Schema`. The component subscribes to `Changed` and re-renders on every change.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `FieldTemplates` | `IReadOnlyDictionary<string, RenderFragment<DynamicControlContext>>?` | Renderers of the `FormFieldKind.Custom` fields by `DynamicFieldSchema.CustomKey`, each a `RenderFragment<DynamicControlContext>` that draws the control. They win over the ones registered with `KlyrooFormOptions.CustomField`. |
| `Form` | `DynamicFormController?` | The controller to render. Use it when you need to read or change the form from code. Takes precedence over `KlyDynamicFormEngine.Schema`. |
| `Input` | `IReadOnlyDictionary<string, object?>?` | Initial values by field key for the controller created from `KlyDynamicFormEngine.Schema`. Ignored when `KlyDynamicFormEngine.Form` is set. |
| `OnCancel` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with the current output when the user presses Cancel or the close button. When bound, the Cancel button is shown. |
| `OnSave` | `Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>>?` | Your save step: called with `DynamicFormController.BuildOutput` after validation and the schema hooks passed, while the form is still submitting (the button shows its saving state, a second submit is refused, Cancel is disabled). Return `true` when the save succeeded; the form is then clean and `KlyDynamicFormEngine.OnSubmit` is raised. Return `false` to keep the user's edits dirty and the form open, after reporting the reason with `DynamicFormController.SetError` or `DynamicFormController.SetFormError`. The token is cancelled when the component is disposed. It runs through `DynamicFormController.SubmitAsync`. |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with `DynamicFormController.BuildOutput` after the form is submitted successfully, that is after validation, the schema hooks and `KlyDynamicFormEngine.OnSave` passed. At that point the submission is over: `DynamicFormController.IsSubmitting` is `false` and the form is clean. Do the save itself in `KlyDynamicFormEngine.OnSave`. |
| `Schema` | `DynamicFormSchema?` | The schema to render when no `KlyDynamicFormEngine.Form` is given. The component creates and owns the controller, and creates a new one when the schema instance changes. |
| `Shell` | `FormShellMode` | How the form is presented. Anything other than `FormShellMode.Inline` adds a close button to the header when `KlyDynamicFormEngine.OnCancel` is bound. |
| `ShowReset` | `bool` | Shows a reset button in the footer while the form is dirty. The default is `true`. |
| `SubmitLabel` | `string?` | Text of the submit button. The default is the `Submit` text of the current locale (`FormTexts.Submit`). |

Also accepts the parameters of `KlyComponentBase`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `EffectiveController` | `DynamicFormController?` | The controller in use: `KlyDynamicFormEngine.Form` if set, otherwise the one the component created from `KlyDynamicFormEngine.Schema`. |

#### `KlyDynamicFormPage`

`Klyroo.Form.Dynamic` · component · inherits `KlyComponentBase`

Renders a sectioned form as a page: a sidebar listing each section with its completion status, and one card per section laid out by `DynamicSectionSchema.Span`. Fields render with the same controls, labels, containers and repeatable lists as `KlyDynamicFormEngine`, and the page is a `form` element, so pressing Enter in a field submits it.

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `FieldTemplates` | `IReadOnlyDictionary<string, RenderFragment<DynamicControlContext>>?` | Renderers of the `FormFieldKind.Custom` fields by `DynamicFieldSchema.CustomKey`, each a `RenderFragment<DynamicControlContext>` that draws the control. They win over the ones registered with `KlyrooFormOptions.CustomField`. |
| `Form` | `DynamicFormController` | The controller to render. Its schema must declare `DynamicFormSchema.Sections`. |
| `OnSave` | `Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>>?` | Your save step: called with `DynamicFormController.BuildOutput` after validation and the schema hooks passed, while the form is still submitting. Return `true` when the save succeeded; the form is then clean and `KlyDynamicFormPage.OnSubmit` is raised. Return `false` to keep the edits dirty, after reporting the reason with `DynamicFormController.SetError` or `DynamicFormController.SetFormError`. The token is cancelled when the component is disposed. |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | Called with `DynamicFormController.BuildOutput` after the form is submitted successfully, that is after validation, the schema hooks and `KlyDynamicFormPage.OnSave` passed. The submission is over at that point and the form is clean. |

Also accepts the parameters of `KlyComponentBase`.

#### `FieldNodeView`

`Klyroo.Form.Internal` · component · inherits `KlyComponentBase`

Renders one field of a form: the label, the control of its kind, the description, help and error. It is the one renderer of typed and dynamic forms, and it takes its parameters from `KlyFormFieldView<TModel>`, `KlyDynamicFieldView` and the form components, which are the API; do not use it directly.

Also accepts the parameters of `KlyComponentBase`.

#### `FormPageShell`

`Klyroo.Form.Internal` · component · inherits `KlyComponentBase`

The inside of `KlyFormPage<TModel>` and `KlyDynamicFormPage`: the sections, the navigation and the submit button of a form page, shared by both. It takes its parameters from the two components, which are the API; do not use it directly.

Also accepts the parameters of `KlyComponentBase`.

#### `FormShell`

`Klyroo.Form.Internal` · component · inherits `KlyComponentBase`

The inside of `KlyFormEngine<TModel>` and `KlyDynamicFormEngine`: the header, the stepper, the fields and the actions of a form, shared by both. It takes its parameters from the two components, which are the API; do not use it directly.

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
| `static AddKlyrooForm(IServiceCollection services, Action<KlyrooFormOptions>? configure = null)` | `IServiceCollection` | Registers `FormService` and `DynamicFormService` as scoped services and calls `AddKlyrooUi()`, because the components build on Klyroo.UI. In a Blazor Web App with interactive WebAssembly or Auto, call it in the server project and in the client project. |

### Classes

#### `FormService`

`Klyroo.Form` · class

Opens a typed form as an overlay and returns its result as a task. Register it with `AddKlyrooForm()` (scoped) and mount one `KlyFormProvider` in the layout so the form has somewhere to render. The runtime counterpart, for a form known only at run time, is `DynamicFormService`.

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsOpen` | `bool` | `true` while a form is open. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `CloseForm()` | `void` | Closes the open form without submitting and completes the task of `FormService.CreateFormAsync`` with `FormSubmitOutcome.IsCompleted` set to `false`. Does nothing when no form is open. |
| `CreateFormAsync<TModel>(FormSchema<TModel> schema, TModel model = null, FormShellMode mode = Modal)` | `Task<FormSubmitOutcome<TModel>>` | Opens a form in the mounted `KlyFormProvider` and waits for its result. Opening a form while another is open replaces it, and the replaced form's task completes with `FormSubmitOutcome.IsCompleted` set to `false`, as if it had been closed. |
| `SubmitActiveFormAsync()` | `Task<bool>` | Submits the open form with `FormController.SubmitAsync`. On success it closes the form and completes the task of `FormService.CreateFormAsync``. |

#### `KlyrooFormOptions`

`Klyroo.Form` · class

Configuration of Klyroo.Form: the built-in texts of each locale. It is an extension of `KlyrooOptions` (`KlyrooOptions.Extension``). Configure it with `services.AddKlyrooForm(f => ...)`, in code shared by the server and the WebAssembly client.

| Constructor | Description |
| :--- | :--- |
| `KlyrooFormOptions()` | Creates the options with the built-in texts of `FormLocales`. |

| Method | Returns | Description |
| :--- | :--- | :--- |
| `CustomField(string key, RenderFragment<DynamicControlContext> renderer)` | `KlyrooFormOptions` | Registers the renderer of `FormFieldKind.Custom` fields of dynamic forms whose `DynamicFieldSchema.CustomKey` is `key`, for every form of the app. The renderer draws the control from a `DynamicControlContext`; the components draw the label, description, help and error around it. The `FieldTemplates` parameter of a component wins over it for that component. Registering a key again replaces its dynamic renderer; typed renderers under the same key (`KlyrooFormOptions.CustomField``) are separate. |
| `CustomField<TValue>(string key, RenderFragment<FormControlContext<TValue>> renderer)` | `KlyrooFormOptions` | Registers the renderer of the custom fields of typed forms declared with `AsCustom()` whose value is a `TValue`, for every form of the app. The renderer draws the control from a `FormControlContext`; the components draw the label, description, help and error around it. A `KlyFieldTemplate` of a field wins over it. A field of another value type than `TValue` that names the key shows the "no renderer" notice instead of a control that would read the wrong type, unless a renderer was registered for its type under the same key: renderers are kept by key and value type, and the one of a dynamic form (`KlyrooFormOptions.CustomField`) is separate too. Registering the same key and type again replaces its renderer. |
| `Texts(string localeCode, Func<FormTexts, FormTexts> change)` | `KlyrooFormOptions` | Changes the texts of a locale, like `KlyLocales.Extend` does for Klyroo.UI: `change` receives the set the locale resolves to and returns the one to use, usually `t => t with { ... }`. `localeCode` is a code of `KlyLocales.All` ("fr"), or the code of a locale you added ("en-AU", "tlh"): a locale without a set of its own starts from the set `KlyLocales.ResolveCode` picks for it, then its changes apply. The built-in validation messages are part of the set (`FormTexts.Messages`). The texts become read-only the first time a component resolves one. |

### Records

#### `FormSubmitOutcome<TModel>`

`Klyroo.Form` · record

Result of an overlay opened with `FormService.CreateFormAsync``.

| Constructor | Description |
| :--- | :--- |
| `FormSubmitOutcome<TModel>(bool IsCompleted, TModel Model)` | Result of an overlay opened with `FormService.CreateFormAsync``. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `IsCompleted` (init) | `bool` | `true` when the form was submitted successfully, `false` when it was closed without submitting. |
| `Model` (init) | `TModel` | When completed, the model that was submitted: the one you passed, with the values written into it, or the new one the form created. Otherwise the model you passed (unchanged), or `null` when you passed none. |

#### `FormTexts`

`Klyroo.Form` · record

The built-in texts of `KlyDynamicFormEngine`, `KlyDynamicFormPage`, `KlyFormProvider`, `KlyDynamicFieldView`, `KlyFormErrorSummary` and `KlyFormNavigationGuard`: button labels, accessible names, empty states, counters and the built-in validation messages. There is one set per locale of Klyroo.UI (`FormLocales`), and a component shows the set of its current locale, the one its `Messages` come from. Change a set with `KlyrooFormOptions.Texts` in `AddKlyrooForm(f => ...)`.

| Constructor | Description |
| :--- | :--- |
| `FormTexts()` | Creates the texts with their English defaults. |

| Property | Type | Description |
| :--- | :--- | :--- |
| `AddItem` (init) | `string` | The add button of a repeatable list without an `AddItemLabel` (`"Add item"`). |
| `AllReady` (init) | `string` | The summary of `KlyDynamicFormPage` when no section is pending or invalid (`"Everything is ready."`). |
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
| `FormSections` (init) | `string` | The heading and the accessible name of the section list of `KlyDynamicFormPage` (`"Form sections"`). |
| `FormTitle` (init) | `string` | The accessible name of an overlay form without a title (`"Form"`). |
| `Hide` (init) | `string` | The visible text of the button that hides a password (`"Hide"`). |
| `HidePassword` (init) | `string` | The accessible name of the button that hides a revealed password (`"Hide password"`). |
| `LeftToComplete` (init) | `Func<int, string>` | The summary of `KlyDynamicFormPage`, given the number of sections left (`"2 left to complete."`). |
| `Messages` (init) | `FormMessages` | The built-in validation messages the components give to the controller they render (see `DynamicFormController.Messages`), unless the schema or the code chose its own. |
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
| `SomethingWentWrong` (init) | `string` | The form-level error shown when saving, an asynchronous rule or a handler throws (`"Something went wrong. Try again."`). |
| `StartDate` (init) | `string` | The accessible name of the first input of a date range (`"Start date"`). |
| `Submit` (init) | `string` | The submit button when `SubmitLabel` is not set (`"Submit"`). |
| `TagPlaceholder` (init) | `string` | The placeholder of a tag input without a `Placeholder` (`"Add a tag and press Enter"`). |
| `Tags` (init) | `string` | The accessible name of the tag list of a field without a label (`"Tags"`). |
| `UnsavedChanges` (init) | `string` | The question of the default confirmation of `KlyFormNavigationGuard` (`"You have unsaved changes. Leave this page and discard them?"`). |

## Klyroo.Form.DataAnnotations

Static classes and extensions (1)

### Static classes and extensions

#### `FormModelDataAnnotationsExtensions`

`Klyroo.Form` · service

Reads the `System.ComponentModel.DataAnnotations` attributes of a model into a form declared with `FormSchema.For``.

| Method | Returns | Description |
| :--- | :--- | :--- |
| `static FromDataAnnotations<TModel>(FormSchemaBuilder<TModel> builder)` | `FormSchemaBuilder<TModel>` | Declares a field for every public readable and writable member of the model (`AllMembers()`) and reads their data annotations (see `FormModelDataAnnotationsExtensions.UseDataAnnotations``). It is the whole declaration of a form that is described by attributes: fields are sorted by `[Display(Order = ...)]` and then by declaration. |
| `static UseDataAnnotations<TModel>(FormSchemaBuilder<TModel> builder)` | `FormSchemaBuilder<TModel>` | Applies the data annotations of the members that have a field, and checks the rest at submit. `[Display]`: `Name` is the label, `Description` the description, `Prompt` the placeholder and `Order` the position.`[Editable(false)]` and `[ReadOnly(true)]` disable the field. `[DataType]` picks the kind for `Password`, `MultilineText`, `PhoneNumber`, `Date` and `DateTime`.`[Required]` is the required check of the field, `[StringLength]` and `[MaxLength]` on a string are the length limit of the field, and `[Range]` with `Int32` or `Double` inclusive bounds on a number is the bounds of the field, so the engine checks them with its own, localized messages.A `[StringLength]` with a `MinimumLength` also gets a validator rule for the minimum. A `[Range]` with `typeof(decimal)` or text bounds, an exclusive bound, or on a field that is not a number is a validator rule only.`[MinLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[CreditCard]`, `[Compare]` and the other built-in attributes the engine has no rule for become a validator rule that runs while the user edits, with the message of the attribute: English unless the attribute has an `ErrorMessage` or a resource. That message and the `[Display]` name in it are worked out when the rule fails, so they follow the UI culture of the validation.An `ErrorMessage` (or a resource message) that the developer set replaces the engine message on `[Required]` (fixed when the form is built, like the label, description and placeholder), and turns `[StringLength]`, `[MaxLength]` and `[Range]` into validator rules that use it.Any other `ValidationAttribute` (your own, or a subclass of a built-in one), attributes on the model class and `IValidatableObject` run at submit, on the model built from the values, after the field rules passed. Their errors go to the field of the member they name, or to the form. |
