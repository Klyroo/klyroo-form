using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Xunit;

namespace Klyroo.Form.Tests;

public sealed class BindingMessagesTests
{
    [Fact]
    public void The_messages_of_the_binding_are_localized()
    {
        var form = FormModel.For<Person>(b => b.AllMembers());
        var controller = form.CreateController();
        controller.Messages = FormMessageLocales.Fr;
        controller.SetValue("Age", 3.5);
        controller.SetValue("Id", "not a guid");
        controller.Validate();

        Assert.Equal("Saisissez un nombre entier", controller.GetError("Age"));
        Assert.Equal("Saisissez un identifiant valide", controller.GetError("Id"));

        controller.Messages = FormMessageLocales.En;
        controller.Validate();
        Assert.Equal("Enter a whole number", controller.GetError("Age"));
        Assert.Equal("Enter a valid identifier", controller.GetError("Id"));
    }

    [Fact]
    public void Every_locale_translates_the_messages_of_the_binding()
    {
        foreach (var (code, messages) in FormMessageLocales.Tables)
        {
            Assert.False(string.IsNullOrWhiteSpace(messages.WholeNumber), code);
            Assert.False(string.IsNullOrWhiteSpace(messages.InvalidIdentifier), code);
            var range = messages.NumberOutOfRange("-128", "127");
            Assert.Contains("128", range, StringComparison.Ordinal);
            Assert.Contains("127", range, StringComparison.Ordinal);
            if (code == "en") continue;
            Assert.NotEqual(FormMessageLocales.En.WholeNumber, messages.WholeNumber);
            Assert.NotEqual(FormMessageLocales.En.InvalidIdentifier, messages.InvalidIdentifier);
            Assert.NotEqual(FormMessageLocales.En.NumberOutOfRange("-128", "127"), range);
        }
    }
}
