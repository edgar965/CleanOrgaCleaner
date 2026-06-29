using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Ein Eintrag der (neuen) Checkliste/Putzliste – pro Apartment + Aufgabenart.
/// Name + Beschreibung, Vorgabebilder (Admin), Abhak-Status und Beweis-Fotos (Putzkraft).
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

    /// <summary>Anmerkung des Cleaners zu diesem Eintrag.</summary>
    [JsonPropertyName("kommentar")]
    public string Kommentar { get; set; } = "";

    /// <summary>Vorgabebilder (vom Admin hinterlegt).</summary>
    [JsonPropertyName("bilder")]
    public List<PutzlisteBild>? Bilder { get; set; }

    /// <summary>Beweis-Fotos (von der Putzkraft hochgeladen).</summary>
    [JsonPropertyName("fotos")]
    public List<PutzlisteBild>? Fotos { get; set; }

    public bool HasBilder => Bilder != null && Bilder.Count > 0;
    public bool HasFotos => Fotos != null && Fotos.Count > 0;
    public bool HasBeschreibung => !string.IsNullOrWhiteSpace(Beschreibung);
}

/// <summary>Ein Bild (Vorgabe oder Beweis) eines Putzlisten-Eintrags.</summary>
public class PutzlisteBild
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
