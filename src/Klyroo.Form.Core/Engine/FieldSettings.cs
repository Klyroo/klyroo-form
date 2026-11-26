namespace Klyroo.Form;

internal sealed class FieldSettings
{
    internal FieldSettings(string key) => Key = key;

    public string Key { get; }

    // What an empty input becomes for a string member (true: null, false: an empty string), or null to follow the nullable annotation of the member.
    internal bool? NullWhenEmpty { get; set; }

    internal Func<object?, string?>? ControlCheck { get; set; }

    internal JsonControlReader? JsonReader { get; set; }

    internal Type? ValueType { get; set; }

    internal Func<ControlBinding, FormControlContext>? ContextFactory { get; set; }

    // The codec of the field (a FormCodec of its value type), set once the kind is final and before the explicit settings run.
    internal object? Codec { get; set; }

    public FormFieldKind Kind { get; set; } = FormFieldKind.Text;
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? Hint { get; set; }
    public string? Help { get; set; }
    public string? Placeholder { get; set; }
    public object? DefaultValue { get; set; }
    public Func<FormEngine, object?>? DefaultFactory { get; set; }
    public bool Required { get; set; }
    public Func<FormEngine, bool>? RequiredWhen { get; set; }
    public string? RequiredMessage { get; set; }
    public FormValidationTrigger Trigger { get; set; } = FormValidationTrigger.Blur;
    public IReadOnlyList<string>? DependsOn { get; set; }
    public bool Disabled { get; set; }
    public Func<FormEngine, bool>? DisabledWhen { get; set; }
    public string? DisabledReason { get; set; }
    public Func<FormEngine, bool>? Condition { get; set; }
    public bool Ignore { get; set; }
    public bool OmitFromOutput { get; set; }
    public bool DirtyCheck { get; set; }
    public bool Collapsible { get; set; }
    public bool DefaultCollapsed { get; set; }
    public int Span { get; set; } = 12;
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Step { get; set; }
    public string? Accept { get; set; }
    public long? MaxFileSize { get; set; }
    public string? CustomKey { get; set; }
    public int? MaxLength { get; set; }
    public bool Multiple { get; set; }
    public bool Clearable { get; set; }
    public IReadOnlyList<FormOptionItem>? Options { get; set; }
    public Func<FormEngine, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; set; }
    public Func<string, FormOptionItem?>? OnCreateOption { get; set; }
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; set; }
    public IReadOnlyList<ValidatorRule>? Validators { get; set; }
    public IReadOnlyList<AsyncValidatorRule>? AsyncValidators { get; set; }
    public Func<object?, object?>? TransformInput { get; set; }
    public Func<object?, object?>? TransformOutput { get; set; }
    public Action<object?, FormEngine>? Watch { get; set; }
    public Func<FormEngine, Task>? OnClick { get; set; }
    public string? DisplayKind { get; set; }
    public string? DisplayText { get; set; }
    public string? DisplayTone { get; set; }
    public IReadOnlyList<FieldNode>? Fields { get; set; }
    public IReadOnlyList<TabNode>? Tabs { get; set; }
    public string? AddItemLabel { get; set; }

    internal FieldNode ToNode() => new()
    {
        ControlCheck = ControlCheck,
        JsonReader = JsonReader,
        ValueType = ValueType,
        ContextFactory = ContextFactory,
        Key = Key,
        Kind = Kind,
        Label = Label,
        Description = Description,
        Hint = Hint,
        Help = Help,
        Placeholder = Placeholder,
        DefaultValue = DefaultValue,
        DefaultFactory = DefaultFactory,
        Required = Required,
        RequiredWhen = RequiredWhen,
        RequiredMessage = RequiredMessage,
        Trigger = Trigger,
        DependsOn = DependsOn,
        Disabled = Disabled,
        DisabledWhen = DisabledWhen,
        DisabledReason = DisabledReason,
        Condition = Condition,
        Ignore = Ignore,
        OmitFromOutput = OmitFromOutput,
        DirtyCheck = DirtyCheck,
        Collapsible = Collapsible,
        DefaultCollapsed = DefaultCollapsed,
        Span = Span,
        Min = Min,
        Max = Max,
        Step = Step,
        Accept = Accept,
        MaxFileSize = MaxFileSize,
        CustomKey = CustomKey,
        MaxLength = MaxLength,
        Multiple = Multiple,
        Clearable = Clearable,
        Options = Options,
        OptionsFactory = OptionsFactory,
        OnCreateOption = OnCreateOption,
        PasswordRequirements = PasswordRequirements,
        Validators = Validators,
        AsyncValidators = AsyncValidators,
        TransformInput = TransformInput,
        TransformOutput = TransformOutput,
        Watch = Watch,
        OnClick = OnClick,
        DisplayKind = DisplayKind,
        DisplayText = DisplayText,
        DisplayTone = DisplayTone,
        Fields = Fields,
        Tabs = Tabs,
        AddItemLabel = AddItemLabel,
    };
}
