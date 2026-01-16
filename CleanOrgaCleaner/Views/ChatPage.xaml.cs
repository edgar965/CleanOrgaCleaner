using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Chat page for messaging with admin
/// Supports translations and real-time updates via WebSocket
/// Platform-specific layouts: Grid for iOS, VerticalStackLayout for Android
/// </summary>
public partial class ChatPage : ContentPage, IQueryAttributable
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly ObservableCollection<ChatMessage> _messages;
    private string _partnerId = "admin";
    private string _partnerName = "Admin";

    // Platform-specific element access
    private CollectionView CurrentMessagesCollection => DeviceInfo.Platform == DevicePlatform.iOS
        ? MessagesCollectionIOS : MessagesCollection;

    private Entry CurrentMessageEntry => DeviceInfo.Platform == DevicePlatform.iOS
        ? MessageEntryIOS : MessageEntry;

    private Button CurrentSendButton => DeviceInfo.Platform == DevicePlatform.iOS
        ? SendButtonIOS : SendButton;

    private Button CurrentPreviewButton => DeviceInfo.Platform == DevicePlatform.iOS
        ? PreviewButtonIOS : PreviewButton;

    private Frame CurrentOfflineBanner => DeviceInfo.Platform == DevicePlatform.iOS
        ? OfflineBannerIOS : OfflineBanner;

    private ActivityIndicator CurrentOfflineSpinner => DeviceInfo.Platform == DevicePlatform.iOS
        ? OfflineSpinnerIOS : OfflineSpinner;

    private Button CurrentMenuButton => DeviceInfo.Platform == DevicePlatform.iOS
        ? MenuButtonIOS : MenuButton;

    // Translation Preview Labels - Platform specific
    private Label CurrentPreviewOriginalLabel => DeviceInfo.Platform == DevicePlatform.iOS
        ? PreviewOriginalLabelIOS : PreviewOriginalLabel;

    private Label CurrentPreviewTranslatedLabel => DeviceInfo.Platform == DevicePlatform.iOS
        ? PreviewTranslatedLabelIOS : PreviewTranslatedLabel;

    private Label CurrentPreviewBackLabel => DeviceInfo.Platform == DevicePlatform.iOS
        ? PreviewBackLabelIOS : PreviewBackLabel;

    private Label CurrentTranslationPreviewTitle => DeviceInfo.Platform == DevicePlatform.iOS
        ? TranslationPreviewTitleIOS : TranslationPreviewTitle;

    private Label CurrentYourTextLabel => DeviceInfo.Platform == DevicePlatform.iOS
        ? YourTextLabelIOS : YourTextLabel;

    private Label CurrentTranslationForAdminLabel => DeviceInfo.Platform == DevicePlatform.iOS
        ? TranslationForAdminLabelIOS : TranslationForAdminLabel;

    private Label CurrentBackTranslationLabel => DeviceInfo.Platform == DevicePlatform.iOS
        ? BackTranslationLabelIOS : BackTranslationLabel;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("partner"))
        {
            _partnerId = query["partner"]?.ToString() ?? "admin";
            _partnerName = _partnerId == "admin" ? "Admin" : "Kollege";
        }
    }

    public ChatPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
        _messages = new ObservableCollection<ChatMessage>();

        // Set ItemsSource for both collections (only the visible one matters)
        MessagesCollection.ItemsSource = _messages;
        MessagesCollectionIOS.ItemsSource = _messages;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;
        UpdateOfflineBanner(!_webSocketService.IsOnline);

        ApplyTranslations();

        // Check for pending message from notification
        if (App.PendingChatMessage != null)
        {
            // Small delay to ensure message is saved on server
            await Task.Delay(500);
        }

        // Load existing messages
        await LoadMessagesAsync();

        // Add pending message if not already in list
        if (App.PendingChatMessage != null)
        {
            var pending = App.PendingChatMessage;
            App.PendingChatMessage = null;

            // Check if message is already in the list
            if (!_messages.Any(m => m.Id == pending.Id))
            {
                _messages.Add(pending);
            }
        }

        // Connect WebSocket for real-time updates
        _webSocketService.OnChatMessageReceived += OnNewMessageReceived;
        await _webSocketService.ConnectChatAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnChatMessageReceived -= OnNewMessageReceived;
        _webSocketService.OnConnectionStatusChanged -= OnConnectionStatusChanged;
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateOfflineBanner(!isConnected);
        });
    }

    private void UpdateOfflineBanner(bool showOffline)
    {
        CurrentOfflineBanner.IsVisible = showOffline;
        CurrentOfflineSpinner.IsRunning = showOffline;
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("chat");

        // Apply to both platforms (only visible one matters)
        MessageEntry.Placeholder = t("message_placeholder");
        MessageEntryIOS.Placeholder = t("message_placeholder");
        MenuButton.Text = t("chat");
        MenuButtonIOS.Text = t("chat");

        // Menu translations
        MenuTodayButton.Text = $"🏠 {t("today")}";
        MenuChatButton.Text = $"💬 {t("chat")}";
        MenuSettingsButton.Text = $"⚙️ {t("settings")}";

        // Translation Preview translations - both platforms
        TranslationPreviewTitle.Text = t("translation_preview");
        TranslationPreviewTitleIOS.Text = t("translation_preview");
        YourTextLabel.Text = t("your_text") + ":";
        YourTextLabelIOS.Text = t("your_text") + ":";
        TranslationForAdminLabel.Text = t("translation_for_admin") + ":";
        TranslationForAdminLabelIOS.Text = t("translation_for_admin") + ":";
        BackTranslationLabel.Text = t("back_translation") + ":";
        BackTranslationLabelIOS.Text = t("back_translation") + ":";
    }

    // Menu handling
    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private async void OnMenuTodayClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//TodayPage");
    }

    private void OnMenuChatClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        // Already on chat
    }

    private async void OnMenuMyTasksClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MyTasksPage");
    }

    private async void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//SettingsPage");
    }

    private void OnNewMessageReceived(ChatMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Check if message already exists (avoid duplicates)
            if (!_messages.Any(m => m.Id == message.Id))
            {
                // Add to end and scroll to bottom
                _messages.Add(message);
                CurrentMessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End);
            }
        });
    }

    private async Task LoadMessagesAsync()
    {
        try
        {
            var messages = await _apiService.GetChatMessagesAsync(_partnerId);
            _messages.Clear();

            // Add messages (oldest first, so newest appears at bottom near input)
            foreach (var msg in messages.OrderBy(m => m.Id))
            {
                _messages.Add(msg);
            }

            // Scroll to bottom to show latest messages
            if (_messages.Count > 0)
            {
                await Task.Delay(100); // Let UI render
                CurrentMessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMessages error: {ex.Message}");
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = CurrentMessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Hinweis", "Bitte Nachricht eingeben", "OK");
            return;
        }

        CurrentSendButton.IsEnabled = false;

        try
        {
            var response = await _apiService.SendChatMessageAsync(text, _partnerId);

            if (response.Success && response.Message != null)
            {
                // Check if message already added via WebSocket
                var existing = _messages.FirstOrDefault(m => m.Id == response.Message.Id);
                if (existing != null)
                {
                    // Update FromCurrentUser (WebSocket might have set it wrong)
                    existing.FromCurrentUser = true;
                    // Force UI refresh
                    var idx = _messages.IndexOf(existing);
                    _messages.RemoveAt(idx);
                    _messages.Insert(idx, existing);
                }
                else
                {
                    response.Message.FromCurrentUser = true;
                    _messages.Add(response.Message);
                }
                CurrentMessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End);
                CurrentMessageEntry.Text = "";
            }
            else
            {
                await DisplayAlert("Fehler",
                    response.Error ?? "Nachricht konnte nicht gesendet werden",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            CurrentSendButton.IsEnabled = true;
        }
    }

    private async void OnPreviewClicked(object sender, EventArgs e)
    {
        var text = CurrentMessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Hinweis", "Bitte Nachricht eingeben", "OK");
            return;
        }

        // Tastatur schließen bevor Popup erscheint
        CurrentMessageEntry.Unfocus();
        await Task.Delay(300); // Warten bis Tastatur geschlossen

        CurrentPreviewButton.IsEnabled = false;

        try
        {
            var response = await _apiService.PreviewTranslationAsync(text, _partnerId);

            if (response.Success)
            {
                // Set text on both platforms (only visible one matters)
                PreviewOriginalLabel.Text = text;
                PreviewOriginalLabelIOS.Text = text;
                PreviewTranslatedLabel.Text = response.Translated ?? text;
                PreviewTranslatedLabelIOS.Text = response.Translated ?? text;
                PreviewBackLabel.Text = response.BackTranslated ?? "";
                PreviewBackLabelIOS.Text = response.BackTranslated ?? "";

                TranslationPreview.IsVisible = true;
            }
            else
            {
                await DisplayAlert("Info",
                    response.Message ?? "Keine Uebersetzung noetig",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
        }
        finally
        {
            CurrentPreviewButton.IsEnabled = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainTabs/ChatListPage");
    }

    private void OnReadAloudClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string text)
        {
            SpeakText(text);
        }
    }

    private async void SpeakText(string text)
    {
        try
        {
            var settings = new SpeechOptions
            {
                Pitch = 1.0f,
                Volume = 1.0f
            };

            // Get language from preferences
            var lang = Preferences.Get("language", "de");

            await TextToSpeech.Default.SpeakAsync(text, settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
        }
    }

    private void OnClosePreviewClicked(object sender, EventArgs e)
    {
        TranslationPreview.IsVisible = false;
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            Translations.Get("logout"),
            Translations.Get("really_logout"),
            Translations.Get("yes"),
            Translations.Get("no"));

        if (!confirm)
            return;

        try
        {
            await _apiService.LogoutAsync();
        }
        catch
        {
            // Ignore errors - we're logging out anyway
        }

        // Clear stored credentials
        Preferences.Remove("property_id");
        Preferences.Remove("username");
        Preferences.Remove("language");
        Preferences.Remove("is_logged_in");
        Preferences.Remove("remember_me");
        Preferences.Remove("biometric_login_enabled");

        // Clear secure storage
        SecureStorage.Remove("password");

        // Disconnect WebSocket
        WebSocketService.Instance.Dispose();

        // Navigate to login page
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
