using Xunit;

namespace Klyroo.Form.Tests;

public sealed class SchemaHandleTests
{
    private static FormSchema<Basket> Declare(out FormField<Basket, IReadOnlyList<Role>> roles, out FormField<Basket, IReadOnlyList<Role>> admins)
    {
        FormField<Basket, IReadOnlyList<Role>> declaredRoles = null!;
        FormField<Basket, IReadOnlyList<Role>> declaredAdmins = null!;
        var schema = FormSchema.For<Basket>(s =>
        {
            declaredRoles = s.MultiSelect(b => b.Roles, [new FormOption<Role>(Role.Guest, "Guest"), new FormOption<Role>(Role.Admin, "Boss")]);
            declaredAdmins = s.MultiSelect(b => b.Admins, [new FormOption<Role>(Role.Admin, "Boss")]);
        });
        roles = declaredRoles;
        admins = declaredAdmins;
        return schema;
    }

    [Fact]
    public void The_handle_of_a_multiple_choice_is_got_from_the_schema_with_MultiSelect()
    {
        var schema = Declare(out var roles, out var admins);

        Assert.Same(roles, schema.MultiSelect(b => b.Roles));
        Assert.Same(admins, schema.MultiSelect(b => b.Admins));

        var controller = new FormController<Basket>(schema, new Basket { Roles = [Role.Admin] });
        var found = schema.MultiSelect(b => b.Roles);
        Assert.Equal([Role.Admin], controller.Get(found));
        controller.Set(found, [Role.Guest, Role.Admin]);
        Assert.Equal([Role.Guest, Role.Admin], controller.CreateModel().Roles);
    }

    [Fact]
    public void The_handle_of_a_multiple_choice_is_also_got_with_Field_typed_as_the_list_of_its_values()
    {
        var schema = Declare(out var roles, out _);

        Assert.Same(roles, schema.Field<IReadOnlyList<Role>>(b => b.Roles));
    }

    [Fact]
    public void Field_on_a_collection_member_says_which_method_to_use()
    {
        var schema = Declare(out _, out _);

        var error = Assert.Throws<ArgumentException>(() => schema.Field(b => b.Roles));

        Assert.Contains("MultiSelect", error.Message, StringComparison.Ordinal);
        Assert.Contains("List", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MultiSelect_on_a_member_that_is_not_a_multiple_choice_is_refused()
    {
        var schema = FormSchema.For<Basket>(s =>
        {
            s.Field(b => b.Owner);
            s.List(b => b.Lines, row => row.Field(l => l.Product));
        });

        Assert.Throws<ArgumentException>(() => schema.MultiSelect(b => b.Roles));
        Assert.Throws<ArgumentException>(() => schema.MultiSelect<Line>(b => b.Lines));
    }
}
