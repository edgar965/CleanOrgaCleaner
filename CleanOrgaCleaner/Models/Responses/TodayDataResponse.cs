using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort von /mobile/api/today-data/: die heutigen Aufgaben und der
/// Arbeitszeit-Status der Arbeitskraft.
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
}
