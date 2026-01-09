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
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;
    private const string WsBaseUrl = "wss://cleanorga.com";

    // Events for UI updates
    public event Action<ChatMessage>? OnChatMessageReceived;
    public event Action<string>? OnTaskUpdate;
    public event Action<bool>? OnConnectionStatusChanged;

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

            await _chatSocket.ConnectAsync(uri, _chatCts.Token);

            if (_chatSocket.State == WebSocketState.Open)
            {
                _reconnectAttempts = 0;
                OnConnectionStatusChanged?.Invoke(true);
                System.Diagnostics.Debug.WriteLine("Chat WebSocket connected");

                // Start listening for messages
                _ = ListenForChatMessagesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chat WebSocket error: {ex.Message}");
            OnConnectionStatusChanged?.Invoke(false);
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

            await _taskSocket.ConnectAsync(uri, _taskCts.Token);

            if (_taskSocket.State == WebSocketState.Open)
            {
                System.Diagnostics.Debug.WriteLine("Tasks WebSocket connected");
                _ = ListenForTaskUpdatesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tasks WebSocket error: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chat listen error: {ex.Message}");
        }

        OnConnectionStatusChanged?.Invoke(false);
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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Task listen error: {ex.Message}");
        }
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
        if (_reconnectAttempts >= MaxReconnectAttempts)
        {
            System.Diagnostics.Debug.WriteLine("Max reconnect attempts reached");
            return;
        }

        _reconnectAttempts++;
        var delay = Math.Min(1000 * Math.Pow(2, _reconnectAttempts), 30000);
        System.Diagnostics.Debug.WriteLine($"Reconnecting in {delay}ms (attempt {_reconnectAttempts})");

        await Task.Delay((int)delay);
        await ConnectChatAsync();
    }

    /// <summary>
    /// Disconnect all WebSockets
    /// </summary>
    public async Task DisconnectAsync()
    {
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
