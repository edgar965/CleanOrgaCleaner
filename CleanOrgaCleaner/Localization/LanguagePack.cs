namespace CleanOrgaCleaner.Localization;

/// <summary>
/// Ein Sprachpaket: Sprachcode, Anzeigename, Länderkürzel und alle Oberflächentexte
/// dieser Sprache. Je Sprache existiert genau eine abgeleitete Klasse in
/// <c>Localization/Sprachen</c>; damit ersetzt eine typisierte Klasse das frühere
/// verschachtelte Dictionary&lt;string, Dictionary&lt;string, string&gt;&gt;.
/// </summary>
public abstract class LanguagePack
{
    private readonly Dictionary<string, string> _texte;

    /// <param name="code">ISO-Sprachcode, z. B. "de".</param>
    /// <param name="anzeigename">Name in der jeweiligen Sprache, z. B. "Deutsch".</param>
    /// <param name="flagge">Länderkürzel für die Flaggen-Anzeige, z. B. "DE".</param>
    /// <param name="texte">Schlüssel-Text-Paare dieser Sprache.</param>
    protected LanguagePack(string code, string anzeigename, string flagge,
                           Dictionary<string, string> texte)
    {
        Code = code;
        Anzeigename = anzeigename;
        Flagge = flagge;
        _texte = texte;
    }

    /// <summary>ISO-Sprachcode (klein geschrieben), z. B. "de".</summary>
    public string Code { get; }

    /// <summary>Anzeigename der Sprache in der Sprache selbst.</summary>
    public string Anzeigename { get; }

    /// <summary>Länderkürzel für die Flaggen-Anzeige, z. B. "DE".</summary>
    public string Flagge { get; }

    /// <summary>Anzahl hinterlegter Texte - für Diagnose/Tests.</summary>
    public int Anzahl => _texte.Count;

    /// <summary>
    /// Liefert den Text zu einem Schlüssel. False, wenn diese Sprache den
    /// Schlüssel nicht kennt (dann greift die Fallback-Kette in <see cref="Translations"/>).
    /// </summary>
    public bool TryGetText(string schluessel, out string text)
    {
        if (_texte.TryGetValue(schluessel, out var wert))
        {
            text = wert;
            return true;
        }

        text = "";
        return false;
    }

    /// <summary>Alle Schlüssel dieser Sprache - für Sync-Prüfungen.</summary>
    public IEnumerable<string> Schluessel => _texte.Keys;
}
