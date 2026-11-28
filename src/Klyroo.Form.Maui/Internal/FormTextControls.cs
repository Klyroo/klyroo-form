using System.Globalization;

namespace Klyroo.Form.Maui;

/// <summary>A single-line text field: text, phone number, one-time code.</summary>
internal sealed class FormTextControl : FormControl
{
    private readonly KlyInput _input = new() { ReturnType = ReturnType.Done };

    public FormTextControl(FormSlot slot, Keyboard keyboard, bool limitLength)
        : base(slot)
    {
        var field = slot.Field;
        _input.Keyboard = keyboard;
        _input.Placeholder = field.Placeholder;
        if (limitLength && field.MaxLength is { } max) _input.MaxLength = max;
        _input.TextChanged += (_, e) =>
        {
            if (!Busy) Slot.Set(e.NewTextValue ?? "");
        };
        Init(_input);
        HookBlur(_input);
        HookEnter(_input);
    }

    protected override object? Cleared => "";

    public KlyInput Input => _input;

    protected override void Show()
    {
        var text = Slot.Value?.ToString() ?? "";
        if ((_input.Text ?? "") != text) _input.Text = text;
    }
}

/// <summary>A multi-line text field.</summary>
internal sealed class FormTextareaControl : FormControl
{
    private readonly KlyTextarea _input = new() { Rows = 3 };

    public FormTextareaControl(FormSlot slot)
        : base(slot)
    {
        var field = slot.Field;
        _input.Placeholder = field.Placeholder;
        if (field.MaxLength is { } max) _input.MaxLength = max;
        _input.TextChanged += (_, e) =>
        {
            if (!Busy) Slot.Set(e.NewTextValue ?? "");
        };
        Init(_input);
        HookBlur(_input);
    }

    protected override object? Cleared => "";

    public KlyTextarea Input => _input;

    protected override void Show()
    {
        var text = Slot.Value?.ToString() ?? "";
        if ((_input.Text ?? "") != text) _input.Text = text;
    }
}

/// <summary>A password: a masked text field with a button that shows what is typed and the checklist of the requirements the schema declares.</summary>
internal sealed class FormPasswordControl : FormControl
{
    private readonly KlyInput _input = new() { IsPassword = true, ReturnType = ReturnType.Done };
    private readonly KlyButton _toggle = new() { Variant = ButtonVariant.Ghost, Color = KlyColor.Neutral, Size = KlySize.Xs, VerticalOptions = LayoutOptions.Center };
    private readonly VerticalStackLayout _requirements = new() { Spacing = 2, IsVisible = false };
    private readonly List<(FormPasswordRequirement Requirement, Label Mark, Label Text, View Line)> _rows = [];
    private bool _revealed;

    public FormPasswordControl(FormSlot slot)
        : base(slot)
    {
        var field = slot.Field;
        _input.Placeholder = field.Placeholder;
        if (field.MaxLength is { } max) _input.MaxLength = max;
        _input.TextChanged += (_, e) =>
        {
            if (!Busy) Slot.Set(e.NewTextValue ?? "");
        };
        _toggle.Clicked += (_, _) =>
        {
            _revealed = !_revealed;
            ShowToggle();
        };

        var row = new Grid { ColumnSpacing = 4, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        row.Add(_input, 0);
        row.Add(_toggle, 1);
        var stack = new VerticalStackLayout { Spacing = 6, Children = { row, _requirements } };

        if (field.PasswordRequirements is { Count: > 0 } requirements)
        {
            _requirements.IsVisible = true;
            SemanticProperties.SetDescription(_requirements, FormTexts.Current.PasswordRequirements);
            foreach (var requirement in requirements)
            {
                var mark = FormUi.NewLabel(11, KlyTheme.TextMuted);
                var text = FormUi.NewLabel(11, KlyTheme.TextMuted);
                text.Text = requirement.Label;
                var line = new HorizontalStackLayout { Spacing = 6, Children = { mark, text } };
                SemanticProperties.SetDescription(line, requirement.Label);
                _requirements.Add(line);
                _rows.Add((requirement, mark, text, line));
            }
        }

        ShowToggle();
        Init(stack);
        HookBlur(_input);
        HookEnter(_input);
    }

    protected override object? Cleared => "";

    public KlyInput Input => _input;

    public KlyButton Toggle => _toggle;

    public bool Revealed => _revealed;

    public IReadOnlyList<(FormPasswordRequirement Requirement, Label Mark, Label Text, View Line)> Requirements => _rows;

    protected override void Show()
    {
        var text = Slot.Value?.ToString() ?? "";
        if ((_input.Text ?? "") != text) _input.Text = text;
        ShowToggle();
        var texts = FormTexts.Current;
        foreach (var (requirement, mark, label, line) in _rows)
        {
            var met = requirement.Validate(text);
            mark.Text = met ? "✓" : "○";
            var color = met ? KlyTheme.Resolve(KlyColor.Success).Text : KlyTheme.TextMuted;
            mark.SetThemeColor(Label.TextColorProperty, color);
            label.SetThemeColor(Label.TextColorProperty, color);
            SemanticProperties.SetDescription(line, $"{requirement.Label} {(met ? texts.RequirementMet : texts.RequirementNotMet)}");
        }
    }

    protected override void ApplyEnabled(bool enabled)
    {
        _input.IsEnabled = enabled;
        _toggle.IsEnabled = enabled;
    }

    private void ShowToggle()
    {
        var texts = FormTexts.Current;
        _input.IsPassword = !_revealed;
        _toggle.Text = _revealed ? texts.Hide : texts.Show;
        SemanticProperties.SetDescription(_toggle, _revealed ? texts.HidePassword : texts.ShowPassword);
    }
}

/// <summary>A number: the stepper field, with the exact digits of a decimal.</summary>
internal sealed class FormNumberControl : FormControl
{
    private readonly KlyInputNumber _input = new();

    public FormNumberControl(FormSlot slot)
        : base(slot)
    {
        var field = slot.Field;
        _input.Placeholder = field.Placeholder;
        // The bounds are not given to the field: it would limit the number silently, where the form reports it ("Value must be at most 10") and keeps what was typed.
        if (field.Step is > 0 and var step && FormValueConverter.TryExactDecimal(step, out var exact)) _input.Step = exact;
        _input.ValueChanged += (_, e) =>
        {
            if (Busy) return;
            Slot.Set(e.NewValue is { } number ? FormValueConverter.NumberToControl(number) : null);
        };
        Init(_input);
        HookBlur(_input);
        HookEnter(_input);
    }

    protected override bool HasValue => Slot.Value is not null;

    public KlyInputNumber Input => _input;

    protected override void Show()
    {
        decimal? number = FormValueConverter.TryExactDecimal(Slot.Value, out var exact) ? exact : null;
        if (_input.Value != number) _input.Value = number;
    }
}

/// <summary>A slider: the native slider and the number it stands for.</summary>
internal sealed class FormSliderControl : FormControl
{
    private readonly KlySlider _slider = new();
    private readonly Label _value = FormUi.NewLabel(12, KlyTheme.TextMuted);

    public FormSliderControl(FormSlot slot)
        : base(slot)
    {
        var field = slot.Field;
        _slider.Min = field.Min ?? 0;
        _slider.Max = field.Max ?? 100;
        if (field.Step is > 0 and var step) _slider.Step = step;
        _value.MinimumWidthRequest = 32;
        _value.HorizontalTextAlignment = TextAlignment.End;
        _value.VerticalOptions = LayoutOptions.Center;
        _slider.ValueChanged += (_, e) =>
        {
            if (Busy) return;
            Slot.Commit(FormValueConverter.NumberToControl(e.NewValue));
        };
        var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        row.Add(_slider, 0);
        row.Add(_value, 1);
        Init(row);
    }

    protected override bool CanClear => false;

    public KlySlider Slider => _slider;

    public Label ValueLabel => _value;

    protected override void Show()
    {
        var shown = FormUi.TryDouble(Slot.Value, out var number) ? number : Slot.Field.Min ?? 0;
        if (_slider.Value != shown) _slider.Value = shown;
        _value.Text = shown.ToString(CultureInfo.InvariantCulture);
    }

    protected override void ApplyEnabled(bool enabled) => _slider.IsEnabled = enabled;
}

/// <summary>A checkbox or a switch, with the label of the field as its text.</summary>
internal sealed class FormCheckControl : FormControl
{
    private readonly KlyCheckbox? _checkbox;
    private readonly KlySwitch? _switch;

    public FormCheckControl(FormSlot slot)
        : base(slot)
    {
        if (slot.Field.Kind == FormFieldKind.Switch)
        {
            _switch = new KlySwitch();
            _switch.Toggled += (_, e) =>
            {
                if (!Busy) Slot.Commit(e.Value);
            };
            Init(_switch);
        }
        else
        {
            _checkbox = new KlyCheckbox();
            _checkbox.CheckedChanged += (_, e) =>
            {
                if (!Busy) Slot.Commit(e.Value);
            };
            Init(_checkbox);
        }
    }

    protected override bool CanClear => false;

    public KlyCheckbox? Checkbox => _checkbox;

    public KlySwitch? Switch => _switch;

    protected override void Show()
    {
        var on = Slot.Value is true;
        var text = Slot.Field.Label is { Length: > 0 } label ? label + (Slot.Required ? " *" : "") : null;
        if (_switch is not null)
        {
            if (_switch.IsToggled != on) _switch.IsToggled = on;
            _switch.Text = text;
        }
        else if (_checkbox is not null)
        {
            if (_checkbox.IsChecked != on) _checkbox.IsChecked = on;
            _checkbox.Text = text;
        }
    }
}

/// <summary>A star rating from 1 to the maximum of the field.</summary>
internal sealed class FormRatingControl : FormControl
{
    private readonly KlyInputRating _rating = new();

    public FormRatingControl(FormSlot slot)
        : base(slot)
    {
        _rating.Max = Math.Max(1, (int)Math.Round(slot.Field.Max ?? 5));
        _rating.ValueChanged += (_, e) =>
        {
            if (!Busy) Slot.Commit((int)Math.Round(e.NewValue));
        };
        Init(_rating);
    }

    protected override bool HasValue => Current > 0;

    public KlyInputRating Rating => _rating;

    private int Current => FormUi.TryDouble(Slot.Value, out var r) ? (int)Math.Round(r) : 0;

    protected override void Show()
    {
        if ((int)Math.Round(_rating.Value) != Current) _rating.Value = Current;
    }

}

/// <summary>A color: the picker and its hex field.</summary>
internal sealed class FormColorControl : FormControl
{
    private readonly KlyColorPicker _picker = new();

    public FormColorControl(FormSlot slot)
        : base(slot)
    {
        _picker.ValueChanged += (_, e) =>
        {
            if (!Busy) Slot.Commit(e.NewValue);
        };
        Init(_picker);
    }

    public KlyColorPicker Picker => _picker;

    protected override void Show()
    {
        var value = Slot.Value?.ToString();
        if (string.IsNullOrEmpty(value)) value = null;
        if (!string.Equals(_picker.Value, value, StringComparison.OrdinalIgnoreCase)) _picker.Value = value;
    }
}

/// <summary>A list of free-form tags.</summary>
internal sealed class FormTagsControl : FormControl
{
    private readonly KlyInputTags _tags = new();

    public FormTagsControl(FormSlot slot)
        : base(slot)
    {
        _tags.Placeholder = slot.Field.Placeholder ?? FormTexts.Current.TagPlaceholder;
        _tags.ValueChanged += (_, e) =>
        {
            if (!Busy) Slot.Commit(new List<string>(e.NewValue ?? []));
        };
        Init(_tags);
        HookBlur(_tags);
    }

    public KlyInputTags Tags => _tags;

    protected override void Show()
    {
        var tags = FormUi.AsStringList(Slot.Value);
        if (!tags.SequenceEqual(_tags.Value ?? [])) _tags.Value = tags;
        _tags.Placeholder = Slot.Field.Placeholder ?? FormTexts.Current.TagPlaceholder;
    }
}
