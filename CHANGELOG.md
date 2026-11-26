# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- A form opened with `FormService` ran validation, `OnBeforeSubmit` and `OnSubmit` twice when submitted from its button.
- `Required` on a `Checkbox` or `Switch` field now requires it to be checked; an unchecked box no longer passes.
- `FormFieldSchema.MaxLength` is now validated by the controller, not only rendered as the `maxlength` attribute.

## [0.1.0-preview.1] - 2026-11-24

### Added

- `Klyroo.Form.Core`: a headless `FormController` driven by a declarative `FormSchema`, with 31 `FormFieldKind` values, no UI dependency and NativeAOT compatibility.
- Validation with required and required-when checks, number bounds, password requirements, custom validator rules, four validation modes and three triggers, plus `SetError` for server-side errors.
- Conditional fields (`Condition`, `RequiredWhen`, `DisabledWhen`) and value transforms (`TransformInput`, `TransformOutput`), with hidden fields left out of validation and output.
- Dirty tracking against an initial snapshot, with per-field and per-section reset and a new snapshot after a successful submit.
- Multi-step forms with skippable steps and async `OnBeforeNext` and `OnBeforePrevious` hooks, and sectioned page forms with a completion status per section.
- Repeatable rows (`ArrayList` and `ArrayTable`) and tabbed, grouped and card containers.
- `FormService` and `KlyFormProvider` to open a form as an overlay and await its `FormSubmitOutcome`.
- `Klyroo.Form`: the `KlyFormEngine`, `KlyFormPage` and `KlyFormProvider` Blazor components and `AddKlyrooForm()`, building on Klyroo.UI.

[Unreleased]: https://github.com/klyroo/klyroo-form/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/klyroo/klyroo-form/releases/tag/v0.1.0-preview.1
