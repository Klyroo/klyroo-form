using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Klyroo.Form;

// The assignments a write makes on a model, worked out before the first one is made, so a failure while preparing them (a value that does not convert, an
// object on the way that is null and cannot be created) leaves the model untouched. Applying them can still fail in a setter of the model: the ones already
// made are undone. The same journal undoes a whole write after it succeeded, when a save step that was given the model reports a failure.
internal sealed class FormWritePlan
{
    private sealed class Step(Action apply, Action undo)
    {
        public Action Apply { get; } = apply;
        public Action Undo { get; } = undo;
    }

    private readonly List<Step> _steps = [];
    private int _applied;

    // The nested objects this write creates, by key prefix: a second member of the same object finds the one the first member planned.
    internal Dictionary<string, object> Created { get; } = new(StringComparer.Ordinal);

    // The lists of rows this write sets, with the items each row stands for, in order: a form that stays open after a save adopts them as the origin of its rows.
    internal List<(string Key, IReadOnlyList<object> Items)> Lists { get; } = [];

    public void Add<THolder, TValue>(THolder holder, Action<THolder, TValue> set, TValue value, TValue old) =>
        _steps.Add(new Step(() => set(holder, value), () => set(holder, old)));

    public FormWritePlan Apply()
    {
        try
        {
            foreach (var step in _steps)
            {
                step.Apply();
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
                _steps[i].Undo();
            }
            catch (Exception)
            {
                // The old value cannot be set again, whatever the setter throws.
            }
        }
        _applied = 0;
    }
}
