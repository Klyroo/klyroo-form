namespace Klyroo.Form.Maui;

/// <summary>The error an entry of <see cref="KlyFormErrorSummary"/> stands for, when the user taps it.</summary>
public sealed class FormErrorEntryEventArgs : EventArgs
{
    internal FormErrorEntryEventArgs(FormErrorEntry entry) => Entry = entry;

    /// <summary>The error: the field (or the cell of a repeatable list) it belongs to, and its message.</summary>
    public FormErrorEntry Entry { get; }
}

/// <summary>
/// The list of the errors of a form: the errors of the form as a whole, and the ones of every field once a submit or a Next has failed. A tap on a field's entry moves the focus to its control
/// and scrolls it into view (inside <see cref="KlyFormEngine{TModel}"/> and the other form views; use <see cref="EntrySelected"/> in a layout of your own). A screen reader is told
/// the title and the first error when a submit fails or an error of the form appears, and once the last error is gone, but not for every error fixed while editing. The summary takes no room while there is no error.
/// </summary>
public class KlyFormErrorSummary : ContentView
{
    /// <summary>Identifies <see cref="Form"/>.</summary>
    public static readonly BindableProperty FormProperty = BindableProperty.Create(nameof(Form), typeof(IFormRuntime), typeof(KlyFormErrorSummary), null, propertyChanged: (b, o, n) => ((KlyFormErrorSummary)b).Attach((IFormRuntime?)n));

    private readonly Border _box = new() { Padding = new Thickness(12), StrokeThickness = 1, IsVisible = false };
    private readonly VerticalStackLayout _stack = new() { Spacing = 6 };
    private readonly Label _title = FormUi.NewLabel(12, KlyTheme.Resolve(KlyColor.Error).Text, FontAttributes.Bold);
    private readonly VerticalStackLayout _list = new() { Spacing = 4 };
    private readonly List<(FormErrorEntry Entry, Label Label)> _lines = [];
    private FormEngine? _engine;
    private int _announcedFailures;
    private string _announcedFormErrors = "";
    private string _liveText = "";
    private bool _refreshPending;

    /// <summary>Creates an error summary.</summary>
    public KlyFormErrorSummary()
    {
        var error = KlyTheme.Resolve(KlyColor.Error);
        _box.SetRadius(FormThemeExtensions.RadiusOf(1));
        _box.SetThemeColor(BackgroundColorProperty, error.Fill.WithAlpha(0.10f));
        _box.SetStroke(error.Fill.WithAlpha(0.40f));
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level2);
        _stack.Add(_title);
        _stack.Add(_list);
        _box.Content = _stack;
        Content = _box;
        Unloaded += (_, _) => Detach();
        Loaded += (_, _) =>
        {
            if (_engine is null && Form is { } form) Attach(form);
        };
    }

    /// <summary>Raised when the user taps the entry of a field or a cell: after the focus moved to its control, when the summary is inside a form view that can do it.</summary>
    public event EventHandler<FormErrorEntryEventArgs>? EntrySelected;

    /// <summary>The controller whose errors to list: a <c>FormController&lt;TModel&gt;</c> or a <c>DynamicFormController</c>.</summary>
    /// <exception cref="ArgumentException">The value is a runtime that Klyroo.Form did not create.</exception>
    public IFormRuntime? Form { get => (IFormRuntime?)GetValue(FormProperty); set => SetValue(FormProperty, value); }

    /// <summary>The errors listed now.</summary>
    public IReadOnlyList<FormErrorEntry> Entries => [.. _lines.Select(l => l.Entry)];

    /// <summary>The title of the list ("There are 2 errors to fix"), or <c>null</c> while there is nothing to list.</summary>
    public string? Title => _box.IsVisible ? _title.Text : null;

    /// <summary>What a screen reader was last told: the title and the first error, or empty.</summary>
    public string LiveText => _liveText;

    /// <summary>Moves the focus to the control of an entry. Set by the form views that hold the summary.</summary>
    internal Func<FormErrorEntry, bool>? FocusEntry { get; set; }

    internal Border Box => _box;

    /// <summary>The line of every entry, for the tests.</summary>
    internal IReadOnlyList<(FormErrorEntry Entry, Label Label)> Lines => _lines;

    /// <summary>Lists the errors again. It runs by itself after every change of the form.</summary>
    public void Refresh()
    {
        _refreshPending = false;
        if (_engine is not { } engine)
        {
            _box.IsVisible = false;
            return;
        }
        var texts = FormTexts.Current;
        var entries = engine.GetErrorEntries().Where(e => e.IsFormLevel || engine.SubmitFailed).ToList();
        _liveText = NextLiveText(engine, entries, texts);
        if (entries.Count > 0 && _liveText.Length > 0 && _liveText != _lastAnnounced)
        {
            _lastAnnounced = _liveText;
            FormUi.Announce(_liveText);
        }
        else if (entries.Count == 0)
        {
            _lastAnnounced = "";
        }

        _box.IsVisible = entries.Count > 0;
        _title.Text = entries.Count > 0 ? texts.ErrorsToFix(entries.Count) : null;
        _list.Children.Clear();
        _lines.Clear();
        foreach (var entry in entries)
        {
            var color = KlyTheme.Resolve(KlyColor.Error).Text;
            var line = FormUi.NewLabel(12, color);
            line.Text = Describe(engine, entry, texts);
            if (!entry.IsFormLevel)
            {
                line.TextDecorations = TextDecorations.Underline;
                var tapped = entry;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => Select(tapped);
                line.GestureRecognizers.Add(tap);
                SemanticProperties.SetHint(line, texts.FieldError(engine.AllFields.FirstOrDefault(f => f.Key == entry.FieldKey)?.Label ?? entry.FieldKey ?? "", entry.Message));
            }
            _list.Add(line);
            _lines.Add((entry, line));
        }
    }

    private string _lastAnnounced = "";

    /// <summary>Does what a tap on the entry does: moves the focus to its control, then raises <see cref="EntrySelected"/>.</summary>
    internal void Select(FormErrorEntry entry)
    {
        FocusEntry?.Invoke(entry);
        EntrySelected?.Invoke(this, new FormErrorEntryEventArgs(entry));
    }

    // A live region announces when its text changes, so the text changes only when something new is worth saying: after a failed attempt, when a
    // form-level error appears, and (to empty) when the last error is gone. Editing a field after a failed submit does not announce anything.
    private string NextLiveText(FormEngine engine, IReadOnlyList<FormErrorEntry> entries, FormTexts texts)
    {
        var failures = engine.SubmitFailureCount;
        var formErrors = string.Join('\n', engine.FormErrors);
        if (entries.Count == 0)
        {
            _liveText = "";
        }
        else if (failures != _announcedFailures || formErrors != _announcedFormErrors)
        {
            _liveText = $"{texts.ErrorsToFix(entries.Count)}. {Describe(engine, entries[0], texts)}";
        }
        _announcedFailures = failures;
        _announcedFormErrors = formErrors;
        return _liveText;
    }

    // "Email: This field is required", or "SKU (Lines, 2): ..." for a cell of a repeatable list.
    private static string Describe(FormEngine engine, FormErrorEntry entry, FormTexts texts)
    {
        if (entry.IsFormLevel) return entry.Message;
        var fields = engine.AllFields;
        var owner = fields.FirstOrDefault(f => f.Key == entry.FieldKey);
        var label = owner?.Label ?? entry.FieldKey;
        if (entry.RowIndex is { } row && entry.ChildKey is { } childKey)
        {
            var child = owner?.Fields?.FirstOrDefault(f => f.Key == childKey);
            return texts.CellError(child?.Label ?? childKey, label ?? "", row + 1, entry.Message);
        }
        return texts.FieldError(label ?? "", entry.Message);
    }

    private void Attach(IFormRuntime? form)
    {
        Detach();
        if (form is null)
        {
            Refresh();
            return;
        }
        _engine = FormUi.EngineOf(form);
        if (_box.AutomationId is null) _box.AutomationId = FormUi.SummaryId(_engine);
        _announcedFailures = _engine.SubmitFailureCount;
        _announcedFormErrors = string.Join('\n', _engine.FormErrors);
        _engine.Changed += OnChanged;
        Refresh();
    }

    private void Detach()
    {
        if (_engine is not null) _engine.Changed -= OnChanged;
        _engine = null;
    }

    private void OnChanged()
    {
        if (_refreshPending) return;
        _refreshPending = true;
        FormUi.Post(this, Refresh);
    }
}
