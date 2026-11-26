using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Português (pt). Needs review.</summary>
    public static FormMessages Pt { get; } = new()
    {
        Required = "Este campo é obrigatório",
        MinValue = n => $"O valor deve ser no mínimo {Number(n, ",")}",
        MaxValue = n => $"O valor deve ser no máximo {Number(n, ",")}",
        MaxLength = n => $"No máximo {Number(n, ",")} caracteres",
        DateRangeOrder = "A data de fim não pode ser anterior à data de início",
        FileTooLarge = (name, size) => $"{name} excede {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} não é de um tipo aceite ({accept})",
        SingleFileOnly = "Escolha apenas um ficheiro",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["B", "KB", "MB", "GB", "TB"]),
    };
}
