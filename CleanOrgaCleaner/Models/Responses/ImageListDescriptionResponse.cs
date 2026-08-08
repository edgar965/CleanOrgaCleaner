using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf Anlegen/Ändern eines Problems oder einer Anmerkung.
/// </summary>
public class ImageListDescriptionResponse : ServerAntwort
{
    [JsonPropertyName("item_id")]
    public int? ItemId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Der gespeicherte Eintrag, wie der Server ihn kennt.</summary>
    [JsonPropertyName("item")]
    public ImageListDescription? Item { get; set; }
}
