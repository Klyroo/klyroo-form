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
  KlyFormProvider            hosts the overlay opened by FormService
```

A `FormSchema` is declarative and holds no state, so one schema can back many controllers. A `FormController` is the runtime: it owns the values and everything derived from them. The components only render a controller and forward user input to it. They keep no form state of their own, apart from the selected tab, the revealed passwords and, for `KlyFormEngine`, the controller it creates when you pass a schema.

For forms bound to a model class with data annotations, Klyroo.UI provides `KlyForm`, which works on an `EditContext`. Klyroo.Form is for forms described as data, where the fields, conditions and steps are decided at runtime or you want to run the form without a UI. It has no integration with `DataAnnotations`.

## The schema

`FormSchema` has three ways to declare its content, and exactly one is used:

1. `Steps`, when it is not empty. The form is a wizard.
2. `Sections`, when there are no steps. The form is a page, rendered by `KlyFormPage`.
3. `Fields`, otherwise.

The others are ignored: a schema with steps never reads its `Fields`.

Field keys are flat strings and identify values in the state and in the output. Containers (`Group`, `Card`, `Object`, `Tabs`) only affect layout: their children keep their own keys and become top-level entries of the output, so an `Object` field named `address` does not produce a nested `address` value. Repeatable lists (`ArrayList`, `ArrayTable`) are the exception: they are leaves whose value is a list of row dictionaries.

## State and initialization

The controller holds values in a dictionary of `object?` keyed by field key. When you construct it, or call `Reset`, it fills the state like this:

1. Values from the initial input are copied.
2. For each field that has an input value, `TransformInput` converts it.
3. For each field without an input value, the default applies: `DefaultFactory`, else `DefaultValue`, else an empty list for repeatable lists, else `null`.
4. The result is copied into an initial snapshot.

The built-in controls write strings for text-like kinds, `double` for `Number` and `Slider`, `bool` for `Checkbox` and `Switch`, and the option value (a string) for choices. `GetValue<T>` converts with the invariant culture, so `GetValue<int>("age")` works on a `double`.

Stateless kinds (`Info`, `Divider`, `Section`, `Button`) have no value: they are never validated and never appear in the output.

## Touched fields, errors and validation

A field has a validation error only after validation ran on it, and the error is visible only if the field is touched. This keeps a pristine form quiet.

```text
SetValue(path, value, markTouched)
   |  clears an error set with SetError on the path
   |  touched if markTouched or the field's Trigger is Input
   |  validates the field if touched and Trigger is not Submit
   |  calls Watch, raises Changed
MarkTouched(path)        touched, validates unless Trigger is Submit
Validate() / SubmitAsync()   touch and validate every field they check
```

One validation pass on a field applies these checks in order, and the first failure wins:

1. Required (`Required` or `RequiredWhen`), in modes `All` and `RequiredOnly`. Empty means `null`, a blank string or an empty collection.
2. Number bounds, for `Number` fields, in modes `All` and `ValidatorsOnly`.
3. Password requirements, for `Password` fields, in the same modes.
4. Custom `Validators`, in the same modes. They also run for empty values.

`FormSchema.ValidateMode` selects the checks: `All`, `RequiredOnly`, `ValidatorsOnly` or `None`. Hidden and ignored fields are skipped.

`GetError` and `Errors` return what should be displayed. `HasBlockingErrors` is broader: it reports any validation error, touched or not.

Errors that your own code sets with `SetError`, such as a server response, are a separate set. They are always visible, they disappear on the next `SetValue` for that field, and when you mark one `blocking` it also makes `Validate()` and `SubmitAsync()` fail until it is cleared.

The controller validates synchronously. Anything that needs I/O belongs in `OnBeforeSubmit` or `OnBeforeNext`, which are async, combined with `SetError`.

## Conditions and hidden fields

`Condition` decides whether a field is shown. A hidden field is not rendered, not validated and not part of `BuildOutput()`, but its value stays in the state. This is applied per field: the controller does not look at the condition of a container when it handles the children, so conditions on containers only change what is rendered.

`Disabled` and `DisabledWhen` are rendering hints. A disabled field is still validated and still in the output, and `SetValue` still changes it.

## Dirty tracking

The controller keeps the initial snapshot described above. `DirtyPaths` lists the keys whose current value differs from it, `IsDirty` says whether there are any, and `IgnoreDirtyPaths` removes keys from the comparison. String lists and repeatable lists are compared by content.

The snapshot is replaced in three cases: `Reset`, construction, and a successful `SubmitAsync`. That last one means a form is clean again after saving, so an unsaved-changes guard stops firing.

## Steps and sections

In a wizard, `NextStepAsync` follows this sequence and stops at the first `false`:

1. It does nothing if the form has no steps, is on its last step, or is submitting.
2. It validates the current step only.
3. It runs `OnBeforeNext` with the index of the current step.
4. It moves to the next step whose `SkipWhen` is not true, clears the touched state and validation errors, and raises `Changed`.

`PreviousStepAsync` mirrors it without validation. Moving between steps keeps all values. `SubmitAsync` validates every field of every step, skipped ones included.

A page form has no navigation. `GetSectionStates` computes a status for each visible section from the current values and errors: `Invalid` if any field has an error, otherwise `Pending` if a required field is empty, otherwise `Optional` if the section is optional and empty, otherwise `Complete`.

## Submission

```text
SubmitAsync()
   already submitting?          -> false
   Validate() on every field    -> false on any error
   IsSubmitting = true
   OnBeforeSubmit(controller)   -> false cancels
   OnSubmit(controller)
   new initial snapshot         -> IsDirty = false
   IsSubmitting = false         (also when a hook throws)
```

The components then call their `OnSubmit` callback with `BuildOutput()`: the visible, stateful fields, after `TransformOutput`, as a flat dictionary. If `OnBeforeSubmit` returns `false` or a hook throws, the snapshot is not replaced and the form stays dirty.

`FormService` wraps this for overlays. `CreateFormAsync` creates a controller, shows it in the provider and returns a task. Submitting completes the task with `IsCompleted = true`; closing completes it with `false`. Both carry the output at that moment.

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

The components use only Blazor events and make no JavaScript interop calls of their own. In Static SSR the markup renders but there is nothing to run the event handlers, so the controls are not interactive. [Platforms](platforms.md) covers render modes, .NET MAUI and NativeAOT.

## Error handling

Validation failures are not exceptions. They are values: `Validate()` and `SubmitAsync()` return `false`, and the errors are readable through `GetError` and `Errors`. Exceptions thrown by your delegates (conditions, validator rules, `Watch`, hooks) are not caught: they propagate to the caller of `SetValue`, `Validate`, `SubmitAsync` and so on, or to the renderer when a condition throws during rendering. `SubmitAsync` still resets `IsSubmitting` when a hook throws.

## Design decisions and trade-offs

- **Untyped values keyed by strings.** A form is a dictionary of `object?`, which makes forms that are built from data or change shape at runtime easy, and means there is no compile-time check that a key exists or that a value has the type you expect. You map the output dictionary to your own type.
- **Flat keys.** Containers do not nest values, which keeps paths simple and dirty tracking cheap, and means an `address.street` structure is a naming convention (`address.street` as the key, for example), not a nested object.
- **Synchronous validators, async hooks.** Rules are `Func<object?, FormController, bool>`, so validation can run on every keystroke without awaiting. Server checks go in `OnBeforeSubmit` or `OnBeforeNext` and report through `SetError`.
- **A mutable controller with an event.** The state is mutated in place and `Changed` tells observers. It is small, and it also means you own synchronization if you share a controller across threads.
- **Minimal built-in rendering.** `KlyFormEngine` uses native inputs and renders nine of the 31 kinds as text inputs. The nine kinds without a dedicated control are listed in the [FAQ](faq.md); the controller handles their values like any other.
