using System.Reflection;

namespace Klyroo.Form;

// Whether a string member accepts null, which decides what an empty input becomes: null, or an empty string.
internal static class FormNullability
{
    private const string FeatureSwitch = "System.Reflection.NullabilityInfoContext.IsSupported";

    // The builder's choice wins. Otherwise the nullable annotation decides. A trimmed app may have the feature switch off, which removes the annotations
    // (the context then reports Unknown, or throws): the answer is then "not nullable", because an empty string is valid for any string member while a null
    // is not, so the form never stores a null where the declaration does not allow one.
    public static bool IsNullableReference(PropertyInfo property, bool? nullWhenEmpty) =>
        IsNullableReference(property, nullWhenEmpty, !AppContext.TryGetSwitch(FeatureSwitch, out var supported) || supported, ReadWriteState);

    public static bool IsNullableReference(PropertyInfo property, bool? nullWhenEmpty, bool supported, Func<PropertyInfo, NullabilityState?> read)
    {
        if (nullWhenEmpty is { } chosen) return chosen;
        if (!supported) return false;
        try
        {
            return read(property) == NullabilityState.Nullable;
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }

    private static NullabilityState? ReadWriteState(PropertyInfo property) => new NullabilityInfoContext().Create(property).WriteState;
}
