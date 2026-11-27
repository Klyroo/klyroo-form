using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public sealed class Person
{
    public string Name { get; set; } = "";

    public string? Email { get; set; }

    public int Age { get; set; } = 30;

    public bool Subscribed { get; set; }

    public string Nick { get; set; } = "";
}

// The schema of the typed tests, with the handles the tests use.
public sealed class PersonForm
{
    public PersonForm(bool sections = false)
    {
        var name = default(FormField<Person, string>);
        var email = default(FormField<Person, string?>);
        var age = default(FormField<Person, int>);
        var subscribed = default(FormField<Person, bool>);
        var nick = default(FormField<Person, string>);
        Schema = FormSchema.For<Person>(s =>
        {
            s.Title = "Person";
            name = s.Field(m => m.Name, f => f.Required().Label("Name"));
            email = s.Field(m => m.Email, f => f.Label("Email"));
            age = s.Field(m => m.Age, f => f.Label("Age").Range(0, 120));
            subscribed = s.Field(m => m.Subscribed, f => f.Label("Subscribed"));
            nick = s.Field(m => m.Nick, f => f.AsCustom("nick").Label("Nickname"));
            if (sections) s.Section("main", "Main", _ => { });
        });
        Name = name!;
        Email = email!;
        Age = age!;
        Subscribed = subscribed!;
        Nick = nick!;
    }

    public FormSchema<Person> Schema { get; }

    public FormField<Person, string> Name { get; }

    public FormField<Person, string?> Email { get; }

    public FormField<Person, int> Age { get; }

    public FormField<Person, bool> Subscribed { get; }

    public FormField<Person, string> Nick { get; }
}

public class FormTypedTests
{
    private static FormFieldView View<TModel>(KlyFormEngine<TModel> engine, string key)
        where TModel : class =>
        engine.ShellView!.Views<FormFieldView>().First(v => v.Node.Key == key && !v.IsCell);

    [Fact]
    public void A_schema_and_a_model_make_a_form_that_shows_the_values_of_the_model()
    {
        var person = new Person { Name = "Ada", Age = 36 };
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema, Model = person };

        Assert.NotNull(engine.EffectiveController);
        Assert.Equal("Ada", ((FormTextControl)View(engine, "Name").Control).Input.Text);
        Assert.Equal(36m, ((FormNumberControl)View(engine, "Age").Control).Input.Value);
        Assert.False(engine.EffectiveController!.IsDirty);
        Assert.Equal("Person", engine.ShellView!.TitleLabel.Text);
    }

    [Fact]
    public void The_types_of_the_members_pick_the_controls()
    {
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema };

        Assert.IsType<FormTextControl>(View(engine, "Name").Control);
        Assert.IsType<FormNumberControl>(View(engine, "Age").Control);
        Assert.IsType<FormCheckControl>(View(engine, "Subscribed").Control);
        Assert.IsType<FormCustomControl>(View(engine, "Nick").Control);
    }

    [Fact]
    public async Task A_submit_writes_the_edits_into_the_model_and_raises_Submitted_with_it()
    {
        var person = new Person { Name = "Ada", Age = 36, Nick = "keep" };
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema, Model = person };
        Person? submitted = null;
        engine.Submitted += (_, e) => submitted = e.Model;
        ((FormTextControl)View(engine, "Name").Control).Input.Text = "Grace";
        ((FormNumberControl)View(engine, "Age").Control).Input.Value = 45m;

        Assert.True(await engine.SubmitAsync());

        Assert.Same(person, submitted);
        Assert.Equal("Grace", person.Name);
        Assert.Equal(45, person.Age);
        Assert.Equal("keep", person.Nick);
        Assert.False(engine.EffectiveController!.IsDirty);
    }

    [Fact]
    public async Task Without_a_model_a_submit_makes_a_new_one()
    {
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema };
        Person? submitted = null;
        engine.Submitted += (_, e) => submitted = e.Model;
        ((FormTextControl)View(engine, "Name").Control).Input.Text = "Ada";

        Assert.True(await engine.SubmitAsync());

        Assert.Equal("Ada", submitted!.Name);
    }

    [Fact]
    public async Task OnSave_runs_with_the_model_and_a_false_keeps_the_form_dirty_and_the_model_untouched()
    {
        var person = new Person { Name = "Ada" };
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema, Model = person };
        Person? saved = null;
        engine.OnSave = (model, _) =>
        {
            saved = model;
            return Task.FromResult(false);
        };
        var raised = false;
        engine.Submitted += (_, _) => raised = true;
        ((FormTextControl)View(engine, "Name").Control).Input.Text = "Grace";

        Assert.False(await engine.SubmitAsync());

        Assert.NotNull(saved);
        Assert.False(raised);
        Assert.Equal("Ada", person.Name);
        Assert.True(engine.EffectiveController!.IsDirty);
    }

    [Fact]
    public async Task A_failed_validation_shows_the_error_of_the_member()
    {
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema };

        Assert.False(await engine.SubmitAsync());

        Assert.Equal(FormMessages.Default.Required, View(engine, "Name").Field.Error);
        Assert.Equal("Name: " + FormMessages.Default.Required, engine.ShellView!.Summary.Lines[0].Label.Text);
    }

    [Fact]
    public void A_different_model_instance_reloads_the_form_and_drops_the_edits()
    {
        var schema = new PersonForm().Schema;
        var engine = new KlyFormEngine<Person> { Schema = schema, Model = new Person { Name = "Ada" } };
        var controller = engine.EffectiveController;
        ((FormTextControl)View(engine, "Name").Control).Input.Text = "edited";

        engine.Model = new Person { Name = "Grace" };

        Assert.Same(controller, engine.EffectiveController);
        Assert.Equal("Grace", ((FormTextControl)View(engine, "Name").Control).Input.Text);
        Assert.False(controller!.IsDirty);
    }

    [Fact]
    public void A_given_controller_wins_over_the_schema()
    {
        var form = new PersonForm();
        var controller = new FormController<Person>(form.Schema, new Person { Name = "Ada" });
        var engine = new KlyFormEngine<Person> { Form = controller, Schema = new PersonForm().Schema };

        Assert.Same(controller, engine.EffectiveController);
        controller.Set(form.Name, "Changed from code");
        Assert.Equal("Changed from code", ((FormTextControl)View(engine, "Name").Control).Input.Text);
    }

    [Fact]
    public void The_controller_follows_the_culture_messages_of_the_texts()
    {
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema };

        Assert.Same(FormTexts.Current.Messages, engine.EffectiveController!.Messages);
    }

    [Fact]
    public void Dispose_cancels_the_pending_checks_and_a_disposed_form_ignores_the_controller()
    {
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema };
        var controller = engine.EffectiveController!;

        engine.Dispose();
        controller.Reset();

        Assert.Null(engine.Content);
    }

    // ---- templates

    [Fact]
    public void A_template_draws_the_control_of_its_field_from_the_context_of_the_field()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person { Nick = "ada" } };
        var template = new KlyFieldTemplate<Person, string>(form.Nick, ctx => new Entry { Text = ctx.Value, AutomationId = ctx.Id });
        engine.Templates.Add(template);

        var control = Assert.IsType<FormCustomControl>(View(engine, "Nick").Control);
        var entry = Assert.IsType<Entry>(control.Content);

        Assert.Equal("ada", entry.Text);
        Assert.Equal(engine.ShellView!.Context.Engine.IdPrefix + "Nick", entry.AutomationId);
        Assert.Equal("Nickname", View(engine, "Nick").Field.Label);
    }

    [Fact]
    public void A_template_writes_through_its_context_without_being_built_again_while_the_user_types()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person() };
        FormControlContext<string>? context = null;
        engine.Templates.Add(new KlyFieldTemplate<Person, string>(form.Nick, ctx =>
        {
            context = ctx;
            return new Entry();
        }));
        var control = (FormCustomControl)View(engine, "Nick").Control;
        var builds = control.Builds;

        context!.Set("a");
        context.Set("ab");
        context.Commit("abc");

        Assert.Equal("abc", engine.EffectiveController!.Get(form.Nick));
        Assert.Equal(builds, control.Builds);
    }

    [Fact]
    public void A_template_is_built_again_when_the_form_changes_the_field_from_outside()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person() };
        string? seen = null;
        engine.Templates.Add(new KlyFieldTemplate<Person, string>(form.Nick, ctx =>
        {
            seen = ctx.Value;
            return new Entry();
        }));

        engine.EffectiveController!.Set(form.Nick, "from code");

        Assert.Equal("from code", seen);
    }

    [Fact]
    public void A_template_is_built_again_when_the_field_becomes_disabled()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person() };
        var disabled = new List<bool>();
        engine.Templates.Add(new KlyFieldTemplate<Person, string>(form.Nick, ctx =>
        {
            disabled.Add(ctx.Disabled);
            return new Entry();
        }));
        var gate = new TaskCompletionSource<bool>();
        engine.OnSave = (_, _) => gate.Task;
        ((FormTextControl)View(engine, "Name").Control).Input.Text = "Ada";

        var submit = engine.SubmitAsync();
        gate.SetResult(true);

        Assert.Equal([false, true, false], disabled);
        Assert.True(submit.IsCompleted);
    }

    [Fact]
    public void A_template_for_a_built_in_kind_swaps_its_control_and_removing_the_template_puts_it_back()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person() };
        Assert.IsType<FormTextControl>(View(engine, "Name").Control);
        var template = new KlyFieldTemplate<Person, string>(form.Name, _ => new Label { Text = "mine" });

        engine.Templates.Add(template);
        Assert.IsType<FormCustomControl>(View(engine, "Name").Control);

        engine.Templates.Remove(template);
        Assert.IsType<FormTextControl>(View(engine, "Name").Control);
    }

    [Fact]
    public void Clearing_the_list_of_templates_lets_go_of_all_of_them()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person() };
        engine.Templates.Add(new KlyFieldTemplate<Person, string>(form.Name, _ => new Label()));
        Assert.IsType<FormCustomControl>(View(engine, "Name").Control);

        engine.Templates.Clear();

        Assert.IsType<FormTextControl>(View(engine, "Name").Control);
    }

    [Fact]
    public void A_template_set_before_the_schema_takes_effect_once_the_schema_arrives()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person>();
        engine.Templates.Add(new KlyFieldTemplate<Person, string>(form.Nick, _ => new Label { Text = "mine" }));

        engine.Schema = form.Schema;

        Assert.Equal("mine", Assert.IsType<Label>(((FormCustomControl)View(engine, "Nick").Control).Content).Text);
    }

    [Fact]
    public void A_template_for_another_value_type_is_not_used()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person() };

        engine.Templates.Add(new KlyFieldTemplate<Person, int>(form.Age, _ => new Label { Text = "age" }));
        engine.Templates.Add(new KlyFieldTemplate<Person, int>(form.Age, _ => new Label { Text = "newer age" }));

        Assert.Equal("newer age", Assert.IsType<Label>(((FormCustomControl)View(engine, "Age").Control).Content).Text);
    }

    [Fact]
    public void A_custom_field_without_a_renderer_shows_a_notice_naming_the_key()
    {
        var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema, Model = new Person() };

        var notice = Assert.IsType<Label>(((FormCustomControl)View(engine, "Nick").Control).Content);

        Assert.Contains("\"nick\"", notice.Text);
        Assert.Contains("String", notice.Text);
    }

    [Fact]
    public void A_renderer_registered_for_every_form_draws_the_custom_fields_of_its_key_and_type()
    {
        FormCustomFields.Clear();
        try
        {
            FormCustomFields.Register<string>("nick", ctx => new Label { Text = "nick:" + ctx.Value });
            var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema, Model = new Person { Nick = "ada" } };

            Assert.Equal("nick:ada", Assert.IsType<Label>(((FormCustomControl)View(engine, "Nick").Control).Content).Text);
        }
        finally
        {
            FormCustomFields.Clear();
        }
    }

    [Fact]
    public void A_renderer_of_another_value_type_leaves_the_notice()
    {
        FormCustomFields.Clear();
        try
        {
            FormCustomFields.Register<int>("nick", _ => new Label { Text = "wrong type" });
            var engine = new KlyFormEngine<Person> { Schema = new PersonForm().Schema, Model = new Person() };

            Assert.Contains("No renderer", Assert.IsType<Label>(((FormCustomControl)View(engine, "Nick").Control).Content).Text);
        }
        finally
        {
            FormCustomFields.Clear();
        }
    }

    [Fact]
    public void A_template_added_to_a_wrong_model_is_refused()
    {
        var template = new KlyFieldTemplate<Person, string>(new PersonForm().Name, _ => new Label());
        var other = new KlyFormEngine<Signup> { Schema = FormSchema.For<Signup>(s => s.Field(m => m.Title)) };

        Assert.Throws<InvalidOperationException>(() => other.Templates.Add(template));
    }

    public sealed class Signup
    {
        public string Title { get; set; } = "";
    }

    [Fact]
    public void A_template_checks_its_arguments()
    {
        var form = new PersonForm();

        Assert.Throws<ArgumentNullException>(() => new KlyFieldTemplate<Person, string>(null!, _ => new Label()));
        Assert.Throws<ArgumentNullException>(() => new KlyFieldTemplate<Person, string>(form.Name, null!));
        Assert.Equal("Name", new KlyFieldTemplate<Person, string>(form.Name, _ => new Label()).FieldKey);
    }

    // ---- KlyFormFieldView

    [Fact]
    public void A_field_view_shows_one_field_of_a_given_controller_and_follows_it()
    {
        var form = new PersonForm();
        var controller = new FormController<Person>(form.Schema, new Person { Name = "Ada" });
        var view = new KlyFormFieldView<Person> { Field = form.Name, Form = controller };

        var field = Assert.IsType<FormFieldView>(view.NodeView);
        Assert.Equal("Name", field.Field.Label);
        Assert.Equal("Ada", ((FormTextControl)field.Control).Input.Text);

        controller.Set(form.Name, "Grace");
        Assert.Equal("Grace", ((FormTextControl)field.Control).Input.Text);
    }

    [Fact]
    public void A_field_view_without_a_controller_or_a_field_shows_nothing()
    {
        var form = new PersonForm();

        Assert.Null(new KlyFormFieldView<Person> { Field = form.Name }.Content);
        Assert.Null(new KlyFormFieldView<Person> { Form = new FormController<Person>(form.Schema) }.Content);
    }

    [Fact]
    public void A_field_view_hides_while_its_field_is_not_in_effect()
    {
        var name = default(FormField<Person, string>);
        var email = default(FormField<Person, string?>);
        var schema = FormSchema.For<Person>(s =>
        {
            name = s.Field(m => m.Name);
            email = s.Field(m => m.Email, f => f.VisibleWhen(c => c.Get(name!) == "show"));
        });
        var controller = new FormController<Person>(schema);
        var view = new KlyFormFieldView<Person> { Field = email, Form = controller };
        Assert.False(view.IsVisible);

        controller.Set(name!, "show");

        Assert.True(view.IsVisible);
    }

    [Fact]
    public void A_field_view_finds_the_controller_of_the_form_it_is_in()
    {
        var form = new PersonForm();
        var engine = new KlyFormEngine<Person> { Schema = form.Schema, Model = new Person { Nick = "ada" } };
        KlyFormFieldView<Person>? inner = null;
        engine.Templates.Add(new KlyFieldTemplate<Person, string>(form.Nick, _ =>
        {
            inner = new KlyFormFieldView<Person> { Field = form.Name };
            return new VerticalStackLayout { inner };
        }));

        // The view enters the page after the form view built it: the platform tells it with Loaded.
        inner!.Rebind();

        Assert.Same(engine.EffectiveController, inner.EffectiveController);
        Assert.NotNull(inner.NodeView);
    }

    // ---- page

    [Fact]
    public void A_typed_page_shows_the_sections_of_its_schema()
    {
        var schema = FormSchema.For<Person>(s =>
        {
            s.Title = "Profile";
            s.Section("identity", "Identity", g => g.Field(m => m.Name, f => f.Required()));
            s.Section("more", "More", g => g.Field(m => m.Email));
        });
        var page = new KlyFormPage<Person> { Schema = schema, Model = new Person() };

        Assert.Equal("Profile", page.ShellView!.TitleLabel.Text);
        Assert.Equal(["Identity", "More"], page.ShellView.NavRows.Select(r => r.Label.Text).ToArray());
        Assert.NotNull(page.ShellView.CardOf("identity"));
    }

    [Fact]
    public async Task A_typed_page_submits_into_its_model()
    {
        var schema = FormSchema.For<Person>(s => s.Section("identity", "Identity", g => g.Field(m => m.Name)));
        var person = new Person();
        var page = new KlyFormPage<Person> { Schema = schema, Model = person };
        Person? submitted = null;
        page.Submitted += (_, e) => submitted = e.Model;
        ((FormTextControl)page.ShellView!.Views<FormFieldView>().First().Control).Input.Text = "Ada";

        Assert.True(await page.SubmitAsync());

        Assert.Same(person, submitted);
        Assert.Equal("Ada", person.Name);
    }
}
