using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner;

public partial class App : Application
{
    private static App? _instance;
    public static App Instance => _instance!;

    // Store pending message for ChatCurrentPage to display
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

            // Warm up TTS engine in background (Android needs initialization time)
            _ = Task.Run(async () =>
            {
                try
                {
                    await TextToSpeech.Default.SpeakAsync(" ", new SpeechOptions { Volume = 0 });
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Log($"InitializeWebSocketAsync ERROR: {ex.Message}");
        }
        Log("InitializeWebSocketAsync END");
    }

    /// <summary>
    /// Play notification feedback (haptic + optional TTS)
    /// </summary>
    private async Task PlayNotificationSoundAsync(string? messageToSpeak = null)
    {
        try
        {
            // Haptic feedback
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

            // Text-to-Speech if message provided (uses MAUI built-in, no background mode needed)
            if (!string.IsNullOrEmpty(messageToSpeak) && TtsEnabled)
            {
                await SpeakTextAsync(messageToSpeak);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Notification error: {ex.Message}");
        }
    }

    /// <summary>
    /// TTS enabled setting
    /// </summary>
    public static bool TtsEnabled { get; set; } = true;

    /// <summary>
    /// Speak text using MAUI built-in TextToSpeech (no background audio mode required)
    /// Uses the user's selected language for speech
    /// </summary>
    public static async Task SpeakTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            // Cancel any ongoing speech
            CancelSpeech();
            _speechCancellation = new CancellationTokenSource();

            // Get user's language preference
            var userLang = Preferences.Get("language", "de");

            // Map language codes to locale codes for TTS
            var localeCode = userLang switch
            {
                "en" => "en-US",
                "es" => "es-ES",
                "ro" => "ro-RO",
                "pl" => "pl-PL",
                "ru" => "ru-RU",
                "uk" => "uk-UA",
                "vi" => "vi-VN",
                _ => "de-DE"
            };

            // Try to find matching locale
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var matchingLocale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(userLang, StringComparison.OrdinalIgnoreCase));

            var options = new SpeechOptions();
            if (matchingLocale != null)
            {
                options.Locale = matchingLocale;
            }

            await TextToSpeech.Default.SpeakAsync(text, options, _speechCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Speech was cancelled - ok
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS] Error: {ex.Message}");
        }
    }

    private static CancellationTokenSource? _speechCancellation;

    /// <summary>
    /// Cancel ongoing speech
    /// </summary>
    public static void CancelSpeech()
    {
        try
        {
            _speechCancellation?.Cancel();
            _speechCancellation?.Dispose();
            _speechCancellation = null;
        }
        catch { }
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
                // Check if this is our own message - don't read aloud or notify
                var currentUsername = Preferences.Get("username", "");
                var isOwnMessage = !string.IsNullOrEmpty(currentUsername) &&
                    message.Sender?.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) == true;

                // Store message for ChatCurrentPage
                PendingChatMessage = message;

                // Only play TTS for incoming messages (not our own)
                if (!isOwnMessage)
                {
                    // Play notification with TTS (reads message aloud)
                    // Use DisplayText if available (translated), otherwise Text
                    var messageText = !string.IsNullOrEmpty(message.DisplayText) ? message.DisplayText : message.Text;
                    var currentLang = Translations.CurrentLanguage;
                    var prefLang = Preferences.Get("language", "de");
                    var messageFrom = Translations.Get("message_from");
                    // API sendet sender_name, nicht sender
                    var senderName = !string.IsNullOrEmpty(message.SenderName) ? message.SenderName : "Admin";
                    System.Diagnostics.Debug.WriteLine($"[TTS-DEBUG] CurrentLanguage={currentLang}, Preferences.language={prefLang}, messageFrom='{messageFrom}', senderName='{senderName}'");
                    var ttsText = $"{messageFrom} {senderName}: {messageText}";

                    // Start TTS in background (fire and forget - don't block UI)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SpeakTextAsync(ttsText);
                        }
                        catch { }
                    });

                    // Small delay to let TTS start before potential popup
                    await Task.Delay(200);
                    // Haptic feedback
                    try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

                    // Vibrate
                    try { Vibration.Vibrate(TimeSpan.FromMilliseconds(200)); } catch { }

                    // Check if we're already on the chat page with same partner
                    var currentPage = Shell.Current?.CurrentPage;
                    var pageName = currentPage?.GetType().Name;
                    if (pageName == "ChatCurrentPage")
                    {
                        // Already on chat page - new messages are handled by ChatCurrentPage's OnNewMessageReceived
                        return;
                    }

                    // Auto-navigate to chat (no popup)
                    if (Shell.Current != null)
                    {
                        // CleanerId ist null wenn Admin gesendet hat, sonst die Cleaner-ID
                        var partnerId = message.CleanerId?.ToString() ?? "admin";
                        var partnerNameEncoded = Uri.EscapeDataString(senderName);
                        await Shell.Current.GoToAsync($"ChatCurrentPage?partner={partnerId}&partnerName={partnerNameEncoded}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Chat popup error: {ex.Message}");
            }
        });
    }
}
