using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Português (Brasil) (pt-BR). Needs review.</summary>
    public static FormMessages PtBr { get; } = new()
    {
        Required = "Este campo é obrigatório",
        MinValue = n => $"O valor deve ser no mínimo {Number(n, ",")}",
        MaxValue = n => $"O valor deve ser no máximo {Number(n, ",")}",
        MaxLength = n => $"No máximo {Number(n, ",")} caracteres",
        DateRangeOrder = "A data de término não pode ser anterior à data de início",
        FileTooLarge = (name, size) => $"{name} excede {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} não é de um tipo aceito ({accept})",
        SingleFileOnly = "Escolha apenas um arquivo",
        WholeNumber = "Digite um número inteiro",
        NumberOutOfRange = (min, max) => $"Digite um número entre {Digits(min, ",")} e {Digits(max, ",")}",
        InvalidIdentifier = "Digite um identificador válido",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["B", "KB", "MB", "GB", "TB"]),
    };
}
