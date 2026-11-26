using Xunit;

namespace Klyroo.Form.Tests;

public sealed class BindingMessagesTests
{
    [Fact]
    public void The_messages_of_the_binding_are_localized()
    {
        var controller = new FormController<Person>(FormSchema.For<Person>(s => s.AllMembers()));
        controller.Messages = FormMessageLocales.Fr;
        controller.SetRaw("Age", 3.5);
        controller.SetRaw("Id", "not a guid");
        controller.Validate();

        Assert.Equal("Saisissez un nombre entier", controller.ErrorOf("Age"));
        Assert.Equal("Saisissez un identifiant valide", controller.ErrorOf("Id"));

        controller.Messages = FormMessageLocales.En;
        controller.Validate();
        Assert.Equal("Enter a whole number", controller.ErrorOf("Age"));
        Assert.Equal("Enter a valid identifier", controller.ErrorOf("Id"));
    }

    [Fact]
    public void Every_locale_translates_the_messages_of_the_binding()
    {
        foreach (var (code, messages) in FormMessageLocales.Tables)
        {
            Assert.False(string.IsNullOrWhiteSpace(messages.WholeNumber), code);
            Assert.False(string.IsNullOrWhiteSpace(messages.InvalidIdentifier), code);
            Assert.False(string.IsNullOrWhiteSpace(messages.InvalidValue), code);
            var range = messages.NumberOutOfRange("-128", "127");
            Assert.Contains("128", range, StringComparison.Ordinal);
            Assert.Contains("127", range, StringComparison.Ordinal);
            if (code == "en") continue;
            Assert.NotEqual(FormMessageLocales.En.WholeNumber, messages.WholeNumber);
            Assert.NotEqual(FormMessageLocales.En.InvalidIdentifier, messages.InvalidIdentifier);
            Assert.NotEqual(FormMessageLocales.En.InvalidValue, messages.InvalidValue);
            Assert.NotEqual(FormMessageLocales.En.NumberOutOfRange("-128", "127"), range);
        }
    }
}
