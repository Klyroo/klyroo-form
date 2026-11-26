using Klyroo.Form;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class ArrayCellTests
{
    private static (FormController Form, FormFieldSchema Lines) Invoice(Func<FormController, bool>? noteCondition = null, List<object?>? watched = null)
    {
        var lines = new FormFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayTable,
            Watch = (v, _) => watched?.Add("list"),
            Fields =
            [
                new FormFieldSchema { Key = "sku", Required = true, MaxLength = 5, Watch = (v, _) => watched?.Add(v) },
                new FormFieldSchema { Key = "qty", Kind = FormFieldKind.Number, Min = 1, DefaultValue = 1.0 },
                new FormFieldSchema { Key = "note", Required = true, Condition = noteCondition },
            ],
        };
        return (new FormController(new FormSchema { Fields = [lines] }), lines);
    }

    [Fact]
    public void Validate_checks_each_cell_with_the_rules_of_its_child_field()
    {
        var (form, lines) = Invoice(_ => false);
        form.AddArrayItem(lines);
        form.AddArrayItem(lines);
        form.SetArrayItemValue("lines", 0, "sku", "A-1");
        form.SetArrayItemValue("lines", 1, "sku", "TOO-LONG");
        form.SetArrayItemValue("lines", 1, "qty", 0.0);

        Assert.False(form.Validate());
        Assert.True(form.HasBlockingErrors);
        Assert.Null(form.GetArrayItemError("lines", 0, "sku"));
        Assert.Equal("Must be at most 5 characters", form.GetArrayItemError("lines", 1, "sku"));
        Assert.Equal("Value must be at least 1", form.GetError("lines[1].qty"));
        // The hidden column is neither rendered nor validated.
        Assert.Null(form.GetArrayItemError("lines", 0, "note"));

        form.SetArrayItemValue("lines", 1, "sku", "B-2");
        form.SetArrayItemValue("lines", 1, "qty", 2.0);
        Assert.True(form.Validate());
        Assert.False(form.HasBlockingErrors);
    }

    [Fact]
    public void A_touched_cell_is_validated_on_edit_and_removing_a_row_moves_the_errors_up()
    {
        var (form, lines) = Invoice(_ => false);
        form.AddArrayItem(lines);
        form.AddArrayItem(lines);

        form.MarkArrayItemTouched("lines", 1, "sku");
        Assert.Equal("This field is required", form.GetArrayItemError("lines", 1, "sku"));
        Assert.Null(form.GetArrayItemError("lines", 0, "sku"));

        form.RemoveArrayItem("lines", 0);
        Assert.Equal("This field is required", form.GetArrayItemError("lines", 0, "sku"));

        form.SetArrayItemValue("lines", 0, "sku", "OK", markTouched: true);
        Assert.Null(form.GetArrayItemError("lines", 0, "sku"));
    }

    [Fact]
    public void Cell_edits_call_the_child_and_list_watchers()
    {
        var watched = new List<object?>();
        var (form, lines) = Invoice(watched: watched);
        form.AddArrayItem(lines);
        form.SetArrayItemValue("lines", 0, "sku", "A-1");

        Assert.Equal(["list", "A-1", "list"], watched);
    }
}
