using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>Nachgeholte Änderung eines Auftrags (Vorgangsart "task_update").</summary>
public sealed class AuftragAendern : WarteschlangenAufgabe
{
    public AuftragAendern(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        var felder = AuftragFelder.Lies(Daten);

        var antwort = await api.UpdateAuftragAsync(
            aufgabenId, felder.Titel, felder.GeplantesDatum, felder.ApartmentId, felder.AufgabenartId,
            felder.Hinweis, felder.Status, felder.Zuordnungen).ConfigureAwait(false);
        return antwort.Success;
    }
}
