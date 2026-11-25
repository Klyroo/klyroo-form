# API Reference — Klyroo.Form

Complete public API reference extracted directly from the source code of `Klyroo.Form.Core` and `Klyroo.Form`.

## Razor Components

### `<KlyFormEngine>`
*Package*: `Klyroo.Form` · *Source*: `src/Klyroo.Form/KlyFormEngine.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Form` | `FormController?` | — |
| `Schema` | `FormSchema?` | — |
| `Input` | `IReadOnlyDictionary<string, object?>?` | — |
| `Shell` | `FormShellMode` | — |
| `SubmitLabel` | `string` | — |
| `ShowReset` | `bool` | — |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | — |
| `OnCancel` | `EventCallback<IReadOnlyDictionary<string, object?>>` | — |

### `<KlyFormPage>`
*Package*: `Klyroo.Form` · *Source*: `src/Klyroo.Form/KlyFormPage.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `Form` | `FormController` | — |
| `OnSubmit` | `EventCallback<IReadOnlyDictionary<string, object?>>` | — |

### `<KlyFormProvider>`
*Package*: `Klyroo.Form` · *Source*: `src/Klyroo.Form/KlyFormProvider.razor`

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `ChildContent` | `RenderFragment?` | — |

## C# Types, Controllers, Models & Enums

### `enum FormValidationMode`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Validation mode of a `FormController`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `All` | `enum member` | — |
| `RequiredOnly` | `enum member` | — |
| `ValidatorsOnly` | `enum member` | — |
| `None` | `enum member` | — |

### `enum FormValidationTrigger`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Validation trigger timing for a `FormFieldSchema`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Blur` | `enum member` | — |
| `Input` | `enum member` | — |
| `Submit` | `enum member` | — |

### `enum FormShellMode`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Shell layout of a `KlyFormEngine` or `FormService` overlay.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Inline` | `enum member` | — |
| `Modal` | `enum member` | — |
| `Drawer` | `enum member` | — |
| `Fullscreen` | `enum member` | — |

### `enum FormPageSectionStatus`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Completion status of a `FormPageSectionSchema` in `KlyFormPage`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Complete` | `enum member` | — |
| `Pending` | `enum member` | — |
| `Invalid` | `enum member` | — |
| `Optional` | `enum member` | — |

### `enum FormFieldKind`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Supported field kinds in the schema-driven Form Engine.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Text` | `enum member` | — |
| `Password` | `enum member` | — |
| `Textarea` | `enum member` | — |
| `Number` | `enum member` | — |
| `Checkbox` | `enum member` | — |
| `Switch` | `enum member` | — |
| `Radio` | `enum member` | — |
| `CheckboxGroup` | `enum member` | — |
| `Select` | `enum member` | — |
| `AutoComplete` | `enum member` | — |
| `Date` | `enum member` | — |
| `DateTime` | `enum member` | — |
| `DateRange` | `enum member` | — |
| `Time` | `enum member` | — |
| `PhoneNumber` | `enum member` | — |
| `Slider` | `enum member` | — |
| `Rating` | `enum member` | — |
| `ColorPicker` | `enum member` | — |
| `OneTimeCode` | `enum member` | — |
| `Tag` | `enum member` | — |
| `Hidden` | `enum member` | — |
| `Info` | `enum member` | — |
| `Divider` | `enum member` | — |
| `Section` | `enum member` | — |
| `Button` | `enum member` | — |
| `Group` | `enum member` | — |
| `Card` | `enum member` | — |
| `Tabs` | `enum member` | — |
| `Object` | `enum member` | — |
| `ArrayList` | `enum member` | — |
| `ArrayTable` | `enum member` | — |

### `record FormOptionItem`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

An option item for choice and select fields in `KlyFormEngine`.

### `record FormPasswordRequirement`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

A password requirement rule on a `FormFieldKind.Password` field.

### `record FormValidatorRule`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

A named validator rule on a `FormFieldSchema`.

### `class FormTabItemSchema`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

A tab definition inside a `FormFieldKind.Tabs` container field.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Label` | `string` | — |
| `Fields` | `IReadOnlyList<FormFieldSchema>` | — |
| `Condition` | `Func<FormController, bool>?` | — |

### `class FormFieldSchema`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Declarative definition of a field in `FormSchema`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Kind` | `FormFieldKind` | — |
| `Label` | `string?` | — |
| `Description` | `string?` | — |
| `Hint` | `string?` | — |
| `Help` | `string?` | — |
| `Placeholder` | `string?` | — |
| `DefaultValue` | `object?` | — |
| `DefaultFactory` | `Func<FormController, object?>?` | — |
| `Required` | `bool` | — |
| `RequiredWhen` | `Func<FormController, bool>?` | — |
| `RequiredMessage` | `string?` | — |
| `Trigger` | `FormValidationTrigger` | — |
| `Disabled` | `bool` | — |
| `DisabledWhen` | `Func<FormController, bool>?` | — |
| `DisabledReason` | `string?` | — |
| `Condition` | `Func<FormController, bool>?` | — |
| `Ignore` | `bool` | — |
| `OmitFromOutput` | `bool` | — |
| `DirtyCheck` | `bool` | — |
| `Collapsible` | `bool` | — |
| `DefaultCollapsed` | `bool` | — |
| `Span` | `int` | — |
| `Min` | `double?` | — |
| `Max` | `double?` | — |
| `Step` | `double?` | — |
| `MaxLength` | `int?` | — |
| `Multiple` | `bool` | — |
| `Clearable` | `bool` | — |
| `Options` | `IReadOnlyList<FormOptionItem>?` | — |
| `OptionsFactory` | `Func<FormController, IReadOnlyList<FormOptionItem>>?` | — |
| `OnCreateOption` | `Func<string, FormOptionItem?>?` | — |
| `PasswordRequirements` | `IReadOnlyList<FormPasswordRequirement>?` | — |
| `Validators` | `IReadOnlyList<FormValidatorRule>?` | — |
| `TransformInput` | `Func<object?, object?>?` | — |
| `TransformOutput` | `Func<object?, object?>?` | — |
| `Watch` | `Action<object?, FormController>?` | — |
| `DisplayKind` | `string?` | — |
| `DisplayText` | `string?` | — |
| `DisplayTone` | `string?` | — |

### `class FormStepSchema`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

One step in a multi-step `FormSchema`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Title` | `string` | — |
| `Description` | `string?` | — |
| `Root` | `string?` | — |
| `Fields` | `IReadOnlyList<FormFieldSchema>` | — |
| `SkipWhen` | `Func<FormController, bool>?` | — |

### `class FormPageSectionSchema`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

One section in a `KlyFormPage` schema.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Key` | `string` | — |
| `Label` | `string` | — |
| `Description` | `string?` | — |
| `Optional` | `bool` | — |
| `Span` | `int` | — |
| `Condition` | `Func<FormController, bool>?` | — |
| `Fields` | `IReadOnlyList<FormFieldSchema>` | — |

### `record FormPageSectionState`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Computed runtime state of a `FormPageSectionSchema`.

### `class FormSchema`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Top-level declarative schema for `KlyFormEngine`, `KlyFormPage` and `FormService`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `FormKey` | `string` | — |
| `Title` | `string?` | — |
| `Description` | `string?` | — |
| `Eyebrow` | `string?` | — |
| `Columns` | `int` | — |
| `ShowStepper` | `bool` | — |
| `ValidateMode` | `FormValidationMode` | — |
| `IgnoreDirtyPaths` | `IReadOnlyList<string>?` | — |
| `Fields` | `IReadOnlyList<FormFieldSchema>` | — |
| `Steps` | `IReadOnlyList<FormStepSchema>` | — |
| `Sections` | `IReadOnlyList<FormPageSectionSchema>` | — |
| `OnBeforeSubmit` | `Func<FormController, Task<bool>>?` | — |
| `OnSubmit` | `Func<FormController, Task>?` | — |
| `OnBeforeNext` | `Func<FormController, int, Task<bool>>?` | — |
| `OnBeforePrevious` | `Func<FormController, int, Task<bool>>?` | — |

### `record FormSubmitOutcome`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Result returned when a form submit or overlay session completes.

### `class FormController`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Stateful runtime controller for a `FormSchema`: manages internal state, initial snapshot, dirty path tracking, touch &amp; validation rules, multi-step navigation, array item management, and submission.

| Member | Type | Description |
| :--- | :--- | :--- |
| `Schema` | `FormSchema` | — |
| `CurrentStepIndex` | `int` | — |
| `IsSubmitting` | `bool` | — |
| `IsStepped` | `bool` | — |
| `CurrentStep` | `FormStepSchema?` | — |
| `IsFirstStep` | `bool` | — |
| `IsLastStep` | `bool` | — |
| `InternalState` | `IReadOnlyDictionary<string, object?>` | — |
| `DirtyPaths` | `IReadOnlyList<string>` | — |
| `IsDirty` | `bool` | — |
| `Errors` | `IReadOnlyDictionary<string, string>` | — |
| `HasBlockingErrors` | `bool` | — |
| `AllFields` | `IReadOnlyList<FormFieldSchema>` | — |
| `CurrentVisibleTopFields` | `IReadOnlyList<FormFieldSchema>` | — |
| `GetValue(string path)` | `object?` | — |
| `GetValue<T>(string path)` | `T?` | — |
| `GetInitialValue(string path)` | `object?` | — |
| `SetValue(string path, object? value, bool markTouched = false)` | `void` | — |
| `MarkTouched(string path)` | `void` | — |
| `ResetField(string path)` | `void` | — |
| `ResetSection(string sectionKey)` | `void` | — |
| `SetError(string path, string message, bool blocking = false)` | `void` | — |
| `ClearError(string? path = null)` | `void` | — |
| `GetError(string path)` | `string?` | — |
| `ShouldRenderField(FormFieldSchema field)` | `bool` | — |
| `IsSectionVisible(FormPageSectionSchema section)` | `bool` | — |
| `IsFieldRequired(FormFieldSchema field)` | `bool` | — |
| `IsFieldDisabled(FormFieldSchema field)` | `bool` | — |
| `GetFieldOptions(FormFieldSchema field)` | `IReadOnlyList<FormOptionItem>` | — |
| `CreateFieldOption(FormFieldSchema field, string label)` | `FormOptionItem?` | — |
| `GetArrayItems(string arrayKey)` | `IReadOnlyList<Dictionary<string, object?>>` | — |
| `AddArrayItem(FormFieldSchema arrayField)` | `void` | — |
| `RemoveArrayItem(string arrayKey, int index)` | `void` | — |
| `SetArrayItemValue(string arrayKey, int index, string childKey, object? value)` | `void` | — |
| `Validate(bool currentStepOnly = false)` | `bool` | — |
| `BuildOutput()` | `IReadOnlyDictionary<string, object?>` | Builds the transformed output dictionary excluding hidden, ignored, stateless, and `FormFieldSchema.OmitFromOutput` fields. |
| `NextStepAsync()` | `Task<bool>` | — |
| `PreviousStepAsync()` | `Task<bool>` | — |
| `SubmitAsync()` | `Task<bool>` | — |
| `Reset(IReadOnlyDictionary<string, object?>? newInput = null)` | `void` | — |

### `class FormService`
*Package*: `Klyroo.Form.Core` · *Source*: `src/Klyroo.Form.Core/FormEngineModels.cs`

Scoped service managing programmatic overlay forms opened in `KlyFormProvider`.

| Member | Type | Description |
| :--- | :--- | :--- |
| `ActiveForm` | `FormController?` | — |
| `ActiveMode` | `FormShellMode` | — |
| `IsOpen` | `bool` | — |
| `CreateFormAsync(
        FormSchema schema,
        IReadOnlyDictionary<string, object?>? input = null,
        FormShellMode mode = FormShellMode.Modal)` | `Task<FormSubmitOutcome>` | Opens a `FormSchema` inside the mounted `KlyFormProvider` and awaits completion or cancellation. |
| `SubmitActiveFormAsync()` | `Task<bool>` | — |
| `CloseForm()` | `void` | — |

### `class KlyrooFormServiceCollectionExtensions`
*Package*: `Klyroo.Form` · *Source*: `src/Klyroo.Form/KlyrooFormServiceCollectionExtensions.cs`

| Member | Type | Description |
| :--- | :--- | :--- |
| `AddKlyrooForm(this IServiceCollection services)` | `IServiceCollection` | — |
