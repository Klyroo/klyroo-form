using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    /// <summary>Français (fr). Reviewed.</summary>
    public static FormMessages Fr { get; } = new()
    {
        Required = "Ce champ est obligatoire",
        MinValue = n => $"La valeur doit être supérieure ou égale à {Number(n, ",")}",
        MaxValue = n => $"La valeur doit être inférieure ou égale à {Number(n, ",")}",
        MaxLength = n => $"{Number(n, ",")} caractères au maximum",
        DateRangeOrder = "La date de fin ne peut pas précéder la date de début",
        FileTooLarge = (name, size) => $"{name} dépasse {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} n’est pas d’un type accepté ({accept})",
        SingleFileOnly = "Choisissez un seul fichier",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["o", "Ko", "Mo", "Go", "To"]),
    };
}
