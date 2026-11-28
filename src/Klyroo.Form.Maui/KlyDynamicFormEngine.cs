using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// Shows a <see cref="DynamicFormSchema"/> as a form: header, optional stepper with the description of the current step, a field grid of <see cref="DynamicFormSchema.Columns"/> columns and the footer actions
/// (Back, Reset, Cancel, Next, Submit). Pass either a <see cref="DynamicFormController"/> through <see cref="Form"/> or a schema through <see cref="Schema"/>. The view follows
/// <see cref="DynamicFormController.Changed"/> by itself. Put it in a <see cref="ScrollView"/> when the form is longer than the screen. Call <see cref="Dispose"/> when the page goes away for good.
/// </summary>
public sealed class KlyDynamicFormEngine : ContentView, IDisposable
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(DynamicFormController), typeof(KlyDynamicFormEngine), null, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());
    /// <summary>Identifies <see cref="Schema"/>.</summary>
    public static readonly BindableProperty SchemaProperty = BindableProperty.Create(nameof(Schema), typeof(DynamicFormSchema), typeof(KlyDynamicFormEngine), null, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());
    /// <summary>Identifies <see cref="Input"/>.</summary>
    public static readonly BindableProperty InputProperty = BindableProperty.Create(nameof(Input), typeof(IReadOnlyDictionary<string, object?>), typeof(KlyDynamicFormEngine), null, propertyChanged: (b, _, v) => ((KlyDynamicFormEngine)b).InputChanged((IReadOnlyDictionary<string, object?>?)v));
    /// <summary>Identifies <see cref="Shell"/>.</summary>
    public static readonly BindableProperty ShellProperty = BindableProperty.Create(nameof(Shell), typeof(FormShellMode), typeof(KlyDynamicFormEngine), FormShellMode.Inline, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());
    /// <summary>Identifies <see cref="FieldTemplates"/>.</summary>
    public static readonly BindableProperty FieldTemplatesProperty = BindableProperty.Create(nameof(FieldTemplates), typeof(IReadOnlyDictionary<string, Func<DynamicControlContext, View>>), typeof(KlyDynamicFormEngine), null, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());
    /// <summary>Identifies <see cref="SubmitLabel"/>.</summary>
    public static readonly BindableProperty SubmitLabelProperty = BindableProperty.Create(nameof(SubmitLabel), typeof(string), typeof(KlyDynamicFormEngine), null, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());
    /// <summary>Identifies <see cref="ShowReset"/>.</summary>
    public static readonly BindableProperty ShowResetProperty = BindableProperty.Create(nameof(ShowReset), typeof(bool), typeof(KlyDynamicFormEngine), true, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());
    /// <summary>Identifies <see cref="CompactWidth"/>.</summary>
    public static readonly BindableProperty CompactWidthProperty = BindableProperty.Create(nameof(CompactWidth), typeof(double), typeof(KlyDynamicFormEngine), 480d, propertyChanged: (b, _, _) => ((KlyDynamicFormEngine)b).Reconfigure());

    private readonly FormShellHost _host;
    private DynamicFormController? _internalController;
    private EventHandler<DynamicFormOutputEventArgs>? _cancelled;

    /// <summary>Creates a form view.</summary>
    public KlyDynamicFormEngine()
    {
        _host = new FormShellHost(this, context => new FormShellView(context, SaveAsync));
        Loaded += (_, _) => _host.Resume();
        Unloaded += (_, _) => _host.Suspend();
    }

    /// <summary>Raised with <see cref="DynamicFormController.BuildOutput"/> after the form is submitted successfully, that is after validation, the schema hooks and <see cref="OnSave"/> passed. At that point the submission is over: <see cref="DynamicFormController.IsSubmitting"/> is <c>false</c> and the form is clean. Do the save itself in <see cref="OnSave"/>.</summary>
    public event EventHandler<DynamicFormOutputEventArgs>? Submitted;

    /// <summary>Raised with the current output when the user presses Cancel or the close button. The Cancel button is shown while someone listens.</summary>
    public event EventHandler<DynamicFormOutputEventArgs>? Cancelled
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
    public DynamicFormController? Form { get => (DynamicFormController?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The schema to show when no <see cref="Form"/> is given. The view creates and owns the controller, and creates a new one when the schema instance changes.</summary>
    public DynamicFormSchema? Schema { get => (DynamicFormSchema?)GetValue(SchemaProperty); set => SetValue(SchemaProperty, value); }

    /// <summary>Initial values by field key for the controller created from <see cref="Schema"/>. A different dictionary reloads that controller, dropping unsaved edits. Ignored when <see cref="Form"/> is set.</summary>
    public IReadOnlyDictionary<string, object?>? Input { get => (IReadOnlyDictionary<string, object?>?)GetValue(InputProperty); set => SetValue(InputProperty, value); }

    /// <summary>How the form is presented. Anything other than <see cref="FormShellMode.Inline"/> adds a close button to the header while <see cref="Cancelled"/> has a listener.</summary>
    public FormShellMode Shell { get => (FormShellMode)GetValue(ShellProperty); set => SetValue(ShellProperty, value); }

    /// <summary>Builders of the <see cref="FormFieldKind.Custom"/> fields by <see cref="DynamicFieldSchema.CustomKey"/>, each a function that makes the control from a <see cref="DynamicControlContext"/>. They win over the ones registered with <see cref="FormCustomFields"/>, and reach the fields inside containers, tabs and lists.</summary>
    public IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? FieldTemplates { get => (IReadOnlyDictionary<string, Func<DynamicControlContext, View>>?)GetValue(FieldTemplatesProperty); set => SetValue(FieldTemplatesProperty, value); }

    /// <summary>Text of the submit button. The default is the <see cref="FormTexts.Submit"/> text.</summary>
    public string? SubmitLabel { get => (string?)GetValue(SubmitLabelProperty); set => SetValue(SubmitLabelProperty, value); }

    /// <summary>Shows a reset button in the footer while the form is dirty. The default is <c>true</c>.</summary>
    public bool ShowReset { get => (bool)GetValue(ShowResetProperty); set => SetValue(ShowResetProperty, value); }

    /// <summary>The width under which every field takes the whole row, instead of the columns of the schema. The default is 480. Zero keeps the columns whatever the width.</summary>
    public double CompactWidth { get => (double)GetValue(CompactWidthProperty); set => SetValue(CompactWidthProperty, value); }

    /// <summary>Your save step: called with <see cref="DynamicFormController.BuildOutput"/> after validation and the schema hooks passed, while the form is still submitting (the button shows its saving state, a second submit is refused, Cancel is disabled). Return <c>true</c> when the save succeeded; the form is then clean and <see cref="Submitted"/> is raised. Return <c>false</c> to keep the user's edits dirty and the form open, after reporting the reason with <see cref="DynamicFormController.SetError"/> or <see cref="DynamicFormController.SetFormError"/>. The token is cancelled when the view is disposed. It runs through <see cref="DynamicFormController.SubmitAsync(Func{DynamicFormController, CancellationToken, Task{bool}}, CancellationToken)"/>.</summary>
    public Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>>? OnSave { get; set; }

    /// <summary>The controller in use: <see cref="Form"/> if set, otherwise the one the view created from <see cref="Schema"/>.</summary>
    public DynamicFormController? EffectiveController => Form ?? _internalController;

    internal FormShellView? ShellView => _host.Shell as FormShellView;

    /// <summary>Validates, saves and raises <see cref="Submitted"/>, as the submit button does; before the last step of a stepped form it goes to the next step instead. When it fails, the focus goes to the first error.</summary>
    /// <returns><c>true</c> when the form was submitted (or the step changed).</returns>
    public Task<bool> SubmitAsync() => _host.Shell is { } shell ? shell.SubmitAsync() : Task.FromResult(false);

    /// <summary>Refreshes the view from the controller, now. It follows every change by itself; this is for a change made behind the controller's back.</summary>
    public void Refresh() => _host.Refresh();

    /// <summary>Stops listening to the controller. The controller the view created goes with it; one you passed stays yours, but nobody is left to show its pending checks.</summary>
    public void Dispose()
    {
        _host.Dispose();
        if (_internalController is not null) _internalController.Dispose();
        else Form?.CancelPendingValidations();
        _internalController = null;
    }

    // The properties are set one by one: values that arrive after the schema reload the controller the view made from it.
    private void InputChanged(IReadOnlyDictionary<string, object?>? input)
    {
        if (_host is null) return;
        if (Form is null && _internalController is not null) _internalController.Reset(input);
        Reconfigure();
    }

    private void Reconfigure()
    {
        if (_host is null) return;
        if (Form is null && Schema is not null && (_internalController is null || !ReferenceEquals(_internalController.Schema, Schema)))
        {
            _internalController?.Dispose();
            _internalController = new DynamicFormController(Schema, Input);
        }
        else if (Form is not null || Schema is null)
        {
            _internalController?.Dispose();
            _internalController = null;
        }
        var controller = EffectiveController;
        controller?.UseLocaleMessages(FormTexts.Current.Messages);
        _host.Bind(controller?.Engine, CompactWidth, FieldTemplates);
        if (_host.Shell is FormShellView shell)
        {
            shell.Configure(Shell, SubmitLabel, ShowReset, _cancelled is null ? null : CancelAsync);
            _host.Refresh();
        }
    }

    private async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        var form = EffectiveController;
        if (form is null) return false;
        var done = OnSave is { } save
            ? await form.SubmitAsync((f, ct) => save(f.BuildOutput(), ct), cancellationToken)
            : await form.SubmitAsync();
        if (done) Submitted?.Invoke(this, new DynamicFormOutputEventArgs(form.BuildOutput()));
        return done;
    }

    private Task CancelAsync()
    {
        if (EffectiveController is { } form) _cancelled?.Invoke(this, new DynamicFormOutputEventArgs(form.BuildOutput()));
        return Task.CompletedTask;
    }
}
