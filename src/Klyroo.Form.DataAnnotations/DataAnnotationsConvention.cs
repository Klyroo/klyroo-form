using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Klyroo.Form;

// Maps the attributes of one member onto the settings of its field.
internal static class DataAnnotationsConvention
{
    // The attribute types whose IsValid needs nothing but the value, so they can run while the user types.
    private static readonly HashSet<Type> ContextFree =
    [
        typeof(EmailAddressAttribute), typeof(PhoneAttribute), typeof(UrlAttribute), typeof(RegularExpressionAttribute),
        typeof(MinLengthAttribute), typeof(MaxLengthAttribute), typeof(StringLengthAttribute), typeof(RangeAttribute),
        typeof(CreditCardAttribute), typeof(FileExtensionsAttribute), typeof(Base64StringAttribute), typeof(LengthAttribute),
        typeof(AllowedValuesAttribute), typeof(DeniedValuesAttribute),
    ];

    internal static bool IsContextFree(Type type) => ContextFree.Contains(type);

    public static int? Order(FormMemberInfo member) =>
        member.Property.GetCustomAttributes(typeof(DisplayAttribute), true).OfType<DisplayAttribute>().FirstOrDefault()?.GetOrder();

    public static void Apply(FormFieldConventionContext context)
    {
        var member = context.Member;
        var settings = context.Settings;
        var attributes = member.Property.GetCustomAttributes(true);
        var display = attributes.OfType<DisplayAttribute>().FirstOrDefault();
        var name = display?.GetName() ?? settings.Label ?? member.Name;
        if (display?.GetName() is { } label) settings.Label = label;
        if (display?.GetDescription() is { } description) settings.Description = description;
        if (display?.GetPrompt() is { } prompt) settings.Placeholder = prompt;

        if (attributes.OfType<EditableAttribute>().Any(a => !a.AllowEdit) || attributes.OfType<ReadOnlyAttribute>().Any(a => a.IsReadOnly))
        {
            settings.Disabled = true;
        }

        if (attributes.OfType<DataTypeAttribute>().FirstOrDefault() is { } dataType) ApplyDataType(member, dataType.DataType, settings);

        var rules = new List<ValidatorRule>();
        foreach (var attribute in attributes.OfType<ValidationAttribute>())
        {
            Map(member, settings, attribute, name, rules);
        }
        if (rules.Count > 0) settings.Validators = [.. settings.Validators ?? [], .. rules];
    }

    private static void ApplyDataType(FormMemberInfo member, DataType dataType, FieldSettings settings)
    {
        var isText = member.UnderlyingType == typeof(string);
        var isMoment = member.UnderlyingType == typeof(DateTime) || member.UnderlyingType == typeof(DateTimeOffset);
        switch (dataType)
        {
            case DataType.Password when isText:
                settings.Kind = FormFieldKind.Password;
                break;
            case DataType.MultilineText when isText:
                settings.Kind = FormFieldKind.Textarea;
                break;
            case DataType.PhoneNumber when isText:
                settings.Kind = FormFieldKind.PhoneNumber;
                break;
            case DataType.Date when isText || isMoment:
                settings.Kind = FormFieldKind.Date;
                break;
            case DataType.DateTime when isText || isMoment:
                settings.Kind = FormFieldKind.DateTime;
                break;
        }
    }

    private static void Map(FormMemberInfo member, FieldSettings settings, ValidationAttribute attribute, string display, List<ValidatorRule> rules)
    {
        var type = attribute.GetType();
        var custom = HasMessage(attribute);
        var isText = member.UnderlyingType == typeof(string);

        if (type == typeof(RequiredAttribute))
        {
            settings.Required = true;
            if (custom) settings.RequiredMessage = attribute.FormatErrorMessage(display);
        }
        else if (attribute is StringLengthAttribute { } length && type == typeof(StringLengthAttribute) && isText)
        {
            if (!custom) settings.MaxLength = length.MaximumLength;
            if (custom || length.MinimumLength > 0) rules.Add(Rule(member, attribute, display, "stringLength"));
        }
        else if (attribute is MaxLengthAttribute { } max && type == typeof(MaxLengthAttribute) && isText && max.Length >= 0)
        {
            if (custom) rules.Add(Rule(member, attribute, display, "maxLength"));
            else settings.MaxLength = max.Length;
        }
        else if (attribute is RangeAttribute range && type == typeof(RangeAttribute) && settings.Kind == FormFieldKind.Number && !custom
            && IsInclusive(range) && (range.OperandType == typeof(double) || range.OperandType == typeof(int)))
        {
            settings.Min = Convert.ToDouble(range.Minimum, CultureInfo.InvariantCulture);
            settings.Max = Convert.ToDouble(range.Maximum, CultureInfo.InvariantCulture);
        }
        else if (attribute is CompareAttribute compare && type == typeof(CompareAttribute))
        {
            var other = member.SiblingKey(compare.OtherProperty);
            settings.DependsOn = [.. settings.DependsOn ?? [], other];
            rules.Add(new ValidatorRule("compare", attribute.FormatErrorMessage(display), (raw, form) => SameText(raw, form.GetValue(other)))
            {
                LocalizedMessage = _ => attribute.FormatErrorMessage(DisplayName(member, display)),
            });
        }
        else if (ContextFree.Contains(type))
        {
            rules.Add(Rule(member, attribute, display, type.Name.Replace("Attribute", "", StringComparison.Ordinal)));
        }
    }

    private static bool IsInclusive(RangeAttribute range) => !range.MinimumIsExclusive && !range.MaximumIsExclusive;

    // A message the developer chose: the engine one is used only when there is none.
    internal static bool HasMessage(ValidationAttribute attribute) =>
        !string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceName is not null;

    // The attribute checks the value as the member type, and an empty value is Required's business, as for model binding in ASP.NET Core. The message is the one of
    // the attribute, worked out again when the rule fails (with the [Display] name of that moment) so a resource message follows the UI culture of the validation,
    // not the one that was current when the form was built. The text computed now is only the default.
    private static ValidatorRule Rule(FormMemberInfo member, ValidationAttribute attribute, string display, string key) =>
        new(key, attribute.FormatErrorMessage(display), (raw, _) =>
        {
            if (FormModelRaw.IsEmpty(raw)) return true;
            return attribute.IsValid(FormModelRaw.ToMember(raw, member));
        })
        {
            LocalizedMessage = _ => attribute.FormatErrorMessage(DisplayName(member, display)),
        };

    private static string DisplayName(FormMemberInfo member, string fallback) =>
        member.Property.GetCustomAttributes(typeof(DisplayAttribute), true).OfType<DisplayAttribute>().FirstOrDefault()?.GetName() ?? fallback;

    private static bool SameText(object? a, object? b) =>
        string.Equals(FormModelRaw.Text(a), FormModelRaw.Text(b), StringComparison.Ordinal);
}
