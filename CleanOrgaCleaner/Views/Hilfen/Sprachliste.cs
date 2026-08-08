namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Reihenfolge der Sprachen im Auswahlfeld der Einstellungen.
///
/// Vorher lag das als Wörterbuch 0..7 in der Seite - für eine feste,
/// lückenlose Reihenfolge ist eine Liste das passende Mittel.
/// Die Reihenfolge MUSS zu den Einträgen in SettingsPage.xaml passen.
/// </summary>
public static class Sprachliste
{
    private static readonly string[] Codes =
    {
        "de", // Deutsch
        "en", // English
        "es", // Español
        "ro", // Română
        "pl", // Polski
        "ru", // Русский
        "uk", // Українська
        "vi"  // Tiếng Việt
    };

    public const string Standard = "de";

    /// <summary>Sprachkürzel zur Position im Auswahlfeld.</summary>
    public static string Code(int position)
        => position >= 0 && position < Codes.Length ? Codes[position] : Standard;

    /// <summary>Position im Auswahlfeld zu einem Sprachkürzel (0, wenn unbekannt).</summary>
    public static int Position(string code)
    {
        var index = Array.IndexOf(Codes, code);
        return index < 0 ? 0 : index;
    }
}
