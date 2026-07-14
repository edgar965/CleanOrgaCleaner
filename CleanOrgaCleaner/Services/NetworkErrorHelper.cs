namespace CleanOrgaCleaner.Services;

/// <summary>
/// Erkennt anhand der Fehlermeldung, ob es sich um einen Transport-/Netzwerk-
/// fehler handelt (kein Netz, Timeout, DNS, Socket). Wird an mehreren Stellen
/// genutzt (Views + Offline-Queue), daher zentral in Services.
/// </summary>
public static class NetworkErrorHelper
{
    public static bool IsNetworkError(string? error)
    {
        if (string.IsNullOrEmpty(error)) return false;
        var lowerError = error.ToLowerInvariant();
        return lowerError.Contains("network") ||
               lowerError.Contains("timeout") ||
               lowerError.Contains("timedout") ||
               lowerError.Contains("connection") ||
               lowerError.Contains("internet") ||
               lowerError.Contains("unreachable") ||
               lowerError.Contains("net_http") ||
               lowerError.Contains("failure") ||
               lowerError.Contains("host") ||
               lowerError.Contains("refused");
    }
}
