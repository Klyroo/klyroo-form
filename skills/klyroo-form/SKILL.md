---
name: klyroo-form
description: Klyroo schema-driven Form Engine for .NET, Blazor, and MAUI (FormSchema, FormController, KlyFormEngine, KlyFormPage, KlyFormProvider).
---

# Klyroo.Form Skill

- Use `Klyroo.Form.Core` (`FormSchema`, `FormController`, `FormService`) for headless schema-driven state, validation, dirty tracking, and multi-step wizards.
- Use `Klyroo.Form` (`AddKlyrooForm()`, `KlyFormEngine`, `KlyFormPage`, `KlyFormProvider`) for rendering inline, multi-section, or overlay forms.
- Bind a form to a C# model with `FormModel.For<TModel>(b => b.Field(p => p.Name, f => f.Required()))` (`Klyroo.Form.Core`, keys follow the members, nested members give dotted keys), then `CreateController(model)`, `Write`, `Create` and the typed `SubmitAsync` or `OnSaveOutput`. Add the `Klyroo.Form.DataAnnotations` package and `b.FromDataAnnotations()` to read `[Required]`, `[Range]`, `[Display]` and the other attributes and to run `IValidatableObject` at submit.
- Save with `OnSave` (or `FormController.SubmitAsync(handler, ct)`) so `IsSubmitting` stays true during the save and the form stays dirty on failure; use `SetFormError` for failures that belong to no field.
- Use `DependsOn` for cross-field rules, `AsyncValidators` for per-field checks that need I/O, `KlyFormNavigationGuard` with `ExportState()` and `RestoreDraft()` for unsaved changes, `FormFieldKind.Custom` (`CustomField` or `FieldTemplates`) and `FormFieldKind.File` for other controls.
- Texts and validation messages follow the Klyroo.UI locale: change them with `AddKlyrooForm(f => f.Texts("fr", t => t with { ... }))`, or set `FormController.Messages` without Blazor.
