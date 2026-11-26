using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Italiano (it). Needs review.</summary>
    public static FormMessages It { get; } = new()
    {
        Required = "Questo campo è obbligatorio",
        MinValue = n => $"Il valore deve essere almeno {Number(n, ",")}",
        MaxValue = n => $"Il valore deve essere al massimo {Number(n, ",")}",
        MaxLength = n => $"Al massimo {Number(n, ",")} caratteri",
        DateRangeOrder = "La data di fine non può precedere la data di inizio",
        FileTooLarge = (name, size) => $"{name} supera {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} non è di un tipo accettato ({accept})",
        SingleFileOnly = "Scegli un solo file",
        WholeNumber = "Inserisci un numero intero",
        NumberOutOfRange = (min, max) => $"Inserisci un numero compreso tra {Digits(min, ",")} e {Digits(max, ",")}",
        InvalidIdentifier = "Inserisci un identificatore valido",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["B", "KB", "MB", "GB", "TB"]),
    };
}
