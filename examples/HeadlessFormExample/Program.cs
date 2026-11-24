using Blux.UI.Tools;

var schema = new FormSchema
{
    FormKey = "signup",
    Fields =
    [
        new FormFieldSchema { Key = "email", Label = "Email", Required = true },
        new FormFieldSchema { Key = "plan", Label = "Plan", DefaultValue = "pro" }
    ]
};
var controller = new FormController(schema);
controller.SetValue("email", "alice@luxstack.dev", markTouched: true);
await controller.SubmitAsync();
Console.WriteLine($"Submitted email: {controller.BuildOutput()["email"]}");
