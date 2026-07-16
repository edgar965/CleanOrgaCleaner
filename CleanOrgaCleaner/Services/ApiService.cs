using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Json;

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

    // Source-generated JSON options (no reflection, iOS AOT compatible)
    private static readonly JsonSerializerOptions _jsonOptions = AppJsonContext.Default.Options;

    // Store cleaner info after login
    public string? CleanerName { get; private set; }
    public string? CleanerLanguage { get; private set; }
    public int? CleanerId { get; private set; }

    /// <summary>
    /// Set cleaner info for offline mode (when we can't login to server)
    /// </summary>
    public void SetOfflineCleanerInfo(string cleanerName, int? cleanerId)
    {
        CleanerName = cleanerName;
        CleanerId = cleanerId;
        System.Diagnostics.Debug.WriteLine($"[ApiService] Offline cleaner info set: {cleanerName}, ID: {cleanerId}");
    }

    // Debug callback for LoginPage to show logs on-screen
    public static Action<string>? DebugLog { get; set; }

    // Offline support
    public bool IsOnline => WebSocketService.Instance.IsOnline;

    // Task cache - stores tasks loaded from today-data
    // ConcurrentDictionary: wird auf Thread-Pool-Threads (GetTodayDataAsync in
    // Task.Run) geschrieben und parallel gelesen - ein normales Dictionary
    // kann dabei intern korrumpieren
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, CleaningTask> _taskCache = new();

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

    // Datei-Logging für iOS (kein UI-Thread während Login)
    private static string? _logFilePath;
    private static readonly object _logLock = new object();

    public static void InitFileLogging()
    {
        var dir = FileSystem.CacheDirectory;
        _logFilePath = Path.Combine(dir, "login_debug.log");

        // Alte Log-Datei löschen, neue anfangen
        try
        {
            if (File.Exists(_logFilePath))
                File.Delete(_logFilePath);
        }
        catch { }
    }

    public static string? GetPreviousLogs()
    {
        var dir = FileSystem.CacheDirectory;
        var path = Path.Combine(dir, "login_debug.log");

        try
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }
        catch { }
        return null;
    }

    public static void WriteLog(string msg)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = $"[{timestamp}] {msg}";

        System.Diagnostics.Debug.WriteLine($"[LOGIN-DBG] {line}");

        // In Datei schreiben (synchron, thread-safe)
        if (_logFilePath != null)
        {
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + "\n");
                }
                catch { }
            }
        }
    }

    private static void DbgLog(string msg) => WriteLog($"[API] {msg}");

    /// <summary>
    /// Komplett synchroner Login - kein async/await, kein SynchronizationContext.
    /// Muss von Task.Run() aufgerufen werden um UI nicht zu blockieren.
    /// </summary>
    public LoginResult LoginSync(int propertyId, string username, string password)
    {
        try
        {
            DbgLog($"ENTER prop={propertyId} user={username}");

            var loginData = new
            {
                property_id = propertyId,
                username = username,
                password = password
            };

            DbgLog("JSON serialize");
            var json = JsonSerializer.Serialize(loginData);
            DbgLog($"JSON done: {json.Length} chars");

            var jsonBytes = Encoding.UTF8.GetBytes(json);
            DbgLog($"HttpWebRequest START -> {BaseUrl}/mobile/api/login/");

#pragma warning disable SYSLIB0014 // HttpWebRequest is obsolete - intentional: HttpClient.Send() throws PlatformNotSupportedException on iOS
            var webReq = System.Net.HttpWebRequest.CreateHttp($"{BaseUrl}/mobile/api/login/");
#pragma warning restore SYSLIB0014
            webReq.Method = "POST";
            webReq.ContentType = "application/json; charset=utf-8";
            webReq.Accept = "application/json";
            webReq.CookieContainer = _handler.CookieContainer;
            webReq.Timeout = 30000;
            webReq.ContentLength = jsonBytes.Length;

            DbgLog("Writing request body");
            using (var reqStream = webReq.GetRequestStream())
            {
                reqStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
            DbgLog("GetResponse START");

            string responseJson;
            try
            {
                using var webResp = (System.Net.HttpWebResponse)webReq.GetResponse();
                DbgLog($"GetResponse DONE -> {webResp.StatusCode}");
                // WICHTIG (iOS): Session-Cookie direkt aus der Login-Antwort greifen.
                // Der native NSUrlSessionHandler synchronisiert das Cookie-Storage NICHT
                // zuverlaessig in den managed CookieContainer, den GetCookieHeader() fuer
                // den WebSocket ausliest -> der WS-Handshake ging ohne sessionid raus (403).
                // Aus den Set-Cookie-Headern der Antwort lesen wir sessionid/ws_auth direkt
                // (HttpOnly betrifft nur JS, nicht den HTTP-Client) und legen sie ab.
                ExtractAuthCookies(webResp);
                using var respStream = webResp.GetResponseStream();
                using var reader = new System.IO.StreamReader(respStream);
                responseJson = reader.ReadToEnd();
            }
            catch (System.Net.WebException wex) when (wex.Response is System.Net.HttpWebResponse errResp)
            {
                DbgLog($"HTTP Error -> {errResp.StatusCode}");
                using var errStream = errResp.GetResponseStream();
                using var errReader = new System.IO.StreamReader(errStream);
                responseJson = errReader.ReadToEnd();
            }
            DbgLog($"Response -> {responseJson.Length} chars");
            DbgLog($"JSON: {responseJson}");

            // Manual JSON parsing - JsonSerializer.Deserialize hangs on iOS (AOT/reflection issue)
            DbgLog("JsonDocument.Parse START");
            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            DbgLog("JsonDocument.Parse DONE");

            var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

            if (success)
            {
                string? cleanerName = null;
                int? cleanerId = null;

                string? cleanerLanguage = null;
                if (root.TryGetProperty("cleaner", out var cleanerProp) && cleanerProp.ValueKind == JsonValueKind.Object)
                {
                    cleanerName = cleanerProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    cleanerId = cleanerProp.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : null;
                    // Extract and store avatar (always update, even if empty = default logo)
                    var avatar = cleanerProp.TryGetProperty("avatar", out var avatarProp) ? avatarProp.GetString() : "";
                    Preferences.Set("avatar", avatar ?? "");
                    DbgLog($"Avatar stored: '{avatar}'");
                    // Extract and store language from server
                    cleanerLanguage = cleanerProp.TryGetProperty("language", out var langProp) ? langProp.GetString() : null;
                    DbgLog($"Server returned language: '{cleanerLanguage}'");
                    if (!string.IsNullOrEmpty(cleanerLanguage))
                    {
                        Preferences.Set("language", cleanerLanguage);
                        Localization.Translations.CurrentLanguage = cleanerLanguage;
                        DbgLog($"Language stored and applied: {cleanerLanguage}");
                    }
                }

                CleanerName = cleanerName;
                CleanerLanguage = cleanerLanguage;
                CleanerId = cleanerId;
                DbgLog($"Cleaner: {CleanerName}, id={CleanerId}");

                DbgLog("StartHeartbeat");
                StartHeartbeat();
                DbgLog("StartHeartbeat DONE");

                DbgLog("returning SUCCESS");
                return new LoginResult
                {
                    Success = true,
                    CleanerName = cleanerName,
                    CleanerLanguage = cleanerLanguage
                };
            }

            var error = root.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null;
            DbgLog($"returning FAILED: {error}");
            return new LoginResult
            {
                Success = false,
                ErrorMessage = error ?? "Login fehlgeschlagen"
            };
        }
        catch (Exception ex)
        {
            DbgLog($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            DbgLog($"Stack: {ex.StackTrace}");
            return new LoginResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Pure login: HTTP POST + JSON parse. No side effects (no heartbeat, no websocket).
    /// Call StartHeartbeat() separately after login succeeds.
    /// Uses Task.Run internally so PostAsync runs off the UI thread (iOS MAUI requirement).
    /// </summary>
    public Task<LoginResult> LoginAsync(int propertyId, string username, string password)
    {
        return Task.Run(async () =>
        {
            try
            {
                DbgLog($"ENTER prop={propertyId} user={username}");
                var loginData = new { property_id = propertyId, username, password };
                var json = JsonSerializer.Serialize(loginData);
                DbgLog($"done: {json.Length} chars");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                DbgLog($"PostAsync START -> {BaseUrl}/mobile/api/login/");
                var response = await _httpClient.PostAsync($"{BaseUrl}/mobile/api/login/", content).ConfigureAwait(false);
                DbgLog($"PostAsync DONE -> {response.StatusCode}");

                DbgLog("ReadAsStringAsync START");
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                DbgLog($"ReadAsStringAsync DONE -> {responseJson.Length} chars");

                // Log ersten 200 Zeichen des JSON zur Diagnose
                var preview = responseJson.Length > 200 ? responseJson.Substring(0, 200) + "..." : responseJson;
                DbgLog($"JSON: {preview}");

                return ParseLoginResponse(responseJson);
            }
            catch (Exception ex)
            {
                DbgLog($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                return new LoginResult { Success = false, ErrorMessage = ex.Message };
            }
        });
    }

    /// <summary>
    /// Synchron: JSON parsen, LoginResult bauen.
    /// </summary>
    private LoginResult ParseLoginResponse(string responseJson)
    {
        try
        {
            DbgLog("JsonDocument.Parse START");
            using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
            DbgLog("JsonDocument.Parse DONE");

            var root = doc.RootElement;
            DbgLog("root.RootElement OK");

            var success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
            DbgLog($"success={success}");

            if (!success)
            {
                var error = root.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null;
                DbgLog($"returning FAILED: {error}");
                return new LoginResult { Success = false, ErrorMessage = error ?? "Login fehlgeschlagen" };
            }

            string? cleanerName = null;
            int? cleanerId = null;
            DbgLog("reading cleaner object");

            string? cleanerLanguage = null;
            if (root.TryGetProperty("cleaner", out var cleanerProp) && cleanerProp.ValueKind == JsonValueKind.Object)
            {
                DbgLog("cleaner object found");
                cleanerName = cleanerProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                DbgLog($"cleanerName={cleanerName}");
                cleanerId = cleanerProp.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : null;
                DbgLog($"cleanerId={cleanerId}");

                // Extract and store avatar (always update, even if empty = default logo)
                var avatar = cleanerProp.TryGetProperty("avatar", out var avatarProp) ? avatarProp.GetString() : "";
                Preferences.Set("avatar", avatar ?? "");
                DbgLog($"Avatar stored: '{avatar}'");

                // Extract and store language from server
                cleanerLanguage = cleanerProp.TryGetProperty("language", out var langProp) ? langProp.GetString() : null;
                DbgLog($"language from server: '{cleanerLanguage}'");
                if (!string.IsNullOrEmpty(cleanerLanguage))
                {
                    Preferences.Set("language", cleanerLanguage);
                    Localization.Translations.CurrentLanguage = cleanerLanguage;
                    DbgLog($"Language stored and applied: {cleanerLanguage}");
                }
            }

            CleanerName = cleanerName;
            DbgLog("CleanerName set");
            CleanerLanguage = cleanerLanguage;
            CleanerId = cleanerId;
            DbgLog($"Cleaner: {CleanerName}, id={CleanerId}, lang={CleanerLanguage}");

            DbgLog("returning SUCCESS");
            return new LoginResult { Success = true, CleanerName = cleanerName, CleanerLanguage = cleanerLanguage, CleanerId = cleanerId };
        }
        catch (Exception ex)
        {
            DbgLog($"ParseLoginResponse EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return new LoginResult { Success = false, ErrorMessage = $"JSON Parse Error: {ex.Message}" };
        }
    }

    public void Logout()
    {
        StopHeartbeat();
        CleanerName = null;
        CleanerLanguage = null;
        CleanerId = null;
        ClearCookies();
    }

    public async Task LogoutAsync()
    {
        // WICHTIG: Heartbeat ZUERST stoppen, BEVOR wir den Server-Logout aufrufen!
        // Sonst kann der Heartbeat-Timer während des Logouts feuern und
        // den Online-Status senden, was den Offline-Status überschreibt.
        StopHeartbeat();

        try
        {
            // Server-Logout aufrufen (sendet Offline-Status via WebSocket)
            await _httpClient.PostAsync("/mobile/api/logout/", null).ConfigureAwait(false);
        }
        catch
        {
            // Ignore - server logout is optional
        }

        // Clear local state (cookies, etc.)
        CleanerName = null;
        CleanerLanguage = null;
        CleanerId = null;
        ClearCookies();
    }

    private void ClearCookies()
    {
        _handler.CookieContainer = new System.Net.CookieContainer();
        // Auch die fuer den WebSocket abgelegten Auth-Cookies verwerfen,
        // sonst wuerde sich ein neuer Login mit altem Token verbinden.
        foreach (var (_, pref) in _wsCookieNamen)
            Preferences.Remove(pref);
    }

    /// <summary>
    /// Get session cookies for WebSocket connections
    /// </summary>
    // Namen der Cookies, die wir fuer den WebSocket-Handshake persistieren.
    private static readonly (string Name, string Pref)[] _wsCookieNamen =
    {
        ("sessionid", "ws_sessionid"),
        ("ws_auth", "ws_auth"),
        ("csrftoken", "ws_csrftoken"),
        ("property_id", "ws_property_id"),
    };

    /// <summary>
    /// Liest die auth-relevanten Cookies direkt aus der (Login-)Antwort und legt
    /// sie in den Preferences ab. Notwendig auf iOS, weil der managed
    /// CookieContainer dort nicht zuverlaessig befuellt wird (siehe LoginSync).
    /// </summary>
    private void ExtractAuthCookies(System.Net.HttpWebResponse resp)
    {
        try
        {
            // 1) Bevorzugt die geparste Cookie-Collection der Antwort
            foreach (System.Net.Cookie c in resp.Cookies)
                SpeichereWsCookie(c.Name, c.Value);

            // 2) Zusaetzlich roh aus dem Set-Cookie-Header (iOS: Cookies-Collection
            //    kann leer sein, der Header ist aber vorhanden). Pro bekannter Name
            //    per Regex den Wert ziehen - robust gegen zusammengefaltete Header.
            var raw = resp.Headers["Set-Cookie"];
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (var (name, _) in _wsCookieNamen)
                {
                    var m = System.Text.RegularExpressions.Regex.Match(
                        raw, name + @"=([^;,\s]+)");
                    if (m.Success)
                        SpeichereWsCookie(name, m.Groups[1].Value);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Cookie] ExtractAuthCookies-Fehler: {ex.Message}");
        }
    }

    private void SpeichereWsCookie(string name, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        foreach (var (n, pref) in _wsCookieNamen)
        {
            if (n == name)
            {
                Preferences.Set(pref, value);
                return;
            }
        }
    }

    /// <summary>
    /// Cookie-Header fuer WebSocket-Verbindungen. Baut ihn bevorzugt aus den beim
    /// Login direkt aus der Antwort gegriffenen Cookies (zuverlaessig auf iOS), da
    /// der managed CookieContainer dort veraltet/leer sein kann. Fallback: Container.
    /// </summary>
    public string GetCookieHeader()
    {
        var parts = new List<string>();
        foreach (var (name, pref) in _wsCookieNamen)
        {
            var v = Preferences.Get(pref, "");
            if (!string.IsNullOrEmpty(v))
                parts.Add($"{name}={v}");
        }
        if (parts.Count > 0)
            return string.Join("; ", parts);

        // Fallback (Android/Desktop, wo der managed Container funktioniert)
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
    /// Uses temp file caching for iOS compatibility
    /// </summary>
    public async Task<ImageSource?> GetImageAsync(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return null;

            // Store original URL for caching (before making absolute)
            var originalUrl = url;

            // Make URL absolute if relative
            if (!url.StartsWith("http"))
                url = $"{BaseUrl}{url}";

            System.Diagnostics.Debug.WriteLine($"GetImageAsync: {url}");

            // Try to download from server first
            try
            {
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"GetImageAsync: Got {bytes.Length} bytes");

                    // Cache the image for offline use
                    _ = OfflineDataService.Instance.CacheImageAsync(originalUrl, bytes);

                    // iOS fix: Save to temp file instead of using MemoryStream
                    var tempFile = Path.Combine(FileSystem.CacheDirectory, $"img_{Guid.NewGuid():N}.jpg");
                    await File.WriteAllBytesAsync(tempFile, bytes).ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"GetImageAsync: Saved to {tempFile}");
                    return ImageSource.FromFile(tempFile);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GetImageAsync: Failed {response.StatusCode}");
                }
            }
            catch (Exception netEx)
            {
                System.Diagnostics.Debug.WriteLine($"GetImageAsync network error: {netEx.Message}");

                // Try to load from cache when network fails
                var cachedPath = OfflineDataService.Instance.GetCachedImagePath(originalUrl);
                if (cachedPath != null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetImageAsync: Loading from cache: {cachedPath}");
                    return ImageSource.FromFile(cachedPath);
                }
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

    public Task<TodayDataResponse> GetTodayDataAsync()
    {
        // iOS: Run on thread pool to avoid UI thread deadlock (same pattern as LoginAsync)
        // WICHTIG: Fehler werden NICHT geschluckt, sondern weitergeworfen -
        // nur so kann die TodayPage auf den Offline-Cache zurückfallen, statt
        // eine leere Liste anzuzeigen (und den Cache damit zu überschreiben).
        return Task.Run(async () =>
        {
            try
            {
                WriteLog("[API] GetTodayDataAsync ENTER");
                var cacheBuster = DateTime.Now.Ticks;

                WriteLog("[API] GetAsync START");
                var response = await _httpClient.GetAsync($"/mobile/api/today-data/?_={cacheBuster}").ConfigureAwait(false);
                WriteLog($"[API] GetAsync DONE: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    WriteLog($"[API] GetTodayDataAsync FAILED: {response.StatusCode}");
                    // Eigener Typ: Server hat GEANTWORTET (kein Netzwerkproblem).
                    // Die TodayPage unterscheidet darueber Server- vs. Netzfehler,
                    // statt fehleranfälliger ex.Message-Strings zu matchen.
                    throw new ServerAntwortFehler((int)response.StatusCode);
                }

                WriteLog("[API] ReadAsStringAsync START");
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                WriteLog($"[API] ReadAsStringAsync DONE: {json.Length} chars");

                WriteLog("[API] JsonSerializer.Deserialize START");
                var data = JsonSerializer.Deserialize<TodayDataResponse>(json, _jsonOptions) ?? new TodayDataResponse();
                WriteLog($"[API] Deserialize DONE: {data.Tasks?.Count ?? 0} tasks");

                // Cache tasks (Tasks kann bei unerwartetem JSON null sein)
                _taskCache.Clear();
                foreach (var task in data.Tasks ?? new List<CleaningTask>())
                {
                    _taskCache[task.Id] = task;
                }

                WriteLog("[API] GetTodayDataAsync SUCCESS");
                return data;
            }
            catch (Exception ex)
            {
                WriteLog($"[API] GetTodayDataAsync ERROR: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<CleaningTask?> GetAufgabeDetailAsync(int taskId, bool forceRefresh = true)
    {
        // Always reload to get fresh data with images
        // forceRefresh=true by default to ensure images are loaded
        if (!forceRefresh && _taskCache.TryGetValue(taskId, out var cachedTask))
        {
            System.Diagnostics.Debug.WriteLine($"Aufgabe from cache: {taskId}, Problems: {cachedTask.Problems?.Count ?? 0}, Anmerkungen: {cachedTask.Anmerkungen?.Count ?? 0}");
            return cachedTask;
        }

        // Reload today data to get fresh tasks with images
        try
        {
            System.Diagnostics.Debug.WriteLine($"Aufgabe: reloading today data for task {taskId}");
            _taskCache.Clear(); // Clear cache to force fresh data
            var todayData = await GetTodayDataAsync().ConfigureAwait(false);
            if (_taskCache.TryGetValue(taskId, out var task))
            {
                System.Diagnostics.Debug.WriteLine($"Aufgabe loaded: {taskId}, Problems: {task.Problems?.Count ?? 0}, Anmerkungen: {task.Anmerkungen?.Count ?? 0}");
                return task;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAufgabeDetail error: {ex.Message}");
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

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/start-time/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"StartWork: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WorkTimeResponse>(responseText, _jsonOptions)
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

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/end-time/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"EndWork: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WorkTimeResponse>(responseText, _jsonOptions)
                    ?? new WorkTimeResponse { Success = true };
            }
            return new WorkTimeResponse { Success = false, Error = responseText };
        }
        catch (Exception ex)
        {
            return new WorkTimeResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<WorkTimeResponse?> GetWorkStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/cleaner/work-status/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"GetWorkStatus: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WorkTimeResponse>(responseText, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetWorkStatus error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> StopWorkAsync()
    {
        var result = await EndWorkAsync().ConfigureAwait(false);
        return result.Success;
    }

    public async Task<TaskStateResponse> UpdateTaskStateAsync(int taskId, string state)
    {
        try
        {
            var data = new { state_completed = state };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/task/{taskId}/state/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"UpdateTaskState: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<TaskStateResponse>(responseText, _jsonOptions)
                ?? new TaskStateResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new TaskStateResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<TaskStateResponse> StartTaskAsync(int taskId)
    {
        return await UpdateTaskStateAsync(taskId, "started").ConfigureAwait(false);
    }

    public async Task<TaskStateResponse> StopTaskAsync(int taskId)
    {
        return await UpdateTaskStateAsync(taskId, "completed").ConfigureAwait(false);
    }

    public async Task<ChecklistToggleResponse> ToggleChecklistItemAsync(int taskId, int itemIndex)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/{taskId}/checklist/{itemIndex}/toggle/", null).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ChecklistToggleResponse>(responseText, _jsonOptions)
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
        return await ToggleChecklistItemAsync(taskId, itemIndex).ConfigureAwait(false);
    }

    // ===== Putzliste (neue Checkliste pro Apartment + Aufgabenart) =====

    /// <summary>Hakt einen Putzlisten-Eintrag für eine Aufgabe ab / wieder ab.</summary>
    public async Task<ChecklistToggleResponse> TogglePutzlisteItemAsync(int taskId, int eintragId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/{taskId}/putzliste/{eintragId}/toggle/", null).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<ChecklistToggleResponse>(responseText, _jsonOptions)
                ?? new ChecklistToggleResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TogglePutzliste error: {ex.Message}");
            return new ChecklistToggleResponse { Success = false };
        }
    }

    /// <summary>Speichert die Anmerkung des Cleaners zu einem Putzlisten-Eintrag.</summary>
    public async Task<ApiResponse> SavePutzlisteEintragKommentarAsync(int taskId, int eintragId, string kommentar)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { kommentar = kommentar });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/{taskId}/putzliste/{eintragId}/kommentar/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Speichert die Anmerkung des Cleaners zur gesamten Checkliste der Aufgabe.</summary>
    public async Task<ApiResponse> SavePutzlisteChecklistKommentarAsync(int taskId, string kommentar)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { kommentar = kommentar });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/{taskId}/putzliste/kommentar/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Lädt ein Beweis-Foto zu einem Putzlisten-Eintrag hoch.</summary>
    public async Task<PutzlisteFotoResponse> UploadPutzlisteFotoAsync(int taskId, int eintragId, string fileName, byte[] bytes)
    {
        try
        {
            using var formData = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            formData.Add(fileContent, "foto", fileName);

            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/{taskId}/putzliste/{eintragId}/foto/", formData).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new PutzlisteFotoResponse { Success = false, Error = $"Server error {(int)response.StatusCode}" };
            return JsonSerializer.Deserialize<PutzlisteFotoResponse>(responseText, _jsonOptions)
                ?? new PutzlisteFotoResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UploadPutzlisteFoto error: {ex.Message}");
            return new PutzlisteFotoResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Löscht ein Beweis-Foto eines Putzlisten-Eintrags.</summary>
    public async Task<ApiResponse> DeletePutzlisteFotoAsync(int fotoId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/mobile/api/task/putzliste/foto/{fotoId}/delete/", null).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeletePutzlisteFoto error: {ex.Message}");
            return new ApiResponse { Success = false };
        }
    }

    /// <summary>
    /// Save task notes - alias for SaveTaskNoteAsync
    /// </summary>
    public async Task<ApiResponse> SaveTaskNotesAsync(int taskId, string notes)
    {
        return await SaveTaskNoteAsync(taskId, notes).ConfigureAwait(false);
    }

    #endregion

    #region ImageListDescription API

    /// <summary>
    /// Create ImageListDescription (problem or anmerkung) with byte array photos
    /// </summary>
    public async Task<ImageListDescriptionResponse> CreateImageListItemAsync(int taskId, string itemType, string name, string? description, List<(string FileName, byte[] Bytes)>? photos)
    {
        try
        {
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(name), "name");
            if (!string.IsNullOrEmpty(description))
                formData.Add(new StringContent(description), "description");

            if (photos != null)
            {
                foreach (var photo in photos)
                {
                    var fileContent = new ByteArrayContent(photo.Bytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    formData.Add(fileContent, "images", photo.FileName);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CreateImageListItem] POST /api/task/{taskId}/items/{itemType}/create/ with {photos?.Count ?? 0} photos");
            var response = await _httpClient.PostAsync($"/api/task/{taskId}/items/{itemType}/create/", formData).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[CreateImageListItem] Response: {response.StatusCode} - {responseText.Substring(0, Math.Min(200, responseText.Length))}");

            if (!response.IsSuccessStatusCode)
            {
                return new ImageListDescriptionResponse { Success = false, Error = $"Server error {(int)response.StatusCode}: {responseText.Substring(0, Math.Min(100, responseText.Length))}" };
            }

            return JsonSerializer.Deserialize<ImageListDescriptionResponse>(responseText, _jsonOptions)
                ?? new ImageListDescriptionResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateImageListItem] Exception: {ex.Message}");
            return new ImageListDescriptionResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Update ImageListDescription item
    /// </summary>
    public async Task<ImageListDescriptionResponse> UpdateImageListItemAsync(int itemId, string name, string? description)
    {
        try
        {
            var data = new { name = name, description = description ?? "" };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"[UpdateImageListItem] POST /api/image-list/{itemId}/update/");
            var response = await _httpClient.PostAsync($"/api/image-list/{itemId}/update/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[UpdateImageListItem] Response: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<ImageListDescriptionResponse>(responseText, _jsonOptions)
                ?? new ImageListDescriptionResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateImageListItem] Exception: {ex.Message}");
            return new ImageListDescriptionResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Delete ImageListDescription item
    /// </summary>
    public async Task<ImageListDescriptionDeleteResponse> DeleteImageListItemAsync(int itemId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DeleteImageListItem] POST /api/image-list/{itemId}/delete/");
            var response = await _httpClient.PostAsync($"/api/image-list/{itemId}/delete/", null).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[DeleteImageListItem] Response: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<ImageListDescriptionDeleteResponse>(responseText, _jsonOptions)
                ?? new ImageListDescriptionDeleteResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeleteImageListItem] Exception: {ex.Message}");
            return new ImageListDescriptionDeleteResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Delete a photo from ImageListDescription
    /// </summary>
    public async Task<ApiResponse> DeleteImageListPhotoAsync(int photoId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DeleteImageListPhoto] POST /api/image-list/photo/{photoId}/delete/");
            var response = await _httpClient.PostAsync($"/api/image-list/photo/{photoId}/delete/", null).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[DeleteImageListPhoto] Response: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeleteImageListPhoto] Exception: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

    public async Task<ApiResponse> SaveTaskNoteAsync(int taskId, string note)
    {
        try
        {
            var data = new { anmerkung_mitarbeiter = note };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/notiz/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #region Task Logs

    public async Task<List<LogEntry>> GetTaskLogsAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskLogs: Loading logs for task {taskId}");
            var response = await _httpClient.GetAsync($"/api/task/{taskId}/logs/").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

    #region Chat

    public async Task<List<ChatMessage>> GetChatMessagesAsync(string partnerId = "admin")
    {
        try
        {
            var url = $"/mobile/api/chat/messages/?partner_id={partnerId}";
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"ChatMessages ({partnerId}): {json}");
                var result = JsonSerializer.Deserialize<ChatMessagesResponse>(json, _jsonOptions);
                return result?.Messages ?? new List<ChatMessage>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetChatMessages error: {ex.Message}");
        }
        return new List<ChatMessage>();
    }

    public async Task<ChatSendResponse> SendChatMessageAsync(string text, string receiverId = "admin", string? linkPhotoVideo = null)
    {
        try
        {
            var data = new { text = text, receiver_id = receiverId, link_photo_video = linkPhotoVideo ?? "" };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/chat/send/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"SendChat: {response.StatusCode} - {responseText}");

            return JsonSerializer.Deserialize<ChatSendResponse>(responseText, _jsonOptions)
                ?? new ChatSendResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ChatSendResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ChatImageUploadResponse> UploadChatImageAsync(Stream imageStream, string fileName)
    {
        try
        {
            using var formData = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(fileName));
            formData.Add(streamContent, "image", fileName);

            var response = await _httpClient.PostAsync("/api/chat/upload-image/", formData).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"UploadChatImage: {response.StatusCode} - {responseText}");

            // Check if response is JSON (not HTML redirect/error page)
            if (string.IsNullOrEmpty(responseText) || !responseText.TrimStart().StartsWith("{"))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    return new ChatImageUploadResponse { Success = false, Error = "Keine Berechtigung. Bitte neu anmelden." };
                return new ChatImageUploadResponse { Success = false, Error = $"Server-Fehler: {response.StatusCode}" };
            }

            return JsonSerializer.Deserialize<ChatImageUploadResponse>(responseText, _jsonOptions)
                ?? new ChatImageUploadResponse { Success = false, Error = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ChatImageUploadResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> DeleteChatImageAsync(string path)
    {
        try
        {
            var data = new { path = path };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/chat/delete-image/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Löscht das Bild/Video aus einer bereits gesendeten Nachricht
    /// </summary>
    public async Task<ApiResponse> DeleteMessageImageAsync(int messageId)
    {
        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/chat/message/{messageId}/delete-image/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"[API] DeleteMessageImage response: {response.StatusCode} - {responseText.Substring(0, Math.Min(200, responseText.Length))}");

            // Prüfen ob Antwort JSON ist
            if (string.IsNullOrEmpty(responseText) || !responseText.TrimStart().StartsWith("{"))
            {
                // Nicht-JSON Antwort (z.B. Login-Redirect)
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new ApiResponse { Success = false, Error = "Keine Berechtigung. Bitte neu anmelden." };
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new ApiResponse { Success = false, Error = "Nachricht nicht gefunden." };
                }
                return new ApiResponse { Success = false, Error = $"Server-Fehler: {response.StatusCode}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"[API] JSON parse error: {jsonEx.Message}");
            return new ApiResponse { Success = false, Error = "Ungültige Server-Antwort" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] DeleteMessageImage error: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }

    public async Task<TranslationPreviewResponse> PreviewTranslationAsync(string text, string? receiverId = null)
    {
        try
        {
            var data = new { text = text, receiver_id = receiverId };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/chat/preview-translation/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<TranslationPreviewResponse>(responseText, _jsonOptions)
                ?? new TranslationPreviewResponse { Success = false };
        }
        catch (Exception ex)
        {
            return new TranslationPreviewResponse { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse> DeleteChatMessagesAsync(string receiverId = "admin")
    {
        try
        {
            // partner_id mitgeben - sonst löscht der Server den Default-Chat
            // (Admin) statt des tatsächlich geöffneten Chats
            var response = await _httpClient.PostAsync(
                $"/mobile/api/chat/delete/?partner_id={Uri.EscapeDataString(receiverId)}", null).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Registriert das FCM-Push-Token dieses Geräts beim Server.
    /// platform: "android" oder "ios".
    /// </summary>
    public async Task<ApiResponse> RegisterPushTokenAsync(string token, string platform)
    {
        try
        {
            var data = new { token = token, platform = platform };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/push/register/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Meldet das FCM-Push-Token dieses Geräts beim Server ab (z.B. bei Logout).
    /// </summary>
    public async Task<ApiResponse> UnregisterPushTokenAsync(string token)
    {
        try
        {
            var data = new { token = token };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/push/unregister/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
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

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/language/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                CleanerLanguage = language;
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> SetAvatarAsync(string avatar)
    {
        try
        {
            var data = new { avatar = avatar };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/cleaner/avatar/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
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
            var response = await _httpClient.GetAsync("/mobile/api/cleaners/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CleanersListResponse>(responseText, _jsonOptions);
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

    public async Task<CleanersListResponse?> GetCleanersListAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/cleaners/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<CleanersListResponse>(responseText, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetCleanersList error: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region My Tasks

    public async Task<AuftragsPageDataResponse> GetAuftragsDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/my-tasks-data/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"GetAuftragsData: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<AuftragsPageDataResponse>(responseText, _jsonOptions)
                    ?? new AuftragsPageDataResponse { Success = false, Error = "Parse error" };
            }
            return new AuftragsPageDataResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAuftragsData error: {ex.Message}");
            return new AuftragsPageDataResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResponse> CreateAuftragAsync(string name, string plannedDate, int? apartmentId, int? aufgabenartId, string? hinweis, string status, TaskAssignments? assignments)
    {
        try
        {
            var data = new
            {
                name = name,
                planned_date = plannedDate,
                apartment_id = apartmentId,
                aufgabenart_id = aufgabenartId,
                aufgabe = hinweis ?? "",
                status = status,
                assignments = assignments ?? new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() }
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/mobile/api/task/create/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"CreateAuftrag: {response.StatusCode} - {responseText}");

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

    public async Task<ApiResponse> UpdateAuftragAsync(int taskId, string name, string plannedDate, int? apartmentId, int? aufgabenartId, string? hinweis, string status, TaskAssignments? assignments)
    {
        try
        {
            var data = new
            {
                name = name,
                planned_date = plannedDate,
                apartment_id = apartmentId,
                aufgabenart_id = aufgabenartId,
                aufgabe = hinweis ?? "",
                status = status,
                assignments = assignments ?? new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() }
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/update/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"UpdateAuftrag: {response.StatusCode} - {responseText}");

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

    public async Task<ApiResponse> DeleteAuftragAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DeleteAuftrag: Deleting task {taskId}");
            var response = await _httpClient.PostAsync($"/mobile/api/task/{taskId}/delete/", new StringContent("{}")).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"DeleteAuftrag: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteAuftrag: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    public async Task<List<Views.TaskImageInfo>> GetTaskImagesAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskImages: Loading images for task {taskId}");
            var response = await _httpClient.GetAsync($"/api/task/{taskId}/images/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

    /// <summary>
    /// Get anmerkungen (ImageListDescription items) for a task
    /// </summary>
    public async Task<List<ImageListDescription>> GetTaskAnmerkungenAsync(int taskId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskAnmerkungen: Loading anmerkungen for task {taskId}");
            var response = await _httpClient.GetAsync($"/api/task/{taskId}/items/anmerkung/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"GetTaskAnmerkungen: /api/task/{taskId}/items/anmerkung/ - {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ImageListItemsResponse>(responseText, _jsonOptions);
                if (result?.Items != null)
                {
                    // Fix photo URLs
                    foreach (var item in result.Items)
                    {
                        if (item.Photos != null)
                        {
                            foreach (var photo in item.Photos)
                            {
                                if (!string.IsNullOrEmpty(photo.Url) && !photo.Url.StartsWith("http"))
                                    photo.Url = $"{BaseUrl}{photo.Url}";
                                if (!string.IsNullOrEmpty(photo.ThumbnailUrl) && !photo.ThumbnailUrl.StartsWith("http"))
                                    photo.ThumbnailUrl = $"{BaseUrl}{photo.ThumbnailUrl}";
                            }
                        }
                    }
                    return result.Items;
                }
            }
            return new List<ImageListDescription>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTaskAnmerkungen: EXCEPTION - {ex.Message}");
            return new List<ImageListDescription>();
        }
    }

    /// <summary>
    /// Create a new anmerkung for a task
    /// </summary>
    public async Task<ApiResponse> CreateTaskAnmerkungAsync(int taskId, string name, string description, List<byte[]> photos)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CreateTaskAnmerkung: Create for task {taskId}");

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(name), "name");
            formData.Add(new StringContent(description), "description");

            for (int i = 0; i < photos.Count; i++)
            {
                var fileContent = new ByteArrayContent(photos[i]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                formData.Add(fileContent, "photos", $"photo_{i}.jpg");
            }

            var response = await _httpClient.PostAsync($"/api/task/{taskId}/items/anmerkung/create/", formData).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"CreateTaskAnmerkung: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateTaskAnmerkung: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Update an existing ImageListDescription
    /// </summary>
    public async Task<ApiResponse> UpdateImageListDescriptionAsync(int id, string name, string description)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UpdateImageListDescription: Update {id}");
            var data = new { name, description };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/image-list/{id}/update/", content).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"UpdateImageListDescription: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateImageListDescription: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Add a photo to an existing ImageListDescription
    /// </summary>
    public async Task<ApiResponse> AddPhotoToImageListDescriptionAsync(int id, byte[] photoBytes)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"AddPhotoToImageListDescription: Add photo to {id}");

            var formData = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(photoBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            formData.Add(fileContent, "photo", "photo.jpg");

            var response = await _httpClient.PostAsync($"/api/image-list/{id}/add-photo/", formData).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"AddPhotoToImageListDescription: {response.StatusCode} - {responseText}");

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse { Success = false, Error = $"HTTP {(int)response.StatusCode}: {responseText}" };
            }

            return JsonSerializer.Deserialize<ApiResponse>(responseText, _jsonOptions)
                ?? new ApiResponse { Success = response.IsSuccessStatusCode };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddPhotoToImageListDescription: EXCEPTION - {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Heartbeat / Online Status

    private System.Timers.Timer? _heartbeatTimer;
    private int _heartbeatIntervalSeconds = 30;
    private volatile bool _heartbeatStopped = false; // Flag um Heartbeats nach Logout zu verhindern

    /// <summary>
    /// Starts the heartbeat timer to periodically ping the server.
    /// Call this after successful login.
    /// </summary>
    public void StartHeartbeat()
    {
        StopHeartbeat(); // Stop any existing timer
        _heartbeatStopped = false; // Reset flag

        _heartbeatTimer = new System.Timers.Timer(_heartbeatIntervalSeconds * 1000);
        _heartbeatTimer.Elapsed += async (sender, e) => await SendHeartbeatAsync().ConfigureAwait(false);
        _heartbeatTimer.AutoReset = true;
        _heartbeatTimer.Start();

        System.Diagnostics.Debug.WriteLine($"[Heartbeat] Started with interval {_heartbeatIntervalSeconds}s");

        // Send first heartbeat immediately
        _ = SendHeartbeatAsync();
    }

    /// <summary>
    /// Stops the heartbeat timer.
    /// Call this on logout.
    /// </summary>
    public void StopHeartbeat()
    {
        _heartbeatStopped = true; // Set flag FIRST to prevent any in-flight heartbeats

        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
            _heartbeatTimer = null;
            System.Diagnostics.Debug.WriteLine("[Heartbeat] Stopped");
        }
    }

    /// <summary>
    /// Sends a heartbeat to the server and updates the interval if changed.
    /// </summary>
    private async Task SendHeartbeatAsync()
    {
        // Check if heartbeat was stopped (logout in progress)
        if (_heartbeatStopped)
        {
            System.Diagnostics.Debug.WriteLine("[Heartbeat] Skipped - logout in progress");
            return;
        }

        try
        {
            var response = await _httpClient.GetAsync("/mobile/api/heartbeat/").ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Response: {response.StatusCode} - {responseText}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<HeartbeatResponse>(responseText, _jsonOptions);
                if (result?.PingInterval > 0 && result.PingInterval != _heartbeatIntervalSeconds)
                {
                    _heartbeatIntervalSeconds = result.PingInterval;
                    if (_heartbeatTimer != null)
                    {
                        _heartbeatTimer.Interval = _heartbeatIntervalSeconds * 1000;
                        System.Diagnostics.Debug.WriteLine($"[Heartbeat] Interval updated to {_heartbeatIntervalSeconds}s");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Error: {ex.Message}");
        }
    }

    #endregion

    #region Crash Reporting

    /// <summary>
    /// Send a crash report to the server
    /// </summary>
    public async Task<bool> SendCrashReportAsync(CrashReport report)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("timestamp", report.Timestamp.ToString("o")),
                new KeyValuePair<string, string>("source", report.Source),
                new KeyValuePair<string, string>("exception_type", report.ExceptionType),
                new KeyValuePair<string, string>("message", report.Message),
                new KeyValuePair<string, string>("stack_trace", report.StackTrace),
                new KeyValuePair<string, string>("inner_exception", report.InnerException ?? ""),
                new KeyValuePair<string, string>("device_info", report.DeviceInfo),
                new KeyValuePair<string, string>("app_version", report.AppVersion),
                new KeyValuePair<string, string>("cleaner_name", CleanerName ?? "Unknown"),
            });

            var response = await _httpClient.PostAsync("/api/crash-report/", content).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Send response: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Send error: {ex.Message}");
            return false;
        }
    }

    #endregion

}
/// <summary>
/// Der Server hat geantwortet, aber mit Fehlerstatus (z.B. 500 während eines
/// Deploys). Bewusst KEIN Netzwerkfehler: Aufrufer wie TodayPage dürfen dann
/// nicht auf den (evtl. tagealten) Offline-Cache zurueckfallen.
/// </summary>
public class ServerAntwortFehler : Exception
{
    public int StatusCode { get; }

    public ServerAntwortFehler(int statusCode)
        : base($"Server antwortete mit HTTP {statusCode}")
    {
        StatusCode = statusCode;
    }
}
