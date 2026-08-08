using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline.Aufgaben;

/// <summary>
/// Nachgeholtes Einzelfoto als Anmerkung (Vorgangsart "image").
/// </summary>
public sealed class EinzelbildSenden : WarteschlangenAufgabe
{
    public EinzelbildSenden(JsonElement daten) : base(daten) { }

    public override async Task<bool> AusfuehrenAsync(ApiService api)
    {
        var aufgabenId = PflichtZahl("taskId");
        var base64 = PflichtText("imageBase64");
        if (string.IsNullOrEmpty(base64))
            return true;

        var notiz = Text("notes");
        var fotos = new List<(string, byte[])> { ("offline_image.jpg", Convert.FromBase64String(base64)) };

        var antwort = await api.CreateImageListItemAsync(
            aufgabenId, "anmerkung", notiz ?? "Anmerkung", null, fotos).ConfigureAwait(false);
        return antwort.Success;
    }
}
