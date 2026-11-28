namespace Klyroo.Form.Maui;

/// <summary>
/// The inside of <see cref="KlyFormPage{TModel}"/> and <see cref="KlyDynamicFormPage"/>: a list of the sections with their completion status, one card per section laid out by its span, and the submit button.
/// </summary>
internal sealed class FormPageShellView : FormShellBase
{
    private readonly Label _eyebrow = FormUi.NewLabel(12, KlyTheme.Resolve(KlyColor.Primary).Text, FontAttributes.Bold);
    private readonly Label _title = FormUi.NewLabel(20, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _summaryText = FormUi.NewLabel(12, KlyTheme.TextMuted);
    private readonly VerticalStackLayout _header = new() { Spacing = 4 };
    private readonly Border _headerLine = new() { HeightRequest = 1, StrokeThickness = 0 };
    private readonly Border _nav = new() { Padding = new Thickness(12), StrokeThickness = 1 };
    private readonly Label _navTitle = FormUi.NewLabel(11, KlyTheme.TextMuted, FontAttributes.Bold);
    private readonly VerticalStackLayout _navList = new() { Spacing = 2 };
    private readonly FormGrid _sections;
    private readonly Dictionary<string, SectionView> _views = new(StringComparer.Ordinal);
    private readonly KlyButton _submit = new() { HorizontalOptions = LayoutOptions.End };
    private List<string> _navKeys = [];
    private List<(Border Row, Label Label, KlyBadge Badge, string Key)> _navRows = [];

    public FormPageShellView(FormContext context, Func<CancellationToken, Task<bool>> submit)
        : base(context, submit)
    {
        Spacing = 24;
        var schema = Engine.Schema;
        context.Enter = () => _ = SubmitAsync();

        _eyebrow.Text = schema.Eyebrow;
        _eyebrow.IsVisible = !string.IsNullOrEmpty(schema.Eyebrow);
        _eyebrow.TextTransform = TextTransform.Uppercase;
        _title.Text = schema.Title;
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level1);
        FormUi.Show(_description, schema.Description);
        _header.Add(_eyebrow);
        _header.Add(_title);
        _header.Add(_description);
        _header.Add(_summaryText);
        _headerLine.SetThemeColor(BackgroundColorProperty, KlyTheme.Border);

        _navTitle.TextTransform = TextTransform.Uppercase;
        _nav.SetRadius(KlyTheme.RadiusOf(1.5));
        _nav.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _nav.SetStroke(KlyTheme.Border);
        _nav.Content = new VerticalStackLayout { Spacing = 6, Children = { _navTitle, _navList } };

        _sections = new FormGrid(12, compactWidth: 720, spacing: 16);
        _submit.Clicked += async (_, _) => await SubmitAsync();
        _submit.AutomationId = Engine.IdPrefix + "submit";

        Add(_header);
        Add(_headerLine);
        Add(Summary);
        Add(_nav);
        Add(_sections);
        Add(_submit);
    }

    public KlyButton SubmitButton => _submit;

    public Label TitleLabel => _title;

    public Label SummaryText => _summaryText;

    public Border Nav => _nav;

    public FormGrid Sections => _sections;

    /// <summary>The rows of the navigation, for the tests: the row, its label, its status badge and the key of its section.</summary>
    public IReadOnlyList<(Border Row, Label Label, KlyBadge Badge, string Key)> NavRows => _navRows;

    /// <summary>The card of a section, for the tests.</summary>
    public KlyCard? CardOf(string key) => _views.TryGetValue(key, out var view) ? view.Card : null;

    /// <summary>The grid of the fields of a section, for the tests.</summary>
    public FormGrid? FieldsOf(string key) => _views.TryGetValue(key, out var view) ? view.Fields.Grid : null;

    /// <summary>The reset button of a section, for the tests.</summary>
    public KlyButton? ResetOf(string key) => _views.TryGetValue(key, out var view) ? view.Reset : null;

    public override void Refresh()
    {
        var form = Engine;
        var texts = FormTexts.Current;
        var schema = form.Schema;
        var states = form.GetSectionStates();
        var blocking = states.Count(s => s.Status is FormPageSectionStatus.Invalid or FormPageSectionStatus.Pending);

        _header.IsVisible = !string.IsNullOrEmpty(schema.Title) || !string.IsNullOrEmpty(schema.Description);
        _headerLine.IsVisible = _header.IsVisible;
        _summaryText.Text = blocking == 0 ? texts.AllReady : texts.LeftToComplete(blocking);

        RefreshNav(states, texts);

        var visible = schema.Sections.Where(form.IsSectionVisible).ToList();
        var placed = new List<(View, int)>();
        foreach (var section in visible)
        {
            var state = states.First(s => s.Key == section.Key);
            if (!_views.TryGetValue(section.Key, out var view)) _views[section.Key] = view = new SectionView(this, section);
            view.Refresh(state, texts);
            placed.Add((view.Card, Math.Clamp(section.Span, 1, 12)));
        }
        _sections.SetItems(placed);

        _submit.Text = form.IsSubmitting ? texts.Saving : texts.Submit;
        _submit.IsEnabled = !form.IsSubmitting;
    }

    private void RefreshNav(IReadOnlyList<FormPageSectionState> states, FormTexts texts)
    {
        _nav.IsVisible = states.Count > 0;
        _navTitle.Text = texts.FormSections;
        SemanticProperties.SetDescription(_navList, texts.FormSections);
        var keys = states.Select(s => s.Key).ToList();
        if (!keys.SequenceEqual(_navKeys, StringComparer.Ordinal))
        {
            _navKeys = keys;
            _navList.Children.Clear();
            _navRows = [];
            foreach (var state in states)
            {
                var key = state.Key;
                var label = FormUi.NewLabel(12, KlyTheme.Text, FontAttributes.Bold, LineBreakMode.TailTruncation);
                label.VerticalOptions = LayoutOptions.Center;
                var badge = new KlyBadge { Variant = KlyVariant.Subtle, Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center };
                var grid = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
                grid.Add(label, 0);
                grid.Add(badge, 1);
                var row = new Border { StrokeThickness = 0, Padding = new Thickness(10, 8), BackgroundColor = Colors.Transparent, Content = grid, MinimumHeightRequest = 40 };
                row.SetRadius(KlyTheme.RadiusOf(1));
                row.AutomationId = Engine.IdPrefix + "nav-" + key;
                FormUi.Tap(row, () => GoTo(key));
                _navList.Add(row);
                _navRows.Add((row, label, badge, key));
            }
        }
        foreach (var (row, label, badge, key) in _navRows)
        {
            var state = states.First(s => s.Key == key);
            var (text, color) = state.Status switch
            {
                FormPageSectionStatus.Complete => (texts.SectionComplete, KlyColor.Success),
                FormPageSectionStatus.Invalid => (texts.SectionInvalid, KlyColor.Error),
                FormPageSectionStatus.Optional => (texts.SectionOptional, KlyColor.Neutral),
                _ => (texts.SectionProgress(state.RequiredCount - state.MissingCount, state.RequiredCount), KlyColor.Warning),
            };
            label.Text = state.Label;
            badge.Text = text;
            badge.Color = color;
            SemanticProperties.SetDescription(row, $"{state.Label}, {text}");
        }
    }

    /// <summary>Scrolls to a section, as a tap on its row in the navigation does.</summary>
    public void GoTo(string key)
    {
        if (_views.TryGetValue(key, out var view)) _ = FormUi.RevealAsync(view.Card);
    }

    private sealed class SectionView
    {
        private readonly FormPageShellView _owner;
        private readonly SectionNode _section;
        private readonly Label _title = FormUi.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
        private readonly Label _description = FormUi.NewLabel(12, KlyTheme.TextMuted);
        private readonly Border _line = new() { HeightRequest = 1, StrokeThickness = 0 };

        public SectionView(FormPageShellView owner, SectionNode section)
        {
            _owner = owner;
            _section = section;
            Fields = new FormNodeGrid(owner.Context);
            Reset = new KlyButton { Variant = ButtonVariant.Ghost, Size = KlySize.Xs, VerticalOptions = LayoutOptions.Start, IsVisible = false };
            Reset.Clicked += (_, _) => owner.Engine.ResetSection(section.Key);
            _title.Text = section.Label;
            SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level2);
            FormUi.Show(_description, section.Description);
            _line.SetThemeColor(BackgroundColorProperty, KlyTheme.Border);
            var head = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            head.Add(new VerticalStackLayout { Spacing = 2, Children = { _title, _description } }, 0);
            head.Add(Reset, 1);
            Card = new KlyCard { Padding = new Thickness(16), Content = new VerticalStackLayout { Spacing = 12, Children = { head, _line, Fields.Grid } } };
            Card.AutomationId = owner.Engine.IdPrefix + "section-" + section.Key;
        }

        public KlyCard Card { get; }

        public FormNodeGrid Fields { get; }

        public KlyButton Reset { get; }

        public void Refresh(FormPageSectionState state, FormTexts texts)
        {
            Card.SetStroke(state.Dirty ? KlyTheme.Resolve(KlyColor.Primary).Text : KlyTheme.Border);
            Reset.Text = texts.Reset;
            Reset.IsVisible = state.Dirty;
            Reset.IsEnabled = !_owner.Engine.IsSubmitting;
            Fields.Update(Fields.InEffect(_section.Fields));
        }
    }
}
