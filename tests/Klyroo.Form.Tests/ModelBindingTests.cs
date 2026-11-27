using System.Globalization;
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

public sealed class ModelBindingTests
{
    private static FormSchema<Person> AllMembers() => FormSchema.For<Person>(s => s.AllMembers());

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
        Assert.Equal(kind, AllMembers().Node(key).Kind);
    }

    [Fact]
    public void The_kind_comes_from_the_type_of_a_declared_field_too()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Age);
            s.Field(p => p.Backup);
            s.Field(p => p.Birthday);
            s.Field(p => p.Tags);
        });
        Assert.Equal([FormFieldKind.Number, FormFieldKind.Select, FormFieldKind.Date, FormFieldKind.Tag], schema.Leaves().Select(n => n.Kind));
    }

    public sealed class Words
    {
        public string FirstName { get; set; } = "";

        public string HTMLContent { get; set; } = "";
    }

    [Fact]
    public void All_members_skips_the_ones_without_a_setter_or_a_default_kind()
    {
        var keys = AllMembers().Keys();
        Assert.DoesNotContain("ReadOnly", keys);
        Assert.DoesNotContain("Address", keys);
    }

    [Fact]
    public void Label_defaults_to_the_member_name_in_words()
    {
        var schema = AllMembers();
        Assert.Equal("Name", schema.Node("Name").Label);
        Assert.Equal("Role", schema.Node("Role").Label);
        var words = FormSchema.For<Words>(w => w.AllMembers());
        Assert.Equal("First name", words.Node("FirstName").Label);
        Assert.Equal("HTML content", words.Node("HTMLContent").Label);
    }

    [Fact]
    public void An_enum_gets_its_members_as_options_and_a_nullable_one_is_clearable()
    {
        var schema = AllMembers();
        var role = schema.Node("Role");
        Assert.Equal(["Guest", "TeamMember", "Admin"], role.Options!.Select(o => o.Value));
        Assert.Equal("Team member", role.Options![1].Label);
        Assert.False(role.Clearable);
        Assert.True(schema.Node("Backup").Clearable);
    }

    [Fact]
    public void An_enum_rendered_as_radio_buttons_still_gets_its_members_as_options()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Role, f => f.AsRadio()));
        var role = schema.Node("Role");
        Assert.Equal(FormFieldKind.Radio, role.Kind);
        Assert.Equal(["Guest", "TeamMember", "Admin"], role.Options!.Select(o => o.Value));
        Assert.Equal("Team member", role.Options![1].Label);
    }

    [Fact]
    public void An_integer_member_is_a_whole_number_within_the_range_of_its_type()
    {
        var schema = FormSchema.For<Person>(s => s.AllMembers());
        var controller = new FormController<Person>(schema);
        Assert.Equal(int.MinValue, schema.Node("Age").Min);
        controller.SetRaw("Age", 2.5, markTouched: true);
        Assert.False(controller.Validate());
        Assert.Equal("Enter a whole number", controller.ErrorOf("Age"));
        controller.SetRaw("Age", 3.0, markTouched: true);
        Assert.Null(controller.ErrorOf("Age"));
    }

    [Fact]
    public void Explicit_settings_win_over_the_defaults()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Name, f => f.AsTextarea().Label("Full name").Placeholder("Ada").Span(6).Required(message: "Say your name"));
            s.Field(p => p.Role, f => f.AsRadio().Options([new FormOption<Role>(Role.Guest, "Visitor"), new FormOption<Role>(Role.Admin, "Boss")]));
            s.Field(p => p.Age, f => f.Range(18, 120).Trigger(FormValidationTrigger.Input));
        });
        var name = schema.Node("Name");
        Assert.Equal(FormFieldKind.Textarea, name.Kind);
        Assert.Equal("Full name", name.Label);
        Assert.Equal("Ada", name.Placeholder);
        Assert.Equal(6, name.Span);
        Assert.True(name.Required);
        Assert.Equal("Say your name", name.RequiredMessage);
        var role = schema.Node("Role");
        Assert.Equal(FormFieldKind.Radio, role.Kind);
        Assert.Equal(["Guest", "Admin"], role.Options!.Select(o => o.Value));
        Assert.Equal(["Visitor", "Boss"], role.Options!.Select(o => o.Label));
        var age = schema.Node("Age");
        Assert.Equal((18d, 120d), (age.Min, age.Max));
        Assert.Equal(FormValidationTrigger.Input, age.Trigger);
    }

    [Fact]
    public void A_typed_rule_gets_the_value_of_the_member_type()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Age, f => f.Rule(a => a >= 18, "Must be an adult")));
        var controller = new FormController<Person>(schema);
        var age = schema.Field(p => p.Age);
        controller.Set(age, 12, markTouched: true);
        Assert.Equal("Must be an adult", controller.GetError(age));
        controller.Set(age, 30, markTouched: true);
        Assert.Null(controller.GetError(age));
    }

    [Fact]
    public void Nested_members_use_a_dotted_key_whether_declared_by_chain_or_with_Group()
    {
        var chain = FormSchema.For<Person>(s => s.Field(p => p.Address!.City));
        Assert.Equal("Address.City", chain.Definition.Fields.Single().Key);

        var nested = FormSchema.For<Person>(s => s.Group(p => p.Address!, g =>
        {
            g.Field(a => a.City, f => f.Label("Town"));
            g.Field(a => a.Zip);
        }, label: "Address"));
        Assert.Equal(["Address.City", "Address.Zip"], nested.Keys());
        Assert.Equal("Town", nested.Node("Address.City").Label);
        var group = nested.Definition.Fields.Single();
        Assert.Equal(FormFieldKind.Group, group.Kind);
        Assert.Equal("Address", group.Label);
    }

    [Fact]
    public void Group_creates_the_object_when_a_value_is_written()
    {
        FormField<Person, string?> city = null!;
        var schema = FormSchema.For<Person>(s => s.Group(p => p.Address!, g => city = g.Field(a => a.City)));
        var controller = new FormController<Person>(schema);
        controller.Set(city, "Lyon");
        var person = controller.CreateModel();
        Assert.Equal("Lyon", person.Address!.City);
    }

    [Fact]
    public void A_chain_through_a_null_object_that_nothing_can_create_says_so()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Address!.City));
        var controller = new FormController<Person>(schema);
        controller.SetRaw("Address.City", "Lyon");
        var error = Assert.Throws<InvalidOperationException>(() => controller.CreateModel());
        Assert.Contains("Address", error.Message);
        // an object that exists is simply written through
        var person = new Person { Address = new Address() };
        controller.ApplyTo(person);
        Assert.Equal("Lyon", person.Address.City);
    }

    [Fact]
    public void Round_trip_keeps_every_supported_type()
    {
        var schema = AllMembers();
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
        var controller = new FormController<Person>(schema, source);
        Assert.False(controller.IsDirty);
        var copy = controller.CreateModel();

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
    public void The_typed_handles_read_what_the_model_holds()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Age);
            s.Field(p => p.Children);
            s.Field(p => p.Role);
            s.Field(p => p.Birthday);
            s.Field(p => p.Tags);
        });
        var controller = new FormController<Person>(schema, new Person { Age = 36, Children = 2, Role = Role.Admin, Birthday = new DateOnly(1990, 5, 17), Tags = ["a", "b"] });
        Assert.Equal(36, controller.Get(schema.Field(p => p.Age)));
        Assert.Equal(2, controller.Get(schema.Field(p => p.Children)));
        Assert.Equal(Role.Admin, controller.Get(schema.Field(p => p.Role)));
        Assert.Equal(new DateOnly(1990, 5, 17), controller.Get(schema.Field(p => p.Birthday)));
        Assert.Equal(["a", "b"], controller.Get(schema.Field(p => p.Tags)));

        controller.Set(schema.Field(p => p.Children), null);
        Assert.Null(controller.Get(schema.Field(p => p.Children)));
        Assert.True(controller.IsDirty);
    }

    [Fact]
    public void TryGet_is_false_while_the_text_of_a_control_is_not_a_value()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Age));
        var age = schema.Field(p => p.Age);
        var controller = new FormController<Person>(schema, new Person { Age = 5 });
        Assert.True(controller.TryGet(age, out var value));
        Assert.Equal(5, value);

        controller.SetRaw("Age", 2.5);
        Assert.False(controller.TryGet(age, out value));
        Assert.Equal(0, value);

        controller.SetRaw("Age", null);
        Assert.False(controller.TryGet(age, out _));
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
            var schema = AllMembers();
            var controller = new FormController<Person>(schema, new Person { Salary = 1234.5m, Height = 1.5 });
            Assert.Equal(1234.5, controller.Raw("Salary"));
            controller.Set(schema.Field(p => p.Salary), 99.99m);
            var person = controller.CreateModel();
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
        var controller = new FormController<Person>(AllMembers(), new Person
        {
            Name = "x",
            Nickname = "x",
            Age = 5,
            Children = 3,
            Backup = Role.Admin,
            Appointment = DateTime.Now,
            Height = 1.0,
        });
        controller.SetRaw("Name", "");
        controller.SetRaw("Nickname", "");
        controller.SetRaw("Age", null);
        controller.SetRaw("Children", null);
        controller.SetRaw("Backup", "");
        controller.SetRaw("Appointment", "");
        controller.SetRaw("Height", null);
        controller.SetRaw("Role", "");
        var person = controller.CreateModel();
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
    public void ApplyTo_updates_the_target_and_leaves_hidden_and_undeclared_members_alone()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            var name = s.Field(p => p.Name);
            s.Field(p => p.Nickname, f => f.VisibleWhen(c => c.Get(name) == "show"));
        });
        var target = new Person { Name = "old", Nickname = "keep", Age = 41 };
        var controller = new FormController<Person>(schema, target);
        controller.Set(schema.Field(p => p.Name), "new");
        var returned = controller.ApplyTo(target);
        Assert.Same(target, returned);
        Assert.Equal("new", target.Name);
        Assert.Equal("keep", target.Nickname);
        Assert.Equal(41, target.Age);
    }

    [Fact]
    public void A_date_time_offset_keeps_the_offset_of_the_member()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Created));
        var target = new Person { Created = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(2)) };
        var controller = new FormController<Person>(schema, target);
        Assert.Equal("2026-06-01T10:00", controller.Raw("Created"));
        controller.SetRaw("Created", "2026-06-02T11:30");
        controller.ApplyTo(target);
        Assert.Equal(new DateTimeOffset(2026, 6, 2, 11, 30, 0, TimeSpan.FromHours(2)), target.Created);
    }

    [Fact]
    public void A_date_member_edited_as_a_date_time_and_a_date_time_as_a_date_convert()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Appointment, f => f.Kind(FormFieldKind.Date)));
        var controller = new FormController<Person>(schema, new Person { Appointment = new DateTime(2026, 3, 4, 14, 30, 0) });
        Assert.Equal("2026-03-04", controller.Raw("Appointment"));
        controller.SetRaw("Appointment", "2026-04-05");
        Assert.Equal(new DateTime(2026, 4, 5), controller.CreateModel().Appointment);
    }

    [Fact]
    public void A_new_controller_starts_from_the_initializers_of_the_model()
    {
        var schema = FormSchema.For<Address>(s => s.Field(a => a.Zip));
        Assert.Equal("", new FormController<Address>(schema).Raw("Zip"));
        var withDefault = FormSchema.For<Defaulted>(s => s.Field(d => d.Plan));
        Assert.Equal("free", new FormController<Defaulted>(withDefault).Raw("Plan"));
    }

    [Fact]
    public void A_declared_default_wins_when_no_model_is_given_and_the_model_wins_when_there_is_one()
    {
        var schema = FormSchema.For<Defaulted>(s => s.Field(d => d.Plan, f => f.Default("pro")));
        Assert.Equal("pro", new FormController<Defaulted>(schema).Raw("Plan"));
        Assert.Equal("team", new FormController<Defaulted>(schema, new Defaulted { Plan = "team" }).Raw("Plan"));
    }

    public sealed class Defaulted
    {
        public string Plan { get; set; } = "free";
    }

    [Fact]
    public void A_record_without_a_parameterless_constructor_needs_a_factory()
    {
        var plain = FormSchema.For<Point>(s => s.Field(p => p.Label));
        Assert.Throws<InvalidOperationException>(() => plain.Create(new Dictionary<string, object?>()));
        Assert.Throws<InvalidOperationException>(() => new FormController<Point>(plain).CreateModel());

        FormField<Point, int> x = null!;
        var schema = FormSchema.For<Point>(s =>
        {
            s.Factory(() => new Point(0, 0));
            x = s.Field(p => p.X);
            s.Field(p => p.Y);
            s.Field(p => p.Label);
        });
        var controller = new FormController<Point>(schema, new Point(1, 2) { Label = "a" });
        controller.Set(x, 10);
        var point = controller.CreateModel();
        Assert.Equal(new Point(10, 2) { Label = "a" }, point);
    }

    [Fact]
    public void A_field_with_no_member_is_in_the_form_and_out_of_the_model()
    {
        FormField<Person, bool> terms = null!;
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Name);
            terms = s.Field<bool>("terms", f => f.Required());
            s.Field(p => p.Age);
        });
        Assert.Equal(["Name", "terms", "Age"], schema.Keys());
        var controller = new FormController<Person>(schema);
        Assert.False(controller.Validate());
        controller.Set(terms, true);
        Assert.True(controller.Validate());
        Assert.Equal(0, controller.CreateModel().Age);
    }

    [Fact]
    public void Fields_go_into_the_steps_of_a_schema()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Step("one", "One", st => st.Field(p => p.Name));
            s.Step("two", "Two", st => st.Field(p => p.Age));
        });
        var controller = new FormController<Person>(schema);
        Assert.True(controller.IsStepped);
        Assert.Equal("one", controller.CurrentStepKey);
        Assert.Equal(["Name"], controller.Engine.CurrentStep!.Fields.Select(f => f.Key));
    }

    [Fact]
    public void The_order_function_sorts_the_fields()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Name);
            s.Field(p => p.Age);
            s.Field(p => p.Nickname);
            s.Order(m => m.Name == nameof(Person.Nickname) ? 1 : null);
        });
        Assert.Equal(["Nickname", "Name", "Age"], schema.Keys());
    }

    [Fact]
    public void A_convention_runs_before_the_explicit_settings()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Convention(c => c.SetLabel("from convention " + c.Member.Key));
            s.Field(p => p.Name);
            s.Field(p => p.Nickname, f => f.Label("mine"));
        });
        Assert.Equal("from convention Name", schema.Node("Name").Label);
        Assert.Equal("mine", schema.Node("Nickname").Label);
    }

    [Fact]
    public async Task Model_checks_put_errors_on_fields_and_on_the_form_and_run_again_at_the_next_submit()
    {
        FormField<Person, string> name = null!;
        FormField<Person, int> age = null!;
        FormField<Person, string?> city = null!;
        var schema = FormSchema.For<Person>(s =>
        {
            name = s.Field(p => p.Name);
            age = s.Field(p => p.Age);
            s.Group(p => p.Address!, g => city = g.Field(a => a.City));
            s.Validate((p, errors) =>
            {
                if (p.Name == "root") errors.Add(m => m.Name, "Reserved name");
                if (p.Address?.City == "Nowhere") errors.Add("City", "Unknown city");
                if (p.Age == 13) errors.Add("Thirteen is not allowed");
                if (p.Age == 14) errors.Add("Nothing", "Goes to the form");
            });
        });
        var controller = new FormController<Person>(schema);
        controller.Set(name, "root");
        controller.Set(city, "Nowhere");
        controller.Set(age, 13);

        Assert.False(await controller.SubmitAsync());
        Assert.Equal("Reserved name", controller.GetError(name));
        Assert.Equal("Unknown city", controller.GetError(city));
        Assert.Contains(controller.Errors, e => e.Value == "Thirteen is not allowed");

        controller.Set(name, "ada");
        controller.Set(city, "Lyon");
        controller.Set(age, 14);
        Assert.False(await controller.SubmitAsync());
        Assert.DoesNotContain(controller.Errors, e => e.Value == "Thirteen is not allowed");
        Assert.Contains(controller.Errors, e => e.Value == "Goes to the form");

        controller.Set(age, 30);
        Assert.True(await controller.SubmitAsync());
        Assert.Empty(controller.Errors);
    }

    [Fact]
    public async Task The_typed_submit_hands_over_the_model_through_the_submit_lifecycle()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Name, f => f.Required());
            s.Field(p => p.Age);
        });
        var controller = new FormController<Person>(schema);
        Person? saved = null;
        var ct = Xunit.TestContext.Current.CancellationToken;

        Assert.False(await controller.SubmitAsync((p, _) => { saved = p; return Task.FromResult(true); }, cancellationToken: ct));
        Assert.Null(saved);

        controller.Set(schema.Field(p => p.Name), "Ada");
        controller.Set(schema.Field(p => p.Age), 36);
        Assert.False(await controller.SubmitAsync((p, _) => { saved = p; return Task.FromResult(false); }, cancellationToken: ct));
        Assert.True(controller.IsDirty);
        Assert.Equal("Ada", saved!.Name);

        var target = new Person { Name = "old", Age = 1 };
        Assert.True(await controller.SubmitAsync((p, _) => { saved = p; return Task.FromResult(true); }, target, ct));
        Assert.Same(target, saved);
        Assert.Equal(36, target.Age);
        Assert.False(controller.IsDirty);
    }

    [Fact]
    public async Task The_hooks_run_in_order_after_the_checks_and_before_the_save()
    {
        var calls = new List<string>();
        var schema = FormSchema.For<Person>(s =>
        {
            s.Field(p => p.Name);
            s.Validate((_, _) => calls.Add("validate"));
            s.OnBeforeSubmit((_, _) => { calls.Add("before"); return Task.FromResult(true); });
            s.OnSubmit((_, _) => { calls.Add("submit"); return Task.CompletedTask; });
        });
        var controller = new FormController<Person>(schema);
        Assert.True(await controller.SubmitAsync((_, _) => { calls.Add("save"); return Task.FromResult(true); }, cancellationToken: Xunit.TestContext.Current.CancellationToken));
        Assert.Equal(["validate", "before", "submit", "save"], calls);

        calls.Clear();
        var refusing = FormSchema.For<Person>(s => s.OnBeforeSubmit((_, _) => Task.FromResult(false)));
        Assert.False(await new FormController<Person>(refusing).SubmitAsync((_, _) => { calls.Add("save"); return Task.FromResult(true); }, cancellationToken: Xunit.TestContext.Current.CancellationToken));
        Assert.Empty(calls);
    }

    [Fact]
    public void Reset_with_a_model_replaces_the_values_and_the_baseline()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Name));
        var name = schema.Field(p => p.Name);
        var controller = new FormController<Person>(schema);
        controller.Set(name, "typed");
        Assert.True(controller.IsDirty);
        controller.Reset(new Person { Name = "loaded" });
        Assert.Equal("loaded", controller.Get(name));
        Assert.False(controller.IsDirty);
    }

    [Fact]
    public void Reset_without_a_model_goes_back_to_the_baseline()
    {
        var schema = FormSchema.For<Person>(s => s.Field(p => p.Name));
        var name = schema.Field(p => p.Name);
        var controller = new FormController<Person>(schema, new Person { Name = "start" });
        controller.Set(name, "typed");
        controller.Reset();
        Assert.Equal("start", controller.Get(name));
        Assert.False(controller.IsDirty);
    }

    [Fact]
    public void Bad_declarations_are_refused()
    {
        Assert.Throws<ArgumentException>(() => FormSchema.For<Person>(s => s.Field(p => p.Name.ToUpper())));
        Assert.Throws<ArgumentException>(() => FormSchema.For<Person>(s => { s.Field(p => p.Name); s.Field(p => p.Name); }));
        Assert.Throws<ArgumentException>(() => FormSchema.For<Person>(s => s.Field(p => p.Address)));
        var ok = FormSchema.For<Person>(s => s.Field(p => p.Address, f => f.Kind(FormFieldKind.Custom)));
        Assert.Equal(FormFieldKind.Custom, ok.Leaves().Single().Kind);
    }

    [Fact]
    public void A_field_looked_up_by_member_is_the_one_that_was_declared()
    {
        FormField<Person, string> declared = null!;
        var schema = FormSchema.For<Person>(s => declared = s.Field(p => p.Name));
        Assert.Same(declared, schema.Field(p => p.Name));
        Assert.Throws<ArgumentException>(() => schema.Field(p => p.Age));
    }

    [Fact]
    public void A_condition_can_point_at_an_earlier_field()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            var active = s.Field(p => p.Active);
            s.Field(p => p.Nickname, f => f.VisibleWhen(c => c.Get(active)).Required(c => c.Get(active)));
        });
        var controller = new FormController<Person>(schema);
        var nickname = schema.Field(p => p.Nickname);
        Assert.False(controller.IsVisible(nickname));
        controller.Set(schema.Field(p => p.Active), true);
        Assert.True(controller.IsVisible(nickname));
        Assert.True(controller.IsRequired(nickname));
        Assert.False(controller.Validate());
        Assert.Equal("This field is required", controller.GetError(nickname));
    }
}
