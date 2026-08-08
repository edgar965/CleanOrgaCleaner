using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>Nachgeholtes Anlegen eines Auftrags (Vorgangsart "task_create").</summary>
public sealed class AuftragAnlegen : WarteschlangenAufgabe
{
    public AuftragAnlegen(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var felder = AuftragFelder.Lies(Daten);
        var antwort = await api.CreateAuftragAsync(
            felder.Name, felder.GeplantesDatum, felder.ApartmentId, felder.AufgabenartId,
            felder.Hinweis, felder.Status, felder.Zuordnungen).ConfigureAwait(false);
        return antwort.Success;
    }
}
