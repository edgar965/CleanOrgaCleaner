using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Main API service for all server communication
/// Singleton to maintain session cookies across pages
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _handler;
    public const string BaseUrl = "https://cleanorga.com";

    private static ApiService? _instance;
    private static readonly object _lock = new();

    // JSON Serializer Options - wichtig für korrekte Deserialisierung
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Store cleaner info after login
    public string? CleanerName { get; private set; }
    public string? CleanerLanguage { get; private set; }
    public int? CleanerId { get; private set; }

    // Offline support
    public bool IsOnline => WebSocketService.Instance.IsOnline;

    // Task cache - stores tasks loaded from today-data
    private Dictionary<int, CleaningTask> _taskCache = new();

    public static ApiService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ApiService();
                }
            }
            return _instance;
        }
    }

    public ApiService()
    {
        _handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        };
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store");
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
    }

    #region Authentication

    public async Task<LoginResult> LoginAsync(int propertyId, string username, string password)
    {
        try
        {
            var loginData = new
            {
                property_id = propertyId,
                username = username,
                password = password
            };

            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"Login: {BaseUrl}/mobile/api/login/");
            var response = await _httpClient.PostAsync("/mobile/api/login/", content);

            var responseJson = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Login response: {response.StatusCode} - {responseJson}");

            var result = JsonSerializer.Deserialize<LoginResponse>(responseJson, _jsonOptions);

            if (result?.Success == true)
            {
                CleanerName = result.Cleaner?.Name;
                CleanerLanguage = result.Cleaner?.Language ?? "de";
                CleanerId = result.Cleaner?.Id;

                return new LoginResult
                {
                    Success = true,
                    CleanerName = result.Cleaner?.Name,
                    CleanerLanguage = result.Cleaner?.Language
                };
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = result?.Error ?? "Login fehlgeschlagen"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return new LoginResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public void Logout()
    {
        CleanerName = null;
        CleanerLanguage = null;
        CleanerId = null;
        _handler.CookieContainer = new System.Net.CookieContainer();
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Try to call server logout endpoint (optional)
            await _httpClient.PostAsync("/mobile/api/logout/", null);
        }
        catch
        {
            // Ignore - server logout is optional
        }

        // Clear local state
        Logout();
    }

    /// <summary>
    /// Get session cookies for WebSocket connections
    /// </summary>
    public string GetCookieHeader()
    {
        var cookies = _handler.CookieContainer.GetCookies(new Uri(BaseUrl));
        if (cookies.Count == 0) return "";

        var cookieStrings = new List<string>();
        foreach (System.Net.Cookie cookie in cookies)
        {
            cookieStrings.Add($"{cookie.Name}={cookie.Value}");
        }
        return string.Join("; ", cookieStrings);
    }

    /// <summary>
    /// Download an image with authentication cookies
    /// </summary>
    public async Task<ImageSource?> GetImageAsync(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return null;

            // Make URL absolute if relative
            if (!url.StartsWith("http"))
                url = $"{BaseUrl}{url}";

            System.Diagnostics.Debug.WriteLine($"GetImageAsync: {url}");
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.Diagnostics.Debug.WriteLine($"GetImageAsync: Got {bytes.Length} bytes");
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GetImageAsync: Failed {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetImageAsync error: {ex.Message}");
        }
        return null;
    }

    #endregion

    #region Today / Tasks

    public async Task<TodayDataResponse> GetTodayDataAsync()
    {
        try
        {
            // Cache-Buster hinzufügen um sicherzustellen dass wir frische Daten bekommen
            var cacheBuster = DateTime.Now.Ticks;
            var response = await _httpClient.GetAsync($"/mobile/api/today-data/?_={cacheBuster}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"TodayData JSON length: {json.Length}");

                // Check if bilder is in the response
                if (json.Contains("\"bilder\""))
                {
                    System.Diagnostics.Debug.WriteLine("TodayData: 'bilder' field found in JSON");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("TodayData: WARNING - 'bilder' field NOT in JSON!");
                }

                var data = JsonSerializer.Deserialize<TodayDataResponse>(json, _jsonOptions) ?? new TodayDataResponse();

                // Cache tasks and log bilder count
                _taskCache.Clear();
                foreach (var task in data.Tasks)
                {
                    _taskCache[task.Id] = task;
                    System.Diagnostics.Debug.WriteLine($"Task {task.Id}: Bilder={task.Bilder?.Count ?? 0}");
                    if (task.Bilder != null)
                    {
                        foreach (var bild in task.Bilder)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Bild {bild.Id}: thumb='{bild.ThumbnailUrl}', full='{bild.FullUrl}'");
                        }
                    }
                }

                return data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTodayData error: {ex.Message}");
        }
        return new TodayDataResponse();
    }

    public async Task<CleaningTask?> GetTaskDetailAsync(int taskId, bool forceRefresh = true)
    {
        // Always reload to get fresh data with images
        // forceRefresh=true by default to ensure images are loaded
        if (!forceRefresh && _taskCache.TryGetValue(taskId, out var cachedTask))
        {
            System.Diagnostics.Debug.WriteLine($"TaskDetail from cache: {taskId}, Bilder: {cachedTask.Bilder?.Count ?? 0}");
            return cachedTask;
        }

        // Reload today data to get fresh tasks with images
        try
        {
            System.Diagnostics.Debug.WriteLine($"TaskDetail: reloading today data for task {taskId}");
            _taskCache.Clear(); // Clear cache to force fresh data
            var todayData = await GetTodayDataAsync();
            if (_taskCache.TryGetValue(taskId, out var task))
            {
                System.Diagnostics.Debug.WriteLine($"TaskDetail loaded: {taskId}, Bilder: {task.Bilder?.Count ?? 0}");
                return task;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskDetail error: {ex.Message}");
        }
        return null;
    }

    public async Task<WorkTimeResponse> StartWorkAsync()
    {
        try
        {
            var data = new { date = DateTime.Now.ToString("yyyy-MM-dd") };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/start-time/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"StartWork: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WorkTimeResponse>(responseText)
                    ?? new WorkTimeResponse { Success = true };
            }
            return new WorkTimeResponse { Success = false, Error = responseText };
        }
        catch (Exception ex)
        {
            return new WorkTimeResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<WorkTimeResponse> EndWorkAsync()
    {
        try
        {
            var data = new { date = DateTime.Now.ToString("yyyy-MM-dd") };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/end-time/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"EndWork: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WorkTimeResponse>(responseText)
                    ?? new WorkTimeResponse { Success = true };
            }
            return new WorkTimeResponse { Success = false, Error = responseText };
        }
        catch (Exception ex)
        {
            return new WorkTimeResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<TaskStateResponse> UpdateTaskStateAsync(int taskId, string state)
    {
        try
        {
            var data = new { state_completed = state };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/task/{taskId}/state/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UpdateTaskState: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<TaskStateResponse>(responseText)
                ?? new TaskStateResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new TaskStateResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<TaskStateResponse> StartTaskAsync(int taskId)
    {
        return await UpdateTaskStateAsync(taskId, "started");
    }

    public async Task<TaskStateResponse> StopTaskAsync(int taskId)
    {
        return await UpdateTaskStateAsync(taskId, "completed");
    }

    public async Task<ChecklistToggleResponse> ToggleChecklistItemAsync(int taskId, int itemIndex)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/{taskId}/checklist/{itemIndex}/toggle/", null);
            var responseText = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ChecklistToggleResponse>(responseText)
                ?? new ChecklistToggleResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToggleChecklist error: {ex.Message}");
            return new ChecklistToggleResponse { Success = false };
        }
    }

    /// <summary>
    /// Toggle checklist item with explicit completed state (for offline queue)
    /// </summary>
    public async Task<ChecklistToggleResponse> ToggleChecklistItemAsync(int taskId, int itemIndex, bool completed)
    {
        // The API just toggles, but we can call it knowing the expected state
        return await ToggleChecklistItemAsync(taskId, itemIndex);
    }

    /// <summary>
    /// Save task notes - alias for SaveTaskNoteAsync
    /// </summary>
    public async Task<ApiResponse> SaveTaskNotesAsync(int taskId, string notes)
    {
        return await SaveTaskNoteAsync(taskId, notes);
    }

    /// <summary>
    /// Upload BildStatus with byte array (for offline queue)
    /// </summary>
    public async Task<ApiResponse> UploadBildStatusAsync(int taskId, byte[] imageBytes, string fileName, string? notiz)
    {
        return await UploadBildStatusBytesAsync(taskId, imageBytes, fileName, notiz ?? "");
    }

    /// <summary>
    /// Report problem with byte array photos (for offline queue)
    /// </summary>
    public async Task<ProblemResponse> ReportProblemAsync(int taskId, string name, string? description, List<(string, byte[])>? photos)
    {
        return await ReportProblemWithBytesAsync(taskId, name, description, photos);
    }

    public async Task<ApiResponse> SaveTaskNoteAsync(int taskId, string note)
    {
        try
        {
            var data = new { anmerkung_mitarbeiter = note };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/notiz/", content);
            var responseText = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ProblemResponse> ReportProblemAsync(int taskId, string name, string? beschreibung, List<string>? photoPaths)
    {
        try
        {
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(name), "name");
            if (!string.IsNullOrEmpty(beschreibung))
                formData.Add(new StringContent(beschreibung), "beschreibung");

            if (photoPaths != null)
            {
                foreach (var path in photoPaths)
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    var fileContent = new ByteArrayContent(bytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    formData.Add(fileContent, "fotos", Path.GetFileName(path));
                }
            }

            var response = await _httpClient.PostAsync($"/api/task/{taskId}/problem/create/", formData);
            var responseText = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ProblemResponse>(responseText)
                ?? new ProblemResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ProblemResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ProblemResponse> ReportProblemWithBytesAsync(int taskId, string name, string? beschreibung, List<(string FileName, byte[] Bytes)>? photos)
    {
        try
        {
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(name), "name");
            if (!string.IsNullOrEmpty(beschreibung))
                formData.Add(new StringContent(beschreibung), "beschreibung");

            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    var fileContent = new ByteArrayContent(photo.Bytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    formData.Add(fileContent, "fotos", photo.FileName);
                }
            }

            var response = await _httpClient.PostAsync($"/api/task/{taskId}/problem/create/", formData);
            var responseText = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ProblemResponse>(responseText)
                ?? new ProblemResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ProblemResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> DeleteProblemAsync(int problemId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/problem/{problemId}/delete/", null);
            var responseText = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region BildStatus

    public async Task<ApiResponse> UpdateTaskNotesAsync(int taskId, string notes)
    {
        return await SaveTaskNoteAsync(taskId, notes);
    }

    public async Task<ApiResponse> UploadBildStatusAsync(int taskId, string imagePath, string notiz)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UploadBildStatus: Start - TaskId={taskId}, Path={imagePath}");

            if (!File.Exists(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"UploadBildStatus: Datei existiert nicht: {imagePath}");
                return new ApiResponse { Success = false, Error = "Bilddatei nicht gefunden" };
            }

            var formData = new MultipartFormDataContent();
            var bytes = await File.ReadAllBytesAsync(imagePath);
            System.Diagnostics.Debug.WriteLine($"UploadBildStatus: Datei gelesen, {bytes.Length} bytes");

            var fileContent = new ByteArrayContent(bytes);

            // Content-Type basierend auf Dateiendung
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            formData.Add(fileContent, "image", Path.GetFileName(imagePath));
            if (!string.IsNullOrEmpty(notiz))
                formData.Add(new StringContent(notiz), "notiz");

            System.Diagnostics.Debug.WriteLine($"UploadBildStatus: Sende POST zu /api/task/{taskId}/bilder/upload/");
            var response = await _httpClient.PostAsync($"/api/task/{taskId}/bilder/upload/", formData);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UploadBildStatus: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UploadBildStatus: EXCEPTION - {ex.Message}\n{ex.StackTrace}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> UploadBildStatusBytesAsync(int taskId, byte[] imageBytes, string fileName, string notiz)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UploadBildStatusBytes: Start - TaskId={taskId}, FileName={fileName}, Size={imageBytes.Length}");

            var formData = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(imageBytes);

            // Content-Type basierend auf Dateiendung
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            formData.Add(fileContent, "image", fileName);
            if (!string.IsNullOrEmpty(notiz))
                formData.Add(new StringContent(notiz), "notiz");

            System.Diagnostics.Debug.WriteLine($"UploadBildStatusBytes: POST /api/task/{taskId}/bilder/upload/");
            var response = await _httpClient.PostAsync($"/api/task/{taskId}/bilder/upload/", formData);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UploadBildStatusBytes: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UploadBildStatusBytes: EXCEPTION - {ex.Message}\n{ex.StackTrace}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> DeleteBildStatusAsync(int bildId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DeleteBildStatus: Lösche Bild {bildId}");
            var response = await _httpClient.PostAsync($"/api/bildstatus/{bildId}/delete/", new StringContent("{}"));
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"DeleteBildStatus: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteBildStatus: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> UpdateBildStatusAsync(int bildId, string notiz)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UpdateBildStatus: Update Bild {bildId} mit Notiz");
            var data = new { notiz = notiz };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/bildstatus/{bildId}/update/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UpdateBildStatus: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateBildStatus: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Task Logs

    public async Task<List<LogEntry>> GetTaskLogsAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskLogs: Loading logs for task {taskId}");
            var response = await _httpClient.GetAsync($"/api/task/{taskId}/logs/");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"GetTaskLogs: {json}");
                var result = JsonSerializer.Deserialize<LogsResponse>(json, _jsonOptions);
                return result?.Logs ?? new List<LogEntry>();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GetTaskLogs: HTTP {(int)response.StatusCode}");
                return new List<LogEntry>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskLogs: EXCEPTION - {ex.Message}");
            return new List<LogEntry>();
        }
    }

    #endregion

    #region Task Delete

    public async Task<ApiResponse> DeleteTaskAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask: Deleting task {taskId}");
            var response = await _httpClient.PostAsync($"/api/cleaning-task/{taskId}/delete/", new StringContent("{}"));
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"DeleteTask: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Chat

    public async Task<List<ChatMessage>> GetChatMessagesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/chat/messages/");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"ChatMessages: {json}");
                var result = JsonSerializer.Deserialize<ChatMessagesResponse>(json);
                return result?.Messages ?? new List<ChatMessage>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetChatMessages error: {ex.Message}");
        }
        return new List<ChatMessage>();
    }

    public async Task<ChatSendResponse> SendChatMessageAsync(string text, string receiverId = "admin")
    {
        try
        {
            var data = new { text = text, receiver_id = receiverId };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/chat/send/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"SendChat: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<ChatSendResponse>(responseText)
                ?? new ChatSendResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ChatSendResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<TranslationPreviewResponse> PreviewTranslationAsync(string text, string? receiverId = null)
    {
        try
        {
            var data = new { text = text, receiver_id = receiverId };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/chat/preview-translation/", content);
            var responseText = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TranslationPreviewResponse>(responseText)
                ?? new TranslationPreviewResponse { Success = false };
        }
        catch (Exception ex)
        {
            return new TranslationPreviewResponse { Success = false, Message = ex.Message };
        }
    }

    #endregion

    #region Settings

    public async Task<ApiResponse> SetLanguageAsync(string language)
    {
        try
        {
            var data = new { language = language };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/language/", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                CleanerLanguage = language;
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Cleaners List
    
    public async Task<List<CleanerInfo>> GetAllCleanersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/cleaners/");
            var responseText = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CleanersListResponse>(responseText);
                return result?.Cleaners ?? new List<CleanerInfo>();
            }
            return new List<CleanerInfo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAllCleaners error: {ex.Message}");
            return new List<CleanerInfo>();
        }
    }
    
    #endregion

    #region My Tasks

    public async Task<MyTasksPageDataResponse> GetMyTasksDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/my-tasks-data/");
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"GetMyTasksData: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<MyTasksPageDataResponse>(responseText, _jsonOptions)
                    ?? new MyTasksPageDataResponse { Success = false, Error = "Parse error" };
            }
            return new MyTasksPageDataResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetMyTasksData error: {ex.Message}");
            return new MyTasksPageDataResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<MyTaskDetailResponse> GetMyTaskAsync(int taskId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/mobile/api/task/{taskId}/");
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<MyTaskDetailResponse>(responseText, _jsonOptions)
                    ?? new MyTaskDetailResponse { Success = false, Error = "Parse error" };
            }
            return new MyTaskDetailResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new MyTaskDetailResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> CreateMyTaskAsync(string name, string plannedDate, int? apartmentId, int? aufgabenartId, string? hinweis, string status, TaskAssignments? assignments)
    {
        try
        {
            var data = new
            {
                name = name,
                planned_date = plannedDate,
                apartment_id = apartmentId,
                aufgabenart_id = aufgabenartId,
                wichtiger_hinweis = hinweis ?? "",
                status = status,
                assignments = assignments ?? new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() }
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/task/create/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"CreateMyTask: {response.StatusCode} - {responseText}");

            // Check if response is JSON
            if (string.IsNullOrWhiteSpace(responseText) || !responseText.TrimStart().StartsWith("{"))
            {
                return new ApiResponse { Success = false, Error = response.IsSuccessStatusCode ? "Ungueltiges Antwortformat" : $"Server-Fehler: {response.StatusCode}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> UpdateMyTaskAsync(int taskId, string name, string plannedDate, int? apartmentId, int? aufgabenartId, string? hinweis, string status, TaskAssignments? assignments)
    {
        try
        {
            var data = new
            {
                name = name,
                planned_date = plannedDate,
                apartment_id = apartmentId,
                aufgabenart_id = aufgabenartId,
                wichtiger_hinweis = hinweis ?? "",
                status = status,
                assignments = assignments ?? new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() }
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/update/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UpdateMyTask: {response.StatusCode} - {responseText}");

            // Check if response is JSON
            if (string.IsNullOrWhiteSpace(responseText) || !responseText.TrimStart().StartsWith("{"))
            {
                return new ApiResponse { Success = false, Error = response.IsSuccessStatusCode ? "Ungueltiges Antwortformat" : $"Server-Fehler: {response.StatusCode}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> DeleteMyTaskAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DeleteMyTask: Deleting task {taskId}");
            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/delete/", new StringContent("{}"));
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"DeleteMyTask: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteMyTask: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<List<Views.TaskImageInfo>> GetTaskImagesAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskImages: Loading images for task {taskId}");
            var response = await _httpClient.GetAsync($"/api/task/{taskId}/images/");
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"GetTaskImages: /api/task/{taskId}/images/ - {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TaskImagesResponse>(responseText, _jsonOptions);
                return result?.Images?.Select(i => new Views.TaskImageInfo
                {
                    Id = i.Id,
                    Url = i.Url.StartsWith("http") ? i.Url : $"{BaseUrl}{i.Url}",
                    ThumbnailUrl = i.ThumbnailUrl?.StartsWith("http") == true ? i.ThumbnailUrl : (i.ThumbnailUrl != null ? $"{BaseUrl}{i.ThumbnailUrl}" : null),
                    Note = i.Note,
                    CreatedAt = i.CreatedAt
                }).ToList() ?? new List<Views.TaskImageInfo>();
            }
            return new List<Views.TaskImageInfo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskImages: EXCEPTION - {ex.Message}");
            return new List<Views.TaskImageInfo>();
        }
    }

    public async Task<ApiResponse> UploadTaskImageAsync(int taskId, Stream imageStream, string fileName, string? note)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UploadTaskImage: Start - TaskId={taskId}, FileName={fileName}");

            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            var formData = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            formData.Add(fileContent, "image", fileName);
            if (!string.IsNullOrEmpty(note))
                formData.Add(new StringContent(note), "notiz");

            System.Diagnostics.Debug.WriteLine($"UploadTaskImage: POST /api/task/{taskId}/bilder/upload/");
            var response = await _httpClient.PostAsync($"/api/task/{taskId}/bilder/upload/", formData);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UploadTaskImage: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UploadTaskImage: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> UpdateTaskImageAsync(int imageId, string? note)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UpdateTaskImage: Update image {imageId}");
            var data = new { notiz = note ?? "" };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/bildstatus/{imageId}/update/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"UpdateTaskImage: /api/bildstatus/{imageId}/update/ - {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateTaskImage: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> DeleteTaskImageAsync(int imageId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTaskImage: Delete image {imageId}");
            var response = await _httpClient.PostAsync($"/api/bildstatus/{imageId}/delete/", new StringContent("{}"));
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"DeleteTaskImage: /api/bildstatus/{imageId}/delete/ - {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTaskImage: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

}