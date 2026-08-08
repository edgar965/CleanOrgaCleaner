using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Allgemeine Antwort für einfache Erfolg/Fehler-Aufrufe.
/// </summary>
public class ApiResponse : ServerAntwort
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("task_id")]
    public int? TaskId { get; set; }
}
