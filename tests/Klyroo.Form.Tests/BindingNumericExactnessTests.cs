using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Numbers
{
    public long Long { get; set; }

    public ulong ULong { get; set; }

    public decimal Money { get; set; }

    public float Ratio { get; set; }

    public double Precise { get; set; }

    public long? MaybeLong { get; set; }

    public uint Uint { get; set; }

    public short Short { get; set; }

    public sbyte SByte { get; set; }

    public decimal? MaybeMoney { get; set; }
}

public sealed class BindingNumericExactnessTests : BunitContext
{
    private static readonly FormModel<Numbers> Form = FormModel.For<Numbers>(b => b.AllMembers());

    public BindingNumericExactnessTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static void RoundTrips(Numbers model)
    {
        var input = Form.ToInput(model);
        var written = Form.Write(input, new Numbers());
        var created = Form.Create(input);
        foreach (var result in new[] { written, created })
        {
            Assert.Equal(model.Long, result.Long);
            Assert.Equal(model.ULong, result.ULong);
            Assert.Equal(model.Money, result.Money);
            Assert.Equal(model.Ratio, result.Ratio);
            Assert.Equal(model.Precise, result.Precise);
            Assert.Equal(model.MaybeLong, result.MaybeLong);
            Assert.Equal(model.Uint, result.Uint);
            Assert.Equal(model.Short, result.Short);
            Assert.Equal(model.SByte, result.SByte);
            Assert.Equal(model.MaybeMoney, result.MaybeMoney);
        }
    }

    [Fact]
    public void Long_and_ulong_extremes_round_trip_instead_of_becoming_zero()
    {
        RoundTrips(new Numbers { Long = long.MaxValue, ULong = ulong.MaxValue, MaybeLong = long.MinValue });
        RoundTrips(new Numbers { Long = long.MinValue, ULong = ulong.MaxValue - 1, MaybeLong = long.MaxValue });
        RoundTrips(new Numbers { Long = 9007199254740993, ULong = 18446744073709551614 });
    }

    [Fact]
    public void A_decimal_keeps_all_its_digits()
    {
        RoundTrips(new Numbers { Money = 1234567890123456789.1234567890m, MaybeMoney = -0.0000000000000000000000000001m });
        RoundTrips(new Numbers { Money = decimal.MaxValue, MaybeMoney = decimal.MinValue });
        RoundTrips(new Numbers { Money = 0.1m + 0.2m });
    }

    [Fact]
    public void A_float_and_a_double_round_trip_exactly_and_the_float_shows_its_short_text()
    {
        RoundTrips(new Numbers { Ratio = 0.1f, Precise = 1.2345678901234567 });
        RoundTrips(new Numbers { Ratio = float.MaxValue, Precise = double.MaxValue });
        RoundTrips(new Numbers { Ratio = float.Epsilon, Precise = double.Epsilon });
        RoundTrips(new Numbers { Ratio = 16777217f, Precise = 0.1 + 0.2 });
        var input = Form.ToInput(new Numbers { Ratio = 0.1f });
        Assert.Equal("0.1", Convert.ToString(input["Ratio"], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void The_bounds_of_every_integer_type_round_trip()
    {
        RoundTrips(new Numbers { Uint = uint.MaxValue, Short = short.MinValue, SByte = sbyte.MaxValue });
        RoundTrips(new Numbers { Uint = uint.MinValue, Short = short.MaxValue, SByte = sbyte.MinValue });
    }

    [Fact]
    public void A_number_control_shows_and_posts_the_exact_value()
    {
        var model = new Numbers { Long = long.MaxValue, Money = 1234567890123456789.1234567890m, Ratio = 0.1f };
        var controller = Form.CreateController(model);
        var cut = Render<KlyFormEngine>(p => p.Add(x => x.Form, controller));

        Assert.Equal("9223372036854775807", cut.Find("[data-form-field=Long] input").GetAttribute("value"));
        Assert.Equal("1234567890123456789.1234567890", cut.Find("[data-form-field=Money] input").GetAttribute("value"));
        Assert.Equal("0.1", cut.Find("[data-form-field=Ratio] input").GetAttribute("value"));

        cut.Find("[data-form-field=Long] input").Input("9223372036854775806");
        cut.Find("[data-form-field=ULong] input").Input("18446744073709551615");
        cut.Find("[data-form-field=Money] input").Input("79228162514264337593543950335");
        cut.Find("[data-form-field=Ratio] input").Input("0.3");
        cut.Find("[data-form-field=Precise] input").Input("1.2345678901234567");

        var typed = Form.Create(controller);
        Assert.Equal(9223372036854775806, typed.Long);
        Assert.Equal(ulong.MaxValue, typed.ULong);
        Assert.Equal(decimal.MaxValue, typed.Money);
        Assert.Equal(0.3f, typed.Ratio);
        Assert.Equal(1.2345678901234567, typed.Precise);
    }

    [Theory]
    [InlineData("Long", "9223372036854775808")]
    [InlineData("Long", "-9223372036854775809")]
    [InlineData("ULong", "18446744073709551616")]
    [InlineData("ULong", "-1")]
    [InlineData("Uint", "4294967296")]
    [InlineData("Short", "32768")]
    [InlineData("SByte", "-129")]
    [InlineData("Money", "79228162514264337593543950336")]
    [InlineData("Ratio", "1e39")]
    public void A_value_outside_the_type_is_a_validation_error_and_never_zero(string key, string text)
    {
        var controller = Form.CreateController(new Numbers());
        var cut = Render<KlyFormEngine>(p => p.Add(x => x.Form, controller));
        cut.Find($"[data-form-field={key}] input").Input(text);

        Assert.False(controller.Validate());
        Assert.False(string.IsNullOrEmpty(controller.GetError(key)));
    }

    [Fact]
    public void A_fractional_value_is_not_a_whole_number()
    {
        var controller = Form.CreateController(new Numbers());
        var cut = Render<KlyFormEngine>(p => p.Add(x => x.Form, controller));
        cut.Find("[data-form-field=Long] input").Input("3.5");

        Assert.False(controller.Validate());
        Assert.Equal("Enter a whole number", controller.GetError("Long"));
    }

    [Fact]
    public void Output_and_dirty_tracking_stay_consistent_when_the_same_text_is_typed_back()
    {
        var controller = Form.CreateController(new Numbers { Long = long.MaxValue, Precise = 1.2345678901234567 });
        var cut = Render<KlyFormEngine>(p => p.Add(x => x.Form, controller));
        cut.Find("[data-form-field=Long] input").Input("9223372036854775807");
        cut.Find("[data-form-field=Precise] input").Input("1.2345678901234567");

        Assert.False(controller.IsDirty);
    }

    [Fact]
    public void A_draft_keeps_the_exact_numbers()
    {
        var controller = Form.CreateController(new Numbers { Long = long.MaxValue, Money = 1234567890123456789.1234567890m, Ratio = 0.1f });
        var json = controller.ExportState().ToJson();

        var restored = Form.CreateController();
        restored.RestoreDraft(FormDraft.FromJson(json));
        var model = Form.Create(restored);
        Assert.Equal(long.MaxValue, model.Long);
        Assert.Equal(1234567890123456789.1234567890m, model.Money);
        Assert.Equal(0.1f, model.Ratio);
    }
}
