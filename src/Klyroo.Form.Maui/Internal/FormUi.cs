using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;

namespace Klyroo.Form.Maui;

/// <summary>Small building blocks shared by the views of this library.</summary>
internal static class FormUi
{
    /// <summary>A label with a size and a color that follows the app theme.</summary>
    public static Label NewLabel(double size, KlyThemeColor color, FontAttributes attributes = FontAttributes.None, LineBreakMode lineBreak = LineBreakMode.WordWrap)
    {
        var label = new Label { FontSize = size, FontAttributes = attributes, LineBreakMode = lineBreak };
        label.SetThemeColor(Label.TextColorProperty, color);
        return label;
    }

    /// <summary>Sets a text and hides the label when there is none.</summary>
    public static void Show(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    /// <summary>Calls <paramref name="action"/> when the view is tapped while it is enabled.</summary>
    public static void Tap(View view, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (view.IsEnabled) action();
        };
        view.GestureRecognizers.Add(tap);
    }

    /// <summary>Takes a view out of the view that holds it, so that it can be put in another one.</summary>
    public static void Detach(View? view)
    {
        switch (view?.Parent)
        {
            case ScrollView scroll:
                scroll.Content = null;
                break;
            case Layout layout:
                layout.Remove(view);
                break;
            case Border border:
                border.Content = null;
                break;
            case ContentView contentView:
                contentView.Content = null;
                break;
        }
    }

    /// <summary>The dimming of a disabled control: <c>disabled:opacity-60</c> on the web.</summary>
    public static double OpacityOf(bool enabled) => enabled ? 1 : 0.6;

    /// <summary>Runs an action on the UI thread: at once when the caller already is on it, or when no dispatcher can be found (unit tests).</summary>
    public static void Post(BindableObject owner, Action action)
    {
        IDispatcher? dispatcher = null;
        try
        {
            dispatcher = (owner as Element)?.Dispatcher;
        }
        catch (InvalidOperationException)
        {
            // No application, no dispatcher: run inline.
        }
        if (dispatcher is { IsDispatchRequired: true }) dispatcher.Dispatch(action);
        else action();
    }

    /// <summary>Tells a screen reader a message. Does nothing where there is no screen reader service (the plain framework target).</summary>
    public static void Announce(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            SemanticScreenReader.Default.Announce(text);
        }
        catch (NotImplementedException)
        {
            // The plain net10.0 target has no screen reader.
        }
        catch (FeatureNotSupportedException)
        {
            // The platform has no announcement service.
        }
    }

    /// <summary>The views of the visual tree under a view, depth first, the view itself first.</summary>
    public static IEnumerable<IVisualTreeElement> Descendants(IVisualTreeElement root)
    {
        yield return root;
        foreach (var child in root.GetVisualChildren())
        {
            foreach (var element in Descendants(child)) yield return element;
        }
    }

    /// <summary>The first view of a type under a view, or the view itself.</summary>
    public static T? FindDescendant<T>(View root)
        where T : class =>
        Descendants(root).OfType<T>().FirstOrDefault();

    /// <summary>The nearest parent of a type.</summary>
    public static T? FindAncestor<T>(Element? from)
        where T : class
    {
        for (var element = from?.Parent; element is not null; element = element.Parent)
        {
            if (element is T found) return found;
        }
        return null;
    }

    /// <summary>Scrolls the nearest <see cref="ScrollView"/> above a view so that the view is visible. Does nothing without one.</summary>
    public static async Task RevealAsync(View view)
    {
        try
        {
            if (FindAncestor<ScrollView>(view) is { } scroll) await scroll.ScrollToAsync(view, ScrollToPosition.MakeVisible, true);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Scrolling is a convenience: a view that is not laid out yet stays where it is.
        }
    }

    /// <summary>The ids of the elements a control is built from, made from the id prefix of the form and a key: anything that is not a letter, a digit, a dash or an underscore becomes a dash.</summary>
    public static string Id(FormEngine form, string key)
    {
        Span<char> buffer = key.Length <= 128 ? stackalloc char[key.Length] : new char[key.Length];
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            buffer[i] = char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '-';
        }
        return form.IdPrefix + new string(buffer);
    }

    /// <summary>The id of the error summary of a form.</summary>
    public static string SummaryId(FormEngine form) => Id(form, "summary-" + form.Schema.FormKey);

    /// <summary>The id of the control to focus for an error: the field, the cell of a list, or the summary when the error belongs to the form.</summary>
    public static string TargetId(FormEngine form, FormErrorEntry entry) =>
        entry.FieldKey is null ? SummaryId(form)
        : entry.RowIndex is { } row && entry.ChildKey is { } child ? Id(form, $"{entry.FieldKey}-{row}-{child}")
        : Id(form, entry.FieldKey);

    /// <summary>The engine a controller of Klyroo.Form drives.</summary>
    /// <exception cref="ArgumentException">The runtime is not a controller of Klyroo.Form.</exception>
    public static FormEngine EngineOf(IFormRuntime form) =>
        form is IEngineOwner owner
            ? owner.Engine
            : throw new ArgumentException($"{form.GetType().Name} is not a controller of Klyroo.Form: pass a FormController<TModel> or a DynamicFormController.", nameof(form));

    /// <summary>The text of a value that is a list of strings, a string, or any sequence.</summary>
    public static List<string> AsStringList(object? value) => value switch
    {
        null => [],
        string s => s.Length == 0 ? [] : [s],
        IEnumerable<string> list => list.ToList(),
        System.Collections.IEnumerable items => items.Cast<object?>().Select(i => i?.ToString() ?? "").ToList(),
        _ => [value.ToString() ?? ""],
    };

    /// <summary>Whether a text is the start of a number the user can still finish.</summary>
    public static bool TryDouble(object? value, out double d)
    {
        switch (value)
        {
            case double dbl: d = dbl; return true;
            case int i: d = i; return true;
            case long l: d = l; return true;
            case float f: d = f; return true;
            case decimal m: d = (double)m; return true;
            case string s when s.Length > 0: return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d);
            default: d = 0; return false;
        }
    }
}

/// <summary>A tap target with an icon in the middle: the close, reset and remove buttons of the form views. It is 40 units wide and high, as a touch target must be.</summary>
internal sealed class FormIconButton : Border
{
    private readonly KlyIcon _icon;

    public FormIconButton(string iconName, string description, KlyThemeColor? color = null, KlySize iconSize = KlySize.Sm)
    {
        StrokeThickness = 0;
        Padding = 0;
        WidthRequest = 40;
        HeightRequest = 40;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Center;
        BackgroundColor = Colors.Transparent;
        _icon = new KlyIcon { Name = iconName, Size = iconSize, IconColor = color ?? KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
        Content = _icon;
        SemanticProperties.SetDescription(this, description);
        FormUi.Tap(this, SendClicked);
    }

    public event EventHandler? Clicked;

    public KlyIcon Icon => _icon;

    public void SetDescription(string description) => SemanticProperties.SetDescription(this, description);

    /// <summary>Presses the button, as a tap does.</summary>
    public void SendClicked()
    {
        if (IsEnabled && IsVisible) Clicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Opacity = FormUi.OpacityOf(IsEnabled);
    }
}
