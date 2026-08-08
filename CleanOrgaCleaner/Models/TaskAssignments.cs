using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Zuweisungen einer Aufgabe: Reinigung (mehrere Arbeitskräfte), Kontrolle
/// (genau eine) und Reparatur (mehrere). IDs der Arbeitskräfte.
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
