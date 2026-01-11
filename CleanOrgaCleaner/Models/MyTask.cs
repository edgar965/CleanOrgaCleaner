using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Represents a manually created task by the user
/// </summary>
public class MyTask
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }

    [JsonPropertyName("apartment_id")]
    public int? ApartmentId { get; set; }

    [JsonPropertyName("apartment_name")]
    public string? ApartmentName { get; set; }

    [JsonPropertyName("planned_date")]
    public string PlannedDate { get; set; } = "";

    [JsonPropertyName("wichtiger_hinweis")]
    public string? WichtigerHinweis { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "imported";

    [JsonPropertyName("aufgabenart_id")]
    public int? AufgabenartId { get; set; }

    [JsonPropertyName("aufgabenart_name")]
    public string? AufgabenartName { get; set; }

    [JsonPropertyName("assignments")]
    public TaskAssignments? Assignments { get; set; }

    /// <summary>
    /// Display text for status
    /// </summary>
    public string StatusDisplay => Status switch
    {
        "imported" => "Nicht zugewiesen",
        "assigned" => "Zugewiesen",
        "cleaned" => "Geputzt",
        "checked" => "Gecheckt",
        _ => Status
    };

    /// <summary>
    /// Color for status display
    /// </summary>
    public Color StatusColor => Status switch
    {
        "imported" => Color.FromArgb("#9e9e9e"),
        "assigned" => Color.FromArgb("#ff9800"),
        "cleaned" => Color.FromArgb("#2196F3"),
        "checked" => Color.FromArgb("#4CAF50"),
        _ => Color.FromArgb("#9e9e9e")
    };
}

/// <summary>
/// Task assignments for cleaning, check and repair roles
/// </summary>
public class TaskAssignments
{
    [JsonPropertyName("cleaning")]
    public List<int>? Cleaning { get; set; }

    [JsonPropertyName("check")]
    public int? Check { get; set; }

    [JsonPropertyName("repare")]
    public List<int>? Repare { get; set; }
}

/// <summary>
/// Response for my tasks list
/// </summary>
public class MyTasksResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("tasks")]
    public List<MyTask>? Tasks { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Response for task detail
/// </summary>
public class MyTaskDetailResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("task")]
    public MyTask? Task { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Apartment info for dropdown
/// </summary>
public class ApartmentInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }
}

/// <summary>
/// Task type info for dropdown
/// </summary>
public class AufgabenartInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }
}

/// <summary>
/// Response for my tasks page data
/// </summary>
public class MyTasksPageDataResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("tasks")]
    public List<MyTask>? Tasks { get; set; }

    [JsonPropertyName("apartments")]
    public List<ApartmentInfo>? Apartments { get; set; }

    [JsonPropertyName("aufgabenarten")]
    public List<AufgabenartInfo>? Aufgabenarten { get; set; }

    [JsonPropertyName("cleaners")]
    public List<CleanerInfo>? Cleaners { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
