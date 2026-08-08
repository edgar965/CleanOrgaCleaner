using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>
/// Nachgeholte Zustandsänderung einer Aufgabe (Vorgangsart "task_state").
/// </summary>
public sealed class AufgabenzustandSetzen : WarteschlangenAufgabe
{
    public AufgabenzustandSetzen(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        var zustand = PflichtText("newState") ?? "";

        var antwort = await api.UpdateTaskStateAsync(aufgabenId, zustand).ConfigureAwait(false);
        return antwort.Success;
    }
}
