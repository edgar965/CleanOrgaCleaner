using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort mit allen Problemen bzw. Anmerkungen einer Aufgabe.
/// </summary>
public class ImageListItemsResponse : ServerAntwort
{
    [JsonPropertyName("items")]
    public List<ImageListDescription>? Items { get; set; }
}
