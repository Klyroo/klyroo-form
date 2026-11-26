using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Booking
{
    public DateOnly Day { get; set; }

    public DateOnly? Optional { get; set; }

    public DateTimeOffset At { get; set; }

    public FormDateRange Range { get; set; }

    public List<Visit> Visits { get; set; } = [];
}

public sealed class Visit
{
    public DateOnly On { get; set; }
}

public sealed class InvalidControlValueTests
{
    private static FormSchema<Booking> Schema() => FormSchema.For<Booking>(s =>
    {
        s.Field(b => b.Day);
        s.Field(b => b.Optional);
        s.Field(b => b.At);
        s.Field(b => b.Range);
    });

    private static FormDraft Draft(string values) => FormDraft.FromJson($"{{\"version\":1,\"step\":0,\"values\":{values}}}");

    private static async Task<bool> Submit(FormController<Booking> controller, Action onSave) =>
        await controller.SubmitAsync((_, _) =>
        {
            onSave();
            return Task.FromResult(true);
        }, cancellationToken: TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("{\"Day\":\"abc\"}", "Day")]
    [InlineData("{\"Optional\":\"abc\"}", "Optional")]
    [InlineData("{\"At\":\"not a moment\"}", "At")]
    [InlineData("{\"Range\":[\"x\",\"y\"]}", "Range")]
    public async Task A_draft_with_text_that_is_not_a_value_restores_then_fails_validation_and_never_throws_on_submit(string values, string key)
    {
        var schema = Schema();
        var controller = new FormController<Booking>(schema);
        var saved = false;

        controller.RestoreDraft(Draft(values));

        Assert.False(controller.Validate());
        Assert.NotNull(controller.GetError(schema.Item(key)));
        Assert.False(await Submit(controller, () => saved = true));
        Assert.False(saved);
        Assert.True(controller.HasBlockingErrors);
    }

    [Fact]
    public void The_error_is_the_localized_message_of_the_controller()
    {
        var schema = Schema();
        var controller = new FormController<Booking>(schema);
        controller.SetRaw("Day", "abc");

        Assert.False(controller.Validate());
        Assert.Equal(FormMessages.Default.InvalidValue, controller.GetError(schema.Item("Day")));

        controller.Messages = FormMessageLocales.Fr;
        Assert.False(controller.Validate());
        Assert.Equal("Saisissez une valeur valide", controller.GetError(schema.Item("Day")));
    }

    [Fact]
    public async Task A_control_that_holds_free_text_is_a_validation_error_not_an_exception()
    {
        var schema = Schema();
        var controller = new FormController<Booking>(schema);
        controller.SetRaw("Day", "next tuesday", markTouched: true);

        Assert.False(controller.Validate());
        Assert.NotNull(controller.GetError(schema.Item("Day")));
        Assert.False(await Submit(controller, () => { }));
    }

    [Fact]
    public async Task The_error_clears_when_the_value_is_fixed_and_the_form_submits()
    {
        var schema = Schema();
        var controller = new FormController<Booking>(schema);
        controller.RestoreDraft(Draft("{\"Day\":\"abc\"}"));
        Assert.False(controller.Validate());

        controller.Set(schema.Field(b => b.Day), new DateOnly(2026, 3, 27), markTouched: true);

        Assert.True(controller.Validate());
        Assert.True(await Submit(controller, () => { }));
    }

    [Fact]
    public async Task The_check_does_not_depend_on_the_validation_mode()
    {
        var schema = FormSchema.For<Booking>(s =>
        {
            s.ValidateMode = FormValidationMode.None;
            s.Field(b => b.Day);
        });
        var controller = new FormController<Booking>(schema);
        controller.SetRaw("Day", "abc");

        Assert.False(controller.Validate());
        Assert.False(await controller.SubmitAsync((_, _) => Task.FromResult(true), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_cell_of_a_list_that_is_not_a_value_is_a_cell_error_and_the_submit_does_not_throw()
    {
        FormListField<Booking, Visit> visits = null!;
        FormField<Visit, DateOnly> on = null!;
        var schema = FormSchema.For<Booking>(s => visits = s.List(b => b.Visits, row => on = row.Field(v => v.On)));
        var controller = new FormController<Booking>(schema);
        controller.AddRow(visits);
        controller.Engine.SetArrayItemValue("Visits", 0, "On", "abc");

        Assert.False(controller.Validate());
        Assert.NotNull(controller.Rows(visits)[0].GetError(on));
        Assert.False(await Submit(controller, () => { }));
    }

    [Fact]
    public void A_value_that_is_there_and_valid_passes()
    {
        var schema = Schema();
        var controller = new FormController<Booking>(schema);
        controller.RestoreDraft(Draft("{\"Day\":\"2026-03-27\",\"At\":\"2026-03-27T10:00\",\"Range\":[\"2026-03-01\",\"2026-03-05\"]}"));

        Assert.True(controller.Validate());
        Assert.Equal(new DateOnly(2026, 3, 27), controller.Get(schema.Field(b => b.Day)));
    }
}

public sealed class GetOnInvalidCollectionTests
{
    [Fact]
    public void Get_on_a_list_or_a_multiple_choice_that_is_not_a_value_gives_an_empty_list_and_TryGet_says_false()
    {
        FormListField<Basket, Line> lines = null!;
        FormField<Basket, IReadOnlyList<Role>> roles = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            lines = s.List(b => b.Lines, row => row.Field(l => l.Product));
            roles = s.MultiSelect(b => b.Roles, [new FormOption<Role>(Role.Guest, "Guest")]);
        });
        var controller = new FormController<Basket>(schema);
        controller.SetRaw("Lines", "abc");
        controller.SetRaw("Roles", 5);

        Assert.NotNull(controller.Get(lines));
        Assert.Empty(controller.Get(lines)!);
        Assert.False(controller.TryGet(lines, out var rows));
        Assert.Empty(rows!);
        Assert.NotNull(controller.Get(roles));
        Assert.Empty(controller.Get(roles)!);
        Assert.False(controller.TryGet(roles, out var chosen));
        Assert.Empty(chosen!);
    }

    [Fact]
    public void Get_on_an_empty_list_gives_an_empty_list_and_TryGet_is_false_like_for_any_empty_field()
    {
        FormField<Basket, IReadOnlyList<Role>> roles = null!;
        var schema = FormSchema.For<Basket>(s => roles = s.MultiSelect(b => b.Roles, [new FormOption<Role>(Role.Guest, "Guest")]));
        var controller = new FormController<Basket>(schema);

        Assert.Empty(controller.Get(roles)!);
        Assert.False(controller.TryGet(roles, out _));

        controller.Set(roles, [Role.Guest]);
        Assert.True(controller.TryGet(roles, out var value));
        Assert.Equal([Role.Guest], value);
    }

    [Fact]
    public void Get_on_a_scalar_that_is_not_a_value_gives_the_default_of_the_type()
    {
        var schema = FormSchema.For<Basket>(s => s.Field(b => b.Stars));
        var controller = new FormController<Basket>(schema);
        controller.SetRaw("Stars", "abc");

        Assert.Equal(0, controller.Get(schema.Field(b => b.Stars)));
        Assert.False(controller.TryGet(schema.Field(b => b.Stars), out _));
    }
}
