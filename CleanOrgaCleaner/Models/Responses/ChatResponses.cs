using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Response from chat send API
/// </summary>
public class ChatSendResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Response from translation preview API
/// </summary>
public class TranslationPreviewResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

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

    // UI helpers
    public bool HasTranslation => !string.IsNullOrEmpty(Translated);
    public bool HasBackTranslation => !string.IsNullOrEmpty(BackTranslated);
}

/// <summary>
/// Response from chat messages list API
/// </summary>
public class ChatMessagesResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    // UI helper
    public int MessageCount => Messages.Count;
}

/// <summary>
/// Response from chat image upload API
/// </summary>
public class ChatImageUploadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
