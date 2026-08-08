using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>Nachgeholte Notiz zu einer Aufgabe (Vorgangsart "notes").</summary>
public sealed class NotizSpeichern : WarteschlangenAufgabe
{
    public NotizSpeichern(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        var notiz = PflichtText("notes") ?? "";

        var antwort = await api.SaveTaskNoteAsync(aufgabenId, notiz).ConfigureAwait(false);
        return antwort.Success;
    }
}
