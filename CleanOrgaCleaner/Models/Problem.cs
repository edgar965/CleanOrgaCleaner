using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Problem reported for a cleaning task
/// </summary>
public class Problem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("beschreibung")]
    public string? Beschreibung { get; set; }

    [JsonPropertyName("fotos")]
    public List<string>? Fotos { get; set; }

    [JsonPropertyName("datum")]
    public string? Datum { get; set; }

    [JsonPropertyName("erledigt")]
    public bool Erledigt { get; set; }

    // UI helper
    public bool HasPhotos => Fotos != null && Fotos.Count > 0;
    public int PhotoCount => Fotos?.Count ?? 0;
}
