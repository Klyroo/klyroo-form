using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Español (es). Needs review.</summary>
    public static FormMessages Es { get; } = new()
    {
        Required = "Este campo es obligatorio",
        MinValue = n => $"El valor debe ser al menos {Number(n, ",")}",
        MaxValue = n => $"El valor debe ser como máximo {Number(n, ",")}",
        MaxLength = n => $"Debe tener como máximo {Number(n, ",")} caracteres",
        DateRangeOrder = "La fecha de fin no puede ser anterior a la fecha de inicio",
        FileTooLarge = (name, size) => $"{name} supera {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} no es de un tipo admitido ({accept})",
        SingleFileOnly = "Elige un solo archivo",
        WholeNumber = "Introduce un número entero",
        NumberOutOfRange = (min, max) => $"Introduce un número entre {Digits(min, ",")} y {Digits(max, ",")}",
        InvalidIdentifier = "Introduce un identificador válido",
        InvalidValue = "Introduce un valor válido",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["B", "KB", "MB", "GB", "TB"]),
    };
}
