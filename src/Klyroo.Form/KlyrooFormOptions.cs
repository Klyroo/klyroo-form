using Klyroo.Form.Dynamic;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;

namespace Klyroo.Form;

/// <summary>
/// Configuration of Klyroo.Form: the built-in texts of each locale. It is an extension of <see cref="KlyrooOptions"/>
/// (<see cref="KlyrooOptions.Extension{T}"/>). Configure it with <c>services.AddKlyrooForm(f =&gt; ...)</c>, in code shared by the
/// server and the WebAssembly client.
/// </summary>
public sealed class KlyrooFormOptions
{
    /// <summary>Creates the options with the built-in texts of <see cref="FormLocales"/>.</summary>
    public KlyrooFormOptions() { }

    private readonly object _gate = new();
    private readonly Dictionary<string, List<Func<FormTexts, FormTexts>>> _changes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, FormTexts> _resolved = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _frozen;

    /// <summary>
    /// Changes the texts of a locale, like <see cref="KlyLocales.Extend"/> does for Klyroo.UI: <paramref name="change"/> receives the set the
    /// locale resolves to and returns the one to use, usually <c>t =&gt; t with { ... }</c>. <paramref name="localeCode"/> is a code of
    /// <see cref="KlyLocales.All"/> ("fr"), or the code of a locale you added ("en-AU", "tlh"): a locale without a set of its own starts from
    /// the set <see cref="KlyLocales.ResolveCode"/> picks for it, then its changes apply. The built-in validation messages are part of the set (<see cref="FormTexts.Messages"/>).
    /// The texts become read-only the first time a component resolves one.
    /// </summary>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="InvalidOperationException">A component has already resolved a text.</exception>
    public KlyrooFormOptions Texts(string localeCode, Func<FormTexts, FormTexts> change)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localeCode);
        ArgumentNullException.ThrowIfNull(change);
        lock (_gate)
        {
            if (_frozen)
            {
                throw new InvalidOperationException($"{nameof(KlyrooFormOptions)}.{nameof(Texts)} cannot change once a component has resolved a text: the result of each locale is cached. Set it in AddKlyrooForm, before the app starts rendering.");
            }
            if (!_changes.TryGetValue(localeCode, out var list))
            {
                _changes[localeCode] = list = [];
            }
            list.Add(change);
        }
        return this;
    }

    private readonly ConcurrentDictionary<(string Key, Type? ValueType), CustomRenderer> _customFields = new();

    // A renderer registered for a custom field, kept by key and value type: ValueType is null for a dynamic form, else the value type of the typed fields it draws.
    private sealed record CustomRenderer(Type? ValueType, RenderFragment<DynamicControlContext>? Dynamic, Func<FormControlContext, RenderFragment>? Typed);

    /// <summary>
    /// Registers the renderer of <see cref="FormFieldKind.Custom"/> fields of dynamic forms whose <see cref="DynamicFieldSchema.CustomKey"/> is <paramref name="key"/>, for every form of the app. The renderer draws the control from a
    /// <see cref="DynamicControlContext"/>; the components draw the label, description, help and error around it. The <c>FieldTemplates</c> parameter of a component wins over it for that component. Registering a
    /// key again replaces its dynamic renderer; typed renderers under the same key (<see cref="CustomField{TValue}"/>) are separate.
    /// </summary>
    /// <param name="key">The key a field names in <see cref="DynamicFieldSchema.CustomKey"/>. Keys are case sensitive.</param>
    /// <param name="renderer">Draws the control.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="renderer"/> is <c>null</c>.</exception>
    public KlyrooFormOptions CustomField(string key, RenderFragment<DynamicControlContext> renderer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(renderer);
        _customFields[(key, null)] = new CustomRenderer(null, renderer, null);
        return this;
    }

    /// <summary>
    /// Registers the renderer of the custom fields of typed forms declared with <c>AsCustom(<paramref name="key"/>)</c> whose value is a <typeparamref name="TValue"/>, for every form of the app. The renderer draws the control from a
    /// <see cref="FormControlContext{TValue}"/>; the components draw the label, description, help and error around it. A <c>KlyFieldTemplate</c> of a field wins over it. A field of another value type than
    /// <typeparamref name="TValue"/> that names the key shows the "no renderer" notice instead of a control that would read the wrong type, unless a renderer was registered for its type under the same key: renderers are kept by key and value type, and
    /// the one of a dynamic form (<see cref="CustomField(string, RenderFragment{DynamicControlContext})"/>) is separate too. Registering the same key and type again replaces its renderer.
    /// </summary>
    /// <typeparam name="TValue">The type of the value of the fields the renderer draws.</typeparam>
    /// <param name="key">The key a field names in <c>AsCustom</c>. Keys are case sensitive.</param>
    /// <param name="renderer">Draws the control.</param>
    /// <returns>The same options, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="renderer"/> is <c>null</c>.</exception>
    public KlyrooFormOptions CustomField<TValue>(string key, RenderFragment<FormControlContext<TValue>> renderer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(renderer);
        _customFields[(key, typeof(TValue))] = new CustomRenderer(typeof(TValue), null, context => renderer((FormControlContext<TValue>)context));
        return this;
    }

    internal bool TryGetCustomField(string key, out RenderFragment<DynamicControlContext> renderer)
    {
        renderer = null!;
        if (!_customFields.TryGetValue((key, null), out var found) || found.Dynamic is not { } dynamic) return false;
        renderer = dynamic;
        return true;
    }

    internal bool TryGetCustomField(string key, Type valueType, out Func<FormControlContext, RenderFragment> renderer)
    {
        renderer = null!;
        if (!_customFields.TryGetValue((key, valueType), out var found) || found.Typed is not { } typed) return false;
        renderer = typed;
        return true;
    }

    /// <summary>The texts for a locale code (<see cref="KlyLocale.Code"/>), cached per code. The first call makes <see cref="Texts"/> read-only.</summary>
    internal FormTexts ResolveTexts(string localeCode)
    {
        // Read on every render: take the lock only the first time.
        if (!_frozen)
        {
            lock (_gate) _frozen = true;
        }
        return _resolved.GetOrAdd(localeCode, static (code, self) => self.Compute(code), this);
    }

    private FormTexts Compute(string localeCode)
    {
        var tables = FormLocales.Tables;
        var tableCode = KlyLocales.ResolveCode(localeCode, tables.Keys);
        var texts = tables[tableCode];
        foreach (var key in new[] { tableCode, localeCode }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_changes.TryGetValue(key, out var changes))
            {
                foreach (var change in changes) texts = change(texts) ?? throw new InvalidOperationException($"A change of the texts of '{key}' returned null.");
            }
        }
        return texts;
    }
}
