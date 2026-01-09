using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Services;

public class CleaningTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("apartment_name")]
    public string ApartmentName { get; set; } = "";

    [JsonPropertyName("aufgabenart")]
    public string Aufgabenart { get; set; } = "Reinigung";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("planned_date")]
    public string PlannedDate { get; set; } = "";

    [JsonPropertyName("wichtiger_hinweis")]
    public string? WichtigerHinweis { get; set; }

    [JsonPropertyName("checklist")]
    public List<ChecklistItem>? Checklist { get; set; }

    public string StatusDisplay => Status switch
    {
        "pending" => "Offen",
        "in_progress" => "In Arbeit",
        "completed" => "Erledigt",
        _ => Status
    };

    public Color StatusColor => Status switch
    {
        "pending" => Colors.Orange,
        "in_progress" => Colors.Blue,
        "completed" => Colors.Green,
        _ => Colors.Gray
    };
}

public class ChecklistItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("checked")]
    public bool IsChecked { get; set; }
}

public class TodayDataResponse
{
    [JsonPropertyName("tasks")]
    public List<CleaningTask> Tasks { get; set; } = new();

    [JsonPropertyName("work_status")]
    public WorkStatus WorkStatus { get; set; } = new();
}

public class WorkStatus
{
    [JsonPropertyName("is_working")]
    public bool IsWorking { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }
}
