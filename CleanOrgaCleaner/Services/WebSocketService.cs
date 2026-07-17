using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Json;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// WebSocket service for real-time updates (chat + tasks over a single connection)
/// </summary>
public class WebSocketService : IDisposable
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    // Serialisiert ConnectAsync: Reconnect-Backoff, App-Resume und Login
    // können sonst parallel verbinden und sich gegenseitig den Socket disposen
    private readonly SemaphoreSlim _connectSperre = new(1, 1);
    // 0 = keine Reconnect-Kette aktiv, 1 = eine läuft (Interlocked-Guard)
    private int _reconnecting;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectDelay = 30000; // 30 seconds max
    private const int InitialReconnectDelay = 1000; // 1 second initial
    private const string WsBaseUrl = "wss://cleanorga.com";
    private bool _isOnline = false;
    private bool _shouldReconnect = true;

    // Events for UI updates
    public event Action<ChatMessage>? OnChatMessageReceived;
    public event Action<string>? OnTaskUpdate;
    public event Action<bool>? OnConnectionStatusChanged;

    /// <summary>
    /// Indicates if the WebSocket connection is currently online
    /// </summary>
    public bool IsOnline => _isOnline;

    /// <summary>
    /// True, sobald in dieser App-Sitzung mindestens einmal eine Verbindung
    /// bestand. Verhindert, dass der Offline-Banner beim allerersten Laden
    /// (Verbindungsaufbau läuft noch) fälschlich aufblitzt.
    /// </summary>
    public bool WarSchonVerbunden { get; private set; }

    /// <summary>
    /// Indicates if chat WebSocket is connected (backward-compatible property)
    /// </summary>
    public bool IsChatConnected => _socket?.State == WebSocketState.Open;

    /// <summary>
    /// Indicates if tasks WebSocket is connected (backward-compatible property)
    /// </summary>
    public bool IsTasksConnected => _socket?.State == WebSocketState.Open;

    private static WebSocketService? _instance;
    public static WebSocketService Instance => _instance ??= new WebSocketService();

    /// <summary>
    /// Connect to the unified WebSocket endpoint (/ws/main/).
    /// Startet bei Misserfolg die (einzige) Reconnect-Kette.
    /// </summary>
    public async Task ConnectAsync()
    {
        var ok = await VerbindeEinmalAsync().ConfigureAwait(false);
        if (!ok && _shouldReconnect && !App.IsInBackground)
            await TryReconnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Ein einzelner Verbindungsversuch (per Semaphore serialisiert),
    /// OHNE Folge-Reconnect - den steuert ausschliesslich TryReconnectAsync.
    /// </summary>
    private async Task<bool> VerbindeEinmalAsync()
    {
        if (_socket?.State == WebSocketState.Open)
            return true;

        var fehlgeschlagen = false;
        await _connectSperre.WaitAsync().ConfigureAwait(false);
        try
        {
            // Doppel-Check nach der Sperre: ein paralleler Aufruf kann die
            // Verbindung inzwischen aufgebaut haben
            if (_socket?.State == WebSocketState.Open)
                return true;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _socket?.Dispose();
            _socket = new ClientWebSocket();
            // Keepalive: iOS trennt untaetige WS-Verbindungen nach ~1 Min. Der
            // managed KeepAliveInterval wird auf iOS nicht zuverlaessig umgesetzt,
            // daher zusaetzlich ein eigener App-Ping (KeepAliveLoopAsync).
            _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            // Add session cookies for authentication
            var cookieHeader = ApiService.Instance.GetCookieHeader();
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                _socket.Options.SetRequestHeader("Cookie", cookieHeader);
                _socket.Options.SetRequestHeader("Origin", "https://cleanorga.com");
                System.Diagnostics.Debug.WriteLine($"WebSocket using cookies: {cookieHeader}");
            }

            var uri = new Uri($"{WsBaseUrl}/ws/main/");
            System.Diagnostics.Debug.WriteLine($"Connecting to WebSocket: {uri}");

            // Add timeout for iOS - prevent hanging forever
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);

            await _socket.ConnectAsync(uri, linkedCts.Token).ConfigureAwait(false);

            if (_socket.State == WebSocketState.Open)
            {
                // _reconnectAttempts NICHT hier zurücksetzen: eine flappende
                // Verbindung (verbindet, schließt sofort wieder) würde sonst
                // ewig im 1s-Takt neu verbinden. Reset erst bei erstem echten
                // Nachrichtenempfang (ListenForMessagesAsync) = Beweis, dass die
                // Verbindung wirklich trägt.
                var wasOffline = !_isOnline;
                _isOnline = true;
                WarSchonVerbunden = true;
                UiSicher.SichererInvoke(() => OnConnectionStatusChanged?.Invoke(true), "WS");
                System.Diagnostics.Debug.WriteLine("WebSocket connected (unified)");

                // Start listening for messages
                _ = ListenForMessagesAsync();

                // App-Keepalive-Ping, damit iOS die Verbindung nicht als untaetig
                // trennt (Server antwortet mit 'pong'). Nutzt denselben Socket/Token.
                _ = KeepAliveLoopAsync(_socket, _cts);

                // Process offline queue if we were offline
                if (wasOffline)
                {
                    _ = ProcessOfflineQueueAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket error: {ex.Message}");
            _isOnline = false;
            UiSicher.SichererInvoke(() => OnConnectionStatusChanged?.Invoke(false), "WS");
            fehlgeschlagen = true;
        }
        finally
        {
            _connectSperre.Release();
        }

        return !fehlgeschlagen;
    }

    /// <summary>
    /// Connect to chat WebSocket (backward-compatible - calls ConnectAsync)
    /// </summary>
    public Task ConnectChatAsync() => ConnectAsync();

    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        // Lokale Kopien von Socket + Token: greift ein Reconnect die Felder
        // _socket/_cts an, liest dieser (alte) Listener weiter seinen EIGENEN
        // Socket und beendet sich sauber, statt auf dem neuen Socket parallel
        // zum neuen Listener zu empfangen.
        var socket = _socket;
        var cts = _cts;
        if (socket == null || cts == null) return;

        try
        {
            while (socket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    System.Diagnostics.Debug.WriteLine("WebSocket closed by server");
                    break;
                }

                // Erster echter Empfang = Verbindung trägt -> Backoff-Zähler
                // zurücksetzen, damit ein späterer Abbruch wieder bei 1s startet.
                _reconnectAttempts = 0;

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(text);

                if (result.EndOfMessage)
                {
                    var fullMessage = messageBuilder.ToString();
                    messageBuilder.Clear();
                    ProcessMessage(fullMessage);
                }
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("WebSocket listening cancelled");
            return; // Don't try to reconnect on cancellation
        }
        catch (WebSocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket exception: {ex.Message}");
            if (App.IsInBackground) return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket listen error: {ex.Message}");
            if (App.IsInBackground) return;
        }

        // Nur reagieren, wenn dieser Listener noch der aktive ist (sein Socket
        // == aktuelles Feld). Ein durch Reconnect ersetzter Listener darf
        // weder Status noch eine weitere Reconnect-Kette auslösen.
        if (!ReferenceEquals(socket, _socket)) return;

        _isOnline = false;
        UiSicher.SichererInvoke(() => OnConnectionStatusChanged?.Invoke(false), "WS");
        if (_shouldReconnect && !App.IsInBackground)
            await TryReconnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sendet alle 20s einen App-Ping, solange die Verbindung offen ist. Ohne
    /// diesen Verkehr trennt iOS die WS-Verbindung nach ~1 Min als untaetig
    /// (Flappen). Laeuft auf dem EIGENEN Socket/Token - ein Reconnect beendet
    /// diesen Loop sauber, der neue Connect startet einen neuen.
    /// </summary>
    private static async Task KeepAliveLoopAsync(ClientWebSocket socket, CancellationTokenSource cts)
    {
        var ping = Encoding.UTF8.GetBytes("{\"type\":\"ping\"}");
        try
        {
            while (socket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(20), cts.Token).ConfigureAwait(false);
                if (socket.State != WebSocketState.Open || cts.Token.IsCancellationRequested)
                    break;
                await socket.SendAsync(new ArraySegment<byte>(ping),
                    WebSocketMessageType.Text, true, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WS] KeepAlive beendet: {ex.Message}");
        }
    }

    private void ProcessMessage(string json)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"WS received: {json}");
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();

                if (type == "chat_message" && root.TryGetProperty("message", out var msgElement))
                {
                    // Chat message
                    var message = JsonSerializer.Deserialize(msgElement.GetRawText(), AppJsonContext.Default.ChatMessage);
                    if (message != null)
                    {
                        // Wirft ein Seiten-Handler, darf das nicht den Main-Thread crashen
                        UiSicher.AufMainThread(() => OnChatMessageReceived?.Invoke(message), "WS");
                    }
                }
                else if (type == "pong")
                {
                    // Ping/Pong - ignore
                }
                else
                {
                    // Task update or any other type
                    // Wirft ein Seiten-Handler, darf das nicht den Main-Thread crashen
                    UiSicher.AufMainThread(() => OnTaskUpdate?.Invoke(type ?? "update"), "WS");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessMessage error: {ex.Message}");
        }
    }

    /// <summary>
    /// OnTaskUpdate von außen auslösen, z.B. nach App-Resume: Während die
    /// App im Hintergrund war, war der WebSocket getrennt - Änderungen aus
    /// dieser Zeit kamen nie an. Die Seiten reagieren auf "task_updated" mit
    /// einem kompletten Daten-Reload.
    /// </summary>
    public void NotifyTaskUpdate(string type = "task_updated")
    {
        // Wirft ein Seiten-Handler, darf das nicht den Main-Thread crashen
        UiSicher.AufMainThread(() => OnTaskUpdate?.Invoke(type), "WS");
    }

    /// <summary>
    /// Von aussen (Firestore-Empfang) eine Chat-Nachricht in denselben Event
    /// einspeisen, den auch der WebSocket nutzt. So bleibt die UI unveraendert;
    /// Duplikate (WS + Firestore) fangen die Seiten per Id-Dedup ab.
    /// </summary>
    public void NotifyChatMessage(ChatMessage message)
    {
        if (message == null)
            return;
        UiSicher.AufMainThread(() => OnChatMessageReceived?.Invoke(message), "FS");
    }

    private async Task TryReconnectAsync()
    {
        if (!_shouldReconnect || App.IsInBackground) return;

        // Nur EINE Reconnect-Kette gleichzeitig; der Guard bleibt über den
        // GESAMTEN Backoff-Zyklus (Delay + Verbindungsversuch) gehalten -
        // sonst könnte ein zweiter Auslöser während des Connect-Fensters
        // eine parallele Kette starten.
        if (Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0)
            return;

        try
        {
            while (_shouldReconnect && !App.IsInBackground && _socket?.State != WebSocketState.Open)
            {
                _reconnectAttempts++;
                var delay = Math.Min(InitialReconnectDelay * Math.Pow(2, _reconnectAttempts - 1), MaxReconnectDelay);
                System.Diagnostics.Debug.WriteLine($"WebSocket reconnecting in {delay}ms (attempt {_reconnectAttempts})");

                await Task.Delay((int)delay).ConfigureAwait(false);

                if (!_shouldReconnect || App.IsInBackground)
                    break;

                if (await VerbindeEinmalAsync().ConfigureAwait(false))
                    break;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _reconnecting, 0);
        }

        // Fenster schließen: Stirbt der Socket genau zwischen erfolgreichem
        // Verbinden und dem Freigeben des Guards, wird der TryReconnect des
        // endenden Listeners verworfen. Nach Guard-Freigabe erneut prüfen und
        // ggf. eine neue Kette starten - sonst bliebe die App dauerhaft offline.
        if (_shouldReconnect && !App.IsInBackground && _socket?.State != WebSocketState.Open)
            await TryReconnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Force reconnect WebSocket (e.g., when coming back online)
    /// </summary>
    public async Task ReconnectAsync()
    {
        _shouldReconnect = true;
        _reconnectAttempts = 0;
        await ConnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Process offline queue after coming back online
    /// </summary>
    private async Task ProcessOfflineQueueAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[WebSocket] Processing offline queue...");
            await OfflineQueueService.Instance.ProcessQueueAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebSocket] Error processing offline queue: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect WebSocket
    /// </summary>
    public async Task DisconnectAsync()
    {
        _shouldReconnect = false;
        _cts?.Cancel();

        if (_socket?.State == WebSocketState.Open)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
            }
            catch { }
        }

        _isOnline = false;
        UiSicher.SichererInvoke(() => OnConnectionStatusChanged?.Invoke(false), "WS");
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _socket?.Dispose();
    }
}
