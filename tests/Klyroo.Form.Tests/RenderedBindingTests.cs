using Bunit;
using Klyroo.Form.Tests;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

// The rendered halves of the binding scenarios, with the typed components. The halves that need no rendering are in ModelBindingTests, BindingNumericExactnessTests and DataAnnotationsBindingTests.
public sealed class RenderedBindingTests : BunitContext
{
    private static readonly FormSchema<Numbers> NumberSchema = FormSchema.For<Numbers>(s => s.AllMembers());

    public RenderedBindingTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task A_rendered_engine_is_bound_to_a_model_and_submits_a_typed_instance()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Title = "Person";
            s.Field(p => p.Name, f => f.Required().Placeholder("Name"));
            s.Field(p => p.Age);
            s.Field(p => p.Role);
            s.Field(p => p.Birthday);
            s.Group(p => p.Address!, g => g.Field(a => a.City));
        });
        var source = new Person { Name = "Ada", Age = 36, Role = Role.Admin, Birthday = new DateOnly(1990, 5, 17) };
        Person? saved = null;
        Person? submitted = null;

        var cut = Render<KlyFormEngine<Person>>(p => p
            .Add(x => x.Schema, schema)
            .Add(x => x.Model, source)
            .Add(x => x.OnSave, (person, _) => { saved = person; return Task.FromResult(true); })
            .Add(x => x.OnSubmit, EventCallback.Factory.Create<Person>(this, person => submitted = person)));

        Assert.Equal("Person", cut.Find("[data-slot=form-title]").TextContent);
        cut.Find("[data-form-field=Name] input").Input("Grace");
        cut.Find("[data-form-field=Address\\.City] input").Input("Lyon");
        await cut.Find("form").SubmitAsync();

        Assert.NotNull(saved);
        Assert.Equal("Grace", saved.Name);
        Assert.Equal(36, saved.Age);
        Assert.Equal(Role.Admin, saved.Role);
        Assert.Equal(new DateOnly(1990, 5, 17), saved.Birthday);
        Assert.Equal("Lyon", saved.Address!.City);
        Assert.Same(saved, submitted);
    }

    [Fact]
    public void A_number_control_shows_and_posts_the_exact_value()
    {
        var model = new Numbers { Long = long.MaxValue, Money = 1234567890123456789.1234567890m, Ratio = 0.1f };
        var controller = new FormController<Numbers>(NumberSchema, model);
        var cut = Render<KlyFormEngine<Numbers>>(p => p.Add(x => x.Form, controller));

        Assert.Equal("9223372036854775807", cut.Find("[data-form-field=Long] input").GetAttribute("value"));
        Assert.Equal("1234567890123456789.1234567890", cut.Find("[data-form-field=Money] input").GetAttribute("value"));
        Assert.Equal("0.1", cut.Find("[data-form-field=Ratio] input").GetAttribute("value"));

        cut.Find("[data-form-field=Long] input").Input("9223372036854775806");
        cut.Find("[data-form-field=ULong] input").Input("18446744073709551615");
        cut.Find("[data-form-field=Money] input").Input("79228162514264337593543950335");
        cut.Find("[data-form-field=Ratio] input").Input("0.3");
        cut.Find("[data-form-field=Precise] input").Input("1.2345678901234567");

        var typed = controller.CreateModel();
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
        var controller = new FormController<Numbers>(NumberSchema, new Numbers());
        var cut = Render<KlyFormEngine<Numbers>>(p => p.Add(x => x.Form, controller));
        cut.Find($"[data-form-field={key}] input").Input(text);

        Assert.False(controller.Validate());
        Assert.False(string.IsNullOrEmpty(controller.ErrorOf(key)));
        cut.Render();
        Assert.False(string.IsNullOrEmpty(cut.Find($"[data-form-field={key}] [data-slot=form-field-error]").TextContent));
    }

    [Fact]
    public void A_fractional_value_is_not_a_whole_number()
    {
        var controller = new FormController<Numbers>(NumberSchema, new Numbers());
        var cut = Render<KlyFormEngine<Numbers>>(p => p.Add(x => x.Form, controller));
        cut.Find("[data-form-field=Long] input").Input("3.5");

        Assert.False(controller.Validate());
        Assert.Equal("Enter a whole number", controller.ErrorOf("Long"));
    }

    [Fact]
    public void Output_and_dirty_tracking_stay_consistent_when_the_same_text_is_typed_back()
    {
        var controller = new FormController<Numbers>(NumberSchema, new Numbers { Long = long.MaxValue, Precise = 1.2345678901234567 });
        var cut = Render<KlyFormEngine<Numbers>>(p => p.Add(x => x.Form, controller));
        cut.Find("[data-form-field=Long] input").Input("9223372036854775807");
        cut.Find("[data-form-field=Precise] input").Input("1.2345678901234567");

        Assert.False(controller.IsDirty);
    }

    [Fact]
    public async Task A_rendered_engine_shows_attribute_errors_and_submits_the_typed_model()
    {
        var schema = FormSchema.For<DataAnnotationsBindingTests.Signup>(s => s.FromDataAnnotations());
        DataAnnotationsBindingTests.Signup? saved = null;
        var cut = Render<KlyFormEngine<DataAnnotationsBindingTests.Signup>>(p => p
            .Add(x => x.Schema, schema)
            .Add(x => x.Model, new DataAnnotationsBindingTests.Signup())
            .Add(x => x.OnSave, (s, _) => { saved = s; return Task.FromResult(true); }));

        await cut.Find("form").SubmitAsync();
        Assert.Null(saved);
        Assert.Contains("This field is required", cut.Find("[data-form-field=Name] [data-slot=form-field-error]").TextContent);

        cut.Find("[data-form-field=Name] input").Input("Ada Lovelace");
        cut.Find("[data-form-field=Email] input").Input("ada@example.com");
        cut.Find("[data-form-field=Age] input").Input("36");
        await cut.Find("form").SubmitAsync();

        Assert.NotNull(saved);
        Assert.Equal("Ada Lovelace", saved.Name);
        Assert.Equal("ada@example.com", saved.Email);
        Assert.Equal(36, saved.Age);
    }
}
