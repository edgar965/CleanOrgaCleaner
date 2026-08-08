using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>Ein Bild (Vorgabe oder Beweis) eines Putzlisten-Eintrags.</summary>
public class PutzlisteBild
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
