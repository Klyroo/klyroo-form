namespace Klyroo.Form;

/// <summary>
/// The built-in texts of Klyroo.Form, one set per locale of <see cref="KlyLocales.All"/> and keyed by the same codes. A component picks the set of its current
/// <see cref="KlyLocale.Code"/> with <see cref="KlyLocales.ResolveCode"/>, so "pt-PT" gets "pt" and an unknown language gets English. Change a set with <see cref="KlyrooFormOptions.Texts"/>.
/// </summary>
public static partial class FormLocales
{
    private static IReadOnlyDictionary<string, FormTexts>? _tables;

    /// <summary>Every set by locale code: the codes of <see cref="KlyLocales.All"/>.</summary>
    public static IReadOnlyDictionary<string, FormTexts> Tables => _tables ??= new Dictionary<string, FormTexts>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = En,
        ["fr"] = Fr,
        ["de"] = De,
        ["es"] = Es,
        ["it"] = It,
        ["pt"] = Pt,
        ["pt-BR"] = PtBr,
        ["nl"] = Nl,
        ["ja"] = Ja,
        ["zh-Hans"] = ZhCn,
        ["ar"] = Ar,
        ["ru"] = Ru,
    };

    /// <summary>The codes whose texts were reviewed by a native speaker. The other sets are drafts waiting for review.</summary>
    public static IReadOnlyList<string> ReviewedCodes { get; } = ["en", "fr"];

    /// <summary>English.</summary>
    public static FormTexts En { get; } = new();

    // Russian plural forms: one (1, 21, 31...), few (2-4, 22-24...), many (the rest, 11-14 included).
    internal static string RussianPlural(int n, string one, string few, string many) => FormMessageLocales.RussianPlural(n, one, few, many);
}
