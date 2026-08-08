using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>Nachgeholter Arbeitsbeginn (Vorgangsart "work_start").</summary>
public sealed class ArbeitsbeginnMelden : WarteschlangenAufgabe
{
    public ArbeitsbeginnMelden(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
        => (await api.StartWorkAsync().ConfigureAwait(false)).Success;
}
