using System.Collections.Concurrent;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form.Maui;

/// <summary>
/// The renderers of <see cref="FormFieldKind.Custom"/> fields that every form of the app shares: the native counterpart of <c>KlyrooFormOptions.CustomField</c>. A renderer builds the view of the control
/// from the context of the field; the form views draw the label, description, help and error around it. A <see cref="KlyFieldTemplate{TModel, TValue}"/> of a field and the <c>FieldTemplates</c> of a dynamic form win over them.
/// Register at startup, before the first form is shown.
/// </summary>
public static class FormCustomFields
{
    private static readonly ConcurrentDictionary<(string Key, Type? ValueType), object> Renderers = new();

    /// <summary>Registers the renderer of the custom fields of dynamic forms whose <see cref="DynamicFieldSchema.CustomKey"/> is <paramref name="key"/>. Registering the key again replaces its renderer.</summary>
    /// <param name="key">The key a field names in <see cref="DynamicFieldSchema.CustomKey"/>. Keys are case sensitive.</param>
    /// <param name="renderer">Builds the control from the context of the field.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="renderer"/> is <c>null</c>.</exception>
    public static void Register(string key, Func<DynamicControlContext, View> renderer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(renderer);
        Renderers[(key, null)] = renderer;
    }

    /// <summary>
    /// Registers the renderer of the custom fields of typed forms declared with <c>AsCustom(key)</c> whose value is a <typeparamref name="TValue"/>. A field of another value type that names the key shows the
    /// "no renderer" notice instead of a control that would read the wrong type. Registering the same key and type again replaces its renderer.
    /// </summary>
    /// <typeparam name="TValue">The type of the value of the fields the renderer draws.</typeparam>
    /// <param name="key">The key a field names in <c>AsCustom</c>. Keys are case sensitive.</param>
    /// <param name="renderer">Builds the control from the context of the field.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="renderer"/> is <c>null</c>.</exception>
    public static void Register<TValue>(string key, Func<FormControlContext<TValue>, View> renderer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(renderer);
        Func<FormControlContext, View> typed = context => renderer((FormControlContext<TValue>)context);
        Renderers[(key, typeof(TValue))] = typed;
    }

    /// <summary>Removes every registered renderer.</summary>
    public static void Clear() => Renderers.Clear();

    internal static bool TryGet(string key, out Func<DynamicControlContext, View> renderer)
    {
        renderer = null!;
        if (!Renderers.TryGetValue((key, null), out var found) || found is not Func<DynamicControlContext, View> dynamic) return false;
        renderer = dynamic;
        return true;
    }

    internal static bool TryGet(string key, Type valueType, out Func<FormControlContext, View> renderer)
    {
        renderer = null!;
        if (!Renderers.TryGetValue((key, valueType), out var found) || found is not Func<FormControlContext, View> typed) return false;
        renderer = typed;
        return true;
    }
}
