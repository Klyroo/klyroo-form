# Guides

Task-oriented recipes. Each one states a goal, shows complete code and explains the parts that are not obvious. The Blazor samples assume the setup from [Getting started](getting-started.md): `AddKlyrooForm()` is registered, `@using Klyroo.Form` is in `_Imports.razor` and the stylesheets are loaded. Names such as `accounts` and `WorkspaceDirectory` stand for your own services.

- [Validate a field with custom rules](#validate-a-field-with-custom-rules)
- [Report a server error on a field](#report-a-server-error-on-a-field)
- [Show and require fields conditionally](#show-and-require-fields-conditionally)
- [Build a multi-step wizard](#build-a-multi-step-wizard)
- [Build a settings page with sections](#build-a-settings-page-with-sections)
- [Open a form in a dialog from anywhere](#open-a-form-in-a-dialog-from-anywhere)
- [Edit repeatable rows](#edit-repeatable-rows)
- [Prefill and reset a form](#prefill-and-reset-a-form)
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

A `Condition` on a container (`Group`, `Card`, `Object` or `Tabs`) hides the container but does not hide its children from validation and output. Put the condition on the children too, as `company` and `vat` do above.

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
                    new FormFieldSchema { Key = "vat", Label = "VAT number",
                        RequiredWhen = form => form.GetValue<string>("plan") != "free" },
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

`Steps` replaces `Fields` and `Sections`. The controller validates only the current step when the user presses Next, then runs `OnBeforeNext` with the index of the step being left. Returning `false` keeps the user on the step. The step with `SkipWhen` returning `true` is passed over in both directions, and the stepper does not list it.

`SubmitAsync()` validates every field of every step, including the steps that were skipped. A field in a skippable step therefore must not be unconditionally required, or a user who skipped the step could never submit. `vat` uses `RequiredWhen` with the opposite of the `SkipWhen` test for that reason.

The error in `OnBeforeNext` is blocking: pressing Next again fails validation straight away, without another call to the directory, until the user edits `slug`.

## Build a settings page with sections

Goal: show every group of settings on one page, with a completion status per section and a guard against leaving with unsaved changes.

```razor
@page "/settings"
@rendermode InteractiveServer
@inject IJSRuntime JS

<NavigationLock ConfirmExternalNavigation="@_form.IsDirty" OnBeforeInternalNavigation="ConfirmLeave" />

<KlyFormPage Form="_form" OnSubmit="Save" />

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

    private async Task ConfirmLeave(LocationChangingContext context)
    {
        if (_form.IsDirty && !await JS.InvokeAsync<bool>("confirm", "Discard your changes?"))
        {
            context.PreventNavigation();
        }
    }

    private Task Save(IReadOnlyDictionary<string, object?> data)
    {
        Console.WriteLine($"Saved {data["name"]}");
        return Task.CompletedTask;
    }
}
```

`KlyFormPage` takes a controller, not a schema, because it needs the controller to compute section states. It lists the sections in a sidebar with a status: `Complete`, `Pending` (a required field is empty), `Invalid` (a field has an error) or `Optional` (the section is marked optional and empty). Each section has its own Reset button once one of its fields has changed.

The controller is created with initial input, so the form starts clean and `IsDirty` turns `true` on the first edit. `NavigationLock` is a standard Blazor component: it asks the browser to confirm external navigation while `IsDirty` is `true`, and `OnBeforeInternalNavigation` lets you prompt for in-app navigation.

`KlyFormPage` renders every field as a text input, whatever its kind. Use `KlyFormEngine` with `Steps` or `Fields` when you need selects, checkboxes or textareas.

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

`CreateFormAsync` shows the form in the provider and returns a task. It completes with `IsCompleted` set to `true` and the output after a successful submit, or with `false` if the user cancels. `FormShellMode.Modal` is the default. `Drawer` and `Fullscreen` are accepted, but the provider currently renders all three in the same centered dialog and only sets a `data-mode` attribute.

Do the work after the `await`, as above. The schema hooks `OnBeforeSubmit` and `OnSubmit` run twice for a form opened through the provider, because the engine submits and then the service submits again, so a side effect in `OnSubmit` would fire twice. Calling `CreateFormAsync` while another form is open replaces it, and the replaced form's task never completes.

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
        new FormFieldSchema { Key = "sku", Label = "SKU" },
        new FormFieldSchema { Key = "qty", Label = "Quantity", Kind = FormFieldKind.Number, DefaultValue = 1.0 },
    ],
    Validators =
    [
        new FormValidatorRule("sku", "Every line needs a SKU", (value, _) =>
            value is List<Dictionary<string, object?>> rows
            && rows.All(row => !FormController.IsEmptyValue(row.GetValueOrDefault("sku")))),
    ],
};

var schema = new FormSchema { FormKey = "invoice", Fields = [lines] };
```

`ArrayList` and `ArrayTable` hold a `List<Dictionary<string, object?>>`, one dictionary per row, keyed by the child field keys. `KlyFormEngine` shows an add button (labelled by `AddItemLabel`), a text input per child and a remove button per row. The two kinds are rendered identically. In code, use `AddArrayItem`, `SetArrayItemValue`, `RemoveArrayItem` and `GetArrayItems`.

The controller does not validate the cells: `Required` on a child field is ignored, and so are `Min` and `Max`. Put a validator on the list field itself, which receives the whole list, as `sku` does above. Because the built-in inputs are text, cell values typed by the user are strings, even for a child of kind `Number`; the default of `1.0` is a `double`, so convert both when you read them.

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
