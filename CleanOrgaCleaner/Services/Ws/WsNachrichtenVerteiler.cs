using System.Text.Json;
using CleanOrgaCleaner.Json;
using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Services.Ws;

/// <summary>
/// Wertet eine WebSocket-Nachricht aus und reicht sie an die richtige Stelle
/// weiter: Chat-Nachricht, Ping-Antwort (wird verworfen) oder Aufgaben-Update.
/// </summary>
public sealed class WsNachrichtenVerteiler
{
    private readonly Action<ChatMessage> _beiChatNachricht;
    private readonly Action<string> _beiAufgabenUpdate;

    public WsNachrichtenVerteiler(Action<ChatMessage> beiChatNachricht, Action<string> beiAufgabenUpdate)
    {
        _beiChatNachricht = beiChatNachricht;
        _beiAufgabenUpdate = beiAufgabenUpdate;
    }

    /// <summary>Eine empfangene Nachricht auswerten.</summary>
    public void Verteile(string json)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"WS received: {json}");

            // using: JsonDocument mietet interne Puffer - ohne Freigabe blieben
            // sie bei jeder Nachricht liegen.
            using var doc = JsonDocument.Parse(json);
            var wurzel = doc.RootElement;

            if (!wurzel.TryGetProperty("type", out var artFeld))
                return;

            var art = artFeld.GetString();

            if (art == "chat_message" && wurzel.TryGetProperty("message", out var nachrichtFeld))
            {
                var nachricht = JsonSerializer.Deserialize(nachrichtFeld.GetRawText(), AppJsonContext.Default.ChatMessage);
                if (nachricht != null)
                    _beiChatNachricht(nachricht);
                return;
            }

            if (art == "pong")
                return; // Antwort auf unseren Keepalive-Ping

            _beiAufgabenUpdate(art ?? "update");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessMessage error: {ex.Message}");
        }
    }
}
