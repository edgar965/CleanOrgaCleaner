using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort auf das Senden einer Chat-Nachricht.
/// </summary>
public class ChatSendResponse : ServerAntwort
{
    /// <summary>Die gespeicherte Nachricht, wie der Server sie kennt.</summary>
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }
}
