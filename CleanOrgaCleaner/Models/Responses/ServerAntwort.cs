using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Basis aller Server-Antworten. Jede Antwort der Django-API liefert
/// "success" und im Fehlerfall "error" - die beiden Felder standen bisher in
/// jeder Antwortklasse erneut.
/// </summary>
public abstract class ServerAntwort
{
    /// <summary>True, wenn der Server die Anfrage verarbeiten konnte.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Fehlertext des Servers, sonst null.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
