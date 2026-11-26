using Bunit;
using Klyroo.Form.Dynamic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class DynamicFieldViewTests : BunitContext
{
    public DynamicFieldViewTests()
    {
        Services.AddKlyrooForm();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static DynamicFormController Form() => new(new DynamicFormSchema
    {
        Fields =
        [
            new DynamicFieldSchema { Key = "name", Label = "Name" },
            new DynamicFieldSchema { Key = "age", Label = "Age", Kind = FormFieldKind.Number },
        ],
    });

    [Fact]
    public void A_field_is_rendered_by_its_key()
    {
        var form = Form();
        var cut = Render<KlyDynamicFieldView>(p => p.Add(x => x.Form, form).Add(x => x.Key, "age"));
        Assert.Equal("number", cut.Find("input").GetAttribute("type"));
        cut.Find("input").Input("42");
        Assert.Equal(42.0, form.GetValue("age"));
    }

    [Fact]
    public void An_unknown_key_renders_nothing_and_a_given_field_wins_over_the_key()
    {
        var form = Form();
        Assert.Empty(Render<KlyDynamicFieldView>(p => p.Add(x => x.Form, form).Add(x => x.Key, "nope")).Markup.Trim());
        var cut = Render<KlyDynamicFieldView>(p => p.Add(x => x.Form, form).Add(x => x.Field, form.Schema.Fields[0]).Add(x => x.Key, "age"));
        Assert.Equal("text", cut.Find("input").GetAttribute("type"));
    }
}
