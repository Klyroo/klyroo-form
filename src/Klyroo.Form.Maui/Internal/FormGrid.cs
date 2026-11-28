namespace Klyroo.Form.Maui;

/// <summary>
/// The column grid of the web form: <c>Columns</c> equal columns and every field spanning <c>Span</c> of them, placed in order, wrapping to a new row when the next field no longer fits.
/// Below <see cref="CompactWidth"/> every field takes the whole row, since two or three columns do not fit a phone.
/// </summary>
internal sealed class FormGrid : Grid
{
    private readonly List<(View View, int Span)> _items = [];
    private int _columns;
    private bool _compact;
    private double _compactWidth;

    public FormGrid(int columns, double compactWidth, double spacing = 16)
    {
        _columns = Math.Clamp(columns, 1, 12);
        _compactWidth = compactWidth;
        ColumnSpacing = spacing;
        RowSpacing = spacing;
        SizeChanged += (_, _) => UpdateCompact();
        Rebuild();
    }

    public int Columns => _columns;

    /// <summary>True while the grid is narrower than <see cref="CompactWidth"/>.</summary>
    public bool Compact => _compact;

    /// <summary>The width under which every field takes the whole row. Zero or less keeps the columns whatever the width.</summary>
    public double CompactWidth
    {
        get => _compactWidth;
        set
        {
            if (_compactWidth == value) return;
            _compactWidth = value;
            UpdateCompact();
        }
    }

    public IReadOnlyList<View> Views => [.. _items.Select(i => i.View)];

    /// <summary>The row and column of every field, for the tests.</summary>
    public IReadOnlyList<(View View, int Row, int Column, int ColumnSpan)> Placement =>
        [.. _items.Select(i => (i.View, GetRow((BindableObject)i.View), GetColumn((BindableObject)i.View), GetColumnSpan((BindableObject)i.View)))];

    /// <summary>Puts the views in the grid. Nothing is rebuilt when the same views come again with the same spans.</summary>
    public void SetItems(IReadOnlyList<(View View, int Span)> items)
    {
        if (items.Count == _items.Count && items.Zip(_items).All(p => ReferenceEquals(p.First.View, p.Second.View) && p.First.Span == p.Second.Span)) return;
        _items.Clear();
        _items.AddRange(items);
        Rebuild();
    }

    private void UpdateCompact()
    {
        var compact = _compactWidth > 0 && Width > 0 && Width < _compactWidth;
        if (compact == _compact) return;
        _compact = compact;
        Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();
        var columns = _compact ? 1 : _columns;
        for (var i = 0; i < columns; i++) ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var row = 0;
        var column = 0;
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        foreach (var (view, requested) in _items)
        {
            var span = Math.Clamp(_compact ? columns : requested, 1, columns);
            if (column + span > columns)
            {
                row++;
                column = 0;
                RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
            SetRow((BindableObject)view, row);
            SetColumn((BindableObject)view, column);
            SetColumnSpan((BindableObject)view, span);
            Children.Add(view);
            column += span;
            if (column >= columns)
            {
                row++;
                column = 0;
                RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
        }
    }
}
