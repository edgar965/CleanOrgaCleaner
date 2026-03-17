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

            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions
            {
                WriteIndented = false
            });

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
            var cacheData = JsonSerializer.Deserialize<TaskCacheData>(json);

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

            var json = JsonSerializer.Serialize(state);
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
            var state = JsonSerializer.Deserialize<LoginStateCache>(json);

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
    /// Check if we have valid cached login state
    /// </summary>
    public bool HasValidLoginState()
    {
        try
        {
            if (!File.Exists(_loginStateFile))
                return false;

            var json = File.ReadAllText(_loginStateFile);
            var state = JsonSerializer.Deserialize<LoginStateCache>(json);

            if (state == null || !state.IsValid)
                return false;

            var daysSinceLogin = (DateTime.UtcNow - state.LastLoginAt).TotalDays;
            return daysSinceLogin <= 7;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clear login state (on logout)
    /// </summary>
    public void ClearLoginState()
    {
        try
        {
            if (File.Exists(_loginStateFile))
                File.Delete(_loginStateFile);
            System.Diagnostics.Debug.WriteLine("[OfflineData] Cleared login state");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Clear login state error: {ex.Message}");
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
        // Create a safe filename from the URL
        var hash = url.GetHashCode().ToString("X8");
        var extension = Path.GetExtension(url);
        if (string.IsNullOrEmpty(extension) || extension.Length > 5)
            extension = ".jpg";
        return Path.Combine(_imageCachePath, $"img_{hash}{extension}");
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
    /// Get cached image bytes if available
    /// </summary>
    public async Task<byte[]?> GetCachedImageAsync(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var cachePath = GetImageCachePath(url);
            if (!File.Exists(cachePath))
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Image not cached: {url}");
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(cachePath).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Loaded cached image: {url} ({bytes.Length} bytes)");
            return bytes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Get cached image error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if an image is cached
    /// </summary>
    public bool IsImageCached(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;
        var cachePath = GetImageCachePath(url);
        return File.Exists(cachePath);
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

    /// <summary>
    /// Clear old cached images (older than specified days)
    /// </summary>
    public void CleanupOldImages(int maxAgeDays = 7)
    {
        try
        {
            if (!Directory.Exists(_imageCachePath))
                return;

            var cutoff = DateTime.Now.AddDays(-maxAgeDays);
            var files = Directory.GetFiles(_imageCachePath, "img_*");
            int deleted = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }

            if (deleted > 0)
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Cleaned up {deleted} old cached images");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Cleanup images error: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear all cached images
    /// </summary>
    public void ClearImageCache()
    {
        try
        {
            if (Directory.Exists(_imageCachePath))
            {
                Directory.Delete(_imageCachePath, true);
                System.Diagnostics.Debug.WriteLine("[OfflineData] Cleared image cache");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Clear image cache error: {ex.Message}");
        }
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
