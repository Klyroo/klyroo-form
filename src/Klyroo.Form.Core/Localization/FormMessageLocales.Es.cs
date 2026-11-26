using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Español (es). Needs review.</summary>
    public static FormMessages Es { get; } = new()
    {
        Required = "Este campo es obligatorio",
        MinValue = n => $"El valor debe ser al menos {n.ToString(CultureInfo.InvariantCulture)}",
        MaxValue = n => $"El valor debe ser como máximo {n.ToString(CultureInfo.InvariantCulture)}",
        MaxLength = n => $"Debe tener como máximo {n.ToString(CultureInfo.InvariantCulture)} caracteres",
        DateRangeOrder = "La fecha de fin no puede ser anterior a la fecha de inicio",
        FileTooLarge = (name, size) => $"{name} supera {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} no es de un tipo admitido ({accept})",
        SingleFileOnly = "Elige un solo archivo",
    };
}
