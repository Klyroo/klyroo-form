# FAQ

Troubleshooting, known limitations and common questions. Each entry is something you can reproduce from the code. If you hit a problem that is not here, open an issue with a minimal schema.

## Troubleshooting

### The form is unstyled or has no colors

**Cause:** one of the three stylesheets is missing. The colors, borders and radii come from Klyroo.UI tokens, and the layout classes come from `klyroo-form.css`, which defines no tokens.

**Fix:** load all three in `App.razor` (or `wwwroot/index.html` in MAUI): `klyroo.preflight.css` and `klyroo.css` from `_content/Klyroo.UI/`, and `klyroo-form.css` from `_content/Klyroo.Form/`. See [Getting started](getting-started.md#load-the-stylesheets).

### Typing and buttons do nothing

**Cause:** the page is rendered with Static SSR. The components rely on Blazor event handlers, which only run in an interactive render mode.

**Fix:** add `@rendermode InteractiveServer` (or WebAssembly, or Auto) to the page, or enable interactivity globally. See [Platforms](platforms.md#render-modes).

### What the user typed disappears when the parent re-renders

**Cause:** the schema is built inline in the markup, for example `Schema="new FormSchema { ... }"`. `KlyFormEngine` creates a new controller whenever it receives a different `FormSchema` instance, and a new controller starts from the defaults.

**Fix:** keep the schema in a field or a static member, or create a `FormController` yourself and pass it through `Form`.

### `FormService.CreateFormAsync` shows nothing and never returns

**Cause:** no `KlyFormProvider` is mounted in the current scope, or `AddKlyrooForm()` was not called in this project. The service stores the form and raises an event, and only the provider renders it. The returned task completes only after a submit or a close.

**Fix:** wrap the body of your layout in `<KlyFormProvider>`. With Auto render mode, register `AddKlyrooForm()` in the server and in the client project.

### `OnSubmit` or `OnBeforeSubmit` of the schema runs twice for an overlay

**Cause:** for a form opened through `FormService`, `KlyFormEngine` submits the controller, and then its `OnSubmit` callback, wired by `KlyFormProvider` to `FormService.SubmitActiveFormAsync`, submits it a second time. Validation and both schema hooks run on each pass.

**Fix:** keep side effects out of `FormSchema.OnSubmit` for overlay forms and do them after `await CreateFormAsync(...)`, using the returned `FormData`. Make `OnBeforeSubmit` safe to run twice, or avoid it in overlay schemas.

### A validation error does not show

**Cause:** errors are shown only for touched fields. A field becomes touched when `SetValue` is called with `markTouched: true`, when it loses focus in `KlyFormEngine`, when it has `Trigger = Input` and changes, or when `Validate()` or `SubmitAsync()` runs. Before that, `GetError` returns `null` even though a validation error may exist.

**Fix:** call `Validate()` to touch and check every field, call `MarkTouched(key)` for one field, or pass `markTouched: true` when you set a value from code. A field with `Trigger = FormValidationTrigger.Submit` is also only validated by `Validate()` or a submit.

### A required checkbox accepts an unchecked box

**Cause:** the required check tests for emptiness, and `false` is not empty. A checkbox that has never been toggled holds `null`, so it fails the required check, but once the user checks and unchecks it the value is `false` and passes.

**Fix:** use a validator that insists on `true`: `Validators = [new FormValidatorRule("accepted", "You must accept the terms", (value, _) => value is true)]`.

### `MaxLength` does not stop a long value

**Cause:** `FormFieldSchema.MaxLength` only sets the `maxlength` attribute on the built-in text-like inputs. The controller does not validate it, so a value set from code, or any value in a headless run, is not checked.

**Fix:** add a `FormValidatorRule` that checks the length.

### Fields inside a hidden group are still validated and submitted

**Cause:** the controller evaluates `Condition` per field and does not look at the condition of a container. A `Group`, `Card`, `Object` or `Tabs` field with a `Condition` is hidden, but its children are flattened into the form and handled as ordinary fields.

**Fix:** put the same `Condition` on the children. A hidden tab (`FormTabItemSchema.Condition`) has the same property.

### Required child fields of a repeatable list are not enforced

**Cause:** the controller treats `ArrayList` and `ArrayTable` as leaves and does not validate the cells of their rows. `Required`, `Min`, `Max` and validators on child fields are ignored. Cell edits also do not call `Watch`.

**Fix:** put a validator on the list field, which receives the whole `List<Dictionary<string, object?>>`. See [Edit repeatable rows](guides.md#edit-repeatable-rows).

### The Next button does nothing on the last visible step

**Cause:** the engine shows Next on every step except the last declared one. If the steps after the current one are all skipped by `SkipWhen`, `NextStepAsync` has nowhere to go and returns `false`, and there is no submit button yet.

**Fix:** do not end a wizard with a step that can be skipped. Make the last step unconditional, even if it only shows a summary.

### A field kind shows a text input

**Cause:** `FormFieldKind` has 31 values. `KlyFormEngine` has a dedicated rendering for 22 of them: `Text`, `Password`, `Textarea`, `Number`, `Slider`, `Checkbox`, `Switch`, `Radio`, `Select`, `AutoComplete`, `Date`, `Time`, `Hidden`, `Info`, `Divider`, `Section`, `Group`, `Card`, `Object`, `Tabs`, `ArrayList` and `ArrayTable`. `Switch` renders as a checkbox and `AutoComplete` as a native select. The other nine fall back to a text input: `CheckboxGroup`, `DateTime`, `DateRange`, `PhoneNumber`, `Rating`, `ColorPicker`, `OneTimeCode`, `Tag` and `Button`.

**Fix:** use a kind that has a control, or render your own control for that field and drive it through `FormController.SetValue`. The controller treats all kinds the same way for state, validation and output, except the stateless ones.

## Known limitations

- `Drawer` and `Fullscreen` shell modes render like `Modal`, in a centered dialog.
- `KlyFormPage` renders every field as a text input, and its labels are not programmatically associated with their inputs. `KlyFormEngine` links each label to its control.
- `FormFieldSchema.Multiple`, `OnCreateOption`, `Collapsible`, `DefaultCollapsed`, `DisplayKind`, `DisplayTone`, `FormSchema.Columns`, `FormStepSchema.Description`, `FormStepSchema.Root` and `FormPageSectionSchema.Span` are not used by the built-in components. `FormOptionItem.Description`, `Group` and `Icon` are not rendered either.
- Password requirements are validated but not shown as a checklist.
- Cells of repeatable lists are plain text inputs, so their values are strings.
- The controller is not thread safe.
- Opening a form with `FormService` while another is open replaces it, and the first task never completes.
- Validators are synchronous. Use `OnBeforeSubmit` or `OnBeforeNext` with `SetError` for checks that need I/O.

## Questions

### Can I translate the built-in texts?

Partly. `KlyFormEngine` exposes `SubmitLabel`, and each field exposes `RequiredMessage`, `Label`, `Hint`, `Help` and `Placeholder`, so those follow your language. The other texts are English literals in the components: `Back`, `Next`, `Reset`, `Cancel`, `Show` and `Hide` on password fields, `Saving...`, `No items yet`, the default `Select...` option of choice fields (override it with `Placeholder`), the `aria-label` texts, `Add item` (unless you set `AddItemLabel`), the number and bounds messages (`Value must be at least ...`), and on `KlyFormPage` the section sidebar, the status badges, the summary line and the Submit button.

### Can I bind a form to a model class?

Not directly. The output of a form is a flat dictionary, and the initial input is a dictionary too. Read the model into a dictionary when you create the controller and map `BuildOutput()` back when you save. For forms bound to a model with data annotations, see `KlyForm` in [Klyroo.UI](https://github.com/klyroo/klyroo-ui).

### Does it integrate with DataAnnotations or FluentValidation?

No. Validation is expressed with `Required`, `RequiredWhen`, `Min`, `Max`, `PasswordRequirements` and `Validators` on the schema. A `FormValidatorRule` can call any validator you already have.

### What types do the values have?

The built-in controls store strings for text-like kinds, `double` for `Number` and `Slider`, `bool` for `Checkbox` and `Switch`, and the option value for choices. From code you can store anything, but dirty tracking compares values with `Equals`, apart from string lists and repeatable lists, which are compared by content. `GetValue<T>` converts with the invariant culture and returns `default` when it cannot.

### How do labels and errors reach assistive technology?

In `KlyFormEngine` the label of a text, number, select or similar control is tied to it with `for` and `id`, errors are rendered with `role="alert"`, the stepper marks the current step with `aria-current`, and the close and clear buttons have an `aria-label`. This describes the markup; the library has no automated accessibility audit of the form components in this repository.
