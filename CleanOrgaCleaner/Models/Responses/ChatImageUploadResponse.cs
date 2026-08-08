using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf den Upload eines Chat-Bildes.
/// </summary>
public class ChatImageUploadResponse : ServerAntwort
{
    /// <summary>Serverpfad des gespeicherten Bildes.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
}
