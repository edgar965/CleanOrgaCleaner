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
    /// Connect to the unified WebSocket endpoint (/ws/main/)
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_socket?.State == WebSocketState.Open)
            return;

        try
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _socket?.Dispose();
            _socket = new ClientWebSocket();

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
                _reconnectAttempts = 0;
                var wasOffline = !_isOnline;
                _isOnline = true;
                OnConnectionStatusChanged?.Invoke(true);
                System.Diagnostics.Debug.WriteLine("WebSocket connected (unified)");

                // Start listening for messages
                _ = ListenForMessagesAsync();

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
            OnConnectionStatusChanged?.Invoke(false);
            if (_shouldReconnect)
                await TryReconnectAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Connect to chat WebSocket (backward-compatible - calls ConnectAsync)
    /// </summary>
    public Task ConnectChatAsync() => ConnectAsync();

    /// <summary>
    /// Connect to tasks WebSocket (backward-compatible - calls ConnectAsync)
    /// </summary>
    public Task ConnectTasksAsync() => ConnectAsync();

    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_socket?.State == WebSocketState.Open && !_cts!.Token.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _cts.Token).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    System.Diagnostics.Debug.WriteLine("WebSocket closed by server");
                    break;
                }

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

        _isOnline = false;
        OnConnectionStatusChanged?.Invoke(false);
        if (_shouldReconnect && !App.IsInBackground)
            await TryReconnectAsync().ConfigureAwait(false);
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
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            OnChatMessageReceived?.Invoke(message);
                        });
                    }
                }
                else if (type == "pong")
                {
                    // Ping/Pong - ignore
                }
                else
                {
                    // Task update or any other type
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnTaskUpdate?.Invoke(type ?? "update");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessMessage error: {ex.Message}");
        }
    }

    private async Task TryReconnectAsync()
    {
        if (!_shouldReconnect || App.IsInBackground) return;

        _reconnectAttempts++;
        var delay = Math.Min(InitialReconnectDelay * Math.Pow(2, _reconnectAttempts - 1), MaxReconnectDelay);
        System.Diagnostics.Debug.WriteLine($"WebSocket reconnecting in {delay}ms (attempt {_reconnectAttempts})");

        await Task.Delay((int)delay).ConfigureAwait(false);

        if (_shouldReconnect && !App.IsInBackground)
            await ConnectAsync().ConfigureAwait(false);
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
        OnConnectionStatusChanged?.Invoke(false);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _socket?.Dispose();
    }
}
