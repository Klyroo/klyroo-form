using System.Text.Json;
using Klyroo.Form.Dynamic;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Amounts
{
    public decimal Price { get; set; }

    public long Big { get; set; }

    public ulong Huge { get; set; }

    public double Ratio { get; set; }

    public int Count { get; set; }

    public decimal? Optional { get; set; }
}

public sealed class JsonExactnessTests
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static (FormController<Amounts> Controller, FormSchema<Amounts> Schema) Setup()
    {
        var schema = FormSchema.For<Amounts>(s =>
        {
            s.Field(a => a.Price);
            s.Field(a => a.Big);
            s.Field(a => a.Huge);
            s.Field(a => a.Ratio);
            s.Field(a => a.Count);
            s.Field(a => a.Optional);
        });
        return (new FormController<Amounts>(schema), schema);
    }

    private static IFormFieldAccess Access(FormController<Amounts> controller, string key) => controller.Fields.Single(f => f.Key == key);

    [Fact]
    public void A_decimal_keeps_all_its_digits_through_TrySet()
    {
        var (controller, schema) = Setup();

        var result = Access(controller, "Price").TrySet(Json("1234567890123456789.1234567890"));

        Assert.True(result.Ok, result.Error);
        Assert.Equal(1234567890123456789.1234567890m, controller.Get(schema.Field(a => a.Price)));
        Assert.Equal(1234567890123456789.1234567890m, controller.CreateModel().Price);
    }

    [Theory]
    [InlineData("9007199254740993", 9007199254740993L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("-9223372036854775808", long.MinValue)]
    [InlineData("42", 42L)]
    public void A_long_keeps_every_digit_through_TrySet(string json, long expected)
    {
        var (controller, schema) = Setup();

        var result = Access(controller, "Big").TrySet(Json(json));

        Assert.True(result.Ok, result.Error);
        Assert.Equal(expected, controller.Get(schema.Field(a => a.Big)));
        Assert.Equal(expected, controller.CreateModel().Big);
    }

    [Fact]
    public void A_number_sent_as_text_is_exact_too()
    {
        var (controller, schema) = Setup();

        Assert.True(Access(controller, "Big").TrySet(Json("\"9007199254740993\"")).Ok);
        Assert.Equal(9007199254740993L, controller.Get(schema.Field(a => a.Big)));
    }

    [Fact]
    public void The_largest_unsigned_long_is_kept()
    {
        var (controller, schema) = Setup();

        Assert.True(Access(controller, "Huge").TrySet(Json("18446744073709551615")).Ok);
        Assert.Equal(ulong.MaxValue, controller.Get(schema.Field(a => a.Huge)));
    }

    [Fact]
    public void A_number_that_does_not_fit_the_type_is_refused_and_leaves_the_field_alone()
    {
        var (controller, schema) = Setup();
        Assert.True(Access(controller, "Big").TrySet(Json("5")).Ok);

        var tooBig = Access(controller, "Big").TrySet(Json("9223372036854775808"));
        var fractional = Access(controller, "Count").TrySet(Json("2.5"));

        Assert.False(tooBig.Ok);
        Assert.Contains("fits Int64", tooBig.Error, StringComparison.Ordinal);
        Assert.False(fractional.Ok);
        Assert.Equal(5L, controller.Get(schema.Field(a => a.Big)));
    }

    [Fact]
    public void Doubles_and_short_numbers_keep_their_usual_control_value()
    {
        var (controller, schema) = Setup();

        Assert.True(Access(controller, "Ratio").TrySet(Json("0.1")).Ok);
        Assert.True(Access(controller, "Count").TrySet(Json("3")).Ok);
        Assert.True(Access(controller, "Optional").TrySet(Json("12.5")).Ok);

        Assert.Equal(0.1, controller.Get(schema.Field(a => a.Ratio)));
        Assert.Equal(3, controller.Get(schema.Field(a => a.Count)));
        Assert.Equal(12.5m, controller.Get(schema.Field(a => a.Optional)));
        Assert.Equal(0.1, Assert.IsType<double>(controller.Raw("Ratio")));
    }

    [Fact]
    public void A_dynamic_number_field_keeps_storing_a_double_for_a_large_number()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "f",
            Fields = [new DynamicFieldSchema { Key = "n", Label = "N", Kind = FormFieldKind.Number }],
        });

        Assert.True(form.Fields.Single().TrySet(Json("9007199254740993")).Ok);

        Assert.Equal(9007199254740992d, form.GetValue("n"));
    }

    [Fact]
    public void Choices_still_match_by_value_or_by_label_through_the_codec()
    {
        var schema = FormSchema.For<Basket>(s => s.Field(b => b.Seats, f => f.Options([new FormOption<int?>(1, "One"), new FormOption<int?>(2, "Deux")])));
        var controller = new FormController<Basket>(schema);
        var seats = controller.Fields.Single(f => f.Key == "Seats");

        Assert.True(seats.TrySet(Json("\"deux\"")).Ok);
        Assert.Equal(2, controller.Get(schema.Field(b => b.Seats)));
        Assert.True(seats.TrySet(Json("1")).Ok);
        Assert.Equal(1, controller.Get(schema.Field(b => b.Seats)));
        Assert.False(seats.TrySet(Json("\"three\"")).Ok);
    }
}
