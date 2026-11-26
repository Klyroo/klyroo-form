using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>日本語 (ja). Needs review.</summary>
    public static FormMessages Ja { get; } = new()
    {
        Required = "この項目は必須です",
        MinValue = n => $"{Number(n, ".")} 以上の値を入力してください",
        MaxValue = n => $"{Number(n, ".")} 以下の値を入力してください",
        MaxLength = n => $"{Number(n, ".")} 文字以内で入力してください",
        DateRangeOrder = "終了日は開始日より前にできません",
        FileTooLarge = (name, size) => $"{name} は {size} を超えています",
        FileTypeNotAllowed = (name, accept) => $"{name} は許可されていない形式です（{accept}）",
        SingleFileOnly = "ファイルは1つだけ選択してください",
        FileSize = bytes => FormFileValue.FormatSize(bytes, ".", ["B", "KB", "MB", "GB", "TB"]),
    };
}
