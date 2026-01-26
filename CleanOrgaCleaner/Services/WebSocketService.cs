using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// WebSocket service for real-time updates (chat, tasks)
/// </summary>
public class WebSocketService : IDisposable
{
    private ClientWebSocket? _chatSocket;
    private ClientWebSocket? _taskSocket;
    private CancellationTokenSource? _chatCts;
    private CancellationTokenSource? _taskCts;
    private int _chatReconnectAttempts = 0;
    private int _taskReconnectAttempts = 0;
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
    /// Indicates if the WebSocket connections are currently online
    /// </summary>
    public bool IsOnline => _isOnline;

    /// <summary>
    /// Indicates if chat WebSocket is connected
    /// </summary>
    public bool IsChatConnected => _chatSocket?.State == WebSocketState.Open;

    /// <summary>
    /// Indicates if tasks WebSocket is connected
    /// </summary>
    public bool IsTasksConnected => _taskSocket?.State == WebSocketState.Open;

    private static WebSocketService? _instance;
    public static WebSocketService Instance => _instance ??= new WebSocketService();

    /// <summary>
    /// Connect to chat WebSocket
    /// </summary>
    public async Task ConnectChatAsync()
    {
        if (_chatSocket?.State == WebSocketState.Open)
            return;

        try
        {
            _chatCts?.Cancel();
            _chatCts = new CancellationTokenSource();

            _chatSocket?.Dispose();
            _chatSocket = new ClientWebSocket();

            // Add session cookies for authentication
            var cookieHeader = ApiService.Instance.GetCookieHeader();
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                _chatSocket.Options.SetRequestHeader("Cookie", cookieHeader);
                _chatSocket.Options.SetRequestHeader("Origin", "https://cleanorga.com");
                System.Diagnostics.Debug.WriteLine($"WebSocket using cookies: {cookieHeader}");
            }

            var uri = new Uri($"{WsBaseUrl}/ws/chat/");
            System.Diagnostics.Debug.WriteLine($"Connecting to chat WebSocket: {uri}");

            // Add timeout for iOS - prevent hanging forever
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_chatCts.Token, timeoutCts.Token);

            await _chatSocket.ConnectAsync(uri, linkedCts.Token);

            if (_chatSocket.State == WebSocketState.Open)
            {
                _chatReconnectAttempts = 0;
                var wasOffline = !_isOnline;
                _isOnline = true;
                OnConnectionStatusChanged?.Invoke(true);
                System.Diagnostics.Debug.WriteLine("Chat WebSocket connected");

                // Start listening for messages
                _ = ListenForChatMessagesAsync();

                // Process offline queue if we were offline
                if (wasOffline)
                {
                    _ = ProcessOfflineQueueAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chat WebSocket error: {ex.Message}");
            _isOnline = false;
            OnConnectionStatusChanged?.Invoke(false);
            if (_shouldReconnect)
                await TryReconnectChatAsync();
        }
    }

    /// <summary>
    /// Connect to tasks WebSocket
    /// </summary>
    public async Task ConnectTasksAsync()
    {
        if (_taskSocket?.State == WebSocketState.Open)
            return;

        try
        {
            _taskCts?.Cancel();
            _taskCts = new CancellationTokenSource();

            _taskSocket?.Dispose();
            _taskSocket = new ClientWebSocket();

            // Add session cookies for authentication
            var cookieHeader = ApiService.Instance.GetCookieHeader();
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                _taskSocket.Options.SetRequestHeader("Cookie", cookieHeader);
                _taskSocket.Options.SetRequestHeader("Origin", "https://cleanorga.com");
            }

            var uri = new Uri($"{WsBaseUrl}/ws/tasks/");
            System.Diagnostics.Debug.WriteLine($"Connecting to tasks WebSocket: {uri}");

            // Add timeout for iOS - prevent hanging forever
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_taskCts.Token, timeoutCts.Token);

            await _taskSocket.ConnectAsync(uri, linkedCts.Token);

            if (_taskSocket.State == WebSocketState.Open)
            {
                _taskReconnectAttempts = 0;
                System.Diagnostics.Debug.WriteLine("Tasks WebSocket connected");
                _ = ListenForTaskUpdatesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tasks WebSocket error: {ex.Message}");
            if (_shouldReconnect)
                await TryReconnectTasksAsync();
        }
    }

    private async Task ListenForChatMessagesAsync()
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_chatSocket?.State == WebSocketState.Open && !_chatCts!.Token.IsCancellationRequested)
            {
                var result = await _chatSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _chatCts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    System.Diagnostics.Debug.WriteLine("Chat WebSocket closed by server");
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(text);

                if (result.EndOfMessage)
                {
                    var fullMessage = messageBuilder.ToString();
                    messageBuilder.Clear();
                    ProcessChatMessage(fullMessage);
                }
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Chat listening cancelled");
            return; // Don't try to reconnect on cancellation
        }
        catch (WebSocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chat WebSocket exception: {ex.Message}");
            // Check if app is going to background - if so, don't reconnect
            if (App.IsInBackground) return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chat listen error: {ex.Message}");
            // Check if app is going to background - if so, don't reconnect
            if (App.IsInBackground) return;
        }

        _isOnline = false;
        OnConnectionStatusChanged?.Invoke(false);
        if (_shouldReconnect && !App.IsInBackground)
            await TryReconnectChatAsync();
    }

    private async Task ListenForTaskUpdatesAsync()
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_taskSocket?.State == WebSocketState.Open && !_taskCts!.Token.IsCancellationRequested)
            {
                var result = await _taskSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _taskCts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(text);

                if (result.EndOfMessage)
                {
                    var fullMessage = messageBuilder.ToString();
                    messageBuilder.Clear();
                    ProcessTaskUpdate(fullMessage);
                }
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Task listening cancelled");
            return; // Don't try to reconnect on cancellation
        }
        catch (WebSocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Task WebSocket exception: {ex.Message}");
            // Check if app is going to background - if so, don't reconnect
            if (App.IsInBackground) return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Task listen error: {ex.Message}");
            // Check if app is going to background - if so, don't reconnect
            if (App.IsInBackground) return;
        }

        if (_shouldReconnect && !App.IsInBackground)
            await TryReconnectTasksAsync();
    }

    private void ProcessChatMessage(string json)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Chat WS received: {json}");
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                if (type == "chat_message" && root.TryGetProperty("message", out var msgElement))
                {
                    var message = JsonSerializer.Deserialize<ChatMessage>(msgElement.GetRawText());
                    if (message != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            OnChatMessageReceived?.Invoke(message);
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessChatMessage error: {ex.Message}");
        }
    }

    private void ProcessTaskUpdate(string json)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Task WS received: {json}");
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnTaskUpdate?.Invoke(type ?? "update");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessTaskUpdate error: {ex.Message}");
        }
    }

    private async Task TryReconnectChatAsync()
    {
        // Don't reconnect if in background or shouldReconnect is false
        if (!_shouldReconnect || App.IsInBackground) return;

        _chatReconnectAttempts++;
        var delay = Math.Min(InitialReconnectDelay * Math.Pow(2, _chatReconnectAttempts - 1), MaxReconnectDelay);
        System.Diagnostics.Debug.WriteLine($"Chat reconnecting in {delay}ms (attempt {_chatReconnectAttempts})");

        await Task.Delay((int)delay);

        // Check again after delay
        if (_shouldReconnect && !App.IsInBackground)
            await ConnectChatAsync();
    }

    private async Task TryReconnectTasksAsync()
    {
        // Don't reconnect if in background or shouldReconnect is false
        if (!_shouldReconnect || App.IsInBackground) return;

        _taskReconnectAttempts++;
        var delay = Math.Min(InitialReconnectDelay * Math.Pow(2, _taskReconnectAttempts - 1), MaxReconnectDelay);
        System.Diagnostics.Debug.WriteLine($"Tasks reconnecting in {delay}ms (attempt {_taskReconnectAttempts})");

        await Task.Delay((int)delay);

        // Check again after delay
        if (_shouldReconnect && !App.IsInBackground)
            await ConnectTasksAsync();
    }

    /// <summary>
    /// Force reconnect both WebSockets (e.g., when coming back online)
    /// </summary>
    public async Task ReconnectAsync()
    {
        _shouldReconnect = true;
        _chatReconnectAttempts = 0;
        _taskReconnectAttempts = 0;
        await Task.WhenAll(ConnectChatAsync(), ConnectTasksAsync());
    }

    /// <summary>
    /// Process offline queue after coming back online
    /// </summary>
    private async Task ProcessOfflineQueueAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[WebSocket] Processing offline queue...");
            await OfflineQueueService.Instance.ProcessQueueAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebSocket] Error processing offline queue: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect all WebSockets
    /// </summary>
    public async Task DisconnectAsync()
    {
        _shouldReconnect = false;
        _chatCts?.Cancel();
        _taskCts?.Cancel();

        if (_chatSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _chatSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch { }
        }

        if (_taskSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _taskSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch { }
        }

        _isOnline = false;
        OnConnectionStatusChanged?.Invoke(false);
    }

    public void Dispose()
    {
        _chatCts?.Cancel();
        _taskCts?.Cancel();
        _chatSocket?.Dispose();
        _taskSocket?.Dispose();
    }
}
