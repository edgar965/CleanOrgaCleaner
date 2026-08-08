using SQLite;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Ein Eintrag der Offline-Warteschlange (SQLite-Tabelle).
/// Enthält bewusst nur Daten - die Ausführung steckt in den
/// Warteschlangen-Aufgaben unter Services/Offline.
/// </summary>
public class OfflineQueueItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Art des Vorgangs: chat, status, image, checklist, notes, ...</summary>
    public string OperationType { get; set; } = "";

    /// <summary>Nutzdaten des Vorgangs als JSON.</summary>
    public string Payload { get; set; } = "";

    /// <summary>Zeitpunkt der Einreihung.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Anzahl der bisherigen Versuche (nur zur Diagnose).</summary>
    public int RetryCount { get; set; }

    /// <summary>Letzte Fehlermeldung, falls vorhanden.</summary>
    public string? LastError { get; set; }

    /// <summary>Rang (kleiner = wichtiger): 1=Chat/Arbeitszeit, 2=Rest.</summary>
    public int Priority { get; set; }
}
