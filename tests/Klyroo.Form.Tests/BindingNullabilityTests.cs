using System.Reflection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class Notes
{
    public string Plain { get; set; } = "";

    public string? Optional { get; set; }
}

public sealed class BindingNullabilityTests
{
    private static PropertyInfo Prop(string name) => typeof(Notes).GetProperty(name)!;

    [Fact]
    public void The_annotation_decides_when_it_is_available()
    {
        Assert.True(FormNullability.IsNullableReference(Prop("Optional"), null));
        Assert.False(FormNullability.IsNullableReference(Prop("Plain"), null));
    }

    [Fact]
    public void Without_nullability_metadata_a_string_is_not_nullable_unless_the_builder_says_so()
    {
        // The state the metadata gives when a trimmed app removed it: the feature switch is off.
        static NullabilityState? Throws(PropertyInfo _) => throw new InvalidOperationException("not available");
        static NullabilityState? Unknown(PropertyInfo _) => NullabilityState.Unknown;

        Assert.False(FormNullability.IsNullableReference(Prop("Optional"), null, supported: false, Throws));
        Assert.False(FormNullability.IsNullableReference(Prop("Optional"), null, supported: true, Throws));
        Assert.False(FormNullability.IsNullableReference(Prop("Optional"), null, supported: true, Unknown));
        Assert.False(FormNullability.IsNullableReference(Prop("Optional"), null, supported: false, _ => NullabilityState.Nullable));

        Assert.True(FormNullability.IsNullableReference(Prop("Optional"), true, supported: false, Throws));
        Assert.True(FormNullability.IsNullableReference(Prop("Plain"), true, supported: true, Unknown));
        Assert.False(FormNullability.IsNullableReference(Prop("Optional"), false, supported: true, _ => NullabilityState.Nullable));
    }

    [Fact]
    public void The_builder_can_choose_what_an_empty_string_becomes()
    {
        var form = FormModel.For<Notes>(b => b
            .Field(n => n.Plain, f => f.NullWhenEmpty())
            .Field(n => n.Optional, f => f.NullWhenEmpty(false)));
        var written = form.Create(new Dictionary<string, object?> { ["Plain"] = "", ["Optional"] = "" });
        Assert.Null(written.Plain);
        Assert.Equal("", written.Optional);

        var byAnnotation = FormModel.For<Notes>(b => b.AllMembers()).Create(new Dictionary<string, object?> { ["Plain"] = "", ["Optional"] = "" });
        Assert.Equal("", byAnnotation.Plain);
        Assert.Null(byAnnotation.Optional);
    }
}
