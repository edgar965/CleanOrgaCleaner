using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Eine der Arbeitskraft zugewiesene Aufgabe (Server: CleaningTask).
/// Reines Datenmodell der API-Antwort; Anzeige-Logik gehört in die View.
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

    [JsonPropertyName("aufgabenart_farbe")]
    public string AufgabenartFarbe { get; set; } = Farben.StandardHex;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("state_completed")]
    public string StateCompleted { get; set; } = "not_started";

    [JsonPropertyName("planned_date")]
    public string PlannedDate { get; set; } = "";

    [JsonPropertyName("aufgabe")]
    public string? Aufgabe { get; set; }

    /// <summary>
    /// Server-Übersetzungen der Aufgabenbeschreibung, Schlüssel = Sprachcode.
    /// Bleibt bewusst eine Map: die Sprachen kommen dynamisch vom Server.
    /// </summary>
    [JsonPropertyName("aufgabe_translated")]
    public Dictionary<string, string>? AufgabeTranslated { get; set; }

    [JsonPropertyName("anmerkung_mitarbeiter")]
    public string? AnmerkungMitarbeiter { get; set; }

    /// <summary>Alte, rein textbasierte Checkliste (wird von <see cref="Putzliste"/> abgelöst).</summary>
    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }

    /// <summary>Abhak-Status der alten Checkliste, Schlüssel = Position als Text.</summary>
    [JsonPropertyName("checklist_status")]
    public Dictionary<string, bool>? ChecklistStatus { get; set; }

    /// <summary>
    /// Neue Checkliste pro Apartment + Aufgabenart (Name, Beschreibung, Vorgabebilder,
    /// Abhaken, Beweis-Fotos). Ergänzt die alte string-basierte <see cref="Checkliste"/>.
    /// </summary>
    [JsonPropertyName("putzliste")]
    public List<PutzlisteEintrag>? Putzliste { get; set; }

    /// <summary>Anmerkung der Arbeitskraft zur gesamten Checkliste dieser Aufgabe.</summary>
    [JsonPropertyName("putzliste_kommentar")]
    public string? PutzlisteKommentar { get; set; }

    [JsonPropertyName("problems")]
    public List<ImageListDescription>? Problems { get; set; }

    [JsonPropertyName("anmerkungen")]
    public List<ImageListDescription>? Anmerkungen { get; set; }

    [JsonPropertyName("owner_id")]
    public int? OwnerId { get; set; }

    [JsonPropertyName("is_own_task")]
    public bool IsOwnTask { get; set; }

    [JsonPropertyName("assignments")]
    public TaskAssignments? Assignments { get; set; }

    /// <summary>
    /// Anzeigename der Aufgabe: eigener Name, sonst die Aufgabenart.
    /// </summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(Name) ? Aufgabenart : Name;

    /// <summary>
    /// Ist die Aufgabe fertig? Prüft beide Server-Felder: state_completed
    /// (Detail-API) und status (today-data-API).
    /// </summary>
    [JsonIgnore]
    public bool IsCompleted => StateCompleted == "completed"
        || Status == "completed" || Status == "cleaned" || Status == "checked";
}
