# Klyroo.Form

Schema-driven forms, validation and wizards for .NET developers, with a headless engine and Blazor components.

[![CI](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Klyroo.Form.svg)](https://www.nuget.org/packages/Klyroo.Form)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/klyroo/klyroo-form/blob/main/LICENSE)

You declare a form from a C# model (`FormSchema.For<TModel>`: fields, conditions, rules, steps) or describe it as data (the runtime mode in `Klyroo.Form.Dynamic`), and a controller holds its state: values, touched fields, errors, dirty tracking and step navigation. The controller has no UI dependency, so the same schema can be unit tested, run in a view model, or rendered by the Blazor components. The Blazor layer builds on Klyroo.UI and runs in Interactive Server, WebAssembly and Auto render modes, and in .NET MAUI Blazor Hybrid.

## Features

- **Headless controller**: `FormController<TModel>` (typed) and `DynamicFormController` (runtime) manage values, touched state, errors, dirty tracking, steps and submission with no UI dependency, on one internal engine, and `Klyroo.Form.Core` is NativeAOT compatible.
- **Typed schema**: `FormSchema.For<Order>(s => { s.Field(o => o.Seats, f => f.Range(1, 500)); ... })` declares fields from the members of a C# class with refactor-safe expressions and returns a handle per field, so a condition points at an earlier field through a captured local and `controller.Get(seats)` is an `int`. Kind-specific settings (`AsTextarea`, `Range`, `Options`, `AsSwitch`) are extension methods constrained on the type of the value, so a wrong kind does not compile. `Group`, `List`, `MultiSelect`, `Step` and `Section` declare nested objects, repeatable rows, multiple choice, wizards and pages.
- **Typed components**: `KlyFormEngine<TModel>` and `KlyFormPage<TModel>` render a `FormSchema<TModel>` (or a `FormController<TModel>`) with a `Model`, and infer `TModel` from the markup. `OnSave` receives the model, `OnSubmit` is raised with it, and a submit writes the values into the model you gave, all or nothing. `KlyFormFieldView` draws one field for a layout of your own, and `KlyFieldTemplate` draws the control of one field with a typed `FormControlContext<TValue>`.
- **Runtime mode**: `DynamicFormSchema` and `DynamicFieldSchema` (namespace `Klyroo.Form.Dynamic`) describe the form as data, with values keyed by name. `FormFieldKind` has 33 values, and `KlyDynamicFormEngine` and `KlyDynamicFormPage` render each one with a dedicated control (native inputs, `InputFile` for files, or your own renderer for `Custom`), from date ranges, ratings and tag inputs to collapsible containers, tabs and repeatable lists whose cells are validated.
- **Validation**: required and required-when checks, number bounds, password requirements, custom validator rules, asynchronous rules with a pending state (debounced, stale runs cancelled, awaited by submit), four validation modes and three triggers (blur, input, submit).
- **Model binding that cannot lose data**: values move between the form and the model with culture-independent conversions (a `long`, a `decimal` and a `double` keep every digit), a write into a model is all or nothing and rolls back when the save fails, and a value the user did not change keeps its ticks, kind and offset. `Klyroo.Form.DataAnnotations` reads `[Required]`, `[Range]`, `[Display]` and the other attributes into a typed schema, and runs `IValidatableObject` at submit.
- **Agent ready**: both controllers implement `IFormRuntime`, which lists the fields as `IFormFieldAccess` with JSON read and write (numbers keep their exact digits, a choice matches by value or label), so an agent bridge drives a form without knowing its model type.
- **Server errors**: `SetError` attaches a message to a field, optionally blocking submission until the user edits it.
- **Conditional fields**: `Condition`, `RequiredWhen`, `DisabledWhen` and `SkipWhen` are plain delegates evaluated against the controller, and hidden fields, the children of hidden containers and the fields of skipped steps are left out of validation and output.
- **Dirty tracking**: the controller compares values with an initial snapshot, resets a field or a section, and takes a new snapshot after a successful submit.
- **Wizards and pages**: multi-step forms with skippable steps and async before-next hooks, and sectioned pages that show a completion status per section.
- **Extensible**: `KlyFieldTemplate` swaps the control of a typed field for your markup, `AsCustom(key)` plus `CustomField<TValue>(key, renderer)` registers one for the whole app, and `FormFieldKind.Custom` in a dynamic form renders a control you register (`AddKlyrooForm(f => f.CustomField(key, renderer))` or the `FieldTemplates` parameter) from a public `DynamicControlContext`, and `FormFieldKind.File` holds UI-free `FormFileValue`s checked for `Accept`, `MaxFileSize` and `Multiple` in the core.
- **Localization**: the built-in texts and validation messages follow the Klyroo.UI locale in twelve languages (English and French reviewed, the rest drafts), `AddKlyrooForm(f => f.Texts("fr", t => t with { ... }))` changes them, and `FormMessages` works without Blazor.
- **Unsaved changes**: `KlyFormNavigationGuard` blocks navigation while the form is dirty, and `ExportState()` and `RestoreDraft()` keep a draft across a reload with the original baseline, so the restored draft is still dirty.
- **Overlays**: `FormService.CreateFormAsync<TModel>` (and `DynamicFormService.CreateFormAsync` for a runtime form) opens a form in a modal dialog, a side drawer or a fullscreen view hosted by `KlyFormProvider` (on `KlyModal` and `KlySlideover`: focus trap, Escape, focus restored) and returns the result as a task (`FormSubmitOutcome<TModel>` with the submitted model).

## Packages

| Package | Description |
| :--- | :--- |
| [`Klyroo.Form.Core`](https://www.nuget.org/packages/Klyroo.Form.Core) | Headless engine: the typed `FormSchema` and `FormController<TModel>`, and the runtime `DynamicFormSchema`, `DynamicFormController` and `DynamicFormService`. No UI dependency, trimmable, NativeAOT compatible. |
| [`Klyroo.Form`](https://www.nuget.org/packages/Klyroo.Form) | Blazor components `KlyFormEngine<TModel>`, `KlyFormPage<TModel>`, `KlyFormFieldView<TModel>`, `KlyFieldTemplate`, `KlyDynamicFormEngine`, `KlyDynamicFormPage`, `KlyDynamicFieldView` and `KlyFormProvider`, `FormService`, and the `AddKlyrooForm()` registration. Builds on Klyroo.UI and references `Klyroo.Form.Core`. |
| [`Klyroo.Form.DataAnnotations`](https://www.nuget.org/packages/Klyroo.Form.DataAnnotations) | Builds a typed schema from a C# model and its DataAnnotations attributes, and runs `IValidatableObject` and your own validation attributes at submit. References `Klyroo.Form.Core` only, so it also works without Blazor. |

## Prerequisites

To use the library in an app:

- **The .NET 10 SDK**, and an app that targets `net10.0`. The packages are prereleases, so every `dotnet add package` command needs `--prerelease`.
- **`Klyroo.Form.Core`** alone (the form controller) runs in any .NET 10 app, with no other dependency.
- **A Blazor app** for `Klyroo.Form`: a Blazor Web App or a .NET MAUI Blazor Hybrid app. Typing, validation feedback, steps and submit need an interactive render mode (Interactive Server, WebAssembly or Auto). In a WebAssembly or Auto app, register the services in the server project and in the `.Client` project.
- **Klyroo.UI set up in the app.** `Klyroo.Form` brings the Klyroo.UI package, and `AddKlyrooForm()` registers its services, but the layout still needs the Klyroo.UI stylesheets in `<head>` and `<KlyApp>` around the body, as described in the [Klyroo.UI installation](https://github.com/klyroo/klyroo-ui#installation).
- **`<KlyFormProvider>` in the layout** if you open forms as overlays with `FormService` or `DynamicFormService`.
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
using Klyroo.Form.Dynamic;

builder.Services.AddKlyrooForm();
```

Add the namespace to `_Imports.razor`:

```razor
@using Klyroo.Form
@using Klyroo.Form.Dynamic
```

Load the stylesheets in `App.razor`. The theme tokens come from Klyroo.UI and the form classes from this package:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Form/klyroo-form.css"]" />
```

## Quick start

Headless, typed: declare a form from a model, drive it with a controller.

```csharp
using Klyroo.Form;

public sealed class Contact
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

var schema = FormSchema.For<Contact>(s =>
{
    s.FormKey = "contact";
    s.Field(c => c.Name, f => f.Required());
    s.Field(c => c.Email, f => f.Required().Rule(e => e.Contains('@'), "Enter a valid email address"));
});
var email = schema.Field(c => c.Email);
var form = new FormController<Contact>(schema);

form.Set(schema.Field(c => c.Name), "Alice", markTouched: true);
form.Set(email, "alice", markTouched: true);
Console.WriteLine(form.GetError(email));       // Enter a valid email address
Console.WriteLine(await form.SubmitAsync());   // False

form.Set(email, "alice@example.com");
Contact? saved = null;
Console.WriteLine(await form.SubmitAsync((contact, ct) => { saved = contact; return Task.FromResult(true); }));  // True
Console.WriteLine(form.IsDirty);               // False
```

The runtime mode describes the same form as data, with values keyed by name:

```csharp
using Klyroo.Form.Dynamic;

var dynamicForm = new DynamicFormController(new DynamicFormSchema
{
    Fields =
    [
        new DynamicFieldSchema { Key = "name", Label = "Name", Required = true },
        new DynamicFieldSchema { Key = "email", Label = "Email", Required = true },
    ],
});
dynamicForm.SetValue("email", "alice@example.com");
```

Blazor: render the typed schema with `KlyFormEngine<TModel>`, which infers `TModel` from `Schema` and `Model`, or a dynamic schema with `KlyDynamicFormEngine`:

```razor
<KlyFormEngine Schema="_schema" Model="_contact" OnSave="SaveAsync" />
```

```razor
@page "/contact"
@rendermode InteractiveServer

<KlyDynamicFormEngine Schema="_schema" SubmitLabel="Send" OnSubmit="Send" />

@code {
    private readonly DynamicFormSchema _schema = new()
    {
        FormKey = "contact",
        Fields =
        [
            new DynamicFieldSchema { Key = "name", Label = "Name", Required = true },
            new DynamicFieldSchema { Key = "email", Label = "Email", Required = true },
        ],
    };

    private Task Send(IReadOnlyDictionary<string, object?> data)
    {
        // data holds the validated output: data["name"], data["email"]
        return Task.CompletedTask;
    }
}
```

Keep the schema in a field, as above. `KlyDynamicFormEngine` creates a new controller whenever it receives a different `DynamicFormSchema` instance, which discards what the user typed.

## Documentation

| | |
| :--- | :--- |
| [Getting started](https://github.com/klyroo/klyroo-form/blob/main/docs/getting-started.md) | Step-by-step first integration |
| [Guides](https://github.com/klyroo/klyroo-form/blob/main/docs/guides.md) | Task-oriented recipes |
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
| [Klyroo.Display](https://github.com/klyroo/klyroo-display) | Read-only display blocks, media player controls and file preview |
| [Klyroo.Spreadsheet](https://github.com/klyroo/klyroo-spreadsheet) | Import wizard for CSV, TSV and pasted data, with column and value mapping |
| [Klyroo.Agent](https://github.com/klyroo/klyroo-agent) | Bridge that lets AI agents read and act on Klyroo forms, dashboards and imports |
| [Klyroo.Stack](https://github.com/klyroo/klyroo-stack) | Meta-package, MCP documentation server and `dotnet new` templates |

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](https://github.com/klyroo/klyroo-form/blob/main/CONTRIBUTING.md) before opening a pull request, and follow the
[code of conduct](https://github.com/klyroo/klyroo-form/blob/main/CODE_OF_CONDUCT.md). Report security issues privately as described in [SECURITY.md](https://github.com/klyroo/klyroo-form/blob/main/SECURITY.md).

## License

[MIT](https://github.com/klyroo/klyroo-form/blob/main/LICENSE)
