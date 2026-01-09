using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Work status for a cleaner on a specific day
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

    // UI helpers
    public bool HasStarted => !string.IsNullOrEmpty(StartTime);
    public bool HasEnded => !string.IsNullOrEmpty(EndTime);

    public string DisplayStartTime => StartTime ?? "-";
    public string DisplayEndTime => EndTime ?? "-";
    public string DisplayTotalHours => TotalHours.HasValue
        ? TotalHours.Value.ToString("F2").Replace(".", ",") + "h"
        : "-";
}
