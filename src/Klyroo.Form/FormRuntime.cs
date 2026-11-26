namespace Klyroo.Form;

// Reaches the engine of the controllers of Klyroo.Form from the public IFormRuntime the components take.
internal static class FormRuntime
{
    public static FormEngine EngineOf(IFormRuntime form) =>
        form is IEngineOwner owner
            ? owner.Engine
            : throw new ArgumentException($"{form.GetType().Name} is not a controller of Klyroo.Form: pass a FormController<TModel> or a DynamicFormController.", nameof(form));
}

internal static class InternalParameters
{
    // The internal parameters of the shared components arrive as unmatched attributes (Blazor needs public types for real parameters), so the attributes the
    // component splats on its element must not carry them.
    public static IReadOnlyDictionary<string, object>? Without(IReadOnlyDictionary<string, object>? attributes, params string[] names)
    {
        if (attributes is null || attributes.Count == 0) return attributes;
        var kept = attributes.Where(a => Array.IndexOf(names, a.Key) < 0).ToDictionary(a => a.Key, a => a.Value);
        return kept.Count == 0 ? null : kept;
    }
}
