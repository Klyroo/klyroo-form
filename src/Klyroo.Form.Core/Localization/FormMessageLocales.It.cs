using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Italiano (it). Needs review.</summary>
    public static FormMessages It { get; } = new()
    {
        Required = "Questo campo è obbligatorio",
        MinValue = n => $"Il valore deve essere almeno {n.ToString(CultureInfo.InvariantCulture)}",
        MaxValue = n => $"Il valore deve essere al massimo {n.ToString(CultureInfo.InvariantCulture)}",
        MaxLength = n => $"Al massimo {n.ToString(CultureInfo.InvariantCulture)} caratteri",
        DateRangeOrder = "La data di fine non può precedere la data di inizio",
        FileTooLarge = (name, size) => $"{name} supera {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} non è di un tipo accettato ({accept})",
        SingleFileOnly = "Scegli un solo file",
    };
}
