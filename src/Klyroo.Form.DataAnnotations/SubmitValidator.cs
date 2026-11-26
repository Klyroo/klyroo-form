using System.ComponentModel.DataAnnotations;

namespace Klyroo.Form;

// What data annotations cannot do while the user types, done at submit on the model built from the values: attributes that need the
// model (a ValidationContext), attributes of the class, and IValidatableObject. The built-in attributes the field rules cover are skipped.
internal sealed class SubmitValidator<TModel>(List<FormMemberInfo> members)
{
    public void Validate(TModel model, FormModelErrors<TModel> errors)
    {
        ArgumentNullException.ThrowIfNull(model);
        foreach (var member in members)
        {
            if (member.ModelType != typeof(TModel) || !errors.IsActive(member.Key)) continue;
            var holder = Holder(model, member);
            if (holder is null) continue;
            var value = member.Property.GetValue(holder);
            var display = member.Property.GetCustomAttributes(typeof(DisplayAttribute), true).OfType<DisplayAttribute>().FirstOrDefault()?.GetName() ?? member.Name;
            foreach (var attribute in member.Property.GetCustomAttributes(true).OfType<ValidationAttribute>())
            {
                if (IsCovered(attribute)) continue;
                var context = new ValidationContext(holder, display, null, null) { MemberName = member.Name };
                if (attribute.GetValidationResult(value, context) is { } result)
                {
                    errors.Add(member.Key, result.ErrorMessage ?? attribute.FormatErrorMessage(display));
                }
            }
        }

        var modelContext = new ValidationContext(model, typeof(TModel).Name, null, null);
        foreach (var attribute in typeof(TModel).GetCustomAttributes(true).OfType<ValidationAttribute>())
        {
            if (attribute.GetValidationResult(model, modelContext) is { } result) Report(result, errors);
        }
        if (model is IValidatableObject validatable)
        {
            foreach (var result in validatable.Validate(modelContext))
            {
                if (result != ValidationResult.Success) Report(result, errors);
            }
        }
    }

    private static void Report(ValidationResult result, FormModelErrors<TModel> errors)
    {
        var message = result.ErrorMessage ?? "The form is not valid";
        var names = result.MemberNames.Where(n => !string.IsNullOrEmpty(n)).ToList();
        if (names.Count == 0) errors.Add(message);
        foreach (var name in names) errors.Add(name, message);
    }

    // The object that holds the member: the model, or the nested object on the way to it (null when that object is null).
    private static object? Holder(object model, FormMemberInfo member)
    {
        object? current = model;
        for (var i = 0; i < member.Path.Count - 1 && current is not null; i++) current = member.Path[i].GetValue(current);
        return current;
    }

    // Exactly the attributes the convention turned into field rules: they already ran.
    private static bool IsCovered(ValidationAttribute attribute)
    {
        var type = attribute.GetType();
        return type == typeof(RequiredAttribute) || type == typeof(CompareAttribute) || DataAnnotationsConvention.IsContextFree(type);
    }
}
