using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>
/// Nachgeholter Bildlisten-Eintrag - Problem oder Anmerkung samt Fotos
/// (Vorgangsart "image_list_item").
/// </summary>
public sealed class BildlistenEintragSenden : WarteschlangenAufgabe
{
    public BildlistenEintragSenden(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        var typ = PflichtText("itemType") ?? "problem";
        var name = PflichtText("name") ?? "";
        var beschreibung = Text("description");
        var fotos = Fotos("photos");

        var antwort = await api.CreateImageListItemAsync(aufgabenId, typ, name, beschreibung, fotos).ConfigureAwait(false);
        return antwort.Success;
    }
}
