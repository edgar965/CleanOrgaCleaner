using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Response from task state update API
/// </summary>
public class TaskStateResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("new_state")]
    public string? NewState { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Response from checklist item toggle API
/// </summary>
public class ChecklistToggleResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("checked")]
    public bool Checked { get; set; }

    [JsonPropertyName("item_index")]
    public int ItemIndex { get; set; }
}

/// <summary>
/// Response from task detail API
/// </summary>
public class TaskDetailResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("task")]
    public CleaningTask? Task { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Log entry for task history
/// </summary>
public class LogEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("datum_zeit")]
    public string DatumZeit { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("user")]
    public string User { get; set; } = "";
}

/// <summary>
/// Response from task logs API
/// </summary>
public class LogsResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("logs")]
    public List<LogEntry>? Logs { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
