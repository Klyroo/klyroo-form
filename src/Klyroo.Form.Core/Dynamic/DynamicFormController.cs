using Klyroo.Form;

namespace Klyroo.Form.Dynamic;

/// <summary>Runtime state of a <see cref="DynamicFormSchema"/>: field values, an initial snapshot for dirty tracking, touched fields, validation errors, step navigation, repeatable rows and submission. It has no UI dependency and is not thread safe: use it from one thread or one synchronization context at a time. Values are <see cref="object"/>, keyed by field key; <c>Klyroo.Form</c> offers <c>FormController&lt;TModel&gt;</c> for a form bound to a C# model.</summary>
/// <remarks>Dispose it when it is no longer used: it cancels the asynchronous validations still waiting or running. A controller you pass to a component stays yours to dispose; one the component creates from a schema is disposed with the component.</remarks>
public sealed class DynamicFormController : IDisposable, IFormRuntime
{
    private readonly FormEngine _engine;
    private readonly CompiledDynamic _compiled;
    private IReadOnlyList<IFormFieldAccess>? _access;

    /// <summary>Creates a controller for <paramref name="schema"/> and initializes the state. Values in <paramref name="initialInput"/> win over default values, and the result becomes the baseline for dirty tracking. A stepped form starts on its first step that is not skipped.</summary>
    /// <param name="schema">The schema to run.</param>
    /// <param name="initialInput">Initial values by field key. Keys that match no field are kept in <see cref="InternalState"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <c>null</c>.</exception>
    public DynamicFormController(DynamicFormSchema schema, IReadOnlyDictionary<string, object?>? initialInput = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        _compiled = DynamicCompiler.Compile(schema);
        _engine = new FormEngine(_compiled.Definition, this);
        _engine.Start(initialInput);
    }

    internal FormEngine Engine => _engine;

    internal void UseLocaleMessages(FormMessages messages) => _engine.UseLocaleMessages(messages);

    private FieldNode NodeOf(DynamicFieldSchema field) => _compiled.Node(field ?? throw new ArgumentNullException(nameof(field)));

    private SectionNode NodeOf(DynamicSectionSchema section) => _compiled.Node(section ?? throw new ArgumentNullException(nameof(section)));

    private static IReadOnlyList<DynamicFieldSchema> Map(IReadOnlyList<FieldNode> nodes) => nodes.Select(n => (DynamicFieldSchema)n.Source!).ToList();

    /// <summary>The identifier of the form: <see cref="DynamicFormSchema.FormKey"/>.</summary>
    public string FormKey => Schema.FormKey;

    /// <summary>The title of the form: <see cref="DynamicFormSchema.Title"/>.</summary>
    public string? Title => Schema.Title;

    /// <summary>The key of the current step, or <c>null</c> for a form without steps.</summary>
    public string? CurrentStepKey => CurrentStep?.Key;

    /// <summary>The fields of the form as <see cref="IFormFieldAccess"/> views, for an agent or any code that works with values as JSON. Containers, stateless blocks and <see cref="FormFieldKind.Hidden"/> fields are not listed.</summary>
    public IReadOnlyList<IFormFieldAccess> Fields => _access ??= FieldAccess.For(_engine);

    void IFormRuntime.Reset() => _engine.Reset();

    /// <summary>Raised synchronously, on the calling thread, after every change of state, error, step or submission status. Components subscribe to it to re-render.</summary>
    public event Action? Changed
    {
        add => _engine.Changed += value;
        remove => _engine.Changed -= value;
    }

    /// <summary>The schema this controller runs.</summary>
    public DynamicFormSchema Schema { get; }

    /// <summary>
    /// The built-in validation messages in use: the ones set here, else <see cref="DynamicFormSchema.Messages"/>, else the ones a Klyroo.Form component gave for its locale, else English.
    /// Setting it fixes the language of this controller: components no longer replace it. Errors already computed are computed again with the new messages.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public FormMessages Messages
    {
        get => _engine.Messages;
        set => _engine.Messages = value;
    }

    /// <summary>Zero-based index of the current step. It stays 0 for a form without steps.</summary>
    public int CurrentStepIndex => _engine.CurrentStepIndex;

    /// <summary><c>true</c> while <see cref="SubmitAsync()"/> is running.</summary>
    public bool IsSubmitting => _engine.IsSubmitting;

    /// <summary><c>true</c> when the schema declares steps.</summary>
    public bool IsStepped => _engine.IsStepped;

    /// <summary>The current step, or <c>null</c> for a form without steps.</summary>
    public DynamicStepSchema? CurrentStep => (DynamicStepSchema?)_engine.CurrentStep?.Source;

    /// <summary><c>true</c> when every step before the current one is skipped by <see cref="DynamicStepSchema.SkipWhen"/>, and for a form without steps.</summary>
    public bool IsFirstStep => _engine.IsFirstStep;

    /// <summary><c>true</c> when every step after the current one is skipped by <see cref="DynamicStepSchema.SkipWhen"/>, and for a form without steps. The components show the submit button instead of Next on such a step.</summary>
    public bool IsLastStep => _engine.IsLastStep;

    /// <summary>Live, read-only view of all values by key, including keys of the initial input that match no field.</summary>
    public IReadOnlyDictionary<string, object?> InternalState => _engine.InternalState;

    /// <summary>Keys whose current value differs from the initial snapshot, excluding <see cref="DynamicFormSchema.IgnoreDirtyPaths"/>. The snapshot is taken at construction, by <see cref="Reset"/> and after a successful submission.</summary>
    public IReadOnlyList<string> DirtyPaths => _engine.DirtyPaths;

    /// <summary><c>true</c> when <see cref="DirtyPaths"/> is not empty.</summary>
    public bool IsDirty => _engine.IsDirty;

    /// <summary>Errors to show: validation errors of touched fields, validation errors of touched cells of repeatable lists keyed by <see cref="CellPath"/> (for example <c>lines[0].sku</c>), plus every error set with <see cref="SetError"/>, by key.</summary>
    public IReadOnlyDictionary<string, string> Errors => _engine.Errors;

    /// <summary><c>true</c> if a field in effect (see <see cref="ActiveFields"/>) or a visible cell of one of its repeatable lists has a validation error, touched or not, or if any error set with <see cref="SetError"/> is blocking. Validation errors only exist once validation has run.</summary>
    public bool HasBlockingErrors => _engine.HasBlockingErrors;

    /// <summary>Every leaf field of the form: the fields of the steps, else of the sections, else of <see cref="DynamicFormSchema.Fields"/>, with groups, cards, objects and tabs flattened and ignored fields removed, whether they are visible or not. Repeatable lists are leaves; containers never are.</summary>
    public IReadOnlyList<DynamicFieldSchema> AllFields => Map(_engine.AllFields);

    /// <summary>The stateful leaf fields in effect now, which are the ones validation checks and <see cref="BuildOutput"/> includes: the fields of <see cref="AllFields"/> that are visible, inside visible containers and visible tabs, in a visible section, and in the current step or a step that is not skipped. Stateless kinds are left out.</summary>
    public IReadOnlyList<DynamicFieldSchema> ActiveFields => Map(_engine.ActiveFields);

    /// <summary>Top-level fields to render now: those of the current step, of the visible sections, or of the schema, without hidden and ignored fields.</summary>
    public IReadOnlyList<DynamicFieldSchema> CurrentVisibleTopFields => Map(_engine.CurrentVisibleTopFields);

    /// <summary>Gets the current value of a field.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The value, or <c>null</c> when the key has no value.</returns>
    public object? GetValue(string path) => _engine.GetValue(path);

    /// <summary>Gets the current value converted to <typeparamref name="T"/>. A value already of that type is returned as is. Otherwise it is converted with the invariant culture, in the formats the controls store: <c>T</c> may be a <see cref="Nullable{T}"/>, a number (a whole number only becomes an integer type: 3.5 is not an <see cref="int"/>), <see cref="bool"/>, <see cref="Guid"/>, an enum (by name, ignoring case, or by the number of a member), <see cref="DateOnly"/> (<c>yyyy-MM-dd</c>), <see cref="TimeOnly"/> (<c>HH:mm</c>), <see cref="DateTime"/> (<c>yyyy-MM-ddTHH:mm</c>), <see cref="DateTimeOffset"/> (a value without a zone counts as UTC), <see cref="TimeSpan"/>, a string, or a list of strings.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The converted value, or <c>default</c> when the value is missing, blank or cannot be converted. Use <see cref="TryGetValue{T}"/> to tell those cases apart from a real <c>default</c>.</returns>
    public T? GetValue<T>(string path) => _engine.GetValue<T>(path);

    /// <summary>Tries to get the current value converted to <typeparamref name="T"/>, with the conversions of <see cref="GetValue{T}"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="value">The converted value, or <c>default</c> when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> when the field has a value that converts to <typeparamref name="T"/>. <c>false</c> when the key has no value, the value is <c>null</c> or (except for a string target) blank, or it cannot be converted.</returns>
    public bool TryGetValue<T>(string path, out T? value) => _engine.TryGetValue(path, out value);

    /// <summary>Gets the value a field had in the initial snapshot.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns>The initial value, or <c>null</c>.</returns>
    public object? GetInitialValue(string path) => _engine.GetInitialValue(path);

    /// <summary>Sets a value, clears any error set with <see cref="SetError"/> on the key (and the errors and touched state of the cells when it replaces the rows of a repeatable list), validates the field if it has been touched and its trigger is not <see cref="FormValidationTrigger.Submit"/> (with that trigger, the validation error is cleared instead), calls <see cref="DynamicFieldSchema.Watch"/>, validates again the other touched fields that depend on this one (see <see cref="DynamicFieldSchema.DependsOn"/>) or show an error, and raises <see cref="Changed"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to mark the field touched, so its error is shown. Fields with the <see cref="FormValidationTrigger.Input"/> trigger are always marked.</param>
    public void SetValue(string path, object? value, bool markTouched = false) => _engine.SetValue(path, value, markTouched);

    /// <summary>Marks a field touched, which makes its validation error visible, and validates it unless its trigger is <see cref="FormValidationTrigger.Submit"/>.</summary>
    /// <param name="path">Key of the field.</param>
    public void MarkTouched(string path) => _engine.MarkTouched(path);

    /// <summary>Restores a field to its initial value, clears its errors and touched state (and those of its cells for a repeatable list), and calls <see cref="DynamicFieldSchema.Watch"/> when the value changed.</summary>
    /// <param name="path">Key of the field.</param>
    public void ResetField(string path) => _engine.ResetField(path);

    /// <summary>Resets every field of a section with <see cref="ResetField"/>. Does nothing when the key matches no section.</summary>
    /// <param name="sectionKey">Key of the section.</param>
    public void ResetSection(string sectionKey) => _engine.ResetSection(sectionKey);

    /// <summary>Sets an error on a field, for example one returned by the server, and marks the field touched. The error is removed by the next <see cref="SetValue"/> on that key, by <see cref="ClearError"/> or by <see cref="Reset"/>.</summary>
    /// <param name="path">Key of the field.</param>
    /// <param name="message">The message to show.</param>
    /// <param name="blocking"><c>true</c> to make <see cref="Validate"/> and <see cref="SubmitAsync()"/> fail until the error is cleared.</param>
    public void SetError(string path, string message, bool blocking = false) => _engine.SetError(path, message, blocking);

    /// <summary>Clears errors set with <see cref="SetError"/> and validation errors, for one field or for all. Clearing a repeatable list also clears the errors of its cells.</summary>
    /// <param name="path">Key of the field, or <c>null</c> to clear every error.</param>
    public void ClearError(string? path = null) => _engine.ClearError(path);

    /// <summary>Gets the error to show for a field, or for a cell of a repeatable list by its <see cref="CellPath"/>.</summary>
    /// <param name="path">Key of the field, or path of a cell.</param>
    /// <returns>The message, or <c>null</c> when there is none or the field has not been touched.</returns>
    public string? GetError(string path) => _engine.GetError(path);

    /// <summary>Tells whether a field is shown: it is not ignored and its <see cref="DynamicFieldSchema.Condition"/> is absent or true. It does not look at the containers, tab, section or step that hold the field; <see cref="ActiveFields"/> does.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is visible.</returns>
    public bool ShouldRenderField(DynamicFieldSchema field) => _engine.ShouldRenderField(NodeOf(field));

    /// <summary>Tells whether a section is shown: its <see cref="DynamicSectionSchema.Condition"/> is absent or true.</summary>
    /// <param name="section">The section to test.</param>
    /// <returns><c>true</c> if the section is visible.</returns>
    public bool IsSectionVisible(DynamicSectionSchema section) => _engine.IsSectionVisible(NodeOf(section));

    /// <summary>Tells whether a field is currently required, from <see cref="DynamicFieldSchema.Required"/> and <see cref="DynamicFieldSchema.RequiredWhen"/>.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is required.</returns>
    public bool IsFieldRequired(DynamicFieldSchema field) => _engine.IsFieldRequired(NodeOf(field));

    /// <summary>Tells whether a field is currently disabled, from <see cref="DynamicFieldSchema.Disabled"/>, <see cref="DynamicFieldSchema.DisabledWhen"/> and <see cref="IsSubmitting"/>: every field is disabled while a submission runs, so the user cannot edit values that are being validated or saved. The components render the controls disabled; <see cref="SetValue"/> from code still works.</summary>
    /// <param name="field">The field to test.</param>
    /// <returns><c>true</c> if the field is disabled.</returns>
    public bool IsFieldDisabled(DynamicFieldSchema field) => _engine.IsFieldDisabled(NodeOf(field));

    /// <summary>Gets the options of a field: those from its options factory or its static options, followed by the options added with <see cref="CreateFieldOption"/>.</summary>
    /// <param name="field">The choice field.</param>
    /// <returns>The options, possibly empty.</returns>
    public IReadOnlyList<FormOptionItem> GetFieldOptions(DynamicFieldSchema field) => _engine.GetFieldOptions(NodeOf(field));

    /// <summary>Creates an option from user text with <see cref="DynamicFieldSchema.OnCreateOption"/>, adds it to the field options and selects it, appending to the value for a field with <see cref="DynamicFieldSchema.Multiple"/>.</summary>
    /// <param name="field">The choice field.</param>
    /// <param name="label">The text typed by the user, trimmed before use.</param>
    /// <returns>The created option, or <c>null</c> when the text is blank, the field has no <see cref="DynamicFieldSchema.OnCreateOption"/> or it returned <c>null</c>.</returns>
    public FormOptionItem? CreateFieldOption(DynamicFieldSchema field, string label) => _engine.CreateFieldOption(NodeOf(field), label);

    /// <summary>Gets the rows of a repeatable list.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <returns>The rows, each a dictionary of cell values by child key. Empty when the key has no list.</returns>
    public IReadOnlyList<Dictionary<string, object?>> GetArrayItems(string arrayKey) => _engine.GetArrayItems(arrayKey);

    /// <summary>Appends a row to a repeatable list, filling each cell from the child field's default, then revalidates the list if it was touched and calls its <see cref="DynamicFieldSchema.Watch"/>.</summary>
    /// <param name="arrayField">The list field.</param>
    public void AddArrayItem(DynamicFieldSchema arrayField)
    {
        ArgumentNullException.ThrowIfNull(arrayField);
        _engine.AddArrayItem(NodeOf(arrayField));
    }

    /// <summary>Removes a row from a repeatable list, moving the errors and touched state of the following rows up, then revalidates the list if it was touched and calls its <see cref="DynamicFieldSchema.Watch"/>. Does nothing when the index is out of range.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    public void RemoveArrayItem(string arrayKey, int index) => _engine.RemoveArrayItem(arrayKey, index);

    /// <summary>Sets one cell of a repeatable list. Does nothing when the index is out of range. The cell follows the <see cref="DynamicFieldSchema.Trigger"/> of its child field, like <see cref="SetValue"/> does for a field: it is validated with the child's rules once touched, unless the trigger is <see cref="FormValidationTrigger.Submit"/>, which clears its error instead. Then the child's <see cref="DynamicFieldSchema.Watch"/> is called with the cell value and the list's with the rows.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="markTouched"><c>true</c> to mark the cell touched, so its error is shown. Children with the <see cref="FormValidationTrigger.Input"/> trigger are always marked.</param>
    public void SetArrayItemValue(string arrayKey, int index, string childKey, object? value, bool markTouched = false) =>
        _engine.SetArrayItemValue(arrayKey, index, childKey, value, markTouched);

    /// <summary>Marks a cell of a repeatable list touched, which makes its validation error visible, and validates it unless the trigger of its child field is <see cref="FormValidationTrigger.Submit"/>. <c>KlyFormEngine</c> calls it when a cell loses focus.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    public void MarkArrayItemTouched(string arrayKey, int index, string childKey) => _engine.MarkArrayItemTouched(arrayKey, index, childKey);

    /// <summary>Gets the error to show for a cell of a repeatable list.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <returns>The message, or <c>null</c> when the cell has no error or has not been touched.</returns>
    public string? GetArrayItemError(string arrayKey, int index, string childKey) => _engine.GetArrayItemError(arrayKey, index, childKey);

    /// <summary>Builds the path that identifies a cell of a repeatable list in <see cref="Errors"/> and <see cref="GetError"/>: <c>arrayKey[index].childKey</c>.</summary>
    /// <param name="arrayKey">Key of the list field.</param>
    /// <param name="index">Zero-based index of the row.</param>
    /// <param name="childKey">Key of the child field.</param>
    /// <returns>The path, for example <c>lines[0].sku</c>.</returns>
    public static string CellPath(string arrayKey, int index, string childKey) => FormEngine.CellPath(arrayKey, index, childKey);

    /// <summary>Validates the fields in effect (see <see cref="ActiveFields"/>) according to <see cref="DynamicFormSchema.ValidateMode"/>, including each cell of their repeatable lists, and marks them touched so their errors show. Validation errors of fields that are not in effect, such as hidden ones, are removed.</summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the fields of the current step.</param>
    /// <returns><c>true</c> if none of the validated fields and cells has an error and no blocking error set with <see cref="SetError"/> applies. A blocking error applies when it is on a validated field or cell, or when it is a form-level error (its key matches no field, see <see cref="SetFormError"/>), whichever step is validated.</returns>
    public bool Validate(bool currentStepOnly = false) => _engine.Validate(currentStepOnly);

    /// <summary>Builds the dictionary to submit: every field in effect (see <see cref="ActiveFields"/>) except those with <see cref="DynamicFieldSchema.OmitFromOutput"/>, with <see cref="DynamicFieldSchema.TransformOutput"/> applied. Fields that are hidden, in a hidden container, tab or section, or in a skipped step are left out, and their values stay in the state. Lists are copied.</summary>
    /// <returns>The values by field key.</returns>
    public IReadOnlyDictionary<string, object?> BuildOutput() => _engine.BuildOutput();

    /// <summary>Moves to the next step that is not skipped. It validates the current step, then runs <see cref="DynamicFormSchema.OnBeforeNext"/>, and clears the touched state and validation errors of the new step.</summary>
    /// <returns><c>true</c> if the step changed. <c>false</c> when the form has no steps, is on its last step, is submitting or already changing step, fails validation or a hook returns <c>false</c>.</returns>
    public Task<bool> NextStepAsync() => _engine.NextStepAsync();

    /// <summary>Moves to the previous step that is not skipped, after <see cref="DynamicFormSchema.OnBeforePrevious"/>. Values are kept and the touched state and validation errors are cleared.</summary>
    /// <returns><c>true</c> if the step changed. <c>false</c> when the form has no steps, is on its first step, is submitting or already changing step, or a hook returns <c>false</c>.</returns>
    public Task<bool> PreviousStepAsync() => _engine.PreviousStepAsync();

    /// <summary>Validates every field in effect, runs <see cref="DynamicFormSchema.OnBeforeSubmit"/> then <see cref="DynamicFormSchema.OnSubmit"/>, and on success takes a new initial snapshot so the form is no longer dirty. <see cref="IsSubmitting"/> is true meanwhile. Use <see cref="SubmitAsync(Func{DynamicFormController, CancellationToken, Task{bool}}, CancellationToken)"/> to run your own save step inside the same lifecycle.</summary>
    /// <returns><c>true</c> if the form was submitted. <c>false</c> if a submission is already running, validation fails or <see cref="DynamicFormSchema.OnBeforeSubmit"/> returns <c>false</c>.</returns>
    public Task<bool> SubmitAsync() => _engine.SubmitAsync();

    /// <summary>
    /// Submits like <see cref="SubmitAsync()"/> and runs <paramref name="handler"/>, your save step, after <see cref="DynamicFormSchema.OnSubmit"/>, while <see cref="IsSubmitting"/> is still <c>true</c>.
    /// The dirty snapshot is retaken, and the form stops being dirty, only when every step succeeded, so a handler that returns <c>false</c> or throws leaves the user's edits
    /// dirty and the form editable again. <see cref="IsSubmitting"/> goes back to <c>false</c> in every case, and a second call while one runs returns <c>false</c> at once.
    /// </summary>
    /// <param name="handler">Receives this controller (read <see cref="BuildOutput"/> from it) and <paramref name="cancellationToken"/>, and returns <c>true</c> when the save succeeded. To report a failure, return <c>false</c> after calling <see cref="SetError"/> or <see cref="SetFormError"/>. An exception it throws propagates to the caller.</param>
    /// <param name="cancellationToken">Passed to <paramref name="handler"/> and to the asynchronous validators.</param>
    /// <returns><c>true</c> if the form was submitted and saved. <c>false</c> if a submission is already running, validation fails, <see cref="DynamicFormSchema.OnBeforeSubmit"/> or <paramref name="handler"/> returns <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <c>null</c>.</exception>
    /// <example><c>var saved = await form.SubmitAsync(async (f, ct) => (await api.SaveAsync(f.BuildOutput(), ct)).Ok);</c></example>
    public Task<bool> SubmitAsync(Func<DynamicFormController, CancellationToken, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _engine.SubmitAsync((_, ct) => handler(this, ct), cancellationToken);
    }

    /// <summary>Clears errors and touched state, reloads the state from <paramref name="newInput"/>, which also becomes the new dirty-tracking baseline, and returns to the first step that is not skipped. <see cref="DynamicFieldSchema.Watch"/> is not called.</summary>
    /// <param name="newInput">New initial values, or <c>null</c> to start from the defaults.</param>
    public void Reset(IReadOnlyDictionary<string, object?>? newInput = null) => _engine.Reset(newInput);

    /// <summary>Computes the completion state of each visible section from the current values and errors. Fields in hidden containers or tabs are not counted.</summary>
    /// <returns>One state per visible section, in schema order.</returns>
    public IReadOnlyList<FormPageSectionState> GetSectionStates() => _engine.GetSectionStates();

    /// <summary>Tells whether a kind carries no value: <see cref="FormFieldKind.Info"/>, <see cref="FormFieldKind.Divider"/>, <see cref="FormFieldKind.Section"/> and <see cref="FormFieldKind.Button"/>.</summary>
    /// <param name="kind">The kind to test.</param>
    /// <returns><c>true</c> for a stateless kind.</returns>
    public static bool IsStateless(FormFieldKind kind) => FormEngine.IsStateless(kind);

    /// <summary>Tells whether a kind is a layout container whose children are fields of their own: <see cref="FormFieldKind.Group"/>, <see cref="FormFieldKind.Card"/>, <see cref="FormFieldKind.Object"/> and <see cref="FormFieldKind.Tabs"/>. A container never has a value, even without children.</summary>
    /// <param name="kind">The kind to test.</param>
    /// <returns><c>true</c> for a container kind.</returns>
    public static bool IsContainer(FormFieldKind kind) => FormEngine.IsContainer(kind);

    /// <summary>Tells whether a value counts as empty for required checks: <c>null</c>, a blank string or an empty collection. <c>false</c> and <c>0</c> are not empty.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><c>true</c> if the value is empty.</returns>
    public static bool IsEmptyValue(object? value) => FormEngine.IsEmptyValue(value);

    /// <summary><c>true</c> while any asynchronous rule (see <see cref="DynamicFieldSchema.AsyncValidators"/>) is waiting for its debounce or running.</summary>
    public bool HasPendingValidation => _engine.HasPendingValidation;

    /// <summary>Tells whether an asynchronous rule of a field is waiting for its debounce or running, which the components show as a pending state next to the field.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns><c>true</c> while the field is being validated asynchronously.</returns>
    public bool IsValidating(string path) => _engine.IsValidating(path);

    /// <summary>
    /// Validates like <see cref="Validate"/>, then runs the asynchronous rules (see <see cref="DynamicFieldSchema.AsyncValidators"/>) of the validated fields that have a non-empty value,
    /// pass their synchronous checks and have no result yet for the current value, in parallel across fields, and validates again with their results.
    /// <see cref="SubmitAsync()"/> and <see cref="NextStepAsync"/> call it.
    /// </summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the fields of the current step.</param>
    /// <param name="cancellationToken">Cancels the rules that run. The rules run now, so a background run of the same field is cancelled and its result is not reused.</param>
    /// <returns><c>true</c> if none of the validated fields and cells has an error and no blocking error applies, as for <see cref="Validate"/>.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled. A run replaced by an edit is not an error: it is run again.</exception>
    /// <exception cref="Exception">An exception thrown by an asynchronous rule propagates, after the other rules finished or were cancelled.</exception>
    public Task<bool> ValidateAsync(bool currentStepOnly = false, CancellationToken cancellationToken = default) => _engine.ValidateAsync(currentStepOnly, cancellationToken);

    /// <summary>Cancels the asynchronous validations that are waiting for their debounce or running, without changing any value or error. A component calls it when it goes away.</summary>
    public void CancelPendingValidations() => _engine.CancelPendingValidations();

    /// <summary>Cancels the pending asynchronous validations and stops starting new ones. The values and errors stay readable.</summary>
    public void Dispose() => _engine.Dispose();

    /// <summary>Takes a copy of the values the user has typed and of the current step, to keep them across a reload or a navigation: store <see cref="FormDraft.ToJson"/> and give the draft back to <see cref="RestoreDraft"/> on a controller built from the same initial input.</summary>
    /// <param name="includeSensitive"><c>true</c> to include the values of <see cref="FormFieldKind.Password"/> and <see cref="FormFieldKind.OneTimeCode"/> fields. They are left out by default, because a draft is meant to be stored on the user's device. <see cref="FormFieldKind.File"/> fields are always left out: a file cannot be restored from a draft. The same applies to the columns of a repeatable list.</param>
    /// <returns>The draft. Lists and rows are copied, so later edits do not change it.</returns>
    public FormDraft ExportState(bool includeSensitive = false) => _engine.ExportState(includeSensitive);

    /// <summary>
    /// Puts the values of a draft back into the form, as if the user had typed them. The baseline of dirty tracking is not touched, so the restored fields
    /// differ from it: <see cref="IsDirty"/> and <see cref="DirtyPaths"/> report the draft as unsaved changes, which is what lets a navigation guard protect it.
    /// This is what separates it from <see cref="Reset"/>, which makes the loaded values the new baseline. Errors and touched state are cleared and
    /// <see cref="DynamicFieldSchema.Watch"/> is not called, like <see cref="Reset"/>. Keys of the draft that match no value of the form are ignored, as are <see cref="FormFieldKind.File"/> and <see cref="FormFieldKind.Custom"/> fields (and such columns of a list), and a field the draft
    /// does not mention keeps its value (the sensitive fields <see cref="ExportState"/> leaves out among them).
    /// </summary>
    /// <param name="draft">The draft, from <see cref="ExportState"/> or <see cref="FormDraft.FromJson"/>. Each value is converted to the type its field uses (a number read from JSON becomes an <see cref="int"/> for a rating, a list becomes the rows of a repeatable list).</param>
    /// <exception cref="ArgumentNullException"><paramref name="draft"/> is <c>null</c>.</exception>
    public void RestoreDraft(FormDraft draft) => _engine.RestoreDraft(draft);

    /// <summary>The key <see cref="SetFormError"/> uses for the form-level error. It matches no field, so the error counts as form-level.</summary>
    public const string FormErrorKey = FormEngine.FormErrorKey;

    /// <summary><c>true</c> after a submission, or a move to the next step, failed because of validation, a hook or a handler, until a submission succeeds, the step changes or <see cref="Reset"/> runs. The components list every field error in the error summary while it is set.</summary>
    public bool SubmitFailed => _engine.SubmitFailed;

    /// <summary>How many times a submission or a move to the next step failed since the controller was created or last reset. The error summary announces once per increase instead of on every edit.</summary>
    public int SubmitFailureCount => _engine.SubmitFailureCount;

    /// <summary>Sets an error on the form as a whole, for a failure that belongs to no field (a server rejection, a conflict). It is a <see cref="SetError"/> on <see cref="FormErrorKey"/>, so it shows in the error summary, and it clears with <see cref="ClearError"/> or <see cref="Reset"/>.</summary>
    /// <param name="message">The message to show.</param>
    /// <param name="blocking"><c>true</c> to make <see cref="Validate"/> and <see cref="SubmitAsync()"/> fail until the error is cleared. The default is <c>true</c>, since a form-level error usually means the data cannot be saved as it is; fields do not clear it when they change, so call <see cref="ClearError"/> when the user fixes the cause.</param>
    public void SetFormError(string message, bool blocking = true) => _engine.SetFormError(message, blocking);

    /// <summary>The messages of the form-level errors: those set with <see cref="SetError"/> on a key that matches no field of the schema, such as <see cref="FormErrorKey"/>.</summary>
    public IReadOnlyList<string> FormErrors => _engine.FormErrors;

    /// <summary>Lists the errors to show, in the order of the form: the errors of the fields in effect and of their cells, then the other errors set with <see cref="SetError"/>, form-level ones last. It is built from <see cref="Errors"/>, so errors of untouched fields are not listed.</summary>
    /// <returns>The entries, possibly empty.</returns>
    public IReadOnlyList<FormErrorEntry> GetErrorEntries() => _engine.GetErrorEntries();
}
