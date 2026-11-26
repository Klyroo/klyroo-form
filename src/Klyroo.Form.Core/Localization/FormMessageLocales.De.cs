using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Deutsch (de). Needs review.</summary>
    public static FormMessages De { get; } = new()
    {
        Required = "Dieses Feld ist erforderlich",
        MinValue = n => $"Der Wert muss mindestens {Number(n, ",")} betragen",
        MaxValue = n => $"Der Wert darf höchstens {Number(n, ",")} betragen",
        MaxLength = n => $"Höchstens {Number(n, ",")} Zeichen erlaubt",
        DateRangeOrder = "Das Enddatum darf nicht vor dem Startdatum liegen",
        FileTooLarge = (name, size) => $"{name} ist größer als {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} hat keinen zulässigen Typ ({accept})",
        SingleFileOnly = "Wählen Sie nur eine Datei",
        WholeNumber = "Geben Sie eine ganze Zahl ein",
        NumberOutOfRange = (min, max) => $"Geben Sie eine Zahl zwischen {Digits(min, ",")} und {Digits(max, ",")} ein",
        InvalidIdentifier = "Geben Sie eine gültige Kennung ein",
        InvalidValue = "Geben Sie einen gültigen Wert ein",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["B", "KB", "MB", "GB", "TB"]),
    };
}
