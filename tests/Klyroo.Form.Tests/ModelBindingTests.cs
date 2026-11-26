using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public enum Role
{
    Guest,
    TeamMember,
    Admin,
}

public sealed class Address
{
    public string? City { get; set; }

    public string Zip { get; set; } = "";
}

public sealed class Person
{
    public string Name { get; set; } = "";

    public string? Nickname { get; set; }

    public int Age { get; set; }

    public int? Children { get; set; }

    public decimal Salary { get; set; }

    public double? Height { get; set; }

    public bool Active { get; set; }

    public Role Role { get; set; }

    public Role? Backup { get; set; }

    public DateOnly Birthday { get; set; }

    public DateTime? Appointment { get; set; }

    public DateTimeOffset Created { get; set; }

    public TimeOnly Alarm { get; set; }

    public Guid Id { get; set; }

    public List<string> Tags { get; set; } = [];

    public Address? Address { get; set; }

    public string ReadOnly { get; } = "fixed";
}

public sealed record Point(int X, int Y)
{
    public string Label { get; init; } = "";
}

public sealed class ModelBindingTests : BunitContext
{
    public ModelBindingTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static FormModel<Person> AllMembers() => FormModel.For<Person>(b => b.AllMembers());

    [Theory]
    [InlineData(nameof(Person.Name), FormFieldKind.Text)]
    [InlineData(nameof(Person.Age), FormFieldKind.Number)]
    [InlineData(nameof(Person.Children), FormFieldKind.Number)]
    [InlineData(nameof(Person.Salary), FormFieldKind.Number)]
    [InlineData(nameof(Person.Height), FormFieldKind.Number)]
    [InlineData(nameof(Person.Active), FormFieldKind.Checkbox)]
    [InlineData(nameof(Person.Role), FormFieldKind.Select)]
    [InlineData(nameof(Person.Backup), FormFieldKind.Select)]
    [InlineData(nameof(Person.Birthday), FormFieldKind.Date)]
    [InlineData(nameof(Person.Appointment), FormFieldKind.DateTime)]
    [InlineData(nameof(Person.Created), FormFieldKind.DateTime)]
    [InlineData(nameof(Person.Alarm), FormFieldKind.Time)]
    [InlineData(nameof(Person.Id), FormFieldKind.Text)]
    [InlineData(nameof(Person.Tags), FormFieldKind.Tag)]
    public void The_kind_comes_from_the_member_type(string key, FormFieldKind kind)
    {
        var field = AllMembers().Fields.Single(f => f.Key == key);
        Assert.Equal(kind, field.Kind);
    }

    public sealed class Words
    {
        public string FirstName { get; set; } = "";

        public string HTMLContent { get; set; } = "";
    }

    [Fact]
    public void All_members_skips_the_ones_without_a_setter_or_a_default_kind()
    {
        var keys = AllMembers().Fields.Select(f => f.Key).ToList();
        Assert.DoesNotContain("ReadOnly", keys);
        Assert.DoesNotContain("Address", keys);
    }

    [Fact]
    public void Label_defaults_to_the_member_name_in_words()
    {
        var form = FormModel.For<Person>(b => b.AllMembers());
        Assert.Equal("Name", form.Fields.Single(f => f.Key == "Name").Label);
        Assert.Equal("Role", form.Fields.Single(f => f.Key == "Role").Label);
        var words = FormModel.For<Words>(w => w.AllMembers());
        Assert.Equal("First name", words.Field(x => x.FirstName).Label);
        Assert.Equal("HTML content", words.Field(x => x.HTMLContent).Label);
    }

    [Fact]
    public void An_enum_gets_its_members_as_options_and_a_nullable_one_is_clearable()
    {
        var form = AllMembers();
        var role = form.Fields.Single(f => f.Key == "Role");
        Assert.Equal(["Guest", "TeamMember", "Admin"], role.Options!.Select(o => o.Value));
        Assert.Equal("Team member", role.Options![1].Label);
        Assert.False(role.Clearable);
        Assert.True(form.Fields.Single(f => f.Key == "Backup").Clearable);
    }

    [Fact]
    public void An_integer_member_is_a_whole_number_within_the_range_of_its_type()
    {
        var form = FormModel.For<Person>(b => b.AllMembers());
        var controller = form.CreateController();
        Assert.Equal(int.MinValue, form.Fields.Single(f => f.Key == "Age").Min);
        controller.SetValue("Age", 2.5, markTouched: true);
        Assert.False(controller.Validate());
        Assert.Equal("Enter a whole number", controller.GetError("Age"));
        controller.SetValue("Age", 3.0, markTouched: true);
        Assert.Null(controller.GetError("Age"));
    }

    [Fact]
    public void Explicit_settings_win_over_the_defaults()
    {
        var form = FormModel.For<Person>(b => b
            .Field(p => p.Name, f => f.Kind(FormFieldKind.Textarea).Label("Full name").Placeholder("Ada").Span(6).Required(message: "Say your name"))
            .Field(p => p.Role, f => f.Kind(FormFieldKind.Radio).Options(new FormOptionItem("Guest", "Visitor"), new FormOptionItem("Admin", "Boss")))
            .Field(p => p.Age, f => f.Range(18, 120).Configure(s => s.Trigger = FormValidationTrigger.Input)));
        var name = form.Field(p => p.Name);
        Assert.Equal(FormFieldKind.Textarea, name.Kind);
        Assert.Equal("Full name", name.Label);
        Assert.Equal("Ada", name.Placeholder);
        Assert.Equal(6, name.Span);
        Assert.True(name.Required);
        Assert.Equal("Say your name", name.RequiredMessage);
        Assert.Equal(2, form.Field(p => p.Role).Options!.Count);
        var age = form.Field(p => p.Age);
        Assert.Equal((18d, 120d), (age.Min, age.Max));
        Assert.Equal(FormValidationTrigger.Input, age.Trigger);
    }

    [Fact]
    public void A_typed_rule_gets_the_value_of_the_member_type()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Age, f => f.Rule("adult", "Must be an adult", a => a >= 18)));
        var controller = form.CreateController();
        controller.SetValue("Age", 12.0, markTouched: true);
        Assert.Equal("Must be an adult", controller.GetError("Age"));
        controller.SetValue("Age", 30.0, markTouched: true);
        Assert.Null(controller.GetError("Age"));
    }

    [Fact]
    public void Nested_members_use_a_dotted_key_whether_declared_by_chain_or_with_Nested()
    {
        var chain = FormModel.For<Person>(b => b.Field(p => p.Address!.City));
        Assert.Equal("Address.City", chain.Fields.Single().Key);

        var nested = FormModel.For<Person>(b => b.Nested(p => p.Address!, n => n
            .Field(a => a.City, f => f.Label("Town"))
            .Field(a => a.Zip)));
        Assert.Equal(["Address.City", "Address.Zip"], nested.Fields.Select(f => f.Key));
        Assert.Equal("Town", nested.Fields[0].Label);
    }

    [Fact]
    public void Nested_creates_the_object_when_a_value_is_written()
    {
        var form = FormModel.For<Person>(b => b.Nested(p => p.Address!, n => n.Field(a => a.City)));
        var controller = form.CreateController();
        controller.SetValue("Address.City", "Lyon");
        var person = form.Create(controller);
        Assert.Equal("Lyon", person.Address!.City);
    }

    [Fact]
    public void A_chain_through_a_null_object_that_nothing_can_create_says_so()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Address!.City));
        var controller = form.CreateController();
        controller.SetValue("Address.City", "Lyon");
        var error = Assert.Throws<InvalidOperationException>(() => form.Create(controller));
        Assert.Contains("Address", error.Message);
        // an object that exists is simply written through
        var person = new Person { Address = new Address() };
        form.Write(controller, person);
        Assert.Equal("Lyon", person.Address.City);
    }

    [Fact]
    public void Round_trip_keeps_every_supported_type()
    {
        var form = AllMembers();
        var source = new Person
        {
            Name = "Ada",
            Nickname = "ada",
            Age = 36,
            Children = 2,
            Salary = 1234.56m,
            Height = 1.72,
            Active = true,
            Role = Role.Admin,
            Backup = Role.TeamMember,
            Birthday = new DateOnly(1990, 5, 17),
            Appointment = new DateTime(2026, 3, 4, 14, 30, 0),
            Created = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero),
            Alarm = new TimeOnly(7, 45),
            Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Tags = ["a", "b"],
        };
        var controller = form.CreateController(source);
        Assert.False(controller.IsDirty);
        var copy = form.Create(controller);

        Assert.Equal(source.Name, copy.Name);
        Assert.Equal(source.Nickname, copy.Nickname);
        Assert.Equal(source.Age, copy.Age);
        Assert.Equal(source.Children, copy.Children);
        Assert.Equal(source.Salary, copy.Salary);
        Assert.Equal(source.Height, copy.Height);
        Assert.Equal(source.Active, copy.Active);
        Assert.Equal(source.Role, copy.Role);
        Assert.Equal(source.Backup, copy.Backup);
        Assert.Equal(source.Birthday, copy.Birthday);
        Assert.Equal(source.Appointment, copy.Appointment);
        Assert.Equal(source.Created, copy.Created);
        Assert.Equal(source.Alarm, copy.Alarm);
        Assert.Equal(source.Id, copy.Id);
        Assert.Equal(source.Tags, copy.Tags);
    }

    [Fact]
    public void Values_are_loaded_in_the_formats_of_the_controls()
    {
        var input = AllMembers().ToInput(new Person
        {
            Age = 3,
            Salary = 10.5m,
            Role = Role.TeamMember,
            Birthday = new DateOnly(2000, 1, 2),
            Appointment = new DateTime(2026, 3, 4, 14, 30, 0),
            Alarm = new TimeOnly(7, 5),
            Id = Guid.Empty,
        });
        Assert.Equal(3.0, input["Age"]);
        Assert.Equal(10.5, input["Salary"]);
        Assert.Equal("TeamMember", input["Role"]);
        Assert.Equal("2000-01-02", input["Birthday"]);
        Assert.Equal("2026-03-04T14:30", input["Appointment"]);
        Assert.Equal("07:05", input["Alarm"]);
        Assert.Null(input["Backup"]);
        Assert.Null(input["Children"]);
    }

    [Fact]
    public void Decimals_are_not_affected_by_the_culture_of_the_thread()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            var form = AllMembers();
            var controller = form.CreateController(new Person { Salary = 1234.5m, Height = 1.5 });
            Assert.Equal(1234.5, controller.GetValue("Salary"));
            controller.SetValue("Salary", 99.99);
            var person = form.Create(controller);
            Assert.Equal(99.99m, person.Salary);
            Assert.Equal(1.5, person.Height);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Empty_input_gives_null_for_nullable_members_and_the_default_for_the_rest()
    {
        var form = AllMembers();
        var controller = form.CreateController(new Person
        {
            Name = "x",
            Nickname = "x",
            Age = 5,
            Children = 3,
            Backup = Role.Admin,
            Appointment = DateTime.Now,
            Height = 1.0,
        });
        controller.SetValue("Name", "");
        controller.SetValue("Nickname", "");
        controller.SetValue("Age", null);
        controller.SetValue("Children", null);
        controller.SetValue("Backup", "");
        controller.SetValue("Appointment", "");
        controller.SetValue("Height", null);
        controller.SetValue("Role", "");
        var person = form.Create(controller);
        Assert.Equal("", person.Name);
        Assert.Null(person.Nickname);
        Assert.Equal(0, person.Age);
        Assert.Null(person.Children);
        Assert.Null(person.Backup);
        Assert.Null(person.Appointment);
        Assert.Null(person.Height);
        Assert.Equal(Role.Guest, person.Role);
    }

    [Fact]
    public void Write_updates_the_target_and_leaves_hidden_and_undeclared_members_alone()
    {
        var form = FormModel.For<Person>(b => b
            .Field(p => p.Name)
            .Field(p => p.Nickname, f => f.Condition(c => c.GetValue<string>("Name") == "show")));
        var target = new Person { Name = "old", Nickname = "keep", Age = 41 };
        var controller = form.CreateController(target);
        controller.SetValue("Name", "new");
        var returned = form.Write(controller, target);
        Assert.Same(target, returned);
        Assert.Equal("new", target.Name);
        Assert.Equal("keep", target.Nickname);
        Assert.Equal(41, target.Age);
    }

    [Fact]
    public void A_date_time_offset_keeps_the_offset_of_the_member()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Created));
        var target = new Person { Created = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(2)) };
        var controller = form.CreateController(target);
        Assert.Equal("2026-06-01T10:00", controller.GetValue("Created"));
        controller.SetValue("Created", "2026-06-02T11:30");
        form.Write(controller, target);
        Assert.Equal(new DateTimeOffset(2026, 6, 2, 11, 30, 0, TimeSpan.FromHours(2)), target.Created);
    }

    [Fact]
    public void A_date_member_edited_as_a_date_time_and_a_date_time_as_a_date_convert()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Appointment, f => f.Kind(FormFieldKind.Date)));
        var controller = form.CreateController(new Person { Appointment = new DateTime(2026, 3, 4, 14, 30, 0) });
        Assert.Equal("2026-03-04", controller.GetValue("Appointment"));
        controller.SetValue("Appointment", "2026-04-05");
        Assert.Equal(new DateTime(2026, 4, 5), form.Create(controller).Appointment);
    }

    [Fact]
    public void A_new_controller_starts_from_the_initializers_of_the_model()
    {
        var form = FormModel.For<Address>(b => b.Field(a => a.Zip));
        Assert.Equal("", form.CreateController().GetValue("Zip"));
        var withDefault = FormModel.For<Defaulted>(b => b.Field(d => d.Plan));
        Assert.Equal("free", withDefault.CreateController().GetValue("Plan"));
    }

    public sealed class Defaulted
    {
        public string Plan { get; set; } = "free";
    }

    [Fact]
    public void A_record_without_a_parameterless_constructor_needs_a_factory()
    {
        var plain = FormModel.For<Point>(b => b.Field(p => p.Label));
        Assert.Throws<InvalidOperationException>(() => plain.Create(new Dictionary<string, object?>()));

        var form = FormModel.For<Point>(b => b
            .Factory(() => new Point(0, 0))
            .Field(p => p.X)
            .Field(p => p.Y)
            .Field(p => p.Label));
        var controller = form.CreateController(new Point(1, 2) { Label = "a" });
        controller.SetValue("X", 10.0);
        var point = form.Create(controller);
        Assert.Equal(new Point(10, 2) { Label = "a" }, point);
    }

    [Fact]
    public void Plain_fields_mix_in_with_model_fields_and_stay_out_of_the_model()
    {
        var form = FormModel.For<Person>(b => b
            .Field(p => p.Name)
            .Add(new FormFieldSchema { Key = "terms", Kind = FormFieldKind.Checkbox, Required = true })
            .Field(p => p.Age));
        Assert.Equal(["Name", "terms", "Age"], form.Fields.Select(f => f.Key));
        var controller = form.CreateController();
        controller.SetValue("terms", true);
        Assert.True(controller.Validate());
        Assert.Equal(0, form.Create(controller).Age);
    }

    [Fact]
    public void Fields_go_into_steps_and_sections_of_a_schema_built_by_hand()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Name).Field(p => p.Age));
        var schema = new FormSchema
        {
            Steps =
            [
                new FormStepSchema { Key = "one", Title = "One", Fields = [form.Field(p => p.Name)] },
                new FormStepSchema { Key = "two", Title = "Two", Fields = [form.Field(p => p.Age)] },
            ],
            OnBeforeSubmit = form.OnBeforeSubmit,
        };
        var controller = new FormController(schema);
        Assert.True(controller.IsStepped);
        Assert.Equal(["Name"], controller.CurrentStep!.Fields.Select(f => f.Key));
    }

    [Fact]
    public void The_order_function_sorts_the_fields_and_keeps_plain_ones_in_place()
    {
        var form = FormModel.For<Person>(b => b
            .Field(p => p.Name)
            .Field(p => p.Age)
            .Field(p => p.Nickname)
            .Order(m => m.Name == nameof(Person.Nickname) ? 1 : null));
        Assert.Equal(["Nickname", "Name", "Age"], form.Fields.Select(f => f.Key));
    }

    [Fact]
    public void A_convention_runs_before_the_explicit_settings()
    {
        var form = FormModel.For<Person>(b => b
            .Convention((m, s) => s.Label = "from convention " + m.Key)
            .Field(p => p.Name)
            .Field(p => p.Nickname, f => f.Label("mine")));
        Assert.Equal("from convention Name", form.Field(p => p.Name).Label);
        Assert.Equal("mine", form.Field(p => p.Nickname).Label);
    }

    [Fact]
    public async Task Model_checks_put_errors_on_fields_and_on_the_form_and_run_again_at_the_next_submit()
    {
        var form = FormModel.For<Person>(b => b
            .Field(p => p.Name)
            .Field(p => p.Age)
            .Nested(p => p.Address!, n => n.Field(a => a.City))
            .Validate((p, errors) =>
            {
                if (p.Name == "root") errors.Add("Name", "Reserved name");
                if (p.Address?.City == "Nowhere") errors.Add("City", "Unknown city");
                if (p.Age == 13) errors.Add("Thirteen is not allowed");
                if (p.Age == 14) errors.Add("Nothing", "Goes to the form");
            }));
        var controller = form.CreateController();
        controller.SetValue("Name", "root");
        controller.SetValue("Address.City", "Nowhere");
        controller.SetValue("Age", 13.0);

        Assert.False(await controller.SubmitAsync());
        Assert.Equal("Reserved name", controller.GetError("Name"));
        Assert.Equal("Unknown city", controller.GetError("Address.City"));
        Assert.Contains(controller.Errors, e => e.Value == "Thirteen is not allowed");

        controller.SetValue("Name", "ada");
        controller.SetValue("Address.City", "Lyon");
        controller.SetValue("Age", 14.0);
        Assert.False(await controller.SubmitAsync());
        Assert.DoesNotContain(controller.Errors, e => e.Value == "Thirteen is not allowed");
        Assert.Contains(controller.Errors, e => e.Value == "Goes to the form");

        controller.SetValue("Age", 30.0);
        Assert.True(await controller.SubmitAsync());
        Assert.Empty(controller.Errors);
    }

    [Fact]
    public async Task The_typed_submit_hands_over_the_model_through_the_submit_lifecycle()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Name, f => f.Required()).Field(p => p.Age));
        var controller = form.CreateController();
        Person? saved = null;
        var ct = Xunit.TestContext.Current.CancellationToken;

        Assert.False(await form.SubmitAsync(controller, (p, _) => { saved = p; return Task.FromResult(true); }, cancellationToken: ct));
        Assert.Null(saved);

        controller.SetValue("Name", "Ada");
        controller.SetValue("Age", 36.0);
        Assert.False(await form.SubmitAsync(controller, (p, _) => { saved = p; return Task.FromResult(false); }, cancellationToken: ct));
        Assert.True(controller.IsDirty);
        Assert.Equal("Ada", saved!.Name);

        var target = new Person { Name = "old", Age = 1 };
        Assert.True(await form.SubmitAsync(controller, (p, _) => { saved = p; return Task.FromResult(true); }, target, ct));
        Assert.Same(target, saved);
        Assert.Equal(36, target.Age);
        Assert.False(controller.IsDirty);
    }

    [Fact]
    public void Load_replaces_the_values_and_the_baseline()
    {
        var form = FormModel.For<Person>(b => b.Field(p => p.Name));
        var controller = form.CreateController();
        controller.SetValue("Name", "typed");
        Assert.True(controller.IsDirty);
        form.Load(controller, new Person { Name = "loaded" });
        Assert.Equal("loaded", controller.GetValue("Name"));
        Assert.False(controller.IsDirty);
    }

    [Fact]
    public void Bad_declarations_are_refused()
    {
        Assert.Throws<ArgumentException>(() => FormModel.For<Person>(b => b.Field(p => p.Name.ToUpper())));
        Assert.Throws<ArgumentException>(() => FormModel.For<Person>(b => b.Field(p => p.Name).Field(p => p.Name)));
        Assert.Throws<ArgumentException>(() => FormModel.For<Person>(b => b.Field(p => p.Address)));
        var ok = FormModel.For<Person>(b => b.Field(p => p.Address, f => f.Kind(FormFieldKind.Custom)));
        Assert.Equal(FormFieldKind.Custom, ok.Fields.Single().Kind);
    }

    [Fact]
    public async Task A_rendered_engine_is_bound_to_a_model_and_submits_a_typed_instance()
    {
        var form = FormModel.For<Person>(b =>
        {
            b.Title = "Person";
            b.Field(p => p.Name, f => f.Required().Placeholder("Name"))
                .Field(p => p.Age)
                .Field(p => p.Role)
                .Field(p => p.Birthday)
                .Nested(p => p.Address!, n => n.Field(a => a.City));
        });
        var source = new Person { Name = "Ada", Age = 36, Role = Role.Admin, Birthday = new DateOnly(1990, 5, 17) };
        Person? saved = null;
        Person? submitted = null;

        var cut = Render<KlyFormEngine>(p => p
            .Add(x => x.Schema, form.Schema)
            .Add(x => x.Input, form.ToInput(source))
            .Add(x => x.OnSave, form.OnSaveOutput((person, _) => { saved = person; return Task.FromResult(true); }))
            .Add(x => x.OnSubmit, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<IReadOnlyDictionary<string, object?>>(this, d => submitted = form.Create(d))));

        Assert.Equal("Person", cut.Find("[data-slot=form-title]").TextContent);
        cut.Find("[data-form-field=Name] input").Input("Grace");
        cut.Find("[data-form-field=Address\\.City] input").Input("Lyon");
        await cut.Find("form").SubmitAsync();

        Assert.NotNull(saved);
        Assert.Equal("Grace", saved.Name);
        Assert.Equal(36, saved.Age);
        Assert.Equal(Role.Admin, saved.Role);
        Assert.Equal(new DateOnly(1990, 5, 17), saved.Birthday);
        Assert.Equal("Lyon", saved.Address!.City);
        Assert.Equal("Grace", submitted!.Name);
    }
}
