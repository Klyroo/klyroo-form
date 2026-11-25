# Recipes & Testing — Klyroo.Form

## Recipe 1: Multi-Step Wizard with SkipWhen Branching

```csharp
var onboarding = new FormSchema
{
    FormKey = "onboarding",
    Title = "Onboarding",
    Steps =
    [
        new FormStepSchema { Key = "account", Title = "Account", Fields = [new() { Key = "plan", DefaultValue = "free" }] },
        new FormStepSchema { Key = "billing", Title = "Billing", SkipWhen = f => f.GetValue<string>("plan") == "free", Fields = [new() { Key = "vat", Required = true }] }
    ]
};
```

## Recipe 2: Headless Unit Testing (`xUnit`)

Test all business logic in `Klyroo.Form.Core` without spinning up a browser or Blazor renderer:

```csharp
using Xunit;

public sealed class FormUnitTests
{
    [Fact]
    public void Headless_core_runs_without_ui_host()
    {
        // Arrange & Act using Klyroo.Form.Core directly
        // Assert deterministic outputs with zero JSInterop setup
    }
}
```

## Recipe 3: Component Testing (`bUnit`)

```csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class FormComponentTests : BunitContext
{
    public FormComponentTests()
    {
        Services.AddKlyrooForm();
    }
}
```

> **Tip**: Always register Klyroo services on `Services` **before** calling `SetRendererInfo(...)` in bUnit tests.
