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

**Cause:** the schema is built inline in the markup, for example `Schema="new DynamicFormSchema { ... }"`. `KlyDynamicFormEngine` creates a new controller whenever it receives a different `DynamicFormSchema` instance, and a new controller starts from the defaults.

**Fix:** keep the schema in a field or a static member, or create a `DynamicFormController` yourself and pass it through `Form`.

### `DynamicFormService.CreateFormAsync` shows nothing and never returns

**Cause:** no `KlyFormProvider` is mounted in the current scope, or `AddKlyrooForm()` was not called in this project. The service stores the form and raises an event, and only the provider renders it. The returned task completes only after a submit or a close.

**Fix:** wrap the body of your layout in `<KlyFormProvider>`. With Auto render mode, register `AddKlyrooForm()` in the server and in the client project.

### A validation error does not show

**Cause:** errors are shown only for touched fields. A field becomes touched when `SetValue` is called with `markTouched: true`, when it loses focus in `KlyDynamicFormEngine` or `KlyDynamicFormPage`, when it has `Trigger = Input` and changes, or when `Validate()` or `SubmitAsync()` runs. Before that, `GetError` returns `null` even though a validation error may exist.

**Fix:** call `Validate()` to touch and check every field, call `MarkTouched(key)` for one field, or pass `markTouched: true` when you set a value from code. A field with `Trigger = FormValidationTrigger.Submit` is only validated by `Validate()` or a submit, and editing it clears its error until the next one. A cell of a repeatable list follows the same rules: `MarkArrayItemTouched` touches one cell, and its error is in `GetArrayItemError` or under its path (`lines[0].sku`) in `Errors`.

## Known limitations

- The controller is not thread safe.
- A bound model only reaches fields that are in effect and have a public setter, and the messages of the attribute rules (`[MinLength]`, `[EmailAddress]`, `[RegularExpression]` and others) are not localized: see the questions below.
- `Validators` are synchronous. Use `AsyncValidators` for a per-field check that needs I/O (they do not apply to the cells of a repeatable list), and `OnBeforeSubmit` or `OnBeforeNext` with `SetError` for checks on the whole submission.
- A `Condition` on a child of a repeatable list hides that column in every row, and the column is left out of the output rows: it receives the controller, not the row, so it cannot hide a single cell.
- Tabs have no arrow-key navigation: each tab button is reached with Tab.
- Async validators do not apply to the cells of a repeatable list, files are not part of drafts, and the file size and type shown by the browser come from the client, so check them again on the server.

## Questions

### Can I translate the built-in texts?

Yes, the components follow the locale of Klyroo.UI. Every text the components and the controller produce (buttons, `Saving...`, accessible names, the error summary, the section badges of `KlyDynamicFormPage`, the unsaved-changes question and the built-in validation messages) comes in the twelve languages of Klyroo.UI, as a `FormTexts` set per locale in `FormLocales`: a user who switches language with `KlyLocaleSelect` gets the form in that language too. English and French are reviewed by a native speaker; the other ten sets are drafts waiting for review. Change the texts of a locale at startup:

```csharp
builder.Services.AddKlyrooForm(f => f.Texts("fr", t => t with
{
    Submit = "Envoyer",
    Messages = t.Messages with { Required = "Merci de renseigner ce champ" },
}));
```

Text you write yourself is never translated: `Label`, `Hint`, `Help`, `Placeholder`, `RequiredMessage`, the labels of password requirements, option labels and the messages of your own validators. `SubmitLabel` on `KlyDynamicFormEngine` still overrides the submit text. Without Blazor, `new DynamicFormController(schema) { Messages = FormMessageLocales.Fr }` (or `DynamicFormSchema.Messages`) gives the controller the French validation messages; the components give it the messages of their locale unless the schema or your code chose one.

### Can I bind a form to a model class?

Yes. `FormSchema.For<TModel>(...)` in `Klyroo.Form.Core` declares a field per member with `s.Field(m => m.Name, f => ...)` and returns a handle for it, derives the key and the kind from the member, and a `FormController<TModel>` reads and writes the values as the member types (`Get`, `Set`), builds or fills a model (`CreateModel`, `ApplyTo`) and submits it with `SubmitAsync(save, target)`. See [Bind a C# model](guides.md#bind-a-c-model). The runtime mode (`Klyroo.Form.Dynamic`) keeps the dictionary API for forms built from data; the two modes share one engine but not their schemas.

### Does it integrate with DataAnnotations or FluentValidation?

DataAnnotations, yes, through the `Klyroo.Form.DataAnnotations` package: `FormSchema.For<TModel>(s => s.FromDataAnnotations())` reads `[Required]`, `[StringLength]`, `[MaxLength]`, `[MinLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[Compare]`, `[Display]`, `[Editable]`, `[ReadOnly]` and `[DataType]`, and runs your own attributes and `IValidatableObject` at submit. FluentValidation, no: a `Rule` or `OnBeforeSubmit` (or, in the runtime mode, a `DynamicValidatorRule`) can call any validator you already have, and `SetError` puts its errors on the fields.

### Why do some attribute messages look different from the other ones?

`[Required]`, `[StringLength]`, `[MaxLength]` on a string and `[Range]` on a number are checked by the engine's own rules, so their messages follow the locale of the form. The engine has no built-in rule for a minimum length, a pattern, an email address, a phone number, a URL or a comparison, so `[MinLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[CreditCard]` and `[Compare]` run as validator rules with the message of the attribute, in English unless you set `ErrorMessage` or a resource message. The whole-integer and number-range messages that a typed form adds for integer members, and the valid-identifier message for `Guid` members, follow the `Messages` of the controller like the engine ones.

### Which members can a model bind?

Properties of a class or record: strings, numbers, `bool`, enums (not `[Flags]`), `DateOnly`, `DateTime`, `DateTimeOffset`, `TimeOnly`, `TimeSpan`, `Guid`, lists of strings, `FormDateRange`, `FormFileValue` and the nullable variants, plus object members through `Group` or a member chain such as `m => m.Address.City`, collections of objects through `List` and collections of choices through `MultiSelect`. A member of another type needs `Kind(...)` and a value the engine can convert. Fields, members of struct type on the way to a nested one, flags enums and collections other than `List<T>`, arrays and the interfaces a list implements are not supported, and an object on the way that is `null` is created only for members declared with `Group`.

### What types do the values have?

In a typed form, the values you read and write are the types of the members: `Get` returns an `int` for an `int` member. Underneath, the engine stores what the controls produce, which is what the rest of this answer describes, and converts at the boundary. In the runtime mode you see the stored values directly. The built-in controls store strings for text-like kinds (including dates, times, colors and phone numbers), `double` for `Number` and `Slider` (a `decimal` instead when the digits typed or loaded do not fit a double, such as `long.MaxValue`), `int` for `Rating`, `bool` for `Checkbox` and `Switch`, the option value for single choices, and a `List<string>` for `CheckboxGroup`, `Tag`, `DateRange` (start and end) and a `Select` or `AutoComplete` with `Multiple`. Cells of repeatable lists use the same controls, so a `Number` cell holds a `double`. From code you can store anything, but dirty tracking compares values with `Equals`, apart from string lists and repeatable lists, which are compared by content. `GetValue<T>` converts with the invariant culture (see [Concepts](concepts.md)) and returns `default` when it cannot; `TryGetValue<T>` reports whether it could.

### How do labels and errors reach assistive technology?

In `KlyDynamicFormEngine`, `KlyDynamicFormPage` and `KlyDynamicFieldView` the label of a single control is tied to it with `for` and `id`, and controls made of several inputs (radio and checkbox lists, ratings, date ranges) are a `radiogroup` or `group` with an `id` and `aria-labelledby` pointing at the visible label. A control with an error gets `aria-invalid="true"`, and `aria-describedby` lists the ids of its description, help and error; per-field errors are not alerts: each form has one polite live region and an error summary that announce them once, and after a failed submit or Next focus moves to the first invalid field. The overlay of `KlyFormProvider` is a native modal dialog from Klyroo.UI (`KlyModal`, or `KlySlideover` for the drawer): focus stays inside it, Escape and a click on the backdrop close it (not while the form is saving), and focus returns to the element that opened it. Required controls carry `aria-required`, a `Switch` has `role="switch"`, password requirements are a list the password input points to, the stepper marks the current step with `aria-current`, and the icon-only buttons have an `aria-label`. This describes the markup; the library has no automated accessibility audit of the form components in this repository.
