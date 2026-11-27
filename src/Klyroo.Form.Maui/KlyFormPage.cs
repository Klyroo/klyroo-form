using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form.Maui;

/// <summary>
/// Shows a sectioned typed form as a page: a list of the sections with their completion status (a tap scrolls to the section), and one card per section laid out by its span. Fields have the same controls,
/// labels, containers and repeatable lists as <see cref="KlyFormEngine{TModel}"/>, and the return key in a text field submits the form. <typeparamref name="TModel"/> is inferred from <see cref="Form"/>,
/// <see cref="Schema"/> or <see cref="Model"/>. Put it in a <see cref="ScrollView"/>: the list of sections scrolls the page to the one it names.
/// </summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class KlyFormPage<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel> : ContentView, IFormControllerHost<TModel>, IDisposable
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(FormController<TModel>), typeof(KlyFormPage<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormPage<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="Schema"/>.</summary>
    public static readonly BindableProperty SchemaProperty = BindableProperty.Create(nameof(Schema), typeof(FormSchema<TModel>), typeof(KlyFormPage<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormPage<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="Model"/>.</summary>
    public static readonly BindableProperty ModelProperty = BindableProperty.Create(nameof(Model), typeof(TModel), typeof(KlyFormPage<TModel>), default(TModel), propertyChanged: (b, _, _) => ((KlyFormPage<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="CompactWidth"/>.</summary>
    public static readonly BindableProperty CompactWidthProperty = BindableProperty.Create(nameof(CompactWidth), typeof(double), typeof(KlyFormPage<TModel>), 480d, propertyChanged: (b, _, _) => ((KlyFormPage<TModel>)b).Reconfigure());

    private readonly FormModelHost<TModel> _models = new();
    private readonly FormShellHost _host;
    private readonly ObservableCollection<KlyFieldTemplate> _templates = [];
    private readonly List<KlyFieldTemplate> _attached = [];

    /// <summary>Creates a form page.</summary>
    public KlyFormPage()
    {
        _host = new FormShellHost(this, context => new FormPageShellView(context, SubmitAction));
        _templates.CollectionChanged += (_, _) => AttachTemplates();
        Loaded += (_, _) => _host.Resume();
        Unloaded += (_, _) => _host.Suspend();
    }

    /// <summary>Raised with the model after the form is submitted successfully, that is after validation, the schema hooks and <see cref="OnSave"/> passed. The submission is over at that point and the form is clean.</summary>
    public event EventHandler<FormSubmittedEventArgs<TModel>>? Submitted;

    /// <summary>The controller to show. Its schema must declare sections (<c>Section</c>). Takes precedence over <see cref="Schema"/>.</summary>
    public FormController<TModel>? Form { get => (FormController<TModel>?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The schema to show when no <see cref="Form"/> is given. It must declare sections. The view creates and owns the controller, and creates a new one when the schema instance changes.</summary>
    public FormSchema<TModel>? Schema { get => (FormSchema<TModel>?)GetValue(SchemaProperty); set => SetValue(SchemaProperty, value); }

    /// <summary>The model to edit. Its values are the form's starting values, and a submit writes the values back into it (all or nothing, put back when a step fails), so members the form does not show keep their value. Without a model every submit creates a new <typeparamref name="TModel"/>. A different instance reloads the form, dropping unsaved edits.</summary>
    public TModel? Model { get => (TModel?)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    /// <summary>The width under which every field, and every section, takes the whole row. The default is 480. Zero keeps the columns whatever the width.</summary>
    public double CompactWidth { get => (double)GetValue(CompactWidthProperty); set => SetValue(CompactWidthProperty, value); }

    /// <summary>Your save step: called with the model after validation and the schema hooks passed, while the form is still submitting. Return <c>true</c> when the save succeeded; the form is then clean and <see cref="Submitted"/> is raised. Return <c>false</c> to keep the edits dirty, after reporting the reason with <see cref="FormController{TModel}.SetError"/> or <see cref="FormController{TModel}.SetFormError"/>. The token is cancelled when the view is disposed.</summary>
    public Func<TModel, CancellationToken, Task<bool>>? OnSave { get; set; }

    /// <summary>The templates of the fields (<see cref="KlyFieldTemplate{TModel, TValue}"/>). They draw nothing themselves: they hand their control to the field they name.</summary>
    public IList<KlyFieldTemplate> Templates => _templates;

    /// <summary>The controller in use: <see cref="Form"/> if set, otherwise the one the view created from <see cref="Schema"/>, or <c>null</c> when neither is set.</summary>
    public FormController<TModel>? EffectiveController => _models.Controller;

    internal FormPageShellView? ShellView => _host.Shell as FormPageShellView;

    /// <summary>Validates, saves and raises <see cref="Submitted"/>, as the submit button does. When it fails, the focus goes to the first error.</summary>
    /// <returns><c>true</c> when the form was submitted.</returns>
    public Task<bool> SubmitAsync() => _host.Shell is { } shell ? shell.SubmitAsync() : Task.FromResult(false);

    /// <summary>Refreshes the view from the controller, now. It follows every change by itself; this is for a change made behind the controller's back.</summary>
    public void Refresh() => _host.Refresh();

    /// <summary>Stops listening to the controller and disposes the one the view created.</summary>
    public void Dispose()
    {
        _host.Dispose();
        _models.Dispose();
        foreach (var template in _attached) template.Attach(null);
        _attached.Clear();
    }

    private Func<CancellationToken, Task<bool>> SubmitAction => cancellationToken =>
        _models.SubmitAsync(OnSave, model => Submitted?.Invoke(this, new FormSubmittedEventArgs<TModel>(model)), Model, cancellationToken);

    private void Reconfigure()
    {
        if (_host is null) return;
        _models.Update(Form, Schema, Model);
        var controller = _models.Controller;
        controller?.UseLocaleMessages(FormTexts.Current.Messages);
        _host.Bind(controller?.Engine, CompactWidth, null);
        AttachTemplates();
        if (_host.Shell is not null) _host.Refresh();
    }

    private void AttachTemplates()
    {
        foreach (var template in _attached.Except(_templates).ToList())
        {
            template.Attach(null);
            _attached.Remove(template);
        }
        foreach (var template in _templates.Except(_attached).ToList())
        {
            template.Attach(_models.Controller);
            _attached.Add(template);
        }
        foreach (var template in _attached) template.Attach(_models.Controller);
    }
}
