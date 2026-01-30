using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Represents a cleaning task assigned to a cleaner
/// </summary>
public class CleaningTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("apartment_name")]
    public string ApartmentName { get; set; } = "";

    [JsonPropertyName("apartment_id")]
    public int ApartmentId { get; set; }

    [JsonPropertyName("aufgabenart")]
    public string Aufgabenart { get; set; } = "Reinigung";

    /// <summary>
    /// Display name for the task (Name or Aufgabenart fallback)
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : Aufgabenart;

    [JsonPropertyName("aufgabenart_farbe")]
    public string AufgabenartFarbe { get; set; } = "#667eea";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("state_completed")]
    public string StateCompleted { get; set; } = "not_started";

    [JsonPropertyName("planned_date")]
    public string PlannedDate { get; set; } = "";

    [JsonPropertyName("aufgabe")]
    public string? Aufgabe { get; set; }

    [JsonPropertyName("aufgabe_translated")]
    public Dictionary<string, string>? AufgabeTranslated { get; set; }

    [JsonPropertyName("anmerkung_mitarbeiter")]
    public string? AnmerkungMitarbeiter { get; set; }

    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }

    [JsonPropertyName("checklist_status")]
    public Dictionary<string, bool>? ChecklistStatus { get; set; }

    [JsonPropertyName("bilder")]
    public List<BildStatus>? Bilder { get; set; }

    [JsonPropertyName("probleme")]
    public List<Problem>? Probleme { get; set; }

    [JsonPropertyName("owner_id")]
    public int? OwnerId { get; set; }

    [JsonPropertyName("is_own_task")]
    public bool IsOwnTask { get; set; }

    #region Computed Properties for UI

    /// <summary>
    /// Display text for current status
    /// </summary>
    public string StatusDisplay => StateCompleted switch
    {
        "not_started" => "Offen",
        "started" => "Laeuft",
        "completed" => "Fertig",
        _ => Status switch
        {
            "pending" => "Offen",
            "in_progress" => "Laeuft",
            "completed" => "Fertig",
            _ => Status
        }
    };

    /// <summary>
    /// Color for status display
    /// </summary>
    public Color StatusColor => StateCompleted switch
    {
        "not_started" => Color.FromArgb("#ff9800"),
        "started" => Color.FromArgb("#2196F3"),
        "completed" => Color.FromArgb("#4CAF50"),
        _ => Color.FromArgb("#9e9e9e")
    };

    /// <summary>
    /// Background color based on task type
    /// </summary>
    public Color TaskColor
    {
        get
        {
            try { return Color.FromArgb(AufgabenartFarbe); }
            catch { return Color.FromArgb("#667eea"); }
        }
    }

    /// <summary>
    /// Is the task completed?
    /// Checks both state_completed (from detail API) and status (from today-data API)
    /// </summary>
    public bool IsCompleted => StateCompleted == "completed"
        || Status == "completed" || Status == "cleaned" || Status == "checked";

    /// <summary>
    /// Is the task currently in progress?
    /// Checks both state_completed (from detail API) and status (from today-data API)
    /// </summary>
    public bool IsStarted => StateCompleted == "started"
        || Status == "in_progress" || Status == "cleaning_in_progress";

    /// <summary>
    /// Has the task not been started yet?
    /// </summary>
    public bool IsNotStarted => !IsCompleted && !IsStarted;

    /// <summary>
    /// Does this task have a checklist?
    /// </summary>
    public bool HasChecklist => Checkliste != null && Checkliste.Count > 0;

    /// <summary>
    /// Number of checklist items
    /// </summary>
    public int ChecklistCount => Checkliste?.Count ?? 0;

    /// <summary>
    /// Number of completed checklist items
    /// </summary>
    public int ChecklistCompletedCount
    {
        get
        {
            if (ChecklistStatus == null) return 0;
            return ChecklistStatus.Count(x => x.Value);
        }
    }

    /// <summary>
    /// Does this task have problems reported?
    /// </summary>
    public bool HasProblems => Probleme != null && Probleme.Count > 0;

    /// <summary>
    /// Number of problems
    /// </summary>
    public int ProblemCount => Probleme?.Count ?? 0;

    /// <summary>
    /// Does this task have a task description?
    /// </summary>
    public bool HasAufgabe => !string.IsNullOrEmpty(Aufgabe);

    #endregion
}
