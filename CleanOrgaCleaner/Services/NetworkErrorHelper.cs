namespace CleanOrgaCleaner.Services;

/// <summary>
/// Erkennt anhand der Fehlermeldung, ob es sich um einen Transport-/Netzfehler
/// handelt (kein Netz, Zeitüberschreitung, DNS, Socket). Wird an mehreren
/// Stellen genutzt (Seiten + Offline-Warteschlange), daher zentral hier.
/// </summary>
public static class NetworkErrorHelper
{
    /// <summary>Kennzeichnende Wortteile einer Netz-Fehlermeldung.</summary>
    private static readonly string[] _kennzeichen =
    {
        "network", "timeout", "timedout", "connection", "internet",
        "unreachable", "net_http", "failure", "host", "refused"
    };

    /// <summary>True, wenn die Meldung auf ein Netzproblem hindeutet.</summary>
    public static bool IsNetworkError(string? error)
    {
        if (string.IsNullOrEmpty(error))
            return false;

        // Ohne ToLowerInvariant: der Vergleich läuft direkt ohne Kopie der
        // Zeichenkette (wird bei jeder fehlgeschlagenen Anfrage aufgerufen).
        foreach (var kennzeichen in _kennzeichen)
        {
            if (error.Contains(kennzeichen, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
