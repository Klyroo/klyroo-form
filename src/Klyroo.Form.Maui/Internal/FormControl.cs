namespace Klyroo.Form.Maui;

/// <summary>
/// A native control bound to one <see cref="FormSlot"/>: it builds the klyroo-ui input of the kind of the field once, writes what the user does into the form,
/// and <see cref="Refresh"/> shows the state of the form in it again without touching what already is the same (so typing is never disturbed).
/// </summary>
internal abstract class FormControl
{
    private FormIconButton? _clear;
    private bool _busy;

    protected FormControl(FormSlot slot)
    {
        Slot = slot;
    }

    public FormSlot Slot { get; }

    /// <summary>The view that goes in the form: the control, with its clear button when the field is clearable.</summary>
    public View Root { get; private set; } = null!;

    /// <summary>The control itself.</summary>
    public View Inner { get; private set; } = null!;

    /// <summary>The clear button of a clearable field, for the tests.</summary>
    public FormIconButton? ClearButton => _clear;

    /// <summary>What the field holds once it is cleared: an empty text for the text kinds, nothing for the others.</summary>
    protected virtual object? Cleared => null;

    /// <summary>Whether the field has something to clear.</summary>
    protected virtual bool HasValue => !FormEngine.IsEmptyValue(Slot.Value);

    /// <summary>True while <see cref="Show"/> runs: the events the control raises then come from the form, not from the user.</summary>
    protected bool Busy => _busy;

    /// <summary>Builds <see cref="Root"/> around the control. Every derived control calls it once, at the end of its constructor.</summary>
    protected void Init(View inner)
    {
        Inner = inner;
        if (Slot.Field.Clearable && CanClear)
        {
            _clear = new FormIconButton(KlyIconNames.Close, FormTexts.Current.Clear, KlyTheme.TextMuted) { IsVisible = false, VerticalOptions = LayoutOptions.Start };
            _clear.Clicked += (_, _) => Slot.Commit(Cleared);
            var grid = new Grid { ColumnSpacing = 4, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            grid.Add(inner, 0);
            grid.Add(_clear, 1);
            Root = grid;
        }
        else
        {
            Root = inner;
        }
        Root.AutomationId = Slot.Id;
    }

    protected virtual bool CanClear => true;

    /// <summary>Shows the state of the form in the control.</summary>
    public void Refresh()
    {
        _busy = true;
        try
        {
            Show();
        }
        finally
        {
            _busy = false;
        }
        if (_clear is not null)
        {
            _clear.SetDescription(FormTexts.Current.Clear);
            _clear.IsVisible = HasValue;
            _clear.IsEnabled = !Slot.Disabled;
        }
        ApplyEnabled(!Slot.Disabled);
    }

    /// <summary>Puts the value of the form in the control. Only what differs is written.</summary>
    protected abstract void Show();

    /// <summary>Enables or disables the control, which dims itself the way the web control does.</summary>
    protected virtual void ApplyEnabled(bool enabled)
    {
        if (Inner.IsEnabled != enabled) Inner.IsEnabled = enabled;
    }

    /// <summary>Puts the focus in the control. False when there is nothing to focus.</summary>
    public virtual bool Focus()
    {
        foreach (var view in FormUi.Descendants(Inner).OfType<View>())
        {
            if (view is Entry or Editor or Picker or DatePicker or TimePicker or SearchBar or Slider && view.IsEnabled && view.IsVisible && view.Focus()) return true;
        }
        return false;
    }

    /// <summary>Tells the form the control lost focus, from the native text controls inside <paramref name="root"/>.</summary>
    protected void HookBlur(View root)
    {
        foreach (var view in FormUi.Descendants(root).OfType<View>())
        {
            if (view is Entry or Editor or Picker or DatePicker or TimePicker or SearchBar) view.Unfocused += (_, _) =>
            {
                if (!_busy) Slot.Blur();
            };
        }
    }

    /// <summary>Calls <see cref="FormSlot.Enter"/> when the user presses the return key in the entries inside <paramref name="root"/>.</summary>
    protected void HookEnter(View root)
    {
        foreach (var entry in FormUi.Descendants(root).OfType<Entry>()) entry.Completed += (_, _) => Slot.Enter?.Invoke();
    }
}
