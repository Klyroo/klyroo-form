using System.Runtime.CompilerServices;

namespace Klyroo.Form;

// Rendering state that is not form data (selected tab, revealed passwords, text being typed in a tag, create-option or
// autocomplete input), attached to the controller so every component rendering the same form shares it and it is
// collected with the controller.
internal sealed class FormUiState
{
    private static readonly ConditionalWeakTable<FormController, FormUiState> States = new();

    public Dictionary<string, string> ActiveTabs { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RevealedPasswords { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Drafts { get; } = new(StringComparer.Ordinal);

    public static FormUiState For(FormController form) => States.GetValue(form, _ => new FormUiState());
}
