using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class EvenAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        value is int n && n % 2 != 0 ? new ValidationResult($"{validationContext.DisplayName} must be even") : ValidationResult.Success;
}

public sealed class Annotated
{
    [Required, Display(Name = "Full name", Description = "As on your ID", Prompt = "Ada", Order = 2)]
    [StringLength(10)]
    public string Name { get; set; } = "";

    [EmailAddress]
    [Display(Order = 1)]
    public string? Email { get; set; }

    [Range(18, 65)]
    public int Age { get; set; } = 30;

    [Required(ErrorMessage = "Pick one, {0}!")]
    public string Team { get; set; } = "";

    [MinLength(3)]
    public string? Code { get; set; }

    [RegularExpression("^[A-Z]+$", ErrorMessage = "Capitals only")]
    public string? Letters { get; set; }

    [Phone]
    [DataType(DataType.PhoneNumber)]
    public string? Phone { get; set; }

    [Url]
    public string? Site { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "Passwords differ")]
    [DataType(DataType.Password)]
    public string? Confirm { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Bio { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Day { get; set; }

    [Editable(false)]
    public string? Locked { get; set; }

    [ReadOnly(true)]
    public string? Frozen { get; set; }

    [Even]
    public int Lucky { get; set; }

    [MaxLength(4, ErrorMessage = "Too long")]
    public string? Short { get; set; }

    [Range(1, 5, ErrorMessage = "Between {1} and {2}")]
    public int Stars { get; set; } = 1;
}

public sealed class Order : IValidatableObject
{
    [Required]
    public string? Item { get; set; }

    public int Quantity { get; set; }

    public int Limit { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity > Limit) yield return new ValidationResult("Too many for the limit", [nameof(Quantity)]);
        if (Quantity < 0) yield return new ValidationResult("Negative order");
        if (Item == "both") yield return new ValidationResult("Both fields", [nameof(Item), nameof(Quantity)]);
    }
}

public sealed class Customer
{
    [Required]
    public string Name { get; set; } = "";

    public ShippingAddress Address { get; set; } = new();
}

public sealed class ShippingAddress
{
    [Required, Display(Name = "Town")]
    public string? City { get; set; }

    [Even]
    public int Floor { get; set; }
}

public sealed class DataAnnotationsBindingTests : BunitContext
{
    public DataAnnotationsBindingTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly FormModel<Annotated> Form = FormModel.For<Annotated>(b => b.FromDataAnnotations());

    private static FormFieldSchema Field(string key) => Form.Fields.Single(f => f.Key == key);

    private static FormController Controller(Action<Annotated>? edit = null)
    {
        var model = new Annotated { Name = "Ada", Team = "red" };
        edit?.Invoke(model);
        var c = Form.CreateController(model);
        return c;
    }

    private static string? Error(FormController c, string key)
    {
        c.Validate();
        return c.GetError(key);
    }

    [Fact]
    public void Display_gives_label_description_prompt_and_position()
    {
        var name = Field("Name");
        Assert.Equal("Full name", name.Label);
        Assert.Equal("As on your ID", name.Description);
        Assert.Equal("Ada", name.Placeholder);
        Assert.True(Form.Fields.Select(f => f.Key).ToList().IndexOf("Email") < Form.Fields.Select(f => f.Key).ToList().IndexOf("Name"));
        Assert.Equal("Email", Form.Fields[0].Key);
    }

    [Fact]
    public void Required_is_the_engine_rule_with_its_message_unless_the_developer_wrote_one()
    {
        Assert.True(Field("Name").Required);
        Assert.Null(Field("Name").RequiredMessage);
        var c = Controller(m => m.Name = "");
        Assert.Equal("This field is required", Error(c, "Name"));

        Assert.True(Field("Team").Required);
        var team = Controller(m => m.Team = "");
        Assert.Equal("Pick one, Team!", Error(team, "Team"));
    }

    [Fact]
    public void Required_uses_the_localized_messages_of_the_controller()
    {
        var c = Controller(m => m.Name = "");
        c.Messages = FormMessageLocales.Fr;
        Assert.Equal(FormMessageLocales.Fr.Required, Error(c, "Name"));
    }

    [Fact]
    public void String_length_and_max_length_use_the_native_rule_or_the_attribute_message()
    {
        Assert.Equal(10, Field("Name").MaxLength);
        var c = Controller(m => m.Name = "far too long for ten");
        Assert.Equal(FormMessages.Default.MaxLength(10), Error(c, "Name"));

        Assert.Null(Field("Short").MaxLength);
        var s = Controller();
        s.SetValue("Short", "12345");
        Assert.Equal("Too long", Error(s, "Short"));
    }

    [Fact]
    public void Range_on_a_number_is_min_and_max_and_an_error_message_makes_it_a_rule()
    {
        Assert.Equal((18d, 65d), (Field("Age").Min, Field("Age").Max));
        var c = Controller(m => m.Age = 12);
        Assert.Equal(FormMessages.Default.MinValue(18), Error(c, "Age"));

        Assert.NotEqual(1d, Field("Stars").Min);
        var stars = Controller();
        stars.SetValue("Stars", 9.0);
        Assert.Equal("Between 1 and 5", Error(stars, "Stars"));
        stars.SetValue("Stars", 3.0);
        Assert.Null(Error(stars, "Stars"));
    }

    [Fact]
    public void Min_length_regular_expression_email_phone_and_url_are_rules()
    {
        var c = Controller();
        c.SetValue("Code", "ab");
        Assert.NotNull(Error(c, "Code"));
        c.SetValue("Code", "abc");
        Assert.Null(Error(c, "Code"));

        c.SetValue("Letters", "abc");
        Assert.Equal("Capitals only", Error(c, "Letters"));
        c.SetValue("Letters", "ABC");
        Assert.Null(Error(c, "Letters"));

        c.SetValue("Email", "nope");
        Assert.NotNull(Error(c, "Email"));
        c.SetValue("Email", "ada@example.com");
        Assert.Null(Error(c, "Email"));

        c.SetValue("Phone", "abc");
        Assert.NotNull(Error(c, "Phone"));
        c.SetValue("Phone", "+33 1 23 45 67 89");
        Assert.Null(Error(c, "Phone"));

        c.SetValue("Site", "not a url");
        Assert.NotNull(Error(c, "Site"));
        c.SetValue("Site", "https://example.com");
        Assert.Null(Error(c, "Site"));
    }

    [Fact]
    public void An_empty_optional_value_passes_the_attribute_rules()
    {
        var c = Controller();
        c.SetValue("Email", "");
        c.SetValue("Site", null);
        c.SetValue("Code", "");
        Assert.True(c.Validate());
    }

    [Fact]
    public void Compare_checks_the_other_member_and_revalidates_when_it_changes()
    {
        Assert.Contains("Password", Field("Confirm").DependsOn!);
        var c = Controller();
        c.SetValue("Password", "secret", markTouched: true);
        c.SetValue("Confirm", "secrets", markTouched: true);
        Assert.Equal("Passwords differ", c.GetError("Confirm"));
        c.SetValue("Password", "secrets", markTouched: true);
        Assert.Null(c.GetError("Confirm"));
    }

    [Fact]
    public void Data_type_picks_the_kind()
    {
        Assert.Equal(FormFieldKind.Password, Field("Password").Kind);
        Assert.Equal(FormFieldKind.Textarea, Field("Bio").Kind);
        Assert.Equal(FormFieldKind.PhoneNumber, Field("Phone").Kind);
        Assert.Equal(FormFieldKind.Date, Field("Day").Kind);
        Assert.Equal(FormFieldKind.Number, Field("Age").Kind);
    }

    [Fact]
    public void Editable_false_and_read_only_disable_the_field()
    {
        Assert.True(Field("Locked").Disabled);
        Assert.True(Field("Frozen").Disabled);
        Assert.False(Field("Name").Disabled);
    }

    [Fact]
    public async Task A_custom_attribute_runs_at_submit_and_reports_on_its_member()
    {
        var c = Controller();
        c.SetValue("Lucky", 7.0);
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Lucky must be even", c.GetError("Lucky"));

        c.SetValue("Lucky", 8.0);
        Assert.True(await c.SubmitAsync());
    }

    [Fact]
    public async Task A_custom_attribute_of_a_hidden_field_is_not_run()
    {
        var form = FormModel.For<Annotated>(b => b
            .Field(m => m.Name)
            .Field(m => m.Lucky, f => f.Condition(c => false))
            .UseDataAnnotations());
        var c = form.CreateController(new Annotated { Name = "Ada", Lucky = 3 });
        Assert.True(await c.SubmitAsync());
    }

    [Fact]
    public async Task Validatable_object_errors_go_to_the_named_members_or_to_the_form()
    {
        var form = FormModel.For<Order>(b => b.FromDataAnnotations());
        var c = form.CreateController(new Order { Item = "pen", Quantity = 20 });
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Too many for the limit", c.GetError("Quantity"));

        c.SetValue("Quantity", -1.0);
        Assert.False(await c.SubmitAsync());
        Assert.Contains(c.Errors, e => e.Key == FormController.FormErrorKey && e.Value == "Negative order");

        c.SetValue("Quantity", 3.0);
        c.SetValue("Item", "both");
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Both fields", c.GetError("Item"));
        Assert.Equal("Both fields", c.GetError("Quantity"));

        c.SetValue("Item", "pen");
        Assert.True(await c.SubmitAsync());
    }

    [Fact]
    public async Task Validatable_object_runs_only_after_the_field_rules_passed()
    {
        var form = FormModel.For<Order>(b => b.FromDataAnnotations());
        var c = form.CreateController(new Order { Item = null, Quantity = 20 });
        Assert.False(await c.SubmitAsync());
        Assert.Equal("This field is required", c.GetError("Item"));
        Assert.Null(c.GetError("Quantity"));
    }

    [Fact]
    public async Task Nested_members_get_dotted_keys_and_their_attributes()
    {
        var form = FormModel.For<Customer>(b => b.Field(c => c.Name).Nested(c => c.Address, n => n.Field(a => a.City).Field(a => a.Floor)).UseDataAnnotations());
        Assert.Equal(["Name", "Address.City", "Address.Floor"], form.Fields.Select(f => f.Key));
        Assert.Equal("Town", form.Fields[1].Label);
        Assert.True(form.Fields[1].Required);

        var c = form.CreateController(new Customer { Name = "x", Address = new ShippingAddress { City = "Lyon", Floor = 3 } });
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Floor must be even", c.GetError("Address.Floor"));
    }

    [Fact]
    public void Explicit_field_settings_win_over_the_attributes()
    {
        var form = FormModel.For<Annotated>(b => b
            .Field(m => m.Name, f => f.Label("Your name").Required(false))
            .UseDataAnnotations());
        var name = form.Fields.Single();
        Assert.Equal("Your name", name.Label);
        Assert.False(name.Required);
        Assert.Equal("As on your ID", name.Description);
    }

    [Fact]
    public void Only_declared_fields_are_read_when_not_all_members_are_added()
    {
        var form = FormModel.For<Annotated>(b => b.Field(m => m.Name).UseDataAnnotations());
        Assert.Single(form.Fields);
    }

    [Fact]
    public async Task A_rendered_engine_shows_attribute_errors_and_submits_the_typed_model()
    {
        var form = FormModel.For<Signup>(b => b.FromDataAnnotations());
        Signup? saved = null;
        var cut = Render<KlyFormEngine>(p => p
            .Add(x => x.Schema, form.Schema)
            .Add(x => x.Input, form.ToInput(new Signup()))
            .Add(x => x.OnSave, form.OnSaveOutput((s, _) => { saved = s; return Task.FromResult(true); })));

        await cut.Find("form").SubmitAsync();
        Assert.Null(saved);
        Assert.Contains("This field is required", cut.Find("[data-form-field=Name] [data-slot=form-field-error]").TextContent);

        cut.Find("[data-form-field=Name] input").Input("Ada Lovelace");
        cut.Find("[data-form-field=Email] input").Input("ada@example.com");
        cut.Find("[data-form-field=Age] input").Input("36");
        await cut.Find("form").SubmitAsync();

        Assert.NotNull(saved);
        Assert.Equal("Ada Lovelace", saved.Name);
        Assert.Equal("ada@example.com", saved.Email);
        Assert.Equal(36, saved.Age);
    }

    public sealed class Signup
    {
        [Required, StringLength(80), Display(Name = "Full name")]
        public string Name { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Range(18, 120)]
        public int Age { get; set; } = 18;
    }
}
