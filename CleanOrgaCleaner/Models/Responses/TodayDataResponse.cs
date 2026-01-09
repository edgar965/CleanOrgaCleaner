using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Response from /mobile/api/today-data/
/// Contains today's tasks and work status for the cleaner
/// </summary>
public class TodayDataResponse
{
    [JsonPropertyName("tasks")]
    public List<CleaningTask> Tasks { get; set; } = new();

    [JsonPropertyName("work_status")]
    public WorkStatus WorkStatus { get; set; } = new();

    [JsonPropertyName("cleaner_name")]
    public string? CleanerName { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    // UI helpers
    public int TaskCount => Tasks.Count;
    public int CompletedCount => Tasks.Count(t => t.IsCompleted);
    public int PendingCount => Tasks.Count(t => !t.IsCompleted);
    public bool HasTasks => Tasks.Count > 0;
}
