using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Klyroo.Form;

// The assignments a write makes on a model, worked out before the first one is made, so a failure while preparing them (a value that does not convert, an
// object on the way that is null and cannot be created) leaves the model untouched. Applying them can still fail in a setter of the model: the ones already
// made are undone. The same journal undoes a whole write after it succeeded, when a save step that was given the model reports a failure.
internal sealed class FormWritePlan
{
    private sealed record Step(object Holder, PropertyInfo Property, object? Value, object? Old);

    private readonly List<Step> _steps = [];
    private int _applied;

    // The nested objects this write creates, by key prefix: a second member of the same object finds the one the first member planned.
    internal Dictionary<string, object> Created { get; } = new(StringComparer.Ordinal);

    public void Add(object holder, PropertyInfo property, object? value) => _steps.Add(new Step(holder, property, value, property.GetValue(holder)));

    public FormWritePlan Apply()
    {
        try
        {
            foreach (var step in _steps)
            {
                step.Property.SetValue(step.Holder, step.Value);
                _applied++;
            }
        }
        catch (Exception ex)
        {
            Rollback();
            if (ex is TargetInvocationException { InnerException: { } inner }) ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
        return this;
    }

    // Puts back what the applied assignments replaced, newest first. A setter that refuses to take its old value back is skipped: nothing more can be done for it.
    public void Rollback()
    {
        for (var i = _applied - 1; i >= 0; i--)
        {
            try
            {
                _steps[i].Property.SetValue(_steps[i].Holder, _steps[i].Old);
            }
            catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException)
            {
                // The old value cannot be set again.
            }
        }
        _applied = 0;
    }
}
