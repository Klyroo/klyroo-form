using System.Numerics;

namespace Klyroo.Form;

/// <summary>The settings of a typed field that only fit some kinds of value. Each method is constrained on the type of the value, so declaring a text area for a number, or a range for a string, does not compile.</summary>
public static class FormFieldBuilderExtensions
{
    /// <summary>Renders a text field as a multi-line text area.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TString">The type of the value of the field: a <see cref="string"/>, nullable or not.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TString> AsTextarea<TModel, TString>(this FormFieldBuilder<TModel, TString> field)
        where TString : class?, IComparable<string>?, IEquatable<string>?
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Textarea);
    }

    /// <summary>Renders a text field as a masked password input with a show/hide toggle.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TString">The type of the value of the field: a <see cref="string"/>, nullable or not.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="requirements">Rules the password must satisfy, checked in order on validation and shown as a checklist under the field. The first failing rule becomes the error.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TString> AsPassword<TModel, TString>(this FormFieldBuilder<TModel, TString> field, params FormPasswordRequirement[] requirements)
        where TString : class?, IComparable<string>?, IEquatable<string>?
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(requirements);
        return field.SetKind(FormFieldKind.Password, s => s.PasswordRequirements = requirements.Length == 0 ? null : requirements);
    }

    /// <summary>Renders a text field as a phone number input. The format is not validated.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TString">The type of the value of the field: a <see cref="string"/>, nullable or not.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TString> AsPhone<TModel, TString>(this FormFieldBuilder<TModel, TString> field)
        where TString : class?, IComparable<string>?, IEquatable<string>?
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.PhoneNumber);
    }

    /// <summary>Renders a text field as a color input. The value is <c>#rrggbb</c>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TString">The type of the value of the field: a <see cref="string"/>, nullable or not.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TString> AsColor<TModel, TString>(this FormFieldBuilder<TModel, TString> field)
        where TString : class?, IComparable<string>?, IEquatable<string>?
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.ColorPicker);
    }

    /// <summary>Renders a text field as a one-time code input.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TString">The type of the value of the field: a <see cref="string"/>, nullable or not.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="length">The number of characters of the code, which is the <c>maxlength</c> of the input.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TString> AsOneTimeCode<TModel, TString>(this FormFieldBuilder<TModel, TString> field, int length = 6)
        where TString : class?, IComparable<string>?, IEquatable<string>?
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.OneTimeCode, s => s.MaxLength = length);
    }

    /// <summary>Limits the number of characters of a text value. It is checked on validation and is the <c>maxlength</c> of the input.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TString">The type of the value of the field: a <see cref="string"/>, nullable or not.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="maxLength">The largest accepted number of characters.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TString> MaxLength<TModel, TString>(this FormFieldBuilder<TModel, TString> field, int maxLength)
        where TString : class?, IComparable<string>?, IEquatable<string>?
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.MaxLength = maxLength);
    }

    /// <summary>Bounds a number. The bounds are checked on validation and when a value is written (an agent, <c>Set</c>), and are the <c>min</c> and <c>max</c> of the input. They are stored as <see cref="double"/>, but a value is compared with them digit for digit (a <see cref="decimal"/> of 100.0000000000000001 is above a maximum of 100, a <see cref="long"/> of 2^53 + 1 is above a maximum of 2^53), and the type of the member adds its own exact limits.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The number type.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="min">The smallest accepted value.</param>
    /// <param name="max">The largest accepted value.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> Range<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, TValue min, TValue max)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s =>
        {
            s.Min = double.CreateSaturating(min);
            s.Max = double.CreateSaturating(max);
        });
    }

    /// <summary>Bounds an optional number. See <see cref="Range{TModel, TValue}(FormFieldBuilder{TModel, TValue}, TValue, TValue)"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The number type.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="min">The smallest accepted value.</param>
    /// <param name="max">The largest accepted value.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue?> Range<TModel, TValue>(this FormFieldBuilder<TModel, TValue?> field, TValue min, TValue max)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s =>
        {
            s.Min = double.CreateSaturating(min);
            s.Max = double.CreateSaturating(max);
        });
    }

    /// <summary>Sets the <c>step</c> of a number input.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The number type.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="step">The step. It is not validated.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> StepBy<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, TValue step)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.Step = double.CreateSaturating(step));
    }

    /// <summary>Sets the <c>step</c> of an optional number input.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The number type.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="step">The step. It is not validated.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue?> StepBy<TModel, TValue>(this FormFieldBuilder<TModel, TValue?> field, TValue step)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.Step = double.CreateSaturating(step));
    }

    /// <summary>Renders a number as a range slider, bounded by <see cref="Range{TModel, TValue}(FormFieldBuilder{TModel, TValue}, TValue, TValue)"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The number type.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> AsSlider<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Slider);
    }

    /// <summary>Renders an optional number as a range slider. The slider shows its minimum while the value is empty.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The number type.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue?> AsSlider<TModel, TValue>(this FormFieldBuilder<TModel, TValue?> field)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Slider);
    }

    /// <summary>Renders a whole number as a star rating from 1 to <paramref name="stars"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="stars">The number of stars.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, int> AsRating<TModel>(this FormFieldBuilder<TModel, int> field, int stars = 5)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Rating, s => s.Max = stars);
    }

    /// <summary>Renders an optional whole number as a star rating from 1 to <paramref name="stars"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="stars">The number of stars.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, int?> AsRating<TModel>(this FormFieldBuilder<TModel, int?> field, int stars = 5)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Rating, s => s.Max = stars);
    }

    /// <summary>Renders a boolean as a switch instead of a checkbox.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, bool> AsSwitch<TModel>(this FormFieldBuilder<TModel, bool> field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Switch);
    }

    /// <summary>Renders an optional boolean as a switch instead of a checkbox.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, bool?> AsSwitch<TModel>(this FormFieldBuilder<TModel, bool?> field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Switch);
    }

    /// <summary>Gives a field its options and renders it as a select list, unless <c>AsRadio</c> or <c>AsAutoComplete</c> chose another control. The values are stored in the model as <typeparamref name="TValue"/>; the control only sees their invariant text.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="options">The choices.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> Options<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, IEnumerable<FormOption<TValue>> options)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(options);
        var list = options.ToList();
        return field.ChoiceKind().Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.Options = ToItems(codec, list);
        });
    }

    /// <summary>Gives an optional field its options. See <see cref="Options{TModel, TValue}(FormFieldBuilder{TModel, TValue}, IEnumerable{FormOption{TValue}})"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field, without <see cref="Nullable{T}"/>.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="options">The choices.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue?> Options<TModel, TValue>(this FormFieldBuilder<TModel, TValue?> field, IEnumerable<FormOption<TValue>> options)
        where TValue : struct
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(options);
        var list = options.ToList();
        return field.ChoiceKind().Add(s =>
        {
            var codec = (FormCodec<TValue?>)s.Codec!;
            s.Options = list.Select(o => Item(codec.Format(o.Value), o)).ToList();
        });
    }

    /// <summary>Computes the options from the controller, for example to depend on another field. The options are computed again on every render.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="options">Computes the choices from the controller of the form.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> Options<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, Func<FormController<TModel>, IReadOnlyList<FormOption<TValue>>> options)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(options);
        return field.ChoiceKind().Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.OptionsFactory = e => ToItems(codec, options(FormFieldBuilder<TModel, TValue>.Controller(e)));
        });
    }

    /// <summary>Computes the options of an optional field from the controller. See <see cref="Options{TModel, TValue}(FormFieldBuilder{TModel, TValue}, Func{FormController{TModel}, IReadOnlyList{FormOption{TValue}}})"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field, without <see cref="Nullable{T}"/>.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="options">Computes the choices from the controller of the form.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue?> Options<TModel, TValue>(this FormFieldBuilder<TModel, TValue?> field, Func<FormController<TModel>, IReadOnlyList<FormOption<TValue>>> options)
        where TValue : struct
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(options);
        return field.ChoiceKind().Add(s =>
        {
            var codec = (FormCodec<TValue?>)s.Codec!;
            s.OptionsFactory = e => options(FormFieldBuilder<TModel, TValue?>.Controller(e)).Select(o => Item(codec.Format(o.Value), o)).ToList();
        });
    }

    /// <summary>Renders a choice field as a radio group.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> AsRadio<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.Radio);
    }

    /// <summary>Renders a choice field as a text input that suggests the option labels.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> AsAutoComplete<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.SetKind(FormFieldKind.AutoComplete);
    }

    /// <summary>Turns text typed by the user into a new option, which the components offer as a create button under the control.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="create">Receives the trimmed text and returns the new option, or <c>null</c> to refuse it.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> OnCreateOption<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, Func<string, FormOption<TValue>?> create)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(create);
        return field.Add(s =>
        {
            var codec = (FormCodec<TValue>)s.Codec!;
            s.OnCreateOption = text => create(text) is { } option ? Item(codec.Format(option.Value), option) : null;
        });
    }

    /// <summary>Sets the accepted files of a file field, in the syntax of the HTML <c>accept</c> attribute: extensions (<c>.png</c>), content types (<c>image/png</c>) and wildcards (<c>image/*</c>). Validation checks it too.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="accept">The accepted files.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, FormFileValue?> Accept<TModel>(this FormFieldBuilder<TModel, FormFileValue?> field, string accept)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.Accept = accept);
    }

    /// <summary>Sets the accepted files of a field that takes several files. See <see cref="Accept{TModel}(FormFieldBuilder{TModel, FormFileValue}, string)"/>.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The collection of files.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="accept">The accepted files.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> Accept<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, string accept)
        where TValue : IEnumerable<FormFileValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.Accept = accept);
    }

    /// <summary>Sets the largest accepted size in bytes of each file of a file field. Files are read into memory when chosen, so this is also the most memory one file takes.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="bytes">The largest size in bytes.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, FormFileValue?> MaxFileSize<TModel>(this FormFieldBuilder<TModel, FormFileValue?> field, long bytes)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.MaxFileSize = bytes);
    }

    /// <summary>Sets the largest accepted size in bytes of each file of a field that takes several files.</summary>
    /// <typeparam name="TModel">The model the form is declared for.</typeparam>
    /// <typeparam name="TValue">The collection of files.</typeparam>
    /// <param name="field">The field builder.</param>
    /// <param name="bytes">The largest size in bytes.</param>
    /// <returns>The builder.</returns>
    public static FormFieldBuilder<TModel, TValue> MaxFileSize<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field, long bytes)
        where TValue : IEnumerable<FormFileValue>
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Add(s => s.MaxFileSize = bytes);
    }

    // Giving options makes a field a select list, unless another kind of choice control was asked for.
    internal static FormFieldBuilder<TModel, TValue> ChoiceKind<TModel, TValue>(this FormFieldBuilder<TModel, TValue> field) =>
        field.ExplicitKind is null ? field.SetKind(FormFieldKind.Select) : field;

    internal static List<FormOptionItem> ToItems<TValue>(FormCodec<TValue> codec, IEnumerable<FormOption<TValue>> options) =>
        options.Select(o => Item(codec.Format(o.Value), o)).ToList();

    internal static FormOptionItem Item<TValue>(string? value, FormOption<TValue> option) =>
        new(value ?? "", option.Label, option.Description, option.Group, option.Icon, option.Disabled);
}
