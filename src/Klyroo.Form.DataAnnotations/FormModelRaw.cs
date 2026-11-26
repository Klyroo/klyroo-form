namespace Klyroo.Form;

// Values of the controller, seen the way the attributes expect them.
internal static class FormModelRaw
{
    public static bool IsEmpty(object? raw) => raw is null || FormController.IsEmptyValue(raw);

    public static object? ToMember(object? raw, FormMemberInfo member) => FormValueMap.FromControl(raw, member.Type, nullableReference: false);

    public static string Text(object? raw) => raw is null ? "" : FormValueConverter.ToInvariantText(raw);
}
