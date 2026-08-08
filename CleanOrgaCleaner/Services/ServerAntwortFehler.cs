namespace CleanOrgaCleaner.Services;

/// <summary>
/// Der Server hat geantwortet, aber mit Fehlerstatus (z.B. 500 während eines
/// Deploys). Bewusst KEIN Netzwerkfehler: Aufrufer wie die Tagesansicht dürfen
/// dann nicht auf den (womöglich tagealten) Offline-Zwischenspeicher
/// zurückfallen.
/// </summary>
public class ServerAntwortFehler : Exception
{
    /// <summary>HTTP-Status der Server-Antwort.</summary>
    public int StatusCode { get; }

    public ServerAntwortFehler(int statusCode)
        : base($"Server antwortete mit HTTP {statusCode}")
    {
        StatusCode = statusCode;
    }
}
