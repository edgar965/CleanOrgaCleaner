using CleanOrgaCleaner.Localization.Sprachen;

namespace CleanOrgaCleaner.Localization;

/// <summary>
/// Register aller Sprachpakete. Einzige Stelle, an der eine neue Sprache
/// eingetragen wird (Klasse in <c>Localization/Sprachen</c> anlegen und hier
/// ergänzen). Liefert Pakete ohne Neuaufbau - die Instanzen entstehen einmalig
/// beim ersten Zugriff.
/// </summary>
public static class TranslationCatalog
{
    /// <summary>Sprachcode der Fallback-Sprache (erste Stufe der Fallback-Kette).</summary>
    public const string FallbackCode = "en";

    /// <summary>Sprachcode der vollständigsten Sprache (zweite Fallback-Stufe).</summary>
    public const string ZweitFallbackCode = "de";

    private static readonly LanguagePack[] _pakete =
    [
        new TexteDe(),
        new TexteEn(),
        new TexteEs(),
        new TexteRo(),
        new TextePl(),
        new TexteRu(),
        new TexteUk(),
        new TexteVi()
    ];

    private static readonly Dictionary<string, LanguagePack> _nachCode =
        _pakete.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Alle Sprachpakete in Anzeige-Reihenfolge.</summary>
    public static IReadOnlyList<LanguagePack> Alle => _pakete;

    /// <summary>Alle unterstützten Sprachcodes.</summary>
    public static IEnumerable<string> Codes
    {
        get
        {
            foreach (var paket in _pakete)
                yield return paket.Code;
        }
    }

    /// <summary>Sprachpaket zu einem Code, oder null wenn nicht unterstützt.</summary>
    public static LanguagePack? Finden(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        return _nachCode.TryGetValue(code, out var paket) ? paket : null;
    }

    /// <summary>True, wenn der Sprachcode unterstützt wird.</summary>
    public static bool Unterstuetzt(string? code) => Finden(code) != null;

    /// <summary>Anzeigename einer Sprache; unbekannte Codes werden unverändert geliefert.</summary>
    public static string Anzeigename(string code) => Finden(code)?.Anzeigename ?? code;

    /// <summary>Länderkürzel für die Flagge; unbekannte Codes ergeben den Code in Großbuchstaben.</summary>
    public static string Flagge(string code) => Finden(code)?.Flagge ?? code.ToUpperInvariant();
}
