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
