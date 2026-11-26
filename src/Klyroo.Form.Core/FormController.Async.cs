namespace Klyroo.Form;

public sealed partial class FormController
{
    /// <summary><c>true</c> while any asynchronous rule (see <see cref="FormFieldSchema.AsyncValidators"/>) is waiting for its debounce or running.</summary>
    public bool HasPendingValidation => _pendingValidations.Count > 0;

    /// <summary>Tells whether an asynchronous rule of a field is waiting for its debounce or running, which the components show as a pending state next to the field.</summary>
    /// <param name="path">Key of the field.</param>
    /// <returns><c>true</c> while the field is being validated asynchronously.</returns>
    public bool IsValidating(string path) => _pendingValidations.ContainsKey(path);

    /// <summary>
    /// Validates like <see cref="Validate"/>, then runs the asynchronous rules (see <see cref="FormFieldSchema.AsyncValidators"/>) of the validated fields that have a non-empty value,
    /// pass their synchronous checks and have no result yet for the current value, in parallel across fields, and validates again with their results.
    /// <see cref="SubmitAsync()"/> and <see cref="NextStepAsync"/> call it.
    /// </summary>
    /// <param name="currentStepOnly"><c>true</c> to validate only the fields of the current step.</param>
    /// <param name="cancellationToken">Cancels the rules that run. The rules run now, so a background run of the same field is cancelled and its result is not reused.</param>
    /// <returns><c>true</c> if none of the validated fields and cells has an error and no blocking error applies, as for <see cref="Validate"/>.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled. A run replaced by an edit is not an error: it is run again.</exception>
    /// <exception cref="Exception">An exception thrown by an asynchronous rule propagates, after the other rules finished or were cancelled.</exception>
    public async Task<bool> ValidateAsync(bool currentStepOnly = false, CancellationToken cancellationToken = default)
    {
        var valid = Validate(currentStepOnly);
        for (var attempt = 0; ; attempt++)
        {
            var work = FieldsNeedingAsyncValidation(currentStepOnly);
            if (work.Count == 0) return valid;
            // A value that changes while its rule runs leaves a result for the old value, which is not "no error". Check again, and if the
            // results keep going stale report failure rather than success for a value no rule has seen.
            if (attempt == 3) return false;
            await Task.WhenAll(work.Select(item => CheckNowAsync(item.Field, item.Value, cancellationToken)));
            valid = Validate(currentStepOnly);
        }
    }

    private bool HasAsyncWork(bool currentStepOnly) => FieldsNeedingAsyncValidation(currentStepOnly).Count > 0;

    private List<(FormFieldSchema Field, object? Value)> FieldsNeedingAsyncValidation(bool currentStepOnly)
    {
        var result = new List<(FormFieldSchema, object?)>();
        if (Schema.ValidateMode is not (FormValidationMode.All or FormValidationMode.ValidatorsOnly)) return result;
        IReadOnlyList<FormFieldSchema> scope = ActiveFields;
        if (currentStepOnly && CurrentStep is not null)
        {
            var stepFields = new List<FormFieldSchema>();
            CollectActive(CurrentStep.Fields, stepFields);
            scope = stepFields;
        }
        foreach (var field in scope)
        {
            if (field.AsyncValidators is not { Count: > 0 }) continue;
            var value = GetValue(field.Key);
            if (IsAsyncEligible(field, value) && CachedAsyncResult(field.Key, value) is null)
            {
                result.Add((field, CloneValue(value)));
            }
        }
        return result;
    }

    // An asynchronous rule runs for a value that is not empty and that passes the synchronous checks.
    private bool IsAsyncEligible(FormFieldSchema field, object? value) =>
        field.AsyncValidators is { Count: > 0 }
        && Schema.ValidateMode is FormValidationMode.All or FormValidationMode.ValidatorsOnly
        && !IsEmptyValue(value)
        && CheckValueSync(field, value) is null;

    private (object? Value, string? Message)? CachedAsyncResult(string key, object? value) =>
        _asyncResults.TryGetValue(key, out var result) && ValuesEqual(result.Value, value) ? result : null;

    private string? CachedAsyncError(FormFieldSchema field, object? value) =>
        field.AsyncValidators is { Count: > 0 } ? CachedAsyncResult(field.Key, value)?.Message : null;

    private static async ValueTask<string?> RunRulesAsync(FormFieldSchema field, FormController form, object? value, CancellationToken cancellationToken)
    {
        foreach (var rule in field.AsyncValidators ?? [])
        {
            var message = await rule.Validate(value, form, cancellationToken);
            if (!string.IsNullOrEmpty(message)) return message;
        }
        return null;
    }

    private void StoreAsyncResult(FormFieldSchema field, object? value, string? message)
    {
        _asyncResults[field.Key] = (value, message);
        if (ActiveFields.Contains(field)) ValidateFieldCore(field);
    }

    // Submission needs the result for this value: take it from the background run that has it (cutting its debounce short, so the service is not called
    // twice for one value and submission does not wait for the debounce), otherwise run the rules now.
    private async Task CheckNowAsync(FormFieldSchema field, object? value, CancellationToken cancellationToken)
    {
        if (_backgroundRuns.TryGetValue(field.Key, out var background) && ValuesEqual(background.Value, value))
        {
            background.SkipDebounce.TrySetResult();
            await background.Done.WaitAsync(cancellationToken);
            if (CachedAsyncResult(field.Key, value) is not null) return;
        }
        await RunNowAsync(field, value, cancellationToken);
    }

    private sealed class BackgroundRun(object? value)
    {
        public object? Value { get; } = value;
        public TaskCompletionSource SkipDebounce { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DoneSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Done => DoneSource.Task;
    }

    // Runs the rules of a field now, for submission, and records the result for the value they saw.
    private async Task RunNowAsync(FormFieldSchema field, object? value, CancellationToken cancellationToken)
    {
        CancelPendingValidation(field.Key);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pendingValidations[field.Key] = cts;
        Changed?.Invoke();
        try
        {
            var message = await RunRulesAsync(field, this, value, cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            StoreAsyncResult(field, value, message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // An edit or a reset replaced this run; ValidateAsync notices the missing result and runs the rule again.
        }
        finally
        {
            if (_pendingValidations.TryGetValue(field.Key, out var current) && ReferenceEquals(current, cts))
            {
                _pendingValidations.Remove(field.Key);
            }
            Changed?.Invoke();
        }
    }

    // Starts the debounced background run for the current value, cancelling an older one. Does nothing when there is nothing to check.
    // The new run is registered before the old one is cancelled: cancelling runs the continuations of whoever awaited the old run, inline, and one of
    // them (ValidateAsync) must find the new run and wait for it instead of starting a second run for the same value.
    private void ScheduleAsyncValidation(FormFieldSchema field)
    {
        _pendingValidations.Remove(field.Key, out var previous);
        CancellationTokenSource? cts = null;
        BackgroundRun? run = null;
        object? value = null;
        if (!_disposed)
        {
            value = CloneValue(GetValue(field.Key));
            if (ActiveFields.Contains(field) && IsAsyncEligible(field, value) && CachedAsyncResult(field.Key, value) is null)
            {
                cts = new CancellationTokenSource();
                run = new BackgroundRun(value);
                _pendingValidations[field.Key] = cts;
                _backgroundRuns[field.Key] = run;
            }
        }
        previous?.Cancel();
        if (cts is not null && run is not null) _ = RunInBackgroundAsync(field, value, cts, run);
    }

    private async Task RunInBackgroundAsync(FormFieldSchema field, object? value, CancellationTokenSource cts, BackgroundRun run)
    {
        var finished = false;
        try
        {
            var debounce = field.AsyncValidators!.Max(r => r.Debounce);
            if (debounce > TimeSpan.Zero)
            {
                using var delay = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                await Task.WhenAny(Task.Delay(debounce, delay.Token), run.SkipDebounce.Task);
                await delay.CancelAsync();
                cts.Token.ThrowIfCancellationRequested();
            }
            var message = await RunRulesAsync(field, this, value, cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            StoreAsyncResult(field, value, message);
            finished = true;
        }
        catch (OperationCanceledException)
        {
            // A newer run or a reset replaced this one.
        }
        catch (Exception)
        {
            // The service is unreachable, say: the value stays unchecked, and submission runs the rule again and reports the failure to its caller.
        }
        finally
        {
            var current = _pendingValidations.TryGetValue(field.Key, out var registered) && ReferenceEquals(registered, cts);
            if (current) _pendingValidations.Remove(field.Key);
            if (_backgroundRuns.TryGetValue(field.Key, out var latest) && ReferenceEquals(latest, run)) _backgroundRuns.Remove(field.Key);
            cts.Dispose();
            run.DoneSource.TrySetResult();
            if (current || finished) Changed?.Invoke();
        }
    }

    private void CancelPendingValidation(string key)
    {
        if (_pendingValidations.Remove(key, out var cts))
        {
            cts.Cancel();
        }
    }

    private bool _disposed;

    /// <summary>Cancels the asynchronous validations that are waiting for their debounce or running, without changing any value or error. A component calls it when it goes away.</summary>
    public void CancelPendingValidations() => CancelAllPendingValidations();

    /// <summary>Cancels the pending asynchronous validations and stops starting new ones. The values and errors stay readable.</summary>
    public void Dispose()
    {
        _disposed = true;
        CancelAllPendingValidations();
    }

    private void CancelAllPendingValidations()
    {
        foreach (var key in _pendingValidations.Keys.ToList()) CancelPendingValidation(key);
    }
}
