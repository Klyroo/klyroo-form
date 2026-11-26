# FAQ

Troubleshooting, known limitations and common questions. Each entry is something you can reproduce from the code. If you hit a problem that is not here, open an issue with a minimal schema. The entries use the typed API; where the runtime mode behaves differently, the entry says so and [Forms defined at runtime](runtime-forms.md) has the details.

## Troubleshooting

### The form is unstyled or has no colors

**Cause:** one of the three stylesheets is missing. The colors, borders and radii come from Klyroo.UI tokens, and the layout classes come from `klyroo-form.css`, which defines no tokens.

**Fix:** load all three in `App.razor` (or `wwwroot/index.html` in MAUI): `klyroo.preflight.css` and `klyroo.css` from `_content/Klyroo.UI/`, and `klyroo-form.css` from `_content/Klyroo.Form/`. See [Getting started](getting-started.md#load-the-stylesheets).

### Typing and buttons do nothing

**Cause:** the page is rendered with Static SSR. The components rely on Blazor event handlers, which only run in an interactive render mode.

**Fix:** add `@rendermode InteractiveServer` (or WebAssembly, or Auto) to the page, or enable interactivity globally. See [Platforms](platforms.md#render-modes).

### `KlyFormEngine<TModel>` restarts from the defaults on every render, or the model type is `Order?`

**Cause:** the schema is built inline in the markup (`Schema="FormSchema.For<Order>(...)"`), so each render gives the component a new `FormSchema<TModel>` instance and it creates a new controller, which starts from the defaults and drops what the user typed. The second symptom has another cause: a `Model` field declared `Order?` makes Razor infer `Order?` for `TModel` and warn that the schema is `FormSchema<Order>`.

**Fix:** keep the schema in a field or a static member, and declare the model non-null (`Model="_order!"` if it is loaded later). If you create a `FormController<TModel>` yourself and pass it through `Form`, the component never replaces it. The runtime-mode component behaves the same way with a `DynamicFormSchema` instance.

### Razor reports a conversion error on `OnSubmit` of `KlyFormEngine<TModel>`

**Cause:** `OnSubmit` is an `EventCallback<TModel>`, and Razor cannot infer `TModel` from a method group or a lambda given to it. It picks the non-generic `EventCallback` and reports "cannot convert from 'method group' to 'EventCallback'" (or from `EventCallback` to `EventCallback<Order>` for a lambda).

**Fix:** write the type, `<KlyFormEngine TModel="Order" Schema="_schema" Model="_order" OnSubmit="Saved" />`, or drop `OnSubmit` and do the work in `OnSave`.

### A `KlyFieldTemplate` draws nothing

**Cause:** it must sit inside `KlyFormEngine<TModel>` or `KlyFormPage<TModel>` (or have its `Form` set), and its `Field` must be the handle of a field of that form. It is not used for the columns of a repeatable list, and a renderer registered with `CustomField<TValue>` is not used by a field of another value type.

**Fix:** put it in the body of the form component, and check the type of the handle against the field.

### `FormService.CreateFormAsync` shows nothing and never returns

**Cause:** no `KlyFormProvider` is mounted in the current scope, or `AddKlyrooForm()` was not called in this project. The service stores the form and raises an event, and only the provider renders it. The returned task completes only after a submit or a close. The same holds for `DynamicFormService.CreateFormAsync`.

**Fix:** wrap the body of your layout in `<KlyFormProvider>`. With Auto render mode, register `AddKlyrooForm()` in the server and in the client project.

### A validation error does not show

**Cause:** errors are shown only for touched fields. A field becomes touched when `Set` is called with `markTouched: true`, when it loses focus in the components, when it has `Trigger(FormValidationTrigger.Input)` and changes, or when `Validate()` or `SubmitAsync()` runs. Before that, `GetError` returns `null` even though a validation error may exist.

**Fix:** call `Validate()` to touch and check every field, call `Touch(field)` for one field, or pass `markTouched: true` when you set a value from code. A field with `Trigger(FormValidationTrigger.Submit)` is only validated by `Validate()` or a submit, and editing it clears its error until the next one. A cell of a repeatable list follows the same rules: `row.GetError(column)` reads its error, and so does its path (`lines[0].sku`) in `Errors`.

### `Get` returns `0` or an empty string while the field shows an error

**Cause:** the field holds text that is not a value of the member type yet, such as `abc` in an `int` field. `Get` returns the default of the type for it, and validation reports the error.

**Fix:** call `TryGet(field, out var value)`, which returns `false` while the text does not parse, or read `GetError(field)`.

## Known limitations

- The controller is not thread safe.
- A bound model only reaches fields that are in effect and have a public setter, and the messages of the attribute rules (`[MinLength]`, `[EmailAddress]`, `[RegularExpression]` and others) are not localized: see the questions below.
- `Rule` is synchronous. Use `RuleAsync` for a per-field check that needs I/O (it does not apply to the cells of a repeatable list), and `OnBeforeSubmit` or `OnBeforeNext` with `SetError` for checks on the whole submission.
- A `VisibleWhen` on a column of a repeatable list hides that column in every row, and the column is left out of the output rows: it receives the controller, not the row, so it cannot hide a single cell.
- Tabs have no arrow-key navigation: each tab button is reached with Tab.
- Async rules do not apply to the cells of a repeatable list, files are not part of drafts, and the file size and type shown by the browser come from the client, so check them again on the server.

## Questions

### Should I use the typed or the runtime mode?

Use the typed mode (`FormSchema.For<TModel>`) whenever a C# class describes the data: the compiler checks member names and value types, the form writes into your model, and everything in the [guides](guides.md) applies. Use the runtime mode (`Klyroo.Form.Dynamic`) only when the shape of the form is not known at compile time, for example a form stored as JSON or in a database, or built by a user. It has the same engine and components, with a dictionary of values keyed by name. See [Forms defined at runtime](runtime-forms.md).

### Can I translate the built-in texts?

Yes, the components follow the locale of Klyroo.UI. Every text the components and the controller produce (buttons, `Saving...`, accessible names, the error summary, the section badges of `KlyFormPage<TModel>`, the unsaved-changes question and the built-in validation messages) comes in the twelve languages of Klyroo.UI, as a `FormTexts` set per locale in `FormLocales`: a user who switches language with `KlyLocaleSelect` gets the form in that language too. English and French are reviewed by a native speaker; the other ten sets are drafts waiting for review. Change the texts of a locale at startup:

```csharp
builder.Services.AddKlyrooForm(f => f.Texts("fr", t => t with
{
    Submit = "Envoyer",
    Messages = t.Messages with { Required = "Merci de renseigner ce champ" },
}));
```

Text you write yourself is never translated: `Label`, `Hint`, `Help`, `Placeholder`, the message of `Required(message: ...)`, the labels of password requirements, option labels and the messages of your own rules. `SubmitLabel` on `KlyFormEngine<TModel>` still overrides the submit text. Without Blazor, `new FormController<TModel>(schema) { Messages = FormMessageLocales.Fr }` (or `s.Messages` in the schema) gives the controller the French validation messages; the components give it the messages of their locale unless the schema or your code chose one.

### Does it integrate with DataAnnotations or FluentValidation?

DataAnnotations, yes, through the `Klyroo.Form.DataAnnotations` package: `FormSchema.For<TModel>(s => s.FromDataAnnotations())` reads `[Required]`, `[StringLength]`, `[MaxLength]`, `[MinLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[Compare]`, `[Display]`, `[Editable]`, `[ReadOnly]` and `[DataType]`, and runs your own attributes and `IValidatableObject` at submit. FluentValidation, no: a `Rule` or `OnBeforeSubmit` can call any validator you already have, and `SetError` puts its errors on the fields.

### Why do some attribute messages look different from the other ones?

`[Required]`, `[StringLength]`, `[MaxLength]` on a string and `[Range]` on a number are checked by the engine's own rules, so their messages follow the locale of the form. The engine has no built-in rule for a minimum length, a pattern, an email address, a phone number, a URL or a comparison, so `[MinLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[CreditCard]` and `[Compare]` run as validator rules with the message of the attribute, in English unless you set `ErrorMessage` or a resource message. The whole-integer and number-range messages that a typed form adds for integer members, and the valid-identifier message for `Guid` members, follow the `Messages` of the controller like the engine ones.

### Which members can a model bind?

Properties of a class or record: strings, numbers, `bool`, enums (not `[Flags]`), `DateOnly`, `DateTime`, `DateTimeOffset`, `TimeOnly`, `TimeSpan`, `Guid`, lists of strings, `FormDateRange`, `FormFileValue` and the nullable variants, plus object members through `Group` or a member chain such as `m => m.Address.City`, collections of objects through `List` and collections of choices through `MultiSelect`. A member of another type needs `Kind(...)` and a value the engine can convert. Fields, members of struct type on the way to a nested one, flags enums and collections other than `List<T>`, arrays and the interfaces a list implements are not supported, and an object on the way that is `null` is created only for members declared with `Group`.

### What types do the values have?

In a typed form, the values you read and write are the types of the members: `Get` returns an `int` for an `int` member, and `TryGet` tells a value from text that is not one yet. Underneath, the engine stores what the controls produce and converts at the boundary with the invariant culture, so you never see the stored form unless you use the runtime mode, where values are `object?` keyed by name: see [Values and keys](runtime-forms.md#values-and-keys) for what each control stores. Dirty tracking compares values with `Equals`, apart from string lists and repeatable lists, which are compared by content.

### How do labels and errors reach assistive technology?

In `KlyFormEngine<TModel>`, `KlyFormPage<TModel>` and `KlyFormFieldView` (and the dynamic components, which share the renderer) the label of a single control is tied to it with `for` and `id`, and controls made of several inputs (radio and checkbox lists, ratings, date ranges) are a `radiogroup` or `group` with an `id` and `aria-labelledby` pointing at the visible label. A control with an error gets `aria-invalid="true"`, and `aria-describedby` lists the ids of its description, help and error; per-field errors are not alerts: each form has one polite live region and an error summary that announce them once, and after a failed submit or Next focus moves to the first invalid field. The overlay of `KlyFormProvider` is a native modal dialog from Klyroo.UI (`KlyModal`, or `KlySlideover` for the drawer): focus stays inside it, Escape and a click on the backdrop close it (not while the form is saving), and focus returns to the element that opened it. Required controls carry `aria-required`, a `Switch` has `role="switch"`, password requirements are a list the password input points to, the stepper marks the current step with `aria-current`, and the icon-only buttons have an `aria-label`. This describes the markup; the library has no automated accessibility audit of the form components in this repository.
