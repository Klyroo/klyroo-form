namespace Klyroo.Form;

// The rows of a repeatable list of a typed form: the engine keeps them as dictionaries of control values by column key, and this turns them into items of the
// model and back with the bindings of the columns.
internal sealed class ListRows<TItem>
{
    private static readonly IReadOnlyDictionary<string, Func<object>> NoCreators = new Dictionary<string, Func<object>>();

    public List<IFieldBinding<TItem>> Columns { get; } = [];

    public Func<TItem>? Factory { get; set; }

    public Dictionary<string, object?> ToRow(TItem item)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var column in Columns) row[column.Key] = column.ReadControl(item);
        return row;
    }

    public bool TryCreate(IReadOnlyDictionary<string, object?> row, out TItem? item)
    {
        item = default;
        if (Factory is null) return false;
        var created = Factory();
        try
        {
            var plan = new FormWritePlan();
            foreach (var column in Columns)
            {
                if (row.TryGetValue(column.Key, out var raw)) column.Plan(created, raw, NoCreators, plan);
            }
            plan.Apply();
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        item = created;
        return true;
    }
}

internal sealed class ListCodec<TItem>(ListRows<TItem> rows) : FormCodec<IReadOnlyList<TItem>>
{
    public override FormFieldKind Kind => FormFieldKind.ArrayList;

    public override object? ToControl(IReadOnlyList<TItem>? value) =>
        value is null ? new List<Dictionary<string, object?>>() : value.Select(rows.ToRow).ToList();

    public override ControlValue TryFromControl(object? raw, out IReadOnlyList<TItem>? value)
    {
        value = [];
        if (raw is null) return ControlValue.Empty;
        if (raw is not List<Dictionary<string, object?>> list) return ControlValue.Invalid;
        var items = new List<TItem>();
        foreach (var row in list)
        {
            if (!rows.TryCreate(row, out var item) || item is null) return ControlValue.Invalid;
            items.Add(item);
        }
        value = items;
        return items.Count == 0 ? ControlValue.Empty : ControlValue.Value;
    }

    public override string? Format(IReadOnlyList<TItem>? value) => null;

    public override bool TryParse(string? text, out IReadOnlyList<TItem>? value)
    {
        value = [];
        return string.IsNullOrWhiteSpace(text);
    }
}

// How a collection member of a model is read as a list and written back: the member may be a List, an array, or any of the interfaces a List implements.
internal static class CollectionShape
{
    public static Func<IReadOnlyList<T>, object> Maker<T>(Type member, string name)
    {
        if (member == typeof(T[])) return items => items.ToArray();
        if (member == typeof(List<T>) || member == typeof(IList<T>) || member == typeof(ICollection<T>) || member == typeof(IReadOnlyList<T>)
            || member == typeof(IReadOnlyCollection<T>) || member == typeof(IEnumerable<T>))
        {
            return items => items.ToList();
        }
        throw new ArgumentException($"'{name}' is a {member.Name}: a list or multiple choice member must be a List<{typeof(T).Name}>, an array, or one of IList, ICollection, IReadOnlyList, IReadOnlyCollection and IEnumerable of it.");
    }
}
