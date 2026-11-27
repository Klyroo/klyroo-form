using System.Globalization;

namespace Klyroo.Form.Maui;

/// <summary>The text forms of dates and times in a form: the ones the web inputs produce and the engine stores.</summary>
internal static class FormDateText
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm";

    public static DateTime? ParseDate(object? value) =>
        value?.ToString() is { Length: > 0 } text
        && (DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out exact))
            ? exact.Date
            : null;

    public static TimeOnly? ParseTime(object? value) =>
        value?.ToString() is { Length: > 0 } text && TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? new TimeOnly(time.Hour, time.Minute)
            : null;

    public static DateTime? ParseDateTime(object? value) =>
        value?.ToString() is { Length: > 0 } text && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, 0)
            : null;

    public static string? Format(DateTime? date) => date?.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string? Format(TimeOnly? time) => time?.ToString(TimeFormat, CultureInfo.InvariantCulture);
}

/// <summary>A date: the date picker of Klyroo.UI.Maui, stored as <c>yyyy-MM-dd</c>.</summary>
internal sealed class FormDateControl : FormControl
{
    private readonly KlyInputDate _input = new();

    public FormDateControl(FormSlot slot)
        : base(slot)
    {
        _input.ValueChanged += (_, e) =>
        {
            if (!Busy) Slot.Commit(FormDateText.Format(e.NewValue));
        };
        Init(_input);
        HookBlur(_input);
    }

    public KlyInputDate Input => _input;

    protected override void Show()
    {
        var date = FormDateText.ParseDate(Slot.Value);
        if (_input.Value != date) _input.Value = date;
    }
}

/// <summary>A time of day, stored as <c>HH:mm</c>.</summary>
internal sealed class FormTimeControl : FormControl
{
    private readonly KlyInputTime _input = new();

    public FormTimeControl(FormSlot slot)
        : base(slot)
    {
        _input.ValueChanged += (_, e) =>
        {
            if (!Busy) Slot.Commit(FormDateText.Format(e.NewValue));
        };
        Init(_input);
        HookBlur(_input);
    }

    public KlyInputTime Input => _input;

    protected override void Show()
    {
        var time = FormDateText.ParseTime(Slot.Value);
        if (_input.Value != time) _input.Value = time;
    }
}

/// <summary>
/// A date and a time, side by side, stored as <c>yyyy-MM-ddTHH:mm</c> once both are chosen. The half that is chosen first stays on screen while the form holds nothing yet:
/// the field is empty for the form until it is complete, as the web input is.
/// </summary>
internal sealed class FormDateTimeControl : FormControl
{
    private readonly KlyInputDate _date = new();
    private readonly KlyInputTime _time = new();

    public FormDateTimeControl(FormSlot slot)
        : base(slot)
    {
        _date.ValueChanged += (_, _) => Changed();
        _time.ValueChanged += (_, _) => Changed();
        var row = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        row.Add(_date, 0);
        row.Add(_time, 1);
        Init(row);
        HookBlur(row);
    }

    public KlyInputDate DateInput => _date;

    public KlyInputTime TimeInput => _time;

    protected override bool HasValue => Compose() is not null || Slot.Value is not null;

    protected override object? Cleared => null;

    private string? Compose() =>
        _date.Value is { } date && _time.Value is { } time
            ? new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0).ToString(FormDateText.DateTimeFormat, CultureInfo.InvariantCulture)
            : null;

    protected override void Show()
    {
        var wanted = FormDateText.ParseDateTime(Slot.Value);
        var wantedText = wanted?.ToString(FormDateText.DateTimeFormat, CultureInfo.InvariantCulture);
        if (Compose() == wantedText) return;
        _date.Value = wanted?.Date;
        _time.Value = wanted is { } w ? TimeOnly.FromDateTime(w) : null;
    }

    protected override void ApplyEnabled(bool enabled)
    {
        _date.IsEnabled = enabled;
        _time.IsEnabled = enabled;
    }

    private void Changed()
    {
        if (Busy) return;
        Slot.Commit(Compose());
    }
}

/// <summary>A range of two dates, stored as a list of two <c>yyyy-MM-dd</c> texts (an empty text for a missing date). The end cannot come before the start.</summary>
internal sealed class FormDateRangeControl : FormControl
{
    private readonly KlyInputDate _start = new();
    private readonly KlyInputDate _end = new();

    public FormDateRangeControl(FormSlot slot)
        : base(slot)
    {
        var texts = FormTexts.Current;
        _start.AccessibleName = texts.StartDate;
        _end.AccessibleName = texts.EndDate;
        _start.ValueChanged += (_, _) => Changed();
        _end.ValueChanged += (_, _) => Changed();
        var dash = FormUi.NewLabel(14, KlyTheme.TextMuted);
        dash.Text = "–";
        dash.VerticalOptions = LayoutOptions.Center;
        var row = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        row.Add(_start, 0);
        row.Add(dash, 1);
        row.Add(_end, 2);
        Init(row);
        HookBlur(row);
    }

    public KlyInputDate Start => _start;

    public KlyInputDate End => _end;

    protected override void Show()
    {
        var texts = FormTexts.Current;
        _start.AccessibleName = texts.StartDate;
        _end.AccessibleName = texts.EndDate;
        var range = FormUi.AsStringList(Slot.Value);
        var start = FormDateText.ParseDate(range.Count > 0 ? range[0] : null);
        var end = FormDateText.ParseDate(range.Count > 1 ? range[1] : null);
        if (_start.Value != start) _start.Value = start;
        if (_end.Value != end) _end.Value = end;
        _start.Max = end;
        _end.Min = start;
    }

    protected override void ApplyEnabled(bool enabled)
    {
        _start.IsEnabled = enabled;
        _end.IsEnabled = enabled;
    }

    private void Changed()
    {
        if (Busy) return;
        var start = FormDateText.Format(_start.Value) ?? "";
        var end = FormDateText.Format(_end.Value) ?? "";
        Slot.Commit(start.Length == 0 && end.Length == 0 ? null : new List<string> { start, end });
    }
}
