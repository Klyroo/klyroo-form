using Xunit;

namespace Klyroo.Form.Tests;

public struct Coordinates
{
    public int X { get; set; }
}

public sealed class Inner
{
    public string Value { get; set; } = "";
}

public sealed class Holder
{
    public string Name { get; set; } = "start";

    public int Count { get; set; } = 1;

    public Inner? Inner { get; set; }
}

public sealed class Strict
{
    public Strict(int required) => Required = required;

    public int Required { get; set; }

    public string Name { get; set; } = "";
}

public sealed class BindingAtomicWriteTests
{
    private static FormModel<Holder> HolderForm() => FormModel.For<Holder>(b => b
        .Field(h => h.Name).Field(h => h.Count).Field(h => h.Inner!.Value));

    [Fact]
    public void A_struct_model_is_rejected_with_a_clear_message()
    {
        var error = Assert.Throws<ArgumentException>(() => FormModel.For<Coordinates>(b => b.Field(c => c.X)));
        Assert.Contains("struct", error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Coordinates), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_intermediate_object_without_a_creator_fails_before_anything_is_written()
    {
        var form = HolderForm();
        var target = new Holder();
        var values = new Dictionary<string, object?> { ["Name"] = "changed", ["Count"] = 5.0, ["Inner.Value"] = "x" };

        Assert.Throws<InvalidOperationException>(() => form.Write(values, target));
        Assert.Equal("start", target.Name);
        Assert.Equal(1, target.Count);
        Assert.Null(target.Inner);
    }

    [Fact]
    public void A_value_that_cannot_be_converted_fails_before_anything_is_written()
    {
        var form = HolderForm();
        var target = new Holder { Inner = new Inner() };
        var values = new Dictionary<string, object?> { ["Name"] = "changed", ["Count"] = "abc", ["Inner.Value"] = "x" };

        Assert.Throws<InvalidOperationException>(() => form.Write(values, target));
        Assert.Equal("start", target.Name);
        Assert.Equal(1, target.Count);
        Assert.Equal("", target.Inner.Value);
    }

    [Fact]
    public void A_setter_that_throws_leaves_the_target_as_it_was()
    {
        var form = FormModel.For<Flaky>(b => b.Field(f => f.First).Field(f => f.Second));
        var target = new Flaky();
        var values = new Dictionary<string, object?> { ["First"] = "a", ["Second"] = "b" };

        Assert.Throws<InvalidOperationException>(() => form.Write(values, target));
        Assert.Equal("", target.First);
    }

    [Theory]
    [InlineData("Long", "9223372036854775808")]
    [InlineData("ULong", "-1")]
    [InlineData("Uint", "4294967296")]
    [InlineData("Short", "32768")]
    [InlineData("SByte", "-129")]
    [InlineData("Money", "79228162514264337593543950336")]
    [InlineData("Ratio", "1e39")]
    public void A_number_outside_the_type_is_refused_instead_of_becoming_zero(string key, string text)
    {
        var form = FormModel.For<Numbers>(b => b.AllMembers());
        var values = new Dictionary<string, object?> { [key] = FormValueConverter.NumberFromInput(text) };

        Assert.Throws<InvalidOperationException>(() => form.Create(values));
    }

    [Fact]
    public async Task A_save_that_returns_false_leaves_the_target_unchanged()
    {
        var form = HolderForm();
        var target = new Holder { Inner = new Inner() };
        var controller = form.CreateController(target);
        controller.SetValue("Name", "changed");
        controller.SetValue("Inner.Value", "x");
        Holder? seen = null;

        var ok = await form.SubmitAsync(controller, (m, _) => { seen = new Holder { Name = m.Name }; return Task.FromResult(false); }, target, TestContext.Current.CancellationToken);

        Assert.False(ok);
        Assert.Equal("changed", seen!.Name);
        Assert.Equal("start", target.Name);
        Assert.Equal("", target.Inner.Value);
    }

    [Fact]
    public async Task A_save_that_throws_leaves_the_target_unchanged()
    {
        var form = HolderForm();
        var target = new Holder { Inner = new Inner() };
        var controller = form.CreateController(target);
        controller.SetValue("Name", "changed");

        await Assert.ThrowsAsync<TimeoutException>(() => form.SubmitAsync(controller, (_, _) => throw new TimeoutException(), target, TestContext.Current.CancellationToken));
        Assert.Equal("start", target.Name);

        var handler = form.OnSaveOutput((_, _) => throw new TimeoutException(), target);
        await Assert.ThrowsAsync<TimeoutException>(() => handler(controller.BuildOutput(), TestContext.Current.CancellationToken));
        Assert.Equal("start", target.Name);
    }

    [Fact]
    public async Task A_save_that_succeeds_keeps_the_written_values()
    {
        var form = HolderForm();
        var target = new Holder { Inner = new Inner() };
        var controller = form.CreateController(target);
        controller.SetValue("Name", "changed");

        Assert.True(await form.SubmitAsync(controller, (_, _) => Task.FromResult(true), target, TestContext.Current.CancellationToken));
        Assert.Equal("changed", target.Name);
    }

    [Fact]
    public async Task A_created_nested_object_is_detached_again_when_the_save_fails()
    {
        var form = FormModel.For<Holder>(b => b.Field(h => h.Name).Nested(h => h.Inner!, n => n.Field(i => i.Value)));
        var target = new Holder();
        var values = new Dictionary<string, object?> { ["Name"] = "changed", ["Inner.Value"] = "x" };

        var handler = form.OnSaveOutput((_, _) => Task.FromResult(false), target);
        Assert.False(await handler(values, TestContext.Current.CancellationToken));
        Assert.Null(target.Inner);
        Assert.Equal("start", target.Name);
    }

    [Fact]
    public void A_model_check_without_a_way_to_create_the_model_fails_when_the_form_is_built()
    {
        var error = Assert.Throws<InvalidOperationException>(() => FormModel.For<Strict>(b => b.Field(s => s.Name).Validate((_, _) => { })));
        Assert.Contains("Factory", error.Message, StringComparison.Ordinal);

        var form = FormModel.For<Strict>(b => b.Field(s => s.Name).Factory(() => new Strict(1)).Validate((_, _) => { }));
        Assert.Equal(1, form.Create(new Dictionary<string, object?> { ["Name"] = "x" }).Required);

        Assert.NotNull(FormModel.For<Strict>(b => b.Field(s => s.Name)));
    }
}

public sealed class Flaky
{
    private string _first = "";

    public string First
    {
        get => _first;
        set => _first = value;
    }

    public string Second
    {
        get => "";
        set => throw new InvalidOperationException("no");
    }
}
