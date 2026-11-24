# Getting Started with Lux.Form

Declarative, 100% NativeAOT form engine in Lux.Form.Core: 31 field kinds (FormFieldKind), reactive conditional visibility, dirty/touched diffing, output transforms, multi-step wizards with SkipWhen, sectioned FormPage layouts, and programmatic modal/drawer overlays via FormService.

## 1. Choose Your Package

| Use Case | Package to Install |
| :--- | :--- |
| Headless C#, Minimal APIs, Workers, CLI, or native C# .NET MAUI ViewModels | `Lux.Form.Core` |
| Blazor Web App (Server, WebAssembly, Auto, Static SSR) or `.NET MAUI Blazor Hybrid` | `Lux.Form` |

```bash
dotnet add package Lux.Form.Core
dotnet add package Lux.Form
```

## 2. Register Services

Call `builder.Services.AddLuxForm();` in `Program.cs` (both Server and Client projects when using Blazor Interactive Auto) or `MauiProgram.cs` (after `AddMauiBlazorWebView()`):

```csharp
builder.Services.AddLuxForm();
```

## 3. Add Namespace Imports

In `_Imports.razor`:

```razor
@using Lux.UI
@using Lux.Form

```

## 4. First Example

### Headless C# (`Lux.Form.Core`)

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

### Blazor / MAUI Hybrid (`Lux.Form`)

```razor
<LxFormEngine Form="@_form"
              SubmitLabel="Save profile"
              ShowReset="true"
              OnSubmit="@HandleSubmit" />
```

## Next Steps

- [Architecture & Two-Layer Design](./architecture.md)
- [Complete API Reference](./api-reference.md)
- [.NET MAUI & NativeAOT Guide](./maui-and-aot.md)
- [Recipes & Testing](./recipes.md)
