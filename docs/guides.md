# Guides

Task-oriented recipes. Each one states a goal, shows complete code and explains the parts that are not obvious. The Blazor samples assume the setup from [Getting started](getting-started.md): `AddKlyrooForm()` is registered, `@using Klyroo.Form` and `@using Klyroo.Form.Dynamic` are in `_Imports.razor` and the stylesheets are loaded. Except for [Bind a C# model](#bind-a-c-model), which is the typed mode, the recipes use the runtime mode (`Klyroo.Form.Dynamic`), where a form is data and values are keyed by name. Names such as `accounts` and `WorkspaceDirectory` stand for your own services.

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
- [Bind a C# model](#bind-a-c-model)
- [Add your own control](#add-your-own-control)
- [Upload files](#upload-files)
- [Test your forms](#test-your-forms)

## Validate a field with custom rules

Goal: reject a value that passes the built-in checks, and choose when the error appears.

```csharp
using Klyroo.Form;
using Klyroo.Form.Dynamic;

var schema = new DynamicFormSchema
{
    FormKey = "signup",
    Fields =
    [
        new DynamicFieldSchema
        {
            Key = "username",
            Label = "Username",
            Required = true,
            Trigger = FormValidationTrigger.Input,
            Validators =
            [
                new DynamicValidatorRule("length", "Use 3 to 20 characters",
                    (value, _) => value?.ToString() is { Length: >= 3 and <= 20 }),
                new DynamicValidatorRule("charset", "Use letters, digits and dashes only",
                    (value, _) => value?.ToString()?.All(c => char.IsLetterOrDigit(c) || c == '-') == true),
            ],
        },
        new DynamicFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number, Min = 18, Max = 120 },
        new DynamicFieldSchema
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
        new DynamicFieldSchema
        {
            Key = "terms",
            Label = "I accept the terms",
            Kind = FormFieldKind.Checkbox,
            Validators = [new DynamicValidatorRule("accepted", "You must accept the terms", (value, _) => value is true)],
        },
    ],
};
```

`Required`, number bounds (`Min` and `Max` on `FormFieldKind.Number`), `MaxLength`, password requirements and `Validators` run in that order on each validation, and the first failure becomes the error. For a field that is empty and required, only the required message is reported.

`Validators` also run when the value is empty and the field is not required, so a rule such as `terms` above sees `null` until the user toggles the checkbox. Write rules that accept `null`, or reject it on purpose, as `terms` does. A `Required` checkbox or switch must be checked, so `Required = true` is enough for a consent box; a validator like `terms` is only needed for a custom message or rule.

`Trigger` decides when a field is validated while the user edits it. `Blur` (the default) starts validating when the control first loses focus and then validates on every change, `Input` validates from the first change, and `Submit` waits for `Validate()` or a submit. Errors only show for touched fields, and `Validate()` and `SubmitAsync()` touch every field they check.

Rules that read another field, such as a confirmation that compares itself with the password, stay correct while the user edits the other field. After every change the controller validates again the touched fields that currently show an error, so a stale "Passwords differ" disappears when the password is changed to match. It cannot guess that an untouched-looking valid field became invalid, so name the fields your rules read with `DependsOn`:

```csharp
new DynamicFieldSchema
{
    Key = "confirm",
    Kind = FormFieldKind.Password,
    DependsOn = ["password"],
    Validators = [new DynamicValidatorRule("match", "Passwords differ",
        (value, form) => Equals(value, form.GetValue("password")))],
}
```

With `DependsOn`, a change of `password` validates `confirm` again once the user has touched it, so the mismatch appears and clears as soon as the other field changes. A field the user has not touched yet stays quiet, and a field with `Trigger = Submit` still waits for submission. `RequiredWhen` follows the same rule: its error disappears when the condition stops holding, and appears on a touched field when you list the field the condition reads in `DependsOn`.

## Report a server error on a field

Goal: check a value against a service on submit and show the result on the field.

```csharp
using Klyroo.Form;
using Klyroo.Form.Dynamic;

// accounts is your own service
var schema = new DynamicFormSchema
{
    FormKey = "register",
    Fields = [new DynamicFieldSchema { Key = "email", Label = "Email", Required = true }],
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
using Klyroo.Form.Dynamic;

new DynamicFieldSchema
{
    Key = "username",
    Label = "Username",
    Required = true,
    AsyncValidators =
    [
        new DynamicAsyncValidatorRule("free", async (value, form, ct) =>
            await accounts.IsUsernameTakenAsync((string)value!, ct) ? "This username is taken" : null)
        {
            Debounce = TimeSpan.FromMilliseconds(400),
        },
    ],
}
```

The rule returns the error message, or `null` when the value is valid. It runs after the synchronous checks pass and only for a value that is not empty. Once the field is touched (it loses focus, or `Trigger = Input` and it changes), a run starts when the value has stayed unchanged for `Debounce`. A newer value cancels the older run through the token and its answer is ignored: the error is only ever the answer for the value on screen. While a run is waiting or running, `form.IsValidating("username")` is `true`, and the components show a spinner and "Checking..." under the field and set `aria-busy` on it.

`SubmitAsync()` and `NextStepAsync()` wait for the rules of the fields they validate, keep `IsSubmitting` set meanwhile, and fail if a rule returns a message. A run that already called the service with the current value is joined, not repeated, and an answer that is already known is reused. `await form.ValidateAsync()` does the same without submitting; the synchronous `Validate()` only uses the answers that are already known.

If the service is unreachable during a background run, the exception is swallowed and the field simply stays unchecked: the user can still fix other fields, and `SubmitAsync()` runs the rule again and lets the exception reach your code, like any other hook that throws. A value that is edited while its rule runs is checked again, and a submit never succeeds with a value no rule has seen: if results keep going stale, `SubmitAsync()` returns `false`. An edit that replaces a running rule cancels it silently, never as an `OperationCanceledException` from `SubmitAsync()` or `ValidateAsync()`. Dispose the controller (`DynamicFormController` is `IDisposable`) when you are done with it, which cancels the runs still pending; the components do it for the controller they create, and `DynamicFormService` for the form it closes. A rule does not apply to the cells of a repeatable list. In a host without a synchronization context (a console app, a test), background runs finish on a thread-pool thread, and `Changed` is raised there.

## Save from the component and keep the form busy

Goal: call your API when the user submits, show a saving state while it runs, keep the edits when it fails and report the failure.

```razor
<KlyDynamicFormEngine Form="_form" OnSave="SaveAsync" OnSubmit="Saved" />

@code {
    private readonly DynamicFormController _form = new(Schemas.Profile);

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

`OnSave` runs inside `DynamicFormController.SubmitAsync`, after validation and the schema hooks, while `IsSubmitting` is still `true`: the button shows its saving state, a second submit is refused, and Back, Reset and Cancel are disabled. When it returns `true` the dirty snapshot is retaken, so `IsDirty` turns `false` and `OnSubmit` is raised. When it returns `false`, or throws, the snapshot is kept: the edits stay dirty and the user can fix and resubmit. The token is cancelled when the component is disposed.

While the form submits, every field is disabled (`IsFieldDisabled` reports `true` for all of them while `IsSubmitting` is `true`), so the user cannot change values that are being validated or saved. If `OnSave`, an asynchronous rule, a hook or `OnSubmit` throws, the component logs the exception through `ILogger` and shows a form-level notice ("Something went wrong. Try again.", in the locale) instead of letting it end the Blazor Server circuit; `IsSubmitting` resets, the edits stay, and the notice goes away with the next attempt. After a successful save, the baseline is the set of values the handler received: an edit made from code while it ran stays dirty.

`OnSubmit` on its own runs after the submission is already over (`IsSubmitting` is `false`, the form is clean), so it is the place to navigate, not to save. Without the component, the same lifecycle is `await form.SubmitAsync(async (f, ct) => (await api.SaveAsync(f.BuildOutput(), ct)).Ok, ct)`.

`SetFormError` reports a failure that belongs to no field. It is a blocking error on the key `DynamicFormController.FormErrorKey`, listed in the error summary at the top of the form, and it keeps `Validate()` failing until you call `ClearError()` (or `Reset`). Any `SetError` on a key that matches no field behaves the same way. After a failed submit or Next, the summary lists every field error as a button that focuses the field, a polite live region announces the count and the first error, and focus moves to the first invalid field (or to the summary when only form-level errors remain).

## Show and require fields conditionally

Goal: show a field only for some answers, and make another required depending on a third.

```csharp
using Klyroo.Form;
using Klyroo.Form.Dynamic;

var schema = new DynamicFormSchema
{
    FormKey = "billing",
    Fields =
    [
        new DynamicFieldSchema
        {
            Key = "accountType", Label = "Account type", Kind = FormFieldKind.Select, DefaultValue = "personal",
            Options = [new("personal", "Personal"), new("business", "Business")],
        },
        new DynamicFieldSchema
        {
            Key = "company", Label = "Company", Required = true,
            Condition = form => form.GetValue<string>("accountType") == "business",
        },
        new DynamicFieldSchema { Key = "country", Label = "Country", DefaultValue = "FR" },
        new DynamicFieldSchema
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

<KlyDynamicFormEngine Schema="_schema" SubmitLabel="Finish" OnSubmit="Finish" />

@code {
    private readonly DynamicFormSchema _schema = new()
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
            new DynamicStepSchema
            {
                Key = "workspace", Title = "Workspace",
                Fields =
                [
                    new DynamicFieldSchema { Key = "slug", Label = "Address", Required = true },
                    new DynamicFieldSchema { Key = "plan", Label = "Plan", Kind = FormFieldKind.Radio, DefaultValue = "free",
                        Options = [new("free", "Free"), new("team", "Team")] },
                ],
            },
            new DynamicStepSchema
            {
                Key = "billing", Title = "Billing",
                SkipWhen = form => form.GetValue<string>("plan") == "free",
                Fields =
                [
                    new DynamicFieldSchema { Key = "vat", Label = "VAT number", Required = true },
                ],
            },
            new DynamicStepSchema
            {
                Key = "invite", Title = "Invite",
                Fields = [new DynamicFieldSchema { Key = "emails", Label = "Teammates", Kind = FormFieldKind.Textarea }],
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
<KlyDynamicFormPage Form="_form" OnSave="SaveAsync" />

@code {
    private readonly DynamicFormController _form = new(
        new DynamicFormSchema
        {
            FormKey = "settings",
            Title = "Workspace settings",
            Sections =
            [
                new DynamicSectionSchema
                {
                    Key = "general", Label = "General",
                    Fields =
                    [
                        new DynamicFieldSchema { Key = "name", Label = "Workspace name", Required = true },
                        new DynamicFieldSchema { Key = "domain", Label = "Domain", Required = true },
                    ],
                },
                new DynamicSectionSchema
                {
                    Key = "notes", Label = "Notes", Optional = true,
                    Fields = [new DynamicFieldSchema { Key = "description", Label = "Description" }],
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

`KlyDynamicFormPage` takes a controller, not a schema, because it needs the controller to compute section states. It lists the sections in a sidebar with a status: `Complete`, `Pending` (a required field is empty), `Invalid` (a field has an error) or `Optional` (the section is marked optional and empty). Each section has its own Reset button once one of its fields has changed.

The controller is created with initial input, so the form starts clean and `IsDirty` turns `true` on the first edit. `KlyFormNavigationGuard` is a `NavigationLock` that follows the controller. It subscribes to `Changed` and re-renders itself when `IsDirty` flips, which a `NavigationLock` placed on the page cannot do: a page that renders the form through `KlyDynamicFormPage` never re-renders when the user types, so `ConfirmExternalNavigation="@_form.IsDirty"` on the page stays at the value it had on the first render and the browser prompt never switches on. While the form is dirty the guard asks the browser to confirm leaving the site or closing the tab, and it asks `window.confirm` before in-app navigation, or calls your `ConfirmLeave` (return `true` to leave) if you prefer your own dialog. Because `OnSave` returning `true` retakes the baseline, the guard lets the page navigate after a successful save.

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

`RestoreDraft` puts the values back without changing the baseline, so the restored fields count as unsaved changes (`IsDirty` is `true` and the guard protects them). `Reset(input)` would have made the draft the new baseline and the form clean. `ExportState()` leaves password and one-time code fields, and those columns inside repeatable lists, out unless you pass `includeSensitive: true`, because a draft usually lives on the user's device.

`KlyDynamicFormPage` renders fields exactly like `KlyDynamicFormEngine`: the same controls per kind, labels, containers, tabs and repeatable lists. The page is a `form` element, so pressing Enter in a text field submits it. `DynamicFormSchema.Columns` sets the number of columns of the field grids, and `DynamicSectionSchema.Span` lets two sections share a row on large screens (`Span = 6` each).

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
@inject DynamicFormService Forms

<button type="button" @onclick="Create">New project</button>

<ul>
    @foreach (var name in _projects)
    {
        <li>@name</li>
    }
</ul>

@code {
    private readonly List<string> _projects = [];

    private static readonly DynamicFormSchema NewProject = new()
    {
        FormKey = "new-project",
        Title = "New project",
        Fields = [new DynamicFieldSchema { Key = "name", Label = "Name", Required = true }],
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
using Klyroo.Form.Dynamic;

var lines = new DynamicFieldSchema
{
    Key = "lines",
    Label = "Lines",
    Kind = FormFieldKind.ArrayTable,
    Required = true,
    AddItemLabel = "Add line",
    Fields =
    [
        new DynamicFieldSchema { Key = "sku", Label = "SKU", Required = true, MaxLength = 12 },
        new DynamicFieldSchema { Key = "qty", Label = "Quantity", Kind = FormFieldKind.Number, Min = 1, DefaultValue = 1.0 },
    ],
    Validators =
    [
        new DynamicValidatorRule("unique", "Each SKU can appear once", (value, _) =>
            value is List<Dictionary<string, object?>> rows
            && rows.Select(row => row.GetValueOrDefault("sku")?.ToString()).Distinct().Count() == rows.Count),
    ],
};

var schema = new DynamicFormSchema { FormKey = "invoice", Fields = [lines] };
```

`ArrayList` and `ArrayTable` hold a `List<Dictionary<string, object?>>`, one dictionary per row, keyed by the child field keys. `KlyDynamicFormEngine` and `KlyDynamicFormPage` show an add button (labelled by `AddItemLabel`), the control of each child's kind per cell and a remove button per row. The two kinds are rendered identically. In code, use `AddArrayItem`, `SetArrayItemValue`, `RemoveArrayItem` and `GetArrayItems`.

Each cell is validated with the rules of its child field: `Required`, `Min`, `Max`, `MaxLength`, password requirements and `Validators`. `Validate()` and a submit check every cell, and a cell the user edited is checked as it changes, following the child's `Trigger`. The error shows under the cell, and `GetArrayItemError("lines", 0, "sku")` or `GetError("lines[0].sku")` reads it. A rule that spans rows, like `unique` above, goes on the list field, which receives the whole list. A `Number` cell holds a `double`, like a `Number` field. `Required` on the list itself requires at least one row, `Disabled` disables the cells and the buttons, a child's `Condition` hides its column, and a child's `Watch` runs with the cell value when the cell changes.

## Prefill and reset a form

Goal: load existing data into a form, and restore it on demand.

```razor
@page "/controlled"
@rendermode InteractiveServer

<KlyDynamicFormEngine Form="_form" ShowReset="false" />

<button type="button" @onclick="Prefill" disabled="@_form.IsSubmitting">Use my profile</button>

@code {
    private readonly DynamicFormController _form = new(new DynamicFormSchema
    {
        FormKey = "profile",
        Fields =
        [
            new DynamicFieldSchema { Key = "name", Label = "Name" },
            new DynamicFieldSchema { Key = "email", Label = "Email" },
        ],
    });

    private void Prefill() => _form.Reset(new Dictionary<string, object?>
    {
        ["name"] = "Alice",
        ["email"] = "alice@example.com",
    });
}
```

Pass a `DynamicFormController` to `KlyDynamicFormEngine` through `Form` when you need to read or change the form from code. `Reset(newInput)` clears errors and touched state, returns to the first step, loads the values and makes them the new baseline, so the form is not dirty afterwards. `Reset()` without input returns to the defaults. To restore a single field use `ResetField(key)`, and for a section `ResetSection(key)`; both go back to the baseline, not to the defaults.

Keys of the input that match no field are kept in `InternalState` but never appear in the output.

## Bind a C# model

Goal: declare a form from a C# class, read and write its values as the types of the members, validate it with the attributes you already put on it, and receive the class back on submit.

This is the typed mode of Klyroo.Form: `FormSchema.For<TModel>` declares the form once, the builder returns a handle for each field, and a `FormController<TModel>` reads and writes values through those handles. It lives in `Klyroo.Form.Core`. The typed Blazor components arrive in the next change; the headless controller below works today, in a test, a view model or a console app.

```csharp
public sealed class Order
{
    public string Customer { get; set; } = "";
    public bool IsCompany { get; set; }
    public string? CompanyName { get; set; }
    public int Seats { get; set; } = 1;
    public Plan Plan { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderLine
{
    public string Product { get; set; } = "";
    public int Qty { get; set; } = 1;
}

public enum Plan { Free, Team, Business }

var schema = FormSchema.For<Order>(s =>
{
    s.Title = "New order";
    s.Field(o => o.Customer, f => f.Required().MaxLength(80));
    var company = s.Field(o => o.IsCompany, f => f.AsSwitch());
    s.Field(o => o.CompanyName, f => f.Required(c => c.Get(company)).VisibleWhen(c => c.Get(company)));
    s.Field(o => o.Seats, f => f.Range(1, 500));
    s.Field(o => o.Plan, f => f.AsRadio());
    s.List(o => o.Lines, row =>
    {
        row.Field(l => l.Product, f => f.Required());
        row.Field(l => l.Qty, f => f.Range(1, 999));
    }, addLabel: "Add line");
});
```

A field is a handle, not a string key. A later condition points at an earlier field through the captured local (`company` above), and code outside the builder gets a handle with `schema.Field(o => o.Seats)`. The handle of a list is `schema.List(o => o.Lines)`, and the handle of a multiple choice is `schema.MultiSelect(o => o.Roles)` (a field of `IReadOnlyList<TElement>`, which `schema.Field<IReadOnlyList<Role>>(o => o.Roles)` also finds). `schema.Field(o => o.Roles)` throws, because a `List<Role>` member is not a field of `List<Role>`, and its message names `MultiSelect` and `List`. `Required(...)`, `VisibleWhen(...)`, `DisabledWhen(...)`, `Default(...)`, `OnChange(...)` and `Rule(...)` take the controller or the value typed as the member, so there are no casts. Settings that only fit some kinds of value are extension methods on the builder, constrained on the type of the value: `AsTextarea()`, `AsPassword(...)`, `AsPhone()`, `AsColor()` and `MaxLength` exist for a string, `Range`, `StepBy` and `AsSlider()` for numbers, `AsRating()` for an integer, `AsSwitch()` for a boolean, `Options(...)`, `AsRadio()` and `OnCreateOption(...)` for any value, and `Accept(...)` and `MaxFileSize(...)` for files. A wrong kind does not compile.

Drive it with a controller:

```csharp
var form = new FormController<Order>(schema, new Order { Customer = "Ada" });

form.Set(schema.Field(o => o.IsCompany), true);
form.Set(schema.Field(o => o.CompanyName), "Analytical Engines");
var seats = form.Get(schema.Field(o => o.Seats));                // int

var saved = await form.SubmitAsync(async (order, ct) =>
{
    // order is an Order, written from the values of the form
    return await orders.SaveAsync(order, ct);
});
```

`Get` returns the value as its type (the default of the type while the field is empty or holds text that is not a value yet: `0` for an `int`, `null` for a string, and an empty list, never `null`, for a list or a multiple choice), and `TryGet` tells those cases apart: it returns `false` while the field is empty, which includes an empty list, or holds text that is not a value. `CreateModel()` builds a new model, `ApplyTo(target)` writes into an existing one, and `Reset(model)` loads another.

What happens when `SubmitAsync(save, target)` runs:

1. The field rules run, waiting for asynchronous ones.
2. The values are written into `target`, or into a new model when there is none, and every conversion is checked before the first member is set.
3. The model checks (`s.Validate((order, errors) => errors.Add(o => o.Seats, "..."))`, and the attributes below) run on that model, then `OnBeforeSubmit` and `OnSubmit`.
4. `save` runs inside the submit lifecycle, so `IsSubmitting` stays true while it works. When a step fails, or `save` returns `false` or throws, the members the write changed get their previous values again (a list holds its previous items and a row its previous cells) and the form stays dirty. Members the form did not write are not touched, and nothing your hooks or your save step did is undone: `OnBeforeSubmit`, `OnSubmit` (which runs before `save`, so its side effects stay when the save fails) and `save` itself are yours to compensate. On success the baseline is retaken.

### Data annotations

Install the extra package. It brings `Klyroo.Form.Core`:

```bash
dotnet add package Klyroo.Form.DataAnnotations --prerelease
```

```csharp
public sealed class Signup : IValidatableObject
{
    [Required, StringLength(80), Display(Name = "Full name", Prompt = "Ada Lovelace", Order = 1)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, Display(Order = 2)]
    public string Email { get; set; } = "";

    [Range(18, 120), Display(Order = 3)]
    public int Age { get; set; } = 18;

    public Plan Plan { get; set; } = Plan.Free;

    [DataType(DataType.Date), Display(Name = "Start date")]
    public DateOnly? StartDate { get; set; }

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "The passwords differ")]
    [DataType(DataType.Password), Display(Name = "Confirm password")]
    public string? ConfirmPassword { get; set; }

    public Address Address { get; set; } = new();

    public bool AcceptTerms { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!AcceptTerms)
            yield return new ValidationResult("You must accept the terms", [nameof(AcceptTerms)]);
    }
}

var schema = FormSchema.For<Signup>(s =>
{
    s.Title = "Create your account";
    s.Group(m => m.Address, g =>
    {
        g.Field(a => a.Street);
        g.Field(a => a.City);
    });
    s.FromDataAnnotations();
});
```

`FromDataAnnotations()` declares a field for every other member that has a default control and reads the attributes of all of them; `UseDataAnnotations()` only reads the attributes of the fields you declared. The attributes of a field are read by a convention, which runs after the defaults of the member type and before the settings you write in the `Field` call.

Which attribute becomes which rule:

| Attribute | Becomes | Message |
| --- | --- | --- |
| `[Required]` | `Required` of the engine | Localized by the engine, or the `ErrorMessage` you set (fixed when the form is built) |
| `[StringLength(max)]`, `[MaxLength]` on a string | `MaxLength` of the engine | Localized by the engine |
| `[StringLength(max, MinimumLength = n)]`, `n > 0` | `MaxLength` of the engine, and a validator rule that checks both bounds | Too long: the engine one. Too short: the English message of the attribute |
| `[StringLength]` or `[MaxLength]` with an `ErrorMessage` | A validator rule only, with no `MaxLength` on the input | The message of the attribute |
| `[Range(18, 65)]` or `[Range(0.5, 2.5)]` (int or double bounds, inclusive) on a `Number` field | `Min` and `Max` of the engine | Localized by the engine |
| `[Range]` with `typeof(decimal)` or text bounds, an exclusive bound, an `ErrorMessage`, or on a field that is not a `Number` | A validator rule | The message of the attribute |
| `[MinLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[CreditCard]` and the other built-in attributes | A validator rule | The message of the attribute |
| `[Compare]` | A validator rule, and the compared field in `DependsOn` | The message of the attribute |

Your own `ValidationAttribute`, attributes on the class and `IValidatableObject` run at submit on the model, after the field rules passed. Their errors go to the field of the member they name, or to the form-level error summary when the result names none.

The message of an attribute is English unless you give it an `ErrorMessage` or a resource (`ErrorMessageResourceType` and `ErrorMessageResourceName`). The messages of validator rules, and the `[Display]` name they put in them, are worked out when the rule fails, so a form built once and shared by several cultures shows the message of the culture of the validation (`CultureInfo.CurrentUICulture`). What is fixed when `FormSchema.For` runs is the label, description and placeholder from `[Display]` and the `[Required]` message: build the schema once per culture, or set them yourself, when those must change with the culture. The messages the engine produces follow the `Messages` of the controller, not the thread culture.

### Things to know

- **How the key and the kind are chosen.** The key of a field is the member name, and the names of the members down to it joined with dots for a nested one (`Address.City`). The kind comes from the member type: text for a string, `Number` for numbers (an integer must be whole and inside the range of its type, and a `decimal` or `float` inside its own), `Checkbox` for a `bool`, `Select` with the members as options for an enum, `Date`, `DateTime` and `Time` for the date and time types, text for a `Guid`, `Tag` for a list of strings, `DateRange` for a `FormDateRange` and `File` for a `FormFileValue` or a list of them. `[DataType]` can pick `Password`, `Textarea`, `PhoneNumber`, `Date` or `DateTime` instead.
- **Settings written in code win.** Defaults come first, conventions such as data annotations second and the `Field(...)` call last, so `s.Field(o => o.Plan, f => f.AsRadio().Label("Your plan"))` overrides both.
- **Groups, lists and multiple choice.** `s.Group(o => o.Address, g => ...)` lays out the fields of an object member and creates the object when a value is written. `s.List(o => o.Lines, row => ...)` edits a collection member as repeatable rows: the member may be a `List<T>`, an array, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>` or `IEnumerable<T>`, and the item type needs a parameterless constructor or an `itemFactory`. `controller.Rows(lines)` gives rows whose `Get(column)` reads a cell as its type, and `AddRow` and `RemoveRow` change the list. A row remembers the item it was loaded from: `ApplyTo(model)` writes the edited columns into that same item, so members that are not columns (an `Id`, a concurrency token) survive, creates a new item only for an added row, drops the items of removed rows, keeps the order and does not touch the collection when no row was added, removed or replaced. The collection is changed in place when it can be (a `List<T>`, a `Collection<T>`), through the setter for an array or a read-only list. Only an item the target holds now is written into, so `ApplyTo(other)` and `CreateModel()` create new items and never change the rows of the model the form was loaded from. After a successful `SubmitAsync(save, target)` the rows stand for the items the target now holds, so a form that stays open keeps what `save` set on them. A draft restored with `RestoreDraft` has rows that came from no item, so they are written as new items. `s.MultiSelect(o => o.Roles, options)` holds several chosen values as `FormOption<TElement>`. A chain such as `o => o.Address.City` is accepted for a field and is written only when `Group` declared the object on the way; a list or a multiple choice must be a direct member, so `s.List(o => o.Address.Lines, ...)` is refused when the schema is built, and the message points to `s.Group(o => o.Address, g => g.List(a => a.Lines, ...))`. A collection member with no setter (`public List<Line> Lines { get; } = new();`) is filled in place; one whose type cannot be changed in place (an array, `IReadOnlyList<T>`, `IEnumerable<T>`) is refused when the schema is built, and a collection that is read-only at run time (`new List<T>().AsReadOnly()`) makes `ApplyTo` throw before it changes anything.
- **Fields that are not in the model.** `s.Field<bool>("terms", f => f.Required())` declares a field the form has and the model does not, such as the acceptance of terms. It is read and written through its handle, validated and part of the draft, and never reaches the model.
- **Steps, sections and layout.** `s.Step("account", "Account", st => ..., skipWhen: c => ...)` and `s.Section("billing", "Billing", sec => ..., optional: true)` declare the blocks, and `Card`, `Tabs`, `Heading`, `Info`, `Divider` and `Button` lay a form out. Each returns a handle a field can depend on.
- **Hidden fields.** A field that `VisibleWhen` hides, or that sits in a skipped step, is not written: its member keeps the value it had.
- **Empty values.** An empty input gives `null` for a nullable member and for a nullable string (`string?`), an empty string for a string that is not nullable, and `default` for another value type. A value that is there but is not a value of the member type (text for a number, `9223372036854775808` for a `long`, `abc` in a date restored from a draft, free text written by a custom control) is never turned into `default`: validation reports it as an error on the field (`FormMessages.InvalidValue`, or the more specific number and identifier messages), whatever the validation mode, so `Validate` and `SubmitAsync` fail without throwing, and `ApplyTo` and `CreateModel` called directly throw an `InvalidOperationException` for it. Numbers, enums and dates are converted with the invariant culture whatever the culture of the thread, so a `decimal` round-trips under `fr-FR`.
- **Nullable strings under trimming.** Whether a string member is nullable comes from its nullable annotation, which is read at run time. A trimmed app can turn that metadata off (`System.Reflection.NullabilityInfoContext.IsSupported`, the `NullabilityInfoContextSupport` property of the project): the annotation is then unknown, and every string member gets an empty string for an empty input, because an empty string is valid for any string and a `null` is not. Say it per field with `f.NullWhenEmpty()` (`null`) or `f.NullWhenEmpty(false)` (empty string), which wins over the annotation.
- **Exact numbers.** The `Number` control stores a `double` when the number has up to 15 digits, which is what a user types, and a `decimal` when its digits would not fit, so `long.MaxValue`, `ulong.MaxValue`, a `decimal` of 28 digits and a `double` of 17 digits round-trip exactly, and an input shows every digit (a `float` shows `0.1`, not `0.10000000149011612`). `Range` bounds are doubles and cannot tell `long.MaxValue` from the next integer, so every integer member also gets exact rules for being whole and inside its type, and `decimal` and `float` members one for fitting their type, with `FormMessages.WholeNumber` and `FormMessages.NumberOutOfRange`.
- **Dates and times.** The control shows a `DateTime` to the millisecond, which is all a browser `datetime-local` input accepts. A `DateTime`, `DateTimeOffset`, `TimeOnly` or enum member that the user did not change keeps its value as it is when you write into the same model, with its ticks, its `DateTimeKind` and, for an enum, a number that no member has (`(Status)42`). A value the user typed takes the `DateTimeKind` of the value it replaces, and is `Unspecified` when there was none. A `DateTimeOffset` keeps the offset of the value it replaces, and a default one has offset zero (the text is read as UTC).
- **A write is all or nothing.** `ApplyTo` converts every value and checks every object on the way to a member (creating the ones declared with `Group`) before it sets the first member, so an error leaves the model as it was. If a setter of your model throws, the members already set are put back. With a `target`, `SubmitAsync` also puts the members back when a check, a hook or your save step fails or throws, so the model holds only what was saved. Only the members the form wrote are put back: what a hook or your save step changes itself, on the model or elsewhere, is its own business, and a setter that refuses to take its old value back is skipped.
- **A record without a parameterless constructor.** Tell the builder how to create one with `s.Factory(() => new Point(0, 0))`. The model is created for `CreateModel`, for the model checks and for a new controller without a model, which starts from the values of that empty model, so property initializers are the defaults of the form (a `Default(...)` on a field wins over them). A form with model checks (`Validate`, `UseDataAnnotations`, `FromDataAnnotations`) and no way to create the model throws an `InvalidOperationException` from `FormSchema.For`, instead of failing at every submit.
- **Not supported.** Flags enums, a struct as the model (`FormSchema.For<TModel>` requires a class: the form writes into the object it is given, and a struct would be copied on the way), members of struct type nested in the model, fields (only properties are bound), a collection that is not one of the shapes above (`HashSet<T>`), and attributes on an enum member (`[Display]` on the members of an enum: pass `Options` instead).

The schema does not compile expressions: `o => o.Address.City` is only read to find the members. Values are read and written through delegates created from the property accessors, and by reflection for a chain, an interface member or a collection. The model type is annotated so its public properties and constructor are kept when an app is trimmed. `Klyroo.Form.Core` is trim and AOT analyzer clean; `Klyroo.Form.DataAnnotations` is trim-correct and not declared AOT compatible, see [Platforms](platforms.md#nativeaot-and-trimming).

## Add your own control

Goal: use a control the library does not have, such as a star picker or a map, as a field that is validated, dirty-tracked and part of the output like any other.

```razor
@* StarPicker.razor: a Razor component that edits one field through its DynamicControlContext *@
@using Klyroo.Form
@using Klyroo.Form.Dynamic

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
    [Parameter, EditorRequired] public DynamicControlContext Context { get; set; } = default!;
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
new DynamicFieldSchema { Key = "rating", Kind = FormFieldKind.Custom, CustomKey = "stars", Label = "How was it?", Required = true }
```

A custom field is a normal stateful field: it holds whatever value you store (a string, a number, a list), is checked by `Required` and `Validators`, and is in `BuildOutput()`. The component draws the label, description, help, error and the reset button around your control, so the renderer only draws the control. `DynamicControlContext` gives it what the built-in controls use: `Id` (put it on the control so the label points at it), `Value` and `Current()`, `Set(value)` while the user edits, `Commit(value)` for a complete change that should show its error at once, `Blur()` when focus leaves, `Error`, `Disabled`, `Required`, `Validating`, and `DescribedBy` and `LabelledBy` for the ARIA attributes. For a control made of several inputs, put `role="group"` or `radiogroup` on its root and point `aria-labelledby` at `Context.LabelledBy`, or at the label id yourself.

To register the renderer for one form instead of the whole app, pass it in the `FieldTemplates` parameter of `KlyDynamicFormEngine`, `KlyDynamicFormPage` or `KlyDynamicFieldView`: a `Dictionary<string, RenderFragment<DynamicControlContext>>` that wins over the registry and reaches fields inside containers, tabs and the cells of repeatable lists. A custom field whose key has no renderer shows a notice instead of a control. Because `DynamicControlContext` has a public constructor, you can also build one in a unit test: `new DynamicControlContext(form, field, "id")` then call `Set`, `Commit` and `Blur` and read the controller.

## Upload files

Goal: let the user attach files, reject the wrong ones before they reach your server, and read the content when saving.

```csharp
new DynamicFieldSchema
{
    Key = "attachments",
    Kind = FormFieldKind.File,
    Label = "Attachments",
    Multiple = true,
    Accept = ".pdf,image/*",
    MaxFileSize = 5 * 1024 * 1024,
}
```

A chosen file is read into memory when it is chosen, so the value holds its bytes and does not depend on the browser input, which a step change, a hidden field or a removal unmounts. That makes `MaxFileSize` also the most memory one file can take: it defaults to 10 MiB (`FormFileValue.DefaultMaxSize`) when you leave it unset, each file of a `Multiple` field counts, and the bytes stay for as long as the form holds the value. A file above the limit is not read: it stays in the list so validation can name it, and opening it throws. Set a larger `MaxFileSize` on purpose if you accept larger files, and prefer to upload very large files by another route.

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

`Accept` is the `accept` attribute of the input, which only filters the picker, so the controller checks it again on validation (`.png` matches the extension, `image/png` the content type, `image/*` a family, ignoring case), together with `MaxFileSize` for each file and the number of files for a field without `Multiple`. The messages name the file and the limit (`report.pdf is larger than 5 MB`) and follow the locale. A rejected file stays in the list so the user sees which one it was, and the form cannot be submitted until it is removed. The size and type come from the browser: they are for the user's benefit, so check them again on the server. Files are not part of `ExportState()` drafts, since their content cannot be restored, and `RestoreDraft()` never fills a `File` or `Custom` field (or such a column of a list) from stored text.

In Blazor Server the content streams over the circuit when you read it, and `InputFile` reads from the browser only then, so opening a file you do not need costs nothing. `OpenReadAsync` can be called several times, and each call returns a new stream.

## Test your forms

Goal: test form logic without a browser, and the rendering with bUnit.

Because `DynamicFormController` has no UI dependency, most tests are plain xUnit tests. Keep each schema in a factory method that takes its dependencies as arguments, so a page and its tests build the same form:

```csharp
using Klyroo.Form;
using Klyroo.Form.Dynamic;

public static class SignupForm
{
    public static DynamicFormSchema Create(Func<string, Task<bool>> isEmailTaken) => new()
    {
        FormKey = "signup",
        Fields = [new DynamicFieldSchema { Key = "email", Label = "Email", Required = true }],
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
using Klyroo.Form.Dynamic;
using Xunit;

public sealed class SignupFormTests
{
    [Fact]
    public async Task A_taken_email_cancels_the_submit_and_shows_an_error()
    {
        var form = new DynamicFormController(SignupForm.Create(_ => Task.FromResult(true)));
        form.SetValue("email", "alice@example.com");

        Assert.False(await form.SubmitAsync());
        Assert.Equal("This email is already registered", form.GetError("email"));
        Assert.True(form.IsDirty);
    }

    [Fact]
    public async Task A_free_email_submits_and_leaves_the_form_clean()
    {
        var form = new DynamicFormController(SignupForm.Create(_ => Task.FromResult(false)));
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
using Klyroo.Form.Dynamic;
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
        var cut = Render<KlyDynamicFormEngine>(parameters => parameters
            .Add(p => p.Schema, SignupForm.Create(_ => Task.FromResult(false))));

        cut.Find("form").Submit();

        Assert.Equal("This field is required", cut.Find("[data-slot=form-field-error]").TextContent);
    }

    [Fact]
    public async Task The_overlay_task_completes_with_the_output()
    {
        var provider = Render<KlyFormProvider>();
        var forms = Services.GetRequiredService<DynamicFormService>();

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
