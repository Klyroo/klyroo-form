# Platforms

What runs where, what you must set up, and the limits that are known today. Everything below comes from the project files and the code. Where something was run, the page says so; where it was only declared, it says that too.

## At a glance

| Target | `Klyroo.Form.Core` | `Klyroo.Form` |
| :--- | :--- | :--- |
| Console, worker, minimal API, test project | Yes | Not applicable |
| NativeAOT | Compatible, published and run | Not declared |
| Trimming | Marked trimmable | Marked trimmable |
| Blazor Interactive Server | Not applicable | Yes |
| Blazor Interactive WebAssembly | Not applicable | Yes |
| Blazor Interactive Auto | Not applicable | Yes |
| Blazor Static SSR | Not applicable | Renders, not interactive |
| .NET MAUI Blazor Hybrid | Not applicable | Supported platforms declared, not run on a device |
| .NET MAUI without a `BlazorWebView` | Yes, from view models | Not applicable |

Both packages target `net10.0`. `Klyroo.Form.Core` has no package or project dependencies. `Klyroo.Form` depends on Klyroo.UI and on `Klyroo.Form.Core`.

## Render modes

`KlyFormEngine`, `KlyFormPage` and `KlyFormProvider` are built from standard Blazor events (`@oninput`, `@onchange`, `@onclick`, `@onsubmit`) and make no JavaScript interop calls of their own. They need an interactive render mode to respond to input.

**Interactive Server.** Works. Add `@rendermode InteractiveServer` to the page, or set the render mode globally. `FormService` is registered as a scoped service, so it lives as long as the circuit, and a `KlyFormProvider` in the layout serves the whole circuit.

**Interactive WebAssembly and Auto.** Work. Components run in the browser, and in Auto also on the server for the first render, so call `AddKlyrooForm()` in both the server project and the `.Client` project. A scoped service in WebAssembly lives as long as the app.

**Static SSR.** The components render their markup, but without a circuit or WebAssembly nothing handles the events, so typing, validation, steps and submit do nothing. There is no form-post fallback: the engine does not set a form name or post to the server. Render the form interactively.

**Prerendering.** Interactive pages are prerendered by default. The prerender produces the initial markup; when the page becomes interactive, the component starts again and `KlyFormEngine` creates its own controller from `Schema` and `Input`. Values typed before that moment are not carried over, and neither is state in a controller you created yourself unless you persist it with your own means.

**Controller lifetime.** A controller passed through `Form` belongs to you: create it once, for example in a field of the page, and the component subscribes to it and unsubscribes on dispose. A schema passed through `Schema` belongs to the component, which creates a new controller whenever the instance changes.

## Stylesheets

The theme tokens (`--klyroo-*`) come from Klyroo.UI, and the form's own classes come from `klyroo-form.css`, which the package ships as a static web asset:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Form/klyroo-form.css"]" />
```

The form relies on Klyroo.UI tokens for color, border and radius, so with only `klyroo-form.css` the colors resolve to nothing. The Klyroo.UI [repository](https://github.com/klyroo/klyroo-ui) describes its other styling modes, including Tailwind compiled by your app; this package ships its classes as the precompiled `klyroo-form.css` and this documentation does not cover mixing it with your own Tailwind build.

## .NET MAUI Blazor Hybrid

`Klyroo.Form` declares `browser`, `ios`, `android`, `maccatalyst` and `windows` as `SupportedPlatform` items, so a multi-targeted MAUI project that references it does not get platform compatibility warnings for it. The repository tests render the components with bUnit; no test runs on a device or emulator, so treat the platform list as a declaration.

Register the services in `MauiProgram.cs`, after `AddMauiBlazorWebView()`:

```csharp
builder.Services.AddMauiBlazorWebView();
builder.Services.AddKlyrooForm();
```

Load the same three stylesheets from `wwwroot/index.html`, with plain paths instead of `@Assets`:

```html
<link rel="stylesheet" href="_content/Klyroo.UI/klyroo.preflight.css" />
<link rel="stylesheet" href="_content/Klyroo.UI/klyroo.css" />
<link rel="stylesheet" href="_content/Klyroo.Form/klyroo-form.css" />
```

Klyroo.UI itself declares only `browser` as a supported platform. That is a property of the Klyroo.UI project, and `Klyroo.Form` depends on it.

## .NET MAUI with XAML or C# views

If your pages do not use a `BlazorWebView`, reference only `Klyroo.Form.Core`. A view model owns a `FormController`, binds its properties to the controls, and subscribes to `Changed` to refresh them. `Changed` is raised synchronously on the thread that changed the form, so marshal to the UI thread if you update the controller from another one.

## NativeAOT and trimming

`Klyroo.Form.Core` sets `IsAotCompatible` and `IsTrimmable` to `true`, and the solution builds with warnings as errors, so the AOT and trimming analyzers are part of every build. It was also checked end to end: a console app that references the project and sets `PublishAot` published and ran on `linux-x64` (value conversion in `GetValue<T>`, validation and submit all worked). The core uses no reflection over your types and no serialization: values are `object?` and conversions go through `Convert.ChangeType`.

`Klyroo.Form` sets `IsTrimmable` to `true` but does not set `IsAotCompatible`. Whether a Blazor app that uses it can be trimmed or compiled ahead of time also depends on Klyroo.UI and on the Blazor runtime, and that combination was not tested here.

## Known limitations

- The overlay modes `Drawer` and `Fullscreen` render like `Modal`.
- `KlyFormPage` renders every field as a text input.
- `KlyFormEngine` has no dedicated control for nine field kinds and renders a text input instead. The [FAQ](faq.md#a-field-kind-shows-a-text-input) lists them.
- The built-in labels and messages are English, and only some of them can be changed. See the [FAQ](faq.md#can-i-translate-the-built-in-texts).
