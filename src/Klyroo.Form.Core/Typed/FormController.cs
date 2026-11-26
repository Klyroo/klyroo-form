using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Klyroo.Form.Dynamic;

namespace Klyroo.Form;

/// <summary>Runtime state of a <see cref="FormSchema{TModel}"/>: the values of the fields, a baseline for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. Values are read and written through the handles the schema gave (<c>Get</c>, <c>Set</c>), typed as the members of the model, and move to and from the model with <see cref="CreateModel"/> and <see cref="ApplyTo"/>. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time.</summary>
/// <remarks>Dispose it when it is no longer used: it cancels the asynchronous validations still waiting or running. A controller you pass to a component stays yours to dispose; one the component creates from a schema is disposed with the component.</remarks>
/// <typeparam name="TModel">The model the form is declared for.</typeparam>
public sealed class FormController<[DynamicallyAccessedMembers(FormModelTrim.Members)] TModel> : IFormRuntime, IEngineOwner, IDisposable
{
    private readonly FormEngine _engine;
    private readonly HashSet<string> _reported = new(StringComparer.Ordinal);
    private IReadOnlyList<IFormFieldAccess>? _access;

    /// <summary>Creates a controller for <paramref name="schema"/>, with the values of <paramref name="model"/> as its baseline. Without a model the form starts from the default of each field (the <c>Default</c> of its declaration, else the default of the type).</summary>
    /// <param name="schema">The schema to run.</param>
    /// <param name="model">The model to edit. It is only read: the controller never changes it unless you call <see cref="ApplyTo"/> or give it as the target of <see cref="SubmitAsync(Func{TModel, CancellationToken, Task{bool}}, TModel?, CancellationToken)"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <c>null</c>.</exception>
    public FormController(FormSchema<TModel> schema, TModel? model = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        _engine = new FormEngine(schema.Definition, this);
        IReadOnlyDictionary<string, object?>? input = null;
        if (model is not null) input = schema.ToInput(model);
        else if (schema.Factory is { } factory) input = schema.ToInput(factory(), skipDefaulted: true);
        _engine.Start(input);
    }

    internal FormEngine Engine => _engine;

    FormEngine IEngineOwner.Engine => _engine;

    internal void UseLocaleMessages(FormMessages messages) => _engine.UseLocaleMessages(messages);

    /// <summary>The schema this controller runs.</summary>
    public FormSchema<TModel> Schema { get; }

    /// <summary>The identifier of the form.</summary>
    public string FormKey => Schema.FormKey;

    /// <summary>The title of the form, or <c>null</c>.</summary>
    public string? Title => Schema.Title;

    /// <summary>Raised synchronously, on the calling thread, after every change of state, error, step or submission status. Components subscribe to it to re-render.</summary>
    public event Action? Changed
    {
        add => _engine.Changed += value;
        remove => _engine.Changed -= value;
    }

    /// <summary>The built-in validation messages in use: the ones set here, else the ones of the schema, else the ones a Klyroo.Form component gave for its locale, else English. Setting it fixes the language of this controller.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public FormMessages Messages
    {
        get => _engine.Messages;
        set => _engine.Messages = value;
    }

    /// <summary><c>true</c> when a value differs from the baseline taken at creation, by <see cref="Reset()"/> or after a successful submission.</summary>
    public bool IsDirty => _engine.IsDirty;

    /// <summary><c>true</c> while a submission runs.</summary>
    public bool IsSubmitting => _engine.IsSubmitting;

    /// <summary><c>true</c> when the schema declares steps.</summary>
    public bool IsStepped => _engine.IsStepped;

    /// <summary>Zero-based index of the current step. It stays 0 for a form without steps.</summary>
    public int CurrentStepIndex => _engine.CurrentStepIndex;

    /// <summary>The key of the current step, or <c>null</c> for a form without steps.</summary>
    public string? CurrentStepKey => _engine.CurrentStep?.Key;

    /// <summary><c>true</c> when every step before the current one is skipped, and for a form without steps.</summary>
    public bool IsFirstStep => _engine.IsFirstStep;

    /// <summary><c>true</c> when every step after the current one is skipped, and for a form without steps.</summary>
    public bool IsLastStep => _engine.IsLastStep;

    /// <summary><c>true</c> if a field in effect has a validation error, touched or not, or if an error set with <see cref="SetError"/> is blocking.</summary>
    public bool HasBlockingErrors => _engine.HasBlockingErrors;

    /// <summary><c>true</c> after a submission or a move to the next step failed, until a submission succeeds, the step changes or <see cref="Reset()"/> runs.</summary>
    public bool SubmitFailed => _engine.SubmitFailed;

    /// <summary>How many times a submission or a move to the next step failed since the controller was created or last reset.</summary>
    public int SubmitFailureCount => _engine.SubmitFailureCount;

    /// <summary>The errors to show, by field key: validation errors of touched fields and cells, plus every error set with <see cref="SetError"/>.</summary>
    public IReadOnlyDictionary<string, string> Errors => _engine.Errors;

    /// <summary>The messages of the errors that belong to the form as a whole.</summary>
    public IReadOnlyList<string> FormErrors => _engine.FormErrors;

    /// <summary><c>true</c> while any asynchronous rule is waiting for its debounce or running.</summary>
    public bool HasPendingValidation => _engine.HasPendingValidation;

    /// <summary>The fields of the form as <see cref="IFormFieldAccess"/> views, for an agent or any code that works with values as JSON.</summary>
    public IReadOnlyList<IFormFieldAccess> Fields => _access ??= FieldAccess.For(_engine);

    void IFormRuntime.Reset() => Reset();

    /// <summary>Gets the current value of a field as its type.</summary>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field, as the schema declared it.</param>
    /// <returns>The value, or the default of the type while the field is empty or its text is not a valid value: <c>0</c> for an <c>int</c>, <c>null</c> for a string. A list or a multiple choice gives an empty list, never <c>null</c>, in those cases. Use <see cref="TryGet{TValue}"/> to tell them apart.</returns>
    public TValue? Get<TValue>(FormField<TModel, TValue> field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Codec.FromControlOrDefault(_engine.GetValue(field.Key));
    }

    /// <summary>Tries to get the current value of a field.</summary>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field.</param>
    /// <param name="value">The value, or the default of the type when the method returns <c>false</c> (an empty list for a list or a multiple choice).</param>
    /// <returns><c>true</c> when the field has a value that is a valid <typeparamref name="TValue"/>. <c>false</c> while it is empty (an empty list too), or while the text the user typed is not a value yet (<c>abc</c> in a number field).</returns>
    public bool TryGet<TValue>(FormField<TModel, TValue> field, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Codec.TryFromControl(_engine.GetValue(field.Key), out value) == ControlValue.Value;
    }

    /// <summary>Sets the value of a field, validating it when it was touched and running its change hooks.</summary>
    /// <typeparam name="TValue">The type of the value of the field.</typeparam>
    /// <param name="field">The field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to make the error of the field visible.</param>
    public void Set<TValue>(FormField<TModel, TValue> field, TValue value, bool markTouched = false)
    {
        ArgumentNullException.ThrowIfNull(field);
        _engine.SetValue(field.Key, field.Codec.ToControl(value), markTouched);
    }

    /// <summary>Marks a field touched, which makes its validation error visible, and validates it.</summary>
    /// <param name="item">The field.</param>
    public void Touch(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _engine.MarkTouched(item.Key);
    }

    /// <summary>Gets the error to show for a field.</summary>
    /// <param name="item">The field.</param>
    /// <returns>The message, or <c>null</c> when there is none or the field has not been touched.</returns>
    public string? GetError(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.GetError(item.Key);
    }

    /// <summary>Sets an error on a field, for example after a server check. It stays until the field changes or <see cref="ClearErrors"/> runs.</summary>
    /// <param name="item">The field.</param>
    /// <param name="message">The message to show.</param>
    /// <param name="blocking"><c>true</c> to make validation fail while the error exists.</param>
    public void SetError(FormItem<TModel> item, string message, bool blocking = false)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(message);
        _engine.SetError(item.Key, message, blocking);
    }

    /// <summary>Sets an error on the form as a whole.</summary>
    /// <param name="message">The message to show.</param>
    /// <param name="blocking"><c>true</c> to make validation fail while the error exists.</param>
    public void SetFormError(string message, bool blocking = true)
    {
        ArgumentNullException.ThrowIfNull(message);
        _engine.SetFormError(message, blocking);
    }

    /// <summary>Clears the errors of one field, or every error when no field is given.</summary>
    /// <param name="item">The field, or <c>null</c> for all.</param>
    public void ClearErrors(FormItem<TModel>? item = null) => _engine.ClearError(item?.Key);

    /// <summary>Tells whether the value of a field differs from its baseline.</summary>
    /// <param name="item">The field.</param>
    /// <returns><c>true</c> when the field is dirty.</returns>
    public bool IsFieldDirty(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.DirtyPaths.Contains(item.Key);
    }

    /// <summary>Tells whether an asynchronous rule of a field is waiting for its debounce or running.</summary>
    /// <param name="item">The field.</param>
    /// <returns><c>true</c> while the field is being validated asynchronously.</returns>
    public bool IsValidating(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.IsValidating(item.Key);
    }

    /// <summary>Tells whether a field is shown now: its condition is absent or true. It does not look at the container, step or section that holds it.</summary>
    /// <param name="item">The field or layout element.</param>
    /// <returns><c>true</c> if it is visible.</returns>
    public bool IsVisible(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.ShouldRenderField(item.Node);
    }

    /// <summary>Tells whether a field is required now.</summary>
    /// <param name="item">The field.</param>
    /// <returns><c>true</c> if the field is required.</returns>
    public bool IsRequired(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.IsFieldRequired(item.Node);
    }

    /// <summary>Tells whether a field is disabled now, which includes every field while a submission runs.</summary>
    /// <param name="item">The field.</param>
    /// <returns><c>true</c> if the field is disabled.</returns>
    public bool IsDisabled(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.IsFieldDisabled(item.Node);
    }

    /// <summary>Gets the options of a choice field, with their values as invariant text, as the controls see them.</summary>
    /// <param name="item">The choice field.</param>
    /// <returns>The options, possibly empty.</returns>
    public IReadOnlyList<FormOptionItem> GetOptions(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _engine.GetFieldOptions(item.Node);
    }

    /// <summary>Restores a field to its baseline value and clears its errors and touched state.</summary>
    /// <param name="item">The field.</param>
    public void ResetField(FormItem<TModel> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _engine.ResetField(item.Key);
    }

    /// <summary>Resets every field of a section.</summary>
    /// <param name="sectionKey">The key of the section.</param>
    public void ResetSection(string sectionKey) => _engine.ResetSection(sectionKey);

    /// <summary>Computes the completion state of each visible section from the current values and errors.</summary>
    /// <returns>One state per visible section, in schema order.</returns>
    public IReadOnlyList<FormPageSectionState> GetSectionStates() => _engine.GetSectionStates();

    /// <summary>Gets the rows of a repeatable list.</summary>
    /// <typeparam name="TItem">The type of a row.</typeparam>
    /// <param name="list">The list, as the schema declared it.</param>
    /// <returns>The rows, in order. Read a column of a row with <see cref="FormRow{TItem}.Get{TValue}"/>. A row loaded from a model stands for that item: <see cref="ApplyTo"/> writes its columns into it.</returns>
    public IReadOnlyList<FormRow<TItem>> Rows<TItem>(FormListField<TModel, TItem> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return Enumerable.Range(0, _engine.GetArrayItems(list.Key).Count).Select(i => new FormRow<TItem>(_engine, list.Key, i)).ToList();
    }

    /// <summary>Appends a row to a repeatable list. Each cell starts from the default of its column, or from <paramref name="seed"/> when one is given.</summary>
    /// <typeparam name="TItem">The type of a row.</typeparam>
    /// <param name="list">The list.</param>
    /// <param name="seed">An item whose values fill the new row.</param>
    public void AddRow<TItem>(FormListField<TModel, TItem> list, TItem? seed = null)
        where TItem : class
    {
        ArgumentNullException.ThrowIfNull(list);
        _engine.AddArrayItem(list.Node);
        if (seed is null) return;
        var index = _engine.GetArrayItems(list.Key).Count - 1;
        foreach (var column in list.Rows.Columns) _engine.SetArrayItemValue(list.Key, index, column.Key, column.ReadControl(seed));
    }

    /// <summary>Removes a row from a repeatable list, moving the errors of the following rows up.</summary>
    /// <typeparam name="TItem">The type of a row.</typeparam>
    /// <param name="list">The list.</param>
    /// <param name="index">The zero-based index of the row.</param>
    public void RemoveRow<TItem>(FormListField<TModel, TItem> list, int index)
    {
        ArgumentNullException.ThrowIfNull(list);
        _engine.RemoveArrayItem(list.Key, index);
    }

    /// <summary>Builds a new model from the values of the fields in effect. Fields that are hidden, or in a skipped step, keep the default of their member. The items of a repeatable list are new items: they are never the items of a model the form was loaded from, which this method does not change. Use <see cref="ApplyTo"/> to keep them.</summary>
    /// <returns>The new model.</returns>
    /// <exception cref="InvalidOperationException">The model has no parameterless constructor and the schema has no <c>Factory</c>, or a value cannot be converted to its member.</exception>
    public TModel CreateModel() => Schema.Create(_engine.BuildOutput());

    /// <summary>Writes the values of the fields in effect into <paramref name="target"/>. It is all or nothing: every value is converted and every object on the way is checked before the first member is set, and a setter that throws puts back the members already set. A member that nothing in the form changed keeps its value, with the precision and the kind the text of a control cannot hold. A repeatable list is written into the items its rows were loaded from (see <see cref="Rows{TItem}"/>), so members that are not columns survive; a new item is created only for an added row, and the collection is left alone when no row was added or removed.</summary>
    /// <param name="target">The model to change.</param>
    /// <returns>The target.</returns>
    /// <exception cref="InvalidOperationException">A value cannot be converted to its member, or an object on the way to a member is null and cannot be created.</exception>
    public TModel ApplyTo(TModel target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Schema.PlanWrite(_engine.BuildOutput(), target).Apply();
        return target;
    }

    /// <summary>Puts every value back to the baseline, clears errors and touched state, and goes back to the first step.</summary>
    public void Reset() => _engine.ResetToSnapshot();

    /// <summary>Loads a model: its values become the new baseline, errors and touched state are cleared and the form goes back to its first step.</summary>
    /// <param name="model">The model to load.</param>
    public void Reset(TModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _engine.Reset(Schema.ToInput(model));
    }

    /// <summary>Validates every field in effect: its rules, and whether its control holds a value of the type of the member (a control can hold text that is not a value yet, from a draft or a custom control; that is an error of the field with the message <see cref="FormMessages.InvalidValue"/>, whatever the validation mode).</summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the current step.</param>
    /// <returns><c>true</c> when there is no error.</returns>
    public bool Validate(bool currentStepOnly = false) => _engine.Validate(currentStepOnly);

    /// <summary>Validates every field in effect, waiting for the asynchronous rules.</summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the current step.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns><c>true</c> when there is no error.</returns>
    public Task<bool> ValidateAsync(bool currentStepOnly = false, CancellationToken cancellationToken = default) => _engine.ValidateAsync(currentStepOnly, cancellationToken);

    /// <summary>Moves to the next step that is not skipped, after validating the current one and running the <c>OnBeforeNext</c> hook.</summary>
    /// <returns><c>true</c> if the step changed.</returns>
    public Task<bool> NextStepAsync() => _engine.NextStepAsync();

    /// <summary>Moves to the previous step that is not skipped, after the <c>OnBeforePrevious</c> hook.</summary>
    /// <returns><c>true</c> if the step changed.</returns>
    public Task<bool> PreviousStepAsync() => _engine.PreviousStepAsync();

    /// <summary>Validates every field in effect, runs the model checks, <c>OnBeforeSubmit</c> and <c>OnSubmit</c>, and on success takes a new baseline so the form is no longer dirty. The model is created from the values for the checks and hooks and then dropped; use the overload with a save step to get it.</summary>
    /// <returns><c>true</c> if the form was submitted.</returns>
    public Task<bool> SubmitAsync() => _engine.SubmitAsync((_, ct) => RunAsync(null, default, ct), CancellationToken.None);

    /// <summary>Submits the form with your save step inside the same lifecycle. The steps are: validate (waiting for asynchronous rules); write the values into <paramref name="target"/>, or create a new model when there is none; run the model checks and <c>OnBeforeSubmit</c> and <c>OnSubmit</c> on that model; call <paramref name="save"/>. When a step fails, or <paramref name="save"/> returns <c>false</c> or throws, the members the write changed get their previous values again (a list or a multiple choice holds its previous items, and a row gets its previous cells). Members the form did not write are not touched, and nothing that <c>OnBeforeSubmit</c>, <c>OnSubmit</c> or <paramref name="save"/> did is undone, neither on the model nor outside it: <c>OnSubmit</c> runs before <paramref name="save"/>, so its side effects stay when the save fails. When all succeed the baseline is retaken, so the form is no longer dirty.</summary>
    /// <param name="save">Saves the model. Return <c>false</c> to report a failure.</param>
    /// <param name="target">The model to write into, or <c>null</c> to save a new one.</param>
    /// <param name="cancellationToken">Passed to the waits and to <paramref name="save"/>.</param>
    /// <returns><c>true</c> if the form was submitted and saved.</returns>
    public Task<bool> SubmitAsync(Func<TModel, CancellationToken, Task<bool>> save, TModel? target = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        return _engine.SubmitAsync((_, ct) => RunAsync(save, target, ct), cancellationToken);
    }

    private async Task<bool> RunAsync(Func<TModel, CancellationToken, Task<bool>>? save, TModel? target, CancellationToken cancellationToken)
    {
        var state = Schema.State;
        var needsModel = save is not null || state.Validators.Count > 0 || state.BeforeSubmit is not null || state.OnSubmit is not null;
        FormWritePlan? plan = null;
        TModel? model = default;
        if (needsModel)
        {
            var values = _engine.BuildOutput();
            if (target is not null)
            {
                plan = Schema.PlanWrite(values, target).Apply();
                model = target;
            }
            else
            {
                model = Schema.Create(values);
            }
        }
        try
        {
            if (model is not null)
            {
                if (!CheckModel(model)) return Rollback(plan);
                if (state.BeforeSubmit is { } before && !await before(model, this).ConfigureAwait(false)) return Rollback(plan);
                if (state.OnSubmit is { } submit) await submit(model, this).ConfigureAwait(false);
            }
            if (save is not null && !await save(model!, cancellationToken).ConfigureAwait(false)) return Rollback(plan);
            AdoptRows(plan);
            return true;
        }
        catch
        {
            plan?.Rollback();
            throw;
        }
    }

    // After a write into a target, each row of a list stands for the item the target now holds at its position, so the next write goes into the same items (and
    // keeps what a save step set on them, such as an identifier).
    private void AdoptRows(FormWritePlan? plan)
    {
        if (plan is null) return;
        foreach (var (key, items) in plan.Lists)
        {
            var rows = _engine.GetArrayItems(key);
            if (rows.Count != items.Count) continue;
            for (var i = 0; i < rows.Count; i++) RowOrigin.Set(rows[i], items[i]);
        }
    }

    private static bool Rollback(FormWritePlan? plan)
    {
        plan?.Rollback();
        return false;
    }

    // The checks of the model report on the fields they name: the errors of the previous submission are cleared first, so a fixed field stops showing one.
    private bool CheckModel(TModel model)
    {
        foreach (var key in _reported) _engine.ClearError(key);
        _reported.Clear();
        var validators = Schema.State.Validators;
        if (validators.Count == 0) return true;
        var errors = new FormModelErrors<TModel>(_engine.ActiveFields.Select(f => f.Key));
        foreach (var validator in validators) validator(model, errors);
        foreach (var (member, message) in errors.Items)
        {
            var key = member is null ? null : ResolveKey(member);
            if (key is null) _engine.SetFormError(message, blocking: false);
            else _engine.SetError(key, message, blocking: false);
            _reported.Add(key ?? FormEngine.FormErrorKey);
        }
        return errors.Items.Count == 0;
    }

    // A key, or a member name that exactly one bound field has.
    private string? ResolveKey(string member)
    {
        if (Schema.Bindings.Any(b => b.Key == member)) return member;
        var byName = Schema.Bindings.Where(b => b.Member.Name == member).ToList();
        return byName.Count == 1 ? byName[0].Key : null;
    }

    /// <summary>Takes a snapshot of what the user has typed, for example to keep a long form across a page reload. Passwords and one-time codes are left out unless <paramref name="includeSensitive"/> is set, and files never are part of a draft.</summary>
    /// <param name="includeSensitive"><c>true</c> to include passwords and one-time codes.</param>
    /// <returns>The draft, which <see cref="FormDraft.ToJson"/> writes as JSON.</returns>
    public FormDraft ExportDraft(bool includeSensitive = false) => _engine.ExportState(includeSensitive);

    /// <summary>Puts the values of a draft back into the form, as if the user had typed them. The baseline of dirty tracking is not touched, so the restored fields count as unsaved changes. A value of the draft that is not a value of its field (<c>abc</c> for a date) is kept as the text it is and reported by <see cref="Validate"/> and <see cref="SubmitAsync()"/> as an error of the field. The rows of a restored list come from no item of a model, so a write creates new items for them.</summary>
    /// <param name="draft">The draft, from <see cref="ExportDraft"/> or <see cref="FormDraft.FromJson"/>.</param>
    public void RestoreDraft(FormDraft draft) => _engine.RestoreDraft(draft);

    /// <summary>Cancels the asynchronous validations that are waiting for their debounce or running, without changing any value or error.</summary>
    public void CancelPendingValidations() => _engine.CancelPendingValidations();

    /// <summary>Cancels the pending asynchronous validations and stops starting new ones. The values and errors stay readable.</summary>
    public void Dispose() => _engine.Dispose();
}
