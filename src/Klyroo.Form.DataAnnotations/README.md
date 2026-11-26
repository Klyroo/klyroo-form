# Klyroo.Form.DataAnnotations

Builds a [Klyroo.Form](https://github.com/klyroo/klyroo-form) form from a C# model and its `System.ComponentModel.DataAnnotations` attributes, so a Blazor developer can bind a form to a typed model the way they expect.

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

var form = FormModel.For<Signup>(b => b.FromDataAnnotations());

// Blazor
// <KlyFormEngine Schema="form.Schema" Input="form.ToInput(new Signup())" OnSave="form.OnSaveOutput(SaveAsync)" />
```

What it maps:

- `[Required]`, `[StringLength(max)]` and `[MaxLength]` on a string, and `[Range]` with int or double inclusive bounds on a number, become the rules the engine already has (`Required`, `MaxLength`, `Min` and `Max`), with its localized messages. A `[StringLength]` with a `MinimumLength` also gets a validator rule for the minimum, and `[Range]` with `typeof(decimal)` or text bounds, an exclusive bound, or on a field that is not a number is a validator rule too, with the English message of the attribute. An `ErrorMessage` you set turns `[StringLength]`, `[MaxLength]` and `[Range]` into validator rules with that message; on `[Required]` it replaces the engine message.
- `[Display]` (name, description, prompt, order), `[Editable(false)]`, `[ReadOnly(true)]` and `[DataType]` set the label, description, placeholder, position, disabled state and kind of control.
- `[MinLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, `[Url]`, `[CreditCard]` and `[Compare]` become validator rules that run while the user edits, with the message of the attribute (English, or the `ErrorMessage` or resource you give). The message of a validator rule is worked out when the rule fails, so it follows the UI culture of the validation, and the label, description, placeholder and `[Required]` message are fixed when the form is built.
- Your own `ValidationAttribute`, attributes on the class and `IValidatableObject` run at submit on the model built from the form values. Their errors go to the field of the member they name, or to the form.

The binding is trim safe: nothing is compiled, and the model type is annotated so its public properties are kept. The package needs `Klyroo.Form.Core`, so it works in a headless app too.

The full guide, with the limits, is in the [Bind a C# model](https://github.com/klyroo/klyroo-form/blob/main/docs/guides.md#bind-a-c-model) guide. The repository is at <https://github.com/klyroo/klyroo-form>.
