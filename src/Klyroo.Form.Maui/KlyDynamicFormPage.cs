using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// Shows a sectioned runtime form as a page: a list of the sections with their completion status (a tap scrolls to the section), and one card per section laid out by <see cref="DynamicSectionSchema.Span"/>.
/// Fields have the same controls, labels, containers and repeatable lists as <see cref="KlyDynamicFormEngine"/>, and the return key in a text field submits the form.
/// Put it in a <see cref="ScrollView"/>: the list of sections scrolls the page to the one it names. Call <see cref="Dispose"/> when the page goes away for good.
/// </summary>
public sealed class KlyDynamicFormPage : ContentView, IDisposable
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(DynamicFormController), typeof(KlyDynamicFormPage), null, propertyChanged: (b, _, _) => ((KlyDynamicFormPage)b).Reconfigure());
    /// <summary>Identifies <see cref="FieldTemplates"/>.</summary>
    public static readonly BindableProperty FieldTemplatesProperty = BindableProperty.Create(nameof(FieldTemplates), typeof(IReadOnlyDictionary<string, Func<DynamicControlContext, View>>), typeof(KlyDynamicFormPage), null, propertyChanged: (b, _, _) => ((KlyDynamicFormPage)b).Reconfigure());
    /// <summary>Identifies <see cref="CompactWidth"/>.</summary>
    public static readonly BindableProperty CompactWidthProperty = BindableProperty.Create(nameof(CompactWidth), typeof(double), typeof(KlyDynamicFormPage), 480d, propertyChanged: (b, _, _) => ((KlyDynamicFormPage)b).Reconfigure());

    private readonly FormShellHost _host;

    /// <summary>Creates a form page.</summary>
    public KlyDynamicFormPage()
    {
        _host = new FormShellHost(this, context => new FormPageShellView(context, SaveAsync));
        Loaded += (_, _) => _host.Resume();
        Unloaded += (_, _) => _host.Suspend();
    }

    /// <summary>Raised with <see cref="DynamicFormController.BuildOutput"/> after the form is submitted successfully, that is after validation, the schema hooks and <see cref="OnSave"/> passed. The submission is over at that point and the form is clean.</summary>
    public event EventHandler<DynamicFormOutputEventArgs>? Submitted;

    /// <summary>The controller to show. Its schema must declare <see cref="DynamicFormSchema.Sections"/>.</summary>
    public DynamicFormController? Form { get => (DynamicFormController?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>Builders of the <see cref="FormFieldKind.Custom"/> fields by <see cref="DynamicFieldSchema.CustomKey"/>, each a function that makes the control from a <see cref="DynamicControlContext"/>. They win over the ones registered with <see cref="FormCustomFields"/>.</summary>
    public IReadOnlyDictionary<string, Func<DynamicControlContext, View>>? FieldTemplates { get => (IReadOnlyDictionary<string, Func<DynamicControlContext, View>>?)GetValue(FieldTemplatesProperty); set => SetValue(FieldTemplatesProperty, value); }

    /// <summary>The width under which every field, and every section, takes the whole row. The default is 480. Zero keeps the columns whatever the width.</summary>
    public double CompactWidth { get => (double)GetValue(CompactWidthProperty); set => SetValue(CompactWidthProperty, value); }

    /// <summary>Your save step: called with <see cref="DynamicFormController.BuildOutput"/> after validation and the schema hooks passed, while the form is still submitting. Return <c>true</c> when the save succeeded; the form is then clean and <see cref="Submitted"/> is raised. Return <c>false</c> to keep the edits dirty, after reporting the reason with <see cref="DynamicFormController.SetError"/> or <see cref="DynamicFormController.SetFormError"/>. The token is cancelled when the view is disposed.</summary>
    public Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<bool>>? OnSave { get; set; }

    internal FormPageShellView? ShellView => _host.Shell as FormPageShellView;

    /// <summary>Validates, saves and raises <see cref="Submitted"/>, as the submit button does. When it fails, the focus goes to the first error.</summary>
    /// <returns><c>true</c> when the form was submitted.</returns>
    public Task<bool> SubmitAsync() => _host.Shell is { } shell ? shell.SubmitAsync() : Task.FromResult(false);

    /// <summary>Refreshes the view from the controller, now. It follows every change by itself; this is for a change made behind the controller's back.</summary>
    public void Refresh() => _host.Refresh();

    /// <summary>Stops listening to the controller and cancels its pending checks.</summary>
    public void Dispose()
    {
        _host.Dispose();
        Form?.CancelPendingValidations();
    }

    private void Reconfigure()
    {
        if (_host is null) return;
        Form?.UseLocaleMessages(FormTexts.Current.Messages);
        _host.Bind(Form?.Engine, CompactWidth, FieldTemplates);
        if (_host.Shell is not null) _host.Refresh();
    }

    private async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        if (Form is not { } form) return false;
        var done = OnSave is { } save
            ? await form.SubmitAsync((f, ct) => save(f.BuildOutput(), ct), cancellationToken)
            : await form.SubmitAsync();
        if (done) Submitted?.Invoke(this, new DynamicFormOutputEventArgs(form.BuildOutput()));
        return done;
    }
}
