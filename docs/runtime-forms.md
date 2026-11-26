# Forms defined at runtime

The rest of this documentation declares a form from a C# class with `FormSchema.For<TModel>`. This page covers the other way to declare one: the runtime mode, in the `Klyroo.Form.Dynamic` namespace, where a form is plain data (`DynamicFormSchema` and `DynamicFieldSchema`) and values are keyed by field name.

## When to use it

Use the runtime mode when the shape of the form is not known when you compile:

- the fields come from data you load, such as a form stored as JSON or in a database, or a form a user builds in an editor;
- the fields, conditions or steps change per tenant, per customer or per request;
- the code that handles the values works with names, text and JSON rather than with one model class.

Use the typed API ([Getting started](getting-started.md), [Guides](guides.md)) whenever a C# class describes the data. It checks member names and value types at compile time, writes into your model, and has the same validation, steps, sections, overlays and components. The runtime mode gives up that checking: a key is a string that nothing verifies, and a value is an `object?` that you cast. Both modes compile to one internal engine, so a field validates, tracks dirty state and renders the same way in either, and `Klyroo.Form.Core` stays NativeAOT compatible.

The samples assume the setup from [Getting started](getting-started.md), plus the namespace:

```razor
@using Klyroo.Form
@using Klyroo.Form.Dynamic
```

## How the two modes compare

| Typed mode | Runtime mode |
| :--- | :--- |
| `FormSchema.For<TModel>(s => ...)` builds a `FormSchema<TModel>` | `new DynamicFormSchema { Fields = [...] }` |
| `s.Field(m => m.Email, f => ...)` returns a `FormField<TModel, string>` handle | `new DynamicFieldSchema { Key = "email", ... }`, referred to by its string key |
| `FormController<TModel>`: `Get(handle)`, `Set(handle, value)`, `GetError(handle)` | `DynamicFormController`: `GetValue<T>(key)`, `SetValue(key, value)`, `GetError(key)` |
| `CreateModel()`, `ApplyTo(model)`, `SubmitAsync(save, target)` | `BuildOutput()`, a dictionary of the fields in effect |
| `Required()`, `Range(...)`, `MaxLength(...)`, `Rule(...)`, `RuleAsync(...)` | `Required`, `Min` and `Max`, `MaxLength`, `Validators`, `AsyncValidators` |
| `VisibleWhen(...)`, `Required(condition)`, `DisabledWhen(...)`, `OnChange(...)` | `Condition`, `RequiredWhen`, `DisabledWhen`, `Watch` |
| `s.Step(...)` with `skipWhen`, `s.Section(...)`, `s.OnBeforeNext(...)` | `Steps` with `SkipWhen`, `Sections`, `OnBeforeNext` |
| `s.List(...)` with `row.Field(...)` | `FormFieldKind.ArrayTable` or `ArrayList` with child `Fields` |
| `KlyFormEngine<TModel>`, `KlyFormPage<TModel>`, `KlyFormFieldView`, `KlyFieldTemplate` | `KlyDynamicFormEngine`, `KlyDynamicFormPage`, `KlyDynamicFieldView`, `FieldTemplates` |
| `FormService.CreateFormAsync<TModel>` returns `FormSubmitOutcome<TModel>` | `DynamicFormService.CreateFormAsync` returns `DynamicFormOutcome` with `FormData` |

## Run a form without a UI

```csharp
var schema = new DynamicFormSchema
{
    FormKey = "contact",
    Fields =
    [
        new DynamicFieldSchema { Key = "name", Label = "Name", Required = true },
        new DynamicFieldSchema
        {
            Key = "email",
            Label = "Email",
            Required = true,
            Validators =
            [
                new DynamicValidatorRule("email", "Enter a valid email address",
                    (value, _) => value?.ToString()?.Contains('@') == true),
            ],
        },
    ],
    OnSubmit = form =>
    {
        Console.WriteLine($"Send a reply to {form.GetValue<string>("email")}");
        return Task.CompletedTask;
    },
};

var form = new DynamicFormController(schema);

form.SetValue("name", "Alice", markTouched: true);
form.SetValue("email", "alice", markTouched: true);
Console.WriteLine(form.GetError("email"));    // Enter a valid email address
Console.WriteLine(await form.SubmitAsync());  // False

form.SetValue("email", "alice@example.com");
Console.WriteLine(await form.SubmitAsync());  // True
Console.WriteLine(form.IsDirty);              // False
```

The output is:

```text
Enter a valid email address
False
Send a reply to alice@example.com
True
False
```

`DynamicFormSchema` is plain data. Each `DynamicFieldSchema` has a `Key`, which is the name of the value in the form state and in the output. `SetValue` stores a value, and `markTouched: true` marks the field as touched: only touched fields show their error. `SubmitAsync` validates every field, runs `OnSubmit` when they all pass, and takes a new snapshot of the values, so `IsDirty` is `false` afterwards.

## Render a schema in Blazor

```razor
@page "/dynamic-contact"
@rendermode InteractiveServer

<KlyDynamicFormEngine Schema="_schema" SubmitLabel="Send" OnSubmit="Send" />

@if (_sent is not null)
{
    <p>Thanks, we will write to @_sent.</p>
}

@code {
    private string? _sent;

    private readonly DynamicFormSchema _schema = new()
    {
        FormKey = "contact",
        Title = "Contact us",
        Fields =
        [
            new DynamicFieldSchema { Key = "name", Label = "Name", Required = true },
            new DynamicFieldSchema { Key = "email", Label = "Email", Required = true,
                Validators = [new DynamicValidatorRule("email", "Enter a valid email address",
                    (value, _) => value?.ToString()?.Contains('@') == true)] },
            new DynamicFieldSchema { Key = "topic", Label = "Topic", Kind = FormFieldKind.Select,
                Options = [new("sales", "Sales"), new("support", "Support")] },
            new DynamicFieldSchema { Key = "message", Label = "Message", Kind = FormFieldKind.Textarea, Required = true },
        ],
    };

    private Task Send(IReadOnlyDictionary<string, object?> data)
    {
        _sent = (string?)data["email"];
        return Task.CompletedTask;
    }
}
```

Three details to know:

- `@rendermode InteractiveServer` is needed unless your app already renders interactively everywhere. In Static SSR the form appears but does not react to input.
- Keep the schema in a field. `KlyDynamicFormEngine` creates a new controller whenever it receives a different `DynamicFormSchema` instance, which would discard what the user typed if the schema were built inline in the markup. To read or change the form from code, create a `DynamicFormController` and pass it through `Form`.
- `OnSubmit` runs after validation, the schema hooks and `OnSave` succeed. It receives the same dictionary as `BuildOutput()`.

## Build a schema from stored data

Goal: show a form whose fields are described by a document or a database row.

The library does not define a storage format. You decide what your data holds and map it to `DynamicFieldSchema` yourself, which is a few lines and keeps your format independent of the library:

```csharp
// What your database or JSON document holds: the shape is yours.
public sealed record StoredField(string Key, string Label, string Type, bool Required, string[]? Choices = null);

public static class StoredForms
{
    public static DynamicFormSchema Build(string formKey, IEnumerable<StoredField> stored) => new()
    {
        FormKey = formKey,
        Fields = [.. stored.Select(f => new DynamicFieldSchema
        {
            Key = f.Key,
            Label = f.Label,
            Required = f.Required,
            Kind = f.Type switch
            {
                "number" => FormFieldKind.Number,
                "choice" => FormFieldKind.Select,
                "longtext" => FormFieldKind.Textarea,
                _ => FormFieldKind.Text,
            },
            Options = f.Choices?.Select(c => new FormOptionItem(c, c)).ToList(),
        })],
    };
}
```

```csharp
var schema = StoredForms.Build("survey",
[
    new StoredField("name", "Name", "text", Required: true),
    new StoredField("seats", "Seats", "number", Required: false),
    new StoredField("plan", "Plan", "choice", Required: true, Choices: ["free", "team"]),
]);
var form = new DynamicFormController(schema);
```

The controller validates, tracks dirty state and builds the output like any other, and `KlyDynamicFormEngine` renders it. Anything that cannot be stored as text, such as a rule written as a delegate (`Validators`, `Condition`, `RequiredWhen`), has to be chosen by your mapping code from a name or a small description in your data. If you also need to run the form from an agent, `DynamicFormController` implements `IFormRuntime` like the typed controller.

## Validate a field with custom rules

```csharp
public static readonly DynamicFormSchema Schema = new()
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
            Key = "confirm",
            Kind = FormFieldKind.Password,
            DependsOn = ["password"],
            Validators = [new DynamicValidatorRule("match", "Passwords differ",
                (value, form) => Equals(value, form.GetValue("password")))],
        },
    ],
};
```

`Required`, number bounds (`Min` and `Max` on `FormFieldKind.Number`), `MaxLength`, password requirements and `Validators` run in that order on each validation, and the first failure becomes the error. `Validators` also run when the value is empty and the field is not required, so write rules that accept `null`, or reject it on purpose. A `Required` checkbox or switch must be checked.

`Trigger` decides when a field is validated while the user edits it (`Blur`, `Input` or `Submit`), exactly as in the typed mode. A rule that reads another field needs that field listed in `DependsOn`, as `confirm` does above.

## Report a server error and check a value while the user types

```csharp
// accounts is your own service
public static readonly DynamicFormSchema Schema = new()
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
    OnSubmit = form => Task.CompletedTask,
};
```

`OnBeforeSubmit` runs after local validation passes and before `OnSubmit`. Returning `false` cancels the submit, `SetError("email", ...)` puts the message on the field by key, and the next `SetValue` on that field clears it. `SetError` takes a `blocking` argument with the same meaning as in the typed mode.

For a check that needs I/O while the user types, add an asynchronous rule to the field:

```csharp
public static readonly DynamicFieldSchema Username = new()
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
};
```

The rule returns the error message, or `null` when the value is valid. It is debounced, a newer value cancels an older run, `form.IsValidating("username")` reports it, and `SubmitAsync()`, `NextStepAsync()` and `ValidateAsync()` wait for it. It does not apply to the cells of a repeatable list. The [guide on asynchronous rules](guides.md#check-a-value-against-a-service-while-the-user-types) describes the behavior in detail; it is the same for both modes.

## Save from the component

```razor
@inject IProfiles Profiles
@inject NavigationManager Nav

<KlyDynamicFormEngine Form="_form" OnSave="SaveAsync" OnSubmit="Saved" />

@code {
    private readonly DynamicFormController _form = new(new DynamicFormSchema
    {
        FormKey = "profile",
        Fields =
        [
            new DynamicFieldSchema { Key = "name", Label = "Name", Required = true },
            new DynamicFieldSchema { Key = "email", Label = "Email", Required = true },
        ],
    });

    private async Task<bool> SaveAsync(IReadOnlyDictionary<string, object?> data, CancellationToken ct)
    {
        var result = await Profiles.SaveAsync(new Profile { Name = (string)data["name"]!, Email = (string)data["email"]! }, ct);
        if (result.Conflict)
        {
            _form.SetFormError("Someone else changed this profile. Reload it and try again.");
            return false;
        }
        return true;
    }

    private void Saved(IReadOnlyDictionary<string, object?> data) => Nav.NavigateTo("/profile");
}
```

`OnSave` runs inside `DynamicFormController.SubmitAsync`, after validation and the schema hooks, while `IsSubmitting` is `true`. It receives the output dictionary. Return `true` to retake the baseline and raise `OnSubmit`, or `false` to keep the edits dirty. `SetFormError` reports a failure that belongs to no field. The lifecycle, the busy state and the error summary are the same as in [Save from the component](guides.md#save-from-the-component-and-keep-the-form-busy). Without the component: `await form.SubmitAsync(async (f, ct) => (await api.SaveAsync(f.BuildOutput(), ct)).Ok, ct)`.

## Show and require fields conditionally

```csharp
public static readonly DynamicFormSchema Schema = new()
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

A field whose `Condition` returns `false` is not rendered, is skipped by validation and is left out of `BuildOutput()`. Its value stays in the state, so it comes back if the user changes their mind. `RequiredWhen` is evaluated on each validation, in addition to `Required`. `TransformOutput` changes the value only in the output. A `Condition` on a container (`Group`, `Card`, `Object` or `Tabs`) hides its children too.

## Steps and sections

```csharp
public static readonly DynamicFormSchema Wizard = new()
{
    FormKey = "onboarding",
    Title = "Create your workspace",
    OnBeforeNext = async (form, stepIndex) =>
    {
        if (stepIndex != 0) return true;
        var taken = form.GetValue<string>("slug") == "acme";
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
            Fields = [new DynamicFieldSchema { Key = "vat", Label = "VAT number", Required = true }],
        },
        new DynamicStepSchema
        {
            Key = "invite", Title = "Invite",
            Fields = [new DynamicFieldSchema { Key = "emails", Label = "Teammates", Kind = FormFieldKind.Textarea }],
        },
    ],
};
```

`Steps` replaces `Fields` and `Sections`. The controller validates only the current step when the user presses Next, then runs `OnBeforeNext` with the index of the step being left. A step with `SkipWhen` returning `true` is passed over in both directions. If the steps after the current one are all skipped, the current step is the last one, so the engine shows the submit button there and `OnBeforeNext` does not run. `SubmitAsync()` validates the fields of the steps that are not skipped, and `BuildOutput()` leaves the skipped ones out.

A page with a completion status per section uses `Sections`, rendered by `KlyDynamicFormPage`:

```csharp
public static readonly DynamicFormSchema Settings = new()
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
};
```

`KlyDynamicFormPage` takes a controller through `Form`, because it needs the controller to compute the section states, and `KlyFormNavigationGuard` takes the same controller (`Form` is an `IFormRuntime`). A schema declares exactly one of `Steps`, `Sections` or `Fields`: with steps the others are ignored, and with sections the fields are.

## Edit repeatable rows

```csharp
public static readonly DynamicFieldSchema Lines = new()
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

public static readonly DynamicFormSchema Schema = new() { FormKey = "invoice", Fields = [Lines] };
```

`ArrayList` and `ArrayTable` hold a `List<Dictionary<string, object?>>`, one dictionary per row, keyed by the child field keys, and are rendered identically. In code, use `AddArrayItem`, `SetArrayItemValue`, `RemoveArrayItem` and `GetArrayItems`. Each cell is validated with the rules of its child field, and `GetArrayItemError("lines", 0, "sku")` or `GetError("lines[0].sku")` reads an error. A rule that spans rows, like `unique` above, goes on the list field, which receives the whole list. A `Number` cell holds a `double`, like a `Number` field. `Required` on the list requires at least one row, and a child's `Condition` hides its column.

## Add your own control

A field of `FormFieldKind.Custom` draws a control you provide. It is a normal stateful field: it holds whatever value you store, is checked by `Required` and `Validators`, and is in `BuildOutput()`.

```razor
@* StarPicker.razor: a Razor component that edits one field through its DynamicControlContext *@
<div id="@Context.Id" role="radiogroup" aria-labelledby="@Context.LabelledBy" aria-describedby="@Context.DescribedBy"
     aria-invalid="@(Context.Error is null ? null : "true")">
    @for (var star = 1; star <= 5; star++)
    {
        var value = star;
        <button type="button" disabled="@Context.Disabled" aria-pressed="@(Context.Current<int>() >= value ? "true" : "false")"
                @onclick="@(() => Context.Commit(value))" @onblur="Context.Blur">★</button>
    }
</div>

@code {
    [Parameter, EditorRequired] public DynamicControlContext Context { get; set; } = default!;
}
```

Register the renderer once for the app, and name its key in the schema:

```csharp
// Program.cs, once for the app: the key is the one the schema names
services.AddKlyrooForm(f => f.CustomField("stars", ctx => builder =>
{
    builder.OpenComponent<DynStarPicker>(0);
    builder.AddComponentParameter(1, nameof(DynStarPicker.Context), ctx);
    builder.CloseComponent();
}));
```

```csharp
public static readonly DynamicFieldSchema Rating = new()
{
    Key = "rating", Kind = FormFieldKind.Custom, CustomKey = "stars", Label = "How was it?", Required = true,
};
```

`DynamicControlContext` gives the renderer `Id`, `Value` and `Current<T>()`, `Set(value)`, `Commit(value)`, `Blur()`, `Error`, `Disabled`, `Required`, `Validating`, `DescribedBy` and `LabelledBy`. To register the renderer for one form instead of the whole app, pass it in the `FieldTemplates` parameter of `KlyDynamicFormEngine`, `KlyDynamicFormPage` or `KlyDynamicFieldView`: a `Dictionary<string, RenderFragment<DynamicControlContext>>` that wins over the registry. A custom field whose key has no renderer shows a notice instead of a control. `DynamicControlContext` has a public constructor, so a unit test can build one with `new DynamicControlContext(form, field, "id")`.

## Upload files

```csharp
public static readonly DynamicFieldSchema Attachments = new()
{
    Key = "attachments",
    Kind = FormFieldKind.File,
    Label = "Attachments",
    Multiple = true,
    Accept = ".pdf,image/*",
    MaxFileSize = 5 * 1024 * 1024,
};
```

The value is a `FormFileValue` (or `null`) for a single file, or a `List<FormFileValue>` with `Multiple`. `Accept` and `MaxFileSize` are checked on validation, files are read into memory when chosen (`MaxFileSize` defaults to 10 MiB), and they are not part of drafts. Read the content in `OnSave` with `(List<FormFileValue>?)data["attachments"]` and `file.OpenReadAsync(ct)`. The [typed guide](guides.md#upload-files) describes the rules and the limits.

## Open a form in a dialog

```razor
@page "/dynamic-projects"
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

`DynamicFormService.CreateFormAsync(schema, input, mode)` returns a task of `DynamicFormOutcome`: `IsCompleted` is `true` after a submit and `false` after a close, and `FormData` holds the output. Mount a `KlyFormProvider` in the layout, as in the [typed guide](guides.md#open-a-form-in-a-dialog-from-anywhere).

## Prefill, reset and drafts

`Reset(newInput)` clears errors and touched state, returns to the first step, loads the values from a dictionary and makes them the new baseline. `Reset()` without input returns to the defaults (unlike the typed `Reset()`, which returns to the baseline). `ResetField(key)` and `ResetSection(key)` go back to the baseline. Keys of the input that match no field are kept in `InternalState` but never appear in the output. `ExportState().ToJson()` and `RestoreDraft(FormDraft.FromJson(json))` keep a draft across a reload, as in the typed [settings page](guides.md#build-a-settings-page-with-sections).

## Test a runtime form

`DynamicFormController` has no UI dependency, so a runtime form is tested like a typed one, with plain xUnit tests on the controller:

```csharp
var form = new DynamicFormController(Conditions.Schema);
form.SetValue("accountType", "business");
form.SetValue("company", "Acme");
form.SetValue("country", "US");

Assert.True(await form.SubmitAsync());
Assert.Equal("business", form.BuildOutput()["accountType"]);
Assert.False(form.IsDirty);
```

To test rendering, render `KlyDynamicFormEngine` with bUnit, as the [typed guide](guides.md#test-your-forms) does for `KlyFormEngine<TModel>`.

## Values and keys

The engine holds control values in a dictionary of `object?` keyed by field key, and in the runtime mode that is what you read and write. Field keys are flat strings: containers (`Group`, `Card`, `Object`, `Tabs`) only affect layout, their children keep their own keys and become top-level entries of the output, so an `Object` field named `address` does not produce a nested `address` value, and an `address.street` structure is a naming convention. A container never has a value of its own. Repeatable lists are leaves whose value is a list of row dictionaries.

The built-in controls write strings for text-like kinds (dates, times, colors and phone numbers included), `double` for `Number` and `Slider` (a `decimal` instead when the digits typed or loaded do not fit a double, such as `long.MaxValue`, so no digit is lost), `int` for `Rating`, `bool` for `Checkbox` and `Switch`, the option value (a string) for single choices, a `List<string>` for `CheckboxGroup`, `Tag`, `DateRange` and choices with `Multiple`, and a `FormFileValue` for files. `GetValue<T>` converts with the invariant culture and reads the formats the controls store: `GetValue<int>("age")` works on a whole `double` (and is `0` for 3.5, which is not rounded), `GetValue<DateOnly>("birthday")` on `yyyy-MM-dd`, `GetValue<TimeOnly>` on `HH:mm`, `GetValue<DateTime>` on `yyyy-MM-ddTHH:mm`, and `Nullable<T>`, `Guid`, enums (by name or member number) and `DateTimeOffset` (a value without a zone counts as UTC) work too. `TryGetValue<T>` tells a missing, blank or unconvertible value from a real default. From code you can store anything, but dirty tracking compares values with `Equals`, apart from string lists and repeatable lists, which are compared by content.

When the initial input or `Reset(input)` fills the state, values from the input are copied, `TransformInput` converts each field that has one, a field without an input value takes `DefaultFactory`, else `DefaultValue`, else an empty list for repeatable lists, else `null`, and the result becomes the baseline of dirty tracking.

If you build the typed and the runtime form of the same data, the localization, the accessibility markup and the engine behavior described in [Concepts](concepts.md) are the same.
