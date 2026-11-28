using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form.Maui;

/// <summary>
/// Shows a <see cref="FormSchema{TModel}"/> as a form: header, optional stepper with the description of the current step, a field grid of columns and the footer actions
/// (Back, Reset, Cancel, Next, Submit). Pass either a <see cref="FormController{TModel}"/> through <see cref="Form"/> or a schema through <see cref="Schema"/>, with the <see cref="Model"/> to edit.
/// The view follows <see cref="FormController{TModel}.Changed"/> by itself, and every field is the klyroo-ui input of its kind (<see cref="KlyInput"/>, <see cref="KlySelect"/>, <see cref="KlyInputDate"/>...).
/// Put it in a <see cref="ScrollView"/> when the form is longer than the screen.
/// </summary>
/// <remarks>
/// <para>It is a view over the controller: values, touched state, errors, dirty state, steps and submit all live in <see cref="FormController{TModel}"/>, so a form that uses it behaves like the Blazor one.
/// Below <see cref="CompactWidth"/> every field takes the whole row, since the 12-column grid of the web form does not fit a phone.</para>
/// <para>The view listens to the controller while it is in a page. Call <see cref="Dispose"/> when the page goes away for good: it disposes the controller the view made from <see cref="Schema"/>.</para>
/// </remarks>
/// <example>
/// <code>
/// var engine = new KlyFormEngine&lt;Signup&gt;
/// {
///     Schema = schema,
///     Model = new Signup(),
///     OnSave = async (model, token) => await api.SaveAsync(model, token),
/// };
/// engine.Submitted += (_, e) => Navigate(e.Model);
/// </code>
/// </example>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class KlyFormEngine<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel> : ContentView, IFormControllerHost<TModel>, IDisposable
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(FormController<TModel>), typeof(KlyFormEngine<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="Schema"/>.</summary>
    public static readonly BindableProperty SchemaProperty = BindableProperty.Create(nameof(Schema), typeof(FormSchema<TModel>), typeof(KlyFormEngine<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="Model"/>.</summary>
    public static readonly BindableProperty ModelProperty = BindableProperty.Create(nameof(Model), typeof(TModel), typeof(KlyFormEngine<TModel>), default(TModel), propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="Shell"/>.</summary>
    public static readonly BindableProperty ShellProperty = BindableProperty.Create(nameof(Shell), typeof(FormShellMode), typeof(KlyFormEngine<TModel>), FormShellMode.Inline, propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="SubmitLabel"/>.</summary>
    public static readonly BindableProperty SubmitLabelProperty = BindableProperty.Create(nameof(SubmitLabel), typeof(string), typeof(KlyFormEngine<TModel>), null, propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="ShowReset"/>.</summary>
    public static readonly BindableProperty ShowResetProperty = BindableProperty.Create(nameof(ShowReset), typeof(bool), typeof(KlyFormEngine<TModel>), true, propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());
    /// <summary>Identifies <see cref="CompactWidth"/>.</summary>
    public static readonly BindableProperty CompactWidthProperty = BindableProperty.Create(nameof(CompactWidth), typeof(double), typeof(KlyFormEngine<TModel>), 480d, propertyChanged: (b, _, _) => ((KlyFormEngine<TModel>)b).Reconfigure());

    private readonly FormModelHost<TModel> _models = new();
    private readonly FormShellHost _host;
    private readonly ObservableCollection<KlyFieldTemplate> _templates = [];
    private readonly List<KlyFieldTemplate> _attached = [];
    private EventHandler? _cancelled;

    /// <summary>Creates a form view.</summary>
    public KlyFormEngine()
    {
        _host = new FormShellHost(this, context => new FormShellView(context, SubmitAction));
        _templates.CollectionChanged += OnTemplatesChanged;
        Loaded += (_, _) => _host.Resume();
        Unloaded += (_, _) => _host.Suspend();
    }

    /// <summary>Raised with the model after the form is submitted successfully, that is after validation, the schema hooks and <see cref="OnSave"/> passed. The submission is over at that point: <see cref="FormController{TModel}.IsSubmitting"/> is <c>false</c> and the form is clean. Do the save itself in <see cref="OnSave"/>.</summary>
    public event EventHandler<FormSubmittedEventArgs<TModel>>? Submitted;

    /// <summary>Raised when the user presses Cancel or the close button. The Cancel button is shown while someone listens.</summary>
    public event EventHandler? Cancelled
    {
        add
        {
            _cancelled += value;
            Reconfigure();
        }
        remove
        {
            _cancelled -= value;
            Reconfigure();
        }
    }

    /// <summary>The controller to show. Use it when you need to read or change the form from code. Takes precedence over <see cref="Schema"/>.</summary>
    public FormController<TModel>? Form { get => (FormController<TModel>?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The schema to show when no <see cref="Form"/> is given. The view creates and owns the controller, and creates a new one when the schema instance changes.</summary>
    public FormSchema<TModel>? Schema { get => (FormSchema<TModel>?)GetValue(SchemaProperty); set => SetValue(SchemaProperty, value); }

    /// <summary>The model to edit. Its values are the form's starting values, and a submit writes the values back into it (all or nothing, put back when a step fails), so members the form does not show keep their value. Without a model every submit creates a new <typeparamref name="TModel"/>. A different instance reloads the form, dropping unsaved edits. It is only read by the controller the view creates: with <see cref="Form"/>, it is only the target of a submit.</summary>
    public TModel? Model { get => (TModel?)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    /// <summary>How the form is presented. Anything other than <see cref="FormShellMode.Inline"/> adds a close button to the header while <see cref="Cancelled"/> has a listener.</summary>
    public FormShellMode Shell { get => (FormShellMode)GetValue(ShellProperty); set => SetValue(ShellProperty, value); }

    /// <summary>Text of the submit button. The default is the <see cref="FormTexts.Submit"/> text.</summary>
    public string? SubmitLabel { get => (string?)GetValue(SubmitLabelProperty); set => SetValue(SubmitLabelProperty, value); }

    /// <summary>Shows a reset button in the footer while the form is dirty. The default is <c>true</c>.</summary>
    public bool ShowReset { get => (bool)GetValue(ShowResetProperty); set => SetValue(ShowResetProperty, value); }

    /// <summary>The width under which every field takes the whole row, instead of the columns of the schema. The default is 480. Zero keeps the columns whatever the width.</summary>
    public double CompactWidth { get => (double)GetValue(CompactWidthProperty); set => SetValue(CompactWidthProperty, value); }

    /// <summary>Your save step: called with the model after validation and the schema hooks passed (the <see cref="Model"/> with the values written into it, or a new one), while the form is still submitting (the button shows its saving state, a second submit is refused, Cancel is disabled). Return <c>true</c> when the save succeeded; the form is then clean and <see cref="Submitted"/> is raised. Return <c>false</c> to keep the edits dirty and the form open, after reporting the reason with <see cref="FormController{TModel}.SetError"/> or <see cref="FormController{TModel}.SetFormError"/>; the model gets its previous values back. The token is cancelled when the view is disposed. It runs through <see cref="FormController{TModel}.SubmitAsync(Func{TModel, CancellationToken, Task{bool}}, TModel, CancellationToken)"/>.</summary>
    public Func<TModel, CancellationToken, Task<bool>>? OnSave { get; set; }

    /// <summary>The templates of the fields (<see cref="KlyFieldTemplate{TModel, TValue}"/>). They draw nothing themselves: they hand their control to the field they name.</summary>
    public IList<KlyFieldTemplate> Templates => _templates;

    /// <summary>The controller in use: <see cref="Form"/> if set, otherwise the one the view created from <see cref="Schema"/>, or <c>null</c> when neither is set.</summary>
    public FormController<TModel>? EffectiveController => _models.Controller;

    internal FormShellView? ShellView => _host.Shell as FormShellView;

    /// <summary>Validates, saves and raises <see cref="Submitted"/>, as the submit button does; before the last step of a stepped form it goes to the next step instead. When it fails, the focus goes to the first error.</summary>
    /// <returns><c>true</c> when the form was submitted (or the step changed).</returns>
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

    private Func<Task>? CancelAction => _cancelled is null ? null : () =>
    {
        _cancelled?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    };

    private void Reconfigure()
    {
        // The properties are set one by one while the view is built: nothing to show before the view has what it needs.
        if (_host is null) return;
        _models.Update(Form, Schema, Model);
        var controller = _models.Controller;
        controller?.UseLocaleMessages(FormTexts.Current.Messages);
        _host.Bind(controller?.Engine, CompactWidth, null);
        AttachTemplates();
        if (_host.Shell is FormShellView shell)
        {
            shell.Configure(Shell, SubmitLabel, ShowReset, CancelAction);
            _host.Refresh();
        }
    }

    private void AttachTemplates()
    {
        // Templates that left the list let go of the controller, the ones that came in take it: the list can be replaced as a whole (Clear), where no event names the old items.
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

    private void OnTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e) => AttachTemplates();
}
