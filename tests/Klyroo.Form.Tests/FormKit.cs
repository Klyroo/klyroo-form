namespace Klyroo.Form.Tests;

// Reads the control layer of a typed form (what the engine stores, the nodes it renders) so a test can say what a control would do, such as typing "abc" in a number field.
internal static class FormKit
{
    // Element ids carry a number that is unique per form instance (kly-ff-3-email): tests compare them without it.
    public static string Unscoped(string? id) => System.Text.RegularExpressions.Regex.Replace(id ?? "", @"kly-ff-\d+-", "kly-ff-");

    public static FieldNode Node<TModel>(this FormSchema<TModel> schema, string key) =>
        schema.TryFindNode(key, out var node) ? node : throw new InvalidOperationException($"No node for '{key}'.");

    public static IReadOnlyList<FieldNode> Leaves<TModel>(this FormSchema<TModel> schema) =>
        schema.Definition.AllNodes().Where(n => !FormEngine.IsContainer(n.Kind) && !FormEngine.IsStateless(n.Kind)).ToList();

    public static IReadOnlyList<string> Keys<TModel>(this FormSchema<TModel> schema) => schema.Leaves().Select(n => n.Key).ToList();

    public static object? Raw<TModel>(this FormController<TModel> c, string key) => c.Engine.GetValue(key);

    public static void SetRaw<TModel>(this FormController<TModel> c, string key, object? value, bool markTouched = false) =>
        c.Engine.SetValue(key, value, markTouched);

    public static string? ErrorOf<TModel>(this FormController<TModel> c, string key) => c.Engine.GetError(key);

    public static IReadOnlyDictionary<string, object?> Output<TModel>(this FormController<TModel> c) => c.Engine.BuildOutput();

    public static TModel Write<TModel>(this FormSchema<TModel> schema, IReadOnlyDictionary<string, object?> values, TModel target)
    {
        schema.PlanWrite(values, target).Apply();
        return target;
    }
}
