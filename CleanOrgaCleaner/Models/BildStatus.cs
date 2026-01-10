using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Represents an image with note attached to a cleaning task
/// </summary>
public class BildStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("pfad")]
    public string Pfad { get; set; } = "";

    [JsonPropertyName("notiz")]
    public string Notiz { get; set; } = "";

    [JsonPropertyName("erstellt_am")]
    public string ErstelltAm { get; set; } = "";

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("full_url")]
    public string? FullUrl { get; set; }

    /// <summary>
    /// URL for displaying the image (prefers full_url, falls back to thumbnail_url)
    /// </summary>
    public string Url => FullUrl ?? ThumbnailUrl ?? "";
}
