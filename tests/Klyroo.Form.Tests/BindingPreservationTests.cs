using Xunit;

namespace Klyroo.Form.Tests;

public enum Sparse
{
    One = 1,
    Two = 2,
}

public sealed class Moments
{
    public Sparse Level { get; set; } = Sparse.One;

    public Sparse? MaybeLevel { get; set; }

    public DateTime At { get; set; }

    public DateTime? MaybeAt { get; set; }

    public DateTimeOffset Offset { get; set; }

    public TimeOnly Time { get; set; }

    public DateTime Day { get; set; }
}

public sealed class BindingPreservationTests
{
    private static readonly FormSchema<Moments> Schema = FormSchema.For<Moments>(s =>
    {
        s.Field(m => m.Level);
        s.Field(m => m.MaybeLevel);
        s.Field(m => m.At);
        s.Field(m => m.MaybeAt);
        s.Field(m => m.Offset);
        s.Field(m => m.Time);
        s.Field(m => m.Day, f => f.Kind(FormFieldKind.Date));
    });

    [Fact]
    public void An_enum_value_without_a_member_survives_an_untouched_write()
    {
        var source = new Moments { Level = (Sparse)42, MaybeLevel = (Sparse)42 };
        var input = Schema.ToInput(source);

        var written = Schema.Write(input, new Moments { Level = (Sparse)42, MaybeLevel = (Sparse)42 });
        Assert.Equal((Sparse)42, written.Level);
        Assert.Equal((Sparse)42, written.MaybeLevel);

        var created = Schema.Create(input);
        Assert.Equal((Sparse)42, created.Level);
        Assert.Equal((Sparse?)42, created.MaybeLevel);
    }

    [Fact]
    public void An_enum_number_that_does_not_fit_the_underlying_type_is_refused()
    {
        var values = new Dictionary<string, object?> { ["Level"] = "99999999999" };
        Assert.Throws<InvalidOperationException>(() => Schema.Create(values));
    }

    [Fact]
    public void A_datetime_keeps_its_kind_and_ticks_when_it_is_untouched()
    {
        foreach (var kind in new[] { DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified })
        {
            var at = new DateTime(2024, 5, 6, 7, 8, 9, kind).AddTicks(1234567);
            var source = new Moments { At = at, MaybeAt = at, Offset = new DateTimeOffset(at.Ticks, TimeSpan.FromHours(2)), Time = new TimeOnly(at.TimeOfDay.Ticks) };
            var input = Schema.ToInput(source);

            var target = new Moments { At = at, MaybeAt = at, Offset = source.Offset, Time = source.Time };
            Schema.Write(input, target);

            Assert.Equal(at.Ticks, target.At.Ticks);
            Assert.Equal(kind, target.At.Kind);
            Assert.Equal(at.Ticks, target.MaybeAt!.Value.Ticks);
            Assert.Equal(kind, target.MaybeAt.Value.Kind);
            Assert.Equal(source.Offset, target.Offset);
            Assert.Equal(source.Time, target.Time);
        }
    }

    [Fact]
    public void An_untouched_date_field_keeps_the_time_of_the_datetime()
    {
        var day = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(55);
        var target = new Moments { Day = day };
        Schema.Write(Schema.ToInput(target), target);
        Assert.Equal(day, target.Day);
        Assert.Equal(DateTimeKind.Utc, target.Day.Kind);
    }

    [Fact]
    public void A_typed_datetime_takes_the_kind_of_the_existing_value_and_is_unspecified_for_a_new_one()
    {
        var values = new Dictionary<string, object?> { ["At"] = "2024-05-06T07:08", ["MaybeAt"] = "2024-05-06T07:08" };

        var utc = Schema.Write(values, new Moments { At = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaybeAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local) });
        Assert.Equal(new DateTime(2024, 5, 6, 7, 8, 0), utc.At);
        Assert.Equal(DateTimeKind.Utc, utc.At.Kind);
        Assert.Equal(DateTimeKind.Local, utc.MaybeAt!.Value.Kind);

        var fresh = Schema.Write(values, new Moments());
        Assert.Equal(DateTimeKind.Unspecified, fresh.At.Kind);
        Assert.Equal(DateTimeKind.Unspecified, fresh.MaybeAt!.Value.Kind);
        Assert.Equal(DateTimeKind.Unspecified, Schema.Create(values).At.Kind);
    }

    [Fact]
    public void The_text_of_a_datetime_keeps_the_milliseconds()
    {
        var input = Schema.ToInput(new Moments { At = new DateTime(2024, 5, 6, 7, 8, 9, 123) });
        Assert.Equal("2024-05-06T07:08:09.123", input["At"]);
    }
}
