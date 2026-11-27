namespace Klyroo.Form.Maui;

/// <summary>The model of a typed form that was submitted.</summary>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class FormSubmittedEventArgs<TModel> : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="model">The model that was submitted.</param>
    public FormSubmittedEventArgs(TModel model) => Model = model;

    /// <summary>The model that was submitted: the one the form edits with the values written into it, or the new one the form created.</summary>
    public TModel Model { get; }
}

/// <summary>The output of a runtime form, at the moment it was submitted or cancelled.</summary>
public sealed class DynamicFormOutputEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="output">The output of the form.</param>
    public DynamicFormOutputEventArgs(IReadOnlyDictionary<string, object?> output) => Output = output;

    /// <summary>The output of the form, as <c>DynamicFormController.BuildOutput</c> returns it.</summary>
    public IReadOnlyDictionary<string, object?> Output { get; }
}

/// <summary>Implemented by the form views that hold a typed controller, so the views inside them (<see cref="KlyFormFieldView{TModel}"/>) find it.</summary>
internal interface IFormControllerHost<TModel>
{
    FormController<TModel>? EffectiveController { get; }
}
