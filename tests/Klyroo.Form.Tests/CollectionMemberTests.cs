using Xunit;

namespace Klyroo.Form.Tests;

public sealed class GetterOnly
{
    public string Name { get; set; } = "";

    public List<Entry> Items { get; } = new();

    public List<Role> Roles { get; } = new();

    public IList<Entry> Locked { get; } = new List<Entry>().AsReadOnly();
}

public sealed class SealedLists
{
    public IReadOnlyList<Entry> Frozen { get; } = [];

    public Entry[] Array { get; } = [];

    public IEnumerable<Role> Roles { get; } = [];
}

public sealed class EntryHolder
{
    public List<Entry> Lines { get; set; } = [];

    public List<Role> Roles { get; set; } = [];
}

public sealed class Wrapper
{
    public EntryHolder Inner { get; set; } = new();
}

public sealed class CollectionMemberTests
{
    [Fact]
    public void A_getter_only_list_is_written_in_place_and_keeps_its_items()
    {
        FormListField<GetterOnly, Entry> items = null!;
        FormField<Entry, string> product = null!;
        FormField<GetterOnly, IReadOnlyList<Role>> roles = null!;
        var schema = FormSchema.For<GetterOnly>(s =>
        {
            items = s.List(g => g.Items, row => product = row.Field(e => e.Product));
            roles = s.MultiSelect(g => g.Roles, [new FormOption<Role>(Role.Guest, "Guest"), new FormOption<Role>(Role.Admin, "Boss")]);
        });
        var first = new Entry { Id = 1, Product = "a" };
        var model = new GetterOnly { Items = { first }, Roles = { Role.Guest } };
        var list = model.Items;
        var controller = new FormController<GetterOnly>(schema, model);

        controller.Rows(items)[0].Set(product, "changed");
        controller.AddRow(items);
        controller.Rows(items)[1].Set(product, "added");
        controller.Set(roles, [Role.Admin, Role.Guest]);
        controller.ApplyTo(model);

        Assert.Same(list, model.Items);
        Assert.Equal(2, model.Items.Count);
        Assert.Same(first, model.Items[0]);
        Assert.Equal("changed", first.Product);
        Assert.Equal("added", model.Items[1].Product);
        Assert.Equal([Role.Admin, Role.Guest], model.Roles);
    }

    [Fact]
    public void A_getter_only_collection_is_written_into_a_new_model_too()
    {
        FormListField<GetterOnly, Entry> items = null!;
        var schema = FormSchema.For<GetterOnly>(s => items = s.List(g => g.Items, row => row.Field(e => e.Product)));
        var controller = new FormController<GetterOnly>(schema);
        controller.AddRow(items);

        Assert.Single(controller.CreateModel().Items);
    }

    [Fact]
    public void A_getter_only_collection_that_cannot_be_changed_in_place_is_refused_when_the_schema_is_built()
    {
        var frozen = Assert.Throws<ArgumentException>(() => FormSchema.For<SealedLists>(s => s.List(l => l.Frozen, row => row.Field(e => e.Product))));
        Assert.Contains("no public setter", frozen.Message, StringComparison.Ordinal);
        Assert.Contains("Frozen", frozen.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => FormSchema.For<SealedLists>(s => s.List(l => l.Array, row => row.Field(e => e.Product))));
        Assert.Throws<ArgumentException>(() => FormSchema.For<SealedLists>(s => s.MultiSelect(l => l.Roles, [new FormOption<Role>(Role.Guest, "Guest")])));
    }

    [Fact]
    public void A_getter_only_collection_that_is_read_only_at_run_time_fails_before_anything_is_written()
    {
        FormListField<GetterOnly, Entry> locked = null!;
        var schema = FormSchema.For<GetterOnly>(s =>
        {
            s.Field(g => g.Name);
            locked = s.List(g => g.Locked, row => row.Field(e => e.Product));
        });
        var model = new GetterOnly { Name = "start" };
        var controller = new FormController<GetterOnly>(schema, model);
        controller.Set(schema.Field(g => g.Name), "changed");
        controller.AddRow(locked);

        var error = Assert.Throws<InvalidOperationException>(() => controller.ApplyTo(model));

        Assert.Contains("Locked", error.Message, StringComparison.Ordinal);
        Assert.Equal("start", model.Name);
    }

    [Fact]
    public void A_list_reached_through_a_chain_is_refused_and_the_message_points_to_Group()
    {
        var list = Assert.Throws<ArgumentException>(() => FormSchema.For<Wrapper>(s => s.List(w => w.Inner.Lines, row => row.Field(e => e.Product))));
        Assert.Contains("Group(", list.Message, StringComparison.Ordinal);
        Assert.Contains("Inner", list.Message, StringComparison.Ordinal);
        var multi = Assert.Throws<ArgumentException>(() => FormSchema.For<Wrapper>(s => s.MultiSelect(w => w.Inner.Roles, [new FormOption<Role>(Role.Guest, "Guest")])));
        Assert.Contains("Group(", multi.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_list_inside_a_group_reads_and_writes_the_items_of_the_nested_object()
    {
        FormListField<Wrapper, Entry> lines = null!;
        FormField<Entry, string> product = null!;
        var schema = FormSchema.For<Wrapper>(s => s.Group(w => w.Inner, g => lines = g.List(h => h.Lines, row => product = row.Field(e => e.Product))));
        var first = new Entry { Id = 4, Product = "a" };
        var model = new Wrapper { Inner = new EntryHolder { Lines = [first] } };
        var controller = new FormController<Wrapper>(schema, model);

        Assert.Single(controller.Rows(lines));
        controller.Rows(lines)[0].Set(product, "changed");
        controller.AddRow(lines);
        controller.ApplyTo(model);

        Assert.Equal(2, model.Inner.Lines.Count);
        Assert.Same(first, model.Inner.Lines[0]);
        Assert.Equal("changed", first.Product);
        Assert.Equal(4, first.Id);
    }
}
