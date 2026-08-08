using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Inhalt der Datei cached_tasks.json: die Aufgaben eines Tages samt
/// Zeitstempel, damit ein veralteter Stand erkannt wird.
/// </summary>
public class TaskCacheData
{
    /// <summary>Die zwischengespeicherten Aufgaben.</summary>
    public List<CleaningTask> Tasks { get; set; } = new();

    /// <summary>Zeitpunkt des Speicherns (UTC).</summary>
    public DateTime CachedAt { get; set; }

    /// <summary>Tag des Speicherns als yyyy-MM-dd.</summary>
    public string CachedDate { get; set; } = "";
}
