using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    // Store cleaner info after login
    public string? CleanerName { get; private set; }
    public string? CleanerLanguage { get; private set; }
    public int? CleanerId { get; private set; }

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

            var result = JsonSerializer.Deserialize<LoginResponse>(responseJson);

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

    #endregion

    #region Today / Tasks

    public async Task<TodayDataResponse> GetTodayDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/today-data/");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"TodayData: {json}");
                var data = JsonSerializer.Deserialize<TodayDataResponse>(json) ?? new TodayDataResponse();
                
                // Cache tasks for later use in TaskDetailPage
                _taskCache.Clear();
                foreach (var task in data.Tasks)
                {
                    _taskCache[task.Id] = task;
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

    public async Task<CleaningTask?> GetTaskDetailAsync(int taskId)
    {
        // First check cache (tasks loaded from today-data)
        if (_taskCache.TryGetValue(taskId, out var cachedTask))
        {
            System.Diagnostics.Debug.WriteLine($"TaskDetail from cache: {taskId}");
            return cachedTask;
        }

        // Fallback: reload today data to get fresh tasks
        try
        {
            System.Diagnostics.Debug.WriteLine($"TaskDetail: reloading today data for task {taskId}");
            var todayData = await GetTodayDataAsync();
            if (_taskCache.TryGetValue(taskId, out var task))
            {
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

    public async Task<ChatSendResponse> SendChatMessageAsync(string text)
    {
        try
        {
            var data = new { text = text };
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

    public async Task<TranslationPreviewResponse> PreviewTranslationAsync(string text)
    {
        try
        {
            var data = new { text = text };
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
}
