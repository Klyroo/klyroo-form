using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Entry
{
    private int _qty = 1;

    // Not a column of the form: it has to survive an edit of the row.
    public int Id { get; set; }

    public string Product { get; set; } = "";

    public int Qty
    {
        get => _qty;
        set
        {
            if (value == 99) throw new InvalidOperationException("99 is refused by the setter.");
            _qty = value;
        }
    }
}

public sealed class Ledger
{
    private List<Entry> _entries = [];

    public List<Entry> Entries
    {
        get => _entries;
        set
        {
            _entries = value;
            Assignments++;
        }
    }

    public int Assignments { get; private set; }
}

public sealed class ListIdentityTests
{
    private static (FormSchema<Ledger> Schema, FormListField<Ledger, Entry> Entries, FormField<Entry, string> Product, FormField<Entry, int> Qty) Declare()
    {
        FormListField<Ledger, Entry> entries = null!;
        FormField<Entry, string> product = null!;
        FormField<Entry, int> qty = null!;
        var schema = FormSchema.For<Ledger>(s => entries = s.List(l => l.Entries, row =>
        {
            product = row.Field(e => e.Product);
            qty = row.Field(e => e.Qty);
        }));
        return (schema, entries, product, qty);
    }

    [Fact]
    public void Editing_a_row_writes_into_the_item_it_was_loaded_from_and_keeps_its_other_members()
    {
        var (schema, entries, product, _) = Declare();
        var first = new Entry { Id = 10, Product = "pen" };
        var second = new Entry { Id = 20, Product = "ink" };
        var model = new Ledger { Entries = [first, second] };
        var assignmentsBefore = model.Assignments;
        var list = model.Entries;
        var controller = new FormController<Ledger>(schema, model);

        controller.Rows(entries)[0].Set(product, "pencil");
        controller.ApplyTo(model);

        Assert.Same(first, model.Entries[0]);
        Assert.Same(second, model.Entries[1]);
        Assert.Equal(10, first.Id);
        Assert.Equal(20, second.Id);
        Assert.Equal("pencil", first.Product);
        Assert.Equal("ink", second.Product);
        Assert.Same(list, model.Entries);
        Assert.Equal(assignmentsBefore, model.Assignments);
    }

    [Fact]
    public void A_list_nobody_edited_is_not_reassigned()
    {
        var (schema, _, _, _) = Declare();
        var model = new Ledger { Entries = [new Entry { Id = 1, Product = "a" }, new Entry { Id = 2, Product = "b" }] };
        var assignmentsBefore = model.Assignments;

        new FormController<Ledger>(schema, model).ApplyTo(model);

        Assert.Equal(assignmentsBefore, model.Assignments);
        Assert.Equal([1, 2], model.Entries.Select(e => e.Id));
    }

    [Fact]
    public void An_added_row_is_a_new_item_a_removed_row_is_dropped_and_the_order_is_kept()
    {
        var (schema, entries, product, _) = Declare();
        var a = new Entry { Id = 1, Product = "a" };
        var b = new Entry { Id = 2, Product = "b" };
        var c = new Entry { Id = 3, Product = "c" };
        var model = new Ledger { Entries = [a, b, c] };
        var controller = new FormController<Ledger>(schema, model);

        controller.RemoveRow(entries, 1);
        controller.AddRow(entries);
        controller.Rows(entries)[2].Set(product, "d");
        controller.ApplyTo(model);

        Assert.Equal(3, model.Entries.Count);
        Assert.Same(a, model.Entries[0]);
        Assert.Same(c, model.Entries[1]);
        Assert.NotSame(b, model.Entries[2]);
        Assert.Equal(0, model.Entries[2].Id);
        Assert.Equal("d", model.Entries[2].Product);
    }

    [Fact]
    public void A_setter_that_throws_on_a_later_row_puts_back_the_rows_already_written_and_the_list()
    {
        var (schema, entries, product, qty) = Declare();
        var a = new Entry { Id = 1, Product = "a" };
        var b = new Entry { Id = 2, Product = "b" };
        var model = new Ledger { Entries = [a, b] };
        var controller = new FormController<Ledger>(schema, model);
        controller.AddRow(entries);
        var rows = controller.Rows(entries);
        rows[0].Set(product, "changed");
        rows[1].Set(qty, 99);

        Assert.Throws<InvalidOperationException>(() => controller.ApplyTo(model));

        Assert.Equal("a", a.Product);
        Assert.Equal(1, b.Qty);
        Assert.Equal([a, b], model.Entries);
    }

    [Fact]
    public void A_cell_that_cannot_be_converted_fails_before_any_row_is_written()
    {
        var (schema, entries, product, _) = Declare();
        var a = new Entry { Id = 1, Product = "a" };
        var model = new Ledger { Entries = [a] };
        var controller = new FormController<Ledger>(schema, model);
        controller.Rows(entries)[0].Set(product, "changed");
        controller.Engine.SetArrayItemValue("Entries", 0, "Qty", "abc");

        Assert.Throws<InvalidOperationException>(() => controller.ApplyTo(model));

        Assert.Equal("a", a.Product);
        Assert.Equal(1, a.Qty);
    }

    [Fact]
    public async Task A_form_that_stays_open_after_a_save_keeps_the_items_the_first_save_created()
    {
        var (schema, entries, product, _) = Declare();
        var model = new Ledger();
        var controller = new FormController<Ledger>(schema, model);
        controller.AddRow(entries);
        controller.Rows(entries)[0].Set(product, "pen");

        Assert.True(await controller.SubmitAsync((m, _) =>
        {
            m.Entries[0].Id = 7;
            return Task.FromResult(true);
        }, model, TestContext.Current.CancellationToken));
        var created = model.Entries[0];

        controller.Rows(entries)[0].Set(product, "pencil");
        Assert.True(await controller.SubmitAsync((_, _) => Task.FromResult(true), model, TestContext.Current.CancellationToken));

        Assert.Same(created, model.Entries[0]);
        Assert.Equal(7, created.Id);
        Assert.Equal("pencil", created.Product);
    }

    [Fact]
    public async Task A_failed_save_puts_back_the_rows_and_the_list()
    {
        var (schema, entries, product, _) = Declare();
        var a = new Entry { Id = 1, Product = "a" };
        var model = new Ledger { Entries = [a] };
        var controller = new FormController<Ledger>(schema, model);
        controller.Rows(entries)[0].Set(product, "changed");
        controller.AddRow(entries);

        Assert.False(await controller.SubmitAsync((_, _) => Task.FromResult(false), model, TestContext.Current.CancellationToken));

        Assert.Equal("a", a.Product);
        Assert.Equal([a], model.Entries);
    }

    [Fact]
    public void Writing_into_another_model_creates_items_and_never_changes_the_rows_of_the_loaded_one()
    {
        var (schema, entries, product, _) = Declare();
        var a = new Entry { Id = 1, Product = "a" };
        var loaded = new Ledger { Entries = [a] };
        var other = new Ledger();
        var controller = new FormController<Ledger>(schema, loaded);
        controller.Rows(entries)[0].Set(product, "changed");

        controller.ApplyTo(other);

        Assert.NotSame(a, other.Entries[0]);
        Assert.Equal("changed", other.Entries[0].Product);
        Assert.Equal("a", a.Product);
        Assert.Equal("changed", controller.CreateModel().Entries[0].Product);
        Assert.Equal("a", a.Product);
    }
}
