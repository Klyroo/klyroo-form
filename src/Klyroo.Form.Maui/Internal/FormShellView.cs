namespace Klyroo.Form.Maui;

/// <summary>
/// The inside of <see cref="KlyFormEngine{TModel}"/> and <see cref="KlyDynamicFormEngine"/>: the header, the stepper, the fields and the actions of a form, shared by both.
/// </summary>
internal sealed class FormShellView : FormShellBase
{
    private readonly Label _eyebrow = FormUi.NewLabel(11, KlyTheme.Resolve(KlyColor.Primary).Text, FontAttributes.Bold);
    private readonly Label _title = FormUi.NewLabel(16, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly FormIconButton _close = new(KlyIconNames.Close, "Close form") { IsVisible = false, VerticalOptions = LayoutOptions.Start };
    private readonly VerticalStackLayout _header = new() { Spacing = 12 };
    private readonly KlyStepper _stepper = new() { Size = KlySize.Sm };
    private readonly Label _stepDescription = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly FormNodeGrid _fields;
    private readonly KlyButton _back = new() { Variant = ButtonVariant.Outline, Color = KlyColor.Neutral, IsVisible = false };
    private readonly KlyButton _reset = new() { Variant = ButtonVariant.Ghost, Color = KlyColor.Neutral, IsVisible = false };
    private readonly KlyButton _cancel = new() { Variant = ButtonVariant.Outline, Color = KlyColor.Neutral, IsVisible = false };
    private readonly KlyButton _next = new() { IsVisible = false };
    private readonly KlyButton _submit = new();
    private readonly Border _headerLine = new() { HeightRequest = 1, StrokeThickness = 0 };
    private readonly Border _footerLine = new() { HeightRequest = 1, StrokeThickness = 0 };
    private FormShellMode _mode;
    private string? _submitLabel;
    private bool _showReset = true;
    private Func<Task>? _cancelAction;
    private List<string> _stepKeys = [];

    public FormShellView(FormContext context, Func<CancellationToken, Task<bool>> submit)
        : base(context, submit)
    {
        var schema = Engine.Schema;
        _fields = new FormNodeGrid(context);
        context.Enter = () => _ = EnterAsync();

        _eyebrow.Text = schema.Eyebrow;
        _eyebrow.IsVisible = !string.IsNullOrEmpty(schema.Eyebrow);
        _eyebrow.TextTransform = TextTransform.Uppercase;
        _title.Text = schema.Title;
        _title.IsVisible = !string.IsNullOrEmpty(schema.Title);
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level2);
        FormUi.Show(_description, schema.Description);

        var texts = new VerticalStackLayout { Spacing = 2, Children = { _eyebrow, _title, _description } };
        var top = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        top.Add(texts, 0);
        top.Add(_close, 1);
        _header.Add(top);
        _header.Add(_stepper);
        _headerLine.SetThemeColor(BackgroundColorProperty, KlyTheme.Border);
        _footerLine.SetThemeColor(BackgroundColorProperty, KlyTheme.Border);

        _close.Clicked += async (_, _) => await CancelAsync();
        _back.Clicked += async (_, _) => await PreviousAsync();
        _reset.Clicked += (_, _) => Engine.ResetToSnapshot();
        _cancel.Clicked += async (_, _) => await CancelAsync();
        _next.Clicked += async (_, _) => await NextAsync();
        _submit.Clicked += async (_, _) => await SubmitAsync();

        var left = new HorizontalStackLayout { Spacing = 8, Children = { _back, _reset } };
        var right = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End, Children = { _cancel, _next, _submit } };
        var footer = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        footer.Add(left, 0);
        footer.Add(right, 1);

        Add(_header);
        Add(_headerLine);
        Add(_stepDescription);
        Add(Summary);
        Add(_fields.Grid);
        Add(_footerLine);
        Add(footer);
        _back.AutomationId = Engine.IdPrefix + "back";
        _reset.AutomationId = Engine.IdPrefix + "reset";
        _cancel.AutomationId = Engine.IdPrefix + "cancel";
        _next.AutomationId = Engine.IdPrefix + "next";
        _submit.AutomationId = Engine.IdPrefix + "submit";
        _close.AutomationId = Engine.IdPrefix + "close";
    }

    public FormShellMode Mode => _mode;

    public KlyStepper Stepper => _stepper;

    public FormGrid Grid => _fields.Grid;

    public Label TitleLabel => _title;

    public Label EyebrowLabel => _eyebrow;

    public Label DescriptionLabel => _description;

    public Label StepDescription => _stepDescription;

    public FormIconButton CloseButton => _close;

    public KlyButton BackButton => _back;

    public KlyButton ResetButton => _reset;

    public KlyButton CancelButton => _cancel;

    public KlyButton NextButton => _next;

    public KlyButton SubmitButton => _submit;

    public View HeaderLine => _headerLine;

    /// <summary>Sets what the form shows and does around its fields. The header, footer and stepper follow the next <see cref="Refresh"/>.</summary>
    public void Configure(FormShellMode mode, string? submitLabel, bool showReset, Func<Task>? cancel)
    {
        _mode = mode;
        _submitLabel = submitLabel;
        _showReset = showReset;
        _cancelAction = cancel;
    }

    public override void Refresh()
    {
        var form = Engine;
        var texts = FormTexts.Current;
        var schema = form.Schema;
        var stepped = form.IsStepped;
        var submitting = form.IsSubmitting;

        // Header and stepper.
        var showStepper = stepped && schema.ShowStepper;
        _header.IsVisible = !string.IsNullOrEmpty(schema.Title) || !string.IsNullOrEmpty(schema.Description) || showStepper;
        _headerLine.IsVisible = _header.IsVisible;
        _close.IsVisible = _mode != FormShellMode.Inline && _cancelAction is not null;
        _close.SetDescription(texts.CloseForm);
        _close.IsEnabled = !submitting;
        RefreshStepper(showStepper);

        FormUi.Show(_stepDescription, form.CurrentStep?.Description);

        // Fields.
        _fields.Update(form.CurrentVisibleTopFields);

        // Actions.
        _back.Text = texts.Back;
        _back.IsVisible = stepped && !form.IsFirstStep;
        _back.IsEnabled = !submitting;
        _reset.Text = texts.Reset;
        _reset.IsVisible = _showReset && form.IsDirty;
        _reset.IsEnabled = !submitting;
        _cancel.Text = texts.Cancel;
        _cancel.IsVisible = _cancelAction is not null;
        _cancel.IsEnabled = !submitting;
        var more = stepped && !form.IsLastStep;
        _next.Text = texts.Next;
        _next.IsVisible = more;
        _next.IsEnabled = !submitting;
        _submit.Text = submitting ? texts.Saving : _submitLabel ?? texts.Submit;
        _submit.IsVisible = !more;
        _submit.IsEnabled = !submitting;
    }

    /// <summary>What the return key does in a field: before the last step it means Next, not a submit of the whole wizard.</summary>
    public override Task<bool> SubmitAsync()
    {
        if (Engine.IsStepped && !Engine.IsLastStep) return NextAsync();
        return base.SubmitAsync();
    }

    /// <summary>Moves to the next step, after validating the current one. True when the step changed.</summary>
    public async Task<bool> NextAsync()
    {
        var form = Engine;
        var moved = false;
        var ok = await FormRun.RunAsync(async () => moved = await form.NextStepAsync(), form, FormTexts.Current, Token);
        if (!ok || !moved) RequestFocus();
        return moved;
    }

    /// <summary>Moves to the previous step. True when the step changed.</summary>
    public Task<bool> PreviousAsync() => Engine.PreviousStepAsync();

    /// <summary>Calls the cancel handler of the component, as the Cancel button and the close button do.</summary>
    public async Task CancelAsync()
    {
        if (_cancelAction is not null && !Engine.IsSubmitting) await _cancelAction();
    }

    private async Task EnterAsync()
    {
        if (Engine.IsSubmitting) return;
        await SubmitAsync();
    }

    private void RefreshStepper(bool show)
    {
        _stepper.IsVisible = show;
        if (!show) return;
        var form = Engine;
        var steps = form.Schema.Steps;
        var visible = new List<(int Index, StepNode Step)>();
        for (var i = 0; i < steps.Count; i++)
        {
            // A step that is skipped is not on the list, unless the form is on it.
            if (i != form.CurrentStepIndex && steps[i].SkipWhen?.Invoke(form) == true) continue;
            visible.Add((i, steps[i]));
        }
        var keys = visible.Select(v => v.Step.Key).ToList();
        if (!keys.SequenceEqual(_stepKeys, StringComparer.Ordinal))
        {
            _stepKeys = keys;
            _stepper.Items = [.. visible.Select(v => new KlyStepperItem(v.Step.Title))];
        }
        var current = visible.FindIndex(v => v.Index == form.CurrentStepIndex);
        if (_stepper.Current != Math.Max(current, 0)) _stepper.Current = Math.Max(current, 0);
    }
}
