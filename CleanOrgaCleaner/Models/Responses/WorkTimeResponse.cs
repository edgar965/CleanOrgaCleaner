using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Response from work time start/end API endpoints
/// </summary>
public class WorkTimeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("total_hours")]
    public double? TotalHours { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    // UI helpers
    public string DisplayTotalHours => TotalHours.HasValue
        ? TotalHours.Value.ToString("F2").Replace(".", ",")
        : "?";
}
