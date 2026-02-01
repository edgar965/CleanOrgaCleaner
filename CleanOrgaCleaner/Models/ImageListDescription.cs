using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Photo attached to an ImageListDescription
/// </summary>
public class ImageListDescriptionPhoto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Unified model for Problems and Notes (Anmerkungen)
/// Replaces the old Problem and BildStatus models
/// </summary>
public class ImageListDescription
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = ""; // "problem" or "anmerkung"

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("photos")]
    public List<ImageListDescriptionPhoto>? Photos { get; set; }

    [JsonPropertyName("erstellt_am")]
    public string? ErstelltAm { get; set; }

    [JsonPropertyName("erledigt")]
    public bool Erledigt { get; set; }

    // UI helpers
    public bool HasPhotos => Photos != null && Photos.Count > 0;
    public int PhotoCount => Photos?.Count ?? 0;

    public bool IsProblem => Type == "problem";
    public bool IsAnmerkung => Type == "anmerkung";

    /// <summary>
    /// Get the first photo URL (for thumbnail display)
    /// </summary>
    public string? FirstPhotoUrl => Photos?.FirstOrDefault()?.Url
                                    ?? Photos?.FirstOrDefault()?.ThumbnailUrl;
}
