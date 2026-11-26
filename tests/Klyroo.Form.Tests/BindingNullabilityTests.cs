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
        var schema = FormSchema.For<Notes>(s =>
        {
            s.Field(n => n.Plain, f => f.NullWhenEmpty());
            s.Field(n => n.Optional, f => f.NullWhenEmpty(false));
        });
        var written = schema.Create(new Dictionary<string, object?> { ["Plain"] = "", ["Optional"] = "" });
        Assert.Null(written.Plain);
        Assert.Equal("", written.Optional);

        var byAnnotation = FormSchema.For<Notes>(s => s.AllMembers()).Create(new Dictionary<string, object?> { ["Plain"] = "", ["Optional"] = "" });
        Assert.Equal("", byAnnotation.Plain);
        Assert.Null(byAnnotation.Optional);
    }

    [Fact]
    public void A_typed_field_follows_the_annotation_for_what_an_empty_text_becomes()
    {
        var schema = FormSchema.For<Notes>(s =>
        {
            s.Field(n => n.Plain);
            s.Field(n => n.Optional);
        });
        var controller = new FormController<Notes>(schema, new Notes { Plain = "a", Optional = "b" });
        controller.Set(schema.Field(n => n.Plain), "");
        controller.Set(schema.Field(n => n.Optional), "");
        var model = controller.CreateModel();
        Assert.Equal("", model.Plain);
        Assert.Null(model.Optional);
    }
}
