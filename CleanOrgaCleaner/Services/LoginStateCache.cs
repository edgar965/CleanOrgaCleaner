namespace CleanOrgaCleaner.Services;

/// <summary>
/// Inhalt der Datei login_state.json: der zuletzt erfolgreiche Login, damit
/// die App ohne Netz starten kann.
/// </summary>
public class LoginStateCache
{
    /// <summary>Name der Arbeitskraft.</summary>
    public string CleanerName { get; set; } = "";

    /// <summary>Sprachkürzel (Standard: de).</summary>
    public string Language { get; set; } = "de";

    /// <summary>Id der Arbeitskraft.</summary>
    public int? CleanerId { get; set; }

    /// <summary>Zeitpunkt der letzten Anmeldung (UTC).</summary>
    public DateTime LastLoginAt { get; set; }

    /// <summary>False = Eintrag nicht mehr verwenden.</summary>
    public bool IsValid { get; set; }
}
