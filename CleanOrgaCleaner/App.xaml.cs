using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using Plugin.Maui.Audio;

namespace CleanOrgaCleaner;

public partial class App : Application
{
    private static App? _instance;
    public static App Instance => _instance!;

    // Store pending message for ChatPage to display
    public static ChatMessage? PendingChatMessage { get; set; }

    // Track if app is in background
    private static bool _isInBackground = false;
    public static bool IsInBackground => _isInBackground;

    public App()
    {
        InitializeComponent();
        _instance = this;

        // Subscribe to chat messages for global notifications
        WebSocketService.Instance.OnChatMessageReceived += OnChatMessageReceived;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Handle app lifecycle events
        window.Stopped += OnAppStopped;
        window.Resumed += OnAppResumed;
        window.Destroying += OnAppDestroying;

        return window;
    }

    /// <summary>
    /// Called when app goes to background (screen off, home button, etc.)
    /// </summary>
    private async void OnAppStopped(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[App] Going to background - disconnecting WebSockets");
        _isInBackground = true;

        try
        {
            // Gracefully disconnect WebSockets to prevent crashes
            await WebSocketService.Instance.DisconnectAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Error disconnecting WebSockets: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when app comes back to foreground
    /// </summary>
    private async void OnAppResumed(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[App] Resuming from background - reconnecting WebSockets");
        _isInBackground = false;

        try
        {
            // Only reconnect if user is logged in
            var isLoggedIn = Preferences.Get("is_logged_in", false);
            if (isLoggedIn)
            {
                await WebSocketService.Instance.ReconnectAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Error reconnecting WebSockets: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when app is being terminated
    /// </summary>
    private void OnAppDestroying(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[App] App destroying - cleaning up");
        WebSocketService.Instance.Dispose();
    }

    private static void Log(string msg)
    {
        var line = $"[APP] {msg}";
        System.Diagnostics.Debug.WriteLine(line);
        _ = Task.Run(() => ApiService.WriteLog(line));
    }

    /// <summary>
    /// Initialize WebSocket connection after successful login
    /// </summary>
    public static async Task InitializeWebSocketAsync()
    {
        Log("InitializeWebSocketAsync START");
        try
        {
            Log("WebSocketService.ConnectAsync START");
            await WebSocketService.Instance.ConnectAsync().ConfigureAwait(false);
            Log("WebSocketService.ConnectAsync DONE");
        }
        catch (Exception ex)
        {
            Log($"InitializeWebSocketAsync ERROR: {ex.Message}");
        }
        Log("InitializeWebSocketAsync END");
    }

    /// <summary>
    /// Play notification sound
    /// </summary>
    private async Task PlayNotificationSoundAsync()
    {
        try
        {
            var audioManager = AudioManager.Current;
            var player = audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("notification.wav"));
            player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Sound error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle incoming chat messages - show popup if not on chat page
    /// </summary>
    private void OnChatMessageReceived(ChatMessage message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Store message for ChatPage
                PendingChatMessage = message;

                // Play notification sound
                await PlayNotificationSoundAsync();

                // Vibrate
                try { Vibration.Vibrate(TimeSpan.FromMilliseconds(200)); } catch { }

                // Check if we're already on the chat page
                var currentPage = Shell.Current?.CurrentPage;
                var pageName = currentPage?.GetType().Name;
                if (pageName == "ChatPage" || pageName == "ChatCurrentPage")
                {
                    // Already on chat page, don't show popup
                    return;
                }

                // Show notification popup
                currentPage = Shell.Current?.CurrentPage;
                if (currentPage == null) return;
                var result = await currentPage.DisplayAlertAsync(
                    $"Neue Nachricht von {message.Sender}",
                    message.Text,
                    "Zum Chat",
                    "Schliessen");

                if (result)
                {
                    // Navigate to chat page
                    if (Shell.Current != null)
                        await Shell.Current.GoToAsync("//MainTabs/ChatPage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Chat popup error: {ex.Message}");
            }
        });
    }
}
