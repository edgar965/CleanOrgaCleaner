using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>
/// Nachgeholtes Umschalten eines Checklisten-Eintrags (Vorgangsart "checklist").
/// Der Server schaltet lediglich um; das gespeicherte "completed" dient der
/// Nachvollziehbarkeit des Eintrags.
/// </summary>
public sealed class ChecklisteSchalten : WarteschlangenAufgabe
{
    public ChecklisteSchalten(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        var eintragId = PflichtZahl("itemId");
        _ = PflichtJaNein("completed"); // Pflichtfeld: defekte Nutzdaten sollen auffallen

        var antwort = await api.ToggleChecklistItemAsync(aufgabenId, eintragId).ConfigureAwait(false);
        return antwort.Success;
    }
}
