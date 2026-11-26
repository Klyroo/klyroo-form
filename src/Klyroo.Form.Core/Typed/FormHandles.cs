namespace Klyroo.Form;

/// <summary>An element of a typed form that a condition can point at: a field, or a layout element such as a group, a card, a section heading or an info block. It is what <c>DependsOn</c> takes, and what <see cref="FormSchemaBuilder{TModel}"/> methods return.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public class FormItem<TModel>
{
    private FieldNode? _node;

    internal FormItem(string key) => Key = key;

    /// <summary>The key of the element in the form: the names of the members from the model down to it, joined with dots (<c>Address.City</c>) for a field, or the key the element was declared with for a layout element.</summary>
    public string Key { get; }

    // The node the schema built for this element. It exists once the schema is built.
    internal FieldNode Node
    {
        get => _node ?? throw new InvalidOperationException($"'{Key}' belongs to a schema that is still being declared.");
        set => _node = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Key;
}

// The codec of a field, known once its kind is: rule closures read it when they run.
internal interface ICodecSource<TValue>
{
    FormCodec<TValue> Codec { get; }
}

/// <summary>A field of a typed form: a handle on one member of the model, with the type of its value. Declare it with <c>Field</c>, then read and write it with <see cref="FormController{TModel}"/>. A handle is immutable and can be used by every controller of the schema.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="TValue">The type of the value of the field.</typeparam>
public class FormField<TModel, TValue> : FormItem<TModel>, ICodecSource<TValue>
{
    private FormCodec<TValue>? _codec;

    internal FormField(string key)
        : base(key)
    {
    }

    internal FormCodec<TValue> Codec
    {
        get => _codec ?? throw new InvalidOperationException($"'{Key}' belongs to a schema that is still being declared.");
        set => _codec = value;
    }

    FormCodec<TValue> ICodecSource<TValue>.Codec => Codec;

    // The binding that moves the value between the model and the controller.
    internal IFieldBinding<TModel>? Binding { get; set; }
}

/// <summary>A repeatable list of rows of a typed form: a handle on a collection member of the model. A row is a <typeparamref name="TItem"/>; its columns are the fields declared in the <c>List</c> call, as <see cref="FormField{TModel, TValue}"/> of the item.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
/// <typeparam name="TItem">The type of a row.</typeparam>
public sealed class FormListField<TModel, TItem> : FormField<TModel, IReadOnlyList<TItem>>
{
    internal FormListField(string key)
        : base(key)
    {
    }

    internal ListRows<TItem> Rows { get; set; } = default!;
}

/// <summary>One choice of a choice field of a typed form: the value stored in the model, and the text shown to the user.</summary>
/// <typeparam name="TValue">The type of the value of the field.</typeparam>
/// <param name="Value">The value stored in the model when the option is selected.</param>
/// <param name="Label">The text shown to the user.</param>
/// <param name="Description">Optional secondary text, shown under the label in radio and checkbox lists.</param>
/// <param name="Group">Optional group name: options with the same group are rendered together.</param>
/// <param name="Icon">Optional icon name, shown before the label in radio and checkbox lists.</param>
/// <param name="Disabled"><c>true</c> to show the option but prevent selecting it.</param>
public sealed record FormOption<TValue>(
    TValue Value,
    string Label,
    string? Description = null,
    string? Group = null,
    string? Icon = null,
    bool Disabled = false);

/// <summary>A row of a repeatable list of a typed form, read and written through a <see cref="FormController{TModel}"/>. Its columns are the fields of the item type declared in the <c>List</c> call.</summary>
/// <typeparam name="TItem">The type of a row.</typeparam>
public sealed class FormRow<TItem>
{
    private readonly FormEngine _engine;
    private readonly string _listKey;

    internal FormRow(FormEngine engine, string listKey, int index)
    {
        _engine = engine;
        _listKey = listKey;
        Index = index;
    }

    /// <summary>The zero-based position of the row in the list.</summary>
    public int Index { get; }

    private IDictionary<string, object?>? Cells => Index >= 0 && Index < _engine.GetArrayItems(_listKey).Count ? _engine.GetArrayItems(_listKey)[Index] : null;

    /// <summary>Reads the value of a column of this row.</summary>
    /// <typeparam name="TValue">The type of the value of the column.</typeparam>
    /// <param name="column">The column, as declared in the <c>List</c> call.</param>
    /// <returns>The value, or the default of the type when the cell is empty or its text is not a valid value. Use <see cref="TryGet{TValue}"/> to tell them apart.</returns>
    public TValue? Get<TValue>(FormField<TItem, TValue> column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return Cells is { } cells && cells.TryGetValue(column.Key, out var raw) ? column.Codec.FromControlOrDefault(raw) : default;
    }

    /// <summary>Tells whether a column of this row has a value that is a valid <typeparamref name="TValue"/>.</summary>
    /// <typeparam name="TValue">The type of the value of the column.</typeparam>
    /// <param name="column">The column.</param>
    /// <param name="value">The value, or the default of the type when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> when the cell holds a valid value.</returns>
    public bool TryGet<TValue>(FormField<TItem, TValue> column, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(column);
        var raw = Cells is { } cells && cells.TryGetValue(column.Key, out var found) ? found : null;
        return column.Codec.TryFromControl(raw, out value) == ControlValue.Value;
    }

    /// <summary>Sets the value of a column of this row, revalidating the cell when it was touched.</summary>
    /// <typeparam name="TValue">The type of the value of the column.</typeparam>
    /// <param name="column">The column.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to make the error of the cell visible.</param>
    public void Set<TValue>(FormField<TItem, TValue> column, TValue value, bool markTouched = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        _engine.SetArrayItemValue(_listKey, Index, column.Key, column.Codec.ToControl(value), markTouched);
    }

    /// <summary>Gets the error to show for a column of this row.</summary>
    /// <typeparam name="TValue">The type of the value of the column.</typeparam>
    /// <param name="column">The column.</param>
    /// <returns>The message, or <c>null</c> when there is none or the cell has not been touched.</returns>
    public string? GetError<TValue>(FormField<TItem, TValue> column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return _engine.GetArrayItemError(_listKey, Index, column.Key);
    }
}
