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

    public App()
    {
        InitializeComponent();
        _instance = this;

        // Subscribe to chat messages for global notifications
        WebSocketService.Instance.OnChatMessageReceived += OnChatMessageReceived;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use AppShell for navigation
        return new Window(new AppShell());
    }

    /// <summary>
    /// Initialize WebSocket connection after successful login
    /// </summary>
    public static async Task InitializeWebSocketAsync()
    {
        try
        {
            await WebSocketService.Instance.ConnectChatAsync();
            await WebSocketService.Instance.ConnectTasksAsync();
            System.Diagnostics.Debug.WriteLine("[App] WebSocket connected");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] WebSocket error: {ex.Message}");
        }
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
                if (currentPage?.GetType().Name == "ChatPage")
                {
                    // Already on chat page, don't show popup
                    return;
                }

                // Show notification popup
                var result = await Shell.Current.CurrentPage.DisplayAlert(
                    $"Neue Nachricht von {message.Sender}",
                    message.Text,
                    "Zum Chat",
                    "Schliessen");

                if (result)
                {
                    // Navigate to chat page
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
