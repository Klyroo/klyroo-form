using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>Русский (ru). Needs review.</summary>
    public static FormMessages Ru { get; } = new()
    {
        Required = "Это поле обязательно",
        MinValue = n => $"Значение должно быть не меньше {Number(n, ",")}",
        MaxValue = n => $"Значение должно быть не больше {Number(n, ",")}",
        MaxLength = n => $"Не более {n} " + RussianPlural(n, "символа", "символов", "символов"),
        DateRangeOrder = "Дата окончания не может быть раньше даты начала",
        FileTooLarge = (name, size) => $"{name} больше {size}",
        FileTypeNotAllowed = (name, accept) => $"{name}: недопустимый тип ({accept})",
        SingleFileOnly = "Выберите только один файл",
        WholeNumber = "Введите целое число",
        NumberOutOfRange = (min, max) => $"Введите число от {Digits(min, ",")} до {Digits(max, ",")}",
        InvalidIdentifier = "Введите корректный идентификатор",
        InvalidValue = "Введите допустимое значение",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ",", ["Б", "КБ", "МБ", "ГБ", "ТБ"]),
    };
}
