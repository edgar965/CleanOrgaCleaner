using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf Start/Ende der Arbeitszeit sowie auf die Statusabfrage.
/// </summary>
public class WorkTimeResponse : ServerAntwort
{
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("total_hours")]
    public double? TotalHours { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("is_working")]
    public bool IsWorking { get; set; }
}
