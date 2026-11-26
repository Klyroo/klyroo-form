namespace Klyroo.Form;

internal sealed partial class FormEngine
{
    internal FormDraft ExportState(bool includeSensitive = false)
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

    internal void RestoreDraft(FormDraft draft)
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
