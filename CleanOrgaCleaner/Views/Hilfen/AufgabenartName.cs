using CleanOrgaCleaner.Localization;

namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Übersetzt die vom Server auf Deutsch gelieferte Aufgabenart in die Sprache
/// der Arbeitskraft. Die Zuordnungstabelle ist statisch - vorher entstand sie
/// bei jedem Aufruf neu.
/// </summary>
public static class AufgabenartName
{
    private static readonly Dictionary<string, string> Schluessel = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Reinigung", "cleaning" },
        { "Check", "check_task" },
        { "Reparatur", "repair" },
        { "Putzen", "cleaning" }
    };

    /// <summary>
    /// Liefert die übersetzte Aufgabenart; ist keine Übersetzung hinterlegt,
    /// bleibt der ursprüngliche Text stehen.
    /// </summary>
    public static string Uebersetzt(string aufgabenart)
    {
        if (string.IsNullOrEmpty(aufgabenart)) return aufgabenart;

        if (!Schluessel.TryGetValue(aufgabenart, out var schluessel))
            return aufgabenart;

        var uebersetzt = Translations.Get(schluessel);
        return !string.IsNullOrEmpty(uebersetzt) && uebersetzt != schluessel ? uebersetzt : aufgabenart;
    }
}
