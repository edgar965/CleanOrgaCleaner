using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>Nachgeholte Chat-Nachricht (Vorgangsart "chat").</summary>
public sealed class ChatNachrichtSenden : WarteschlangenAufgabe
{
    public ChatNachrichtSenden(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var text = PflichtText("message");
        if (string.IsNullOrEmpty(text))
            return true; // nichts zu senden - Eintrag darf weg

        // Empfänger aus den Nutzdaten; ältere Einträge (ohne Empfänger) gehen
        // weiterhin an die Verwaltung
        var empfaenger = Text("receiver", "admin") ?? "admin";
        var antwort = await api.SendChatMessageAsync(text, empfaenger).ConfigureAwait(false);
        return antwort.Success;
    }
}
