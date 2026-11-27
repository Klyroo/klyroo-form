# Klyroo.Form

Schema-driven forms, validation and wizards for .NET developers, with a headless engine and Blazor components.

[![CI](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Klyroo.Form.svg)](https://www.nuget.org/packages/Klyroo.Form)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/klyroo/klyroo-form/blob/main/LICENSE)

You declare a form from a C# model with `FormSchema.For<TModel>`: fields, conditions, rules, steps and sections, written with refactor-safe expressions and checked by the compiler. A `FormController<TModel>` holds its state: values typed as your members, touched fields, errors, dirty tracking and step navigation. The controller has no UI dependency, so the same schema can be unit tested, run in a view model, or rendered by the Blazor components. The Blazor layer builds on Klyroo.UI and runs in Interactive Server, WebAssembly and Auto render modes, and in .NET MAUI Blazor Hybrid.

Every page of the documentation uses the typed API except one. A form that only exists as data at runtime, such as one stored as JSON or in a database, uses the runtime mode in `Klyroo.Form.Dynamic`, which is documented apart in [Forms defined at runtime](https://github.com/klyroo/klyroo-form/blob/main/docs/runtime-forms.md).

## Features

- **Typed schema**: `FormSchema.For<Order>(s => { s.Field(o => o.Seats, f => f.Range(1, 500)); ... })` declares fields from the members of a C# class and returns a handle per field, so a condition points at an earlier field through a captured local and `controller.Get(seats)` is an `int`. Kind-specific settings (`AsTextarea`, `Range`, `Options`, `AsSwitch`) are extension methods constrained on the type of the value, so a wrong kind does not compile. `Group`, `List`, `MultiSelect`, `Step` and `Section` declare nested objects, repeatable rows, multiple choice, wizards and pages.
- **Headless controller**: `FormController<TModel>` manages values, touched state, errors, dirty tracking, steps and submission with no UI dependency, and `Klyroo.Form.Core` is NativeAOT compatible.
- **Typed components**: `KlyFormEngine<TModel>` and `KlyFormPage<TModel>` render a `FormSchema<TModel>` (or a `FormController<TModel>`) with a `Model`, and infer `TModel` from the markup. `OnSave` receives the model, `OnSubmit` is raised with it, and a submit writes the values into the model you gave, all or nothing. `KlyFormFieldView` draws one field for a layout of your own, and `KlyFieldTemplate` draws the control of one field with a typed `FormControlContext<TValue>`.
- **Validation**: required and required-when checks, number bounds, password requirements, typed `Rule` and `RuleAsync` rules with a pending state (debounced, stale runs cancelled, awaited by submit), four validation modes and three triggers (blur, input, submit).
- **Model binding that cannot lose data**: values move between the form and the model with culture-independent conversions (a `long`, a `decimal` and a `double` keep every digit), a write into a model is all or nothing and rolls back when the save fails, and a value the user did not change keeps its ticks, kind and offset. `Klyroo.Form.DataAnnotations` reads `[Required]`, `[Range]`, `[Display]` and the other attributes into a typed schema, and runs `IValidatableObject` at submit.
- **Server errors**: `SetError` attaches a message to a field, optionally blocking submission until the user edits it.
- **Conditional fields**: `VisibleWhen`, `Required(condition)`, `DisabledWhen` and `skipWhen` are plain delegates evaluated against the controller, and hidden fields, the children of hidden containers and the fields of skipped steps are left out of validation and of the model.
- **Dirty tracking**: the controller compares values with an initial snapshot, resets a field or a section, and takes a new snapshot after a successful submit.
- **Wizards and pages**: multi-step forms with skippable steps and async before-next hooks, and sectioned pages that show a completion status per section.
- **Extensible**: `KlyFieldTemplate` swaps the control of a typed field for your markup, `AsCustom(key)` plus `CustomField<TValue>(key, renderer)` registers one for the whole app, and `FormFileValue` fields are checked for `Accept`, `MaxFileSize` and multiple files in the core.
- **Localization**: the built-in texts and validation messages follow the Klyroo.UI locale in twelve languages (English and French reviewed, the rest drafts), `AddKlyrooForm(f => f.Texts("fr", t => t with { ... }))` changes them, and `FormMessages` works without Blazor.
- **Unsaved changes**: `KlyFormNavigationGuard` blocks navigation while the form is dirty, and `ExportDraft()` and `RestoreDraft()` keep a draft across a reload with the original baseline, so the restored draft is still dirty.
- **Overlays**: `FormService.CreateFormAsync<TModel>` opens a form in a modal dialog, a side drawer or a fullscreen view hosted by `KlyFormProvider` (on `KlyModal` and `KlySlideover`: focus trap, Escape, focus restored) and returns the result as a task (`FormSubmitOutcome<TModel>` with the submitted model).
- **Agent ready**: `FormController<TModel>` (like the runtime `DynamicFormController`) implements `IFormRuntime`, which lists the fields as `IFormFieldAccess` with JSON read and write (numbers keep their exact digits, a choice matches by value or label), so an agent bridge drives a form without knowing its model type.
- **Runtime mode**: when the fields are only known at runtime, `DynamicFormSchema` and `DynamicFieldSchema` (namespace `Klyroo.Form.Dynamic`) describe the form as data, with values keyed by name, on the same engine. `FormFieldKind` has 33 values, and `KlyDynamicFormEngine` and `KlyDynamicFormPage` render each one. See [Forms defined at runtime](https://github.com/klyroo/klyroo-form/blob/main/docs/runtime-forms.md).

## Packages

| Package | Description |
| :--- | :--- |
| [`Klyroo.Form.Core`](https://www.nuget.org/packages/Klyroo.Form.Core) | Headless engine: the typed `FormSchema` and `FormController<TModel>`, and for forms defined at runtime `DynamicFormSchema`, `DynamicFormController` and `DynamicFormService`. No UI dependency, trimmable, NativeAOT compatible. |
| [`Klyroo.Form`](https://www.nuget.org/packages/Klyroo.Form) | Blazor components `KlyFormEngine<TModel>`, `KlyFormPage<TModel>`, `KlyFormFieldView<TModel>`, `KlyFieldTemplate` and `KlyFormProvider`, `FormService`, and the `AddKlyrooForm()` registration, plus `KlyDynamicFormEngine`, `KlyDynamicFormPage` and `KlyDynamicFieldView` for the runtime mode. Builds on Klyroo.UI and references `Klyroo.Form.Core`. |
| [`Klyroo.Form.DataAnnotations`](https://www.nuget.org/packages/Klyroo.Form.DataAnnotations) | Builds a typed schema from a C# model and its DataAnnotations attributes, and runs `IValidatableObject` and your own validation attributes at submit. References `Klyroo.Form.Core` only, so it also works without Blazor. |

## Prerequisites

To use the library in an app:

- **The .NET 10 SDK**, and an app that targets `net10.0`. The packages are prereleases, so every `dotnet add package` command needs `--prerelease`.
- **`Klyroo.Form.Core`** alone (the form controller) runs in any .NET 10 app, with no other dependency.
- **A Blazor app** for `Klyroo.Form`: a Blazor Web App or a .NET MAUI Blazor Hybrid app. Typing, validation feedback, steps and submit need an interactive render mode (Interactive Server, WebAssembly or Auto). In a WebAssembly or Auto app, register the services in the server project and in the `.Client` project.
- **Klyroo.UI set up in the app.** `Klyroo.Form` brings the Klyroo.UI package, and `AddKlyrooForm()` registers its services, but the layout still needs the Klyroo.UI stylesheets in `<head>` and `<KlyApp>` around the body, as described in the [Klyroo.UI installation](https://github.com/klyroo/klyroo-ui#installation).
- **`<KlyFormProvider>` in the layout** if you open forms as overlays with `FormService` (or `DynamicFormService` in the runtime mode).
- **A browser with the native popover API**, which Klyroo.UI uses: Chrome and Edge 114, Safari 17 or Firefox 125, or later. In a .NET MAUI app, that means iOS or Mac Catalyst 17, or an Android System WebView 114 or later.
- **Network access on the first build** only if your app self-hosts web fonts (`KlyrooFont`), uses icons outside the built-in set, or compiles Tailwind (`KlyrooTailwind`): the build downloads them once and caches them under `obj/klyroo`. Node.js is not needed.

To build this repository you need the .NET 10 SDK (a newer SDK works too, see `global.json`) and a clone of `klyroo-ui` in the same parent folder: some projects reference it by relative path (`../klyroo-ui/...`). See [Contributing](https://github.com/klyroo/klyroo-form/blob/main/CONTRIBUTING.md).

## Installation

```bash
dotnet add package Klyroo.Form --prerelease
```

For the engine alone, without Blazor, install `Klyroo.Form.Core` instead.

Register the services in `Program.cs` (or `MauiProgram.cs`). It also registers Klyroo.UI:

```csharp
using Klyroo.Form;

builder.Services.AddKlyrooForm();
```

Add the namespace to `_Imports.razor`:

```razor
@using Klyroo.Form
```

The runtime mode lives in its own namespace, `Klyroo.Form.Dynamic`: add it only where you use that mode.

Load the stylesheets in `App.razor`. The theme tokens come from Klyroo.UI and the form classes from this package:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Form/klyroo-form.css"]" />
```

## Quick start

Headless: declare a form from a model, drive it with a controller.

```csharp
using Klyroo.Form;
```

```csharp
public sealed class Contact
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public Topic Topic { get; set; }

    public string? Message { get; set; }
}

public enum Topic { Sales, Support }
```

```csharp
var schema = FormSchema.For<Contact>(s =>
{
    s.FormKey = "contact";
    s.Field(c => c.Name, f => f.Required());
    s.Field(c => c.Email, f => f.Required().Rule(e => e.Contains('@'), "Enter a valid email address"));
});
var name = schema.Field(c => c.Name);
var email = schema.Field(c => c.Email);
var form = new FormController<Contact>(schema);

form.Set(name, "Alice", markTouched: true);
form.Set(email, "alice", markTouched: true);
Console.WriteLine(form.GetError(email));      // Enter a valid email address
Console.WriteLine(await form.SubmitAsync());  // False

form.Set(email, "alice@example.com");
Console.WriteLine(await form.SubmitAsync((contact, ct) =>
{
    Console.WriteLine($"Send a reply to {contact.Email}");
    return Task.FromResult(true);
}));                                          // True
Console.WriteLine(form.IsDirty);              // False
```

Blazor: render the same schema with `KlyFormEngine<TModel>`. It infers `TModel` from `Schema` and `Model`, and the form writes the values into `_contact` when it submits.

```razor
@page "/contact"
@rendermode InteractiveServer
@inject IContactApi Api

<KlyFormEngine TModel="Contact" Schema="_schema" Model="_contact" SubmitLabel="Send" OnSave="SaveAsync" OnSubmit="Sent" />

@if (_sentTo is not null)
{
    <p>Thanks, we will write to @_sentTo.</p>
}

@code {
    private readonly Contact _contact = new();

    private string? _sentTo;

    // Keep the schema in a field: a new instance makes the component start a new controller.
    private static readonly FormSchema<Contact> _schema = FormSchema.For<Contact>(s =>
    {
        s.Title = "Contact us";
        s.Field(c => c.Name, f => f.Required());
        s.Field(c => c.Email, f => f.Required().Rule(e => e.Contains('@'), "Enter a valid email address"));
        s.Field(c => c.Topic);
        s.Field(c => c.Message, f => f.Required().AsTextarea().MaxLength(500));
    });

    private async Task<bool> SaveAsync(Contact contact, CancellationToken ct) => await Api.SendAsync(contact, ct);

    private void Sent(Contact contact) => _sentTo = contact.Email;
}
```

`Api` stands for your own service. Keep the schema in a field or a static member. `KlyFormEngine<TModel>` creates a new controller whenever it receives a different `FormSchema<TModel>` instance, which discards what the user typed. `TModel="Contact"` is written out because Razor cannot infer it when `OnSubmit` is a method group. A form that is not known until runtime is declared with the runtime mode instead: see [Forms defined at runtime](https://github.com/klyroo/klyroo-form/blob/main/docs/runtime-forms.md).

## Documentation

| | |
| :--- | :--- |
| [Getting started](https://github.com/klyroo/klyroo-form/blob/main/docs/getting-started.md) | Step-by-step first integration |
| [Guides](https://github.com/klyroo/klyroo-form/blob/main/docs/guides.md) | Task-oriented recipes with the typed API |
| [Forms defined at runtime](https://github.com/klyroo/klyroo-form/blob/main/docs/runtime-forms.md) | The runtime mode, for forms stored as data |
| [Concepts](https://github.com/klyroo/klyroo-form/blob/main/docs/concepts.md) | How the library works |
| [Platforms](https://github.com/klyroo/klyroo-form/blob/main/docs/platforms.md) | Render modes, .NET MAUI, NativeAOT |
| [API reference](https://github.com/klyroo/klyroo-form/blob/main/docs/api-reference.md) | Every public type and member |
| [FAQ](https://github.com/klyroo/klyroo-form/blob/main/docs/faq.md) | Troubleshooting and limitations |

## Compatibility

- .NET 10 SDK.
- `Klyroo.Form.Core` targets `net10.0`, has no dependencies, is marked trimmable and AOT compatible, and a console app that references it publishes and runs with `PublishAot`.
- `Klyroo.Form` works in Interactive Server, Interactive WebAssembly and Interactive Auto render modes. In Static SSR the markup renders but the controls are not interactive.
- `Klyroo.Form` declares `browser`, `ios`, `android`, `maccatalyst` and `windows` as supported platforms for .NET MAUI Blazor Hybrid. The test suite runs in bUnit, not on devices.
- `Klyroo.Form` is marked trimmable and does not declare NativeAOT compatibility.

## Klyroo ecosystem

Klyroo is a family of independent .NET libraries that compose. Each one has its own repository, versions and releases.

| Library | Description |
| :--- | :--- |
| [Klyroo.UI](https://github.com/klyroo/klyroo-ui) | Accessible Blazor components for the web and .NET MAUI Blazor Hybrid, styled with Tailwind CSS v4 |
| [Klyroo.Query](https://github.com/klyroo/klyroo-query) | Typed query model, URL state sync, and IQueryable / EF Core execution |
| [Klyroo.Table](https://github.com/klyroo/klyroo-table) | Data lists with table and card-grid layouts, filters, selection and actions |
| **[Klyroo.Form](https://github.com/klyroo/klyroo-form)** | Schema-driven forms, validation and wizards |
| [Klyroo.Dashboard](https://github.com/klyroo/klyroo-dashboard) | Dashboard controller, filters and Blazor blocks: stats, gauges, charts, tables and feeds |
| [Klyroo.Display](https://github.com/klyroo/klyroo-display) | Code highlighting, secret and file list components, media player controls and file preview |
| [Klyroo.Spreadsheet](https://github.com/klyroo/klyroo-spreadsheet) | Import wizard for CSV, TSV and pasted data, with column and value mapping |
| [Klyroo.Agent](https://github.com/klyroo/klyroo-agent) | Bridge that lets AI agents read and act on Klyroo forms, dashboards and imports |
| [Klyroo.Stack](https://github.com/klyroo/klyroo-stack) | Meta-package, MCP documentation server and `dotnet new` templates |

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](https://github.com/klyroo/klyroo-form/blob/main/CONTRIBUTING.md) before opening a pull request, and follow the
[code of conduct](https://github.com/klyroo/klyroo-form/blob/main/CODE_OF_CONDUCT.md). Report security issues privately as described in [SECURITY.md](https://github.com/klyroo/klyroo-form/blob/main/SECURITY.md).

## License

[MIT](https://github.com/klyroo/klyroo-form/blob/main/LICENSE)
