# Guides

Task-oriented recipes. Each one states a goal, shows complete code and explains the parts that are not obvious. The Blazor samples assume the setup from [Getting started](getting-started.md): `AddKlyrooForm()` is registered, `@using Klyroo.Form` is in `_Imports.razor` and the stylesheets are loaded. Names such as `accounts` and `WorkspaceDirectory` stand for your own services.

- [Validate a field with custom rules](#validate-a-field-with-custom-rules)
- [Report a server error on a field](#report-a-server-error-on-a-field)
- [Check a value against a service while the user types](#check-a-value-against-a-service-while-the-user-types)
- [Save from the component and keep the form busy](#save-from-the-component-and-keep-the-form-busy)
- [Show and require fields conditionally](#show-and-require-fields-conditionally)
- [Build a multi-step wizard](#build-a-multi-step-wizard)
- [Build a settings page with sections](#build-a-settings-page-with-sections)
- [Open a form in a dialog from anywhere](#open-a-form-in-a-dialog-from-anywhere)
- [Edit repeatable rows](#edit-repeatable-rows)
- [Prefill and reset a form](#prefill-and-reset-a-form)
- [Add your own control](#add-your-own-control)
- [Upload files](#upload-files)
- [Test your forms](#test-your-forms)

## Validate a field with custom rules

Goal: reject a value that passes the built-in checks, and choose when the error appears.

```csharp
using Klyroo.Form;

var schema = new FormSchema
{
    FormKey = "signup",
    Fields =
    [
        new FormFieldSchema
        {
            Key = "username",
            Label = "Username",
            Required = true,
            Trigger = FormValidationTrigger.Input,
            Validators =
            [
                new FormValidatorRule("length", "Use 3 to 20 characters",
                    (value, _) => value?.ToString() is { Length: >= 3 and <= 20 }),
                new FormValidatorRule("charset", "Use letters, digits and dashes only",
                    (value, _) => value?.ToString()?.All(c => char.IsLetterOrDigit(c) || c == '-') == true),
            ],
        },
        new FormFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, Min = 18, Max = 120 },
        new FormFieldSchema
        {
            Key = "password",
            Label = "Password",
            Kind = FormFieldKind.Password,
            Required = true,
            PasswordRequirements =
            [
                new FormPasswordRequirement("length", "Use at least 12 characters", p => p.Length >= 12),
                new FormPasswordRequirement("digit", "Use at least one digit", p => p.Any(char.IsDigit)),
            ],
        },
        new FormFieldSchema
        {
            Key = "terms",
            Label = "I accept the terms",
            Kind = FormFieldKind.Checkbox,
            Validators = [new FormValidatorRule("accepted", "You must accept the terms", (value, _) => value is true)],
        },
    ],
};
```

`Required`, number bounds (`Min` and `Max` on `FormFieldKind.Number`), `MaxLength`, password requirements and `Validators` run in that order on each validation, and the first failure becomes the error. For a field that is empty and required, only the required message is reported.

`Validators` also run when the value is empty and the field is not required, so a rule such as `terms` above sees `null` until the user toggles the checkbox. Write rules that accept `null`, or reject it on purpose, as `terms` does. A `Required` checkbox or switch must be checked, so `Required = true` is enough for a consent box; a validator like `terms` is only needed for a custom message or rule.

`Trigger` decides when a field is validated while the user edits it. `Blur` (the default) starts validating when the control first loses focus and then validates on every change, `Input` validates from the first change, and `Submit` waits for `Validate()` or a submit. Errors only show for touched fields, and `Validate()` and `SubmitAsync()` touch every field they check.

Rules that read another field, such as a confirmation that compares itself with the password, stay correct while the user edits the other field. After every change the controller validates again the touched fields that currently show an error, so a stale "Passwords differ" disappears when the password is changed to match. It cannot guess that an untouched-looking valid field became invalid, so name the fields your rules read with `DependsOn`:

```csharp
new FormFieldSchema
{
    Key = "confirm",
    Kind = FormFieldKind.Password,
    DependsOn = ["password"],
    Validators = [new FormValidatorRule("match", "Passwords differ",
        (value, form) => Equals(value, form.GetValue("password")))],
}
```

With `DependsOn`, a change of `password` validates `confirm` again once the user has touched it, so the mismatch appears and clears as soon as the other field changes. A field the user has not touched yet stays quiet, and a field with `Trigger = Submit` still waits for submission. `RequiredWhen` follows the same rule: its error disappears when the condition stops holding, and appears on a touched field when you list the field the condition reads in `DependsOn`.

## Report a server error on a field

Goal: check a value against a service on submit and show the result on the field.

```csharp
using Klyroo.Form;

// accounts is your own service
var schema = new FormSchema
{
    FormKey = "register",
    Fields = [new FormFieldSchema { Key = "email", Label = "Email", Required = true }],
    OnBeforeSubmit = async form =>
    {
        if (await accounts.IsEmailTakenAsync(form.GetValue<string>("email")!))
        {
            form.SetError("email", "This email is already registered");
            return false;
        }
        return true;
    },
    OnSubmit = form => accounts.CreateAsync(form.BuildOutput()),
};
```

`OnBeforeSubmit` runs after local validation passes and before `OnSubmit`. Returning `false` cancels the submit, `SetError` puts the message on the field, and the next `SetValue` on that field clears it. `SubmitAsync()` returns `false` and the form stays dirty.

`SetError` takes a `blocking` argument. A blocking error makes `Validate()` and `SubmitAsync()` fail until it is cleared, without calling your service again. Use it when you set the error outside of the submit pipeline, for example from `OnBeforeNext` in a wizard. For an error set inside `OnBeforeSubmit`, which already returns `false`, the default is enough.

## Check a value against a service while the user types

Goal: tell the user that a username is taken as soon as they leave the field, without calling the service on every key and without letting a stale answer through.

```csharp
using Klyroo.Form;

new FormFieldSchema
{
    Key = "username",
    Label = "Username",
    Required = true,
    AsyncValidators =
    [
        new FormAsyncValidatorRule("free", async (value, form, ct) =>
            await accounts.IsUsernameTakenAsync((string)value!, ct) ? "This username is taken" : null)
        {
            Debounce = TimeSpan.FromMilliseconds(400),
        },
    ],
}
```

The rule returns the error message, or `null` when the value is valid. It runs after the synchronous checks pass and only for a value that is not empty. Once the field is touched (it loses focus, or `Trigger = Input` and it changes), a run starts when the value has stayed unchanged for `Debounce`. A newer value cancels the older run through the token and its answer is ignored: the error is only ever the answer for the value on screen. While a run is waiting or running, `form.IsValidating("username")` is `true`, and the components show a spinner and "Checking..." under the field and set `aria-busy` on it.

`SubmitAsync()` and `NextStepAsync()` wait for the rules of the fields they validate, keep `IsSubmitting` set meanwhile, and fail if a rule returns a message. A run that already called the service with the current value is joined, not repeated, and an answer that is already known is reused. `await form.ValidateAsync()` does the same without submitting; the synchronous `Validate()` only uses the answers that are already known.

If the service is unreachable during a background run, the exception is swallowed and the field simply stays unchecked: the user can still fix other fields, and `SubmitAsync()` runs the rule again and lets the exception reach your code, like any other hook that throws. A rule does not apply to the cells of a repeatable list. In a host without a synchronization context (a console app, a test), background runs finish on a thread-pool thread, and `Changed` is raised there.

## Save from the component and keep the form busy

Goal: call your API when the user submits, show a saving state while it runs, keep the edits when it fails and report the failure.

```razor
<KlyFormEngine Form="_form" OnSave="SaveAsync" OnSubmit="Saved" />

@code {
    private readonly FormController _form = new(Schemas.Profile);

    private async Task<bool> SaveAsync(IReadOnlyDictionary<string, object?> data, CancellationToken ct)
    {
        var result = await profiles.SaveAsync(data, ct);
        if (result.Conflict)
        {
            _form.SetFormError("Someone else changed this profile. Reload it and try again.");
            return false;
        }
        return true;
    }

    private void Saved(IReadOnlyDictionary<string, object?> data) => nav.NavigateTo("/profile");
}
```

`OnSave` runs inside `FormController.SubmitAsync`, after validation and the schema hooks, while `IsSubmitting` is still `true`: the button shows its saving state, a second submit is refused, and Back, Reset and Cancel are disabled. When it returns `true` the dirty snapshot is retaken, so `IsDirty` turns `false` and `OnSubmit` is raised. When it returns `false`, or throws, the snapshot is kept: the edits stay dirty and the user can fix and resubmit. The token is cancelled when the component is disposed.

`OnSubmit` on its own runs after the submission is already over (`IsSubmitting` is `false`, the form is clean), so it is the place to navigate, not to save. Without the component, the same lifecycle is `await form.SubmitAsync(async (f, ct) => (await api.SaveAsync(f.BuildOutput(), ct)).Ok, ct)`.

`SetFormError` reports a failure that belongs to no field. It is a blocking error on the key `FormController.FormErrorKey`, listed in the error summary at the top of the form, and it keeps `Validate()` failing until you call `ClearError()` (or `Reset`). Any `SetError` on a key that matches no field behaves the same way. After a failed submit or Next, the summary lists every field error as a button that focuses the field, a polite live region announces the count and the first error, and focus moves to the first invalid field (or to the summary when only form-level errors remain).

## Show and require fields conditionally

Goal: show a field only for some answers, and make another required depending on a third.

```csharp
using Klyroo.Form;

var schema = new FormSchema
{
    FormKey = "billing",
    Fields =
    [
        new FormFieldSchema
        {
            Key = "accountType", Label = "Account type", Kind = FormFieldKind.Select, DefaultValue = "personal",
            Options = [new("personal", "Personal"), new("business", "Business")],
        },
        new FormFieldSchema
        {
            Key = "company", Label = "Company", Required = true,
            Condition = form => form.GetValue<string>("accountType") == "business",
        },
        new FormFieldSchema { Key = "country", Label = "Country", DefaultValue = "FR" },
        new FormFieldSchema
        {
            Key = "vat", Label = "VAT number",
            Condition = form => form.GetValue<string>("accountType") == "business",
            RequiredWhen = form => form.GetValue<string>("country") != "US",
            TransformOutput = value => value?.ToString()?.Replace(" ", "").ToUpperInvariant(),
        },
    ],
};
```

A field whose `Condition` returns `false` is not rendered, is skipped by validation and is left out of `BuildOutput()`. Its value stays in the state, so it comes back if the user changes their mind. `RequiredWhen` is evaluated on each validation, in addition to `Required`.

`TransformOutput` changes the value only in the output, here to normalize a VAT number. The state keeps what the user typed, so the field does not change under their cursor.

A `Condition` on a container (`Group`, `Card`, `Object` or `Tabs`) hides its children too, with the same effect: they are not validated and not output, and their values stay in the state. So `company` and `vat` above could share a single condition on a `Group` that holds both.

## Build a multi-step wizard

Goal: split a long form into steps, skip one that does not apply, and run an async check before moving on.

```razor
@page "/onboarding"
@rendermode InteractiveServer

<KlyFormEngine Schema="_schema" SubmitLabel="Finish" OnSubmit="Finish" />

@code {
    private readonly FormSchema _schema = new()
    {
        FormKey = "onboarding",
        Title = "Create your workspace",
        OnBeforeNext = async (form, stepIndex) =>
        {
            if (stepIndex != 0) return true;
            var taken = await WorkspaceDirectory.IsTakenAsync(form.GetValue<string>("slug")!);
            if (taken) form.SetError("slug", "This address is taken", blocking: true);
            return !taken;
        },
        Steps =
        [
            new FormStepSchema
            {
                Key = "workspace", Title = "Workspace",
                Fields =
                [
                    new FormFieldSchema { Key = "slug", Label = "Address", Required = true },
                    new FormFieldSchema { Key = "plan", Label = "Plan", Kind = FormFieldKind.Radio, DefaultValue = "free",
                        Options = [new("free", "Free"), new("team", "Team")] },
                ],
            },
            new FormStepSchema
            {
                Key = "billing", Title = "Billing",
                SkipWhen = form => form.GetValue<string>("plan") == "free",
                Fields =
                [
                    new FormFieldSchema { Key = "vat", Label = "VAT number", Required = true },
                ],
            },
            new FormStepSchema
            {
                Key = "invite", Title = "Invite",
                Fields = [new FormFieldSchema { Key = "emails", Label = "Teammates", Kind = FormFieldKind.Textarea }],
            },
        ],
    };

    private Task Finish(IReadOnlyDictionary<string, object?> data)
    {
        Console.WriteLine($"Created {data["slug"]}");
        return Task.CompletedTask;
    }
}
```

`Steps` replaces `Fields` and `Sections`. The controller validates only the current step when the user presses Next, then runs `OnBeforeNext` with the index of the step being left. Returning `false` keeps the user on the step. The step with `SkipWhen` returning `true` is passed over in both directions, and the stepper does not list it. If the steps after the current one are all skipped, the current step is the last one: the engine shows the submit button there instead of Next. A form whose first step is skipped starts on the next one.

`SubmitAsync()` validates the fields of the steps that are not skipped, and `BuildOutput()` leaves the skipped ones out. A field in a skippable step can therefore be `Required`, as `vat` is: a user on the free plan never sees the billing step and is never asked for a VAT number.

The error in `OnBeforeNext` is blocking: pressing Next again fails validation straight away, without another call to the directory, until the user edits `slug`.

## Build a settings page with sections

Goal: show every group of settings on one page, with a completion status per section and a guard against leaving with unsaved changes.

```razor
@page "/settings"
@rendermode InteractiveServer

<KlyFormNavigationGuard Form="_form" />
<KlyFormPage Form="_form" OnSave="SaveAsync" />

@code {
    private readonly FormController _form = new(
        new FormSchema
        {
            FormKey = "settings",
            Title = "Workspace settings",
            Sections =
            [
                new FormPageSectionSchema
                {
                    Key = "general", Label = "General",
                    Fields =
                    [
                        new FormFieldSchema { Key = "name", Label = "Workspace name", Required = true },
                        new FormFieldSchema { Key = "domain", Label = "Domain", Required = true },
                    ],
                },
                new FormPageSectionSchema
                {
                    Key = "notes", Label = "Notes", Optional = true,
                    Fields = [new FormFieldSchema { Key = "description", Label = "Description" }],
                },
            ],
        },
        new Dictionary<string, object?> { ["name"] = "Acme", ["domain"] = "acme.example" });

    private async Task<bool> SaveAsync(IReadOnlyDictionary<string, object?> data, CancellationToken ct)
    {
        await workspaces.SaveAsync(data, ct);
        return true;
    }
}
```

`KlyFormPage` takes a controller, not a schema, because it needs the controller to compute section states. It lists the sections in a sidebar with a status: `Complete`, `Pending` (a required field is empty), `Invalid` (a field has an error) or `Optional` (the section is marked optional and empty). Each section has its own Reset button once one of its fields has changed.

The controller is created with initial input, so the form starts clean and `IsDirty` turns `true` on the first edit. `KlyFormNavigationGuard` is a `NavigationLock` that follows the controller. It subscribes to `Changed` and re-renders itself when `IsDirty` flips, which a `NavigationLock` placed on the page cannot do: a page that renders the form through `KlyFormPage` never re-renders when the user types, so `ConfirmExternalNavigation="@_form.IsDirty"` on the page stays at the value it had on the first render and the browser prompt never switches on. While the form is dirty the guard asks the browser to confirm leaving the site or closing the tab, and it asks `window.confirm` before in-app navigation, or calls your `ConfirmLeave` (return `true` to leave) if you prefer your own dialog. Because `OnSave` returning `true` retakes the baseline, the guard lets the page navigate after a successful save.

To keep unsaved edits across a reload, store a draft and restore it on the new controller:

```csharp
// while the user edits, for example on form.Changed
await storage.SetAsync("settings-draft", _form.ExportState().ToJson());

// when the page loads, with the controller built from the saved data as before
if (await storage.GetAsync("settings-draft") is { } json)
{
    _form.RestoreDraft(FormDraft.FromJson(json));
}
```

`RestoreDraft` puts the values back without changing the baseline, so the restored fields count as unsaved changes (`IsDirty` is `true` and the guard protects them). `Reset(input)` would have made the draft the new baseline and the form clean. `ExportState()` leaves password and one-time code fields out unless you pass `includeSensitive: true`, because a draft usually lives on the user's device.

`KlyFormPage` renders fields exactly like `KlyFormEngine`: the same controls per kind, labels, containers, tabs and repeatable lists. The page is a `form` element, so pressing Enter in a text field submits it. `FormSchema.Columns` sets the number of columns of the field grids, and `FormPageSectionSchema.Span` lets two sections share a row on large screens (`Span = 6` each).

## Open a form in a dialog from anywhere

Goal: ask the user for a few values from code, wherever you are, and await the answer.

Mount one `KlyFormProvider` in the layout:

```razor
@inherits LayoutComponentBase

<KlyFormProvider>
    @Body
</KlyFormProvider>
```

Then open a form from any component in the same scope:

```razor
@page "/projects"
@rendermode InteractiveServer
@inject FormService Forms

<button type="button" @onclick="Create">New project</button>

<ul>
    @foreach (var name in _projects)
    {
        <li>@name</li>
    }
</ul>

@code {
    private readonly List<string> _projects = [];

    private static readonly FormSchema NewProject = new()
    {
        FormKey = "new-project",
        Title = "New project",
        Fields = [new FormFieldSchema { Key = "name", Label = "Name", Required = true }],
    };

    private async Task Create()
    {
        var outcome = await Forms.CreateFormAsync(NewProject, mode: FormShellMode.Modal);
        if (outcome.IsCompleted)
        {
            _projects.Add((string)outcome.FormData["name"]!);
        }
    }
}
```

`CreateFormAsync` shows the form in the provider and returns a task. It completes with `IsCompleted` set to `true` and the output after a successful submit, or with `false` if the user cancels. `FormShellMode.Modal` is the default and renders a centered dialog; `Drawer` renders a full-height panel docked to the end of the viewport, and `Fullscreen` covers the whole viewport. The overlay carries the mode in a `data-mode` attribute. It is a native modal dialog from Klyroo.UI (`KlyModal`, `KlySlideover` for `Drawer`), so focus stays inside it, Escape and a click on the backdrop close the form (the task completes with `IsCompleted = false`), and focus returns to the element that opened the form. While the form is saving it cannot be dismissed.

Do the work after the `await`, as above. The schema hooks `OnBeforeSubmit` and `OnSubmit` run once per submit. Calling `CreateFormAsync` while another form is open replaces it, and the replaced form's task completes with `IsCompleted` set to `false`, as if the user had closed it.

## Edit repeatable rows

Goal: let the user add and remove rows, such as invoice lines, and validate each row.

```csharp
using Klyroo.Form;

var lines = new FormFieldSchema
{
    Key = "lines",
    Label = "Lines",
    Kind = FormFieldKind.ArrayTable,
    Required = true,
    AddItemLabel = "Add line",
    Fields =
    [
        new FormFieldSchema { Key = "sku", Label = "SKU", Required = true, MaxLength = 12 },
        new FormFieldSchema { Key = "qty", Label = "Quantity", Kind = FormFieldKind.Number, Min = 1, DefaultValue = 1.0 },
    ],
    Validators =
    [
        new FormValidatorRule("unique", "Each SKU can appear once", (value, _) =>
            value is List<Dictionary<string, object?>> rows
            && rows.Select(row => row.GetValueOrDefault("sku")?.ToString()).Distinct().Count() == rows.Count),
    ],
};

var schema = new FormSchema { FormKey = "invoice", Fields = [lines] };
```

`ArrayList` and `ArrayTable` hold a `List<Dictionary<string, object?>>`, one dictionary per row, keyed by the child field keys. `KlyFormEngine` and `KlyFormPage` show an add button (labelled by `AddItemLabel`), the control of each child's kind per cell and a remove button per row. The two kinds are rendered identically. In code, use `AddArrayItem`, `SetArrayItemValue`, `RemoveArrayItem` and `GetArrayItems`.

Each cell is validated with the rules of its child field: `Required`, `Min`, `Max`, `MaxLength`, password requirements and `Validators`. `Validate()` and a submit check every cell, and a cell the user edited is checked as it changes, following the child's `Trigger`. The error shows under the cell, and `GetArrayItemError("lines", 0, "sku")` or `GetError("lines[0].sku")` reads it. A rule that spans rows, like `unique` above, goes on the list field, which receives the whole list. A `Number` cell holds a `double`, like a `Number` field. `Required` on the list itself requires at least one row, `Disabled` disables the cells and the buttons, a child's `Condition` hides its column, and a child's `Watch` runs with the cell value when the cell changes.

## Prefill and reset a form

Goal: load existing data into a form, and restore it on demand.

```razor
@page "/controlled"
@rendermode InteractiveServer

<KlyFormEngine Form="_form" ShowReset="false" />

<button type="button" @onclick="Prefill" disabled="@_form.IsSubmitting">Use my profile</button>

@code {
    private readonly FormController _form = new(new FormSchema
    {
        FormKey = "profile",
        Fields =
        [
            new FormFieldSchema { Key = "name", Label = "Name" },
            new FormFieldSchema { Key = "email", Label = "Email" },
        ],
    });

    private void Prefill() => _form.Reset(new Dictionary<string, object?>
    {
        ["name"] = "Alice",
        ["email"] = "alice@example.com",
    });
}
```

Pass a `FormController` to `KlyFormEngine` through `Form` when you need to read or change the form from code. `Reset(newInput)` clears errors and touched state, returns to the first step, loads the values and makes them the new baseline, so the form is not dirty afterwards. `Reset()` without input returns to the defaults. To restore a single field use `ResetField(key)`, and for a section `ResetSection(key)`; both go back to the baseline, not to the defaults.

Keys of the input that match no field are kept in `InternalState` but never appear in the output.

## Add your own control

Goal: use a control the library does not have, such as a star picker or a map, as a field that is validated, dirty-tracked and part of the output like any other.

```razor
@* StarPicker.razor: a Razor component that edits one field through its FormControlContext *@
@using Klyroo.Form

<div id="@Context.Id" role="radiogroup" aria-labelledby="@Context.LabelledBy" aria-describedby="@Context.DescribedBy"
     aria-invalid="@(Context.Error is null ? null : "true")">
    @for (var star = 1; star <= 5; star++)
    {
        var value = star;
        <button type="button" disabled="@Context.Disabled" aria-pressed="@(Context.Current<int>() >= value)"
                @onclick="@(() => Context.Commit(value))" @onblur="Context.Blur">★</button>
    }
</div>

@code {
    [Parameter, EditorRequired] public FormControlContext Context { get; set; } = default!;
}
```

```csharp
// Program.cs, once for the app: the key is the one the schema names
builder.Services.AddKlyrooForm(f => f.CustomField("stars", ctx => builder =>
{
    builder.OpenComponent<StarPicker>(0);
    builder.AddComponentParameter(1, nameof(StarPicker.Context), ctx);
    builder.CloseComponent();
}));

// the schema
new FormFieldSchema { Key = "rating", Kind = FormFieldKind.Custom, CustomKey = "stars", Label = "How was it?", Required = true }
```

A custom field is a normal stateful field: it holds whatever value you store (a string, a number, a list), is checked by `Required` and `Validators`, and is in `BuildOutput()`. The component draws the label, description, help, error and the reset button around your control, so the renderer only draws the control. `FormControlContext` gives it what the built-in controls use: `Id` (put it on the control so the label points at it), `Value` and `Current()`, `Set(value)` while the user edits, `Commit(value)` for a complete change that should show its error at once, `Blur()` when focus leaves, `Error`, `Disabled`, `Required`, `Validating`, and `DescribedBy` and `LabelledBy` for the ARIA attributes. For a control made of several inputs, put `role="group"` or `radiogroup` on its root and point `aria-labelledby` at `Context.LabelledBy`, or at the label id yourself.

To register the renderer for one form instead of the whole app, pass it in the `FieldTemplates` parameter of `KlyFormEngine`, `KlyFormPage` or `KlySchemaField`: a `Dictionary<string, RenderFragment<FormControlContext>>` that wins over the registry and reaches fields inside containers, tabs and the cells of repeatable lists. A custom field whose key has no renderer shows a notice instead of a control. Because `FormControlContext` has a public constructor, you can also build one in a unit test: `new FormControlContext(form, field, "id")` then call `Set`, `Commit` and `Blur` and read the controller.

## Upload files

Goal: let the user attach files, reject the wrong ones before they reach your server, and read the content when saving.

```csharp
new FormFieldSchema
{
    Key = "attachments",
    Kind = FormFieldKind.File,
    Label = "Attachments",
    Multiple = true,
    Accept = ".pdf,image/*",
    MaxFileSize = 5 * 1024 * 1024,
}
```

The control is a Blazor `InputFile` behind a button, with the chosen files listed under it and a remove button on each. The value is a `FormFileValue` (or `null`) for a single file, or a `List<FormFileValue>` with `Multiple`: the name, size and content type the browser reported, and `OpenReadAsync()` to open the content. A `FormFileValue` has no UI type in it, so the same field works in a unit test (`FormFileValue.FromBytes("a.pdf", bytes, "application/pdf")`) and in the output you hand to your service:

```csharp
OnSave = async (data, ct) =>
{
    foreach (var file in (List<FormFileValue>?)data["attachments"] ?? [])
    {
        await using var stream = await file.OpenReadAsync(ct);
        await storage.UploadAsync(file.Name, stream, ct);
    }
    return true;
}
```

`Accept` is the `accept` attribute of the input, which only filters the picker, so the controller checks it again on validation (`.png` matches the extension, `image/png` the content type, `image/*` a family, ignoring case), together with `MaxFileSize` for each file and the number of files for a field without `Multiple`. The messages name the file and the limit (`report.pdf is larger than 5 MB`) and follow the locale. A rejected file stays in the list so the user sees which one it was, and the form cannot be submitted until it is removed. The size and type come from the browser: they are for the user's benefit, so check them again on the server. Files are not part of `ExportState()` drafts, since their content cannot be restored.

In Blazor Server the content streams over the circuit when you read it, and `InputFile` reads from the browser only then, so opening a file you do not need costs nothing. `OpenReadAsync` can be called several times, and each call returns a new stream.

## Test your forms

Goal: test form logic without a browser, and the rendering with bUnit.

Because `FormController` has no UI dependency, most tests are plain xUnit tests. Keep each schema in a factory method that takes its dependencies as arguments, so a page and its tests build the same form:

```csharp
using Klyroo.Form;

public static class SignupForm
{
    public static FormSchema Create(Func<string, Task<bool>> isEmailTaken) => new()
    {
        FormKey = "signup",
        Fields = [new FormFieldSchema { Key = "email", Label = "Email", Required = true }],
        OnBeforeSubmit = async form =>
        {
            if (await isEmailTaken(form.GetValue<string>("email")!))
            {
                form.SetError("email", "This email is already registered");
                return false;
            }
            return true;
        },
    };
}
```

Then drive the controller directly:

```csharp
using Klyroo.Form;
using Xunit;

public sealed class SignupFormTests
{
    [Fact]
    public async Task A_taken_email_cancels_the_submit_and_shows_an_error()
    {
        var form = new FormController(SignupForm.Create(_ => Task.FromResult(true)));
        form.SetValue("email", "alice@example.com");

        Assert.False(await form.SubmitAsync());
        Assert.Equal("This email is already registered", form.GetError("email"));
        Assert.True(form.IsDirty);
    }

    [Fact]
    public async Task A_free_email_submits_and_leaves_the_form_clean()
    {
        var form = new FormController(SignupForm.Create(_ => Task.FromResult(false)));
        form.SetValue("email", "alice@example.com");

        Assert.True(await form.SubmitAsync());
        Assert.False(form.IsDirty);
        Assert.Equal("alice@example.com", form.BuildOutput()["email"]);
    }
}
```

To test rendering, add `xunit.v3` and `bunit` (version 2 or later, which uses `BunitContext`) to a test project that references `Klyroo.Form`. Register the services in the constructor, render the components and query the `data-slot` attributes they expose, such as `form-field-error` and `form-title`, instead of CSS classes:

```csharp
using Bunit;
using Klyroo.Form;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class SignupFormRenderingTests : BunitContext
{
    public SignupFormRenderingTests()
    {
        Services.AddKlyrooForm();
    }

    [Fact]
    public void Submitting_an_empty_form_shows_the_required_error()
    {
        var cut = Render<KlyFormEngine>(parameters => parameters
            .Add(p => p.Schema, SignupForm.Create(_ => Task.FromResult(false))));

        cut.Find("form").Submit();

        Assert.Equal("This field is required", cut.Find("[data-slot=form-field-error]").TextContent);
    }

    [Fact]
    public async Task The_overlay_task_completes_with_the_output()
    {
        var provider = Render<KlyFormProvider>();
        var forms = Services.GetRequiredService<FormService>();

        var pending = forms.CreateFormAsync(
            SignupForm.Create(_ => Task.FromResult(false)),
            new Dictionary<string, object?> { ["email"] = "alice@example.com" });
        provider.Render();

        provider.Find("form").Submit();
        var outcome = await pending;

        Assert.True(outcome.IsCompleted);
        Assert.Equal("alice@example.com", outcome.FormData["email"]);
    }
}
```

Submitting the `form` element runs the same path as the submit button. The overlay test completes because submitting the form in the provider resolves the task returned by `CreateFormAsync`.
