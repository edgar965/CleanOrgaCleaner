using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort mit den Nachrichten einer Konversation.
/// </summary>
public class ChatMessagesResponse : ServerAntwort
{
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();
}
