using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>العربية (ar). Needs review.</summary>
    public static FormMessages Ar { get; } = new()
    {
        Required = "هذا الحقل مطلوب",
        MinValue = n => $"يجب أن تكون القيمة {Number(n, ".")} على الأقل",
        MaxValue = n => $"يجب ألا تتجاوز القيمة {Number(n, ".")}",
        MaxLength = n => $"الحد الأقصى {Number(n, ".")} من الأحرف",
        DateRangeOrder = "يجب ألا يكون تاريخ الانتهاء قبل تاريخ البدء",
        FileTooLarge = (name, size) => $"{name} أكبر من {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} ليس من الأنواع المقبولة ({accept})",
        SingleFileOnly = "اختر ملفًا واحدًا فقط",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ".", ["B", "KB", "MB", "GB", "TB"]),
    };
}
