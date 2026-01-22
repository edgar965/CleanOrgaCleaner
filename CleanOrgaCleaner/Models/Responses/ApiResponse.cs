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

    /// <summary>
    /// Get the error message or a default message
    /// </summary>
    public string GetErrorMessage(string defaultMessage = "Ein Fehler ist aufgetreten")
    {
        return Error ?? Message ?? defaultMessage;
    }
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
