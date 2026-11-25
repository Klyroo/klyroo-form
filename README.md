# Klyroo.Form

Schema-driven forms, validation and wizards for .NET developers, with a headless engine and Blazor components.

[![CI](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Klyroo.Form.svg)](https://www.nuget.org/packages/Klyroo.Form)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

You describe a form as data (fields, conditions, validators, steps) and a `FormController` holds its state: values, touched fields, errors, dirty tracking and step navigation. The controller has no UI dependency, so the same schema can be unit tested, run in a view model, or rendered by the Blazor components. The Blazor layer builds on Klyroo.UI and runs in Interactive Server, WebAssembly and Auto render modes, and in .NET MAUI Blazor Hybrid.

## Features

- **Headless controller**: `FormController` manages values, touched state, errors, dirty tracking, steps and submission with no UI dependency, and `Klyroo.Form.Core` is NativeAOT compatible.
- **Declarative schema**: `FormSchema` and `FormFieldSchema` describe the form as data. `FormFieldKind` has 31 values; `KlyFormEngine` renders 22 of them with a dedicated control and falls back to a text input for the other nine (see the [FAQ](docs/faq.md)).
- **Validation**: required and required-when checks, number bounds, password requirements, custom validator rules, four validation modes and three triggers (blur, input, submit).
- **Server errors**: `SetError` attaches a message to a field, optionally blocking submission until the user edits it.
- **Conditional fields**: `Condition`, `RequiredWhen`, `DisabledWhen` and `SkipWhen` are plain delegates evaluated against the controller, and hidden fields are left out of validation and output.
- **Dirty tracking**: the controller compares values with an initial snapshot, resets a field or a section, and takes a new snapshot after a successful submit.
- **Wizards and pages**: multi-step forms with skippable steps and async before-next hooks, and sectioned pages that show a completion status per section.
- **Overlays**: `FormService.CreateFormAsync` opens a form in a dialog hosted by `KlyFormProvider` and returns the result as a task.

## Packages

| Package | Description |
| :--- | :--- |
| [`Klyroo.Form.Core`](https://www.nuget.org/packages/Klyroo.Form.Core) | Headless engine: `FormSchema`, `FormController` and `FormService`. No UI dependency, trimmable, NativeAOT compatible. |
| [`Klyroo.Form`](https://www.nuget.org/packages/Klyroo.Form) | Blazor components `KlyFormEngine`, `KlyFormPage` and `KlyFormProvider`, and the `AddKlyrooForm()` registration. Builds on Klyroo.UI and references `Klyroo.Form.Core`. |

## Installation

    dotnet add package Klyroo.Form --prerelease

For the engine alone, without Blazor, install `Klyroo.Form.Core` instead.

Register the services in `Program.cs` (or `MauiProgram.cs`). It also registers Klyroo.UI:

```csharp
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
| [Getting started](docs/getting-started.md) | Step-by-step first integration |
| [Guides](docs/guides.md) | Task-oriented recipes |
| [Concepts](docs/concepts.md) | How the library works |
| [Platforms](docs/platforms.md) | Render modes, .NET MAUI, NativeAOT |
| [API reference](docs/api-reference.md) | Every public type and member |
| [FAQ](docs/faq.md) | Troubleshooting and limitations |

## Requirements

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

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request, and follow the
[code of conduct](CODE_OF_CONDUCT.md). Report security issues privately as described in [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
