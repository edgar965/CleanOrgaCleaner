using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>
/// Nachgeholter Start/Stopp einer Aufgabe (Vorgangsart "status").
/// </summary>
public sealed class AufgabenStatusMelden : WarteschlangenAufgabe
{
    public AufgabenStatusMelden(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        switch (PflichtText("action"))
        {
            case "start":
                return (await api.StartTaskAsync(aufgabenId).ConfigureAwait(false)).Success;
            case "stop":
                return (await api.StopTaskAsync(aufgabenId).ConfigureAwait(false)).Success;
            default:
                return true; // unbekannte Aktion - Eintrag verwerfen
        }
    }
}
