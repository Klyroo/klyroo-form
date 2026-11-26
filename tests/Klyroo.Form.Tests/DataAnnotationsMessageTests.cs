using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Xunit;

namespace Klyroo.Form.Tests;

public static class ResourceTexts
{
    private static bool French => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";

    public static string Title => French ? "Titre" : "Title";

    public static string TooShort => French ? "{0} est trop court" : "{0} is too short";

    public static string Differs => French ? "{0} diffère" : "{0} differs";

    public static string Needed => French ? "{0} est requis" : "{0} is required";
}

public sealed class Localized
{
    [Display(ResourceType = typeof(ResourceTexts), Name = nameof(ResourceTexts.Title))]
    [MinLength(5, ErrorMessageResourceType = typeof(ResourceTexts), ErrorMessageResourceName = nameof(ResourceTexts.TooShort))]
    public string? Code { get; set; }

    public string? Other { get; set; }

    [Compare(nameof(Other), ErrorMessageResourceType = typeof(ResourceTexts), ErrorMessageResourceName = nameof(ResourceTexts.Differs))]
    public string? Confirm { get; set; }

    [Required(ErrorMessageResourceType = typeof(ResourceTexts), ErrorMessageResourceName = nameof(ResourceTexts.Needed))]
    public string Must { get; set; } = "";
}

public sealed class Ranged
{
    [StringLength(10, MinimumLength = 5)]
    public string? Text { get; set; }

    [Range(18, 65)]
    public int Inclusive { get; set; } = 30;

    [Range(typeof(decimal), "1", "5")]
    public decimal Typed { get; set; } = 2;

    [Range(1, 5, MinimumIsExclusive = true)]
    public int Exclusive { get; set; } = 2;

    [Range(1, 5, ErrorMessage = "Custom range")]
    public int Custom { get; set; } = 2;
}

public sealed class DataAnnotationsMessageTests
{
    private static T InCulture<T>(string name, Func<T> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Attribute_messages_follow_the_culture_of_the_validation_not_the_one_that_built_the_form()
    {
        var form = InCulture("en", () => FormSchema.For<Localized>(s => s.FromDataAnnotations()));
        var controller = new FormController<Localized>(form);
        controller.SetRaw("Code", "ab");
        controller.SetRaw("Other", "x");
        controller.SetRaw("Confirm", "y");

        Assert.Equal("Title is too short", InCulture("en", () => { controller.Validate(); return controller.ErrorOf("Code"); }));
        Assert.Equal("Titre est trop court", InCulture("fr", () => { controller.Validate(); return controller.ErrorOf("Code"); }));
        Assert.Equal("Confirm differs", InCulture("en", () => { controller.Validate(); return controller.ErrorOf("Confirm"); }));
        Assert.Equal("Confirm diffère", InCulture("fr", () => { controller.Validate(); return controller.ErrorOf("Confirm"); }));
    }

    [Fact]
    public void A_required_message_and_the_labels_are_fixed_when_the_form_is_built()
    {
        var form = InCulture("fr", () => FormSchema.For<Localized>(s => s.FromDataAnnotations()));
        Assert.Equal("Titre", form.Node("Code").Label);
        Assert.Equal("Must est requis", form.Node("Must").RequiredMessage);
        var controller = new FormController<Localized>(form);
        Assert.Equal("Must est requis", InCulture("en", () => { controller.Validate(); return controller.ErrorOf("Must"); }));
    }

    [Fact]
    public void Documented_mapping_of_range_and_string_length()
    {
        var form = FormSchema.For<Ranged>(s => s.FromDataAnnotations());

        var text = form.Node("Text");
        Assert.Equal(10, text.MaxLength);
        Assert.Contains(text.Validators!, v => v.Key == "stringLength");

        var inclusive = form.Node("Inclusive");
        Assert.Equal(18, inclusive.Min);
        Assert.Equal(65, inclusive.Max);
        Assert.Null(inclusive.Validators?.FirstOrDefault(v => v.Key == "Range"));

        foreach (var name in new[] { nameof(Ranged.Typed), nameof(Ranged.Exclusive), nameof(Ranged.Custom) })
        {
            var field = form.Node(name);
            Assert.NotEqual(1, field.Min);
            Assert.NotEqual(5, field.Max);
            Assert.Contains(field.Validators!, v => v.Key == "Range");
        }
        Assert.Equal("Custom range", form.Node("Custom").Validators!.Single(v => v.Key == "Range").Message);

        var controller = new FormController<Ranged>(form);
        controller.SetRaw("Text", "ab");
        controller.Validate();
        Assert.Equal("The field Text must be a string with a minimum length of 5 and a maximum length of 10.", controller.ErrorOf("Text"));
    }
}
