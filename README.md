# Klyroo.Form

Schema-driven forms, validation and wizards for .NET developers, with a headless engine and Blazor components.

[![CI](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Klyroo.Form.svg)](https://www.nuget.org/packages/Klyroo.Form)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/klyroo/klyroo-form/blob/main/LICENSE)

You describe a form as data (fields, conditions, validators, steps) and a `FormController` holds its state: values, touched fields, errors, dirty tracking and step navigation. The controller has no UI dependency, so the same schema can be unit tested, run in a view model, or rendered by the Blazor components. The Blazor layer builds on Klyroo.UI and runs in Interactive Server, WebAssembly and Auto render modes, and in .NET MAUI Blazor Hybrid.

## Features

- **Headless controller**: `FormController` manages values, touched state, errors, dirty tracking, steps and submission with no UI dependency, and `Klyroo.Form.Core` is NativeAOT compatible.
- **Declarative schema**: `FormSchema` and `FormFieldSchema` describe the form as data. `FormFieldKind` has 31 values, and `KlyFormEngine` and `KlyFormPage` render each one with a dedicated native control, from date ranges, ratings and tag inputs to collapsible containers, tabs and repeatable lists whose cells are validated.
- **Validation**: required and required-when checks, number bounds, password requirements, custom validator rules, four validation modes and three triggers (blur, input, submit).
- **Server errors**: `SetError` attaches a message to a field, optionally blocking submission until the user edits it.
- **Conditional fields**: `Condition`, `RequiredWhen`, `DisabledWhen` and `SkipWhen` are plain delegates evaluated against the controller, and hidden fields, the children of hidden containers and the fields of skipped steps are left out of validation and output.
- **Dirty tracking**: the controller compares values with an initial snapshot, resets a field or a section, and takes a new snapshot after a successful submit.
- **Wizards and pages**: multi-step forms with skippable steps and async before-next hooks, and sectioned pages that show a completion status per section.
- **Unsaved changes**: `KlyFormNavigationGuard` blocks navigation while the form is dirty, and `ExportState()` and `RestoreDraft()` keep a draft across a reload with the original baseline, so the restored draft is still dirty.
- **Overlays**: `FormService.CreateFormAsync` opens a form in a modal dialog, a side drawer or a fullscreen view hosted by `KlyFormProvider` (on `KlyModal` and `KlySlideover`: focus trap, Escape, focus restored) and returns the result as a task.

## Packages

| Package | Description |
| :--- | :--- |
| [`Klyroo.Form.Core`](https://www.nuget.org/packages/Klyroo.Form.Core) | Headless engine: `FormSchema`, `FormController` and `FormService`. No UI dependency, trimmable, NativeAOT compatible. |
| [`Klyroo.Form`](https://www.nuget.org/packages/Klyroo.Form) | Blazor components `KlyFormEngine`, `KlyFormPage`, `KlySchemaField` and `KlyFormProvider`, and the `AddKlyrooForm()` registration. Builds on Klyroo.UI and references `Klyroo.Form.Core`. |

## Prerequisites

To use the library in an app:

- **The .NET 10 SDK**, and an app that targets `net10.0`. The packages are prereleases, so every `dotnet add package` command needs `--prerelease`.
- **`Klyroo.Form.Core`** alone (the form controller) runs in any .NET 10 app, with no other dependency.
- **A Blazor app** for `Klyroo.Form`: a Blazor Web App or a .NET MAUI Blazor Hybrid app. Typing, validation feedback, steps and submit need an interactive render mode (Interactive Server, WebAssembly or Auto). In a WebAssembly or Auto app, register the services in the server project and in the `.Client` project.
- **Klyroo.UI set up in the app.** `Klyroo.Form` brings the Klyroo.UI package, and `AddKlyrooForm()` registers its services, but the layout still needs the Klyroo.UI stylesheets in `<head>` and `<KlyApp>` around the body, as described in the [Klyroo.UI installation](https://github.com/klyroo/klyroo-ui#installation).
- **`<KlyFormProvider>` in the layout** if you open forms as overlays with `FormService`.
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

Load the stylesheets in `App.razor`. The theme tokens come from Klyroo.UI and the form classes from this package:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Form/klyroo-form.css"]" />
```

## Quick start

Headless: define a schema, drive it with a controller.

```csharp
using Klyroo.Form;

var schema = new FormSchema
{
    FormKey = "contact",
    Fields =
    [
        new FormFieldSchema { Key = "name", Label = "Name", Required = true },
        new FormFieldSchema
        {
            Key = "email",
            Label = "Email",
            Required = true,
            Validators =
            [
                new FormValidatorRule("email", "Enter a valid email address",
                    (value, _) => value?.ToString()?.Contains('@') == true),
            ],
        },
    ],
    OnSubmit = form =>
    {
        Console.WriteLine($"Send a reply to {form.GetValue<string>("email")}");
        return Task.CompletedTask;
    },
};

var form = new FormController(schema);

form.SetValue("name", "Alice", markTouched: true);
form.SetValue("email", "alice", markTouched: true);
Console.WriteLine(form.GetError("email"));    // Enter a valid email address
Console.WriteLine(await form.SubmitAsync());  // False

form.SetValue("email", "alice@example.com");
Console.WriteLine(await form.SubmitAsync());  // True
Console.WriteLine(form.IsDirty);              // False
```

Blazor: render a schema with `KlyFormEngine`.

```razor
@page "/contact"
@rendermode InteractiveServer

<KlyFormEngine Schema="_schema" SubmitLabel="Send" OnSubmit="Send" />

@code {
    private readonly FormSchema _schema = new()
    {
        FormKey = "contact",
        Fields =
        [
            new FormFieldSchema { Key = "name", Label = "Name", Required = true },
            new FormFieldSchema { Key = "email", Label = "Email", Required = true },
        ],
    };

    private Task Send(IReadOnlyDictionary<string, object?> data)
    {
        // data holds the validated output: data["name"], data["email"]
        return Task.CompletedTask;
    }
}
```

Keep the schema in a field, as above. `KlyFormEngine` creates a new controller whenever it receives a different `FormSchema` instance, which discards what the user typed.

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
