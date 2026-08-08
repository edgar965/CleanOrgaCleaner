using Microsoft.Maui.Graphics;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Farbpalette der Modelle. Die Farben werden einmalig erzeugt statt bei jedem
/// Binding-Zugriff neu geparst - Listen mit vielen Einträgen fragen die
/// Farb-Properties sonst pro Zeile erneut ab.
/// </summary>
public static class Farben
{
    /// <summary>Standardfarbe für Aufgabenarten ohne eigene Farbe.</summary>
    public const string StandardHex = "#667eea";

    /// <summary>Blau: einer Arbeitskraft zugewiesen.</summary>
    public static readonly Color Zugewiesen = Color.FromArgb(StandardHex);

    /// <summary>Grau: keine Zuweisung / kein Status.</summary>
    public static readonly Color Neutral = Color.FromArgb("#9e9e9e");
}
