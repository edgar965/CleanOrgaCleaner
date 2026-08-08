using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf das Löschen eines Problems oder einer Anmerkung.
/// </summary>
public class ImageListDescriptionDeleteResponse : ServerAntwort
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
