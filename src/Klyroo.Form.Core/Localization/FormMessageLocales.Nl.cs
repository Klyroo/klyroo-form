using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Nederlands (nl). Needs review.</summary>
    public static FormMessages Nl { get; } = new()
    {
        Required = "Dit veld is verplicht",
        MinValue = n => $"De waarde moet minstens {n.ToString(CultureInfo.InvariantCulture)} zijn",
        MaxValue = n => $"De waarde mag hoogstens {n.ToString(CultureInfo.InvariantCulture)} zijn",
        MaxLength = n => $"Maximaal {n.ToString(CultureInfo.InvariantCulture)} tekens",
        DateRangeOrder = "De einddatum mag niet vóór de startdatum liggen",
        FileTooLarge = (name, size) => $"{name} is groter dan {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} is geen toegestaan type ({accept})",
        SingleFileOnly = "Kies slechts één bestand",
    };
}
