# Concepts

This page explains how the pieces of Klyroo.Form fit together and why they work the way they do. For step-by-step instructions see [Getting started](getting-started.md) and the [guides](guides.md); for the exact signatures see the [API reference](api-reference.md). The page describes the typed API, which is how forms are declared; the runtime mode for forms stored as data has [its own page](runtime-forms.md).

## The pieces

```text
Klyroo.Form.Core (no UI dependency)

  FormSchema.For<TModel>        typed mode: declares fields, steps, rules and hooks from a model
    FormSchema<TModel>          immutable schema, holds FormField<TModel, TValue> handles
         |
         v
    FormController<TModel>      state read and written as the members of the model
         ^
         | opens and completes
    FormService                 CreateFormAsync<TModel>() -> Task<FormSubmitOutcome<TModel>>

  Klyroo.Form.Dynamic           runtime mode: a form is data, values are object? keyed by name
    DynamicFormSchema           plain data: fields, steps or sections, hooks
      DynamicFieldSchema        one field, container or layout element
      DynamicStepSchema         one step of a wizard
      DynamicSectionSchema      one section of a page
         |
         v
    DynamicFormController       state: values, touched, errors, dirty, current step
         ^
         | opens and completes
    DynamicFormService          CreateFormAsync() -> Task<DynamicFormOutcome>

  Both modes compile to one internal FieldNode tree and run on one internal FormEngine.
  IFormRuntime and IFormFieldAccess give a driver (an agent) a JSON view of either controller.

Klyroo.Form (Blazor, builds on Klyroo.UI)

  KlyFormEngine<TModel>         renders a typed schema or controller, with a Model
  KlyFormPage<TModel>           renders its sections with a completion status
  KlyFormFieldView<TModel>      renders one field of a typed controller, for your own layout
  KlyFieldTemplate              draws the control of one typed field from a FormControlContext<TValue>
  KlyFormProvider               hosts the overlays opened by FormService and DynamicFormService, in a KlyModal or KlySlideover
  KlyFormErrorSummary           lists the errors and announces them; the form components render it
  KlyFormNavigationGuard        blocks navigation while the controller is dirty (either controller)
  KlyDynamicFormEngine, KlyDynamicFormPage, KlyDynamicFieldView   the same three components for a dynamic schema or controller

  One renderer draws the fields of both modes: it reads the FieldNode tree through the engine, so the markup, the ids and the ARIA
  attributes are the same for a typed and a dynamic form.
```

A schema is declarative and holds no state, so one schema can back many controllers. A controller is the runtime: it owns the values and everything derived from them. The components only render a controller and forward user input to it. They keep no form state of their own, apart from the selected tab, the revealed passwords and the text being typed in a tag, autocomplete or create-option input, which they attach to the controller so every component rendering it shares them, and, for `KlyFormEngine<TModel>`, the controller it creates when you pass a schema.

The typed mode is for forms bound to a model class, where the compiler checks the members and the types of the values. The runtime mode is for forms described as data, where the fields, conditions and steps are decided at runtime, and for code that works with values as text and JSON. Klyroo.UI also provides `KlyForm`, which works on an `EditContext`; Klyroo.Form is for forms described as a schema, with conditions, steps and a headless controller.

## The schema

`FormSchema.For<TModel>(s => ...)` runs the builder once and returns an immutable `FormSchema<TModel>`. The builder declares the content in one of three ways:

1. Fields, declared directly on the builder: a single form.
2. `s.Step(key, title, configure, skipWhen: ...)` blocks: a wizard.
3. `s.Section(key, label, configure, optional: ...)` blocks: a page, rendered by `KlyFormPage<TModel>`.

`s.Field(m => m.Email, f => ...)` declares one field from a member of the model and returns its handle, a `FormField<TModel, TValue>`. The handle carries the type of the value, which is how `controller.Get(handle)` returns an `int` for an `int` member and a conversion mistake does not compile. Settings are methods on the field builder; those that only fit some kinds of value are extension methods constrained on `TValue`. The key of a field is the member name, and the names of the members down to it joined with dots for a nested one (`Address.City`). The kind of control comes from the member type.

`Group`, `Card` and `Tabs` lay fields out. `Group` and `List` follow the structure of the model: `Group(m => m.Address, ...)` declares the fields of an object member and creates the object when a value is written, and `List(m => m.Lines, ...)` edits a collection member as repeatable rows, whose cells are validated. `Card` and `Tabs` are layout only. A container never has a value of its own, even without children, and then renders nothing. `Heading`, `Info`, `Divider` and `Button` are stateless: they have no value, are never validated and never reach the model.

## State and initialization

The engine holds control values, the raw data a control produces, in a dictionary keyed by field key. In the typed mode a codec converts at the boundary of the controller: `Get` turns the stored value into the type of the member and `Set` turns your value into what the control stores, so the engine can still hold a text such as `abc` in a number field while the user is typing, and report `Enter a number` without losing it. The same codec reads and writes invariant text and JSON, which is what drafts and agents use. Numbers, enums and dates are converted with the invariant culture, so a `decimal` round-trips under any thread culture.

When you construct a controller, or call `Reset(model)`, it fills the state like this:

1. The values of the model are read through the accessors of the fields (a controller without a model starts from a new one, so property initializers are the defaults of the form, and a `Default(...)` on a field wins over them).
2. For each field with a value, `TransformInput` converts it.
3. The result is copied into an initial snapshot, the baseline of dirty tracking.

`CreateModel()` builds a new model from the current values, and `ApplyTo(target)` writes them into one you already have, all or nothing.

## Touched fields, errors and validation

A field has a validation error only after validation ran on it, and the error is visible only if the field is touched. This keeps a pristine form quiet.

```text
Set(field, value, markTouched)
   |  clears an error set with SetError on the field
   |  touched if markTouched or the field's Trigger is Input
   |  validates the field if touched and Trigger is not Submit
   |  (with Trigger Submit, clears its validation error instead)
   |  calls OnChange, raises Changed
Touch(field)             touched, validates unless Trigger is Submit
Validate() / SubmitAsync()   touch and validate every field they check
```

One validation pass on a field applies these checks in order, and the first failure wins:

1. Required (`Required()` or `Required(condition)`), in modes `All` and `RequiredOnly`. Empty means `null`, a blank string or an empty collection. A required `bool` must be true.
2. Number bounds (`Range`), in modes `All` and `ValidatorsOnly`.
3. `MaxLength`, for string values, in the same modes.
4. Password requirements, for `AsPassword(...)` fields, in the same modes.
5. Custom rules (`Rule`), in the same modes. They also run for empty values.

A date range also fails when its end is before its start, and a required one needs both dates. A value that is there but is not a value of the member type (text for a number, a date that does not parse, free text from a custom control) is never turned into `default`: validation reports it as an error on the field, whatever the validation mode, and `Validate` and `SubmitAsync` fail without throwing.

`s.ValidateMode` selects the checks: `All`, `RequiredOnly`, `ValidatorsOnly` or `None`. Only the fields in effect are checked (see the next section).

Each cell of a repeatable list is validated with the rules of its column, row by row: `Validate()` checks every cell, and an edited cell follows the trigger of its column like a field does. Cell errors are keyed by a path such as `lines[0].sku`, read with `row.GetError(column)`, and count in `HasBlockingErrors`. Removing a row moves the errors of the following rows up.

After `Set`, `ResetField` or a change of a repeatable list, the controller validates again the other touched fields that currently show an error, and the touched fields that list the changed field in `DependsOn`. That keeps cross-field rules (a confirmation, `Required(condition)`) from leaving stale errors or a wrong `HasBlockingErrors`. Untouched fields and fields with the `Submit` trigger are not validated this way.

`GetError` and `Errors` return what should be displayed. `HasBlockingErrors` is broader: it reports any validation error of a field in effect, touched or not.

Errors that your own code sets with `SetError`, such as a server response, are a separate set. They are always visible, they disappear on the next `Set` for that field, and when you mark one `blocking` it also makes `Validate()` and `SubmitAsync()` fail until it is cleared.

`Validate()` and the rules in `Rule` are synchronous. A check that needs I/O, such as "is this username free", is a `RuleAsync` rule: it runs in the background once the field is touched and while it is edited (debounced, an older run cancelled by a newer one), `IsValidating(field)` reports it so the components can show a pending state, and `SubmitAsync()`, `NextStepAsync()` and `ValidateAsync()` wait for it. A rule does not run for an empty value or for one that already fails a synchronous rule, and it applies to fields, not to the cells of a repeatable list. `OnBeforeSubmit` and `OnBeforeNext` stay the place for checks that belong to the whole submission, combined with `SetError`, and `s.Validate((model, errors) => ...)` for checks on the whole model.

## Conditions and hidden fields

`VisibleWhen` decides whether a field is shown. A hidden field is not rendered, not validated and not written into the model, but its value stays in the form state. The same applies to everything a hidden thing contains: the children of a hidden `Card`, the fields of a hidden tab or section, and the fields of a step that `skipWhen` skips. Those are exactly the fields in effect: the ones validation checks and a submit writes. Validation also drops the errors of fields that left that set.

`Disabled` and `DisabledWhen` are rendering hints, and every field also counts as disabled (`IsDisabled`) while `IsSubmitting` is `true`. A disabled field is still validated and still written into the model, and `Set` still changes it. On a repeatable list they also disable the cells and the add and remove buttons.

`OnChange` runs after the value of its field changes: through `Set`, through `ResetField` when the value differs, and for a repeatable list after a row is added or removed or a cell changes. `Reset` does not call it, because it loads a complete state that is already consistent.

## Dirty tracking

The controller keeps the initial snapshot described above. `IsDirty` says whether any value differs from it, `IsFieldDirty(field)` asks about one field, and `s.IgnoreDirty(handles)` removes fields from the comparison. String lists and repeatable lists are compared by content.

The snapshot is replaced in three cases: `Reset(model)`, construction, and a successful `SubmitAsync`. That last one means a form is clean again after saving, so an unsaved-changes guard stops firing. `Reset()` without a model puts the values back to the snapshot.

## Steps and sections

In a wizard, `NextStepAsync` follows this sequence and stops at the first `false`:

1. It does nothing if the form has no steps, is on its last step (every later step is skipped), or is submitting.
2. It validates the current step only.
3. It runs `OnBeforeNext` with the index of the current step.
4. It moves to the next step whose `skipWhen` is not true, clears the touched state and validation errors, and raises `Changed`.

`PreviousStepAsync` mirrors it without validation. Moving between steps keeps all values. A new controller and `Reset` start on the first step that is not skipped, and `IsFirstStep` and `IsLastStep` look past skipped steps, so the components hide Back and show Submit at the right place. `SubmitAsync` validates the fields of the current step and of every step that is not skipped; the fields of skipped steps are neither validated nor written.

A page form has no navigation. `GetSectionStates` computes a status for each visible section from the current values and errors: `Invalid` if any field has an error, otherwise `Pending` if a required field is empty, otherwise `Optional` if the section is optional and empty, otherwise `Complete`.

## Submission

```text
SubmitAsync() / SubmitAsync(save, target, ct)
   already submitting or changing step? -> false
   Validate() on every field            -> false on any error, or on a blocking
                                           form-level error (SubmitFailed = true)
   IsSubmitting = true
   write the values into target, or a new model; model checks (s.Validate)
   OnBeforeSubmit(model, controller)    -> false cancels
   OnSubmit(model, controller)
   save(model, ct)                      -> false cancels (your save step)
   new initial snapshot                 -> the values the save step received; IsDirty = false
   IsSubmitting = false                 (also when a hook or the save step throws)
```

`SubmitAsync(save, target)` exists so that the save step runs inside the lifecycle: `IsSubmitting` stays `true` while it works, and the snapshot is retaken only when it returns `true`. A step that fails, or a `save` that returns `false` or throws, puts the target back as it was: only the members the write changed, and nothing your hooks or your save step did. The components expose the save step as `OnSave` (`Func<TModel, CancellationToken, Task<bool>>`). Their `OnSubmit` callback is raised afterwards, with the model, and only on success. If `OnBeforeSubmit` or the save step returns `false`, or a hook throws, the snapshot is not replaced and the form stays dirty.

An error set with `SetFormError` is a form-level error. A blocking one makes `Validate()` fail whatever step is validated, and it is listed in the error summary after the field errors.

`FormService` opens typed forms as overlays. `CreateFormAsync<TModel>(schema, model, mode)` creates a controller, shows it in the provider and returns a task of `FormSubmitOutcome<TModel>`, which carries the submitted model. Submitting completes the task with `IsCompleted = true`; closing completes it with `false`, and so does opening another form, which replaces the first.

## Localization

Numbers and file sizes in the built-in messages follow the language of the set: French reports `3,5` and `1,5 Mo`, English `3.5` and `1.5 MB` (`FormMessages.FileSize` formats a size, and each locale chooses its decimal separator and unit names).

The controller has no locale, so its built-in validation messages (required, number bounds, text length, date range order, files) are a `FormMessages` record: English by default, `s.Messages` or `FormController<TModel>.Messages` to choose another, and `FormMessageLocales` for the twelve built-in sets, which work without Blazor. A message that contains a number or a name is a function, so each language can order the words and choose its plural forms. `Required(message: ...)` on a field and the messages of your own rules are never translated.

The components add the texts of their own: `FormTexts`, one set per locale of Klyroo.UI in `FormLocales`, picked with the locale of the component (`KlyLocales.ResolveCode`, so `pt-PT` gets `pt` and an unknown language gets English). A set holds a `FormMessages`, and a component gives it to the controller it renders unless the schema or your code already chose one. `AddKlyrooForm(f => f.Texts("fr", t => t with { ... }))` changes a set; the texts become read-only once a component has resolved one. English and French are reviewed by a native speaker, the other sets are drafts.

## Core and Blazor

| | `Klyroo.Form.Core` | `Klyroo.Form` |
| :--- | :--- | :--- |
| Contains | `FormSchema`, `FormController<TModel>`, `FormControlContext<TValue>`, `IFormRuntime`, `FormMessages`, and the `Klyroo.Form.Dynamic` types of the runtime mode | `KlyFormEngine<TModel>`, `KlyFormPage<TModel>`, `KlyFormFieldView<TModel>`, `KlyFieldTemplate`, `KlyFormProvider`, `KlyFormNavigationGuard`, `FormService`, `FormTexts`, `AddKlyrooForm()`, and the dynamic components |
| Dependencies | None | Klyroo.UI and `Klyroo.Form.Core` |
| Runs in | Any .NET 10 host, including NativeAOT | Blazor (Interactive Server, WebAssembly, Auto) and .NET MAUI Blazor Hybrid |
| Use it for | Tests, view models, background logic | Rendering |

The split exists so that the behavior of a form (what is required, what is hidden, what a wizard does next) can be tested and reused without a renderer. The components stay thin: they translate input events into `Set`, `Touch` and `SubmitAsync` calls and render what the controller reports.

## Render modes and threading

The controller raises `Changed` synchronously, on the thread that made the change. `KlyFormEngine<TModel>`, `KlyFormPage<TModel>` and `KlyFormProvider` subscribe to it and re-render through `InvokeAsync`, so a change from another thread does not touch the renderer from the wrong context. The controller itself is made of plain dictionaries and is not thread safe: serialize access if more than one thread uses it.

The components use Blazor events for everything the form does. The only JavaScript interop is a best-effort call that moves focus (to the first invalid field after a failed submit or Next, or when an entry of the error summary is clicked), through the small `klyroo-form.js` module shipped with the package: when the call fails, as in prerendering or a test host without JavaScript, the form still works and focus just stays where it was. The overlay of `KlyFormProvider` is a Klyroo.UI modal, which brings its own script. In Static SSR the markup renders but there is nothing to run the event handlers, so the controls are not interactive. [Platforms](platforms.md) covers render modes, .NET MAUI and NativeAOT.

## Error handling

Validation failures are not exceptions. They are values: `Validate()` and `SubmitAsync()` return `false`, and the errors are readable through `GetError` and `Errors`. Exceptions thrown by your delegates (conditions, rules, `OnChange`, hooks) are not caught: they propagate to the caller of `Set`, `Validate`, `SubmitAsync` and so on, or to the renderer when a condition throws during rendering. `SubmitAsync` still resets `IsSubmitting` when a hook throws.

## The runtime mode

The runtime mode (`Klyroo.Form.Dynamic`) has the same engine, validation, steps, sections and components, with a schema made of plain data and values keyed by name, which suits a form stored as JSON or in a database. Its vocabulary differs (`SetValue`, `Condition`, `Validators`, `BuildOutput()`), and [Forms defined at runtime](runtime-forms.md) lists the correspondence, the storage types of the values and the recipes.

## Design decisions and trade-offs

- **Typed facade over an untyped engine.** The engine stores control values as `object?` keyed by string, because it must hold what a control produces, including a text that is not a number yet. The typed mode puts a facade on it: handles carry the type of the value, so a rename or a wrong type is a compile error, and a codec converts at the boundary. The runtime mode exposes the dictionary directly, which makes forms built from data easy and means nothing checks that a key exists or that a value has the type you expect. Both are views of the same engine and share its behavior and its tests (see [Declare a form from a C# model](guides.md#declare-a-form-from-a-c-model)).
- **Handles, not strings.** A condition or a rule points at a field through the handle the builder returned, so a field that is renamed or removed breaks the code that reads it at compile time, and `controller.Get(handle)` needs no cast. The cost is that a later condition needs the local the builder returned, or `schema.Field(m => m.X)`.
- **Flat keys.** The engine keeps one flat dictionary, which keeps paths simple and dirty tracking cheap. A typed schema follows the structure of the model on top of it: `m => m.Address.City` gives the key `Address.City`, and the values are copied into and out of the nested object when the model is loaded and written.
- **Synchronous rules, async rules and hooks.** Rules are `Func<TValue, bool>`, so validation can run on every keystroke without awaiting. Per-field server checks are `RuleAsync`, which run apart from the synchronous path and are awaited by submission; checks that belong to the whole submission go in `OnBeforeSubmit` or `OnBeforeNext` and report through `SetError`.
- **A mutable controller with an event.** The state is mutated in place and `Changed` tells observers. It is small, and it also means you own synchronization if you share a controller across threads.
- **Native controls, almost no JavaScript.** The components render every kind with native elements (`select`, `datalist`, `details`, date and color inputs, radio-based stars) and Blazor events, plus one optional focus helper. The tag and create-option inputs avoid submitting the form on Enter with a `form` attribute that points at no form, instead of a script.
