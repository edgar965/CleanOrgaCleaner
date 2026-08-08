using System.Text.Json;
using CleanOrgaCleaner.Json;
using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Lokaler Datenbestand für den Offline-Betrieb: Aufgaben des Tages,
/// letzter Login und Bilder.
///
/// Die Bilder verwaltet <see cref="BildCache"/> - diese Klasse hält nur noch
/// die beiden JSON-Dateien.
/// </summary>
public class OfflineDataService
{
    private static readonly Lazy<OfflineDataService> _instanz = new(() => new OfflineDataService());

    /// <summary>Die eine Instanz der App.</summary>
    public static OfflineDataService Instance => _instanz.Value;

    /// <summary>Höchstalter eines gespeicherten Logins.</summary>
    private static readonly TimeSpan _loginHoechstalter = TimeSpan.FromDays(7);

    private readonly string _aufgabenDatei;
    private readonly string _loginDatei;
    private readonly BildCache _bilder;

    private OfflineDataService()
    {
        var verzeichnis = FileSystem.AppDataDirectory;
        _aufgabenDatei = Path.Combine(verzeichnis, "cached_tasks.json");
        _loginDatei = Path.Combine(verzeichnis, "login_state.json");
        _bilder = new BildCache(Path.Combine(verzeichnis, "image_cache"));
    }

    #region Aufgaben

    /// <summary>Aufgaben des Tages ablegen.</summary>
    public async Task SaveTasksAsync(List<CleaningTask> tasks)
    {
        try
        {
            var daten = new TaskCacheData
            {
                Tasks = tasks,
                CachedAt = DateTime.UtcNow,
                CachedDate = DateTime.Today.ToString("yyyy-MM-dd")
            };

            var json = JsonSerializer.Serialize(daten, AppJsonContext.Default.TaskCacheData);
            await File.WriteAllTextAsync(_aufgabenDatei, json).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Saved {tasks.Count} tasks to cache");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Save tasks error: {ex.Message}");
        }
    }

    /// <summary>
    /// Aufgaben laden. null, wenn nichts da ist oder der Stand von einem
    /// anderen Tag stammt (außer allowStale).
    /// </summary>
    public async Task<List<CleaningTask>?> LoadCachedTasksAsync(bool allowStale = false)
    {
        try
        {
            if (!File.Exists(_aufgabenDatei))
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] No cached tasks file");
                return null;
            }

            var json = await File.ReadAllTextAsync(_aufgabenDatei).ConfigureAwait(false);
            var daten = JsonSerializer.Deserialize(json, AppJsonContext.Default.TaskCacheData);

            if (daten?.Tasks == null)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] Cache data is null");
                return null;
            }

            var heute = DateTime.Today.ToString("yyyy-MM-dd");
            if (!allowStale && daten.CachedDate != heute)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Cache is stale: {daten.CachedDate} != {heute}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[OfflineData] Loaded {daten.Tasks.Count} tasks from cache (date: {daten.CachedDate})");
            return daten.Tasks;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Load tasks error: {ex.Message}");
            return null;
        }
    }

    /// <summary>True, wenn überhaupt Aufgaben abgelegt sind (auch veraltete).</summary>
    public bool HasCachedTasks() => File.Exists(_aufgabenDatei);

    #endregion

    #region Login

    /// <summary>Login-Zustand für den Offline-Start ablegen.</summary>
    public async Task SaveLoginStateAsync(string cleanerName, string? language, int? cleanerId)
    {
        try
        {
            var zustand = new LoginStateCache
            {
                CleanerName = cleanerName,
                Language = language ?? "de",
                CleanerId = cleanerId,
                LastLoginAt = DateTime.UtcNow,
                IsValid = true
            };

            var json = JsonSerializer.Serialize(zustand, AppJsonContext.Default.LoginStateCache);
            await File.WriteAllTextAsync(_loginDatei, json).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Saved login state for {cleanerName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Save login state error: {ex.Message}");
        }
    }

    /// <summary>Gespeicherten Login laden (null, wenn ungültig oder zu alt).</summary>
    public async Task<LoginStateCache?> LoadLoginStateAsync()
    {
        try
        {
            if (!File.Exists(_loginDatei))
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] No cached login state");
                return null;
            }

            var json = await File.ReadAllTextAsync(_loginDatei).ConfigureAwait(false);
            var zustand = JsonSerializer.Deserialize(json, AppJsonContext.Default.LoginStateCache);

            if (zustand == null || !zustand.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] Login state invalid");
                return null;
            }

            var alter = DateTime.UtcNow - zustand.LastLoginAt;
            if (alter > _loginHoechstalter)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Login state too old: {alter.TotalDays} days");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[OfflineData] Loaded login state for {zustand.CleanerName}");
            return zustand;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Load login state error: {ex.Message}");
            return null;
        }
    }

    /// <summary>Aufgaben- und Login-Datei löschen (Abmelden).</summary>
    public void ClearAll()
    {
        try
        {
            if (File.Exists(_aufgabenDatei))
                File.Delete(_aufgabenDatei);
            if (File.Exists(_loginDatei))
                File.Delete(_loginDatei);
            System.Diagnostics.Debug.WriteLine("[OfflineData] Cleared all cached data");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Clear all error: {ex.Message}");
        }
    }

    #endregion

    #region Bilder

    /// <summary>Bild ablegen (Pfad oder null).</summary>
    public Task<string?> CacheImageAsync(string url, byte[] imageBytes) => _bilder.SpeichereAsync(url, imageBytes);

    /// <summary>Pfad eines abgelegten Bildes (null, wenn nicht vorhanden).</summary>
    public string? GetCachedImagePath(string url) => _bilder.PfadWennVorhanden(url);

    #endregion
}
