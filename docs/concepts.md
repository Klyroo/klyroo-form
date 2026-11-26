# Concepts

This page explains how the pieces of Klyroo.Form fit together and why they work the way they do. For step-by-step instructions see [Getting started](getting-started.md) and the [guides](guides.md); for the exact signatures see the [API reference](api-reference.md).

## The pieces

```text
Klyroo.Form.Core (no UI dependency)

  FormSchema                 plain data: fields, steps or sections, hooks
    FormFieldSchema          one field, container or layout element
    FormStepSchema           one step of a wizard
    FormPageSectionSchema    one section of a page
         |
         v
  FormController             state: values, touched, errors, dirty, current step
         ^
         | opens and completes
  FormService                CreateFormAsync() -> Task<FormSubmitOutcome>

Klyroo.Form (Blazor, builds on Klyroo.UI)

  KlyFormEngine              renders a schema or a controller
  KlyFormPage                renders sections with a completion status
  KlySchemaField             renders one field; both components above use it
  KlyFormProvider            hosts the overlay opened by FormService
```

A `FormSchema` is declarative and holds no state, so one schema can back many controllers. A `FormController` is the runtime: it owns the values and everything derived from them. The components only render a controller and forward user input to it. They keep no form state of their own, apart from the selected tab, the revealed passwords and the text being typed in a tag, autocomplete or create-option input, which they attach to the controller so every component rendering it shares them, and, for `KlyFormEngine`, the controller it creates when you pass a schema.

For forms bound to a model class with data annotations, Klyroo.UI provides `KlyForm`, which works on an `EditContext`. Klyroo.Form is for forms described as data, where the fields, conditions and steps are decided at runtime or you want to run the form without a UI. It has no integration with `DataAnnotations`.

## The schema

`FormSchema` has three ways to declare its content, and exactly one is used:

1. `Steps`, when it is not empty. The form is a wizard.
2. `Sections`, when there are no steps. The form is a page, rendered by `KlyFormPage`.
3. `Fields`, otherwise.

The others are ignored: a schema with steps never reads its `Fields`.

Field keys are flat strings and identify values in the state and in the output. Containers (`Group`, `Card`, `Object`, `Tabs`) only affect layout: their children keep their own keys and become top-level entries of the output, so an `Object` field named `address` does not produce a nested `address` value. A container never has a value of its own, even without children, and then renders nothing. Repeatable lists (`ArrayList`, `ArrayTable`) are the exception: they are leaves whose value is a list of row dictionaries.

## State and initialization

The controller holds values in a dictionary of `object?` keyed by field key. When you construct it, or call `Reset`, it fills the state like this:

1. Values from the initial input are copied.
2. For each field that has an input value, `TransformInput` converts it.
3. For each field without an input value, the default applies: `DefaultFactory`, else `DefaultValue`, else an empty list for repeatable lists, else `null`.
4. The result is copied into an initial snapshot.

The built-in controls write strings for text-like kinds (dates, times, colors and phone numbers included), `double` for `Number` and `Slider`, `int` for `Rating`, `bool` for `Checkbox` and `Switch`, the option value (a string) for single choices, and a `List<string>` for `CheckboxGroup`, `Tag`, `DateRange` and choices with `Multiple`. `GetValue<T>` converts with the invariant culture and reads the formats the controls store: `GetValue<int>("age")` works on a whole `double` (and is `0` for 3.5, which is not rounded), `GetValue<DateOnly>("birthday")` on `yyyy-MM-dd`, `GetValue<TimeOnly>` on `HH:mm`, `GetValue<DateTime>` on `yyyy-MM-ddTHH:mm`, and `Nullable<T>`, `Guid`, enums (by name or member number) and `DateTimeOffset` (a value without a zone counts as UTC) work too. `TryGetValue<T>` tells a missing, blank or unconvertible value from a real default.

Stateless kinds (`Info`, `Divider`, `Section`, `Button`) have no value: they are never validated and never appear in the output.

## Touched fields, errors and validation

A field has a validation error only after validation ran on it, and the error is visible only if the field is touched. This keeps a pristine form quiet.

```text
SetValue(path, value, markTouched)
   |  clears an error set with SetError on the path
   |  touched if markTouched or the field's Trigger is Input
   |  validates the field if touched and Trigger is not Submit
   |  (with Trigger Submit, clears its validation error instead)
   |  calls Watch, raises Changed
MarkTouched(path)        touched, validates unless Trigger is Submit
Validate() / SubmitAsync()   touch and validate every field they check
```

One validation pass on a field applies these checks in order, and the first failure wins:

1. Required (`Required` or `RequiredWhen`), in modes `All` and `RequiredOnly`. Empty means `null`, a blank string or an empty collection. A required `Checkbox` or `Switch` must be checked: `false` fails the check.
2. Number bounds, for `Number` fields, in modes `All` and `ValidatorsOnly`.
3. `MaxLength`, for string values, in the same modes.
4. Password requirements, for `Password` fields, in the same modes.
5. Custom `Validators`, in the same modes. They also run for empty values.

A `DateRange` also fails when its end is before its start, and a required one needs both dates.

`FormSchema.ValidateMode` selects the checks: `All`, `RequiredOnly`, `ValidatorsOnly` or `None`. Only the fields in effect are checked (see the next section).

Each cell of a repeatable list is validated with the rules of its child field, row by row: `Validate()` checks every cell, and an edited cell follows the trigger of its child like a field does. Cell errors are keyed by a path such as `lines[0].sku`, read with `GetArrayItemError`, and count in `HasBlockingErrors`. Removing a row moves the errors of the following rows up.

After `SetValue`, `ResetField` or a change of a repeatable list, the controller validates again the other touched fields that currently show an error, and the touched fields that list the changed key in `FormFieldSchema.DependsOn`. That keeps cross-field rules (a confirmation, `RequiredWhen`) from leaving stale errors or a wrong `HasBlockingErrors`. Untouched fields and fields with the `Submit` trigger are not validated this way.

`GetError` and `Errors` return what should be displayed. `HasBlockingErrors` is broader: it reports any validation error of a field in effect, touched or not.

Errors that your own code sets with `SetError`, such as a server response, are a separate set. They are always visible, they disappear on the next `SetValue` for that field, and when you mark one `blocking` it also makes `Validate()` and `SubmitAsync()` fail until it is cleared.

The controller validates synchronously. Anything that needs I/O belongs in `OnBeforeSubmit` or `OnBeforeNext`, which are async, combined with `SetError`.

## Conditions and hidden fields

`Condition` decides whether a field is shown. A hidden field is not rendered, not validated and not part of `BuildOutput()`, but its value stays in the state. The same applies to everything a hidden thing contains: the children of a hidden container, the fields of a hidden tab or section, and the fields of a step that `SkipWhen` skips. `ActiveFields` lists the fields that remain, which are exactly the ones validation checks and the output contains. Validation also drops the errors of fields that left that list.

`Disabled` and `DisabledWhen` are rendering hints. A disabled field is still validated and still in the output, and `SetValue` still changes it. On a repeatable list they also disable the cells and the add and remove buttons.

`Watch` runs after the value of its field changes: through `SetValue`, through `ResetField` when the value differs, and for a repeatable list after a row is added or removed or a cell changes (a child's `Watch` receives the cell value). `Reset` does not call it, because it loads a complete state that is already consistent.

## Dirty tracking

The controller keeps the initial snapshot described above. `DirtyPaths` lists the keys whose current value differs from it, `IsDirty` says whether there are any, and `IgnoreDirtyPaths` removes keys from the comparison. String lists and repeatable lists are compared by content.

The snapshot is replaced in three cases: `Reset`, construction, and a successful `SubmitAsync`. That last one means a form is clean again after saving, so an unsaved-changes guard stops firing.

## Steps and sections

In a wizard, `NextStepAsync` follows this sequence and stops at the first `false`:

1. It does nothing if the form has no steps, is on its last step (every later step is skipped), or is submitting.
2. It validates the current step only.
3. It runs `OnBeforeNext` with the index of the current step.
4. It moves to the next step whose `SkipWhen` is not true, clears the touched state and validation errors, and raises `Changed`.

`PreviousStepAsync` mirrors it without validation. Moving between steps keeps all values. A new controller and `Reset` start on the first step that is not skipped, and `IsFirstStep` and `IsLastStep` look past skipped steps, so the components hide Back and show Submit at the right place. `SubmitAsync` validates the fields of the current step and of every step that is not skipped; the fields of skipped steps are neither validated nor output.

A page form has no navigation. `GetSectionStates` computes a status for each visible section from the current values and errors: `Invalid` if any field has an error, otherwise `Pending` if a required field is empty, otherwise `Optional` if the section is optional and empty, otherwise `Complete`.

## Submission

```text
SubmitAsync() / SubmitAsync(handler, ct)
   already submitting or changing step? -> false
   Validate() on every field            -> false on any error, or on a blocking
                                           form-level error (SubmitFailed = true)
   IsSubmitting = true
   OnBeforeSubmit(controller)           -> false cancels
   OnSubmit(controller)
   handler(controller, ct)              -> false cancels (your save step)
   new initial snapshot                 -> IsDirty = false
   IsSubmitting = false                 (also when a hook or the handler throws)
```

`SubmitAsync(handler)` exists so that the save step runs inside the lifecycle: `IsSubmitting` stays `true` while it works, and the snapshot is retaken only when it returns `true`. The components expose it as `OnSave` (`Func<output, CancellationToken, Task<bool>>`). Their `OnSubmit` callback is raised afterwards, with `BuildOutput()`, and only on success: the stateful fields in effect, after `TransformOutput`, as a flat dictionary. If `OnBeforeSubmit` or the handler returns `false`, or a hook throws, the snapshot is not replaced and the form stays dirty.

An error set with `SetError` on a key that matches no field is a form-level error (`SetFormError` uses the key `FormErrorKey`). A blocking one makes `Validate()` fail whatever step is validated, and `GetErrorEntries()` lists it after the field errors, which is what the error summary renders.

`FormService` wraps this for overlays. `CreateFormAsync` creates a controller, shows it in the provider and returns a task. Submitting completes the task with `IsCompleted = true`; closing completes it with `false`, and so does opening another form, which replaces the first. All carry the output at that moment.

## Core and Blazor

| | `Klyroo.Form.Core` | `Klyroo.Form` |
| :--- | :--- | :--- |
| Contains | Schema types, `FormController`, `FormService` | `KlyFormEngine`, `KlyFormPage`, `KlyFormProvider`, `AddKlyrooForm()` |
| Dependencies | None | Klyroo.UI and `Klyroo.Form.Core` |
| Runs in | Any .NET 10 host, including NativeAOT | Blazor (Interactive Server, WebAssembly, Auto) and .NET MAUI Blazor Hybrid |
| Use it for | Tests, view models, background logic | Rendering |

The split exists so that the behavior of a form (what is required, what is hidden, what a wizard does next) can be tested and reused without a renderer. The components stay thin: they translate input events into `SetValue`, `MarkTouched` and `SubmitAsync` calls and render what the controller reports.

## Render modes and threading

The controller raises `Changed` synchronously, on the thread that made the change. `KlyFormEngine`, `KlyFormPage` and `KlyFormProvider` subscribe to it and re-render through `InvokeAsync`, so a change from another thread does not touch the renderer from the wrong context. The controller itself is made of plain dictionaries and is not thread safe: serialize access if more than one thread uses it.

The components use Blazor events for everything the form does. The only JavaScript interop is a best-effort call that moves focus (to the first invalid field after a failed submit or Next, or when an entry of the error summary is clicked), through the small `klyroo-form.js` module shipped with the package: when the call fails, as in prerendering or a test host without JavaScript, the form still works and focus just stays where it was. The overlay of `KlyFormProvider` is a Klyroo.UI modal, which brings its own script. In Static SSR the markup renders but there is nothing to run the event handlers, so the controls are not interactive. [Platforms](platforms.md) covers render modes, .NET MAUI and NativeAOT.

## Error handling

Validation failures are not exceptions. They are values: `Validate()` and `SubmitAsync()` return `false`, and the errors are readable through `GetError` and `Errors`. Exceptions thrown by your delegates (conditions, validator rules, `Watch`, hooks) are not caught: they propagate to the caller of `SetValue`, `Validate`, `SubmitAsync` and so on, or to the renderer when a condition throws during rendering. `SubmitAsync` still resets `IsSubmitting` when a hook throws.

## Design decisions and trade-offs

- **Untyped values keyed by strings.** A form is a dictionary of `object?`, which makes forms that are built from data or change shape at runtime easy, and means there is no compile-time check that a key exists or that a value has the type you expect. You map the output dictionary to your own type.
- **Flat keys.** Containers do not nest values, which keeps paths simple and dirty tracking cheap, and means an `address.street` structure is a naming convention (`address.street` as the key, for example), not a nested object.
- **Synchronous validators, async hooks.** Rules are `Func<object?, FormController, bool>`, so validation can run on every keystroke without awaiting. Server checks go in `OnBeforeSubmit` or `OnBeforeNext` and report through `SetError`.
- **A mutable controller with an event.** The state is mutated in place and `Changed` tells observers. It is small, and it also means you own synchronization if you share a controller across threads.
- **Native controls, almost no JavaScript.** The components render every kind with native elements (`select`, `datalist`, `details`, date and color inputs, radio-based stars) and Blazor events, plus one optional focus helper. The tag and create-option inputs avoid submitting the form on Enter with a `form` attribute that points at no form, instead of a script.
