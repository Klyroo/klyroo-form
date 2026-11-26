using System.Text.Json;

namespace Klyroo.Form;

internal delegate bool JsonControlReader(JsonElement element, FieldNode field, IReadOnlyList<FormOptionItem> options, out object? control, out string? reason);

internal sealed record ValidatorRule(
    string Key,
    string Message,
    Func<object?, FormEngine, bool> Validate)
{
    public Func<FormMessages, string?>? LocalizedMessage { get; init; }
}

internal sealed record AsyncValidatorRule(
    string Key,
    Func<object?, FormEngine, CancellationToken, ValueTask<string?>> Validate)
{
    public TimeSpan Debounce { get; init; } = TimeSpan.FromMilliseconds(300);
}

internal sealed class TabNode
{
    public object? Source { get; init; }

    public required string Key { get; init; }
    public required string Label { get; init; }
    public IReadOnlyList<FieldNode> Fields { get; init; } = [];
    public Func<FormEngine, bool>? Condition { get; init; }
}

internal sealed class FieldNode
{
    public object? Source { get; init; }

    // Typed fields: says why a control value cannot be a value of the field type (null when it can). Used when a value arrives as JSON.
    public Func<object?, string?>? ControlCheck { get; init; }

    // Typed fields: reads the JSON of an agent or a draft through the codec of the field, to the control value it stands for.
    public JsonControlReader? JsonReader { get; init; }

    // Typed fields: the type of the value, and how to build the typed context a custom renderer receives (a FormControlContext of that type).
    public Type? ValueType { get; init; }

    public Func<ControlBinding, FormControlContext>? ContextFactory { get; init; }

    public required string Key { get; init; }
    public FormFieldKind Kind { get; init; } = FormFieldKind.Text;
    public string? Label { get; init; }
    public string? Description { get; init; }
    public string? Hint { get; init; }
    public string? Help { get; init; }
    public string? Placeholder { get; init; }
    public object? DefaultValue { get; init; }
    public Func<FormEngine, object?>? DefaultFactory { get; init; }

    public bool Required { get; init; }
    public Func<FormEngine, bool>? RequiredWhen { get; init; }
    public string? RequiredMessage { get; init; }
    public FormValidationTrigger Trigger { get; init; } = FormValidationTrigger.Blur;
    public IReadOnlyList<string>? DependsOn { get; init; }

    public bool Disabled { get; init; }
    public Func<FormEngine, bool>? DisabledWhen { get; init; }
    public string? DisabledReason { get; init; }

    public Func<FormEngine, bool>? Condition { get; init; }
    public bool Ignore { get; init; }
    public bool OmitFromOutput { get; init; }
    public bool DirtyCheck { get; init; }
    public bool Collapsible { get; init; }
    public bool DefaultCollapsed { get; init; }
    public int Span { get; init; } = 12;

    // Bounds & rules
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Step { get; init; }
    public string? Accept { get; init; }
    public long? MaxFileSize { get; init; }
    public string? CustomKey { get; init; }
    public int? MaxLength { get; init; }
    public bool Multiple { get; init; }
    public bool Clearable { get; init; }
    public IReadOnlyList<FormOptionItem>? Options { get; init; }
    public Func<FormEngine, IReadOnlyList<FormOptionItem>>? OptionsFactory { get; init; }
    public Func<string, FormOptionItem?>? OnCreateOption { get; init; }
    public IReadOnlyList<FormPasswordRequirement>? PasswordRequirements { get; init; }
    public IReadOnlyList<ValidatorRule>? Validators { get; init; }
    public IReadOnlyList<AsyncValidatorRule>? AsyncValidators { get; init; }

    // Transforms & watchers
    public Func<object?, object?>? TransformInput { get; init; }
    public Func<object?, object?>? TransformOutput { get; init; }
    public Action<object?, FormEngine>? Watch { get; init; }
    public Func<FormEngine, Task>? OnClick { get; init; }

    // Display / Info block
    public FormInfoKind? DisplayKind { get; init; }
    public string? DisplayText { get; init; }
    public FormInfoTone? DisplayTone { get; init; }

    // Container / Array children
    public IReadOnlyList<FieldNode>? Fields { get; init; }
    public IReadOnlyList<TabNode>? Tabs { get; init; }
    public string? AddItemLabel { get; init; }
}

internal sealed class StepNode
{
    public object? Source { get; init; }

    public required string Key { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<FieldNode> Fields { get; init; } = [];
    public Func<FormEngine, bool>? SkipWhen { get; init; }
}

internal sealed class SectionNode
{
    public object? Source { get; init; }

    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Description { get; init; }
    public bool Optional { get; init; }
    public int Span { get; init; } = 12;
    public Func<FormEngine, bool>? Condition { get; init; }
    public IReadOnlyList<FieldNode> Fields { get; init; } = [];
}

internal sealed class FormDefinition
{
    // Every node of the form that is not a column of a list, containers included, whichever step, section or tab holds it.
    public IEnumerable<FieldNode> AllNodes()
    {
        foreach (var node in Fields.SelectMany(Walk)) yield return node;
        foreach (var node in Steps.SelectMany(s => s.Fields).SelectMany(Walk)) yield return node;
        foreach (var node in Sections.SelectMany(s => s.Fields).SelectMany(Walk)) yield return node;
    }

    private static IEnumerable<FieldNode> Walk(FieldNode node)
    {
        yield return node;
        if (node.Kind is FormFieldKind.ArrayList or FormFieldKind.ArrayTable) yield break;
        foreach (var child in (node.Fields ?? []).SelectMany(Walk)) yield return child;
        foreach (var child in (node.Tabs ?? []).SelectMany(t => t.Fields).SelectMany(Walk)) yield return child;
    }

    public object? Source { get; init; }

    public string FormKey { get; init; } = "form";
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Eyebrow { get; init; }
    public int Columns { get; init; } = 12;
    public bool ShowStepper { get; init; } = true;
    public FormValidationMode ValidateMode { get; init; } = FormValidationMode.All;
    public FormMessages? Messages { get; init; }
    public IReadOnlyList<string>? IgnoreDirtyPaths { get; init; }
    public IReadOnlyList<FieldNode> Fields { get; init; } = [];
    public IReadOnlyList<StepNode> Steps { get; init; } = [];
    public IReadOnlyList<SectionNode> Sections { get; init; } = [];

    public Func<FormEngine, Task<bool>>? OnBeforeSubmit { get; init; }
    public Func<FormEngine, Task>? OnSubmit { get; init; }
    public Func<FormEngine, int, Task<bool>>? OnBeforeNext { get; init; }
    public Func<FormEngine, int, Task<bool>>? OnBeforePrevious { get; init; }
}
