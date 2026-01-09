using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CleanOrgaCleaner.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://cleanorga.com";

    // Singleton instance to maintain session cookies across pages
    private static ApiService? _instance;
    private static readonly object _lock = new();

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
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<LoginResult> LoginAsync(int propertyId, string username, string password)
    {
        try
        {
            // Use JSON body for API endpoint
            var loginData = new
            {
                property_id = propertyId,
                username = username,
                password = password
            };

            var json = JsonSerializer.Serialize(loginData);
            System.Diagnostics.Debug.WriteLine($"Login request: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"Sending to: {BaseUrl}/mobile/api/login/");
            var response = await _httpClient.PostAsync("/mobile/api/login/", content);

            System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
            var responseJson = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Login response: {responseJson}");

            var result = JsonSerializer.Deserialize<ApiLoginResponse>(responseJson);

            if (result?.Success == true)
            {
                return new LoginResult
                {
                    Success = true,
                    CleanerName = result.Cleaner?.Name
                };
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = result?.Error ?? $"Server: {responseJson}"
            };
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error: {httpEx.Message}");
            return new LoginResult { Success = false, ErrorMessage = $"Netzwerk: {httpEx.Message}" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.GetType().Name}: {ex.Message}");
            return new LoginResult { Success = false, ErrorMessage = $"{ex.GetType().Name}: {ex.Message}" };
        }
    }

    public async Task<TodayDataResponse> GetTodayDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/today-data/");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Tasks response: {json}");
                return JsonSerializer.Deserialize<TodayDataResponse>(json) ?? new TodayDataResponse();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTodayData error: {ex.Message}");
        }
        return new TodayDataResponse();
    }

    public async Task<bool> StartWorkAsync()
    {
        try
        {
            var data = new { date = DateTime.Now.ToString("yyyy-MM-dd") };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/start-time/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"StartWork response: {response.StatusCode} - {responseText}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StartWork error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EndWorkAsync()
    {
        try
        {
            var data = new { date = DateTime.Now.ToString("yyyy-MM-dd") };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/end-time/", content);
            var responseText = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"EndWork response: {response.StatusCode} - {responseText}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EndWork error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateTaskStatusAsync(int taskId, string status)
    {
        try
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("status", status)
            });
            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/status/", formData);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// API Response Models
public class ApiLoginResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public ApiUser? User { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cleaner")]
    public ApiCleaner? Cleaner { get; set; }
}

public class ApiUser
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("username")]
    public string? Username { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("is_staff")]
    public bool IsStaff { get; set; }
}

public class ApiCleaner
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("language")]
    public string? Language { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CleanerName { get; set; }
}
