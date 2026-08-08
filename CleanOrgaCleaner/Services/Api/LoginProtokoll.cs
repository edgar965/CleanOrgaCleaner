namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Datei-Protokoll für den Login-Ablauf.
///
/// Auf iOS steht während des Logins kein UI-Thread für Bildschirm-Ausgaben zur
/// Verfügung; die Zeilen landen deshalb zusätzlich in
/// <c>CacheDirectory/login_debug.log</c> und können beim nächsten Start
/// angezeigt werden.
/// </summary>
public static class LoginProtokoll
{
    private const string Dateiname = "login_debug.log";

    private static string? _pfad;
    private static readonly object _sperre = new();

    /// <summary>Neues Protokoll beginnen (alte Datei verwerfen).</summary>
    public static void Beginne()
    {
        _pfad = Path.Combine(FileSystem.CacheDirectory, Dateiname);
        try
        {
            if (File.Exists(_pfad))
                File.Delete(_pfad);
        }
        catch { }
    }

    /// <summary>Protokoll des vorherigen Laufs lesen (null, wenn keins da ist).</summary>
    public static string? LiesVorherige()
    {
        try
        {
            var pfad = Path.Combine(FileSystem.CacheDirectory, Dateiname);
            if (File.Exists(pfad))
                return File.ReadAllText(pfad);
        }
        catch { }
        return null;
    }

    /// <summary>Zeile mit Zeitstempel schreiben (synchron, thread-sicher).</summary>
    public static void Schreibe(string meldung)
    {
        var zeile = $"[{DateTime.Now:HH:mm:ss.fff}] {meldung}";
        System.Diagnostics.Debug.WriteLine($"[LOGIN-DBG] {zeile}");

        if (_pfad == null)
            return;

        lock (_sperre)
        {
            try
            {
                File.AppendAllText(_pfad, zeile + "\n");
            }
            catch { }
        }
    }

    /// <summary>Zeile mit API-Kennung.</summary>
    public static void SchreibeApi(string meldung) => Schreibe($"[API] {meldung}");
}
