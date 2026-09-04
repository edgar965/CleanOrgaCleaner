using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Eine von der Arbeitskraft selbst angelegte Aufgabe.
/// </summary>
public class Auftrag
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Kurzer Titel der Aufgabe - steht auf der Kachel und in der Liste.</summary>
    [JsonPropertyName("titel")]
    public string? Titel { get; set; }

    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }

    [JsonPropertyName("apartment_id")]
    public int? ApartmentId { get; set; }

    [JsonPropertyName("apartment_name")]
    public string? ApartmentName { get; set; }

    [JsonPropertyName("planned_date")]
    public string PlannedDate { get; set; } = "";

    [JsonPropertyName("aufgabe")]
    public string? Aufgabe { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "imported";

    [JsonPropertyName("aufgabenart_id")]
    public int? AufgabenartId { get; set; }

    [JsonPropertyName("aufgabenart_name")]
    public string? AufgabenartName { get; set; }

    [JsonPropertyName("assignments")]
    public TaskAssignments? Assignments { get; set; }

    [JsonPropertyName("assigned_cleaner_names")]
    public List<string>? AssignedCleanerNames { get; set; }

    [JsonPropertyName("anmerkungen")]
    public List<ImageListDescription>? Anmerkungen { get; set; }

    /// <summary>
    /// Beschriftung in der Liste: der Titel, solange es einen gibt.
    ///
    /// Aufgaben aus der Zeit vor dem Titelfeld (und die aus dem Kalender-Import)
    /// haben keinen - dort bleibt der Name stehen, sonst waere die Zeile leer.
    /// </summary>
    [JsonIgnore]
    public string Anzeigename => string.IsNullOrWhiteSpace(Titel) ? Name : Titel!;

    /// <summary>True, wenn mindestens eine Arbeitskraft zugewiesen ist.</summary>
    [JsonIgnore]
    public bool IstZugewiesen => AssignedCleanerNames is { Count: > 0 };

    /// <summary>
    /// Anzeigetext: Namen der zugewiesenen Arbeitskräfte, sonst leer
    /// (bewusst nicht "Nicht zugewiesen").
    /// </summary>
    [JsonIgnore]
    public string StatusDisplay => IstZugewiesen
        ? string.Join(", ", AssignedCleanerNames!)
        : "";

    /// <summary>Blau bei Zuweisung, sonst grau - Farben aus der gemeinsamen Palette.</summary>
    [JsonIgnore]
    public Color StatusColor => IstZugewiesen ? Farben.Zugewiesen : Farben.Neutral;
}
