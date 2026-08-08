using CleanOrgaCleaner.Localization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Sprachen der App - schlanke Fassade auf den <see cref="TranslationCatalog"/>.
/// Die früheren Dictionaries (Anzeigename, Flagge) sind entfallen: die Daten
/// stehen jetzt als typisierte Member am jeweiligen <see cref="LanguagePack"/>
/// und existieren damit nur noch an einer Stelle.
/// </summary>
public static class Language
{
    /// <summary>Standardsprache, wenn nichts gespeichert und nichts erkannt wurde.</summary>
    public const string Default = "de";

    /// <summary>Alle unterstützten Sprachen mit Code, Anzeigename und Flagge.</summary>
    public static IReadOnlyList<LanguagePack> Alle => TranslationCatalog.Alle;

    /// <summary>Anzeigename zu einem Sprachcode ("de" -> "Deutsch").</summary>
    public static string GetDisplayName(string code) => TranslationCatalog.Anzeigename(code);

    /// <summary>Länderkürzel für die Flagge ("uk" -> "UA").</summary>
    public static string GetFlag(string code) => TranslationCatalog.Flagge(code);
}
