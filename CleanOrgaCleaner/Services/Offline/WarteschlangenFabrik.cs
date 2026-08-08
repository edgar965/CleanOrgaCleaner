using System.Text.Json;
using CleanOrgaCleaner.Services.Offline.Aufgaben;

namespace CleanOrgaCleaner.Services.Offline;

/// <summary>
/// Erzeugt zu einem Warteschlangen-Eintrag die passende Aufgabe.
/// Einzige Stelle, an der die Vorgangsarten den Klassen zugeordnet sind.
/// </summary>
public static class WarteschlangenFabrik
{
    /// <summary>
    /// Aufgabe zum Eintrag bauen; null bei unbekannter Vorgangsart
    /// (solche Einträge werden verworfen).
    /// </summary>
    public static WarteschlangenAufgabe? Erzeuge(OfflineQueueItem eintrag)
    {
        var daten = JsonSerializer.Deserialize<JsonElement>(eintrag.Payload);

        return eintrag.OperationType switch
        {
            "chat" => new ChatNachrichtSenden(daten),
            "status" => new AufgabenStatusMelden(daten),
            "image" => new EinzelbildSenden(daten),
            "checklist" => new ChecklisteSchalten(daten),
            "notes" => new NotizSpeichern(daten),
            "image_list_item" => new BildlistenEintragSenden(daten),
            "task_create" => new AuftragAnlegen(daten),
            "task_update" => new AuftragAendern(daten),
            "work_start" => new ArbeitsbeginnMelden(daten),
            "work_stop" => new ArbeitsendeMelden(daten),
            "task_state" => new AufgabenzustandSetzen(daten),
            _ => null
        };
    }
}
