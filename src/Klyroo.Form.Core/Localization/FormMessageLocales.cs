using System.Globalization;

namespace Klyroo.Form;

/// <summary>
/// The built-in <see cref="FormMessages"/>, one set per locale of Klyroo.UI and keyed by the same codes, usable without Blazor: <c>new FormController(schema) { Messages = FormMessageLocales.Fr }</c>.
/// English and French are reviewed by a native speaker, the other sets are drafts waiting for review. The Blazor components pick the set of their current locale
/// and let you change it with <c>KlyrooFormOptions.Texts</c>.
/// </summary>
public static partial class FormMessageLocales
{
    private static IReadOnlyDictionary<string, FormMessages>? _tables;

    /// <summary>Every set by locale code: <c>en</c>, <c>fr</c>, <c>de</c>, <c>es</c>, <c>it</c>, <c>pt</c>, <c>pt-BR</c>, <c>nl</c>, <c>ja</c>, <c>zh-Hans</c>, <c>ar</c> and <c>ru</c>.</summary>
    public static IReadOnlyDictionary<string, FormMessages> Tables => _tables ??= new Dictionary<string, FormMessages>(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>The codes whose messages were reviewed by a native speaker. The other sets are drafts waiting for review.</summary>
    public static IReadOnlyList<string> ReviewedCodes { get; } = ["en", "fr"];

    /// <summary>English.</summary>
    public static FormMessages En { get; } = FormMessages.Default;

    /// <summary>
    /// Picks the set for a culture name with the rules Klyroo.UI uses for its own texts: the exact code ("pt-BR"), then its parent cultures ("pt-PT" gets "pt",
    /// "zh-CN" gets "zh-Hans"), then English. A name the runtime has no data for (invariant globalization) is walked by trimming its last "-" segment.
    /// </summary>
    /// <param name="code">A culture name such as <c>fr-CA</c>, usually the code of the Klyroo.UI locale.</param>
    /// <returns>The messages of the best match, never <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <c>null</c>.</exception>
    public static FormMessages Resolve(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        foreach (var name in Chain(code))
        {
            if (Tables.TryGetValue(name, out var messages)) return messages;
        }
        return En;
    }

    private static IEnumerable<string> Chain(string code)
    {
        CultureInfo? culture = null;
        try
        {
            culture = CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            // The runtime has no data for this name: walk the name itself.
        }
        if (culture is null)
        {
            for (var name = code; name.Length > 0;)
            {
                yield return name;
                var dash = name.LastIndexOf('-');
                name = dash < 0 ? "" : name[..dash];
            }
            yield break;
        }
        for (var c = culture; c.Name.Length > 0; c = c.Parent) yield return c.Name;
    }

    // A number with the decimal separator of a language, without culture data (an app may run with invariant globalization).
    internal static string Number(double n, string decimalSeparator) =>
        n.ToString(CultureInfo.InvariantCulture).Replace(".", decimalSeparator, StringComparison.Ordinal);

    // Russian plural forms: one (1, 21, 31...), few (2-4, 22-24...), many (the rest, 11-14 included).
    internal static string RussianPlural(int n, string one, string few, string many)
    {
        var mod10 = Math.Abs(n) % 10;
        var mod100 = Math.Abs(n) % 100;
        if (mod10 == 1 && mod100 != 11) return one;
        return mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14 ? few : many;
    }
}
