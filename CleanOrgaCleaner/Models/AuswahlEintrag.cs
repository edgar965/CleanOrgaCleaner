using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Gemeinsame Basis der Auswahl-Einträge für die Picker der Aufgaben-Maske
/// (Apartment, Aufgabenart). Beide Listen liefern dieselben Felder - die
/// gemeinsame Basis vermeidet zwei identische Klassen.
/// </summary>
public abstract class AuswahlEintrag
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Vorlage für die Checkliste, die beim Auswählen übernommen wird.</summary>
    [JsonPropertyName("checkliste")]
    public List<string>? Checkliste { get; set; }
}
