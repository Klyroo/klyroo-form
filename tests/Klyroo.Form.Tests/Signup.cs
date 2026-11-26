namespace Klyroo.Form.Tests;

public sealed class Signup
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Email { get; set; }

    public int Rating { get; set; } = 3;

    public string Nickname { get; set; } = "";
}

// The schema of the typed markup pages, with the handles the pages and the tests use.
public sealed class SignupForm
{
    public SignupForm()
    {
        var name = default(FormField<Signup, string>);
        var rating = default(FormField<Signup, int>);
        var nickname = default(FormField<Signup, string>);
        Schema = FormSchema.For<Signup>(s =>
        {
            s.Title = "Sign up";
            name = s.Field(m => m.Name, f => f.Required().Description("As on your ID").Help("Shown on your badge"));
            s.Field(m => m.Email);
            rating = s.Field(m => m.Rating, f => f.Range(1, 5));
            nickname = s.Field(m => m.Nickname, f => f.AsCustom("nick").Label("Nickname"));
        });
        Name = name!;
        Rating = rating!;
        Nickname = nickname!;
    }

    public FormSchema<Signup> Schema { get; }

    public FormField<Signup, string> Name { get; }

    public FormField<Signup, int> Rating { get; }

    public FormField<Signup, string> Nickname { get; }
}
