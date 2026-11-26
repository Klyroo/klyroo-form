# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Localization: the built-in texts of the components (`FormTexts`, one set per Klyroo.UI locale in `FormLocales`) and the built-in validation messages (`FormMessages`, in `Klyroo.Form.Core` so they work without Blazor, one set per locale in `FormMessageLocales`) follow the locale in twelve languages. English and French are reviewed, the others are drafts. `AddKlyrooForm(f => f.Texts(code, change))` changes a set, `FormSchema.Messages` and `FormController.Messages` choose the language of one form, and `FormMessageLocales.Resolve(code)` picks a set for a culture name.
- Asynchronous validation: `FormFieldSchema.AsyncValidators` take `FormAsyncValidatorRule` (`(value, controller, CancellationToken) => ValueTask<string?>`, with a `Debounce`). They run in the background after blur and while editing, stale runs are cancelled, and `SubmitAsync`, `NextStepAsync` and the new `FormController.ValidateAsync` wait for them. `IsValidating(key)` and `HasPendingValidation` report a run, and the components show a pending state next to the field.
- `KlyFormNavigationGuard`, a `NavigationLock` that follows `FormController.IsDirty`: it asks the browser to confirm external navigation while the form is dirty and confirms in-app navigation with `window.confirm` or your `ConfirmLeave`.
- `FormController.ExportState()` and `RestoreDraft()` with `FormDraft` (and `ToJson` and `FromJson`, written without reflection). A restored draft keeps the original baseline, so it is still dirty; passwords and one-time codes are left out unless asked for.
- `FormController.TryGetValue<T>`, and a `GetValue<T>` that converts the formats the controls store: `Nullable<T>`, `DateOnly` (`yyyy-MM-dd`), `TimeOnly` (`HH:mm`), `DateTime` (`yyyy-MM-ddTHH:mm`), `DateTimeOffset`, `TimeSpan`, `Guid`, enums (by name or member number), `bool` text and numbers in the invariant culture. It uses no reflection over the target type, so it stays AOT and trim compatible.
- `FormFieldSchema.DependsOn` names the fields whose value a field's rules read. A change of one of them validates the touched field again, so cross-field errors (a confirmation, `RequiredWhen`) appear and clear as the other field changes.
- `FormController.SubmitAsync(handler, cancellationToken)` runs your save step inside the submission: `IsSubmitting` stays `true` while it works and the dirty snapshot is retaken only when it returns `true`. `KlyFormEngine` and `KlyFormPage` expose it as `OnSave` (`Func<output, CancellationToken, Task<bool>>`) and refuse a second submit while one runs.
- Form-level errors: `SetFormError`, `FormErrorKey`, `FormErrors`, `GetErrorEntries` (with `FormErrorEntry`) and `SubmitFailed`. An error set with `SetError` on a key that matches no field is form-level, and `KlyFormErrorSummary` renders them at the top of `KlyFormEngine` and `KlyFormPage`, followed by the field errors after a failed submit or Next, each as a button that focuses the field.
- One polite live region per form announces the error count and the first error, and focus moves to the first invalid field after a failed submit or Next (best effort, through a small `klyroo-form.js` module).
- Dedicated controls for every field kind that fell back to a text input: `CheckboxGroup` (checkbox list, `List<string>`), `DateTime` (`datetime-local`), `DateRange` (start and end dates, a two-item `List<string>`, with a required check on both dates and an order check), `PhoneNumber` (`type="tel"`), `Rating` (radio-based stars from 1 to `Max`, default 5, stored as `int`), `ColorPicker` (`type="color"`), `OneTimeCode` (`autocomplete="one-time-code"`, numeric keyboard, `maxlength`) and `Tag` (Enter or comma adds a tag, each tag has a remove button, `List<string>`).
- `FormFieldKind.Button` renders a button that never submits and runs the new `FormFieldSchema.OnClick`.
- `FormFieldSchema.Multiple` on `Select` and `AutoComplete` renders a checkbox list that writes a `List<string>`.
- Fields with `OnCreateOption` get a create entry (text input and button) that calls `FormController.CreateFieldOption`.
- `FormOptionItem.Group` renders an `optgroup` in select lists and a heading in radio and checkbox lists; `Description` and `Icon` (a `KlyIcon` name) render in radio and checkbox lists.
- `Collapsible` and `DefaultCollapsed` render `Group`, `Card` and `Object` containers as a `details` disclosure.
- `DisplayTone` (`neutral`, `info`, `success`, `warning`, `error`) and `DisplayKind` (`callout` or `text`) style `Info` blocks with the Klyroo.UI tone colors.
- `FormStepSchema.Description` shows under the stepper for the current step, `FormSchema.Columns` sets the column count of every field grid, and `FormPageSectionSchema.Span` lays sections out on a 12-column grid in `KlyFormPage`.
- Cells of `ArrayList` and `ArrayTable` render the control of their child kind and are validated with the child's `Required`, `Min`, `Max`, `MaxLength`, password requirements and `Validators`, honoring its `Condition`, `Disabled` and `Trigger`. New members: `FormController.GetArrayItemError`, `MarkArrayItemTouched`, `CellPath`, and a `markTouched` parameter on `SetArrayItemValue`. Cell errors count in `HasBlockingErrors` and appear in `Errors` as `lines[0].sku`.
- Password fields with `PasswordRequirements` show a live checklist.
- `Clearable` also applies to `Textarea`, `Password`, `Number` and `Rating`.
- `KlyFormProvider` renders `FormShellMode.Drawer` as a full-height side panel and `Fullscreen` over the whole viewport.
- `KlySchemaField`, the component that renders one field, shared by `KlyFormEngine` and `KlyFormPage`.
- `FormController.ActiveFields` (the fields validation checks and the output contains) and `FormController.IsContainer`.
- `aria-invalid`, `aria-describedby` (description, help and error) and `aria-required` on the controls, and `role="switch"` on `Switch`.

### Changed

- `KlyFormEngine.SubmitLabel` is `string?` and defaults to the `Submit` text of the current locale instead of the literal `Submit`; `KlyFormNavigationGuard.Message` defaults the same way. `KlySchemaField` derives from `KlyComponentBase`, which gives it `Class` and unmatched attributes like the other components.
- `KlyFormProvider` hosts its overlay in `KlyModal` (`Modal`, `Fullscreen`) and `KlySlideover` (`Drawer`) of Klyroo.UI instead of a hand-made `div`: the native modal dialog traps focus, closes on Escape and on a click on the backdrop, and returns focus to the opener. It cannot be dismissed while the form saves. The `form-provider-dialog` and `form-provider-overlay` class names are gone; the overlay carries `data-slot="form-provider-overlay"`, `data-mode` and `data-open` on a wrapper and `data-form-dialog` on the dialog.
- `GetValue<T>` no longer rounds a fraction into an integer type (`GetValue<int>` on 3.5 returned 4): it returns `default`, as it does for any other value that does not fit the type.
- Per-field error messages no longer carry `role="alert"`: they stay linked to the control with `aria-describedby`, and the live region and the error summary announce errors once for the whole form.
- `NextStepAsync` and `PreviousStepAsync` refuse a call while another step change is running, and `SubmitAsync` refuses one during a step change.
- `AutoComplete` is a text input with `datalist` suggestions instead of a select list. It stores the value of the option whose label matches the text (ignoring case) or whose value equals it, or the text itself.
- `Watch` now also runs after `ResetField` changes a value, after a row of a repeatable list is added or removed, and, for a child of a list, after its cell changes. `Reset` still does not call it.
- `Validate` removes the validation errors of fields that are no longer in effect, and `HasBlockingErrors` ignores them.

### Removed

- `FormStepSchema.Root`, which was reserved and never read.

### Fixed

- The sections guide put `NavigationLock ConfirmExternalNavigation="@_form.IsDirty"` on a page that never re-renders when the user types, so the browser prompt never turned on. `KlyFormNavigationGuard` re-renders itself when `IsDirty` changes.
- The overlay of `KlyFormProvider` was a `div` with `role="dialog"`: focus was not trapped, Escape did nothing and focus was lost on close.
- `GetValue<T>` returned `default` for `Nullable<T>`, `DateOnly`, `TimeOnly`, `DateTime`, `Guid` and enum targets, which `Convert.ChangeType` cannot produce from the strings the controls store.
- A cross-field error stayed on screen after the other field was fixed (a confirm password that now matched, a `RequiredWhen` whose condition no longer held), and `HasBlockingErrors` stayed `true`. After a value changes, the controller now validates again the touched fields that show an error.
- `Validate` ignored a blocking error set with `SetError` on a key that matches no field, so a form-level failure did not stop `SubmitAsync`. It now fails on any blocking error that is form-level or on a validated field.
- Two quick clicks on Next ran `OnBeforeNext` twice and skipped a step.
- `OnSubmit` of the components ran after the submission was over (`IsSubmitting` already reset, snapshot already retaken), so an API failure there could not keep the form dirty. Use the new `OnSave`.
- Hidden and omitted columns of a repeatable list are left out of the rows of `BuildOutput`, like hidden and omitted fields.
- `KlyFormPage` rendered only the top-level fields of a section, each as an unlabelled text input: the children of containers and tabs never showed although they were validated, and repeatable lists were text inputs. It now renders fields like `KlyFormEngine`, and it is a `form` element, so Enter submits it.
- The children of a hidden `Group`, `Card` or `Object`, the fields of a hidden tab or section, and the fields of a step skipped by `SkipWhen` were still validated and output. They are now left out of validation and `BuildOutput`, and keep their values in the state.
- A `Tabs` field without tabs, or a container without `Fields`, became a stateful field rendered as a text input. Containers are now layout only and render nothing when empty.
- A stepped form whose first step is skipped started on it, and `Reset` returned to it. Both now go to the first step that is not skipped, and `IsFirstStep` and `IsLastStep` look past skipped steps, so Back is hidden and Submit shows on the last reachable step.
- Editing a field with `Trigger = Submit` left its last validation error on screen; the error is now cleared on change, without validating.
- `FormService.CreateFormAsync` called while another form was open left the replaced form's task pending forever. It now completes with `IsCompleted = false`.
- Repeatable lists in `KlyFormEngine` did not show the list's own error, required marker, description, hint or help, ignored `Disabled` and `DisabledWhen`, and had no `DirtyCheck` reset.
- A `Switch` showed its label twice, and the `Hint` and `DirtyCheck` reset of checkboxes and label-less fields were not rendered.
- The label of a radio group pointed to an id that no element had. The group now has an id and `aria-labelledby`.
- `MaxLength` was not rendered as `maxlength` on `Textarea` and `Password` inputs.
- Pressing Enter in a field of a wizard before its last step submitted the whole form; it now moves to the next step.
- A `Slider` without a value showed the browser's midpoint. It now shows `Min` (or 0) and keeps `null` in the state until the user moves it.

- A form opened with `FormService` ran validation, `OnBeforeSubmit` and `OnSubmit` twice when submitted from its button.
- `Required` on a `Checkbox` or `Switch` field now requires it to be checked; an unchecked box no longer passes. A switch where off is a valid answer must not be `Required`.
- `FormFieldSchema.MaxLength` is now validated by the controller, not only rendered as the `maxlength` attribute.

## [0.1.0-preview.1] - 2026-11-24

### Added

- `Klyroo.Form.Core`: a headless `FormController` driven by a declarative `FormSchema`, with 31 `FormFieldKind` values, no UI dependency and NativeAOT compatibility.
- Validation with required and required-when checks, number bounds, password requirements, custom validator rules, four validation modes and three triggers, plus `SetError` for server-side errors.
- Conditional fields (`Condition`, `RequiredWhen`, `DisabledWhen`) and value transforms (`TransformInput`, `TransformOutput`), with hidden fields left out of validation and output.
- Dirty tracking against an initial snapshot, with per-field and per-section reset and a new snapshot after a successful submit.
- Multi-step forms with skippable steps and async `OnBeforeNext` and `OnBeforePrevious` hooks, and sectioned page forms with a completion status per section.
- Repeatable rows (`ArrayList` and `ArrayTable`) and tabbed, grouped and card containers.
- `FormService` and `KlyFormProvider` to open a form as an overlay and await its `FormSubmitOutcome`.
- `Klyroo.Form`: the `KlyFormEngine`, `KlyFormPage` and `KlyFormProvider` Blazor components and `AddKlyrooForm()`, building on Klyroo.UI.

[Unreleased]: https://github.com/klyroo/klyroo-form/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/klyroo/klyroo-form/releases/tag/v0.1.0-preview.1
