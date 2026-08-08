using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort mit dem Protokoll einer Aufgabe.
/// </summary>
public class LogsResponse : ServerAntwort
{
    [JsonPropertyName("logs")]
    public List<LogEntry>? Logs { get; set; }
}
