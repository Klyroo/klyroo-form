using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Nederlands (nl). Needs review.</summary>
    public static FormMessages Nl { get; } = new()
    {
        Required = "Dit veld is verplicht",
        MinValue = n => $"De waarde moet minstens {Number(n, ",")} zijn",
        MaxValue = n => $"De waarde mag hoogstens {Number(n, ",")} zijn",
        MaxLength = n => $"Maximaal {Number(n, ",")} tekens",
        DateRangeOrder = "De einddatum mag niet vóór de startdatum liggen",
        FileTooLarge = (name, size) => $"{name} is groter dan {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} is geen toegestaan type ({accept})",
        SingleFileOnly = "Kies slechts één bestand",
        WholeNumber = "Voer een geheel getal in",
        NumberOutOfRange = (min, max) => $"Voer een getal in tussen {Digits(min, ",")} en {Digits(max, ",")}",
        InvalidIdentifier = "Voer een geldige id in",
        InvalidValue = "Voer een geldige waarde in",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["B", "KB", "MB", "GB", "TB"]),
    };
}
