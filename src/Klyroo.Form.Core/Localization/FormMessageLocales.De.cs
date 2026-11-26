using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Deutsch (de). Needs review.</summary>
    public static FormMessages De { get; } = new()
    {
        Required = "Dieses Feld ist erforderlich",
        MinValue = n => $"Der Wert muss mindestens {n.ToString(CultureInfo.InvariantCulture)} betragen",
        MaxValue = n => $"Der Wert darf höchstens {n.ToString(CultureInfo.InvariantCulture)} betragen",
        MaxLength = n => $"Höchstens {n.ToString(CultureInfo.InvariantCulture)} Zeichen erlaubt",
        DateRangeOrder = "Das Enddatum darf nicht vor dem Startdatum liegen",
        FileTooLarge = (name, size) => $"{name} ist größer als {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} hat keinen zulässigen Typ ({accept})",
        SingleFileOnly = "Wählen Sie nur eine Datei",
    };
}
