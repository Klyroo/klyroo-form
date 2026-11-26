# Getting started

This tutorial builds a contact form twice. First you run it headless, as a console program, to see how the controller validates and submits. Then you render the same schema in a Blazor app. It takes about ten minutes.

Klyroo.Form has two ways to declare a form. The **typed** one binds a form to a C# model (`FormSchema.For<TModel>`, `FormController<TModel>`), and is shown at the end. The **runtime** one, in the `Klyroo.Form.Dynamic` namespace, describes a form as data and keys values by name (`DynamicFormSchema`, `DynamicFormController`); it suits forms built from data or changing shape at runtime, and it is what the first three steps use because it needs no model class. Both run on the same engine.

Check the [prerequisites](../README.md#prerequisites) first: the .NET 10 SDK, and for the components a Blazor app with Klyroo.UI set up.

## 1. Run a form without a UI

Create a console project and add the engine package:

```bash
dotnet new console -n ContactForm
cd ContactForm
dotnet add package Klyroo.Form.Core --prerelease
```

Replace `Program.cs` with a schema and a controller:

```csharp
using Klyroo.Form;
using Klyroo.Form.Dynamic;

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

Run it with `dotnet run`. The output is:

```text
Enter a valid email address
False
Send a reply to alice@example.com
True
False
```

What happened:

- `DynamicFormSchema` is plain data. Each `DynamicFieldSchema` has a `Key`, which is the name of the value in the form state and in the output.
- `SetValue` stores a value. `markTouched: true` marks the field as touched, and only touched fields show their error, so `GetError("email")` returns the message. A field that the user has not interacted with stays quiet until you call `Validate()` or submit.
- `SubmitAsync` validates every field. The first submit fails because the email has no `@`, so `OnSubmit` does not run. The second passes validation, runs `OnSubmit`, and returns `true`.
- After a successful submit the controller takes a new snapshot of the values, so `IsDirty` is `false` again.

The `Klyroo.Form.Core` package has no UI dependency. The rest of this tutorial uses the same schema in Blazor.

## 2. Add Klyroo.Form to a Blazor app

Use a Blazor Web App with Interactive Server, WebAssembly or Auto rendering, or a .NET MAUI Blazor Hybrid app. Add the package to the project that hosts the components:

```bash
dotnet add package Klyroo.Form --prerelease
```

The package builds on Klyroo.UI and brings it in as a dependency.

### Register the services

Call `AddKlyrooForm()` in `Program.cs`. It registers `DynamicFormService` and calls `AddKlyrooUi()`:

```csharp
using Klyroo.Form;
using Klyroo.Form.Dynamic;

builder.Services.AddKlyrooForm();
```

With Interactive WebAssembly or Auto, call it in the server project and in the `.Client` project, because components run in both.

### Import the namespace

In `_Imports.razor`:

```razor
@using Klyroo.Form
@using Klyroo.Form.Dynamic
```

### Load the stylesheets

The components use the theme tokens of Klyroo.UI and classes that this package ships precompiled. In `App.razor`, inside `<head>`:

```razor
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
<link rel="stylesheet" href="@Assets["_content/Klyroo.Form/klyroo-form.css"]" />
```

Klyroo.UI documents more setup options, such as color mode and fonts, in its [repository](https://github.com/klyroo/klyroo-ui). Without these three files the form renders unstyled.

## 3. Render the schema

Create `Components/Pages/Contact.razor`:

```razor
@page "/contact"
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

Run the app and open `/contact`. Press Send with the form empty: each required field shows `This field is required`. Type `alice` in the email field and leave it: the field shows the email message. Fill every field correctly and press Send: `Send` receives the output dictionary, and the confirmation appears.

Three details to know:

- `@rendermode InteractiveServer` is needed unless your app already renders interactively everywhere. In Static SSR the form appears but does not react to input.
- Keep the schema in a field. `KlyDynamicFormEngine` creates a new controller whenever it receives a different `DynamicFormSchema` instance, which would discard what the user typed if the schema were built inline in the markup.
- `OnSubmit` on the component runs after validation, the schema hooks and `OnSave` succeed. It receives the same dictionary as `DynamicFormController.BuildOutput()`. To call your API while the form shows its saving state, pass `OnSave` (it returns `true` on success): see [Save from the component](guides.md#save-from-the-component-and-keep-the-form-busy).

## 4. Bind a C# model (typed mode)

If you would rather write a class than a dictionary, `Klyroo.Form.Core` builds the same form from a model. Fields are handles the builder returns, and values are read and written as the member types:

```csharp
using Klyroo.Form;

public sealed class Contact
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public string? Message { get; set; }
}

var schema = FormSchema.For<Contact>(s =>
{
    s.Field(c => c.Name, f => f.Required().Label("Your name"));
    s.Field(c => c.Email, f => f.Required().Rule(e => e.Contains('@'), "Enter a valid email address"));
    s.Field(c => c.Message, f => f.AsTextarea().MaxLength(500));
});

var form = new FormController<Contact>(schema);
form.Set(schema.Field(c => c.Name), "Alice", markTouched: true);
form.Set(schema.Field(c => c.Email), "alice@example.com");

var saved = await form.SubmitAsync((contact, ct) => Task.FromResult(true)); // contact is a Contact
```

`Klyroo.Form.DataAnnotations` reads the attributes of the model instead: `FormSchema.For<Contact>(s => s.FromDataAnnotations())`. 

In Blazor, render the typed schema with `KlyFormEngine<TModel>`. `TModel` is inferred from `Schema`, `Model` or `Form`, so the markup names no type:

```razor
@page "/contact"
@rendermode InteractiveServer

<KlyFormEngine Schema="_schema" Model="_contact" SubmitLabel="Send" OnSave="SaveAsync" OnSubmit="Sent" />

@code {
    private readonly Contact _contact = new();

    // Keep the schema in a field: a new instance makes the component start a new controller.
    private static readonly FormSchema<Contact> _schema = FormSchema.For<Contact>(s =>
    {
        s.Title = "Contact us";
        s.Field(c => c.Name, f => f.Required());
        s.Field(c => c.Email, f => f.Required());
        s.Field(c => c.Message, f => f.AsTextarea().MaxLength(500));
    });

    private async Task<bool> SaveAsync(Contact contact, CancellationToken ct) => await Api.SendAsync(contact, ct);

    private void Sent(Contact contact) { /* the form is clean and the model holds what was sent */ }
}
```

The form writes the values into `_contact` when it submits (all or nothing: if validation, a hook or `OnSave` fails, the model keeps its previous values), so members the form does not show keep what they had. Pass no `Model` and every submit creates a new `Contact`. The [Bind a C# model](guides.md#bind-a-c-model) guide has the complete example and the limits.

## Next steps

- [Guides](guides.md): bind a C# model, validate against the server, build a wizard, open a form in a dialog, test your forms.
- [Concepts](concepts.md): how the schema, the controller and the components fit together.
- [Platforms](platforms.md): render modes, .NET MAUI Blazor Hybrid and NativeAOT.
- [API reference](api-reference.md): every public type and member.
