using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>Nachgeholtes Arbeitsende (Vorgangsart "work_stop").</summary>
public sealed class ArbeitsendeMelden : WarteschlangenAufgabe
{
    public ArbeitsendeMelden(JsonElement daten) : base(daten) { }

    public override Task<bool> AusfuehrenAsync(ApiService api) => api.StopWorkAsync();
}
