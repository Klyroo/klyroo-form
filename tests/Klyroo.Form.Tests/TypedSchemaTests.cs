using System.Text.Json;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Line
{
    public string Product { get; set; } = "";

    public int Qty { get; set; } = 1;

    public decimal? Discount { get; set; }
}

public sealed class Basket
{
    public string Owner { get; set; } = "";

    public List<Line> Lines { get; set; } = [];

    public Line[] Spare { get; set; } = [];

    public IReadOnlyList<Line> Frozen { get; set; } = [];

    public List<Role> Roles { get; set; } = [];

    public Role[] Admins { get; set; } = [];

    public FormDateRange Stay { get; set; }

    public FormFileValue? Photo { get; set; }

    public int Stars { get; set; } = 3;

    public bool Notify { get; set; }

    public int? Seats { get; set; }

    public string Color { get; set; } = "#000000";

    public string Secret { get; set; } = "";
}

public sealed class TypedSchemaTests
{
    [Fact]
    public void A_list_is_edited_as_rows_and_written_back_as_items()
    {
        FormListField<Basket, Line> lines = null!;
        FormField<Line, string> product = null!;
        FormField<Line, int> qty = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            lines = s.List(b => b.Lines, row =>
            {
                product = row.Field(l => l.Product, f => f.Required());
                qty = row.Field(l => l.Qty, f => f.Range(1, 999));
                row.Field(l => l.Discount);
            }, addLabel: "Add line");
        });
        Assert.Equal("Add line", schema.Node("Lines").AddItemLabel);
        Assert.Equal(FormFieldKind.ArrayList, schema.Node("Lines").Kind);
        Assert.Equal(["Product", "Qty", "Discount"], schema.Node("Lines").Fields!.Select(f => f.Key));

        var controller = new FormController<Basket>(schema, new Basket { Lines = [new Line { Product = "pen", Qty = 2 }] });
        var rows = controller.Rows(lines);
        Assert.Single(rows);
        Assert.Equal("pen", rows[0].Get(product));
        Assert.Equal(2, rows[0].Get(qty));

        controller.AddRow(lines, new Line { Product = "ink", Qty = 5, Discount = 0.5m });
        controller.AddRow(lines);
        rows = controller.Rows(lines);
        Assert.Equal(3, rows.Count);
        Assert.Equal("ink", rows[1].Get(product));

        Assert.False(controller.Validate());
        rows[2].Set(product, "cap");
        rows[2].Set(qty, 7);
        Assert.True(controller.Validate());

        var model = controller.CreateModel();
        Assert.Equal(["pen", "ink", "cap"], model.Lines.Select(l => l.Product));
        Assert.Equal([2, 5, 7], model.Lines.Select(l => l.Qty));
        Assert.Equal(0.5m, model.Lines[1].Discount);
        Assert.Equal(new[] { "pen", "ink", "cap" }, controller.Get(lines)!.Select(l => l.Product));

        controller.RemoveRow(lines, 0);
        Assert.Equal(["ink", "cap"], controller.CreateModel().Lines.Select(l => l.Product));
    }

    [Fact]
    public void A_cell_is_validated_with_the_rules_of_its_column_and_reports_by_row()
    {
        FormListField<Basket, Line> lines = null!;
        FormField<Line, int> qty = null!;
        var schema = FormSchema.For<Basket>(s => lines = s.List(b => b.Lines, row => qty = row.Field(l => l.Qty, f => f.Range(1, 9))));
        var controller = new FormController<Basket>(schema, new Basket { Lines = [new Line { Qty = 3 }, new Line { Qty = 50 }] });
        Assert.False(controller.Validate());
        var rows = controller.Rows(lines);
        Assert.Null(rows[0].GetError(qty));
        Assert.Equal(FormMessages.Default.MaxValue(9), rows[1].GetError(qty));
    }

    [Fact]
    public void A_list_member_can_be_an_array_or_a_read_only_list_and_the_shape_is_kept()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            s.List(b => b.Spare, row => row.Field(l => l.Product));
            s.List(b => b.Frozen, row => row.Field(l => l.Product));
        });
        var source = new Basket { Spare = [new Line { Product = "a" }], Frozen = [new Line { Product = "b" }] };
        var model = new FormController<Basket>(schema, source).CreateModel();
        Assert.Equal(["a"], model.Spare.Select(l => l.Product));
        Assert.Equal(["b"], model.Frozen.Select(l => l.Product));
        Assert.IsType<Line[]>(model.Spare);
    }

    [Fact]
    public void A_list_of_an_unsupported_collection_type_is_refused()
    {
        Assert.Throws<ArgumentException>(() => FormSchema.For<Unsupported>(s => s.List(u => u.Lines, row => row.Field(l => l.Product))));
    }

    public sealed class Unsupported
    {
        public HashSet<Line> Lines { get; set; } = [];
    }

    [Fact]
    public void A_multiple_choice_holds_the_values_as_the_element_type()
    {
        FormField<Basket, IReadOnlyList<Role>> roles = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            roles = s.MultiSelect(b => b.Roles, [new FormOption<Role>(Role.Guest, "Guest"), new FormOption<Role>(Role.Admin, "Boss")]);
            s.MultiSelect(b => b.Admins, [new FormOption<Role>(Role.Admin, "Boss")]);
        });
        var node = schema.Node("Roles");
        Assert.Equal(FormFieldKind.Select, node.Kind);
        Assert.True(node.Multiple);
        Assert.Equal(["Guest", "Admin"], node.Options!.Select(o => o.Value));

        var controller = new FormController<Basket>(schema, new Basket { Roles = [Role.Admin], Admins = [Role.Admin] });
        Assert.Equal([Role.Admin], controller.Get(roles));
        Assert.Equal(["Admin"], Assert.IsType<List<string>>(controller.Raw("Roles")));

        controller.Set(roles, [Role.Guest, Role.Admin]);
        var model = controller.CreateModel();
        Assert.Equal([Role.Guest, Role.Admin], model.Roles);
        Assert.IsType<Role[]>(model.Admins);
    }

    [Fact]
    public void Options_have_the_values_of_the_field_type_and_a_wrong_text_is_not_a_value()
    {
        FormField<Basket, int?> seats = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            seats = s.Field(b => b.Seats, f => f.Options([new FormOption<int>(1, "One"), new FormOption<int>(5, "Five", Description: "Team")]));
        });
        var node = schema.Node("Seats");
        Assert.Equal(FormFieldKind.Select, node.Kind);
        Assert.Equal(["1", "5"], node.Options!.Select(o => o.Value));
        Assert.Equal("Team", node.Options![1].Description);

        var controller = new FormController<Basket>(schema);
        controller.SetRaw("Seats", "5");
        Assert.Equal(5, controller.Get(seats));
        controller.SetRaw("Seats", "five");
        Assert.False(controller.TryGet(seats, out _));
        controller.Set(seats, 1);
        Assert.Equal("1", controller.Raw("Seats"));
    }

    [Fact]
    public void Options_computed_from_the_controller_follow_another_field()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            var notify = s.Field(b => b.Notify);
            s.Field(b => b.Seats, f => f.Options(c => c.Get(notify) ? [new FormOption<int>(1, "One")] : [new FormOption<int>(2, "Two")]));
        });
        var controller = new FormController<Basket>(schema);
        var seats = schema.Field(b => b.Seats);
        Assert.Equal(["2"], controller.GetOptions(seats).Select(o => o.Value));
        controller.Set(schema.Field(b => b.Notify), true);
        Assert.Equal(["1"], controller.GetOptions(seats).Select(o => o.Value));
    }

    [Fact]
    public void Kind_specific_settings_pick_the_control()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            s.Field(b => b.Owner, f => f.AsTextarea().MaxLength(50));
            s.Field(b => b.Secret, f => f.AsPassword(new FormPasswordRequirement("len", "At least 8 characters", p => p.Length >= 8)));
            s.Field(b => b.Color, f => f.AsColor());
            s.Field(b => b.Notify, f => f.AsSwitch());
            s.Field(b => b.Seats, f => f.AsSlider().Range(1, 10).StepBy(1));
            s.Field(b => b.Stars, f => f.AsRating(7));
        });
        Assert.Equal(FormFieldKind.Textarea, schema.Node("Owner").Kind);
        Assert.Equal(50, schema.Node("Owner").MaxLength);
        Assert.Equal(FormFieldKind.Password, schema.Node("Secret").Kind);
        Assert.Single(schema.Node("Secret").PasswordRequirements!);
        Assert.Equal(FormFieldKind.ColorPicker, schema.Node("Color").Kind);
        Assert.Equal(FormFieldKind.Switch, schema.Node("Notify").Kind);
        var slider = schema.Node("Seats");
        Assert.Equal(FormFieldKind.Slider, slider.Kind);
        Assert.Equal((1d, 10d, 1d), (slider.Min, slider.Max, slider.Step));
        Assert.Equal(FormFieldKind.Rating, schema.Node("Stars").Kind);
        Assert.Equal(7, schema.Node("Stars").Max);

        var controller = new FormController<Basket>(schema, new Basket { Stars = 4 });
        Assert.Equal(4, controller.Get(schema.Field(b => b.Stars)));
        Assert.IsType<int>(controller.Raw("Stars"));
        controller.Set(schema.Field(b => b.Secret), "short", markTouched: true);
        Assert.False(controller.Validate());
        Assert.Equal("At least 8 characters", controller.GetError(schema.Field(b => b.Secret)));
    }

    [Fact]
    public void String_settings_fit_a_nullable_string_member_too()
    {
        var schema = FormSchema.For<Notes>(s =>
        {
            s.Field(n => n.Optional, f => f.AsTextarea().MaxLength(5));
            s.Field(n => n.Plain, f => f.AsPassword().MaxLength(9));
        });
        Assert.Equal((FormFieldKind.Textarea, 5), (schema.Node("Optional").Kind, schema.Node("Optional").MaxLength));
        Assert.Equal((FormFieldKind.Password, 9), (schema.Node("Plain").Kind, schema.Node("Plain").MaxLength));
    }

    [Fact]
    public void A_date_range_and_a_file_have_their_own_value_types()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            s.Field(b => b.Stay);
            s.Field(b => b.Photo, f => f.Accept("image/*").MaxFileSize(1000));
        });
        Assert.Equal(FormFieldKind.DateRange, schema.Node("Stay").Kind);
        Assert.Equal(FormFieldKind.File, schema.Node("Photo").Kind);
        Assert.Equal("image/*", schema.Node("Photo").Accept);

        var stay = schema.Field(b => b.Stay);
        var controller = new FormController<Basket>(schema, new Basket { Stay = new FormDateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 8)) });
        Assert.Equal(["2026-01-01", "2026-01-08"], Assert.IsType<List<string>>(controller.Raw("Stay")));
        Assert.Equal(new DateOnly(2026, 1, 8), controller.Get(stay).End);

        controller.Set(stay, new FormDateRange(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)), markTouched: true);
        Assert.False(controller.Validate());
        Assert.Equal(FormMessages.Default.DateRangeOrder, controller.GetError(stay));
        controller.Set(stay, default);
        Assert.Null(controller.Raw("Stay"));
        Assert.Equal(default, controller.CreateModel().Stay);
    }

    [Fact]
    public void Defaults_transforms_and_change_hooks_work_on_typed_values()
    {
        var changes = new List<string>();
        FormField<Basket, string> owner = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            owner = s.Field(b => b.Owner, f => f
                .TransformOutput(v => v.Trim())
                .OnChange((v, _) => changes.Add(v)));
            s.Field(b => b.Seats, f => f.Default(4));
            s.Field(b => b.Stars, f => f.Default(c => c.Get(owner) == "" ? 9 : 1));
        });
        var controller = new FormController<Basket>(schema, new Basket { Seats = null });
        Assert.Equal(4, controller.Get(schema.Field(b => b.Seats)));
        controller.Set(schema.Field(b => b.Owner), "  Ada  ");
        Assert.Equal(["  Ada  "], changes);
        Assert.Equal("Ada", controller.CreateModel().Owner);
    }

    [Fact]
    public async Task An_asynchronous_rule_gets_the_typed_value()
    {
        var schema = FormSchema.For<Basket>(s => s.Field(b => b.Owner, f => f.RuleAsync(async (v, _, ct) =>
        {
            await Task.Yield();
            return v == "taken" ? "Already used" : null;
        }, TimeSpan.Zero)));
        var owner = schema.Field(b => b.Owner);
        var controller = new FormController<Basket>(schema);
        controller.Set(owner, "taken", markTouched: true);
        Assert.False(await controller.ValidateAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken));
        Assert.Equal("Already used", controller.GetError(owner));
        controller.Set(owner, "free", markTouched: true);
        Assert.True(await controller.ValidateAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public void A_draft_round_trips_and_leaves_out_the_secrets()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            s.Field(b => b.Owner);
            s.Field(b => b.Secret, f => f.AsPassword());
            s.List(b => b.Lines, row => row.Field(l => l.Product));
        });
        var controller = new FormController<Basket>(schema, new Basket { Owner = "Ada", Secret = "hunter2", Lines = [new Line { Product = "pen" }] });
        controller.Set(schema.Field(b => b.Owner), "Grace");
        var json = controller.ExportDraft().ToJson();
        Assert.DoesNotContain("hunter2", json, StringComparison.Ordinal);

        var restored = new FormController<Basket>(schema, new Basket { Owner = "Ada", Secret = "hunter2", Lines = [new Line { Product = "pen" }] });
        restored.RestoreDraft(FormDraft.FromJson(json));
        Assert.True(restored.IsDirty);
        var model = restored.CreateModel();
        Assert.Equal("Grace", model.Owner);
        Assert.Equal("hunter2", model.Secret);
        Assert.Equal(["pen"], model.Lines.Select(l => l.Product));
    }

    [Fact]
    public async Task Steps_skip_and_navigate_and_the_sections_report_their_state()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            var notify = s.Field(b => b.Notify);
            s.Step("a", "A", st => st.Field(b => b.Owner, f => f.Required()), skipWhen: c => false);
            s.Step("b", "B", st => st.Field(b => b.Seats), skipWhen: c => c.Get(notify));
            s.Step("c", "C", st => st.Field(b => b.Color));
        });
        var controller = new FormController<Basket>(schema);
        Assert.Equal("a", controller.CurrentStepKey);
        Assert.False(await controller.NextStepAsync());
        controller.Set(schema.Field(b => b.Owner), "x");
        Assert.True(await controller.NextStepAsync());
        Assert.Equal("b", controller.CurrentStepKey);
        Assert.True(await controller.PreviousStepAsync());

        var sections = FormSchema.For<Basket>(s =>
        {
            s.Section("who", "Who", sec => sec.Field(b => b.Owner, f => f.Required()), description: "Owner");
            s.Section("opt", "Optional", sec => sec.Field(b => b.Seats), optional: true);
        });
        var states = new FormController<Basket>(sections).GetSectionStates();
        Assert.Equal([FormPageSectionStatus.Pending, FormPageSectionStatus.Optional], states.Select(x => x.Status));
    }

    [Fact]
    public void Layout_elements_are_handles_a_field_can_depend_on_and_hide_with()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            var notify = s.Field(b => b.Notify);
            s.Heading("h", "Heading");
            s.Info("i", "Be careful", tone: FormInfoTone.Warning, visibleWhen: c => c.Get(notify));
            s.Divider("d");
            s.Card("card", "Card", c => c.Field(b => b.Owner), collapsible: true);
            s.Tabs("tabs", t =>
            {
                t.Tab("one", "One", g => g.Field(b => b.Color));
                t.Tab("two", "Two", g => g.Field(b => b.Seats), visibleWhen: c => c.Get(notify));
            });
        });
        Assert.Equal(["Notify", "Owner", "Color", "Seats"], schema.Keys());
        var controller = new FormController<Basket>(schema);
        Assert.False(controller.IsVisible(schema.Item("i")));
        controller.Set(schema.Field(b => b.Notify), true);
        Assert.True(controller.IsVisible(schema.Item("i")));
        Assert.Equal(FormFieldKind.Card, schema.Node("card").Kind);
        Assert.Equal(2, schema.Node("tabs").Tabs!.Count);
    }

    [Fact]
    public void A_dependent_field_validates_again_when_the_field_it_points_at_changes()
    {
        FormField<Basket, string> owner = null!;
        FormField<Basket, string> color = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            owner = s.Field(b => b.Owner);
            color = s.Field(b => b.Color, f => f.DependsOn(owner).Rule((v, c) => v == c.Get(owner), "Must match"));
        });
        var controller = new FormController<Basket>(schema);
        controller.Set(owner, "a", markTouched: true);
        controller.Set(color, "b", markTouched: true);
        Assert.Equal("Must match", controller.GetError(color));
        controller.Set(owner, "b", markTouched: true);
        Assert.Null(controller.GetError(color));
    }

    [Fact]
    public async Task Both_controllers_expose_their_fields_as_a_runtime_for_json()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            s.Field(b => b.Owner, f => f.Required());
            s.Field(b => b.Seats, f => f.Range(1, 5));
            s.Field(b => b.Secret, f => f.AsPassword());
        });
        IFormRuntime runtime = new FormController<Basket>(schema, new Basket { Owner = "Ada" });
        Assert.Equal("form", runtime.FormKey);
        Assert.False(runtime.IsDirty);

        var owner = runtime.Fields.Single(f => f.Key == "Owner");
        var seats = runtime.Fields.Single(f => f.Key == "Seats");
        Assert.True(owner.IsRequired);
        Assert.True(runtime.Fields.Single(f => f.Key == "Secret").Sensitive);

        Assert.True(seats.TrySet(JsonDocument.Parse("3").RootElement).Ok);
        Assert.True(runtime.IsDirty);
        var tooBig = seats.TrySet(JsonDocument.Parse("9").RootElement);
        Assert.False(tooBig.Ok);
        Assert.Contains("at most 5", tooBig.Error);
        var fractional = seats.TrySet(JsonDocument.Parse("2.5").RootElement);
        Assert.False(fractional.Ok);
        Assert.Contains("whole number", fractional.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(owner.TrySet(JsonDocument.Parse("\"Grace\"").RootElement).Ok);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            seats.WriteValue(writer);
        }
        Assert.Equal("3", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
        Assert.True(await runtime.SubmitAsync());
    }

    [Fact]
    public void The_codec_formats_and_parses_invariant_text()
    {
        var number = FormCodec<decimal>.For(FormFieldKind.Number, false);
        Assert.Equal("1234.5", number.Format(1234.5m));
        Assert.True(number.TryParse("1234.5", out var parsed));
        Assert.Equal(1234.5m, parsed);
        Assert.False(number.TryParse("1,5", out _));
        var guid = FormCodec<Guid>.For(FormFieldKind.Text, false);
        Assert.True(guid.TryParse(Guid.Empty.ToString(), out _));
        var date = FormCodec<DateOnly>.For(FormFieldKind.Date, false);
        Assert.Equal("2026-01-02", date.Format(new DateOnly(2026, 1, 2)));
        var role = FormCodec<Role>.For(FormFieldKind.Select, false);
        Assert.Equal("TeamMember", role.Format(Role.TeamMember));
        Assert.True(role.TryParse("admin", out var admin));
        Assert.Equal(Role.Admin, admin);
    }

    [Fact]
    public void A_schema_is_shared_between_controllers_with_their_own_state()
    {
        var schema = FormSchema.For<Basket>(s => s.Field(b => b.Owner));
        var owner = schema.Field(b => b.Owner);
        var one = new FormController<Basket>(schema, new Basket { Owner = "a" });
        var two = new FormController<Basket>(schema, new Basket { Owner = "b" });
        one.Set(owner, "changed");
        Assert.Equal("changed", one.Get(owner));
        Assert.Equal("b", two.Get(owner));
        Assert.False(two.IsDirty);
    }
}
