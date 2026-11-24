# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

- **Primary**: .NET 10 / Blazor and `.NET MAUI Blazor Hybrid` developers building schema-driven CRUD workflows, multi-step onboarding/settings forms, reactive drawers/modals, and AI-operable forms.

## Product Purpose

`lux-form` pairs a headless NativeAOT reactive form engine (`Lux.Form.Core`) with `LxFormEngine` and imperative form shells (`Lux.Form`, supporting Drawer, Modal, and Page presentation) for Blazor Web and `.NET MAUI Blazor Hybrid`.

Success means developers define a `FormSchema` once and automatically get conditional field visibility, dynamic required rules, dirty tracking and diffs, typed validation, secret-field handling, and seamless `Lux.Agent` registration without hand-wiring boilerplate form state.

## Positioning

Unlike manual `EditForm` markup that duplicates validation, conditional visibility, and shell orchestration per screen, `Lux.Form` executes reactive field graphs, conditional rules, and dirty diffs in a zero-UI NativeAOT `FormController` (`Lux.Form.Core`) that works across Page, Modal, and Drawer shells and exposes controls semantically to `Lux.Agent`.

## Operating Context

- **Packages**: `Lux.Form.Core` (`FormSchema`, `FormFieldSchema`, `FormController`, validation, dirty diff) and `Lux.Form` (`LxFormEngine`, `FormService`, `FormShellMode.Drawer`/`Modal`/`Page`, `lux-form.css`).
- **Integration**: Built on `Lux.UI` form primitives and natively registrable in `Lux.Agent` (`AgentBridgeService.RegisterForm`) with password/OTP redaction and submit confirmation policies.
- **Showcase**: Powers both the reactive `New Account` Drawer (with conditional EU VAT field) and the Copilot-bound `Quick Deal` form in `Lux.Stack.Site` `/showcase`.

## Capabilities and Constraints

- `Lux.Form.Core` is 100% headless with `<IsAotCompatible>true</IsAotCompatible>` and `<IsTrimmable>true</IsTrimmable>`.
- Supports conditional field visibility (`Condition`), dynamic required predicates (`RequiredWhen`), default values, dirty diff tracking, and sensitive field kinds (`Password`, `OneTimeCode`).
- Ships precompiled zero-friction CSS (`lux-form.css`) alongside native Tailwind CSS v4 compatibility.
- 100% MIT open-source license; strict `Lux.Form*` / `LxForm*` / `--lux-*` naming.

## Brand Commitments

- **Name**: `Lux.Form` (`lux-form`), part of the **LuxStack** ecosystem.
- **Voice**: Clear, engineering-focused, and ergonomic.

## Evidence on Hand

- Passing unit and bUnit test suite (`tests/Lux.Form.Tests`) plus NativeAOT publish verification in CI.
- Live Drawer CRUD and Copilot-connected form demonstrations in `Lux.Stack.Site` `/showcase` and product docs at `/form`.
- Verified precompiled standalone CSS (`lux-form.css`).
- **Explicit absences (do not fabricate)**: No NuGet download stats, customer logos, or third-party testimonials exist yet.

## Product Principles

1. **Schema-driven reactivity in pure C#**: Conditions, validation, and dirty state live in testable `FormController` logic, independent of the DOM.
2. **One schema, any shell**: The same `FormSchema` renders as an inline page form, a modal dialog, or a slide-over drawer.
3. **Safe handling of sensitive inputs**: Password and one-time-code fields carry explicit semantic kinds so UI reveal toggles and AI bridges redact them consistently.
4. **Clear, actionable validation**: Required markers, inline field errors, and non-destructive reset/cancel flows keep users in control.

## Accessibility & Inclusion

- Durable standard: **WCAG 2.1 AA** across all generated form fields and shells: explicit `<label>` associations, `aria-invalid`/`aria-describedby` error linking, accessible password show/hide toggles, keyboard-operable drawer/modal focus trapping, and clear focus indicators.
