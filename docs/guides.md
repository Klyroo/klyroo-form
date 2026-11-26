# Guides

Task-oriented recipes. Each one states a goal, shows complete code and explains the parts that are not obvious. Every recipe uses the typed API: a schema declared with `FormSchema.For<TModel>`, a `FormController<TModel>` and the `KlyFormEngine<TModel>` and `KlyFormPage<TModel>` components. The Blazor samples assume the setup from [Getting started](getting-started.md): `AddKlyrooForm()` is registered, `@using Klyroo.Form` is in `_Imports.razor` and the stylesheets are loaded. Names such as `accounts`, `orders` and `WorkspaceDirectory` stand for your own services. A form that is only known at runtime, such as one stored as JSON, is covered on its own page: [Forms defined at runtime](runtime-forms.md).

- [Declare a form from a C# model](#declare-a-form-from-a-c-model)
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
- [Lay out the fields yourself](#lay-out-the-fields-yourself)
- [Add your own control](#add-your-own-control)
- [Upload files](#upload-files)
- [Read the rules from data annotations](#read-the-rules-from-data-annotations)
- [Test your forms](#test-your-forms)
- [Things to know when binding a model](#things-to-know-when-binding-a-model)

## Declare a form from a C# model

Goal: declare a form from a C# class, read and write its values as the types of the members, and receive the class back on submit.

`FormSchema.For<TModel>` declares the form once, the builder returns a handle for each field, and a `FormController<TModel>` reads and writes values through those handles. The schema and the controller live in `Klyroo.Form.Core` and work in a test, a view model or a console app; `KlyFormEngine<TModel>` and `KlyFormPage<TModel>` render them in Blazor.

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
```

```csharp
public static readonly FormSchema<Order> Schema = FormSchema.For<Order>(s =>
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

What happens when `SubmitAsync(save, target)` runs:

1. The field rules run, waiting for asynchronous ones.
2. The values are written into `target`, or into a new model when there is none, and every conversion is checked before the first member is set.
3. The model checks (`s.Validate((order, errors) => errors.Add(o => o.Seats, "..."))`, and the attributes in [Read the rules from data annotations](#read-the-rules-from-data-annotations)) run on that model, then `OnBeforeSubmit` and `OnSubmit`.
4. `save` runs inside the submit lifecycle, so `IsSubmitting` stays true while it works. When a step fails, or `save` returns `false` or throws, the members the write changed get their previous values again (a list holds its previous items and a row its previous cells) and the form stays dirty. Members the form did not write are not touched, and nothing your hooks or your save step did is undone: `OnBeforeSubmit`, `OnSubmit` (which runs before `save`, so its side effects stay when the save fails) and `save` itself are yours to compensate. On success the baseline is retaken.

### Nested objects, multiple choice and fields outside the model

`Group` lays out the fields of an object member, `MultiSelect` holds several chosen values, and `s.Field<TValue>("key", ...)` declares a field the form has and the model does not, such as the acceptance of terms:

```csharp
public sealed class Team
{
    public string Name { get; set; } = "";
    public Address Office { get; set; } = new();
    public List<Role> Roles { get; set; } = [];
}

public enum Role { Admin, Editor, Viewer }

public sealed class TeamForm
{
    public TeamForm()
    {
        FormField<Team, bool> terms = null!;
        Schema = FormSchema.For<Team>(s =>
        {
            s.Field(m => m.Name, f => f.Required());
            s.Group(m => m.Office, g =>
            {
                g.Field(a => a.Street);
                g.Field(a => a.City, f => f.Required());
            }, label: "Office");
            s.MultiSelect(m => m.Roles, [
                new FormOption<Role>(Role.Admin, "Administrator"),
                new FormOption<Role>(Role.Editor, "Editor"),
                new FormOption<Role>(Role.Viewer, "Viewer"),
            ]);
            terms = s.Field<bool>("terms", f => f.Required());   // a field the form has and the model does not
        });
        Terms = terms;
    }

    public FormSchema<Team> Schema { get; }

    public FormField<Team, bool> Terms { get; }
}
```

### Render the model in Blazor

`KlyFormEngine<TModel>` renders a `FormSchema<TModel>` with the header, the stepper, the fields and the actions, and `KlyFormPage<TModel>` renders a schema with sections as a page. Give either a `Schema` and a `Model`, or a `FormController<TModel>` through `Form` when you need to read the form from code. Razor infers `TModel` from `Schema`, `Model`, `Form` or `OnSave`, so the markup usually names no type:

```razor
<KlyFormEngine Schema="_schema" Model="_order" OnSave="SaveAsync" OnCancel="Cancelled" />
```

- `Model` is the object the form edits. Its values start the form, and a submit writes the values back into it, all or nothing: if validation, the model checks, `OnBeforeSubmit` or `OnSave` fails, the members the write changed get their previous values again. Members the form does not show (an `Id`, a `CreatedAt`) are not touched. Without a `Model` every submit creates a new `TModel`. A different `Model` instance reloads the form and drops unsaved edits; the same instance does not.
- `OnSave` is `Func<TModel, CancellationToken, Task<bool>>`, called with the model after validation and the hooks passed, while the form is still submitting. Return `false` to keep the edits and the form open after reporting the reason with `SetError` or `SetFormError`. `OnSubmit` is `EventCallback<TModel>`, raised afterwards, on success only, with the same model. `OnCancel` is an `EventCallback` and shows the Cancel button when it is bound.
- Keep the schema in a field or a static member. A new `FormSchema<TModel>` instance makes the component start a new controller.
- A method group given to `OnSubmit` (`OnSubmit="Saved"`) stops Razor from inferring `TModel` and fails with a conversion error: write the type, `<KlyFormEngine TModel="Order" ... OnSubmit="Saved" />`. The same happens with a lambda.
- The `Model` parameter is nullable, so a `Model="_order"` where `_order` is declared `Order?` makes Razor infer `Order?` as the type and warn about the schema. Declare the field non-null, or write `Model="_order!"`.
- A failed submit moves focus to the first invalid field or to the error summary, labels point at their control, and descriptions, help and errors are tied to it with `aria-describedby`. `KlyFormErrorSummary` and `KlyFormNavigationGuard` take a `FormController<TModel>` (their `Form` is an `IFormRuntime`).

To lay the fields out yourself, see [Lay out the fields yourself](#lay-out-the-fields-yourself).

## Validate a field with custom rules

Goal: reject a value that passes the built-in checks, and choose when the error appears.

```csharp
public sealed class Signup
{
    public string Username { get; set; } = "";
    public int Age { get; set; } = 18;
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public bool AcceptTerms { get; set; }
}

public static class SignupForm
{
    public static readonly FormSchema<Signup> Schema = FormSchema.For<Signup>(s =>
    {
        s.FormKey = "signup";
        s.Field(m => m.Username, f => f
            .Required()
            .MaxLength(20)
            .Trigger(FormValidationTrigger.Input)
            .Rule(name => name.Length >= 3, "Use at least 3 characters")
            .Rule(name => name.All(c => char.IsLetterOrDigit(c) || c == '-'), "Use letters, digits and dashes only"));
        s.Field(m => m.Age, f => f.Range(18, 120));
        var password = s.Field(m => m.Password, f => f
            .Required()
            .AsPassword(
                new FormPasswordRequirement("length", "Use at least 12 characters", p => p.Length >= 12),
                new FormPasswordRequirement("digit", "Use at least one digit", p => p.Any(char.IsDigit))));
        s.Field(m => m.ConfirmPassword, f => f
            .AsPassword()
            .DependsOn(password)
            .Rule((value, form) => value == form.Get(password), "Passwords differ"));
        s.Field(m => m.AcceptTerms, f => f.Required(message: "You must accept the terms"));
    });
}
```

`Required`, number bounds (`Range`), `MaxLength`, password requirements and `Rule` run in that order on each validation, and the first failure becomes the error. For a field that is empty and required, only the required message is reported. A `Required` checkbox or switch must be checked, so `Required()` is enough for a consent box, and `Required(message: "...")` gives it a message of your own.

Rules also run when the value is empty and the field is not required. The rule then receives the default of the type: `""` for a string, `0` for an int. Write rules that accept it, or reject it on purpose.

`Trigger` decides when a field is validated while the user edits it. `Blur` (the default) starts validating when the control first loses focus and then validates on every change, `Input` validates from the first change, and `Submit` waits for `Validate()` or a submit. Errors only show for touched fields, and `Validate()` and `SubmitAsync()` touch every field they check.

Rules that read another field, such as a confirmation that compares itself with the password, stay correct while the user edits the other field. After every change the controller validates again the touched fields that currently show an error, so a stale "Passwords differ" disappears when the password is changed to match. It cannot guess that an untouched-looking valid field became invalid, so name the fields your rules read with `DependsOn(handle)`, as `ConfirmPassword` does above: a change of `Password` validates `ConfirmPassword` again once the user has touched it, so the mismatch appears and clears as soon as the other field changes. A field the user has not touched yet stays quiet, and a field with `Trigger(FormValidationTrigger.Submit)` still waits for submission. `Required(condition)` follows the same rule: its error disappears when the condition stops holding, and appears on a touched field when you list the field the condition reads in `DependsOn`.

## Report a server error on a field

Goal: check a value against a service on submit and show the result on the field.

```csharp
// accounts is your own service
public static readonly FormSchema<Registration> Register = FormSchema.For<Registration>(s =>
{
    s.FormKey = "register";
    var email = s.Field(m => m.Email, f => f.Required());
    s.OnBeforeSubmit(async (registration, form) =>
    {
        if (await accounts.IsEmailTakenAsync(registration.Email))
        {
            form.SetError(email, "This email is already registered");
            return false;
        }
        return true;
    });
});
```

`OnBeforeSubmit` runs after local validation passes and before the save step. It receives the model the values were written into and the controller. Returning `false` cancels the submit, `SetError` puts the message on the field through its handle, and the next `Set` on that field clears it. `SubmitAsync()` returns `false` and the form stays dirty: the baseline is only retaken after a successful submit.

`SetError` takes a `blocking` argument. A blocking error makes `Validate()` and `SubmitAsync()` fail until it is cleared, without calling your service again. Use it when you set the error outside of the submit pipeline, for example from `OnBeforeNext` in a wizard. For an error set inside `OnBeforeSubmit`, which already returns `false`, the default is enough.

`SetFormError(message)` reports a failure that belongs to no field. `ClearErrors(handle)` clears the errors of one field, and `ClearErrors()` clears every error.

## Check a value against a service while the user types

Goal: tell the user that a username is taken as soon as they leave the field, without calling the service on every key and without letting a stale answer through.

```csharp
public static readonly FormSchema<Account> AccountForm = FormSchema.For<Account>(s =>
{
    s.Field(m => m.Username, f => f
        .Required()
        .RuleAsync(
            async (username, form, ct) => await accounts.IsUsernameTakenAsync(username, ct) ? "This username is taken" : null,
            debounce: TimeSpan.FromMilliseconds(400)));
});
```

The rule returns the error message, or `null` when the value is valid. It runs after the synchronous checks pass and only for a value that is not empty. Once the field is touched (it loses focus, or `Trigger(FormValidationTrigger.Input)` and it changes), a run starts when the value has stayed unchanged for `debounce`. A newer value cancels the older run through the token and its answer is ignored: the error is only ever the answer for the value on screen. While a run is waiting or running, `form.IsValidating(username)` is `true`, and the components show a spinner and "Checking..." under the field and set `aria-busy` on it.

`SubmitAsync()` and `NextStepAsync()` wait for the rules of the fields they validate, keep `IsSubmitting` set meanwhile, and fail if a rule returns a message. A run that already called the service with the current value is joined, not repeated, and an answer that is already known is reused. `await form.ValidateAsync()` does the same without submitting; the synchronous `Validate()` only uses the answers that are already known.

If the service is unreachable during a background run, the exception is swallowed and the field simply stays unchecked: the user can still fix other fields, and `SubmitAsync()` runs the rule again and lets the exception reach your code, like any other hook that throws. A value that is edited while its rule runs is checked again, and a submit never succeeds with a value no rule has seen: if results keep going stale, `SubmitAsync()` returns `false`. An edit that replaces a running rule cancels it silently, never as an `OperationCanceledException` from `SubmitAsync()` or `ValidateAsync()`. Dispose the controller (`FormController<TModel>` is `IDisposable`) when you are done with it, which cancels the runs still pending; the components do it for the controller they create, and `FormService` for the form it closes. A rule does not apply to the cells of a repeatable list. In a host without a synchronization context (a console app, a test), background runs finish on a thread-pool thread, and `Changed` is raised there.

## Save from the component and keep the form busy

Goal: call your API when the user submits, show a saving state while it runs, keep the edits when it fails and report the failure.

```razor
@page "/profile/edit"
@rendermode InteractiveServer
@inject IProfiles Profiles
@inject NavigationManager Nav

<KlyFormEngine TModel="Profile" Form="_form" Model="_profile" OnSave="SaveAsync" OnSubmit="Saved" />

@code {
    private readonly Profile _profile = new() { Name = "Ada", Email = "ada@example.com" };

    private readonly FormController<Profile> _form;

    public ProfilePage() => _form = new(Schemas.Profile, _profile);

    private async Task<bool> SaveAsync(Profile profile, CancellationToken ct)
    {
        var result = await Profiles.SaveAsync(profile, ct);
        if (result.Conflict)
        {
            _form.SetFormError("Someone else changed this profile. Reload it and try again.");
            return false;
        }
        return true;
    }

    private void Saved(Profile profile) => Nav.NavigateTo("/profile");
}
```

`OnSave` runs inside `FormController<TModel>.SubmitAsync`, after validation and the schema hooks, while `IsSubmitting` is still `true`: the button shows its saving state, a second submit is refused, and Back, Reset and Cancel are disabled. When it returns `true` the dirty snapshot is retaken, so `IsDirty` turns `false` and `OnSubmit` is raised. When it returns `false`, or throws, the snapshot is kept: the edits stay dirty, the model gets its previous values back, and the user can fix and resubmit. The token is cancelled when the component is disposed. The page passes both `Form` and `Model`: with `Form` the controller is yours, and `Model` is the object a submit writes into.

While the form submits, every field is disabled (`IsDisabled(field)` reports `true` for all of them while `IsSubmitting` is `true`), so the user cannot change values that are being validated or saved. If `OnSave`, an asynchronous rule, a hook or `OnSubmit` throws, the component logs the exception through `ILogger` and shows a form-level notice ("Something went wrong. Try again.", in the locale) instead of letting it end the Blazor Server circuit; `IsSubmitting` resets, the edits stay, and the notice goes away with the next attempt. After a successful save, the baseline is the set of values the handler received: an edit made from code while it ran stays dirty.

`OnSubmit` on its own runs after the submission is already over (`IsSubmitting` is `false`, the form is clean), so it is the place to navigate, not to save. Without the component, the same lifecycle is `await form.SubmitAsync(async (model, ct) => (await api.SaveAsync(model, ct)).Ok, target, ct)`.

`SetFormError` reports a failure that belongs to no field. It is a blocking error listed in the error summary at the top of the form, and it keeps `Validate()` failing until you call `ClearErrors()` (or `Reset`). After a failed submit or Next, the summary lists every field error as a button that focuses the field, a polite live region announces the count and the first error, and focus moves to the first invalid field (or to the summary when only form-level errors remain).

## Show and require fields conditionally

Goal: show a field only for some answers, and make another required depending on a third.

```csharp
public enum AccountType { Personal, Business }

public sealed class Billing
{
    public AccountType AccountType { get; set; }
    public string Company { get; set; } = "";
    public string Country { get; set; } = "";
    public string VatNumber { get; set; } = "";
}

public static class BillingForm
{
    public static readonly FormSchema<Billing> Schema = FormSchema.For<Billing>(s =>
    {
        s.FormKey = "billing";
        var type = s.Field(m => m.AccountType, f => f.AsRadio());
        s.Field(m => m.Company, f => f
            .Required()
            .VisibleWhen(c => c.Get(type) == AccountType.Business));
        var country = s.Field(m => m.Country, f => f.Default("FR"));
        s.Field(m => m.VatNumber, f => f
            .VisibleWhen(c => c.Get(type) == AccountType.Business)
            .Required(c => c.Get(country) != "US")
            .DependsOn(country)
            .TransformOutput(vat => vat.Replace(" ", "").ToUpperInvariant()));
    });
}
```

A field whose `VisibleWhen` returns `false` is not rendered, is skipped by validation and is not written into the model. Its value stays in the form state, so it comes back if the user changes their mind, and the member keeps the value it had. `Required(condition)` is evaluated on each validation; `DependsOn(country)` makes a touched `VatNumber` show or clear its error as soon as the country changes. Handles such as `type` and `country` are the captured locals the builder returned: the conditions are typed (`c.Get(type) == AccountType.Business`), so a renamed member or a wrong comparison does not compile.

`TransformOutput` changes the value only on its way into the model, here to normalize a VAT number. The form keeps what the user typed, so the field does not change under their cursor: `controller.CreateModel().VatNumber` is `FR12345` while `controller.Get(vat)` is still `fr 12 345`.

A `Card` or a `Tab` takes `visibleWhen` and hides its children too, with the same effect: they are not validated and not written, and their values stay in the form state. So `Company` and `VatNumber` can share a single condition:

```csharp
public sealed class BillingCard
{
    public static readonly FormSchema<Billing> Schema = FormSchema.For<Billing>(s =>
    {
        var type = s.Field(m => m.AccountType, f => f.AsRadio());
        s.Card("business", "Business details", card =>
        {
            card.Field(m => m.Company, f => f.Required());
            card.Field(m => m.VatNumber);
        }, visibleWhen: c => c.Get(type) == AccountType.Business);
    });
}
```

## Build a multi-step wizard

Goal: split a long form into steps, skip one that does not apply, and run an async check before moving on.

```csharp
public enum Tier { Free, Team }

public sealed class Onboarding
{
    public string Slug { get; set; } = "";
    public Tier Plan { get; set; }
    public string VatNumber { get; set; } = "";
    public string? Emails { get; set; }
}

public static class OnboardingForm
{
    public static IWorkspaceDirectory WorkspaceDirectory = new FakeDirectory();

    public static readonly FormSchema<Onboarding> Schema = Create();

    private static FormSchema<Onboarding> Create()
    {
        FormField<Onboarding, string> slug = null!;
        FormField<Onboarding, Tier> plan = null!;

        return FormSchema.For<Onboarding>(s =>
        {
            s.FormKey = "onboarding";
            s.Title = "Create your workspace";
            s.Step("workspace", "Workspace", step =>
            {
                slug = step.Field(m => m.Slug, f => f.Required().Label("Address"));
                plan = step.Field(m => m.Plan, f => f.AsRadio());
            });
            s.Step("billing", "Billing", step =>
            {
                step.Field(m => m.VatNumber, f => f.Required().Label("VAT number"));
            }, skipWhen: c => c.Get(plan) == Tier.Free);
            s.Step("invite", "Invite", step =>
            {
                step.Field(m => m.Emails, f => f.AsTextarea().Label("Teammates"));
            });
            s.OnBeforeNext(async (form, stepIndex) =>
            {
                if (stepIndex != 0) return true;
                var taken = await WorkspaceDirectory.IsTakenAsync(form.Get(slug)!);
                if (taken) form.SetError(slug, "This address is taken", blocking: true);
                return !taken;
            });
        });
    }
}
```

```razor
@page "/onboarding"
@rendermode InteractiveServer

<KlyFormEngine TModel="Onboarding" Schema="OnboardingForm.Schema" SubmitLabel="Finish" OnSubmit="Finish" />

@if (_created is not null)
{
    <p>Created @_created</p>
}

@code {
    private string? _created;

    private void Finish(Onboarding onboarding) => _created = onboarding.Slug;
}
```

Each `s.Step(key, title, configure)` declares one step, and the fields declared inside belong to it. A step's `skipWhen` and `s.OnBeforeNext(...)` take the controller, so they read values through handles. The handles of the first steps are captured in locals declared before `FormSchema.For` (the builder runs the step lambdas as it goes, so they are assigned by the time a condition runs). The controller validates only the current step when the user presses Next, then runs `OnBeforeNext` with the index of the step being left. Returning `false` keeps the user on the step. A step whose `skipWhen` returns `true` is passed over in both directions, and the stepper does not list it. If the steps after the current one are all skipped, the current step is the last one: the engine shows the submit button there instead of Next, and `OnBeforeNext` does not run, which is why the check on the address above sits on a step that a third step follows. A form whose first step is skipped starts on the next one.

`SubmitAsync()` validates the fields of the steps that are not skipped, and fields of skipped steps are not written into the model. A field in a skippable step can therefore be `Required`, as `VatNumber` is: a user on the free plan never sees the billing step and is never asked for a VAT number. In code, `NextStepAsync()`, `PreviousStepAsync()`, `CurrentStepKey`, `IsFirstStep` and `IsLastStep` drive and read the wizard.

The error in `OnBeforeNext` is blocking: pressing Next again fails validation straight away, without another call to the directory, until the user edits `Slug`.

## Build a settings page with sections

Goal: show every group of settings on one page, with a completion status per section and a guard against leaving with unsaved changes.

```razor
@page "/settings"
@rendermode InteractiveServer
@inject IWorkspaces Workspaces

<KlyFormNavigationGuard Form="_form" />
<KlyFormPage Form="_form" Model="_settings" OnSave="SaveAsync" />

@code {
    private readonly Settings _settings = new() { Name = "Acme", Domain = "acme.example" };

    private static readonly FormSchema<Settings> _schema = FormSchema.For<Settings>(s =>
    {
        s.FormKey = "settings";
        s.Title = "Workspace settings";
        s.Section("general", "General", section =>
        {
            section.Field(m => m.Name, f => f.Required().Label("Workspace name"));
            section.Field(m => m.Domain, f => f.Required());
        });
        s.Section("notes", "Notes", section =>
        {
            section.Field(m => m.Description);
        }, optional: true);
    });

    private readonly FormController<Settings> _form;

    public SettingsPage() => _form = new(_schema, _settings);

    private async Task<bool> SaveAsync(Settings settings, CancellationToken ct)
    {
        await Workspaces.SaveAsync(settings, ct);
        return true;
    }
}
```

`KlyFormPage<TModel>` renders a schema whose fields are declared in `s.Section(key, label, configure, optional: ...)` blocks. The page passes a controller through `Form` because `KlyFormNavigationGuard` needs the same controller; with no guard, `Schema` and `Model` are enough. It lists the sections in a sidebar with a status: `Complete`, `Pending` (a required field is empty), `Invalid` (a field has an error) or `Optional` (the section is marked optional and empty). Each section has its own Reset button once one of its fields has changed, and in code `GetSectionStates()` and `ResetSection(key)` give the same information.

The controller is created with the model, so the form starts clean and `IsDirty` turns `true` on the first edit. `KlyFormNavigationGuard` is a `NavigationLock` that follows the controller. It subscribes to `Changed` and re-renders itself when `IsDirty` flips, which a `NavigationLock` placed on the page cannot do: a page that renders the form through `KlyFormPage<TModel>` never re-renders when the user types, so `ConfirmExternalNavigation="@_form.IsDirty"` on the page stays at the value it had on the first render and the browser prompt never switches on. While the form is dirty the guard asks the browser to confirm leaving the site or closing the tab, and it asks `window.confirm` before in-app navigation, or calls your `ConfirmLeave` (return `true` to leave) if you prefer your own dialog. Because `OnSave` returning `true` retakes the baseline, the guard lets the page navigate after a successful save.

To keep unsaved edits across a reload, store a draft and restore it on the new controller:

```csharp
public static class DraftSample
{
    public static async Task Store(FormController<Settings> form, IDraftStorage storage)
    {
        // while the user edits, for example on form.Changed
        await storage.SetAsync("settings-draft", form.ExportDraft().ToJson());
    }

    public static async Task Load(FormController<Settings> form, IDraftStorage storage)
    {
        // when the page loads, with the controller built from the saved data as before
        if (await storage.GetAsync("settings-draft") is { } json)
        {
            form.RestoreDraft(FormDraft.FromJson(json));
        }
    }
}
```

A draft holds the values the user typed and the current step, not the model: restore it on a controller built from the same data. A restored draft is still dirty, because the baseline is the original data. Files are not part of drafts.

## Open a form in a dialog from anywhere

Goal: open a form in a modal dialog or a side drawer from any component, and continue with the result after the user submits.

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

    private static readonly FormSchema<NewProject> Schema = FormSchema.For<NewProject>(s =>
    {
        s.FormKey = "new-project";
        s.Title = "New project";
        s.Field(m => m.Name, f => f.Required());
    });

    private async Task Create()
    {
        var outcome = await Forms.CreateFormAsync(Schema, mode: FormShellMode.Modal);
        if (outcome.IsCompleted)
        {
            _projects.Add(outcome.Model!.Name);
        }
    }
}
```

`CreateFormAsync<TModel>(schema, model, mode)` shows the form in the provider and returns a task of `FormSubmitOutcome<TModel>`. `TModel` is inferred from the schema, and the model is optional: without one the form creates a new model. The task completes with `IsCompleted` set to `true` and `Model` set to the submitted model after a successful submit (the one you passed with the values written into it, or the new one the form created), or with `IsCompleted` set to `false` and the model you passed, unchanged, if the user cancels. `FormShellMode.Modal` is the default and renders a centered dialog; `Drawer` renders a full-height panel docked to the end of the viewport, and `Fullscreen` covers the whole viewport. The overlay carries the mode in a `data-mode` attribute. It is a native modal dialog from Klyroo.UI (`KlyModal`, `KlySlideover` for `Drawer`), so focus stays inside it, Escape and a click on the backdrop close the form (the task completes with `IsCompleted = false`), and focus returns to the element that opened the form. While the form is saving it cannot be dismissed.

Do the work after the `await`, as above. The schema hooks `OnBeforeSubmit` and `OnSubmit` run once per submit. Calling `CreateFormAsync` while another form is open replaces it, and the replaced form's task completes with `IsCompleted` set to `false`, as if the user had closed it. `AddKlyrooForm()` registers `FormService` and `DynamicFormService` as scoped services, and one `KlyFormProvider` hosts both.

## Edit repeatable rows

Goal: let the user add and remove rows, such as invoice lines, and validate each row.

```csharp
public sealed class Invoice
{
    public List<InvoiceLine> Lines { get; set; } = [];
}

public sealed class InvoiceLine
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public int Qty { get; set; } = 1;
}

public sealed class InvoiceForm
{
    public InvoiceForm()
    {
        FormField<InvoiceLine, string> sku = null!;
        FormField<InvoiceLine, int> qty = null!;
        FormListField<Invoice, InvoiceLine> lines = null!;

        Schema = FormSchema.For<Invoice>(s =>
        {
            s.FormKey = "invoice";
            lines = s.List(m => m.Lines, row =>
            {
                sku = row.Field(l => l.Sku, f => f.Required().MaxLength(12).Label("SKU"));
                qty = row.Field(l => l.Qty, f => f.Range(1, 999).Label("Quantity"));
            },
            addLabel: "Add line",
            configure: f => f
                .Required()
                .Rule(rows => rows.Select(r => r.Sku).Distinct().Count() == rows.Count, "Each SKU can appear once"));
        });
        Lines = lines;
        Sku = sku;
        Qty = qty;
    }

    public FormSchema<Invoice> Schema { get; }
    public FormListField<Invoice, InvoiceLine> Lines { get; }
    public FormField<InvoiceLine, string> Sku { get; }
    public FormField<InvoiceLine, int> Qty { get; }
}
```

`s.List(member, columns, addLabel, configure, itemFactory)` edits a collection member as repeatable rows. The columns are declared with `row.Field(...)`, which returns a handle typed on the row (`FormField<InvoiceLine, string>`), and the list itself has a handle, `FormListField<Invoice, InvoiceLine>`. `KlyFormEngine<TModel>` and `KlyFormPage<TModel>` show an add button (labelled by `addLabel`), the control of each column per cell and a remove button per row. The `configure` argument is the builder of the list field itself, so a rule that spans rows, like the one on `Sku` above, goes there and receives the whole list. `Required()` on the list requires at least one row.

In code, the rows are read and changed through the controller:

```csharp
var invoice = new InvoiceForm();
var form = new FormController<Invoice>(invoice.Schema);

form.AddRow(invoice.Lines);
var row = form.Rows(invoice.Lines)[0];
row.Set(invoice.Sku, "A-1", markTouched: true);
row.Set(invoice.Qty, 2);
var quantity = row.Get(invoice.Qty);                      // int
var skuError = row.GetError(invoice.Sku);                 // null

form.AddRow(invoice.Lines, new InvoiceLine { Sku = "A-1" });  // a row seeded from an item
var saved = await form.SubmitAsync((model, ct) => Task.FromResult(true));
var listError = form.GetError(invoice.Lines);             // Each SKU can appear once

form.RemoveRow(invoice.Lines, 1);
```

Each cell is validated with the rules of its column (`Required`, `Range`, `MaxLength` and the others) when you validate or submit, and a cell the user edited is checked as it changes, following the column's `Trigger`. The error shows under the cell, and `row.GetError(column)` reads it. A row remembers the item it was loaded from, so `ApplyTo(model)` and a submit write the edited columns into that same item and keep the members that are not columns (the `Id` above). The [things to know](#things-to-know-when-binding-a-model) describe the supported collection shapes.

## Prefill and reset a form

Goal: load existing data into a form, and restore it on demand.

```razor
@page "/controlled"
@rendermode InteractiveServer

<KlyFormEngine Form="_form" ShowReset="false" />

<button type="button" @onclick="Prefill" disabled="@_form.IsSubmitting">Use my profile</button>

@code {
    private readonly FormController<Profile> _form = new(Schemas.Profile);

    private void Prefill() => _form.Reset(new Profile { Name = "Alice", Email = "alice@example.com" });
}
```

Pass a `FormController<TModel>` to `KlyFormEngine<TModel>` through `Form` when you need to read or change the form from code. `Reset(model)` clears errors and touched state, returns to the first step, loads the values of the model and makes them the new baseline, so the form is not dirty afterwards. `Reset()` without a model returns to the baseline the controller has now (the values it was built with, or those of the last `Reset(model)` or successful submit). To restore a single field use `ResetField(handle)`, and for a section `ResetSection(key)`; both go back to the baseline too.

## Lay out the fields yourself

Goal: arrange the fields in your own markup, instead of the stacked layout of the form component.

```razor
<CascadingValue Value="_form">
    <div class="grid grid-cols-2 gap-4">
        <KlyFormFieldView Field="@_name" />
        <KlyFormFieldView Field="@_email" />
    </div>
</CascadingValue>

@code {
    private readonly FormController<Profile> _form = new(Schemas.Profile);
    private readonly FormField<Profile, string> _name = Schemas.Profile.Field(m => m.Name);
    private readonly FormField<Profile, string> _email = Schemas.Profile.Field(m => m.Email);
}
```

`KlyFormFieldView` takes the handle the schema gave and draws the field with its label, help and error. Place the controller with a `CascadingValue` (or pass it with `Form`), and Razor infers `TModel` and `TValue` from the handle. It does not subscribe to the controller: render it from a component that re-renders on `Changed` (`KlyFormEngine<TModel>` does) so changes made elsewhere show up.

## Add your own control

Goal: use a control the library does not have, such as a star picker or a map, as a field that is validated, dirty-tracked and written into the model like any other.

In a typed form the control gets the type of the member. Write the control as a component that edits one field through a `FormControlContext<TValue>`, where `TValue` is the type of the member:

```razor
@* StarPicker.razor: a Razor component that edits one int field through its FormControlContext *@
<div id="@Control.Id" role="radiogroup" aria-labelledby="@Control.LabelledBy" aria-describedby="@Control.DescribedBy"
     aria-invalid="@(Control.Error is null ? null : "true")">
    @for (var star = 1; star <= 5; star++)
    {
        var value = star;
        <button type="button" disabled="@Control.Disabled" aria-pressed="@(Control.Current() >= value ? "true" : "false")"
                @onclick="@(() => Control.Commit(value))" @onblur="Control.Blur">★</button>
    }
</div>

@code {
    [Parameter, EditorRequired] public FormControlContext<int> Control { get; set; } = default!;
}
```

`FormControlContext<TValue>` gives it what the built-in controls use: `Id` (put it on the control so the label points at it), `Value` and `Current()`, `Set(value)` while the user edits, `Commit(value)` for a complete change that should show its error at once, `Blur()` when focus leaves, `Error`, `Disabled`, `Required`, `Validating`, and `DescribedBy`, `LabelledBy` and `ErrorId` for the ARIA attributes. `Value` is typed as the member (`0` for an `int` that is empty or holds text that is not a number yet). For a control made of several inputs, put `role="group"` or `radiogroup` on its root and point `aria-labelledby` at `Control.LabelledBy`. The component draws the label, description, help, error and the reset button around your control, so the renderer only draws the control.

To use it in one form, put a `KlyFieldTemplate` inside the form component and name the field with its handle. Its content draws the control from the context, and it replaces the control of any field, a built-in kind as well as a field declared with `AsCustom()`:

```razor
<KlyFormEngine Schema="_schema" Model="_review" OnSave="SaveAsync">
    <KlyFieldTemplate Field="@_rating" Context="rating">
        <StarPicker Control="rating" />
    </KlyFieldTemplate>
    <KlyFieldTemplate Field="@_volume" Context="c">
        <input type="range" min="0" max="11" id="@c.Id" value="@c.Value" aria-describedby="@c.DescribedBy"
               @oninput="@(e => c.Set(int.Parse(e.Value!.ToString()!)))" @onblur="c.Blur" />
    </KlyFieldTemplate>
</KlyFormEngine>

@code {
    private readonly Review _review = new();

    private static readonly FormSchema<Review> _schema = FormSchema.For<Review>(s =>
    {
        s.Field(m => m.Rating, f => f.Range(1, 5).Required().Label("How was it?"));
        s.Field(m => m.Volume, f => f.Range(0, 11));
        s.Field(m => m.Comment);
    });

    private readonly FormField<Review, int> _rating = _schema.Field(m => m.Rating);
    private readonly FormField<Review, int> _volume = _schema.Field(m => m.Volume);

    private Task<bool> SaveAsync(Review review, CancellationToken ct) => Task.FromResult(true);
}
```

The base `FormControlContext` also has the string boundary for a raw HTML input: `Text` is what the input should show (the text the user typed when it is not a value yet) and `SetText(text)` parses it, keeping text that is not a value so the field reports it instead of a zero. A `KlyFieldTemplate` must sit inside `KlyFormEngine<TModel>` or `KlyFormPage<TModel>` (or have its `Form` set), and it does not apply to the columns of a repeatable list.

To register a control for the whole app, declare the field with `AsCustom(key)` and register the renderer by value type:

```csharp
public static readonly FormSchema<Rated> Schema = FormSchema.For<Rated>(s =>
    s.Field(m => m.Rating, f => f.AsCustom("stars").Label("How was it?").Range(1, 5)));
```

```csharp
services.AddKlyrooForm(f => f.CustomField<int>("stars", ctx => builder =>
{
    builder.OpenComponent<StarPicker>(0);
    builder.AddComponentParameter(1, nameof(StarPicker.Control), ctx);
    builder.CloseComponent();
}));
```

A renderer is used only by fields of its value type: a field of another type that names the key shows the "no renderer" notice instead of a control that would read the wrong type. Renderers are kept by key and value type, so typed renderers of several value types, and a runtime-mode renderer, can share one key. A `KlyFieldTemplate` of the field wins over the registered renderer. A column of a repeatable list declared with `AsCustom(key)` uses the registered renderer. Because `FormControlContext<TValue>` is created by the form, you test a control by rendering the form that holds it, as in [Test your forms](#test-your-forms).

## Upload files

Goal: let the user attach files, reject the wrong ones before they reach your server, and read the content when saving.

```csharp
public sealed class Report
{
    public string Title { get; set; } = "";
    public List<FormFileValue> Attachments { get; set; } = [];
}

public static class ReportForm
{
    public static readonly FormSchema<Report> Schema = FormSchema.For<Report>(s =>
    {
        s.Field(m => m.Title, f => f.Required());
        s.Field(m => m.Attachments, f => f
            .Label("Attachments")
            .Accept(".pdf,image/*")
            .MaxFileSize(5 * 1024 * 1024));
    });
}
```

A member of type `FormFileValue?` holds one file, and a `List<FormFileValue>` holds several. A chosen file is read into memory when it is chosen, so the value holds its bytes and does not depend on the browser input, which a step change, a hidden field or a removal unmounts. That makes `MaxFileSize` also the most memory one file can take: it defaults to 10 MiB (`FormFileValue.DefaultMaxSize`) when you leave it unset, each file of a multiple field counts, and the bytes stay for as long as the form holds the value. A file above the limit is not read: it stays in the list so validation can name it, and opening it throws. Set a larger `MaxFileSize` on purpose if you accept larger files, and prefer to upload very large files by another route.

The control is a Blazor `InputFile` behind a button, with the chosen files listed under it and a remove button on each. A `FormFileValue` has the name, size and content type the browser reported, and `OpenReadAsync()` to open the content. It has no UI type in it, so the same field works in a unit test (`FormFileValue.FromBytes("a.pdf", bytes, "application/pdf")`) and in the model you hand to your service:

```csharp
var saved = await form.SubmitAsync(async (report, ct) =>
{
    foreach (var file in report.Attachments)
    {
        await using var stream = await file.OpenReadAsync(ct);
        await storage.UploadAsync(file.Name, stream, ct);
    }
    return true;
});
```

`Accept` is the `accept` attribute of the input, which only filters the picker, so the controller checks it again on validation (`.png` matches the extension, `image/png` the content type, `image/*` a family, ignoring case), together with `MaxFileSize` for each file and the number of files for a field that takes one. The messages name the file and the limit (`report.pdf is larger than 5 MB`) and follow the locale. A rejected file stays in the list so the user sees which one it was, and the form cannot be submitted until it is removed. The size and type come from the browser: they are for the user's benefit, so check them again on the server. Files are not part of drafts, since their content cannot be restored, and `RestoreDraft()` never fills a file field from stored text.

In Blazor Server the content streams over the circuit when you read it, and `InputFile` reads from the browser only then, so opening a file you do not need costs nothing. `OpenReadAsync` can be called several times, and each call returns a new stream.

## Read the rules from data annotations

Goal: reuse the attributes you already put on a model as the rules of the form, and keep the model checks you wrote with `IValidatableObject`.

Install the extra package. It brings `Klyroo.Form.Core`:

```bash
dotnet add package Klyroo.Form.DataAnnotations --prerelease
```

```csharp
public sealed class AnnotatedSignup : IValidatableObject
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

public sealed class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

public enum Plan { Free, Team, Business }

public static class AnnotatedForm
{
    public static readonly FormSchema<AnnotatedSignup> Schema = FormSchema.For<AnnotatedSignup>(s =>
    {
        s.Title = "Create your account";
        s.Group(m => m.Address, g =>
        {
            g.Field(a => a.Street);
            g.Field(a => a.City);
        });
        s.FromDataAnnotations();
    });
}
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


## Test your forms

Goal: test form logic without a browser, and the rendering with bUnit.

Because `FormController<TModel>` has no UI dependency, most tests are plain xUnit tests. Keep each schema in a factory method that takes its dependencies as arguments, so a page and its tests build the same form:

```csharp
public static class RegisterForm
{
    public static FormSchema<Registration> Create(Func<string, Task<bool>> isEmailTaken) => FormSchema.For<Registration>(s =>
    {
        s.FormKey = "register";
        var email = s.Field(m => m.Email, f => f.Required());
        s.OnBeforeSubmit(async (registration, form) =>
        {
            if (await isEmailTaken(registration.Email))
            {
                form.SetError(email, "This email is already registered");
                return false;
            }
            return true;
        });
    });
}
```

Then drive the controller directly:

```csharp
public sealed class RegisterFormTests
{
    [Fact]
    public async Task A_taken_email_cancels_the_submit_and_shows_an_error()
    {
        var schema = RegisterForm.Create(_ => Task.FromResult(true));
        var form = new FormController<Registration>(schema);
        form.Set(schema.Field(m => m.Email), "alice@example.com");

        Assert.False(await form.SubmitAsync());
        Assert.Equal("This email is already registered", form.GetError(schema.Field(m => m.Email)));
        Assert.True(form.IsDirty);
    }

    [Fact]
    public async Task A_free_email_submits_and_leaves_the_form_clean()
    {
        var schema = RegisterForm.Create(_ => Task.FromResult(false));
        var form = new FormController<Registration>(schema);
        form.Set(schema.Field(m => m.Email), "alice@example.com");

        Registration? saved = null;
        Assert.True(await form.SubmitAsync((registration, ct) =>
        {
            saved = registration;
            return Task.FromResult(true);
        }));
        Assert.False(form.IsDirty);
        Assert.Equal("alice@example.com", saved?.Email);
    }
}
```

To test rendering, add `xunit.v3` and `bunit` (version 2 or later, which uses `BunitContext`) to a test project that references `Klyroo.Form`. Register the services in the constructor, render the components and query the `data-slot` attributes they expose, such as `form-field-error` and `form-title`, instead of CSS classes:

```csharp
public sealed class RegisterFormRenderingTests : BunitContext
{
    public RegisterFormRenderingTests()
    {
        Services.AddKlyrooForm();
    }

    [Fact]
    public void Submitting_an_empty_form_shows_the_required_error()
    {
        var cut = Render<KlyFormEngine<Registration>>(parameters => parameters
            .Add(p => p.Schema, RegisterForm.Create(_ => Task.FromResult(false))));

        cut.Find("form").Submit();

        Assert.Equal("This field is required", cut.Find("[data-slot=form-field-error]").TextContent);
    }

    [Fact]
    public async Task The_overlay_task_completes_with_the_submitted_model()
    {
        var provider = Render<KlyFormProvider>();
        var forms = Services.GetRequiredService<FormService>();

        var pending = forms.CreateFormAsync(
            RegisterForm.Create(_ => Task.FromResult(false)),
            new Registration { Email = "alice@example.com" });
        provider.Render();

        provider.Find("form").Submit();
        var outcome = await pending;

        Assert.True(outcome.IsCompleted);
        Assert.Equal("alice@example.com", outcome.Model?.Email);
    }
}
```

Submitting the `form` element runs the same path as the submit button. The overlay test completes because submitting the form in the provider resolves the task returned by `CreateFormAsync`. To test a field the way a user edits it, find its wrapper with `[data-form-field=Email]` and call `Input(...)` and `Blur()` on the control inside.

## Things to know when binding a model

- **How the key and the kind are chosen.** The key of a field is the member name, and the names of the members down to it joined with dots for a nested one (`Address.City`). The kind comes from the member type: text for a string, `Number` for numbers (an integer must be whole and inside the range of its type, and a `decimal` or `float` inside its own), `Checkbox` for a `bool`, `Select` with the members as options for an enum, `Date`, `DateTime` and `Time` for the date and time types, text for a `Guid`, `Tag` for a list of strings, `DateRange` for a `FormDateRange` and `File` for a `FormFileValue` or a list of them. `[DataType]` can pick `Password`, `Textarea`, `PhoneNumber`, `Date` or `DateTime` instead.
- **Settings written in code win.** Defaults come first, conventions such as data annotations second and the `Field(...)` call last, so `s.Field(o => o.Plan, f => f.AsRadio().Label("Your plan"))` overrides both.
- **Groups, lists and multiple choice.** `s.Group(o => o.Address, g => ...)` lays out the fields of an object member and creates the object when a value is written. `s.List(o => o.Lines, row => ...)` edits a collection member as repeatable rows: the member may be a `List<T>`, an array, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>` or `IEnumerable<T>`, and the item type needs a parameterless constructor or an `itemFactory`. `controller.Rows(lines)` gives rows whose `Get(column)` reads a cell as its type, and `AddRow` and `RemoveRow` change the list. A row remembers the item it was loaded from: `ApplyTo(model)` writes the edited columns into that same item, so members that are not columns (an `Id`, a concurrency token) survive, creates a new item only for an added row, drops the items of removed rows, keeps the order and does not touch the collection when no row was added, removed or replaced. The collection is changed in place when it can be (a `List<T>`, a `Collection<T>`), through the setter for an array or a read-only list. Only an item the target holds now is written into, so `ApplyTo(other)` and `CreateModel()` create new items and never change the rows of the model the form was loaded from. After a successful `SubmitAsync(save, target)` the rows stand for the items the target now holds, so a form that stays open keeps what `save` set on them. A draft restored with `RestoreDraft` has rows that came from no item, so they are written as new items. `s.MultiSelect(o => o.Roles, options)` holds several chosen values as `FormOption<TElement>`. A chain such as `o => o.Address.City` is accepted for a field and is written only when `Group` declared the object on the way; a list or a multiple choice must be a direct member, so `s.List(o => o.Address.Lines, ...)` is refused when the schema is built, and the message points to `s.Group(o => o.Address, g => g.List(a => a.Lines, ...))`. A collection member with no setter (`public List<Line> Lines { get; } = new();`) is filled in place; one whose type cannot be changed in place (an array, `IReadOnlyList<T>`, `IEnumerable<T>`) is refused when the schema is built, and a collection that is read-only at run time (`new List<T>().AsReadOnly()`) makes `ApplyTo` throw before it changes anything.
- **Fields that are not in the model.** `s.Field<bool>("terms", f => f.Required())` declares a field the form has and the model does not, such as the acceptance of terms. It is read and written through its handle, validated and part of the draft, and never reaches the model.
- **Steps, sections and layout.** `s.Step("account", "Account", st => ..., skipWhen: c => ...)` and `s.Section("billing", "Billing", sec => ..., optional: true)` declare the blocks, and `Card`, `Tabs`, `Heading`, `Info`, `Divider` and `Button` lay a form out. Each returns a handle a field can depend on.
- **Hidden fields.** A field that `VisibleWhen` hides, or that sits in a skipped step, is not written: its member keeps the value it had.
- **Empty values.** An empty input gives `null` for a nullable member and for a nullable string (`string?`), an empty string for a string that is not nullable, and `default` for another value type. A value that is there but is not a value of the member type (text for a number, `9223372036854775808` for a `long`, `abc` in a date restored from a draft, free text written by a custom control) is never turned into `default`: validation reports it as an error on the field (`FormMessages.InvalidValue`, or the more specific number and identifier messages), whatever the validation mode, so `Validate` and `SubmitAsync` fail without throwing, and `ApplyTo` and `CreateModel` called directly throw an `InvalidOperationException` for it. Numbers, enums and dates are converted with the invariant culture whatever the culture of the thread, so a `decimal` round-trips under `fr-FR`.
- **Nullable strings under trimming.** Whether a string member is nullable comes from its nullable annotation, which is read at run time. A trimmed app can turn that metadata off (`System.Reflection.NullabilityInfoContext.IsSupported`, the `NullabilityInfoContextSupport` property of the project): the annotation is then unknown, and every string member gets an empty string for an empty input, because an empty string is valid for any string and a `null` is not. Say it per field with `f.NullWhenEmpty()` (`null`) or `f.NullWhenEmpty(false)` (empty string), which wins over the annotation.
- **Exact numbers.** The `Number` control stores a `double` when the number has up to 15 digits, which is what a user types, and a `decimal` when its digits would not fit, so `long.MaxValue`, `ulong.MaxValue`, a `decimal` of 28 digits and a `double` of 17 digits round-trip exactly, and an input shows every digit (a `float` shows `0.1`, not `0.10000000149011612`). `Range` bounds are stored as doubles, but a value is compared with them digit for digit, both when it is written (`TrySet`, an agent) and when the form is validated: a `decimal` of `100.0000000000000001` is above a `Range(0m, 100m)`, and a `long` of `9007199254740993` is above a maximum of `2^53`. A double cannot tell `long.MaxValue` from the next integer, so every integer member also gets exact rules for being whole and inside its type, and `decimal` and `float` members one for fitting their type, with `FormMessages.WholeNumber` and `FormMessages.NumberOutOfRange`.
- **Dates and times.** The control shows a `DateTime` to the millisecond, which is all a browser `datetime-local` input accepts. A `DateTime`, `DateTimeOffset`, `TimeOnly` or enum member that the user did not change keeps its value as it is when you write into the same model, with its ticks, its `DateTimeKind` and, for an enum, a number that no member has (`(Status)42`). A value the user typed takes the `DateTimeKind` of the value it replaces, and is `Unspecified` when there was none. A `DateTimeOffset` keeps the offset of the value it replaces, and a default one has offset zero (the text is read as UTC).
- **A write is all or nothing.** `ApplyTo` converts every value and checks every object on the way to a member (creating the ones declared with `Group`) before it sets the first member, so an error leaves the model as it was. If a setter of your model throws, the members already set are put back. With a `target`, `SubmitAsync` also puts the members back when a check, a hook or your save step fails or throws, so the model holds only what was saved. Only the members the form wrote are put back: what a hook or your save step changes itself, on the model or elsewhere, is its own business, and a setter that refuses to take its old value back is skipped.
- **A record without a parameterless constructor.** Tell the builder how to create one with `s.Factory(() => new Point(0, 0))`. The model is created for `CreateModel`, for the model checks and for a new controller without a model, which starts from the values of that empty model, so property initializers are the defaults of the form (a `Default(...)` on a field wins over them). A form with model checks (`Validate`, `UseDataAnnotations`, `FromDataAnnotations`) and no way to create the model throws an `InvalidOperationException` from `FormSchema.For`, instead of failing at every submit.
- **Not supported.** Flags enums, a struct as the model (`FormSchema.For<TModel>` requires a class: the form writes into the object it is given, and a struct would be copied on the way), members of struct type nested in the model, fields (only properties are bound), a collection that is not one of the shapes above (`HashSet<T>`), and attributes on an enum member (`[Display]` on the members of an enum: pass `Options` instead).

The schema does not compile expressions: `o => o.Address.City` is only read to find the members. Values are read and written through delegates created from the property accessors, and by reflection for a chain, an interface member or a collection. The model type is annotated so its public properties and constructor are kept when an app is trimmed. `Klyroo.Form.Core` is trim and AOT analyzer clean; `Klyroo.Form.DataAnnotations` is trim-correct and not declared AOT compatible, see [Platforms](platforms.md#nativeaot-and-trimming).

