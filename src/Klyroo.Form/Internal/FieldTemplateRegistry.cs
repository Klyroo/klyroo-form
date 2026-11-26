using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;

namespace Klyroo.Form.Internal;

// The KlyFieldTemplate components of a form, by field key. They register with the controller they sit under, and the renderer of a field looks its key up when it draws
// the control, so a template works whether it is a child of KlyFormEngine<TModel> or of a layout built with KlyFormFieldView<TModel>.
internal sealed class FieldTemplateRegistry
{
    private static readonly ConditionalWeakTable<FormEngine, FieldTemplateRegistry> Registries = new();

    private readonly Dictionary<string, Entry> _items = new(StringComparer.Ordinal);

    private sealed record Entry(object Owner, Type ValueType, Func<FormControlContext, RenderFragment> Render);

    public static FieldTemplateRegistry For(FormEngine engine) => Registries.GetValue(engine, _ => new FieldTemplateRegistry());

    public void Set(string key, object owner, Type valueType, Func<FormControlContext, RenderFragment> render)
    {
        lock (_items) _items[key] = new Entry(owner, valueType, render);
    }

    public void Remove(string key, object owner)
    {
        lock (_items)
        {
            if (_items.TryGetValue(key, out var entry) && ReferenceEquals(entry.Owner, owner)) _items.Remove(key);
        }
    }

    // A template made for another value type than the field has is not used: it would read the control as the wrong type.
    public bool TryGet(string key, Type? valueType, out Func<FormControlContext, RenderFragment> render)
    {
        lock (_items)
        {
            if (_items.TryGetValue(key, out var entry) && entry.ValueType == valueType)
            {
                render = entry.Render;
                return true;
            }
        }
        render = null!;
        return false;
    }
}
