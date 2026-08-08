namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Ergebnis eines Anmeldeversuchs für die App-Seite (kein Server-JSON):
/// Erfolg plus die Daten, die die Oberfläche danach braucht.
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CleanerName { get; set; }
    public string? CleanerLanguage { get; set; }
    public int? CleanerId { get; set; }
}
