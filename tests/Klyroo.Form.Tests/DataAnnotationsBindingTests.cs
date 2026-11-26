using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

public sealed class DataAnnotationsBindingTests
{
    private static readonly FormSchema<Annotated> Form = FormSchema.For<Annotated>(s => s.FromDataAnnotations());

    private static FieldNode Field(string key) => Form.Node(key);

    private static FormController<Annotated> Controller(Action<Annotated>? edit = null)
    {
        var model = new Annotated { Name = "Ada", Team = "red" };
        edit?.Invoke(model);
        return new FormController<Annotated>(Form, model);
    }

    private static string? Error<TModel>(FormController<TModel> c, string key)
    {
        c.Validate();
        return c.ErrorOf(key);
    }

    [Fact]
    public void Display_gives_label_description_prompt_and_position()
    {
        var name = Field("Name");
        Assert.Equal("Full name", name.Label);
        Assert.Equal("As on your ID", name.Description);
        Assert.Equal("Ada", name.Placeholder);
        Assert.True(Form.Keys().ToList().IndexOf("Email") < Form.Keys().ToList().IndexOf("Name"));
        Assert.Equal("Email", Form.Keys()[0]);
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
        s.SetRaw("Short", "12345");
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
        stars.SetRaw("Stars", 9.0);
        Assert.Equal("Between 1 and 5", Error(stars, "Stars"));
        stars.SetRaw("Stars", 3.0);
        Assert.Null(Error(stars, "Stars"));
    }

    [Fact]
    public void Min_length_regular_expression_email_phone_and_url_are_rules()
    {
        var c = Controller();
        c.SetRaw("Code", "ab");
        Assert.NotNull(Error(c, "Code"));
        c.SetRaw("Code", "abc");
        Assert.Null(Error(c, "Code"));

        c.SetRaw("Letters", "abc");
        Assert.Equal("Capitals only", Error(c, "Letters"));
        c.SetRaw("Letters", "ABC");
        Assert.Null(Error(c, "Letters"));

        c.SetRaw("Email", "nope");
        Assert.NotNull(Error(c, "Email"));
        c.SetRaw("Email", "ada@example.com");
        Assert.Null(Error(c, "Email"));

        c.SetRaw("Phone", "abc");
        Assert.NotNull(Error(c, "Phone"));
        c.SetRaw("Phone", "+33 1 23 45 67 89");
        Assert.Null(Error(c, "Phone"));

        c.SetRaw("Site", "not a url");
        Assert.NotNull(Error(c, "Site"));
        c.SetRaw("Site", "https://example.com");
        Assert.Null(Error(c, "Site"));
    }

    [Fact]
    public void An_empty_optional_value_passes_the_attribute_rules()
    {
        var c = Controller();
        c.SetRaw("Email", "");
        c.SetRaw("Site", null);
        c.SetRaw("Code", "");
        Assert.True(c.Validate());
    }

    [Fact]
    public void Compare_checks_the_other_member_and_revalidates_when_it_changes()
    {
        Assert.Contains("Password", Field("Confirm").DependsOn!);
        var c = Controller();
        c.SetRaw("Password", "secret", markTouched: true);
        c.SetRaw("Confirm", "secrets", markTouched: true);
        Assert.Equal("Passwords differ", c.ErrorOf("Confirm"));
        c.SetRaw("Password", "secrets", markTouched: true);
        Assert.Null(c.ErrorOf("Confirm"));
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
        c.SetRaw("Lucky", 7.0);
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Lucky must be even", c.ErrorOf("Lucky"));

        c.SetRaw("Lucky", 8.0);
        Assert.True(await c.SubmitAsync());
    }

    [Fact]
    public async Task A_custom_attribute_of_a_hidden_field_is_not_run()
    {
        var form = FormSchema.For<Annotated>(s =>
        {
            s.Field(m => m.Name);
            s.Field(m => m.Lucky, f => f.VisibleWhen(_ => false));
            s.UseDataAnnotations();
        });
        var c = new FormController<Annotated>(form, new Annotated { Name = "Ada", Lucky = 3 });
        Assert.True(await c.SubmitAsync());
    }

    [Fact]
    public async Task Validatable_object_errors_go_to_the_named_members_or_to_the_form()
    {
        var form = FormSchema.For<Order>(s => s.FromDataAnnotations());
        var c = new FormController<Order>(form, new Order { Item = "pen", Quantity = 20 });
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Too many for the limit", c.ErrorOf("Quantity"));

        c.SetRaw("Quantity", -1.0);
        Assert.False(await c.SubmitAsync());
        Assert.Contains(c.Errors, e => e.Key == FormEngine.FormErrorKey && e.Value == "Negative order");

        c.SetRaw("Quantity", 3.0);
        c.SetRaw("Item", "both");
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Both fields", c.ErrorOf("Item"));
        Assert.Equal("Both fields", c.ErrorOf("Quantity"));

        c.SetRaw("Item", "pen");
        Assert.True(await c.SubmitAsync());
    }

    [Fact]
    public async Task Validatable_object_runs_only_after_the_field_rules_passed()
    {
        var form = FormSchema.For<Order>(s => s.FromDataAnnotations());
        var c = new FormController<Order>(form, new Order { Item = null, Quantity = 20 });
        Assert.False(await c.SubmitAsync());
        Assert.Equal("This field is required", c.ErrorOf("Item"));
        Assert.Null(c.ErrorOf("Quantity"));
    }

    [Fact]
    public async Task Nested_members_get_dotted_keys_and_their_attributes()
    {
        var form = FormSchema.For<Customer>(s =>
        {
            s.Field(c => c.Name);
            s.Group(c => c.Address, g =>
            {
                g.Field(a => a.City);
                g.Field(a => a.Floor);
            });
            s.UseDataAnnotations();
        });
        Assert.Equal(["Name", "Address.City", "Address.Floor"], form.Keys());
        Assert.Equal("Town", form.Node("Address.City").Label);
        Assert.True(form.Node("Address.City").Required);

        var c = new FormController<Customer>(form, new Customer { Name = "x", Address = new ShippingAddress { City = "Lyon", Floor = 3 } });
        Assert.False(await c.SubmitAsync());
        Assert.Equal("Floor must be even", c.ErrorOf("Address.Floor"));
    }

    [Fact]
    public void Explicit_field_settings_win_over_the_attributes()
    {
        var form = FormSchema.For<Annotated>(s =>
        {
            s.Field(m => m.Name, f => f.Label("Your name").Required(false));
            s.UseDataAnnotations();
        });
        var name = form.Leaves().Single();
        Assert.Equal("Your name", name.Label);
        Assert.False(name.Required);
        Assert.Equal("As on your ID", name.Description);
    }

    [Fact]
    public void Only_declared_fields_are_read_when_not_all_members_are_added()
    {
        var form = FormSchema.For<Annotated>(s =>
        {
            s.Field(m => m.Name);
            s.UseDataAnnotations();
        });
        Assert.Single(form.Leaves());
    }

    [Fact]
    public async Task A_typed_form_shows_attribute_errors_and_saves_the_typed_model()
    {
        var form = FormSchema.For<Signup>(s => s.FromDataAnnotations());
        var controller = new FormController<Signup>(form, new Signup());
        Signup? saved = null;
        var ct = Xunit.TestContext.Current.CancellationToken;

        Assert.False(await controller.SubmitAsync((s, _) => { saved = s; return Task.FromResult(true); }, cancellationToken: ct));
        Assert.Null(saved);
        Assert.Equal("This field is required", controller.ErrorOf("Name"));

        controller.Set(form.Field(s => s.Name), "Ada Lovelace");
        controller.Set(form.Field(s => s.Email), "ada@example.com");
        controller.Set(form.Field(s => s.Age), 36);
        Assert.True(await controller.SubmitAsync((s, _) => { saved = s; return Task.FromResult(true); }, cancellationToken: ct));

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

    public enum Plan { Free, Team, Business }

    public sealed class GuideAddress
    {
        [Required]
        public string? Street { get; set; }

        [Required, Display(Name = "Town")]
        public string? City { get; set; }
    }

    public sealed class GuideSignup : IValidatableObject
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

        public GuideAddress Address { get; set; } = new();

        public bool AcceptTerms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (!AcceptTerms)
                yield return new ValidationResult("You must accept the terms", [nameof(AcceptTerms)]);
        }
    }

    [Fact]
    public async Task The_model_of_the_guide_binds_validates_and_saves()
    {
        var form = FormSchema.For<GuideSignup>(s =>
        {
            s.Title = "Create your account";
            s.Group(x => x.Address, g =>
            {
                g.Field(a => a.Street);
                g.Field(a => a.City);
            });
            s.FromDataAnnotations();
        });
        Assert.Equal(["Name", "Email", "Age", "Address.Street", "Address.City", "Plan", "StartDate", "Password", "ConfirmPassword", "AcceptTerms"],
            form.Keys());

        var c = new FormController<GuideSignup>(form, new GuideSignup());
        c.SetRaw("Name", "Ada");
        c.SetRaw("Email", "ada@example.com");
        c.SetRaw("Address.Street", "1 rue X");
        c.SetRaw("Address.City", "Lyon");
        c.SetRaw("Password", "longenough");
        c.SetRaw("ConfirmPassword", "longenough");
        Assert.False(await c.SubmitAsync());
        Assert.Equal("You must accept the terms", c.ErrorOf("AcceptTerms"));

        c.SetRaw("AcceptTerms", true);
        c.SetRaw("Plan", "Team");
        c.SetRaw("StartDate", "2027-01-15");
        GuideSignup? saved = null;
        Assert.True(await c.SubmitAsync((s, _) => { saved = s; return Task.FromResult(true); }, cancellationToken: Xunit.TestContext.Current.CancellationToken));
        Assert.Equal(Plan.Team, saved!.Plan);
        Assert.Equal(new DateOnly(2027, 1, 15), saved.StartDate);
        Assert.Equal("Lyon", saved.Address.City);
    }
}
