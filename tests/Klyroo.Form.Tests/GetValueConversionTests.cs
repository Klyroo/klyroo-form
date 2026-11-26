using System.Globalization;
using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class GetValueConversionTests
{
    private enum Level { Low, High = 5 }

    [Flags]
    private enum Perm { None = 0, Read = 1, Write = 2 }

    private static DynamicFormController With(object? value)
    {
        var form = new DynamicFormController(new DynamicFormSchema { FormKey = "c", Fields = [new DynamicFieldSchema { Key = "v" }] });
        form.SetValue("v", value);
        return form;
    }

    [Fact]
    public void A_value_already_of_the_type_is_returned_as_is()
    {
        var list = new List<string> { "a" };
        Assert.Same(list, With(list).GetValue<List<string>>("v"));
        Assert.Equal(4.5, With(4.5).GetValue<double>("v"));
        Assert.Equal("x", With("x").GetValue<string>("v"));
    }

    [Fact]
    public void Missing_null_and_blank_values_give_default_and_TryGetValue_false()
    {
        var form = With(null);
        Assert.Equal(0, form.GetValue<int>("v"));
        Assert.Null(form.GetValue<int?>("v"));
        Assert.False(form.TryGetValue<int>("v", out var n));
        Assert.Equal(0, n);
        Assert.False(form.TryGetValue<string>("missing", out var s));
        Assert.Null(s);

        var blank = With("  ");
        Assert.False(blank.TryGetValue<int>("v", out _));
        Assert.False(blank.TryGetValue<DateOnly>("v", out _));
        Assert.False(blank.TryGetValue<Guid?>("v", out _));
        Assert.True(blank.TryGetValue<string>("v", out var text));
        Assert.Equal("  ", text);
    }

    [Fact]
    public void Nullable_targets_convert_and_unwrap()
    {
        Assert.Equal(7, With(7.0).GetValue<int?>("v"));
        Assert.Equal(7, With("7").GetValue<int?>("v"));
        Assert.Equal(new DateOnly(2026, 11, 26), With("2026-11-26").GetValue<DateOnly?>("v"));
        Assert.Equal(Level.High, With("high").GetValue<Level?>("v"));
        Assert.True(With("true").TryGetValue<bool?>("v", out var b));
        Assert.True(b);
        Assert.False(With("nope").TryGetValue<int?>("v", out var bad));
        Assert.Null(bad);
    }

    [Theory]
    [InlineData("2026-11-26", 2026, 11, 26)]
    [InlineData("2026-11-26T09:30", 2026, 11, 26)]
    [InlineData("2026-02-28T23:59:59", 2026, 2, 28)]
    public void DateOnly_reads_the_date_and_datetime_formats_of_the_controls(string raw, int y, int m, int d)
    {
        Assert.Equal(new DateOnly(y, m, d), With(raw).GetValue<DateOnly>("v"));
    }

    [Fact]
    public void DateOnly_rejects_a_non_date_and_accepts_date_values()
    {
        Assert.False(With("26/11/2026").TryGetValue<DateOnly>("v", out _));
        Assert.False(With("2026-13-40").TryGetValue<DateOnly>("v", out _));
        Assert.Equal(new DateOnly(2026, 1, 2), With(new DateTime(2026, 1, 2, 5, 0, 0)).GetValue<DateOnly>("v"));
    }

    [Theory]
    [InlineData("09:30", 9, 30, 0)]
    [InlineData("9:05", 9, 5, 0)]
    [InlineData("23:59:58", 23, 59, 58)]
    public void TimeOnly_reads_the_time_formats_of_the_control(string raw, int h, int m, int s)
    {
        Assert.Equal(new TimeOnly(h, m, s), With(raw).GetValue<TimeOnly>("v"));
    }

    [Fact]
    public void TimeOnly_rejects_out_of_range_values()
    {
        Assert.False(With("24:00").TryGetValue<TimeOnly>("v", out _));
        Assert.False(With("12:61").TryGetValue<TimeOnly>("v", out _));
        Assert.False(With(TimeSpan.FromHours(30)).TryGetValue<TimeOnly>("v", out _));
        Assert.Equal(new TimeOnly(6, 0), With(TimeSpan.FromHours(6)).GetValue<TimeOnly>("v"));
    }

    [Fact]
    public void DateTime_reads_the_datetime_local_format_and_iso_strings()
    {
        Assert.Equal(new DateTime(2026, 11, 26, 9, 30, 0), With("2026-11-26T09:30").GetValue<DateTime>("v"));
        Assert.Equal(new DateTime(2026, 11, 26, 9, 30, 15), With("2026-11-26T09:30:15").GetValue<DateTime>("v"));
        Assert.Equal(new DateTime(2026, 11, 26), With("2026-11-26").GetValue<DateTime>("v"));
        Assert.Equal(new DateTime(2026, 11, 26, 9, 30, 0, DateTimeKind.Utc), With("2026-11-26T09:30:00Z").GetValue<DateTime>("v"));
        Assert.False(With("tomorrow").TryGetValue<DateTime>("v", out _));
    }

    [Fact]
    public void DateTimeOffset_treats_a_value_without_a_zone_as_utc()
    {
        var parsed = With("2026-11-26T09:30").GetValue<DateTimeOffset>("v");
        Assert.Equal(new DateTimeOffset(2026, 11, 26, 9, 30, 0, TimeSpan.Zero), parsed);
        Assert.Equal(TimeSpan.Zero, parsed.Offset);

        var withZone = With("2026-11-26T09:30:00+02:00").GetValue<DateTimeOffset>("v");
        Assert.Equal(TimeSpan.FromHours(2), withZone.Offset);
        Assert.Equal(new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero), With(new DateOnly(2026, 11, 26)).GetValue<DateTimeOffset>("v"));
    }

    [Fact]
    public void Guid_parses_text_and_rejects_garbage()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, With(id.ToString()).GetValue<Guid>("v"));
        Assert.Equal(id, With($" {id:N} ").GetValue<Guid>("v"));
        Assert.Equal(id, With(id).GetValue<Guid>("v"));
        Assert.False(With("not-a-guid").TryGetValue<Guid>("v", out _));
    }

    [Fact]
    public void Enums_convert_by_name_ignoring_case_and_by_member_number()
    {
        Assert.Equal(Level.High, With("High").GetValue<Level>("v"));
        Assert.Equal(Level.Low, With("low").GetValue<Level>("v"));
        Assert.Equal(Level.High, With("5").GetValue<Level>("v"));
        Assert.Equal(Level.High, With(5).GetValue<Level>("v"));
        Assert.Equal(Level.High, With(5.0).GetValue<Level>("v"));
        Assert.False(With("7").TryGetValue<Level>("v", out _));
        Assert.False(With(7).TryGetValue<Level>("v", out _));
        Assert.False(With("Medium").TryGetValue<Level>("v", out _));
        Assert.Equal(Perm.Read | Perm.Write, With("Read, Write").GetValue<Perm>("v"));
    }

    [Fact]
    public void Numbers_use_the_invariant_culture_whatever_the_current_culture_is()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            Assert.Equal(3.5, With("3.5").GetValue<double>("v"));
            Assert.Equal(3.5m, With("3.5").GetValue<decimal>("v"));
            Assert.Equal(3.5f, With("3.5").GetValue<float>("v"));
            Assert.False(With("3,5").TryGetValue<double>("v", out _));
            Assert.Equal("3.5", With(3.5).GetValue<string>("v"));
            Assert.Equal(1e3, With("1e3").GetValue<double>("v"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Whole_numbers_become_integers_and_fractions_do_not_round()
    {
        Assert.Equal(3, With(3.0).GetValue<int>("v"));
        Assert.Equal(3, With("3.0").GetValue<int>("v"));
        Assert.Equal(3L, With(3).GetValue<long>("v"));
        Assert.False(With(3.5).TryGetValue<int>("v", out var n));
        Assert.Equal(0, n);
        Assert.False(With("3.5").TryGetValue<int>("v", out _));
        Assert.False(With(300).TryGetValue<byte>("v", out _));
        Assert.False(With(-1).TryGetValue<uint>("v", out _));
        Assert.False(With(double.NaN).TryGetValue<int>("v", out _));
        Assert.False(With(double.PositiveInfinity).TryGetValue<long>("v", out _));
        Assert.Equal(byte.MaxValue, With(255).GetValue<byte>("v"));
        Assert.Equal(ulong.MaxValue, With("18446744073709551615").GetValue<ulong>("v"));
    }

    [Fact]
    public void Numbers_read_from_other_numeric_types()
    {
        Assert.Equal(2.0, With(2).GetValue<double>("v"));
        Assert.Equal(2.5m, With(2.5).GetValue<decimal>("v"));
        Assert.Equal(12.25, With(12.25m).GetValue<double>("v"));
        Assert.False(With(double.NaN).TryGetValue<float>("v", out _));
    }

    [Fact]
    public void Bool_accepts_text_the_browser_and_people_write()
    {
        Assert.True(With("true").GetValue<bool>("v"));
        Assert.False(With("False").GetValue<bool>("v"));
        Assert.True(With("on").GetValue<bool>("v"));
        Assert.True(With("1").GetValue<bool>("v"));
        Assert.False(With("0").GetValue<bool>("v"));
        Assert.False(With("maybe").TryGetValue<bool>("v", out _));
        // Boxed numbers 1 and 0 are flags (a checkbox value read from JSON or a database); other numbers are not.
        Assert.True(With(1).GetValue<bool>("v"));
        Assert.False(With(0.0).GetValue<bool>("v"));
        Assert.True(With(0.0).TryGetValue<bool>("v", out _));
        Assert.False(With(2).TryGetValue<bool>("v", out _));
    }

    [Fact]
    public void Strings_come_from_any_value_in_the_formats_the_controls_use()
    {
        Assert.Equal("true", With(true).GetValue<string>("v"));
        Assert.Equal("2026-11-26", With(new DateOnly(2026, 11, 26)).GetValue<string>("v"));
        Assert.Equal("09:30", With(new TimeOnly(9, 30)).GetValue<string>("v"));
        Assert.Equal("2026-11-26T09:30", With(new DateTime(2026, 11, 26, 9, 30, 0)).GetValue<string>("v"));
        Assert.Equal("High", With(Level.High).GetValue<string>("v"));
        Assert.Equal("5", With(5).GetValue<string>("v"));
    }

    [Fact]
    public void Lists_of_strings_convert_between_collection_types()
    {
        var form = With(new[] { "a", "b" });
        Assert.Equal(["a", "b"], form.GetValue<List<string>>("v"));
        Assert.Equal(["a", "b"], form.GetValue<IReadOnlyList<string>>("v"));
        Assert.Equal(["a", "b"], form.GetValue<string[]>("v")!);
        Assert.False(With(5).TryGetValue<List<string>>("v", out _));
    }

    [Fact]
    public void TimeSpan_reads_text_and_time_values()
    {
        Assert.Equal(TimeSpan.FromMinutes(90), With("01:30:00").GetValue<TimeSpan>("v"));
        Assert.Equal(TimeSpan.FromMinutes(570), With(new TimeOnly(9, 30)).GetValue<TimeSpan>("v"));
        Assert.False(With("later").TryGetValue<TimeSpan>("v", out _));
    }

    [Fact]
    public void Controls_values_round_trip_through_GetValue()
    {
        var schema = new DynamicFormSchema
        {
            FormKey = "rt",
            Fields =
            [
                new DynamicFieldSchema { Key = "d", Kind = FormFieldKind.Date },
                new DynamicFieldSchema { Key = "dt", Kind = FormFieldKind.DateTime },
                new DynamicFieldSchema { Key = "t", Kind = FormFieldKind.Time },
                new DynamicFieldSchema { Key = "n", Kind = FormFieldKind.Number },
                new DynamicFieldSchema { Key = "r", Kind = FormFieldKind.Rating },
            ],
        };
        var form = new DynamicFormController(schema);
        form.SetValue("d", "2026-11-26");
        form.SetValue("dt", "2026-11-26T09:30");
        form.SetValue("t", "09:30");
        form.SetValue("n", 12.5);
        form.SetValue("r", 4);

        Assert.Equal(new DateOnly(2026, 11, 26), form.GetValue<DateOnly>("d"));
        Assert.Equal(new DateTime(2026, 11, 26, 9, 30, 0), form.GetValue<DateTime>("dt"));
        Assert.Equal(new TimeOnly(9, 30), form.GetValue<TimeOnly>("t"));
        Assert.Equal(12.5, form.GetValue<double>("n"));
        Assert.Equal(12.5m, form.GetValue<decimal>("n"));
        Assert.Equal(4, form.GetValue<int>("r"));
        Assert.True(form.TryGetValue<double>("n", out var n));
        Assert.Equal(12.5, n);
    }
}
