# Klyroo.Form

*Part of [Klyroo](https://klyroo.dev): Build better. Compose freely.*

[![CI](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml/badge.svg)](https://github.com/klyroo/klyroo-form/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/badge/NuGet-0.1.0--preview.1-004880.svg)](https://www.nuget.org/packages/Klyroo.Form) [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/) [![NativeAOT](https://img.shields.io/badge/NativeAOT-Compatible-10b981.svg)](https://klyroo.dev/why) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

> **31-kind schema-driven form engine, reactive conditions, dirty diffing & wizards**

Declarative, 100% NativeAOT form engine in Klyroo.Form.Core: 31 field kinds (FormFieldKind), reactive conditional visibility, dirty/touched diffing, output transforms, multi-step wizards with SkipWhen, sectioned FormPage layouts, and programmatic modal/drawer overlays via FormService.

[![MAUI Hybrid](https://img.shields.io/badge/.NET%20MAUI-Blazor%20Hybrid-8b5cf6.svg)](./docs/maui-and-aot.md)

---

## Architecture (Two-Layer Headless Split)

- **`Klyroo.Form.Core`** — 100% headless C#, zero UI dependencies, `<IsAotCompatible>true</IsAotCompatible>`, `<IsTrimmable>true</IsTrimmable>`. Runs in Minimal APIs, background workers, CLI apps, and native C# .NET MAUI ViewModels.
- **`Klyroo.Form`** — Multiplatform Blazor Web (Static SSR, Interactive Server, WebAssembly, Auto) & `.NET MAUI Blazor Hybrid` (`browser`, `ios`, `android`, `maccatalyst`, `windows`).

## Highlights

- **31 Declarative Field Kinds** (`31 Field Kinds`) — Text, Password (with requirement checklist), Number, Select, AutoComplete, Checkbox, Switch, Slider, Repeatable Arrays, and Tabs driven by FormSchema.
- **Reactive Conditions & Transforms** (`Reactive Engine`) — Evaluates Condition = f => ... live, automatically skips hidden fields during validation and output building, and applies TransformOutput on submit.
- **Multi-Step Wizards with SkipWhen** (`Wizard Flow`) — Split complex flows into FormStepSchema with conditional SkipWhen branching, per-step validation, and automatic dirty re-baselining after submit.
- **FormPage & Overlay FormService** (`Overlay Host`) — Sectioned pages with completion indicators (KlyFormPage) and programmatic modal/drawer form hosts via FormService + KlyFormProvider.

## Prerequisites

- .NET 10 SDK or later.
- A Blazor Web App (Static SSR, Interactive Server, WebAssembly or Auto) or a .NET MAUI Blazor Hybrid app for the UI
  package; the `.Core` package runs anywhere .NET 10 runs, NativeAOT included.
- For .NET MAUI: the `maui` workload (`dotnet workload install maui`).

## Installation

```bash
dotnet add package Klyroo.Form.Core   # Headless C# / NativeAOT / MAUI C#
dotnet add package Klyroo.Form        # Blazor Web & .NET MAUI Blazor Hybrid
```

Register services in `Program.cs` (Blazor Web App) or `MauiProgram.cs` (.NET MAUI Blazor Hybrid):

```csharp
builder.Services.AddKlyrooForm();
```

## Quick Start

### Headless Core (`Klyroo.Form.Core`)

```csharp
using Klyroo.Form;

var schema = new FormSchema
{
    FormKey = "profile",
    Title = "User Profile",
    Fields =
    [
        new FormFieldSchema { Key = "name", Label = "Full Name", Required = true, TransformOutput = v => v?.ToString()?.Trim() },
        new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, DefaultValue = 25.0 },
        new FormFieldSchema { Key = "company", Label = "Company", Required = true, Condition = f => f.GetValue<double>("age") >= 21 }
    ]
};

var form = new FormController(schema, new Dictionary<string, object?> { ["name"] = "Alice" });
var ok = await form.SubmitAsync();
```

### Blazor Web & .NET MAUI Hybrid (`Klyroo.Form`)

```razor
<KlyFormEngine Form="@_form"
              SubmitLabel="Save profile"
              ShowReset="true"
              OnSubmit="@HandleSubmit" />
```

## Documentation

| Guide | Description |
| :--- | :--- |
| [Online documentation](https://docs.klyroo.dev/form) | Guides, live examples and API reference |
| [Getting Started](./docs/getting-started.md) | Installation, DI registration, stylesheets, and first working example |
| [Architecture](./docs/architecture.md) | Headless `.Core` vs UI split, NativeAOT guarantees, and platform matrix |
| [API Reference](./docs/api-reference.md) | Exhaustive reference of every public class, enum, property, method, and Razor parameter |
| [MAUI & NativeAOT](./docs/maui-and-aot.md) | `.NET MAUI Blazor Hybrid` (`iOS`, `Android`, `macOS`, `Windows`) and NativeAOT deployment |
| [Recipes & Testing](./docs/recipes.md) | End-to-end production patterns and `xUnit` / `bUnit` testing recipes |
| [AI Agent Skill](./skills/klyroo-form/SKILL.md) | Machine-readable guidance for coding agents |

## Key Public Types

| Type | Kind | Package | Summary |
| :--- | :--- | :--- | :--- |
| `FormValidationMode` | `enum` | `Klyroo.Form.Core` | Validation mode of a `FormController`. |
| `FormValidationTrigger` | `enum` | `Klyroo.Form.Core` | Validation trigger timing for a `FormFieldSchema`. |
| `FormShellMode` | `enum` | `Klyroo.Form.Core` | Shell layout of a `KlyFormEngine` or `FormService` overlay. |
| `FormPageSectionStatus` | `enum` | `Klyroo.Form.Core` | Completion status of a `FormPageSectionSchema` in `KlyFormPage`. |
| `FormFieldKind` | `enum` | `Klyroo.Form.Core` | Supported field kinds in the schema-driven Form Engine. |
| `FormOptionItem` | `record` | `Klyroo.Form.Core` | An option item for choice and select fields in `KlyFormEngine`. |
| `FormPasswordRequirement` | `record` | `Klyroo.Form.Core` | A password requirement rule on a `FormFieldKind.Password` field. |
| `FormValidatorRule` | `record` | `Klyroo.Form.Core` | A named validator rule on a `FormFieldSchema`. |
| `FormTabItemSchema` | `class` | `Klyroo.Form.Core` | A tab definition inside a `FormFieldKind.Tabs` container field. |
| `FormFieldSchema` | `class` | `Klyroo.Form.Core` | Declarative definition of a field in `FormSchema`. |
| `FormStepSchema` | `class` | `Klyroo.Form.Core` | One step in a multi-step `FormSchema`. |
| `FormPageSectionSchema` | `class` | `Klyroo.Form.Core` | One section in a `KlyFormPage` schema. |
| `FormPageSectionState` | `record` | `Klyroo.Form.Core` | Computed runtime state of a `FormPageSectionSchema`. |
| `FormSchema` | `class` | `Klyroo.Form.Core` | Top-level declarative schema for `KlyFormEngine`, `KlyFormPage` and `FormService`. |
| `FormSubmitOutcome` | `record` | `Klyroo.Form.Core` | Result returned when a form submit or overlay session completes. |
| `FormController` | `class` | `Klyroo.Form.Core` | Stateful runtime controller for a `FormSchema`: manages internal state, initial snapshot, dirty path tracking, touch &amp; validation rules, multi-step navigation, array item management, and submission. |
| `FormService` | `class` | `Klyroo.Form.Core` | Scoped service managing programmatic overlay forms opened in `KlyFormProvider`. |
| `KlyrooFormServiceCollectionExtensions` | `class` | `Klyroo.Form` | Public API type |

## Razor Components

| Component | Package | Parameters | Source |
| :--- | :--- | :--- | :--- |
| `<KlyFormEngine>` | `Klyroo.Form` | 8 parameters | `src/Klyroo.Form/KlyFormEngine.razor` |
| `<KlyFormPage>` | `Klyroo.Form` | 2 parameters | `src/Klyroo.Form/KlyFormPage.razor` |
| `<KlyFormProvider>` | `Klyroo.Form` | 1 parameters | `src/Klyroo.Form/KlyFormProvider.razor` |

## Building & Testing

```bash
dotnet build
dotnet test
dotnet pack -c Release
```

## Contributing

Issues and pull requests are welcome. Read [CONTRIBUTING.md](./CONTRIBUTING.md) for the workflow (Conventional
Commits, tests, documentation), follow the [Code of Conduct](./CODE_OF_CONDUCT.md), and report vulnerabilities
privately as described in [SECURITY.md](./SECURITY.md).

## Migrating from Lux

This library was previously named `lux-form`. [MIGRATION.md](./MIGRATION.md) lists every renamed package, namespace,
component, CSS token and build setting.

## License

MIT — see [LICENSE](./LICENSE). Part of the **[Klyroo](../klyroo-stack/README.md)** ecosystem.
