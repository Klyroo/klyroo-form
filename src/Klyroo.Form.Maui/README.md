# Klyroo.Form.Maui

Native .NET MAUI views (no WebView, no Blazor) for Klyroo.Form. They are views over the controllers of `Klyroo.Form.Core`: values, touched state, errors, dirty state, steps and submit all stay in `FormController<TModel>` and `DynamicFormController`, and every field is drawn with the inputs of [Klyroo.UI.Maui](https://github.com/klyroo/klyroo-ui) (`KlyInput`, `KlyTextarea`, `KlySelect`, `KlyCheckbox`, `KlySwitch`, `KlyInputNumber`, `KlyInputDate`, `KlyRadioGroup`, `KlyFileUpload`...), so a form behaves like the Blazor one and looks like the rest of the app, light and dark.

`Klyroo.Form` itself runs in a `BlazorWebView` on MAUI. This package is for apps that want real native views. The typed views are generic, so they are made in C# (a generic view cannot be a XAML element); the runtime-schema ones (`KlyDynamic*`) are not generic and have bindable properties.

## Controls

| Control | Blazor counterpart | Notes |
| :--- | :--- | :--- |
| `KlyFormEngine<TModel>` | `KlyFormEngine<TModel>` | Header, stepper, fields grid and footer actions of a typed form. `Form` or `Schema` + `Model`, `Shell`, `SubmitLabel`, `ShowReset`, `OnSave`, `Submitted`, `Cancelled`, `Templates`. |
| `KlyFormPage<TModel>` | `KlyFormPage<TModel>` | A sectioned typed form: list of sections with their status (a tap scrolls to the section), one card per section, submit button. |
| `KlyFormFieldView<TModel>` | `KlyFormFieldView<TModel>` | One field of a typed form, for a layout of your own. It follows the controller by itself. |
| `KlyFieldTemplate<TModel, TValue>` | `KlyFieldTemplate<TModel, TValue>` | Draws the control of one field with a view of your own, from its `FormControlContext<TValue>`. |
| `KlyFormProvider` | `KlyFormProvider` | A `KlyOverlayHost` that shows the forms opened with `FormService` and `DynamicFormService` in a `KlyModal`, a `KlySlideover` or a fullscreen layer. |
| `KlyFormErrorSummary` | `KlyFormErrorSummary` | The errors of a form; a tap on an entry focuses its control. Tells a screen reader after a failed submit. |
| `KlyFormNavigationGuard` | `KlyFormNavigationGuard` | Asks before leaving a page with unsaved changes, through `Shell.Navigating`. |
| `KlyDynamicFormEngine` | `KlyDynamicFormEngine` | The same as `KlyFormEngine<TModel>` for a `DynamicFormSchema`. |
| `KlyDynamicFormPage` | `KlyDynamicFormPage` | The same as `KlyFormPage<TModel>` for a `DynamicFormSchema`. |
| `KlyDynamicFieldView` | `KlyDynamicFieldView` | The same as `KlyFormFieldView<TModel>` for a `DynamicFormController`. |

Also public: `FormService` (typed overlays; `DynamicFormService` comes from Core), `FormTexts`, `FormCustomFields`, `AddKlyrooFormMaui`.

## Fields

| Kind | Control |
| :--- | :--- |
| Text, PhoneNumber, OneTimeCode | `KlyInput` (telephone and numeric keyboards) |
| Textarea | `KlyTextarea` |
| Password | `KlyInput` with a Show/Hide button and the checklist of the requirements |
| Number | `KlyInputNumber` (every digit of a decimal is kept; bounds are reported by the form, not applied by the field) |
| Slider | `KlySlider` and its value |
| Checkbox, Switch | `KlyCheckbox`, `KlySwitch` |
| Radio, CheckboxGroup, multiple Select and AutoComplete | `KlyRadioGroup` / `KlyCheckboxGroup`, one per option group |
| Select | `KlySelect` (the native picker), or `KlySelectMenu` when an option is grouped or disabled |
| AutoComplete | `KlyInputMenu` |
| Date, Time, DateTime, DateRange | `KlyInputDate`, `KlyInputTime`, both side by side, two dates |
| Rating, ColorPicker, Tag, File | `KlyInputRating`, `KlyColorPicker`, `KlyInputTags`, `KlyFileUpload` |
| Section, Info, Divider, Button | a heading label, `KlyAlert` (or plain text), `KlySeparator`, `KlyButton` |
| Group, Card, Object, Tabs, ArrayList | a grid of fields, `KlyCard`, `KlyCollapsible`, `KlyTabs`, a card of rows with `KlyButton` and an icon button |
| Custom | the `KlyFieldTemplate` of the field, the `FieldTemplates` of a dynamic form, then `FormCustomFields` |

## Use

```csharp
using Klyroo.Form.Maui;

var engine = new KlyFormEngine<Signup>
{
    Schema = schema,
    Model = new Signup(),
    OnSave = async (model, token) => await api.SaveAsync(model, token),
};
engine.Submitted += (_, e) => Navigate(e.Model);

Content = new KlyFormProvider { Content = new ScrollView { Content = engine } };
```

Open a form in an overlay from anywhere that has the service:

```csharp
var outcome = await FormService.Default.CreateFormAsync(schema, model, FormShellMode.Drawer);
if (outcome.IsCompleted) await SaveAsync(outcome.Model!);
```

Guard a page:

```csharp
layout.Add(new KlyFormNavigationGuard { Form = engine.EffectiveController });
```

## What the app provides

Nothing is required: `KlyFormProvider` serves `FormService.Default` and `FormService.DefaultDynamic`. To use the container of the app instead, register the services (singletons, a MAUI app has one window) and the provider picks them up:

```csharp
builder.Services.AddKlyrooFormMaui();
```

Keep one `KlyFormProvider` around the content of the page (or of the shell page): it is the `KlyOverlayHost` the form overlays, and the other overlay controls of Klyroo.UI.Maui, draw in. The texts follow `FormTexts.Current` (English, with the validation messages of the UI culture of the device); replace it at startup to translate the views.

## Differences from the Blazor views

- **Layout.** The web 12-column grid is kept (`Columns`, `Span`), but below `CompactWidth` (480 by default) every field takes the whole row, since two columns do not fit a phone. A form longer than the screen goes in a `ScrollView`.
- **Live views.** A native view does not re-render from its parent, so `KlyFormFieldView`, `KlyDynamicFieldView` and the engines subscribe to the controller and listen while they are in a page. Call `Dispose` when the page goes away for good; it disposes the controller the view made from `Schema`.
- **Events.** `OnSubmit` is the `Submitted` event and `OnCancel` the `Cancelled` event (the Cancel button shows while someone listens). `OnSave` keeps its name. A dynamic form passes its output in `DynamicFormOutputEventArgs`.
- **Templates.** `KlyFormEngine.Templates` takes `KlyFieldTemplate`s instead of child content, and a template builds a `View`. The context is a snapshot: the view is built again when the form changes the field outside of it, or when the field becomes disabled, required or gets another error, but not for a change the control makes through its own context.
- **Reset.** The Reset button puts the form back to its baseline (the values at creation or after the last successful submit). On the Blazor shell a dynamic form built with `Input` goes back to the schema defaults instead.
- **Select.** The native picker has no groups and no disabled rows, so a select with either uses `KlySelectMenu`; the empty option of the web `<select>` is the first row of the picker. A radio or checkbox option has no icon (the Klyroo.UI.Maui items have none).
- **Dates.** A date and a time are two native pickers; a DateTime is stored once both are chosen, and the half chosen first stays on screen meanwhile.
- **Text input.** A field of the form is written on every keystroke, as on the web. An AutoComplete writes when the text is committed (return, or leaving the field), and ignores `MaxLength`.
- **Files.** A picked file is read into memory up to `MaxFileSize`, like the Blazor one. A file the form already holds (a draft, code) cannot be drawn in the list, which shows what the user picked; emptying the form empties the list.
- **Per-field reset.** The `DirtyCheck` reset link sits under the control, not in the label row. Help and error share the line under the control, the error replacing the help (`KlyFormField` behavior).
- **Overlay.** `Inline` and `Modal` forms open in a `KlyModal`, `Drawer` in a `KlySlideover` on the right, `Fullscreen` in a layer that covers the host. The close button, a tap outside and the back button of Android close it, except while it saves.

## Navigation guard

`KlyFormNavigationGuard` listens to `Shell.Navigating` (the back button of Android, the back gesture, the back arrow, a tab or a route change) and holds the navigation with a deferral while it asks, in the `KlyOverlayHost` it is in (else a page alert). Pushing a page on top is not guarded.

Not feasible: outside a Shell (a `NavigationPage`, a `TabbedPage`) MAUI has no event that can cancel a back press or a swipe, so call `ConfirmLeaveAsync()` from your own back handling instead; closing the app, sending it to the background and the system killing it cannot be confirmed (the web `ConfirmExternalNavigation` has no native counterpart), so keep a draft (`ExportDraft`, or `ExportState` on a dynamic controller) when the loss matters.

## Build

This project is not part of `Klyroo.Form.slnx`: it needs the MAUI workload. It targets `net10.0` (the unit tests run on it) and `net10.0-android`, plus iOS and Mac Catalyst outside Linux and Windows on Windows. A machine whose MAUI workload is the .NET 11 one builds with `-p:KlyrooMauiTfm=net11.0`. `Klyroo.Form.Core` shows its internals to this assembly (the same `InternalsVisibleTo` as `Klyroo.Form`), since the views drive the engine of the controllers.

```bash
dotnet workload install maui-android
dotnet build src/Klyroo.Form.Maui -f net10.0
dotnet test --project tests/Klyroo.Form.Maui.Tests
```

## Status

Unit-tested on the plain `net10.0` target (state, styling and the controller wiring of every view). Built for Android; not run on a device or an emulator, nor on iOS, Mac Catalyst or Windows. Not exercised without a device: the focus that moves to a field after a failed submit, the scroll to a section, the announcements to a screen reader, the services the provider takes from the container of the app, and the subscription of the guard to the real `Shell`.
