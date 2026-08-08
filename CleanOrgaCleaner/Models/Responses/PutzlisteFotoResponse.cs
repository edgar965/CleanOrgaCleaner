using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>Antwort auf den Upload eines Beweis-Fotos zu einem Putzlisten-Eintrag.</summary>
public class PutzlisteFotoResponse : ServerAntwort
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
