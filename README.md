# Lux.Form (LuxStack)

> **31-kind schema-driven form engine, reactive conditions, dirty diffing & wizards**

Declarative, 100% NativeAOT form engine in Lux.Form.Core: 31 field kinds (FormFieldKind), reactive conditional visibility, dirty/touched diffing, output transforms, multi-step wizards with SkipWhen, sectioned FormPage layouts, and programmatic modal/drawer overlays via FormService.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![NativeAOT](https://img.shields.io/badge/Core-NativeAOT%20Ready-10b981.svg)](./docs/maui-and-aot.md)
[![MAUI Hybrid](https://img.shields.io/badge/.NET%20MAUI-Blazor%20Hybrid-8b5cf6.svg)](./docs/maui-and-aot.md)

---

## Architecture (Two-Layer Headless Split)

- **`Lux.Form.Core`** — 100% headless C#, zero UI dependencies, `<IsAotCompatible>true</IsAotCompatible>`, `<IsTrimmable>true</IsTrimmable>`. Runs in Minimal APIs, background workers, CLI apps, and native C# .NET MAUI ViewModels.
- **`Lux.Form`** — Multiplatform Blazor Web (Static SSR, Interactive Server, WebAssembly, Auto) & `.NET MAUI Blazor Hybrid` (`browser`, `ios`, `android`, `maccatalyst`, `windows`).

## Highlights

- **31 Declarative Field Kinds** (`31 Field Kinds`) — Text, Password (with requirement checklist), Number, Select, AutoComplete, Checkbox, Switch, Slider, Repeatable Arrays, and Tabs driven by FormSchema.
- **Reactive Conditions & Transforms** (`Reactive Engine`) — Evaluates Condition = f => ... live, automatically skips hidden fields during validation and output building, and applies TransformOutput on submit.
- **Multi-Step Wizards with SkipWhen** (`Wizard Flow`) — Split complex flows into FormStepSchema with conditional SkipWhen branching, per-step validation, and automatic dirty re-baselining after submit.
- **FormPage & Overlay FormService** (`Overlay Host`) — Sectioned pages with completion indicators (BxFormPage) and programmatic modal/drawer form hosts via FormService + BxFormProvider.

## Installation

```bash
dotnet add package Lux.Form.Core   # Headless C# / NativeAOT / MAUI C#
dotnet add package Lux.Form        # Blazor Web & .NET MAUI Blazor Hybrid
```

Register services in `Program.cs` (Blazor Web App) or `MauiProgram.cs` (.NET MAUI Blazor Hybrid):

```csharp
builder.Services.AddLuxForm();
```

## Quick Start

### Headless Core (`Lux.Form.Core`)

```csharp
using Lux.Form;

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

### Blazor Web & .NET MAUI Hybrid (`Lux.Form`)

```razor
<BxFormEngine Form="@_form"
              SubmitLabel="Save profile"
              ShowReset="true"
              OnSubmit="@HandleSubmit" />
```

## Documentation

| Guide | Description |
| :--- | :--- |
| [Getting Started](./docs/getting-started.md) | Installation, DI registration, stylesheets, and first working example |
| [Architecture](./docs/architecture.md) | Headless `.Core` vs UI split, NativeAOT guarantees, and platform matrix |
| [API Reference](./docs/api-reference.md) | Exhaustive reference of every public class, enum, property, method, and Razor parameter |
| [MAUI & NativeAOT](./docs/maui-and-aot.md) | `.NET MAUI Blazor Hybrid` (`iOS`, `Android`, `macOS`, `Windows`) and NativeAOT deployment |
| [Recipes & Testing](./docs/recipes.md) | End-to-end production patterns and `xUnit` / `bUnit` testing recipes |
| [AI Agent Skill](./skills/lux-form/SKILL.md) | Machine-readable guidance for coding agents |

## Key Public Types

| Type | Kind | Package | Summary |
| :--- | :--- | :--- | :--- |
| `FormValidationMode` | `enum` | `Lux.Form.Core` | Validation mode of a `FormController`. |
| `FormValidationTrigger` | `enum` | `Lux.Form.Core` | Validation trigger timing for a `FormFieldSchema`. |
| `FormShellMode` | `enum` | `Lux.Form.Core` | Shell layout of a `BxFormEngine` or `FormService` overlay. |
| `FormPageSectionStatus` | `enum` | `Lux.Form.Core` | Completion status of a `FormPageSectionSchema` in `BxFormPage`. |
| `FormFieldKind` | `enum` | `Lux.Form.Core` | Supported field kinds in the schema-driven Form Engine. |
| `FormOptionItem` | `record` | `Lux.Form.Core` | An option item for choice and select fields in `BxFormEngine`. |
| `FormPasswordRequirement` | `record` | `Lux.Form.Core` | A password requirement rule on a `FormFieldKind.Password` field. |
| `FormValidatorRule` | `record` | `Lux.Form.Core` | A named validator rule on a `FormFieldSchema`. |
| `FormTabItemSchema` | `class` | `Lux.Form.Core` | A tab definition inside a `FormFieldKind.Tabs` container field. |
| `FormFieldSchema` | `class` | `Lux.Form.Core` | Declarative definition of a field in `FormSchema`. |
| `FormStepSchema` | `class` | `Lux.Form.Core` | One step in a multi-step `FormSchema`. |
| `FormPageSectionSchema` | `class` | `Lux.Form.Core` | One section in a `BxFormPage` schema. |
| `FormPageSectionState` | `record` | `Lux.Form.Core` | Computed runtime state of a `FormPageSectionSchema`. |
| `FormSchema` | `class` | `Lux.Form.Core` | Top-level declarative schema for `BxFormEngine`, `BxFormPage` and `FormService`. |
| `FormSubmitOutcome` | `record` | `Lux.Form.Core` | Result returned when a form submit or overlay session completes. |
| `FormController` | `class` | `Lux.Form.Core` | Stateful runtime controller for a `FormSchema`: manages internal state, initial snapshot, dirty path tracking, touch &amp; validation rules, multi-step navigation, array item management, and submission. |
| `FormService` | `class` | `Lux.Form.Core` | Scoped service managing programmatic overlay forms opened in `BxFormProvider`. |
| `LuxFormServiceCollectionExtensions` | `class` | `Lux.Form` | Public API type |

## Razor Components

| Component | Package | Parameters | Source |
| :--- | :--- | :--- | :--- |
| `<BxFormEngine>` | `Lux.Form` | 8 parameters | `src/Lux.Form/BxFormEngine.razor` |
| `<BxFormPage>` | `Lux.Form` | 2 parameters | `src/Lux.Form/BxFormPage.razor` |
| `<BxFormProvider>` | `Lux.Form` | 1 parameters | `src/Lux.Form/BxFormProvider.razor` |

## Building & Testing

```bash
dotnet build
dotnet test
dotnet pack -c Release
```

## License

MIT — see [LICENSE](./LICENSE). Part of the **[LuxStack](../lux.stack/README.md)** ecosystem.
