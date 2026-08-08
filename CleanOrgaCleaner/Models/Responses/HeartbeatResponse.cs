using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort des Heartbeat-/Ping-Endpunkts.
/// </summary>
public class HeartbeatResponse : ServerAntwort
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>Vom Server gewünschter Ping-Abstand in Sekunden.</summary>
    [JsonPropertyName("ping_interval")]
    public int PingInterval { get; set; } = 30;
}
