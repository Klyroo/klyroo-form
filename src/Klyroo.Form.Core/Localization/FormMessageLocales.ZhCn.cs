using System.Globalization;

namespace Klyroo.Form;

public static partial class FormMessageLocales
{
    // Draft translation, marked for review by a native speaker before a stable release.
    /// <summary>简体中文 (zh-Hans). Needs review.</summary>
    public static FormMessages ZhCn { get; } = new()
    {
        Required = "此字段为必填项",
        MinValue = n => $"值不能小于 {n.ToString(CultureInfo.InvariantCulture)}",
        MaxValue = n => $"值不能大于 {n.ToString(CultureInfo.InvariantCulture)}",
        MaxLength = n => $"最多 {n.ToString(CultureInfo.InvariantCulture)} 个字符",
        DateRangeOrder = "结束日期不能早于开始日期",
        FileTooLarge = (name, size) => $"{name} 超过 {size}",
        FileTypeNotAllowed = (name, accept) => $"{name} 不是允许的类型（{accept}）",
        SingleFileOnly = "只能选择一个文件",
    };
}
