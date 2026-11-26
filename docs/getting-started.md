# Getting started

This tutorial builds a contact form twice. First you run it headless, as a console program, to see how the controller validates and submits. Then you render the same schema in a Blazor app. It takes about ten minutes.

You declare a form from a C# class: `FormSchema.For<TModel>` describes the fields, and a `FormController<TModel>` reads and writes their values as the types of your members, so a typo in a member name or a value of the wrong type is a compile error. Both steps below use that typed API. A form that is only known at runtime, for example one stored as JSON, uses the [runtime mode](runtime-forms.md) instead.

Check the [prerequisites](../README.md#prerequisites) first: the .NET 10 SDK, and for the components a Blazor app with Klyroo.UI set up.

## 1. Run a form without a UI

Create a console project and add the engine package:

```bash
dotnet new console -n ContactForm
cd ContactForm
dotnet add package Klyroo.Form.Core --prerelease
```

Replace `Program.cs` with a model, a schema and a controller:

```csharp
using Klyroo.Form;
```

```csharp
public sealed class Contact
{
    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public Topic Topic { get; set; }

    public string? Message { get; set; }
}

public enum Topic { Sales, Support }
```

```csharp
var schema = FormSchema.For<Contact>(s =>
{
    s.FormKey = "contact";
    s.Field(c => c.Name, f => f.Required());
    s.Field(c => c.Email, f => f.Required().Rule(e => e.Contains('@'), "Enter a valid email address"));
});
var name = schema.Field(c => c.Name);
var email = schema.Field(c => c.Email);
var form = new FormController<Contact>(schema);

form.Set(name, "Alice", markTouched: true);
form.Set(email, "alice", markTouched: true);
Console.WriteLine(form.GetError(email));      // Enter a valid email address
Console.WriteLine(await form.SubmitAsync());  // False

form.Set(email, "alice@example.com");
Console.WriteLine(await form.SubmitAsync((contact, ct) =>
{
    Console.WriteLine($"Send a reply to {contact.Email}");
    return Task.FromResult(true);
}));                                          // True
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

- `FormSchema.For<Contact>` declares one field per member you name. `s.Field(c => c.Email, ...)` returns a handle, `FormField<Contact, string>`, which carries the type of the value. The schema is immutable and holds no state, so you can keep it in a static member and share it.
- `Set` stores a value through its handle. `markTouched: true` marks the field as touched, and only touched fields show their error, so `GetError(email)` returns the message. A field that the user has not interacted with stays quiet until you call `Validate()` or submit.
- `SubmitAsync` validates every field. The first submit fails because the email has no `@`, so the save step does not run. The second passes validation, writes the values into a new `Contact`, calls your save step with it, and returns `true`.
- After a successful submit the controller takes a new snapshot of the values, so `IsDirty` is `false` again.

The `Klyroo.Form.Core` package has no UI dependency. The rest of this tutorial uses the same schema in Blazor.

## 2. Add Klyroo.Form to a Blazor app

Use a Blazor Web App with Interactive Server, WebAssembly or Auto rendering, or a .NET MAUI Blazor Hybrid app. Add the package to the project that hosts the components:

```bash
dotnet add package Klyroo.Form --prerelease
```

The package builds on Klyroo.UI and brings it in as a dependency.

### Register the services

Call `AddKlyrooForm()` in `Program.cs`. It registers `FormService` and `DynamicFormService` and calls `AddKlyrooUi()`:

```csharp
using Klyroo.Form;

builder.Services.AddKlyrooForm();
```

With Interactive WebAssembly or Auto, call it in the server project and in the `.Client` project, because components run in both.

### Import the namespace

In `_Imports.razor`:

```razor
@using Klyroo.Form
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

Put the `Contact` class in your project, then create `Components/Pages/Contact.razor`. `Api` stands for your own service:

```razor
@page "/contact"
@rendermode InteractiveServer
@inject IContactApi Api

<KlyFormEngine TModel="Contact" Schema="_schema" Model="_contact" SubmitLabel="Send" OnSave="SaveAsync" OnSubmit="Sent" />

@if (_sentTo is not null)
{
    <p>Thanks, we will write to @_sentTo.</p>
}

@code {
    private readonly Contact _contact = new();

    private string? _sentTo;

    // Keep the schema in a field: a new instance makes the component start a new controller.
    private static readonly FormSchema<Contact> _schema = FormSchema.For<Contact>(s =>
    {
        s.Title = "Contact us";
        s.Field(c => c.Name, f => f.Required());
        s.Field(c => c.Email, f => f.Required().Rule(e => e.Contains('@'), "Enter a valid email address"));
        s.Field(c => c.Topic);
        s.Field(c => c.Message, f => f.Required().AsTextarea().MaxLength(500));
    });

    private async Task<bool> SaveAsync(Contact contact, CancellationToken ct) => await Api.SendAsync(contact, ct);

    private void Sent(Contact contact) => _sentTo = contact.Email;
}
```

Run the app and open `/contact`. Press Send with the form empty: each required field shows `This field is required`. Type `alice` in the email field and leave it: the field shows the email message. Fill every field correctly and press Send: `SaveAsync` receives the `Contact` the form wrote its values into, and the confirmation appears.

Four details to know:

- `@rendermode InteractiveServer` is needed unless your app already renders interactively everywhere. In Static SSR the form appears but does not react to input.
- Keep the schema in a field or a static member. `KlyFormEngine<TModel>` creates a new controller whenever it receives a different `FormSchema<TModel>` instance, which would discard what the user typed if the schema were built inline in the markup.
- Razor infers `TModel` from `Schema`, `Model`, `Form` or `OnSave`, so the markup usually names no type. A method given to `OnSubmit` stops the inference with a conversion error, so this page writes `TModel="Contact"`. A lambda or a bound `EventCallback<Contact>` has the same effect.
- `OnSave` runs after validation and the schema hooks succeed, while the form shows its saving state, and returns `true` on success. `OnSubmit` is raised afterwards with the same model, once the form is clean, so it is the place to navigate. See [Save from the component](guides.md#save-from-the-component-and-keep-the-form-busy).

The form writes the values into `_contact` when it submits, all or nothing: if validation, a hook or `OnSave` fails, the model keeps its previous values, and members the form does not show keep what they had. Pass no `Model` and every submit creates a new `Contact`.

## 4. Read the rules from attributes

If your model already carries DataAnnotations attributes, add the extra package and let the schema read them:

```bash
dotnet add package Klyroo.Form.DataAnnotations --prerelease
```

```csharp
var schema = FormSchema.For<Contact>(s => s.FromDataAnnotations());
```

`[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, `[Display]` and the other attributes become the same rules you wrote by hand above, and `IValidatableObject` runs at submit. The [guides](guides.md#read-the-rules-from-data-annotations) list which attribute becomes which rule.

## Next steps

- [Guides](guides.md): declare a form from a model, validate against the server, build a wizard, open a form in a dialog, test your forms.
- [Concepts](concepts.md): how the schema, the controller and the components fit together.
- [Platforms](platforms.md): render modes, .NET MAUI Blazor Hybrid and NativeAOT.
- [Forms defined at runtime](runtime-forms.md): the dictionary-based mode, for forms stored as data.
- [API reference](api-reference.md): every public type and member.
