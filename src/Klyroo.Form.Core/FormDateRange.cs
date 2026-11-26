using System.Globalization;

namespace Klyroo.Form;

/// <summary>A range of two dates, the value of a <see cref="FormFieldKind.DateRange"/> field. Either end can be missing while the user is still choosing, and validation rejects an end before the start.</summary>
/// <param name="Start">The first day, or <c>null</c> when none is chosen.</param>
/// <param name="End">The last day, or <c>null</c> when none is chosen.</param>
public readonly record struct FormDateRange(DateOnly? Start, DateOnly? End)
{
    // The two dates as the controls store them: yyyy-MM-dd, an empty string for a missing one, and no value at all when both are missing.
    internal List<string>? ToControl() =>
        Start is null && End is null
            ? null
            : [Start?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", End?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""];
}
