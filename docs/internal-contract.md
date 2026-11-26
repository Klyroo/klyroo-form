# Internal contract of the engine

This note is for the people who build on the internals of `Klyroo.Form.Core`: the typed components (`KlyFormEngine<TModel>`, `KlyFormPage<TModel>`, `KlyFieldTemplate`, `FormService<TModel>`) and the agent bridge. Nothing here is public API. `Klyroo.Form`, `Klyroo.Form.DataAnnotations` and `Klyroo.Form.Tests` see these types through `InternalsVisibleTo`; other packages go through `IFormRuntime`.

## Layers

```text
FormSchema.For<TModel>  --builds-->  FormSchema<TModel>  --holds-->  FormDefinition (FieldNode, StepNode, SectionNode, TabNode)
                                                                  +  IFieldBinding<TModel> list, typed handles, hooks
DynamicFormSchema       --compiles->  CompiledDynamic     --holds-->  FormDefinition + a map from each public schema object to its node

FormController<TModel>  --wraps-->   FormEngine (one per controller)  <--wraps--  DynamicFormController
```

A schema (either kind) owns a `FormDefinition`, which is immutable and shared. A controller owns one `FormEngine`, which runs that definition. Every delegate in a definition takes the `FormEngine`; the public wrappers adapt user delegates with `engine.Owner`, the public controller that created the engine, so a user condition receives `FormController<TModel>` or `DynamicFormController`, never the engine.

Create an engine in two steps, because a default factory or a skip condition may call back into the owner: `new FormEngine(definition, owner)`, assign it to the owner field, then `engine.Start(initialInput)`.

## FormDefinition and nodes

`FormDefinition` is the old `FormSchema` data: `FormKey`, `Title`, `Description`, `Eyebrow`, `Columns`, `ShowStepper`, `ValidateMode`, `Messages`, `IgnoreDirtyPaths`, `Fields`, `Steps`, `Sections`, and the hooks `OnBeforeSubmit`, `OnSubmit`, `OnBeforeNext`, `OnBeforePrevious` (all `Func<FormEngine, ...>`). `AllNodes()` walks every node that is not a column of a list.

`FieldNode` is the old `FormFieldSchema`, with the same members: `Key`, `Kind`, texts, `DefaultValue`, `DefaultFactory`, `Required`, `RequiredWhen`, `Trigger`, `DependsOn`, `Disabled`, `DisabledWhen`, `Condition`, `Ignore`, `OmitFromOutput`, `Span`, `Min`, `Max`, `Step`, `Accept`, `MaxFileSize`, `CustomKey`, `MaxLength`, `Multiple`, `Clearable`, `Options`, `OptionsFactory`, `OnCreateOption`, `PasswordRequirements`, `Validators` (`ValidatorRule`), `AsyncValidators` (`AsyncValidatorRule`), `TransformInput`, `TransformOutput`, `Watch`, `OnClick`, the display members, `Fields` (children, or the columns of a list) and `Tabs`. Two additions:

- `Source`: the public schema object the node was compiled from (`DynamicFieldSchema`, `DynamicStepSchema`, ...), or `null` for a typed node. `DynamicFormController` uses it to give public schema objects back.
- `ControlCheck`: for a typed field, says why a control value is not a value of the field type (`null` when it is). The engine does not apply it in `SetValue`, which accepts any control value so a control can hold text such as `abc`, but `CheckValueSync` applies it when the field is validated, after the rules of the field and whatever the validation mode: a control value that is there and is not a value of the type is the error `FormMessages.InvalidValue`, for a field and for a cell of a list. So a draft with `{"Day":"abc"}` for a `DateOnly`, `["x","y"]` for a `FormDateRange`, or free text from a custom control, restores without error, fails `Validate` and the submit, and never reaches the write, which would throw. The rules keep their own wording for numbers and identifiers (`WholeNumber`, `NumberOutOfRange`, `InvalidIdentifier`). `FieldNode.JsonReader` reads JSON through the codec, which includes the same check.

Values stored in the engine are control values: a `string` for text-like kinds (dates and times as `yyyy-MM-dd`, `yyyy-MM-ddTHH:mm`, `HH:mm`), `double` or `decimal` for `Number` and `Slider`, `int` for `Rating`, `bool`, the option text for single choices, `List<string>` for tags, checkbox groups, multiple choices and a date range (two items), `FormFileValue` or `List<FormFileValue>` for files, whatever a custom renderer stores for `Custom`, and `List<Dictionary<string, object?>>` for the rows of a list.

## FormEngine

The members the components and controllers use (all `internal`, same behavior and tests as the old `FormController`):

| Area | Members |
| :--- | :--- |
| Lifecycle | `FormEngine(definition, owner)`, `Start(input)`, `Reset(input)`, `ResetToSnapshot()`, `Dispose()`, `Changed`, `Owner`, `Schema` (the definition) |
| State | `GetValue(path)`, `GetValue<T>`, `TryGetValue<T>`, `GetInitialValue`, `SetValue(path, value, markTouched)`, `MarkTouched`, `ResetField`, `ResetSection`, `InternalState`, `DirtyPaths`, `IsDirty`, `BuildOutput()` |
| Errors | `Errors`, `GetError`, `SetError`, `ClearError`, `SetFormError`, `FormErrors`, `GetErrorEntries`, `HasBlockingErrors`, `SubmitFailed`, `SubmitFailureCount`, `FormErrorKey`, `Messages`, `UseLocaleMessages` |
| Fields | `AllFields`, `ActiveFields`, `CurrentVisibleTopFields`, `ShouldRenderField(node)`, `IsFieldRequired(node)`, `IsFieldDisabled(node)`, `GetFieldOptions(node)`, `CreateFieldOption(node, label)`, `IsSectionVisible(node)` |
| Rows | `GetArrayItems(key)`, `AddArrayItem(node)`, `RemoveArrayItem`, `SetArrayItemValue`, `MarkArrayItemTouched`, `GetArrayItemError`, `CellPath` |
| Validation | `Validate(currentStepOnly)`, `ValidateAsync`, `IsValidating(path)`, `HasPendingValidation`, `CancelPendingValidations()` |
| Steps and submit | `IsStepped`, `CurrentStep`, `CurrentStepIndex`, `IsFirstStep`, `IsLastStep`, `NextStepAsync()`, `PreviousStepAsync()`, `SubmitAsync()`, `SubmitAsync(handler, ct)` with `handler(engine, ct)`, `IsSubmitting`, `GetSectionStates()` |
| Drafts | `ExportState(includeSensitive)`, `RestoreDraft(draft)` (a `FormDraft` keeps its values internal) |
| Statics | `IsStateless(kind)`, `IsContainer(kind)`, `IsEmptyValue(value)` |

`FormEngine` is not thread safe and raises `Changed` synchronously, like the controllers.

## Typed side

`FormSchema<TModel>` exposes to the internals: `Definition`, `Bindings` (one `IFieldBinding<TModel>` per bound field), `Writable`, `Factory`, `State` (checks and hooks), `TryFindNode(key, out node)`, `ToInput(model, skipDefaulted)`, `PlanWrite(values, target)` and `Create(values)`.

A handle (`FormField<TModel, TValue>`, `FormListField`, `FormItem<TModel>`) carries `Key`, `Node`, and for a field `Codec` and `Binding`. They are assigned when the schema is built, so a handle used while the schema is still being declared throws.

`FormCodec<TValue>` (abstract, one per field, chosen from the final kind of the field):

| Member | Meaning |
| :--- | :--- |
| `Kind` | the kind the codec was made for |
| `ToControl(TValue)` | the control value of a value |
| `TryFromControl(raw, out TValue)` | `ControlValue.Value`, `Empty` (the default is given) or `Invalid` (the default is given, and a write refuses it) |
| `FromControlOrDefault(raw)` | the value, or the default |
| `Format(value)` and `TryParse(text, out value)` | invariant text (lists are comma separated), for hidden inputs and a future static post |
| `TryReadJsonControl(element, node, options, out control, out reason)` and `TryReadJson(element, node, options, out value, out reason)` | the JSON of an agent, read with the kind rules (`FormJsonCoercion.TryCoerce`: bounds, options by value or label, date formats), then with the exact digits of a number, then checked against the type. `TrySet` stores the control value `TryReadJsonControl` gives |
| `WriteJson(writer, value)` | the value as JSON, the way a control value is written; nothing calls it yet, because drafts and `WriteValue` write the control value the engine holds (which can be text that is not a value) |
| `NullableReference` | whether an empty text is `null` for a string member |

`ScalarCodec<TValue>` covers a single value, a list of strings and files with `FormValueMap`, which compares types and never inspects them. `MultiCodec<TElement>` is a multiple choice (the control holds the formatted option values). `ListCodec<TItem>` maps `List<Dictionary<string, object?>>` to items through the `ListRows<TItem>` columns; it makes new items, so it serves reads (`Get`, `TryGet`), never writes.

`IFieldBinding<TModel>` reads a member into a control value (`ReadControl`) and plans a write (`Plan`) into a `FormWritePlan`, which converts everything before it sets anything and rolls back on demand. Implementations: `TypedBinding` (typed delegates from `CreateDelegate`, composed through `HolderPath` and `NestedHolder` for `Group`), `ReflectedBinding` (a member known only at run time: `AllMembers`), the column bindings of a list, which are `IFieldBinding<TItem>`, and the two collection bindings, `RowsBinding` (a `List`) and `MultiBinding` (a `MultiSelect`), which share `CollectionBinding`.

`CollectionBinding` binds a collection member through the property of its holder. A chain written without `Group` is refused when the schema is built (`List` and `MultiSelect` give an `ArgumentException` that points to `Group(...)`), and so is a member with no setter whose type cannot be changed in place (an array, `IReadOnlyList<T>`, `IEnumerable<T>`). A write plans one step: when the collection the member holds is an `ICollection<T>` that is not read-only and not an array, it is made to hold the new items in place (positions kept, and put back by the same routine on a rollback); otherwise the setter gets a new `List<T>` or array. A collection that already holds the new items, in order, is not written at all. A getter-only member whose value turns out to be null or read-only throws at planning time, before any change.

`RowsBinding` writes a row into the item it was loaded from. The engine keeps rows as dictionaries, and a `RowOrigin` (a `ConditionalWeakTable` from the row dictionary to the item) remembers the item beside the row, so the item is never a value of the row: it is not in a draft, in the dirty check or in the JSON. `ListRows.ToRow` sets it, and `CloneValue` carries it over to the copies the engine makes (snapshot, `BuildOutput`). `RowsBinding.Plan` writes the columns into the origin only when the collection holds that item now, so a row loaded from another model, a draft restored row and an added row all get a new item, and a write into a new model never changes the rows of the model the form was loaded from. Each planned list is recorded in `FormWritePlan.Lists`, and `FormController.SubmitAsync` with a target sets the origin of the engine rows to those items after a successful save, before the engine retakes its snapshot.

## IFormRuntime

`FormController<TModel>` and `DynamicFormController` implement `IFormRuntime`. `Fields` returns the `FieldAccess` views (`Engine/FieldAccess.cs`): the stateful leaf fields of the form, whether in effect or not, without stateless blocks and hidden inputs. `FieldAccess` reads `IsHidden` from `ActiveFields`, `IsOffPage` from the current step, and writes JSON through `FieldNode.JsonReader` for a typed field (the codec's `TryReadJsonControl`: the rules of the agent `FormValueCoercion`, with their tests in `FormJsonCoercionTests`, plus the exact digits of a number and the check against the type) and through `FormJsonCoercion.TryCoerce` for a dynamic field, then `engine.SetValue(key, control, markTouched: true)`. A dynamic `Number` field keeps a `double`, as before; a typed one stores a `decimal` when the digits would not fit a double, so `long.MaxValue`, `9007199254740993` and a decimal of 28 digits arrive exactly. Drafts do not go through the codec: a draft holds control values, which can be text that is not a value yet (`abc` in a number field) and must survive a reload; `FormDraft.FromJson` and `FormValueConverter.NormalizeForField` read its numbers with the same `NumberFromInput` and `NumberToControl` rules, and the typed check happens when the field is validated. A sensitive field (`Password`, `OneTimeCode`) writes `null` from `WriteValue`.

## What the next changes build on this

Typed components (`KlyFormEngine<TModel>`, `KlyFormPage<TModel>`, `KlyFieldTemplate`, `FormService<TModel>`):

- Render from a `FormController<TModel>` through its `Engine` and the `FieldNode` tree of `schema.Definition`. The components of `Klyroo.Form` today run on the public `DynamicFormController` API (`KlyDynamicFieldView` is the old `KlySchemaField` with its types renamed), so the body of that component is the code to move onto `FormEngine` and `FieldNode`, keeping one renderer for both modes. `FormUiState` is keyed by `DynamicFormController` and has to be keyed by the engine.
- `DynamicControlContext` is the old `FormControlContext`: its public constructor takes the dynamic types. The typed `FormControlContext<TValue>` is built over `FormCodec<TValue>`: `Value` is `codec.FromControlOrDefault(engine.GetValue(key))`, `Set` and `Commit` call `engine.SetValue(key, codec.ToControl(value), markTouched)`, and `Text` and `SetText` are the string boundary (`NumberFromInput` for numbers).
- `KlyrooFormOptions.CustomField(key, RenderFragment<DynamicControlContext>)` is still the dynamic form of the global renderers; `CustomField<TValue>` is the typed one.
- `DynamicFormService` and `DynamicFormOutcome` are the old `FormService` and `FormSubmitOutcome`, kept in `Klyroo.Form.Dynamic`; `FormService<TModel>` and `FormSubmitOutcome<TModel>` are new, and `KlyFormProvider` has to host both.
- `KlyFormErrorSummary`, `KlyFormNavigationGuard`, `FormRun` and `FormLayout` take `DynamicFormController` today. They only use members the engine has, so they can take the engine (or `IFormRuntime` plus the engine) to serve both modes.
- Typed submit: `FormController<TModel>.SubmitAsync(save, target, ct)` already runs write, checks, hooks and save with rollback. A component's `OnSave` is `save`, `OnSubmit` is called with the model afterwards.
- Rendered scenarios that moved out of the F1 tests because they need the typed components, to be ported with them: a rendered engine bound to a model that submits a typed instance (`ModelBindingTests`, labeled in the F1 history as `A_rendered_engine_is_bound_to_a_model_and_submits_a_typed_instance`), the display half of `A_number_control_shows_and_posts_the_exact_value` (the input shows `9223372036854775807`, `1234567890123456789.1234567890` and `0.1`), the rendered half of the numeric exactness tests (an input outside the type shows an error and never becomes zero) and the rendered data annotations scenario (errors show under the fields, then the typed model is saved). Their non-rendered halves are in `BindingNumericExactnessTests`, `ModelBindingTests` and `DataAnnotationsBindingTests`.

Agent bridge: `RegisterForm(IFormRuntime form, bool confirmSubmit)` replaces `RegisterForm(FormController, ...)`. Everything it read from `FormController` and `FormFieldSchema` is on `IFormFieldAccess` (`Key`, `Kind`, `Label`, `Sensitive`, `IsHidden`, `IsOffPage`, `IsDisabled`, `Options`, `WriteValue`, `TrySet`, `Clear`, `Toggle`, `Reset`) and `IFormRuntime` (`FormKey`, `Title`, `IsDirty`, `IsStepped`, `CurrentStepKey`, `SubmitAsync`, `Reset`, `NextStepAsync`, `PreviousStepAsync`, `Changed`). `AgentArgs` and `FormValueCoercion` can be deleted once the bridge passes `JsonElement` to `TrySet`: the rules, the exact-number handling (`RawNumber`) and the option matching by normalized label are in `Engine/FormJsonArgs.cs` and `Engine/FormJsonCoercion.cs`.
