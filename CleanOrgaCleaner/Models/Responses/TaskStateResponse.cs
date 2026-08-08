using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf das Umschalten des Aufgaben-Status.
/// </summary>
public class TaskStateResponse : ServerAntwort
{
    /// <summary>Neuer Status: not_started, started oder completed.</summary>
    [JsonPropertyName("new_state")]
    public string? NewState { get; set; }
}
