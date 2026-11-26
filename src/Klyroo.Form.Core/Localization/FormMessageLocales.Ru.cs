using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Русский (ru). Needs review.</summary>
    public static FormMessages Ru { get; } = new()
    {
        Required = "Это поле обязательно",
        MinValue = n => $"Значение должно быть не меньше {n.ToString(CultureInfo.InvariantCulture)}",
        MaxValue = n => $"Значение должно быть не больше {n.ToString(CultureInfo.InvariantCulture)}",
        MaxLength = n => $"Не более {n} " + RussianPlural(n, "символа", "символов", "символов"),
        DateRangeOrder = "Дата окончания не может быть раньше даты начала",
        FileTooLarge = (name, size) => $"{name} больше {size}",
        FileTypeNotAllowed = (name, accept) => $"{name}: недопустимый тип ({accept})",
        SingleFileOnly = "Выберите только один файл",
    };
}
