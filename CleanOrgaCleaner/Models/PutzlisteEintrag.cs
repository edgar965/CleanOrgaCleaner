using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Ein Eintrag der (neuen) Checkliste/Putzliste – pro Apartment + Aufgabenart.
/// Name + Beschreibung, Vorgabebilder (Verwaltung), Abhak-Status und
/// Beweis-Fotos (Arbeitskraft).
/// </summary>
public class PutzlisteEintrag
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("beschreibung")]
    public string Beschreibung { get; set; } = "";

    [JsonPropertyName("checked")]
    public bool Checked { get; set; }

    /// <summary>Anmerkung der Arbeitskraft zu diesem Eintrag.</summary>
    [JsonPropertyName("kommentar")]
    public string Kommentar { get; set; } = "";

    /// <summary>Vorgabebilder (von der Verwaltung hinterlegt).</summary>
    [JsonPropertyName("bilder")]
    public List<PutzlisteBild>? Bilder { get; set; }

    /// <summary>Beweis-Fotos (von der Arbeitskraft hochgeladen).</summary>
    [JsonPropertyName("fotos")]
    public List<PutzlisteBild>? Fotos { get; set; }

    [JsonIgnore]
    public bool HasBilder => Bilder is { Count: > 0 };

    [JsonIgnore]
    public bool HasFotos => Fotos is { Count: > 0 };

    [JsonIgnore]
    public bool HasBeschreibung => !string.IsNullOrWhiteSpace(Beschreibung);
}
