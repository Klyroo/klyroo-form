# Architecture — Lux.Form

Lux.Form follows the strict headless two-layer architecture shared across every repository in **LuxStack**:

```text
+-------------------------------------------------------------------+
|  Lux.Form                                                        |
|  Blazor Web (SSR / Server / WASM / Auto) & .NET MAUI Hybrid       |
|  SupportedPlatform: browser, ios, android, maccatalyst, windows   |
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|  Lux.Form.Core                                                   |
|  100% Headless C# · Zero UI Dependency · IsAotCompatible=true     |
|  Works in APIs, Workers, CLI, NativeAOT & Native C# MAUI          |
+-------------------------------------------------------------------+
```

## Design Principles

1. **Zero UI Coupling in `Lux.Form.Core`** — State machines, validation rules, ASTs, formatters, and parsers live in `Lux.Form.Core` so they can be unit tested in milliseconds with plain `xUnit` without spinning up a renderer.
2. **NativeAOT & Trimming Verified** — `Lux.Form.Core` sets `<IsAotCompatible>true</IsAotCompatible>` and `<IsTrimmable>true</IsTrimmable>` with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
3. **Every Render Mode & Native Hybrid** — `Lux.Form` avoids browser-only assumptions during prerendering and declares `<SupportedPlatform>` for `browser`, `ios`, `android`, `maccatalyst`, and `windows`.
4. **Composable with `Lux.Stack`** — Install `Lux.Form` standalone or aggregate the entire ecosystem with `dotnet add package Lux.Stack` and `services.AddLuxStack()`.

## Supported Platforms

- Headless C# / NativeAOT
- .NET MAUI Native C#
- Blazor Server / WASM / Auto
- .NET MAUI Blazor Hybrid
