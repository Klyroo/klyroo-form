# Klyroo.Form.DataAnnotations

Builds a typed [Klyroo.Form](https://github.com/klyroo/klyroo-form) schema from a C# model and its `System.ComponentModel.DataAnnotations` attributes, so a .NET developer can bind a form to a model the way they expect.

```csharp
public sealed class Signup
{
    [Required, Display(Name = "Full name", Prompt = "Ada Lovelace")]
    [StringLength(80)]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Range(18, 120)]
    public int Age { get; set; } = 18;

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = "";

    [Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

var schema = FormSchema.For<Signup>(s => s.FromDataAnnotations());

var form = new FormController<Signup>(schema, new Signup());
await form.SubmitAsync(async (signup, ct) => await SaveAsync(signup, ct));   // signup is a Signup

// Blazor: the typed components arrive in the next change
```

What it maps:

- `[Required]`, `[StringLength(max)]` and `[MaxLength]` on a string, and `[Range]` with int or double inclusive bounds on a number, become the rules the engine already has (`Required`, `MaxLength`, `Min` and `Max`), with its localized messages. A `[StringLength]` with a `MinimumLength` also gets a validator rule for the minimum, and `[Range]` with `typeof(decimal)` or text bounds, an exclusive bound, or on a field that is not a number is a validator rule too, with the English message of the attribute. An `ErrorMessage` you set turns `[StringLength]`, `[MaxLength]` and `[Range]` into validator rules with that message; on `[Required]` it replaces the engine message.
- `[Display]` (name, description, prompt, order), `[Editable(false)]`, `[ReadOnly(true)]` and `[DataType]` set the label, description, placeholder, position, disabled state and kind of control.
- `[MinLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[CreditCard]` and `[Compare]` become validator rules that run while the user edits, with the message of the attribute (English, or the `ErrorMessage` or resource you give). The message of a validator rule is worked out when the rule fails, so it follows the UI culture of the validation, and the label, description, placeholder and `[Required]` message are fixed when the form is built.
- Your own `ValidationAttribute`, attributes on the class and `IValidatableObject` run at submit on the model built from the form values. Their errors go to the field of the member they name, or to the form.

The schema is trim safe: nothing is compiled, and the model type is annotated so its public properties are kept. `FromDataAnnotations()` declares the other members of the model through `AllMembers()`, and `UseDataAnnotations()` reads the attributes of the fields you declared. The package needs `Klyroo.Form.Core`, so it works in a headless app too.

The full guide, with the limits, is in the [Bind a C# model](https://github.com/klyroo/klyroo-form/blob/main/docs/guides.md#bind-a-c-model) guide. The repository is at <https://github.com/klyroo/klyroo-form>.
