using SQLite;
using System.Text.Json;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Queue item stored in SQLite for offline operations
/// </summary>
public class OfflineQueueItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Type of operation: chat, status, image, checklist, notes, problem
    /// </summary>
    public string OperationType { get; set; } = "";

    /// <summary>
    /// JSON payload of the operation
    /// </summary>
    public string Payload { get; set; } = "";

    /// <summary>
    /// Timestamp when the operation was queued
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last error message if any
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Priority (lower = higher priority): 1=chat, 2=status, 3=images
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Service for managing offline queue with SQLite storage
/// </summary>
public class OfflineQueueService : IDisposable
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private bool _isProcessing = false;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    private static OfflineQueueService? _instance;
    public static OfflineQueueService Instance => _instance ??= new OfflineQueueService();

    /// <summary>
    /// Event fired when queue changes (items added or processed)
    /// </summary>
    public event Action<int>? OnQueueCountChanged;

    /// <summary>
    /// Event fired when an item is successfully synced
    /// </summary>
    public event Action<OfflineQueueItem>? OnItemSynced;

    /// <summary>
    /// Event fired when an item sync fails
    /// </summary>
    public event Action<OfflineQueueItem, string>? OnItemSyncFailed;

    public OfflineQueueService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "offline_queue.db");
    }

    /// <summary>
    /// Initialize the database connection and create tables
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_database != null) return;

        _database = new SQLiteAsyncConnection(_dbPath);
        await _database.CreateTableAsync<OfflineQueueItem>();
        System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Initialized at {_dbPath}");
    }

    /// <summary>
    /// Enqueue a chat message for later sending
    /// </summary>
    public async Task EnqueueChatMessageAsync(string message)
    {
        var payload = JsonSerializer.Serialize(new { message });
        await EnqueueAsync("chat", payload, priority: 1);
    }

    /// <summary>
    /// Enqueue a task status change
    /// </summary>
    public async Task EnqueueStatusChangeAsync(int taskId, string action, string? notes = null)
    {
        var payload = JsonSerializer.Serialize(new { taskId, action, notes, timestamp = DateTime.UtcNow });
        await EnqueueAsync("status", payload, priority: 2);
    }

    /// <summary>
    /// Enqueue an image upload
    /// </summary>
    public async Task EnqueueImageUploadAsync(int taskId, byte[] imageBytes, string? notes = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            taskId,
            imageBase64 = Convert.ToBase64String(imageBytes),
            notes,
            timestamp = DateTime.UtcNow
        });
        await EnqueueAsync("image", payload, priority: 3);
    }

    /// <summary>
    /// Enqueue a checklist item toggle
    /// </summary>
    public async Task EnqueueChecklistToggleAsync(int taskId, int itemId, bool completed)
    {
        var payload = JsonSerializer.Serialize(new { taskId, itemId, completed, timestamp = DateTime.UtcNow });
        await EnqueueAsync("checklist", payload, priority: 2);
    }

    /// <summary>
    /// Enqueue notes update
    /// </summary>
    public async Task EnqueueNotesUpdateAsync(int taskId, string notes)
    {
        var payload = JsonSerializer.Serialize(new { taskId, notes, timestamp = DateTime.UtcNow });
        await EnqueueAsync("notes", payload, priority: 2);
    }

    /// <summary>
    /// Enqueue a problem report
    /// </summary>
    public async Task EnqueueProblemReportAsync(int taskId, string name, string? description, List<byte[]>? photos)
    {
        var photoBase64List = photos?.Select(p => Convert.ToBase64String(p)).ToList();
        var payload = JsonSerializer.Serialize(new
        {
            taskId,
            name,
            description,
            photos = photoBase64List,
            timestamp = DateTime.UtcNow
        });
        await EnqueueAsync("problem", payload, priority: 2);
    }

    /// <summary>
    /// Generic enqueue method
    /// </summary>
    private async Task EnqueueAsync(string operationType, string payload, int priority)
    {
        await InitializeAsync();

        var item = new OfflineQueueItem
        {
            OperationType = operationType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Priority = priority
        };

        await _database!.InsertAsync(item);
        System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Enqueued {operationType} (ID: {item.Id})");

        var count = await GetQueueCountAsync();
        OnQueueCountChanged?.Invoke(count);
    }

    /// <summary>
    /// Get the number of pending items in the queue
    /// </summary>
    public async Task<int> GetQueueCountAsync()
    {
        await InitializeAsync();
        return await _database!.Table<OfflineQueueItem>().CountAsync();
    }

    /// <summary>
    /// Get all pending items ordered by priority and creation time
    /// </summary>
    public async Task<List<OfflineQueueItem>> GetPendingItemsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<OfflineQueueItem>()
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Process the queue when back online
    /// </summary>
    public async Task ProcessQueueAsync()
    {
        if (!await _processingLock.WaitAsync(0))
        {
            System.Diagnostics.Debug.WriteLine("[OfflineQueue] Already processing");
            return;
        }

        try
        {
            _isProcessing = true;
            await InitializeAsync();

            var items = await GetPendingItemsAsync();
            System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Processing {items.Count} items");

            foreach (var item in items)
            {
                try
                {
                    var success = await ProcessItemAsync(item);
                    if (success)
                    {
                        await _database!.DeleteAsync(item);
                        OnItemSynced?.Invoke(item);
                        System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Synced {item.OperationType} (ID: {item.Id})");
                    }
                    else
                    {
                        item.RetryCount++;
                        await _database!.UpdateAsync(item);
                    }
                }
                catch (Exception ex)
                {
                    item.RetryCount++;
                    item.LastError = ex.Message;
                    await _database!.UpdateAsync(item);
                    OnItemSyncFailed?.Invoke(item, ex.Message);
                    System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Failed {item.OperationType}: {ex.Message}");
                }

                // Small delay between items
                await Task.Delay(100);
            }

            var count = await GetQueueCountAsync();
            OnQueueCountChanged?.Invoke(count);
        }
        finally
        {
            _isProcessing = false;
            _processingLock.Release();
        }
    }

    /// <summary>
    /// Process a single queue item
    /// </summary>
    private async Task<bool> ProcessItemAsync(OfflineQueueItem item)
    {
        var apiService = ApiService.Instance;

        switch (item.OperationType)
        {
            case "chat":
                return await ProcessChatAsync(item, apiService);
            case "status":
                return await ProcessStatusAsync(item, apiService);
            case "image":
                return await ProcessImageAsync(item, apiService);
            case "checklist":
                return await ProcessChecklistAsync(item, apiService);
            case "notes":
                return await ProcessNotesAsync(item, apiService);
            case "problem":
                return await ProcessProblemAsync(item, apiService);
            default:
                System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Unknown operation type: {item.OperationType}");
                return true; // Remove unknown items
        }
    }

    private async Task<bool> ProcessChatAsync(OfflineQueueItem item, ApiService api)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(item.Payload);
        var message = data.GetProperty("message").GetString();
        if (string.IsNullOrEmpty(message)) return true;

        var response = await api.SendChatMessageAsync(message);
        return response.Success;
    }

    private async Task<bool> ProcessStatusAsync(OfflineQueueItem item, ApiService api)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(item.Payload);
        var taskId = data.GetProperty("taskId").GetInt32();
        var action = data.GetProperty("action").GetString();

        switch (action)
        {
            case "start":
                var startResponse = await api.StartTaskAsync(taskId);
                return startResponse.Success;
            case "stop":
                var stopResponse = await api.StopTaskAsync(taskId);
                return stopResponse.Success;
            default:
                return true;
        }
    }

    private async Task<bool> ProcessImageAsync(OfflineQueueItem item, ApiService api)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(item.Payload);
        var taskId = data.GetProperty("taskId").GetInt32();
        var imageBase64 = data.GetProperty("imageBase64").GetString();
        var notes = data.TryGetProperty("notes", out var notesEl) ? notesEl.GetString() : null;

        if (string.IsNullOrEmpty(imageBase64)) return true;

        var imageBytes = Convert.FromBase64String(imageBase64);
        var response = await api.UploadBildStatusAsync(taskId, imageBytes, "offline_image.jpg", notes);
        return response.Success;
    }

    private async Task<bool> ProcessChecklistAsync(OfflineQueueItem item, ApiService api)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(item.Payload);
        var taskId = data.GetProperty("taskId").GetInt32();
        var itemId = data.GetProperty("itemId").GetInt32();
        var completed = data.GetProperty("completed").GetBoolean();

        var response = await api.ToggleChecklistItemAsync(taskId, itemId, completed);
        return response.Success;
    }

    private async Task<bool> ProcessNotesAsync(OfflineQueueItem item, ApiService api)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(item.Payload);
        var taskId = data.GetProperty("taskId").GetInt32();
        var notes = data.GetProperty("notes").GetString() ?? "";

        var response = await api.SaveTaskNotesAsync(taskId, notes);
        return response.Success;
    }

    private async Task<bool> ProcessProblemAsync(OfflineQueueItem item, ApiService api)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(item.Payload);
        var taskId = data.GetProperty("taskId").GetInt32();
        var name = data.GetProperty("name").GetString() ?? "";
        var description = data.TryGetProperty("description", out var descEl) ? descEl.GetString() : null;

        List<(string, byte[])>? photos = null;
        if (data.TryGetProperty("photos", out var photosEl) && photosEl.ValueKind == JsonValueKind.Array)
        {
            photos = photosEl.EnumerateArray()
                .Select((p, i) => ($"photo_{i}.jpg", Convert.FromBase64String(p.GetString() ?? "")))
                .ToList();
        }

        var response = await api.ReportProblemAsync(taskId, name, description, photos);
        return response.Success;
    }

    /// <summary>
    /// Clear all items from the queue
    /// </summary>
    public async Task ClearQueueAsync()
    {
        await InitializeAsync();
        await _database!.DeleteAllAsync<OfflineQueueItem>();
        OnQueueCountChanged?.Invoke(0);
    }

    public void Dispose()
    {
        _database?.CloseAsync();
        _processingLock.Dispose();
    }
}
