using Xunit;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Tests;

public sealed class CrossFieldRevalidationTests
{
    private static DynamicFieldSchema Password() => new() { Key = "password", Kind = FormFieldKind.Password };

    private static DynamicFieldSchema Confirm(bool dependsOn = false, FormValidationTrigger trigger = FormValidationTrigger.Blur) => new()
    {
        Key = "confirm",
        Kind = FormFieldKind.Password,
        Trigger = trigger,
        DependsOn = dependsOn ? ["password"] : null,
        Validators = [new DynamicValidatorRule("match", "Passwords differ",
            (v, f) => Equals(v ?? "", f.GetValue("password") ?? ""))],
    };

    private static DynamicFormController Form(params DynamicFieldSchema[] fields) =>
        new(new DynamicFormSchema { FormKey = "x", Fields = fields });

    [Fact]
    public void Changing_the_password_clears_the_stale_confirm_error()
    {
        var form = Form(Password(), Confirm());
        form.SetValue("password", "abc");
        form.SetValue("confirm", "abd", markTouched: true);
        Assert.Equal("Passwords differ", form.GetError("confirm"));
        Assert.True(form.HasBlockingErrors);

        form.SetValue("password", "abd");

        Assert.Null(form.GetError("confirm"));
        Assert.False(form.HasBlockingErrors);
    }

    [Fact]
    public void Without_DependsOn_a_touched_valid_field_does_not_gain_an_error()
    {
        var form = Form(Password(), Confirm());
        form.SetValue("password", "abc");
        form.SetValue("confirm", "abc", markTouched: true);
        Assert.Null(form.GetError("confirm"));

        form.SetValue("password", "xyz");

        Assert.Null(form.GetError("confirm"));
    }

    [Fact]
    public void DependsOn_raises_a_new_mismatch_on_a_touched_field_and_clears_it_again()
    {
        var form = Form(Password(), Confirm(dependsOn: true));
        form.SetValue("password", "abc");
        form.SetValue("confirm", "abc", markTouched: true);

        form.SetValue("password", "xyz");
        Assert.Equal("Passwords differ", form.GetError("confirm"));
        Assert.True(form.HasBlockingErrors);

        form.SetValue("password", "abc");
        Assert.Null(form.GetError("confirm"));
        Assert.False(form.HasBlockingErrors);
    }

    [Fact]
    public void DependsOn_does_not_validate_a_field_the_user_has_not_touched()
    {
        var form = Form(Password(), Confirm(dependsOn: true));

        form.SetValue("password", "abc");

        Assert.Null(form.GetError("confirm"));
        Assert.False(form.HasBlockingErrors);
    }

    [Fact]
    public void Fields_with_the_Submit_trigger_wait_for_submission()
    {
        var form = Form(Password(), Confirm(dependsOn: true, trigger: FormValidationTrigger.Submit));
        form.SetValue("password", "abc");
        Assert.False(form.Validate());
        Assert.Equal("Passwords differ", form.GetError("confirm"));

        form.SetValue("password", "");

        // The Submit trigger does not validate while editing, so the dependency change leaves the error as it is.
        Assert.Equal("Passwords differ", form.GetError("confirm"));
        Assert.True(form.Validate());
    }

    [Fact]
    public void RequiredWhen_error_clears_when_the_condition_stops_holding()
    {
        var form = Form(
            new DynamicFieldSchema { Key = "kind" },
            new DynamicFieldSchema { Key = "vat", RequiredWhen = f => f.GetValue<string>("kind") == "company" });
        form.SetValue("kind", "company");
        form.MarkTouched("vat");
        Assert.Equal("This field is required", form.GetError("vat"));
        Assert.True(form.HasBlockingErrors);

        form.SetValue("kind", "person");

        Assert.Null(form.GetError("vat"));
        Assert.False(form.HasBlockingErrors);
    }

    [Fact]
    public void RequiredWhen_error_shows_up_through_DependsOn_once_the_field_is_touched()
    {
        var form = Form(
            new DynamicFieldSchema { Key = "kind" },
            new DynamicFieldSchema { Key = "vat", DependsOn = ["kind"], RequiredWhen = f => f.GetValue<string>("kind") == "company" });
        form.MarkTouched("vat");
        Assert.Null(form.GetError("vat"));

        form.SetValue("kind", "company");

        Assert.Equal("This field is required", form.GetError("vat"));
    }

    [Fact]
    public void Validate_then_fixing_the_other_field_clears_the_error_on_the_dependent_one()
    {
        var form = Form(Password(), Confirm());
        form.SetValue("password", "abc");
        form.SetValue("confirm", "abd");
        Assert.False(form.Validate());

        form.SetValue("password", "abd");

        Assert.Null(form.GetError("confirm"));
        Assert.True(form.Validate());
    }

    [Fact]
    public void ResetField_revalidates_the_fields_that_depend_on_it()
    {
        var form = new DynamicFormController(
            new DynamicFormSchema { FormKey = "x", Fields = [Password(), Confirm()] },
            new Dictionary<string, object?> { ["password"] = "abc", ["confirm"] = "abc" });
        form.SetValue("password", "zzz");
        form.SetValue("confirm", "abc", markTouched: true);
        Assert.Equal("Passwords differ", form.GetError("confirm"));

        form.ResetField("password");

        Assert.Null(form.GetError("confirm"));
    }

    [Fact]
    public void Cell_errors_clear_when_the_field_they_compare_with_changes()
    {
        var lines = new DynamicFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayList,
            Fields =
            [
                new DynamicFieldSchema
                {
                    Key = "qty",
                    Kind = FormFieldKind.Number,
                    Validators = [new DynamicValidatorRule("cap", "Above the cap",
                        (v, f) => v is not double d || d <= (f.GetValue<double?>("cap") ?? double.MaxValue))],
                },
            ],
        };
        var form = Form(new DynamicFieldSchema { Key = "cap", Kind = FormFieldKind.Number }, lines);
        form.SetValue("cap", 5.0);
        form.AddArrayItem(lines);
        form.SetArrayItemValue("lines", 0, "qty", 9.0, markTouched: true);
        Assert.Equal("Above the cap", form.GetArrayItemError("lines", 0, "qty"));

        form.SetValue("cap", 10.0);

        Assert.Null(form.GetArrayItemError("lines", 0, "qty"));
        Assert.False(form.HasBlockingErrors);
    }

    [Fact]
    public void A_cell_DependsOn_raises_an_error_on_a_touched_cell()
    {
        var lines = new DynamicFieldSchema
        {
            Key = "lines",
            Kind = FormFieldKind.ArrayList,
            Fields =
            [
                new DynamicFieldSchema
                {
                    Key = "qty",
                    Kind = FormFieldKind.Number,
                    DependsOn = ["cap"],
                    Validators = [new DynamicValidatorRule("cap", "Above the cap",
                        (v, f) => v is not double d || d <= (f.GetValue<double?>("cap") ?? double.MaxValue))],
                },
            ],
        };
        var form = Form(new DynamicFieldSchema { Key = "cap", Kind = FormFieldKind.Number }, lines);
        form.SetValue("cap", 10.0);
        form.AddArrayItem(lines);
        form.SetArrayItemValue("lines", 0, "qty", 9.0, markTouched: true);
        Assert.Null(form.GetArrayItemError("lines", 0, "qty"));

        form.SetValue("cap", 5.0);

        Assert.Equal("Above the cap", form.GetArrayItemError("lines", 0, "qty"));
    }
}
