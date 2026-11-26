using System.Reflection;
using System.Runtime.CompilerServices;

namespace Klyroo.Form;

// The item a row of a repeatable list was loaded from, kept beside the dictionary the engine stores for the row. It is not a value of the row: it is left out of
// the draft, the dirty check and the JSON, and a copy of the row (a snapshot, a draft restore of other rows) carries it only through Carry.
internal static class RowOrigin
{
    private static readonly ConditionalWeakTable<Dictionary<string, object?>, object> Origins = new();

    public static void Set(Dictionary<string, object?> row, object item) => Origins.AddOrUpdate(row, item);

    public static object? Get(IReadOnlyDictionary<string, object?> row) =>
        row is Dictionary<string, object?> dictionary && Origins.TryGetValue(dictionary, out var item) ? item : null;

    public static Dictionary<string, object?> Carry(Dictionary<string, object?> from, Dictionary<string, object?> to)
    {
        if (Origins.TryGetValue(from, out var item)) Origins.AddOrUpdate(to, item);
        return to;
    }
}

// A collection member of a model (a list of rows, or the values of a multiple choice), read as a list and written back in place when the collection can be
// changed in place, and through the setter otherwise. The write is planned like any other: nothing is touched before every value is known to convert, and the
// whole write can be undone.
internal abstract class CollectionBinding<TModel, THolder, TElement> : IFieldBinding<TModel>
{
    private readonly HolderPath<TModel, THolder> _holder;
    private readonly Func<THolder, IEnumerable<TElement>?> _get;
    private readonly Func<IReadOnlyList<TElement>, object> _make;
    private readonly PropertyInfo _property;

    protected CollectionBinding(HolderPath<TModel, THolder> holder, FormMemberInfo member, Func<IReadOnlyList<TElement>, object> make, IEqualityComparer<TElement> comparer)
    {
        _holder = holder;
        Member = member;
        _make = make;
        Comparer = comparer;
        _property = member.Property;
        _get = Accessors.Getter<THolder, IEnumerable<TElement>?>(_property);
        CanSet = _property.SetMethod is { IsPublic: true };
    }

    protected IEqualityComparer<TElement> Comparer { get; }

    private bool CanSet { get; }

    public string Key => Member.Key;

    public FormMemberInfo Member { get; }

    // A member that cannot be set is accepted only when its type lets the collection be changed in place, which the builder checks.
    public bool CanWrite => true;

    public abstract object? ReadControl(TModel model);

    // The items of the member now (none when it is null), or null when an object on the way to it is null.
    protected List<TElement>? ReadItems(TModel model)
    {
        var holder = _holder.Read(model);
        return holder is null ? null : _get(holder)?.ToList() ?? [];
    }

    // The items the control stands for, given the items the member has now. It plans the writes the items themselves need.
    protected abstract IReadOnlyList<TElement> Desired(object? raw, IReadOnlyList<TElement> current, FormWritePlan plan);

    public void Plan(TModel model, object? raw, IReadOnlyDictionary<string, Func<object>> creators, FormWritePlan plan)
    {
        var holder = _holder.Resolve(model, plan);
        var existing = _get(holder);
        var current = existing?.ToList();
        var desired = Desired(raw, current ?? [], plan);
        if (current is not null && current.Count == desired.Count && current.Zip(desired).All(pair => Comparer.Equals(pair.First, pair.Second))) return;
        if (existing is ICollection<TElement> { IsReadOnly: false } collection and not Array)
        {
            var comparer = Comparer;
            plan.Add<ICollection<TElement>, IReadOnlyList<TElement>>(collection, (c, items) => Sync(c, items, comparer), desired, current!);
        }
        else if (CanSet)
        {
            var property = _property;
            plan.Add<THolder, object?>(holder, (h, v) => property.SetValue(h, v), _make(desired), existing);
        }
        else
        {
            throw new InvalidOperationException($"'{Key}' has no public setter and its value cannot be changed in place ({(existing is null ? "it is null" : "it is read-only")}): give it a setter, or a collection that can be changed.");
        }
    }

    // Makes the collection hold the items, keeping the position of the ones that are already there.
    private static void Sync(ICollection<TElement> collection, IReadOnlyList<TElement> items, IEqualityComparer<TElement> comparer)
    {
        if (collection is IList<TElement> list)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (i >= list.Count) list.Add(items[i]);
                else if (!comparer.Equals(list[i], items[i])) list[i] = items[i];
            }
            while (list.Count > items.Count) list.RemoveAt(list.Count - 1);
            return;
        }
        collection.Clear();
        foreach (var item in items) collection.Add(item);
    }
}

// A multiple choice: the control holds the option values as text.
internal sealed class MultiBinding<TModel, THolder, TElement>(
    HolderPath<TModel, THolder> holder,
    FormMemberInfo member,
    FormCodec<IReadOnlyList<TElement>> codec,
    Func<IReadOnlyList<TElement>, object> make)
    : CollectionBinding<TModel, THolder, TElement>(holder, member, make, EqualityComparer<TElement>.Default)
{
    public override object? ReadControl(TModel model) => ReadItems(model) is { } items ? codec.ToControl(items) : null;

    protected override IReadOnlyList<TElement> Desired(object? raw, IReadOnlyList<TElement> current, FormWritePlan plan)
    {
        if (codec.TryFromControl(raw, out var items) == ControlValue.Invalid)
        {
            throw new InvalidOperationException($"The value of '{Key}' cannot be converted to a list of {typeof(TElement).Name}.");
        }
        return items ?? [];
    }
}

// A repeatable list: a row is the item it was loaded from with its columns written into it, or a new item for a row that was added.
internal sealed class RowsBinding<TModel, THolder, TItem>(
    HolderPath<TModel, THolder> holder,
    FormMemberInfo member,
    ListRows<TItem> rows,
    ListCodec<TItem> codec,
    Func<IReadOnlyList<TItem>, object> make)
    : CollectionBinding<TModel, THolder, TItem>(holder, member, make, ReferenceComparer.Instance)
    where TItem : class
{
    private static readonly IReadOnlyDictionary<string, Func<object>> NoCreators = new Dictionary<string, Func<object>>();

    public override object? ReadControl(TModel model) => ReadItems(model) is { } items ? codec.ToControl(items) : null;

    protected override IReadOnlyList<TItem> Desired(object? raw, IReadOnlyList<TItem> current, FormWritePlan plan)
    {
        if (raw is not null and not List<Dictionary<string, object?>>)
        {
            throw new InvalidOperationException($"The value of '{Key}' cannot be converted to a list of {typeof(TItem).Name}.");
        }
        var source = raw as List<Dictionary<string, object?>> ?? [];
        // Only an item the collection holds now is written into: a row that came from another model gets a new item, so two models never share a row.
        var present = new HashSet<TItem>(current, ReferenceComparer.Instance);
        var items = new List<TItem>(source.Count);
        foreach (var row in source)
        {
            if (RowOrigin.Get(row) is not TItem item || !present.Remove(item))
            {
                item = rows.Factory?.Invoke() ?? throw new InvalidOperationException(
                    $"'{Key}' has a new row and {typeof(TItem).Name} has no parameterless constructor: give itemFactory to List(...) to say how a row is created.");
            }
            foreach (var column in rows.Columns)
            {
                if (row.TryGetValue(column.Key, out var cell)) column.Plan(item, cell, NoCreators, plan);
            }
            items.Add(item);
        }
        plan.Lists.Add((Key, items));
        return items;
    }

    private sealed class ReferenceComparer : IEqualityComparer<TItem>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(TItem? x, TItem? y) => ReferenceEquals(x, y);

        public int GetHashCode(TItem obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
