using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>Antwort auf den Upload eines Beweis-Fotos zu einem Putzlisten-Eintrag.</summary>
public class PutzlisteFotoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
