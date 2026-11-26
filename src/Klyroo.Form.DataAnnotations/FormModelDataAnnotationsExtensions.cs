using System.Diagnostics.CodeAnalysis;

namespace Klyroo.Form;

/// <summary>Reads the <c>System.ComponentModel.DataAnnotations</c> attributes of a model into a form declared with <see cref="FormModel.For{TModel}(Action{FormModelBuilder{TModel}})"/>.</summary>
public static class FormModelDataAnnotationsExtensions
{
    private const DynamicallyAccessedMemberTypes ModelMembers =
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

    /// <summary>
    /// Applies the data annotations of the members that have a field, and checks the rest at submit.
    /// <list type="bullet">
    /// <item><c>[Display]</c>: <c>Name</c> is the label, <c>Description</c> the description, <c>Prompt</c> the placeholder and <c>Order</c> the position.</item>
    /// <item><c>[Editable(false)]</c> and <c>[ReadOnly(true)]</c> disable the field. <c>[DataType]</c> picks the kind for <c>Password</c>, <c>MultilineText</c>, <c>PhoneNumber</c>, <c>Date</c> and <c>DateTime</c>.</item>
    /// <item><c>[Required]</c> is <see cref="FormFieldSchema.Required"/>, <c>[StringLength]</c> and <c>[MaxLength]</c> on a string are <see cref="FormFieldSchema.MaxLength"/>, and <c>[Range]</c> with <see cref="int"/> or <see cref="double"/> inclusive bounds on a number is <see cref="FormFieldSchema.Min"/> and <see cref="FormFieldSchema.Max"/>, so the engine checks them with its own, localized messages.</item>
    /// <item>A <c>[StringLength]</c> with a <c>MinimumLength</c> also gets a <see cref="FormValidatorRule"/> for the minimum. A <c>[Range]</c> with <c>typeof(decimal)</c> or text bounds, an exclusive bound, or on a field that is not a number is a <see cref="FormValidatorRule"/> only.</item>
    /// <item><c>[MinLength]</c>, <c>[RegularExpression]</c>, <c>[EmailAddress]</c>, <c>[Phone]</c>, <c>[Url]</c>, <c>[CreditCard]</c>, <c>[Compare]</c> and the other built-in attributes the engine has no rule for become a <see cref="FormValidatorRule"/> that runs while the user edits, with the message of the attribute: English unless the attribute has an <c>ErrorMessage</c> or a resource. That message and the <c>[Display]</c> name in it are worked out when the rule fails, so they follow the UI culture of the validation.</item>
    /// <item>An <c>ErrorMessage</c> (or a resource message) that the developer set replaces the engine message on <c>[Required]</c> (fixed when the form is built, like the label, description and placeholder), and turns <c>[StringLength]</c>, <c>[MaxLength]</c> and <c>[Range]</c> into validator rules that use it.</item>
    /// <item>Any other <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> (your own, or a subclass of a built-in one), attributes on the model class and <see cref="System.ComponentModel.DataAnnotations.IValidatableObject"/> run at submit, on the model built from the values, after the field rules passed. Their errors go to the field of the member they name, or to the form.</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="builder">The builder of the form.</param>
    /// <returns>The builder.</returns>
    public static FormModelBuilder<TModel> UseDataAnnotations<[DynamicallyAccessedMembers(ModelMembers)] TModel>(this FormModelBuilder<TModel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var members = new List<FormMemberInfo>();
        builder.Convention((member, settings) =>
        {
            members.Add(member);
            DataAnnotationsConvention.Apply(member, settings);
        });
        builder.Order(DataAnnotationsConvention.Order);
        builder.Validate(new SubmitValidator<TModel>(members).Validate);
        return builder;
    }

    /// <summary>Declares a field for every public readable and writable member of the model (see <see cref="FormModelBuilder{TModel}.AllMembers"/>) and reads their data annotations (see <see cref="UseDataAnnotations{TModel}"/>). It is the whole declaration of a form that is described by attributes: fields are sorted by <c>[Display(Order = ...)]</c> and then by declaration.</summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="builder">The builder of the form.</param>
    /// <returns>The builder.</returns>
    public static FormModelBuilder<TModel> FromDataAnnotations<[DynamicallyAccessedMembers(ModelMembers)] TModel>(this FormModelBuilder<TModel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AllMembers().UseDataAnnotations();
    }
}
