using System.Globalization;

namespace Klyroo.Form;

internal sealed partial class FormEngine : IDisposable
{
    private readonly Dictionary<string, object?> _state = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _initialSnapshot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _validationErrors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string Message, bool Blocking)> _customErrors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _touchedPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<CellKey, string> _cellErrors = [];
    private readonly HashSet<CellKey> _touchedCells = [];
    private readonly Dictionary<string, List<FormOptionItem>> _createdOptions = new(StringComparer.Ordinal);
    private bool _navigating;
    private readonly Dictionary<string, (object? Value, string? Message)> _asyncResults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _pendingValidations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BackgroundRun> _backgroundRuns = new(StringComparer.Ordinal);

    private readonly record struct CellKey(string Array, int Row, string Child)
    {
        public override string ToString() => CellPath(Array, Row, Child);
    }

    // Owner is the public controller that wraps this engine: the delegates of a schema receive it, not the engine.
    // The owner holds the engine in a field before Start runs, because a default factory or a skip condition may call back into the owner.
    internal FormEngine(FormDefinition schema, object? owner = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
        Owner = owner;
    }

    internal FormEngine Start(IReadOnlyDictionary<string, object?>? initialInput)
    {
        InitializeState(initialInput);
        CurrentStepIndex = FirstReachableStepIndex();
        return this;
    }

    internal object? Owner { get; }

    internal event Action? Changed;

    internal FormDefinition Schema { get; }

    private FormMessages? _messages;
    private bool _messagesExplicit;

    internal FormMessages Messages
    {
        get => _messages ?? Schema.Messages ?? FormMessages.Default;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _messagesExplicit = true;
            ApplyMessages(value);
        }
    }

    // What a component does for its locale: only when neither the schema nor the code chose the language.
    internal void UseLocaleMessages(FormMessages messages)
    {
        if (_messagesExplicit || Schema.Messages is not null) return;
        ApplyMessages(messages);
    }

    private void ApplyMessages(FormMessages messages)
    {
        if (ReferenceEquals(Messages, messages)) return;
        _messages = messages;
        foreach (var field in ActiveFields.Where(f => _validationErrors.ContainsKey(f.Key)).ToList())
        {
            ValidateFieldCore(field);
        }
        foreach (var cell in _cellErrors.Keys.ToList())
        {
            var arrayField = ActiveFields.FirstOrDefault(f => f.Key == cell.Array);
            var child = arrayField?.Fields?.FirstOrDefault(c => c.Key == cell.Child);
            if (arrayField is not null && child is not null) ValidateCell(arrayField, child, cell.Row);
        }
        Changed?.Invoke();
    }
    internal int CurrentStepIndex { get; private set; }
    internal bool IsSubmitting { get; private set; }
    internal bool IsStepped => Schema.Steps.Count > 0;
    internal StepNode? CurrentStep => IsStepped && CurrentStepIndex < Schema.Steps.Count ? Schema.Steps[CurrentStepIndex] : null;
    internal bool IsFirstStep
    {
        get
        {
            if (!IsStepped) return true;
            for (var i = 0; i < CurrentStepIndex && i < Schema.Steps.Count; i++)
            {
                if (!IsStepSkipped(i)) return false;
            }
            return true;
        }
    }
    internal bool IsLastStep
    {
        get
        {
            if (!IsStepped) return true;
            for (var i = CurrentStepIndex + 1; i < Schema.Steps.Count; i++)
            {
                if (!IsStepSkipped(i)) return false;
            }
            return true;
        }
    }

    internal IReadOnlyDictionary<string, object?> InternalState => _state;

    internal IReadOnlyList<string> DirtyPaths
    {
        get
        {
            var ignored = Schema.IgnoreDirtyPaths ?? [];
            var allKeys = _state.Keys.Union(_initialSnapshot.Keys, StringComparer.Ordinal);
            var dirty = new List<string>();
            foreach (var key in allKeys)
            {
                if (ignored.Any(ig => key.Equals(ig, StringComparison.Ordinal) || key.StartsWith(ig + ".", StringComparison.Ordinal)))
                {
                    continue;
                }
                _state.TryGetValue(key, out var cur);
                _initialSnapshot.TryGetValue(key, out var init);
                if (!ValuesEqual(cur, init))
                {
                    dirty.Add(key);
                }
            }
            return dirty;
        }
    }

    internal bool IsDirty => DirtyPaths.Count > 0;

    internal IReadOnlyDictionary<string, string> Errors
    {
        get
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (path, msg) in _validationErrors)
            {
                if (_touchedPaths.Contains(path))
                {
                    result[path] = msg;
                }
            }
            if (_cellErrors.Count > 0)
            {
                var columns = ActiveCellColumns();
                foreach (var (cell, msg) in _cellErrors)
                {
                    if (_touchedCells.Contains(cell) && IsCellInEffect(columns, cell))
                    {
                        result[cell.ToString()] = msg;
                    }
                }
            }
            foreach (var (path, (msg, _)) in _customErrors)
            {
                result[path] = msg;
            }
            return result;
        }
    }

    internal bool HasBlockingErrors
    {
        get
        {
            if (_customErrors.Values.Any(e => e.Blocking)) return true;
            if (_validationErrors.Count == 0 && _cellErrors.Count == 0) return false;
            var activeKeys = ActiveFields.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
            if (_validationErrors.Keys.Any(activeKeys.Contains)) return true;
            var columns = ActiveCellColumns();
            return _cellErrors.Keys.Any(c => IsCellInEffect(columns, c));
        }
    }

    internal IReadOnlyList<FieldNode> AllFields
    {
        get
        {
            if (IsStepped) return Schema.Steps.SelectMany(s => FlattenFields(s.Fields)).ToList();
            if (Schema.Sections.Count > 0) return Schema.Sections.SelectMany(s => FlattenFields(s.Fields)).ToList();
            return FlattenFields(Schema.Fields).ToList();
        }
    }

    internal IReadOnlyList<FieldNode> ActiveFields
    {
        get
        {
            var result = new List<FieldNode>();
            if (IsStepped)
            {
                for (var i = 0; i < Schema.Steps.Count; i++)
                {
                    if (i == CurrentStepIndex || !IsStepSkipped(i)) CollectActive(Schema.Steps[i].Fields, result);
                }
            }
            else if (Schema.Sections.Count > 0)
            {
                foreach (var section in Schema.Sections)
                {
                    if (IsSectionVisible(section)) CollectActive(section.Fields, result);
                }
            }
            else
            {
                CollectActive(Schema.Fields, result);
            }
            return result;
        }
    }

    internal IReadOnlyList<FieldNode> CurrentVisibleTopFields
    {
        get
        {
            var source = IsStepped ? (CurrentStep?.Fields ?? [])
                : Schema.Sections.Count > 0 ? Schema.Sections.Where(IsSectionVisible).SelectMany(s => s.Fields).ToList()
                : Schema.Fields;
            return source.Where(ShouldRenderField).ToList();
        }
    }

    internal object? GetValue(string path) =>
        _state.TryGetValue(path, out var val) ? val : null;

    internal T? GetValue<T>(string path) =>
        FormValueConverter.TryConvert<T>(GetValue(path), out var value) ? value : default;

    internal bool TryGetValue<T>(string path, out T? value) =>
        FormValueConverter.TryConvert(GetValue(path), out value);

    internal object? GetInitialValue(string path) =>
        _initialSnapshot.TryGetValue(path, out var val) ? val : null;

    internal void SetValue(string path, object? value, bool markTouched = false)
    {
        _state[path] = value;
        _customErrors.Remove(path);
        RemoveCellState(path);

        var field = FindField(path);
        if (markTouched || field?.Trigger == FormValidationTrigger.Input)
        {
            _touchedPaths.Add(path);
        }

        if (field?.Trigger == FormValidationTrigger.Submit)
        {
            _validationErrors.Remove(path);
        }
        else if (_touchedPaths.Contains(path) && field is not null)
        {
            ValidateField(field);
        }

        field?.Watch?.Invoke(value, this);
        RevalidateDependents(path);
        if (field is not null && field.AsyncValidators is { Count: > 0 })
        {
            if (field.Trigger != FormValidationTrigger.Submit && _touchedPaths.Contains(path)) ScheduleAsyncValidation(field);
            else CancelPendingValidation(path);
        }
        Changed?.Invoke();
    }

    internal void MarkTouched(string path)
    {
        _touchedPaths.Add(path);
        var field = FindField(path);
        if (field is not null && field.Trigger != FormValidationTrigger.Submit)
        {
            ValidateField(field);
            if (field.AsyncValidators is { Count: > 0 }) ScheduleAsyncValidation(field);
        }
        Changed?.Invoke();
    }

    internal void ResetField(string path)
    {
        _state.TryGetValue(path, out var before);
        _initialSnapshot.TryGetValue(path, out var init);
        var restored = CloneValue(init);
        _state[path] = restored;
        _validationErrors.Remove(path);
        _customErrors.Remove(path);
        _touchedPaths.Remove(path);
        RemoveCellState(path);
        CancelPendingValidation(path);
        _asyncResults.Remove(path);
        if (!ValuesEqual(before, restored))
        {
            FindField(path)?.Watch?.Invoke(restored, this);
            RevalidateDependents(path);
        }
        Changed?.Invoke();
    }

    internal void ResetSection(string sectionKey)
    {
        var section = Schema.Sections.FirstOrDefault(s => s.Key == sectionKey);
        if (section is null) return;
        foreach (var field in FlattenFields(section.Fields))
        {
            ResetField(field.Key);
        }
        Changed?.Invoke();
    }

    internal void SetError(string path, string message, bool blocking = false)
    {
        _customErrors[path] = (message, blocking);
        _touchedPaths.Add(path);
        Changed?.Invoke();
    }

    internal void ClearError(string? path = null)
    {
        if (path is null)
        {
            _customErrors.Clear();
            _validationErrors.Clear();
            _cellErrors.Clear();
        }
        else
        {
            _customErrors.Remove(path);
            _validationErrors.Remove(path);
            foreach (var cell in _cellErrors.Keys.Where(c => c.Array == path).ToList())
            {
                _cellErrors.Remove(cell);
            }
        }
        Changed?.Invoke();
    }

    internal string? GetError(string path) =>
        Errors.TryGetValue(path, out var err) ? err : null;

    internal bool ShouldRenderField(FieldNode field) =>
        !field.Ignore && (field.Condition is null || field.Condition(this));

    internal bool IsSectionVisible(SectionNode section) =>
        section.Condition is null || section.Condition(this);

    internal bool IsFieldRequired(FieldNode field) =>
        field.Required || (field.RequiredWhen is not null && field.RequiredWhen(this));

    internal bool IsFieldDisabled(FieldNode field) =>
        IsSubmitting || field.Disabled || (field.DisabledWhen is not null && field.DisabledWhen(this));

    internal IReadOnlyList<FormOptionItem> GetFieldOptions(FieldNode field)
    {
        var baseOpts = field.OptionsFactory is not null
            ? field.OptionsFactory(this)
            : (field.Options ?? []);
        if (_createdOptions.TryGetValue(field.Key, out var extra) && extra.Count > 0)
        {
            return baseOpts.Concat(extra).ToList();
        }
        return baseOpts;
    }

    internal FormOptionItem? CreateFieldOption(FieldNode field, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || field.OnCreateOption is null) return null;
        var created = field.OnCreateOption(label.Trim());
        if (created is null) return null;
        if (!_createdOptions.TryGetValue(field.Key, out var list))
        {
            list = [];
            _createdOptions[field.Key] = list;
        }
        if (list.All(o => o.Value != created.Value))
        {
            list.Add(created);
        }
        if (field.Multiple)
        {
            var cur = GetValue<IReadOnlyList<string>>(field.Key)?.ToList() ?? [];
            if (!cur.Contains(created.Value, StringComparer.Ordinal))
            {
                cur.Add(created.Value);
                SetValue(field.Key, cur, markTouched: true);
            }
        }
        else
        {
            SetValue(field.Key, created.Value, markTouched: true);
        }
        return created;
    }

    // Array item management
    internal IReadOnlyList<Dictionary<string, object?>> GetArrayItems(string arrayKey)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list)
        {
            return list;
        }
        return [];
    }

    internal void AddArrayItem(FieldNode arrayField)
    {
        ArgumentNullException.ThrowIfNull(arrayField);
        if (!_state.TryGetValue(arrayField.Key, out var val) || val is not List<Dictionary<string, object?>> list)
        {
            list = [];
            _state[arrayField.Key] = list;
        }
        var item = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (arrayField.Fields is not null)
        {
            foreach (var child in arrayField.Fields)
            {
                item[child.Key] = child.DefaultFactory is not null ? child.DefaultFactory(this) : child.DefaultValue;
            }
        }
        list.Add(item);
        AfterListChanged(arrayField, list);
        Changed?.Invoke();
    }

    internal void RemoveArrayItem(string arrayKey, int index)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
            ShiftCellState(arrayKey, index);
            if (FindField(arrayKey) is { } arrayField)
            {
                AfterListChanged(arrayField, list);
            }
            Changed?.Invoke();
        }
    }

    internal void SetArrayItemValue(string arrayKey, int index, string childKey, object? value, bool markTouched = false)
    {
        if (_state.TryGetValue(arrayKey, out var val) && val is List<Dictionary<string, object?>> list
            && index >= 0 && index < list.Count)
        {
            list[index][childKey] = value;
            var arrayField = FindField(arrayKey);
            var child = arrayField?.Fields?.FirstOrDefault(f => f.Key == childKey);
            if (arrayField is not null && child is not null)
            {
                var cell = new CellKey(arrayKey, index, childKey);
                if (markTouched || child.Trigger == FormValidationTrigger.Input)
                {
                    _touchedCells.Add(cell);
                }
                if (child.Trigger == FormValidationTrigger.Submit)
                {
                    _cellErrors.Remove(cell);
                }
                else if (_touchedCells.Contains(cell))
                {
                    ValidateCell(arrayField, child, index);
                }
                child.Watch?.Invoke(value, this);
                AfterListChanged(arrayField, list);
            }
            Changed?.Invoke();
        }
    }

    internal void MarkArrayItemTouched(string arrayKey, int index, string childKey)
    {
        var arrayField = FindField(arrayKey);
        var child = arrayField?.Fields?.FirstOrDefault(f => f.Key == childKey);
        if (arrayField is null || child is null || index < 0 || index >= GetArrayItems(arrayKey).Count) return;
        _touchedCells.Add(new CellKey(arrayKey, index, childKey));
        if (child.Trigger != FormValidationTrigger.Submit)
        {
            ValidateCell(arrayField, child, index);
        }
        Changed?.Invoke();
    }

    internal string? GetArrayItemError(string arrayKey, int index, string childKey)
    {
        var cell = new CellKey(arrayKey, index, childKey);
        return _touchedCells.Contains(cell) && _cellErrors.TryGetValue(cell, out var err) && IsCellInEffect(ActiveCellColumns(), cell) ? err : null;
    }

    internal static string CellPath(string arrayKey, int index, string childKey) =>
        string.Create(CultureInfo.InvariantCulture, $"{arrayKey}[{index}].{childKey}");

    internal bool Validate(bool currentStepOnly = false)
    {
        if (Schema.ValidateMode == FormValidationMode.None)
        {
            _validationErrors.Clear();
            _cellErrors.Clear();
            Changed?.Invoke();
            return !_customErrors.Values.Any(e => e.Blocking);
        }

        var active = ActiveFields;
        var activeKeys = active.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var key in _validationErrors.Keys.Where(k => !activeKeys.Contains(k)).ToList())
        {
            _validationErrors.Remove(key);
        }
        foreach (var cell in _cellErrors.Keys.Where(c => !activeKeys.Contains(c.Array)).ToList())
        {
            _cellErrors.Remove(cell);
        }

        IReadOnlyList<FieldNode> fieldsToValidate = active;
        if (currentStepOnly && CurrentStep is not null)
        {
            var stepFields = new List<FieldNode>();
            CollectActive(CurrentStep.Fields, stepFields);
            fieldsToValidate = stepFields;
        }

        foreach (var field in fieldsToValidate)
        {
            _touchedPaths.Add(field.Key);
            ValidateFieldCore(field);
            if (field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
            {
                ValidateArrayCells(field);
            }
        }

        Changed?.Invoke();
        var scopeKeys = fieldsToValidate.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        var hasErrors = fieldsToValidate.Any(f =>
            _validationErrors.ContainsKey(f.Key)
            || _cellErrors.Keys.Any(c => c.Array == f.Key))
            || _customErrors.Any(e => e.Value.Blocking && (IsFormLevelKey(e.Key) || scopeKeys.Contains(BaseKey(e.Key))));
        return !hasErrors;
    }

    private void ValidateField(FieldNode field)
    {
        _validationErrors.Remove(field.Key);
        if (!ActiveFields.Contains(field)) return;
        ValidateFieldCore(field);
    }

    private void ValidateFieldCore(FieldNode field)
    {
        _validationErrors.Remove(field.Key);
        if (CheckValue(field, GetValue(field.Key)) is { } error)
        {
            _validationErrors[field.Key] = error;
        }
    }

    private void ValidateArrayCells(FieldNode arrayField)
    {
        foreach (var cell in _cellErrors.Keys.Where(c => c.Array == arrayField.Key).ToList())
        {
            _cellErrors.Remove(cell);
        }
        var rows = GetArrayItems(arrayField.Key);
        var children = CellFields(arrayField);
        for (var i = 0; i < rows.Count; i++)
        {
            foreach (var child in children)
            {
                var cell = new CellKey(arrayField.Key, i, child.Key);
                _touchedCells.Add(cell);
                if (CheckValue(child, rows[i].GetValueOrDefault(child.Key), withAsync: false) is { } error)
                {
                    _cellErrors[cell] = error;
                }
            }
        }
    }

    private void ValidateCell(FieldNode arrayField, FieldNode child, int index)
    {
        var cell = new CellKey(arrayField.Key, index, child.Key);
        _cellErrors.Remove(cell);
        if (!ActiveFields.Contains(arrayField) || !CellFields(arrayField).Contains(child)) return;
        var rows = GetArrayItems(arrayField.Key);
        if (index < 0 || index >= rows.Count) return;
        if (CheckValue(child, rows[index].GetValueOrDefault(child.Key), withAsync: false) is { } error)
        {
            _cellErrors[cell] = error;
        }
    }

    // After a value changes, validate again the touched fields that show an error (so stale errors clear and HasBlockingErrors is right)
    // and the touched fields that declare the changed key in DependsOn (so a new mismatch shows). The changed field itself is left alone.
    private void RevalidateDependents(string changedKey)
    {
        if (Schema.ValidateMode == FormValidationMode.None) return;
        var hasCellErrors = _cellErrors.Count > 0;
        foreach (var field in ActiveFields)
        {
            var isList = field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable;
            if (field.Key != changedKey && field.Trigger != FormValidationTrigger.Submit && _touchedPaths.Contains(field.Key)
                && (_validationErrors.ContainsKey(field.Key) || DependsOnKey(field, changedKey)))
            {
                ValidateFieldCore(field);
            }
            if (!isList) continue;
            var rows = GetArrayItems(field.Key);
            foreach (var child in CellFields(field))
            {
                if (child.Trigger == FormValidationTrigger.Submit) continue;
                var dependent = DependsOnKey(child, changedKey);
                if (!dependent && !hasCellErrors) continue;
                for (var row = 0; row < rows.Count; row++)
                {
                    var cell = new CellKey(field.Key, row, child.Key);
                    if (_touchedCells.Contains(cell) && (dependent || _cellErrors.ContainsKey(cell)))
                    {
                        ValidateCell(field, child, row);
                    }
                }
            }
        }
    }

    private static bool DependsOnKey(FieldNode field, string key) =>
        field.DependsOn is { Count: > 0 } deps && deps.Contains(key, StringComparer.Ordinal);

    // The visible columns of every repeatable list in effect, by list key.
    private Dictionary<string, HashSet<string>> ActiveCellColumns() =>
        ActiveFields.Where(f => f.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
            .GroupBy(f => f.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.SelectMany(CellFields).Select(c => c.Key).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);

    private bool IsCellInEffect(Dictionary<string, HashSet<string>> columns, CellKey cell) =>
        columns.TryGetValue(cell.Array, out var children) && children.Contains(cell.Child)
        && cell.Row >= 0 && cell.Row < GetArrayItems(cell.Array).Count;

    // The children of a repeatable list that are columns now: visible, stateful leaves.
    private List<FieldNode> CellFields(FieldNode arrayField) =>
        (arrayField.Fields ?? []).Where(c => ShouldRenderField(c) && !IsStateless(c.Kind) && !IsContainer(c.Kind)).ToList();

    private void AfterListChanged(FieldNode arrayField, List<Dictionary<string, object?>> list)
    {
        if (arrayField.Trigger == FormValidationTrigger.Submit)
        {
            _validationErrors.Remove(arrayField.Key);
        }
        else if (_touchedPaths.Contains(arrayField.Key))
        {
            ValidateField(arrayField);
        }
        arrayField.Watch?.Invoke(list, this);
        RevalidateDependents(arrayField.Key);
    }

    private void RemoveCellState(string arrayKey)
    {
        _cellErrors.Keys.Where(c => c.Array == arrayKey).ToList().ForEach(c => _cellErrors.Remove(c));
        _touchedCells.RemoveWhere(c => c.Array == arrayKey);
    }

    private void ShiftCellState(string arrayKey, int removedRow)
    {
        var errors = _cellErrors.Where(e => e.Key.Array == arrayKey).ToList();
        var touched = _touchedCells.Where(c => c.Array == arrayKey).ToList();
        RemoveCellState(arrayKey);
        foreach (var (cell, msg) in errors)
        {
            if (cell.Row != removedRow) _cellErrors[cell.Row > removedRow ? cell with { Row = cell.Row - 1 } : cell] = msg;
        }
        foreach (var cell in touched)
        {
            if (cell.Row != removedRow) _touchedCells.Add(cell.Row > removedRow ? cell with { Row = cell.Row - 1 } : cell);
        }
    }

    // The first failing check of a value against the rules of a field, or null. The result of an asynchronous rule counts once it is known for this value.
    private string? CheckValue(FieldNode field, object? val, bool withAsync = true) =>
        CheckValueSync(field, val) ?? (withAsync ? CachedAsyncError(field, val) : null);

    private string? CheckValueSync(FieldNode field, object? val)
    {
        if (Schema.ValidateMode == FormValidationMode.None) return null;
        var isEmpty = IsEmptyValue(val);

        if (Schema.ValidateMode is FormValidationMode.All or FormValidationMode.RequiredOnly
            && IsFieldRequired(field) && IsMissingValue(field, val))
        {
            return field.RequiredMessage ?? Messages.Required;
        }

        if (Schema.ValidateMode == FormValidationMode.RequiredOnly) return null;

        // Number bounds
        if (field.Kind == FormFieldKind.Number && !isEmpty && TryConvertDouble(val, out var num))
        {
            if (field.Min is { } min && num < min)
            {
                return Messages.MinValue(min);
            }
            if (field.Max is { } max && num > max)
            {
                return Messages.MaxValue(max);
            }
        }

        // Text length
        if (field.MaxLength is { } maxLength && val is string text && text.Length > maxLength)
        {
            return Messages.MaxLength(maxLength);
        }

        // Date range order
        if (field.Kind == FormFieldKind.DateRange && val is IReadOnlyList<string> { Count: 2 } range
            && DateOnly.TryParseExact(range[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            && DateOnly.TryParseExact(range[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            && end < start)
        {
            return Messages.DateRangeOrder;
        }

        // Password requirements
        if (field.Kind == FormFieldKind.Password && !isEmpty && field.PasswordRequirements is { Count: > 0 })
        {
            var str = val?.ToString() ?? "";
            foreach (var req in field.PasswordRequirements)
            {
                if (!req.Validate(str))
                {
                    return req.Label;
                }
            }
        }

        // Files
        if (field.Kind == FormFieldKind.File && !isEmpty && CheckFiles(field, val) is { } fileError)
        {
            return fileError;
        }

        // Custom validators
        if (field.Validators is { Count: > 0 })
        {
            foreach (var rule in field.Validators)
            {
                if (!rule.Validate(val, this))
                {
                    return rule.LocalizedMessage?.Invoke(Messages) ?? rule.Message;
                }
            }
        }
        return null;
    }

    private string? CheckFiles(FieldNode field, object? val)
    {
        var files = val switch
        {
            FormFileValue single => [single],
            IEnumerable<FormFileValue> many => many.ToList(),
            _ => new List<FormFileValue>(),
        };
        if (!field.Multiple && files.Count > 1) return Messages.SingleFileOnly;
        foreach (var file in files)
        {
            if (!file.Matches(field.Accept)) return Messages.FileTypeNotAllowed(file.Name, field.Accept!);
            var max = field.MaxFileSize ?? FormFileValue.DefaultMaxSize;
            if (file.Size > max) return Messages.FileTooLarge(file.Name, Messages.FileSize(max));
        }
        return null;
    }

    internal IReadOnlyDictionary<string, object?> BuildOutput()
    {
        var output = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in ActiveFields)
        {
            if (field.OmitFromOutput)
            {
                continue;
            }
            var val = CloneValue(GetValue(field.Key));
            if (field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable
                && field.Fields is { Count: > 0 } columns
                && val is List<Dictionary<string, object?>> rows)
            {
                // Hidden and omitted columns leave the rows, like hidden and omitted fields leave the output.
                var dropped = columns
                    .Where(c => c.OmitFromOutput || c.Condition?.Invoke(this) == false)
                    .Select(c => c.Key)
                    .ToHashSet(StringComparer.Ordinal);
                if (dropped.Count > 0)
                {
                    foreach (var row in rows)
                    {
                        foreach (var key in dropped) row.Remove(key);
                    }
                }
            }
            if (field.TransformOutput is not null)
            {
                val = field.TransformOutput(val);
            }
            output[field.Key] = val;
        }
        return output;
    }

    internal async Task<bool> NextStepAsync()
    {
        if (!IsStepped || IsLastStep || IsSubmitting || _navigating) return false;
        _navigating = true;
        try
        {
            var valid = HasAsyncWork(currentStepOnly: true)
                ? await ValidateAsync(currentStepOnly: true)
                : Validate(currentStepOnly: true);
            if (!valid)
            {
                SubmitFailed = true;
                SubmitFailureCount++;
                Changed?.Invoke();
                return false;
            }

            if (Schema.OnBeforeNext is not null)
            {
                var ok = await Schema.OnBeforeNext(this, CurrentStepIndex);
                if (!ok) return false;
            }

            var nextIdx = CurrentStepIndex + 1;
            while (nextIdx < Schema.Steps.Count && IsStepSkipped(nextIdx))
            {
                nextIdx++;
            }
            if (nextIdx >= Schema.Steps.Count) return false;

            CurrentStepIndex = nextIdx;
            ClearValidationState();
            SubmitFailed = false;
            Changed?.Invoke();
            return true;
        }
        finally
        {
            _navigating = false;
        }
    }

    internal async Task<bool> PreviousStepAsync()
    {
        if (!IsStepped || IsFirstStep || IsSubmitting || _navigating) return false;
        _navigating = true;
        try
        {
            if (Schema.OnBeforePrevious is not null)
            {
                var ok = await Schema.OnBeforePrevious(this, CurrentStepIndex);
                if (!ok) return false;
            }

            var prevIdx = CurrentStepIndex - 1;
            while (prevIdx >= 0 && IsStepSkipped(prevIdx))
            {
                prevIdx--;
            }
            if (prevIdx < 0) return false;

            CurrentStepIndex = prevIdx;
            ClearValidationState();
            SubmitFailed = false;
            Changed?.Invoke();
            return true;
        }
        finally
        {
            _navigating = false;
        }
    }

    internal Task<bool> SubmitAsync() => SubmitCoreAsync(null, CancellationToken.None);

    internal Task<bool> SubmitAsync(Func<FormEngine, CancellationToken, Task<bool>> handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubmitCoreAsync(handler, cancellationToken);
    }

    private async Task<bool> SubmitCoreAsync(Func<FormEngine, CancellationToken, Task<bool>>? handler, CancellationToken cancellationToken)
    {
        if (IsSubmitting || _navigating) return false;
        // Waiting for an asynchronous rule is part of the submission: a second submit during that wait must be refused too.
        var waits = HasAsyncWork(currentStepOnly: false);
        if (waits)
        {
            IsSubmitting = true;
            Changed?.Invoke();
        }
        var succeeded = false;
        try
        {
            var valid = waits ? await ValidateAsync(currentStepOnly: false, cancellationToken) : Validate(currentStepOnly: false);
            if (!valid) return false;

            if (!IsSubmitting)
            {
                IsSubmitting = true;
                Changed?.Invoke();
            }

            if (Schema.OnBeforeSubmit is not null)
            {
                var allowed = await Schema.OnBeforeSubmit(this);
                if (!allowed) return false;
            }

            if (Schema.OnSubmit is not null)
            {
                await Schema.OnSubmit(this);
            }

            var submitted = CloneState();
            if (handler is not null && !await handler(this, cancellationToken))
            {
                return false;
            }

            // Rebaseline dirty snapshot on success, with what the handler was given and not with what the state became since.
            RetakeSnapshot(submitted);
            succeeded = true;
            return true;
        }
        finally
        {
            IsSubmitting = false;
            SubmitFailed = !succeeded;
            if (!succeeded) SubmitFailureCount++;
            Changed?.Invoke();
        }
    }

    private Dictionary<string, object?> CloneState()
    {
        var copy = new Dictionary<string, object?>(_state.Count, StringComparer.Ordinal);
        foreach (var (k, v) in _state)
        {
            copy[k] = CloneValue(v);
        }
        return copy;
    }

    private void RetakeSnapshot(Dictionary<string, object?> values)
    {
        _initialSnapshot.Clear();
        foreach (var (k, v) in values)
        {
            _initialSnapshot[k] = v;
        }
    }

    internal void Reset(IReadOnlyDictionary<string, object?>? newInput = null)
    {
        ClearValidationState();
        _customErrors.Clear();
        SubmitFailed = false;
        SubmitFailureCount = 0;
        CancelAllPendingValidations();
        _asyncResults.Clear();
        CurrentStepIndex = 0;
        InitializeState(newInput);
        CurrentStepIndex = FirstReachableStepIndex();
        Changed?.Invoke();
    }

    internal IReadOnlyList<FormPageSectionState> GetSectionStates()
    {
        var result = new List<FormPageSectionState>();
        var dirtySet = DirtyPaths.ToHashSet(StringComparer.Ordinal);
        var columns = ActiveCellColumns();
        foreach (var section in Schema.Sections.Where(IsSectionVisible))
        {
            var leaves = new List<FieldNode>();
            CollectActive(section.Fields, leaves);
            var reqCount = 0;
            var missingCount = 0;
            var hasError = false;
            var isDirty = false;
            var hasProvided = false;

            foreach (var leaf in leaves)
            {
                var val = GetValue(leaf.Key);
                var empty = IsEmptyValue(val);
                if (!empty) hasProvided = true;
                if (IsFieldRequired(leaf))
                {
                    reqCount++;
                    if (IsMissingValue(leaf, val)) missingCount++;
                }
                if (_validationErrors.ContainsKey(leaf.Key) || _customErrors.ContainsKey(leaf.Key)
                    || _cellErrors.Keys.Any(c => c.Array == leaf.Key && IsCellInEffect(columns, c)))
                {
                    hasError = true;
                }
                if (dirtySet.Contains(leaf.Key))
                {
                    isDirty = true;
                }
            }

            var status = hasError ? FormPageSectionStatus.Invalid
                : missingCount > 0 ? FormPageSectionStatus.Pending
                : (section.Optional && !hasProvided) ? FormPageSectionStatus.Optional
                : FormPageSectionStatus.Complete;

            result.Add(new FormPageSectionState(
                section.Key,
                section.Label,
                section.Description,
                section.Optional,
                reqCount,
                missingCount,
                isDirty,
                status));
        }
        return result;
    }

    private void InitializeState(IReadOnlyDictionary<string, object?>? input)
    {
        _state.Clear();
        _initialSnapshot.Clear();

        if (input is not null)
        {
            foreach (var (k, v) in input)
            {
                _state[k] = CloneValue(v);
            }
        }

        foreach (var field in AllFields)
        {
            if (field.Ignore || IsStateless(field.Kind)) continue;
            if (_state.TryGetValue(field.Key, out var existing) && existing is not null)
            {
                if (field.TransformInput is not null)
                {
                    _state[field.Key] = field.TransformInput(existing);
                }
            }
            else
            {
                var def = field.DefaultFactory is not null ? field.DefaultFactory(this) : field.DefaultValue;
                if (def is null && field.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable)
                {
                    def = new List<Dictionary<string, object?>>();
                }
                _state[field.Key] = CloneValue(def);
            }
        }

        foreach (var (k, v) in _state)
        {
            _initialSnapshot[k] = CloneValue(v);
        }
    }

    // Puts every value back to the baseline snapshot (the creation, the last Reset with a state, or the last successful submission) and clears errors and
    // touched state. Unlike Reset(input) it does not run TransformInput again on values that were transformed once already.
    internal void ResetToSnapshot()
    {
        ClearValidationState();
        _customErrors.Clear();
        SubmitFailed = false;
        SubmitFailureCount = 0;
        CancelAllPendingValidations();
        _asyncResults.Clear();
        _state.Clear();
        foreach (var (k, v) in _initialSnapshot) _state[k] = CloneValue(v);
        CurrentStepIndex = FirstReachableStepIndex();
        Changed?.Invoke();
    }

    private void ClearValidationState()
    {
        _validationErrors.Clear();
        _touchedPaths.Clear();
        _cellErrors.Clear();
        _touchedCells.Clear();
    }

    private bool IsStepSkipped(int index) =>
        Schema.Steps[index].SkipWhen?.Invoke(this) == true;

    private int FirstReachableStepIndex()
    {
        for (var i = 0; i < Schema.Steps.Count; i++)
        {
            if (!IsStepSkipped(i)) return i;
        }
        return 0;
    }

    private FieldNode? FindField(string key) =>
        AllFields.FirstOrDefault(f => f.Key == key);

    private void CollectActive(IEnumerable<FieldNode> fields, List<FieldNode> result)
    {
        foreach (var field in fields)
        {
            if (!ShouldRenderField(field)) continue;
            if (field.Kind == FormFieldKind.Tabs)
            {
                foreach (var tab in field.Tabs ?? [])
                {
                    if (tab.Condition is null || tab.Condition(this)) CollectActive(tab.Fields, result);
                }
            }
            else if (IsContainer(field.Kind))
            {
                CollectActive(field.Fields ?? [], result);
            }
            else if (!IsStateless(field.Kind))
            {
                result.Add(field);
            }
        }
    }

    private static IEnumerable<FieldNode> FlattenFields(IEnumerable<FieldNode> fields)
    {
        foreach (var field in fields)
        {
            if (field.Ignore) continue;
            if (field.Kind == FormFieldKind.Tabs)
            {
                foreach (var tab in field.Tabs ?? [])
                {
                    foreach (var child in FlattenFields(tab.Fields))
                    {
                        yield return child;
                    }
                }
            }
            else if (IsContainer(field.Kind))
            {
                foreach (var child in FlattenFields(field.Fields ?? []))
                {
                    yield return child;
                }
            }
            else
            {
                yield return field;
            }
        }
    }

    internal static bool IsStateless(FormFieldKind kind) =>
        kind is FormFieldKind.Info or FormFieldKind.Divider or FormFieldKind.Section or FormFieldKind.Button;

    internal static bool IsContainer(FormFieldKind kind) =>
        kind is FormFieldKind.Group or FormFieldKind.Card or FormFieldKind.Object or FormFieldKind.Tabs;

    internal static bool IsEmptyValue(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        System.Collections.ICollection c => c.Count == 0,
        IEnumerable<object?> e => !e.Any(),
        _ => false,
    };

    // A required checkbox or switch is a consent: it is satisfied only when checked. A required date range needs both dates.
    private static bool IsMissingValue(FieldNode field, object? value) =>
        IsEmptyValue(value)
        || (field.Kind is FormFieldKind.Checkbox or FormFieldKind.Switch && value is not true)
        || (field.Kind == FormFieldKind.DateRange && (value is not IReadOnlyList<string> { Count: 2 } range || range.Any(string.IsNullOrWhiteSpace)));

    private static bool TryConvertDouble(object? val, out double d)
    {
        switch (val)
        {
            case double dbl:
                d = dbl;
                return true;
            case int i:
                d = i;
                return true;
            case long l:
                d = l;
                return true;
            case decimal m:
                d = (double)m;
                return true;
            default:
                return double.TryParse(val?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }
    }

    private static object? CloneValue(object? value)
    {
        if (value is List<Dictionary<string, object?>> dictList)
        {
            return dictList.Select(d => new Dictionary<string, object?>(d, StringComparer.Ordinal)).ToList();
        }
        if (value is IReadOnlyList<string> strList)
        {
            return strList.ToList();
        }
        if (value is IReadOnlyList<FormFileValue> fileList)
        {
            return fileList.ToList();
        }
        return value;
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a is IReadOnlyList<string> la && b is IReadOnlyList<string> lb)
        {
            return la.SequenceEqual(lb, StringComparer.Ordinal);
        }
        if (a is IReadOnlyList<FormFileValue> fa && b is IReadOnlyList<FormFileValue> fb)
        {
            return fa.SequenceEqual(fb);
        }
        if (a is List<Dictionary<string, object?>> da && b is List<Dictionary<string, object?>> db)
        {
            if (da.Count != db.Count) return false;
            for (var i = 0; i < da.Count; i++)
            {
                if (da[i].Count != db[i].Count) return false;
                foreach (var (k, va) in da[i])
                {
                    if (!db[i].TryGetValue(k, out var vb) || !Equals(va, vb)) return false;
                }
            }
            return true;
        }
        return Equals(a, b);
    }
}

