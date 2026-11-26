using Klyroo.Form.Dynamic;
namespace Klyroo.Form;

/// <summary>Completion state of one section, computed from the current values by <see cref="DynamicFormController.GetSectionStates"/>.</summary>
/// <param name="Key">Key of the section.</param>
/// <param name="Label">Label of the section.</param>
/// <param name="Description">Description of the section.</param>
/// <param name="Optional">Whether the section is optional.</param>
/// <param name="RequiredCount">Number of required, visible fields in the section.</param>
/// <param name="MissingCount">Number of those required fields that are empty.</param>
/// <param name="Dirty"><c>true</c> when any field of the section differs from its initial value.</param>
/// <param name="Status">Completion status of the section.</param>
public sealed record FormPageSectionState(
    string Key,
    string Label,
    string? Description,
    bool Optional,
    int RequiredCount,
    int MissingCount,
    bool Dirty,
    FormPageSectionStatus Status);
