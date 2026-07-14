using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Generic API response for simple success/error responses
/// </summary>
public class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("task_id")]
    public int? TaskId { get; set; }
}

/// <summary>
/// Response from heartbeat/ping endpoint
/// </summary>
public class HeartbeatResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("ping_interval")]
    public int PingInterval { get; set; } = 30;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
