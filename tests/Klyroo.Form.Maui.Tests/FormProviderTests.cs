using Klyroo.Form.Dynamic;
using Klyroo.UI.Maui;
using Microsoft.Maui.Controls;

namespace Klyroo.Form.Maui.Tests;

public class FormProviderTests
{
    private static KlyFormProvider Provider(FormService? typed = null, DynamicFormService? dynamic = null)
    {
        var provider = new KlyFormProvider { Service = typed ?? new FormService(), DynamicService = dynamic ?? new DynamicFormService(), Content = new Label { Text = "page" } };
        provider.Attach();
        return provider;
    }

    private static FormSchema<Person> PersonSchema() => new PersonForm().Schema;

    private static IEnumerable<T> InLayer<T>(KlyOverlayLayer layer)
        where T : class =>
        FormUi.Descendants(layer.Content).OfType<T>();

    // ---- the typed service

    [Fact]
    public async Task A_form_opened_with_the_service_shows_in_a_modal_and_closing_it_completes_the_task_without_submitting()
    {
        var service = new FormService();
        var provider = Provider(service);
        var person = new Person { Name = "Ada" };

        var outcome = service.CreateFormAsync(PersonSchema(), person);

        Assert.True(service.IsOpen);
        Assert.True(provider.TypedModal.IsOpen);
        Assert.False(provider.TypedSlideover.IsOpen);
        var body = Assert.IsType<KlyFormEngine<Person>>(provider.TypedModal.Body);
        Assert.Equal("Ada", ((FormTextControl)body.ShellView!.Views<FormFieldView>().First().Control).Input.Text);
        Assert.False(outcome.IsCompleted);

        service.CloseForm();

        Assert.False(service.IsOpen);
        Assert.False(provider.TypedModal.IsOpen);
        Assert.True(outcome.IsCompleted);
        var result = await outcome;
        Assert.False(result.IsCompleted);
        Assert.Same(person, result.Model);
    }

    [Fact]
    public async Task Submitting_the_form_in_the_overlay_completes_the_task_with_the_model_and_closes_it()
    {
        var service = new FormService();
        var provider = Provider(service);
        var person = new Person { Name = "Ada" };
        var outcome = service.CreateFormAsync(PersonSchema(), person);
        var body = (KlyFormEngine<Person>)provider.TypedModal.Body!;
        ((FormTextControl)body.ShellView!.Views<FormFieldView>().First().Control).Input.Text = "Grace";

        Assert.True(await body.SubmitAsync());

        var result = await outcome;
        Assert.True(result.IsCompleted);
        Assert.Same(person, result.Model);
        Assert.Equal("Grace", person.Name);
        Assert.False(provider.TypedModal.IsOpen);
        Assert.False(service.IsOpen);
    }

    [Fact]
    public async Task SubmitActiveFormAsync_submits_the_open_form_and_reports_when_there_is_none()
    {
        var service = new FormService();
        Provider(service);
        Assert.False(await service.SubmitActiveFormAsync());

        var outcome = service.CreateFormAsync(PersonSchema(), new Person { Name = "Ada" });

        Assert.True(await service.SubmitActiveFormAsync());
        Assert.True((await outcome).IsCompleted);
    }

    [Fact]
    public async Task SubmitActiveFormAsync_leaves_the_form_open_when_it_does_not_validate()
    {
        var service = new FormService();
        var provider = Provider(service);
        var outcome = service.CreateFormAsync(PersonSchema(), new Person());

        Assert.False(await service.SubmitActiveFormAsync());

        Assert.True(service.IsOpen);
        Assert.True(provider.TypedModal.IsOpen);
        Assert.False(outcome.IsCompleted);
    }

    [Fact]
    public async Task The_cancel_button_of_the_form_closes_the_overlay()
    {
        var service = new FormService();
        var provider = Provider(service);
        var outcome = service.CreateFormAsync(PersonSchema(), new Person());
        var body = (KlyFormEngine<Person>)provider.TypedModal.Body!;
        Assert.True(body.ShellView!.CancelButton.IsVisible);
        Assert.True(body.ShellView.CloseButton.IsVisible);

        body.ShellView.CancelButton.SendClicked();

        Assert.True(outcome.IsCompleted);
        Assert.False((await outcome).IsCompleted);
        Assert.False(provider.TypedModal.IsOpen);
    }

    [Fact]
    public async Task A_tap_outside_or_the_back_button_dismisses_the_form_as_if_it_had_been_closed()
    {
        var service = new FormService();
        var provider = Provider(service);
        var outcome = service.CreateFormAsync(PersonSchema(), new Person());
        var layer = Assert.Single(provider.Layers);

        provider.Dismiss(layer);

        Assert.True(outcome.IsCompleted);
        Assert.False((await outcome).IsCompleted);
        Assert.False(service.IsOpen);
        Assert.Empty(provider.Layers);
    }

    [Fact]
    public async Task The_overlay_cannot_be_dismissed_while_the_form_saves()
    {
        var gate = new TaskCompletionSource<bool>();
        var service = new FormService();
        var provider = Provider(service);
        _ = service.CreateFormAsync(PersonSchema(), new Person { Name = "Ada" });
        var body = (KlyFormEngine<Person>)provider.TypedModal.Body!;
        body.OnSave = (_, _) => gate.Task;
        Assert.True(provider.TypedModal.Dismissible);

        var submit = body.SubmitAsync();

        Assert.False(provider.TypedModal.Dismissible);
        Assert.False(Assert.Single(provider.Layers).Dismissible);
        Assert.False(body.ShellView!.CancelButton.IsEnabled);

        gate.SetResult(true);
        await submit;
    }

    [Fact]
    public async Task Opening_a_second_form_replaces_the_first_which_ends_as_if_it_had_been_closed()
    {
        var service = new FormService();
        var provider = Provider(service);
        var first = service.CreateFormAsync(PersonSchema(), new Person { Name = "first" });
        var firstBody = provider.TypedModal.Body;

        var second = service.CreateFormAsync(PersonSchema(), new Person { Name = "second" });

        Assert.True(first.IsCompleted);
        Assert.False((await first).IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.NotSame(firstBody, provider.TypedModal.Body);
        Assert.True(provider.TypedModal.IsOpen);
        Assert.Single(provider.Layers);
        service.CloseForm();
        Assert.False((await second).IsCompleted);
    }

    [Fact]
    public void A_drawer_form_opens_in_a_slideover_and_a_modal_form_after_it_in_the_modal()
    {
        var service = new FormService();
        var provider = Provider(service);

        _ = service.CreateFormAsync(PersonSchema(), new Person(), FormShellMode.Drawer);

        Assert.True(provider.TypedSlideover.IsOpen);
        Assert.False(provider.TypedModal.IsOpen);
        Assert.IsType<KlyFormEngine<Person>>(provider.TypedSlideover.Body);
        Assert.Equal(FormShellMode.Drawer, ((KlyFormEngine<Person>)provider.TypedSlideover.Body!).Shell);

        _ = service.CreateFormAsync(PersonSchema(), new Person(), FormShellMode.Modal);

        Assert.False(provider.TypedSlideover.IsOpen);
        Assert.True(provider.TypedModal.IsOpen);
        Assert.Single(provider.Layers);
    }

    [Fact]
    public void A_fullscreen_form_covers_the_whole_host_and_closes_with_the_service()
    {
        var service = new FormService();
        var provider = Provider(service);
        var outcome = service.CreateFormAsync(PersonSchema(), new Person(), FormShellMode.Fullscreen);

        var layer = Assert.IsType<KlyOverlayLayer>(provider.TypedFullscreen);
        Assert.True(layer.IsShown);
        Assert.False(provider.TypedModal.IsOpen);
        Assert.IsType<FormFullscreen>(layer.Content);
        Assert.Single(InLayer<KlyFormEngine<Person>>(layer));
        Assert.False(layer.Options.Dim);

        service.CloseForm();

        Assert.False(layer.IsShown);
        Assert.True(outcome.IsCompleted);
    }

    [Fact]
    public void Dismissing_a_fullscreen_form_closes_it()
    {
        var service = new FormService();
        var provider = Provider(service);
        var outcome = service.CreateFormAsync(PersonSchema(), new Person(), FormShellMode.Fullscreen);

        provider.Dismiss(provider.TypedFullscreen!);

        Assert.True(outcome.IsCompleted);
        Assert.False(service.IsOpen);
    }

    [Fact]
    public void The_overlay_is_named_for_a_screen_reader_by_the_title_of_the_form_or_the_form_title_text()
    {
        var service = new FormService();
        var provider = Provider(service);

        _ = service.CreateFormAsync(PersonSchema(), new Person());
        Assert.Equal("Person", provider.TypedModal.AccessibleName);
        Assert.False(provider.TypedModal.Close);

        _ = service.CreateFormAsync(FormSchema.For<Person>(s => s.Field(m => m.Name)), new Person());
        Assert.Equal(FormTexts.Current.FormTitle, provider.TypedModal.AccessibleName);
    }

    // ---- the runtime service

    private static DynamicFormSchema Runtime() => new() { Title = "Runtime", Fields = [new DynamicFieldSchema { Key = "name", Label = "Name", Required = true }] };

    [Fact]
    public async Task A_runtime_form_shows_in_its_own_modal_and_submitting_completes_the_task_with_the_output()
    {
        var dynamic = new DynamicFormService();
        var provider = Provider(dynamic: dynamic);
        var outcome = dynamic.CreateFormAsync(Runtime(), new Dictionary<string, object?> { ["name"] = "Ada" });

        Assert.True(provider.DynamicModal.IsOpen);
        Assert.False(provider.TypedModal.IsOpen);
        var body = Assert.IsType<KlyDynamicFormEngine>(provider.DynamicModal.Body);
        Assert.Equal("Runtime", provider.DynamicModal.AccessibleName);

        Assert.True(await body.SubmitAsync());

        var result = await outcome;
        Assert.True(result.IsCompleted);
        Assert.Equal("Ada", result.FormData["name"]);
        Assert.False(provider.DynamicModal.IsOpen);
        Assert.False(dynamic.IsOpen);
    }

    [Fact]
    public async Task A_runtime_form_closes_without_submitting_when_cancelled()
    {
        var dynamic = new DynamicFormService();
        var provider = Provider(dynamic: dynamic);
        var outcome = dynamic.CreateFormAsync(Runtime());
        var body = (KlyDynamicFormEngine)provider.DynamicModal.Body!;
        body.Control<FormTextControl>("name").Input.Text = "draft";

        await body.ShellView!.CancelAsync();

        var result = await outcome;
        Assert.False(result.IsCompleted);
        Assert.Equal("draft", result.FormData["name"]);
        Assert.False(provider.DynamicModal.IsOpen);
    }

    [Fact]
    public void A_typed_and_a_runtime_form_can_be_open_together()
    {
        var service = new FormService();
        var dynamic = new DynamicFormService();
        var provider = Provider(service, dynamic);

        _ = service.CreateFormAsync(PersonSchema(), new Person());
        _ = dynamic.CreateFormAsync(Runtime());

        Assert.True(provider.TypedModal.IsOpen);
        Assert.True(provider.DynamicModal.IsOpen);
        Assert.Equal(2, provider.Layers.Count);
    }

    [Fact]
    public async Task A_replaced_runtime_form_ends_as_if_it_had_been_closed_and_the_overlay_shows_the_new_one()
    {
        var dynamic = new DynamicFormService();
        var provider = Provider(dynamic: dynamic);
        var first = dynamic.CreateFormAsync(Runtime());
        var firstBody = provider.DynamicModal.Body;

        _ = dynamic.CreateFormAsync(Runtime(), mode: FormShellMode.Modal);

        Assert.False((await first).IsCompleted);
        Assert.NotSame(firstBody, provider.DynamicModal.Body);
        Assert.True(provider.DynamicModal.IsOpen);
    }

    // ---- lifetime and services

    [Fact]
    public void A_provider_that_is_not_attached_does_not_listen_and_shows_the_open_form_once_it_is()
    {
        var service = new FormService();
        var provider = new KlyFormProvider { Service = service, DynamicService = new DynamicFormService() };

        _ = service.CreateFormAsync(PersonSchema(), new Person());
        Assert.False(provider.TypedModal.IsOpen);

        provider.Attach();
        Assert.True(provider.TypedModal.IsOpen);
        Assert.True(provider.IsAttached);
    }

    [Fact]
    public void Detaching_closes_the_overlays_but_not_the_form_of_the_service()
    {
        var service = new FormService();
        var provider = Provider(service);
        var outcome = service.CreateFormAsync(PersonSchema(), new Person());

        provider.Detach();

        Assert.False(provider.TypedModal.IsOpen);
        Assert.True(service.IsOpen);
        Assert.False(outcome.IsCompleted);

        provider.Attach();
        Assert.True(provider.TypedModal.IsOpen);
    }

    [Fact]
    public void A_detached_provider_no_longer_follows_the_service()
    {
        var service = new FormService();
        var provider = Provider(service);
        provider.Detach();

        _ = service.CreateFormAsync(PersonSchema(), new Person());

        Assert.False(provider.TypedModal.IsOpen);
    }

    [Fact]
    public void Another_service_makes_the_provider_follow_that_one()
    {
        var one = new FormService();
        var two = new FormService();
        var provider = Provider(one);

        provider.Service = two;
        _ = one.CreateFormAsync(PersonSchema(), new Person());
        Assert.False(provider.TypedModal.IsOpen);
        _ = two.CreateFormAsync(PersonSchema(), new Person());

        Assert.True(provider.TypedModal.IsOpen);
        Assert.Same(two, provider.EffectiveService);
    }

    [Fact]
    public void Without_a_service_the_provider_serves_the_defaults()
    {
        var provider = new KlyFormProvider();

        Assert.Same(FormService.Default, provider.EffectiveService);
        Assert.Same(FormService.DefaultDynamic, provider.EffectiveDynamicService);
    }

    [Fact]
    public void The_provider_is_an_overlay_host_around_its_content()
    {
        var content = new Label { Text = "page" };
        var provider = new KlyFormProvider { Content = content };

        Assert.IsAssignableFrom<KlyOverlayHost>(provider);
        Assert.Same(content, provider.Content);
    }

    [Fact]
    public void Opening_a_form_checks_its_schema()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new FormService().CreateFormAsync<Person>(null!); });
    }

    [Fact]
    public void CloseForm_does_nothing_when_no_form_is_open()
    {
        var service = new FormService();

        service.CloseForm();

        Assert.False(service.IsOpen);
    }
}
