namespace Klyroo.Form;

/// <summary>A mutable copy of the settings of a <see cref="FormFieldSchema"/>, which has init-only properties. A field declared from a model starts with the settings its member type gives it, and <see cref="FormModelBuilder{TModel}"/> hands this object to the code that adjusts them (<see cref="FormFieldConvention"/>, <c>Configure</c>). The properties have the meaning and the defaults of the same-named properties of <see cref="FormFieldSchema"/>.</summary>
public sealed class FormFieldSettings
{
    internal FormFieldSettings(string key) => Key = key;

    /// <summary>Key of the field. It is derived from the member and cannot change.</summary>
    public string Key { get; }

    // What an empty input becomes for a string member (true: null, false: an empty string), or null to follow the nullable annotation of the member.
    internal bool? NullWhenEmpty { get; set; }

    /// <summary>See <see cref="FormFieldSchema.Kind"/>.</summary>
    public FormFieldKind Kind { get; set; } = FormFieldKind.Text;
    /// <summary>See <see cref="FormFieldSchema.Label"/>.</summary>
    public string? Label { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Description"/>.</summary>
    public string? Description { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Hint"/>.</summary>
    public string? Hint { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Help"/>.</summary>
    public string? Help { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Placeholder"/>.</summary>
    public string? Placeholder { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DefaultValue"/>.</summary>
    public object? DefaultValue { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DefaultFactory"/>.</summary>
    public Func<FormController, object?>? DefaultFactory { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Required"/>.</summary>
    public bool Required { get; set; }
    /// <summary>See <see cref="FormFieldSchema.RequiredWhen"/>.</summary>
    public Func<FormController, bool>? RequiredWhen { get; set; }
    /// <summary>See <see cref="FormFieldSchema.RequiredMessage"/>.</summary>
    public string? RequiredMessage { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Trigger"/>.</summary>
    public FormValidationTrigger Trigger { get; set; } = FormValidationTrigger.Blur;
    /// <summary>See <see cref="FormFieldSchema.DependsOn"/>.</summary>
    public IReadOnlyList<string>? DependsOn { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Disabled"/>.</summary>
    public bool Disabled { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DisabledWhen"/>.</summary>
    public Func<FormController, bool>? DisabledWhen { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DisabledReason"/>.</summary>
    public string? DisabledReason { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Condition"/>.</summary>
    public Func<FormController, bool>? Condition { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Ignore"/>.</summary>
    public bool Ignore { get; set; }
    /// <summary>See <see cref="FormFieldSchema.OmitFromOutput"/>.</summary>
    public bool OmitFromOutput { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DirtyCheck"/>.</summary>
    public bool DirtyCheck { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Collapsible"/>.</summary>
    public bool Collapsible { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DefaultCollapsed"/>.</summary>
    public bool DefaultCollapsed { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Span"/>.</summary>
    public int Span { get; set; } = 12;
    /// <summary>See <see cref="FormFieldSchema.Min"/>.</summary>
    public double? Min { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Max"/>.</summary>
    public double? Max { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Step"/>.</summary>
    public double? Step { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Accept"/>.</summary>
    public string? Accept { get; set; }
    /// <summary>See <see cref="FormFieldSchema.MaxFileSize"/>.</summary>
    public long? MaxFileSize { get; set; }
    /// <summary>See <see cref="FormFieldSchema.CustomKey"/>.</summary>
    public string? CustomKey { get; set; }
    /// <summary>See <see cref="FormFieldSchema.MaxLength"/>.</summary>
    public int? MaxLength { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Multiple"/>.</summary>
    public bool Multiple { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Clearable"/>.</summary>
    public bool Clearable { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Options"/>.</summary>
    public IReadOnlyList<FormOptionItem>? Options { get; set; }
    /// <summary>See <see cref="FormFieldSchema.OptionsFactory"/>.</summary>
    public Func<FormController, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; set; }
    /// <summary>See <see cref="FormFieldSchema.OnCreateOption"/>.</summary>
    public Func<string, FormOptionItem?>? OnCreateOption { get; set; }
    /// <summary>See <see cref="FormFieldSchema.PasswordRequirements"/>.</summary>
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Validators"/>.</summary>
    public IReadOnlyList<FormValidatorRule>? Validators { get; set; }
    /// <summary>See <see cref="FormFieldSchema.AsyncValidators"/>.</summary>
    public IReadOnlyList<FormAsyncValidatorRule>? AsyncValidators { get; set; }
    /// <summary>See <see cref="FormFieldSchema.TransformInput"/>.</summary>
    public Func<object?, object?>? TransformInput { get; set; }
    /// <summary>See <see cref="FormFieldSchema.TransformOutput"/>.</summary>
    public Func<object?, object?>? TransformOutput { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Watch"/>.</summary>
    public Action<object?, FormController>? Watch { get; set; }
    /// <summary>See <see cref="FormFieldSchema.OnClick"/>.</summary>
    public Func<FormController, Task>? OnClick { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DisplayKind"/>.</summary>
    public string? DisplayKind { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DisplayText"/>.</summary>
    public string? DisplayText { get; set; }
    /// <summary>See <see cref="FormFieldSchema.DisplayTone"/>.</summary>
    public string? DisplayTone { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Fields"/>.</summary>
    public IReadOnlyList<FormFieldSchema>? Fields { get; set; }
    /// <summary>See <see cref="FormFieldSchema.Tabs"/>.</summary>
    public IReadOnlyList<FormTabItemSchema>? Tabs { get; set; }
    /// <summary>See <see cref="FormFieldSchema.AddItemLabel"/>.</summary>
    public string? AddItemLabel { get; set; }

    /// <summary>Builds the schema of the field from the current settings.</summary>
    internal FormFieldSchema ToSchema() => new()
    {
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
