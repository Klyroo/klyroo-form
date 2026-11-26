using System.Text.Json;
using Klyroo.Form.Dynamic;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Bounded
{
    public decimal Price { get; set; }

    public long Count { get; set; }

    public decimal Tiny { get; set; }

    public double Ratio { get; set; }
}

/// <summary>Min and Max are checked on the exact number, never on its double approximation, when a value is written and when the form is validated.</summary>
public sealed class NumberBoundsExactnessTests
{
    private const long TwoPow53 = 9007199254740992L;

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static (FormController<Bounded> Controller, FormSchema<Bounded> Schema) Setup()
    {
        var schema = FormSchema.For<Bounded>(s =>
        {
            s.Field(b => b.Price, f => f.Range(0m, 100m));
            s.Field(b => b.Count, f => f.Range(-TwoPow53, TwoPow53));
            s.Field(b => b.Tiny, f => f.Range(0.1m, 0.3m));
            s.Field(b => b.Ratio, f => f.Range(0.1, 0.3));
        });
        return (new FormController<Bounded>(schema), schema);
    }

    private static IFormFieldAccess Access(FormController<Bounded> controller, string key) => controller.Fields.Single(f => f.Key == key);

    [Theory]
    [InlineData("100", true)]
    [InlineData("100.0", true)]
    [InlineData("99.9999999999999999", true)]
    [InlineData("100.0000000000000001", false)]
    [InlineData("-0.0000000000000001", false)]
    [InlineData("0", true)]
    public void A_decimal_is_checked_against_its_bounds_digit_for_digit_when_written(string json, bool accepted)
    {
        var (controller, schema) = Setup();

        var result = Access(controller, "Price").TrySet(Json(json));

        Assert.Equal(accepted, result.Ok);
        if (accepted) Assert.Equal(decimal.Parse(json, System.Globalization.CultureInfo.InvariantCulture), controller.Get(schema.Field(b => b.Price)));
        else
        {
            Assert.Contains("must be at", result.Error, StringComparison.Ordinal);
            Assert.Equal(0m, controller.Get(schema.Field(b => b.Price)));
        }
    }

    [Theory]
    [InlineData("9007199254740992", true)]
    [InlineData("9007199254740993", false)]
    [InlineData("-9007199254740992", true)]
    [InlineData("-9007199254740993", false)]
    [InlineData("\"9007199254740993\"", false)]
    public void A_long_is_checked_against_a_bound_of_two_to_the_53_exactly_when_written(string json, bool accepted)
    {
        var (controller, schema) = Setup();

        var result = Access(controller, "Count").TrySet(Json(json));

        Assert.Equal(accepted, result.Ok);
        if (!accepted) Assert.Equal(0L, controller.Get(schema.Field(b => b.Count)));
    }

    [Theory]
    [InlineData("0.1", true)]
    [InlineData("0.3", true)]
    [InlineData("0.30000000000000001", false)]
    [InlineData("0.09999999999999999", false)]
    public void A_decimal_bound_that_has_no_exact_double_still_accepts_its_own_value(string json, bool accepted)
    {
        var (controller, _) = Setup();

        Assert.Equal(accepted, Access(controller, "Tiny").TrySet(Json(json)).Ok);
    }

    [Fact]
    public void A_double_field_keeps_checking_its_bounds_as_doubles()
    {
        var (controller, _) = Setup();

        Assert.True(Access(controller, "Ratio").TrySet(Json("0.1")).Ok);
        Assert.True(Access(controller, "Ratio").TrySet(Json("0.3")).Ok);
        Assert.False(Access(controller, "Ratio").TrySet(Json("0.31")).Ok);
        Assert.False(Access(controller, "Ratio").TrySet(Json("0.09")).Ok);
    }

    [Fact]
    public void Validation_compares_a_decimal_value_with_its_bounds_exactly()
    {
        var (controller, schema) = Setup();
        var price = schema.Field(b => b.Price);

        controller.Set(price, 100m, markTouched: true);
        Assert.Null(controller.GetError(price));

        controller.Set(price, 100.0000000000000001m, markTouched: true);
        Assert.NotNull(controller.GetError(price));
        Assert.False(controller.Validate());

        controller.Set(price, -0.0000000000000001m, markTouched: true);
        Assert.NotNull(controller.GetError(price));
    }

    [Fact]
    public void Validation_compares_a_long_value_with_a_bound_of_two_to_the_53_exactly()
    {
        var (controller, schema) = Setup();
        var count = schema.Field(b => b.Count);

        controller.Set(count, TwoPow53, markTouched: true);
        Assert.Null(controller.GetError(count));

        controller.Set(count, TwoPow53 + 1, markTouched: true);
        Assert.NotNull(controller.GetError(count));

        controller.Set(count, -TwoPow53 - 1, markTouched: true);
        Assert.NotNull(controller.GetError(count));
    }

    [Fact]
    public void Validation_accepts_a_decimal_equal_to_a_bound_that_a_double_cannot_hold()
    {
        var (controller, schema) = Setup();
        var tiny = schema.Field(b => b.Tiny);

        controller.Set(tiny, 0.1m, markTouched: true);
        Assert.Null(controller.GetError(tiny));
        controller.Set(tiny, 0.3m, markTouched: true);
        Assert.Null(controller.GetError(tiny));
        controller.Set(tiny, 0.3000000000000001m, markTouched: true);
        Assert.NotNull(controller.GetError(tiny));
    }

    [Fact]
    public void A_dynamic_number_field_checks_a_decimal_text_exactly_too()
    {
        var form = new DynamicFormController(new DynamicFormSchema
        {
            FormKey = "f",
            Fields = [new DynamicFieldSchema { Key = "n", Label = "N", Kind = FormFieldKind.Number, Max = 100 }],
        });
        var field = form.Fields.Single();

        Assert.True(field.TrySet(Json("100.0")).Ok);
        Assert.False(field.TrySet(Json("100.0000000000000001")).Ok);
        Assert.Equal(100d, form.GetValue("n"));
    }
}
