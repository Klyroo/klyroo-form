# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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

- `AutoComplete` is a text input with `datalist` suggestions instead of a select list. It stores the value of the option whose label or value matches the text, or the text itself.
- `Watch` now also runs after `ResetField` changes a value, after a row of a repeatable list is added or removed, and, for a child of a list, after its cell changes. `Reset` still does not call it.
- `Validate` removes the validation errors of fields that are no longer in effect, and `HasBlockingErrors` ignores them.

### Fixed

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
