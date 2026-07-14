using System.Text.Json;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Json;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Service for caching data locally for offline use
/// Stores tasks, login state, and other critical data
/// </summary>
public class OfflineDataService
{
    private static OfflineDataService? _instance;
    public static OfflineDataService Instance => _instance ??= new OfflineDataService();

    private readonly string _dataPath;
    private readonly string _tasksFile;
    private readonly string _loginStateFile;

    public OfflineDataService()
    {
        _dataPath = FileSystem.AppDataDirectory;
        _tasksFile = Path.Combine(_dataPath, "cached_tasks.json");
        _loginStateFile = Path.Combine(_dataPath, "login_state.json");
        _imageCachePath = Path.Combine(_dataPath, "image_cache");
    }

    #region Tasks Cache

    /// <summary>
    /// Save tasks to local cache
    /// </summary>
    public async Task SaveTasksAsync(List<CleaningTask> tasks)
    {
        try
        {
            var cacheData = new TaskCacheData
            {
                Tasks = tasks,
                CachedAt = DateTime.UtcNow,
                CachedDate = DateTime.Today.ToString("yyyy-MM-dd")
            };

            var json = JsonSerializer.Serialize(cacheData, AppJsonContext.Default.TaskCacheData);

            await File.WriteAllTextAsync(_tasksFile, json).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Saved {tasks.Count} tasks to cache");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Save tasks error: {ex.Message}");
        }
    }

    /// <summary>
    /// Load tasks from local cache
    /// Returns null if cache is stale (different day) or doesn't exist
    /// </summary>
    public async Task<List<CleaningTask>?> LoadCachedTasksAsync(bool allowStale = false)
    {
        try
        {
            if (!File.Exists(_tasksFile))
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] No cached tasks file");
                return null;
            }

            var json = await File.ReadAllTextAsync(_tasksFile).ConfigureAwait(false);
            var cacheData = JsonSerializer.Deserialize(json, AppJsonContext.Default.TaskCacheData);

            if (cacheData == null || cacheData.Tasks == null)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] Cache data is null");
                return null;
            }

            // Check if cache is from today (unless allowStale is true)
            var todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            if (!allowStale && cacheData.CachedDate != todayStr)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Cache is stale: {cacheData.CachedDate} != {todayStr}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[OfflineData] Loaded {cacheData.Tasks.Count} tasks from cache (date: {cacheData.CachedDate})");
            return cacheData.Tasks;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Load tasks error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if we have any cached tasks (even stale ones)
    /// </summary>
    public bool HasCachedTasks()
    {
        return File.Exists(_tasksFile);
    }

    #endregion

    #region Login State Cache

    /// <summary>
    /// Save login state for offline use
    /// </summary>
    public async Task SaveLoginStateAsync(string cleanerName, string? language, int? cleanerId)
    {
        try
        {
            var state = new LoginStateCache
            {
                CleanerName = cleanerName,
                Language = language ?? "de",
                CleanerId = cleanerId,
                LastLoginAt = DateTime.UtcNow,
                IsValid = true
            };

            var json = JsonSerializer.Serialize(state, AppJsonContext.Default.LoginStateCache);
            await File.WriteAllTextAsync(_loginStateFile, json).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Saved login state for {cleanerName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Save login state error: {ex.Message}");
        }
    }

    /// <summary>
    /// Load cached login state
    /// </summary>
    public async Task<LoginStateCache?> LoadLoginStateAsync()
    {
        try
        {
            if (!File.Exists(_loginStateFile))
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] No cached login state");
                return null;
            }

            var json = await File.ReadAllTextAsync(_loginStateFile).ConfigureAwait(false);
            var state = JsonSerializer.Deserialize(json, AppJsonContext.Default.LoginStateCache);

            if (state == null || !state.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("[OfflineData] Login state invalid");
                return null;
            }

            // Check if login state is not too old (7 days max)
            var daysSinceLogin = (DateTime.UtcNow - state.LastLoginAt).TotalDays;
            if (daysSinceLogin > 7)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Login state too old: {daysSinceLogin} days");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[OfflineData] Loaded login state for {state.CleanerName}");
            return state;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Load login state error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clear all cached data
    /// </summary>
    public void ClearAll()
    {
        try
        {
            if (File.Exists(_tasksFile))
                File.Delete(_tasksFile);
            if (File.Exists(_loginStateFile))
                File.Delete(_loginStateFile);
            System.Diagnostics.Debug.WriteLine("[OfflineData] Cleared all cached data");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Clear all error: {ex.Message}");
        }
    }

    #endregion

    #region Image Cache

    private readonly string _imageCachePath;

    /// <summary>
    /// Get the path for a cached image
    /// </summary>
    private string GetImageCachePath(string url)
    {
        // Stabiler Hash statt url.GetHashCode(): String-Hashcodes sind in
        // .NET pro Prozessstart randomisiert - der Cache wurde dadurch nach
        // jedem App-Neustart nie mehr getroffen und wuchs unbegrenzt
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url));
        var hash = Convert.ToHexString(bytes, 0, 8);
        var extension = Path.GetExtension(url);
        if (string.IsNullOrEmpty(extension) || extension.Length > 5)
            extension = ".jpg";
        return Path.Combine(_imageCachePath, $"img_{hash}{extension}");
    }

    private static bool _cacheAufgeraeumt;

    /// <summary>
    /// Alte Cache-Dateien entfernen (einmal pro App-Lauf): räumt auch die
    /// verwaisten Dateien der früheren GetHashCode-Namensgebung ab, die nie
    /// wieder getroffen werden.
    /// </summary>
    private void RaeumeBildCacheAuf()
    {
        if (_cacheAufgeraeumt) return;
        _cacheAufgeraeumt = true;
        _ = Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(_imageCachePath)) return;
                var grenze = DateTime.UtcNow.AddDays(-14);
                foreach (var datei in Directory.GetFiles(_imageCachePath, "img_*"))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(datei) < grenze)
                            File.Delete(datei);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Cache cleanup error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Cache an image from URL
    /// </summary>
    public async Task<string?> CacheImageAsync(string url, byte[] imageBytes)
    {
        try
        {
            if (string.IsNullOrEmpty(url) || imageBytes == null || imageBytes.Length == 0)
                return null;

            RaeumeBildCacheAuf();

            // Ensure cache directory exists
            if (!Directory.Exists(_imageCachePath))
                Directory.CreateDirectory(_imageCachePath);

            var cachePath = GetImageCachePath(url);
            await File.WriteAllBytesAsync(cachePath, imageBytes).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Cached image: {url} -> {cachePath}");
            return cachePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Cache image error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get the local file path for a cached image (for direct use with ImageSource.FromFile)
    /// </summary>
    public string? GetCachedImagePath(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        var cachePath = GetImageCachePath(url);
        return File.Exists(cachePath) ? cachePath : null;
    }

    #endregion
}

/// <summary>
/// Cache container for tasks
/// </summary>
public class TaskCacheData
{
    public List<CleaningTask> Tasks { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public string CachedDate { get; set; } = "";
}

/// <summary>
/// Cache container for login state
/// </summary>
public class LoginStateCache
{
    public string CleanerName { get; set; } = "";
    public string Language { get; set; } = "de";
    public int? CleanerId { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsValid { get; set; }
}
