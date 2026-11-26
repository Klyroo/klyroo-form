using System.Runtime.CompilerServices;

namespace Klyroo.Form.Dynamic;

// A dynamic schema compiled for the engine. The compilation is cached per schema (a schema is immutable data), so the nodes a controller works on are
// the same objects for every controller of the schema, and a public field can be mapped to its node by reference.
internal sealed class CompiledDynamic
{
    public required FormDefinition Definition { get; init; }

    public required Dictionary<object, object> Nodes { get; init; }

    public FieldNode Node(DynamicFieldSchema field) => (FieldNode?)Nodes.GetValueOrDefault(field) ?? throw new ArgumentException($"The field '{field.Key}' does not belong to the schema of this controller.", nameof(field));

    public SectionNode Node(DynamicSectionSchema section) => (SectionNode?)Nodes.GetValueOrDefault(section) ?? throw new ArgumentException($"The section '{section.Key}' does not belong to the schema of this controller.", nameof(section));
}

internal static class DynamicCompiler
{
    private static readonly ConditionalWeakTable<DynamicFormSchema, CompiledDynamic> Cache = new();

    public static CompiledDynamic Compile(DynamicFormSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return Cache.GetValue(schema, Build);
    }

    private static DynamicFormController Owner(FormEngine engine) => (DynamicFormController)engine.Owner!;

    private static CompiledDynamic Build(DynamicFormSchema schema)
    {
        var nodes = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        var definition = new FormDefinition
        {
            Source = schema,
            FormKey = schema.FormKey,
            Title = schema.Title,
            Description = schema.Description,
            Eyebrow = schema.Eyebrow,
            Columns = schema.Columns,
            ShowStepper = schema.ShowStepper,
            ValidateMode = schema.ValidateMode,
            Messages = schema.Messages,
            IgnoreDirtyPaths = schema.IgnoreDirtyPaths,
            Fields = Fields(schema.Fields, nodes),
            Steps = schema.Steps.Select(s => Step(s, nodes)).ToList(),
            Sections = schema.Sections.Select(s => Section(s, nodes)).ToList(),
            OnBeforeSubmit = Adapt(schema.OnBeforeSubmit),
            OnSubmit = Adapt(schema.OnSubmit),
            OnBeforeNext = schema.OnBeforeNext is { } next ? (e, i) => next(Owner(e), i) : null,
            OnBeforePrevious = schema.OnBeforePrevious is { } previous ? (e, i) => previous(Owner(e), i) : null,
        };
        return new CompiledDynamic { Definition = definition, Nodes = nodes };
    }

    private static Func<FormEngine, T>? Adapt<T>(Func<DynamicFormController, T>? f) => f is null ? null : e => f(Owner(e));

    private static List<FieldNode> Fields(IReadOnlyList<DynamicFieldSchema>? fields, Dictionary<object, object> nodes) =>
        (fields ?? []).Select(f => Field(f, nodes)).ToList();

    private static StepNode Step(DynamicStepSchema step, Dictionary<object, object> nodes) => new()
    {
        Source = step,
        Key = step.Key,
        Title = step.Title,
        Description = step.Description,
        Fields = Fields(step.Fields, nodes),
        SkipWhen = Adapt(step.SkipWhen),
    };

    private static SectionNode Section(DynamicSectionSchema section, Dictionary<object, object> nodes)
    {
        var node = new SectionNode
        {
            Source = section,
            Key = section.Key,
            Label = section.Label,
            Description = section.Description,
            Optional = section.Optional,
            Span = section.Span,
            Condition = Adapt(section.Condition),
            Fields = Fields(section.Fields, nodes),
        };
        nodes[section] = node;
        return node;
    }

    private static TabNode Tab(DynamicTabSchema tab, Dictionary<object, object> nodes) => new()
    {
        Source = tab,
        Key = tab.Key,
        Label = tab.Label,
        Fields = Fields(tab.Fields, nodes),
        Condition = Adapt(tab.Condition),
    };

    private static FieldNode Field(DynamicFieldSchema f, Dictionary<object, object> nodes)
    {
        var node = new FieldNode
        {
            Source = f,
            Key = f.Key,
            Kind = f.Kind,
            Label = f.Label,
            Description = f.Description,
            Hint = f.Hint,
            Help = f.Help,
            Placeholder = f.Placeholder,
            DefaultValue = f.DefaultValue,
            DefaultFactory = Adapt(f.DefaultFactory),
            Required = f.Required,
            RequiredWhen = Adapt(f.RequiredWhen),
            RequiredMessage = f.RequiredMessage,
            Trigger = f.Trigger,
            DependsOn = f.DependsOn,
            Disabled = f.Disabled,
            DisabledWhen = Adapt(f.DisabledWhen),
            DisabledReason = f.DisabledReason,
            Condition = Adapt(f.Condition),
            Ignore = f.Ignore,
            OmitFromOutput = f.OmitFromOutput,
            DirtyCheck = f.DirtyCheck,
            Collapsible = f.Collapsible,
            DefaultCollapsed = f.DefaultCollapsed,
            Span = f.Span,
            Min = f.Min,
            Max = f.Max,
            Step = f.Step,
            Accept = f.Accept,
            MaxFileSize = f.MaxFileSize,
            CustomKey = f.CustomKey,
            MaxLength = f.MaxLength,
            Multiple = f.Multiple,
            Clearable = f.Clearable,
            Options = f.Options,
            OptionsFactory = Adapt(f.OptionsFactory),
            OnCreateOption = f.OnCreateOption,
            PasswordRequirements = f.PasswordRequirements,
            Validators = f.Validators?.Select(Rule).ToList(),
            AsyncValidators = f.AsyncValidators?.Select(AsyncRule).ToList(),
            TransformInput = f.TransformInput,
            TransformOutput = f.TransformOutput,
            Watch = f.Watch is { } watch ? (v, e) => watch(v, Owner(e)) : null,
            OnClick = Adapt(f.OnClick),
            DisplayKind = f.DisplayKind,
            DisplayText = f.DisplayText,
            DisplayTone = f.DisplayTone,
            Fields = f.Fields is null ? null : Fields(f.Fields, nodes),
            Tabs = f.Tabs?.Select(t => Tab(t, nodes)).ToList(),
            AddItemLabel = f.AddItemLabel,
        };
        nodes[f] = node;
        return node;
    }

    private static ValidatorRule Rule(DynamicValidatorRule rule) =>
        new(rule.Key, rule.Message, (v, e) => rule.Validate(v, Owner(e))) { LocalizedMessage = rule.LocalizedMessage };

    private static AsyncValidatorRule AsyncRule(DynamicAsyncValidatorRule rule) =>
        new(rule.Key, (v, e, ct) => rule.Validate(v, Owner(e), ct)) { Debounce = rule.Debounce };
}
