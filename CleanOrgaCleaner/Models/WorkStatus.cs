using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Arbeitszeit-Status einer Arbeitskraft für einen Tag.
/// </summary>
public class WorkStatus
{
    [JsonPropertyName("is_working")]
    public bool IsWorking { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    [JsonPropertyName("total_hours")]
    public double? TotalHours { get; set; }
}
