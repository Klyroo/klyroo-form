---
name: klyroo-form
description: Klyroo schema-driven Form Engine for .NET, Blazor, and MAUI (typed FormSchema.For<TModel> and FormController<TModel>, the runtime Klyroo.Form.Dynamic mode, KlyDynamicFormEngine, KlyFormProvider).
---

# Klyroo.Form Skill

- Declare a form from a C# model with `FormSchema.For<TModel>(s => { ... })` in `Klyroo.Form.Core`. The builder returns a handle per field (`var company = s.Field(m => m.IsCompany)`), so a later condition points at it: `s.Field(m => m.CompanyName, f => f.Required(c => c.Get(company)).VisibleWhen(c => c.Get(company)))`. Keys follow the members and nested members give dotted keys.
- Settings are methods on the field builder: `Required`, `Label`, `Rule(value => ..., message)`, `RuleAsync(...)`, `DependsOn(handles)`, `Default`, `OnChange`, `VisibleWhen`, `DisabledWhen`. Kind-specific ones exist only for the types they fit: `AsTextarea`, `AsPassword`, `MaxLength` (string), `Range`, `AsSlider`, `AsRating` (numbers), `AsSwitch` (bool), `Options`, `AsRadio` (any), `Accept` (files). Structure: `Group`, `List` (repeatable rows), `MultiSelect`, `Step`, `Section`, `Card`, `Tabs`, model checks with `Validate`, and `Field<TValue>("key")` for a field that is not in the model.
- Run it with `new FormController<TModel>(schema, model)`: `Get(field)`, `TryGet`, `Set(field, value)`, `GetError(field)`, `CreateModel()`, `ApplyTo(target)`, `Rows(list)`, `ExportDraft()`, and `SubmitAsync(save, target)`, which writes the target, runs the checks and hooks, saves, and puts the target back when anything fails.
- Add the `Klyroo.Form.DataAnnotations` package and `s.FromDataAnnotations()` (or `UseDataAnnotations()`) to read `[Required]`, `[Range]`, `[Display]` and the other attributes and to run `IValidatableObject` at submit.
- For a form described as data (built at runtime, values keyed by name) use the runtime mode in `Klyroo.Form.Dynamic`: `DynamicFormSchema`, `DynamicFieldSchema`, `DynamicFormController`, `DynamicFormService`, and the components `KlyDynamicFormEngine`, `KlyDynamicFormPage` and `KlyDynamicFieldView`. Register with `AddKlyrooForm()` and mount `KlyFormProvider` for overlays. The typed Blazor components arrive in the next change.
- Save with `OnSave` (or `SubmitAsync(handler, ct)` in the runtime mode) so `IsSubmitting` stays true during the save and the form stays dirty on failure; use `SetFormError` for failures that belong to no field.
- Use `DependsOn` for cross-field rules, async rules for per-field checks that need I/O, `KlyFormNavigationGuard` with `ExportState()` or `ExportDraft()` and `RestoreDraft()` for unsaved changes, `FormFieldKind.Custom` and `FormFieldKind.File` for other controls.
- Both controllers implement `IFormRuntime`: `Fields` lists `IFormFieldAccess` with `TrySet(JsonElement)`, `WriteValue(Utf8JsonWriter)`, `Clear`, `Toggle` and `Reset`, for agents.
- Texts and validation messages follow the Klyroo.UI locale: change them with `AddKlyrooForm(f => f.Texts("fr", t => t with { ... }))`, or set `Messages` on the controller without Blazor.
