using Klyroo.Form;

var schema = FormSchema.For<Signup>(s =>
{
    s.FormKey = "signup";
    s.Field(m => m.Email, f => f.Required());
    s.Field(m => m.Plan, f => f.Options([new FormOption<string>("free", "Free"), new FormOption<string>("pro", "Pro")]));
});
var controller = new FormController<Signup>(schema);
controller.Set(schema.Field(m => m.Email), "alice@klyroo.dev", markTouched: true);
await controller.SubmitAsync((signup, _) => Task.FromResult(true));
Console.WriteLine($"Submitted email: {controller.CreateModel().Email}");

public sealed class Signup
{
    public string Email { get; set; } = "";

    public string Plan { get; set; } = "pro";
}
