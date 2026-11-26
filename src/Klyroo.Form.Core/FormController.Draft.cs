namespace Klyroo.Form;

public sealed partial class FormController
{
    /// <summary>Takes a copy of the values the user has typed and of the current step, to keep them across a reload or a navigation: store <see cref="FormDraft.ToJson"/> and give the draft back to <see cref="RestoreDraft"/> on a controller built from the same initial input.</summary>
    /// <param name="includeSensitive"><c>true</c> to include the values of <see cref="FormFieldKind.Password"/> and <see cref="FormFieldKind.OneTimeCode"/> fields. They are left out by default, because a draft is meant to be stored on the user's device. <see cref="FormFieldKind.File"/> fields are always left out: a file cannot be restored from a draft. The same applies to the columns of a repeatable list.</param>
    /// <returns>The draft. Lists and rows are copied, so later edits do not change it.</returns>
    public FormDraft ExportState(bool includeSensitive = false)
    {
        var sensitive = AllFields
            .Where(f => f.Kind == FormFieldKind.File || (!includeSensitive && f.Kind is FormFieldKind.Password or FormFieldKind.OneTimeCode))
            .Select(f => f.Key)
            .ToHashSet(StringComparer.Ordinal);
        var fields = AllFields.GroupBy(f => f.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in _state)
        {
            if (sensitive.Contains(key)) continue;
            var copy = CloneValue(value);
            // The columns of a repeatable list follow the same rules as top-level fields.
            if (copy is List<Dictionary<string, object?>> rows && fields.TryGetValue(key, out var list) && list.Fields is { Count: > 0 } columns)
            {
                var dropped = columns
                    .Where(c => c.Kind == FormFieldKind.File || (!includeSensitive && c.Kind is FormFieldKind.Password or FormFieldKind.OneTimeCode))
                    .Select(c => c.Key)
                    .ToList();
                foreach (var row in rows)
                {
                    foreach (var column in dropped) row.Remove(column);
                }
            }
            values[key] = copy;
        }
        return new FormDraft(values, CurrentStepIndex);
    }

    /// <summary>
    /// Puts the values of a draft back into the form, as if the user had typed them. The baseline of dirty tracking is not touched, so the restored fields
    /// differ from it: <see cref="IsDirty"/> and <see cref="DirtyPaths"/> report the draft as unsaved changes, which is what lets a navigation guard protect it.
    /// This is what separates it from <see cref="Reset"/>, which makes the loaded values the new baseline. Errors and touched state are cleared and
    /// <see cref="FormFieldSchema.Watch"/> is not called, like <see cref="Reset"/>. Keys of the draft that match no value of the form are ignored, as are <see cref="FormFieldKind.File"/> and <see cref="FormFieldKind.Custom"/> fields (and such columns of a list), and a field the draft
    /// does not mention keeps its value (the sensitive fields <see cref="ExportState"/> leaves out among them).
    /// </summary>
    /// <param name="draft">The draft, from <see cref="ExportState"/> or <see cref="FormDraft.FromJson"/>. Each value is converted to the type its field uses (a number read from JSON becomes an <see cref="int"/> for a rating, a list becomes the rows of a repeatable list).</param>
    /// <exception cref="ArgumentNullException"><paramref name="draft"/> is <c>null</c>.</exception>
    public void RestoreDraft(FormDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var fields = AllFields.GroupBy(f => f.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var (key, value) in draft.Values)
        {
            if (!_state.ContainsKey(key)) continue;
            // Files and custom values are never taken from stored text: a draft is data the user's device holds, and these two kinds hold objects only your code can build.
            if (fields.TryGetValue(key, out var kind) && kind.Kind is FormFieldKind.File or FormFieldKind.Custom) continue;
            _state[key] = fields.TryGetValue(key, out var field)
                ? CloneValue(FormValueConverter.NormalizeForField(field, value))
                : CloneValue(value);
        }

        ClearValidationState();
        _customErrors.Clear();
        SubmitFailed = false;
        if (IsStepped && Schema.Steps.Count > 0)
        {
            CurrentStepIndex = Math.Clamp(draft.StepIndex, 0, Schema.Steps.Count - 1);
        }
        Changed?.Invoke();
    }
}
