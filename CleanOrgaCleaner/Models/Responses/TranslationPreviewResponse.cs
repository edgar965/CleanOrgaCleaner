using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort der Übersetzungsvorschau: Übersetzung und Rückübersetzung eines
/// eingetippten Textes.
/// </summary>
public class TranslationPreviewResponse : ServerAntwort
{
    [JsonPropertyName("translated")]
    public string? Translated { get; set; }

    [JsonPropertyName("back_translated")]
    public string? BackTranslated { get; set; }

    [JsonPropertyName("source_lang")]
    public string? SourceLang { get; set; }

    [JsonPropertyName("target_lang")]
    public string? TargetLang { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
