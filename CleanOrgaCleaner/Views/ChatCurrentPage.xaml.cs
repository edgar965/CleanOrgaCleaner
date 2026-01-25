using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

public partial class ChatCurrentPage : ContentPage, IQueryAttributable
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly ObservableCollection<ChatMessage> _messages;
    private string _partnerId = "admin";
    private string _partnerName = "Admin";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("partner"))
        {
            _partnerId = query["partner"]?.ToString() ?? "admin";
            _partnerName = _partnerId == "admin" ? "Admin" : "Kollege";
        }
    }

    public ChatCurrentPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
        _messages = new ObservableCollection<ChatMessage>();
        MessagesCollection.ItemsSource = _messages;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Initialize header (handles translations, user info, work status, offline banner)
        await Header.InitializeAsync();
        Header.SetPageTitle("chat");

        // Update partner header
        UpdatePartnerHeader();

        ApplyTranslations();

        if (App.PendingChatMessage != null)
        {
            await Task.Delay(500);
        }

        await LoadMessagesAsync();

        if (App.PendingChatMessage != null)
        {
            var pending = App.PendingChatMessage;
            App.PendingChatMessage = null;

            if (!_messages.Any(m => m.Id == pending.Id))
            {
                _messages.Add(pending);
            }
        }

        _webSocketService.OnChatMessageReceived += OnNewMessageReceived;
        await _webSocketService.ConnectChatAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnChatMessageReceived -= OnNewMessageReceived;
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("chat");
        MessageEntry.Placeholder = t("message_placeholder");

        TranslationPreviewTitle.Text = t("translation_preview");
        YourTextLabel.Text = t("your_text") + ":";
        TranslationForAdminLabel.Text = t("translation_for_admin") + ":";
        BackTranslationLabel.Text = t("back_translation") + ":";
    }

    private void UpdatePartnerHeader()
    {
        PartnerNameLabel.Text = _partnerName;
        PartnerInitial.Text = _partnerName.Length > 0 ? _partnerName.Substring(0, 1).ToUpper() : "?";

        // Set avatar color based on partner type
        if (_partnerId == "admin")
        {
            // Purple gradient for admin
            PartnerAvatar.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#667eea"), 0),
                    new GradientStop(Color.FromArgb("#764ba2"), 1)
                },
                new Point(0, 0),
                new Point(1, 1));
            PartnerStatusLabel.Text = Translations.Get("admin_contact");
        }
        else
        {
            // Green gradient for colleagues
            PartnerAvatar.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#4CAF50"), 0),
                    new GradientStop(Color.FromArgb("#45a049"), 1)
                },
                new Point(0, 0),
                new Point(1, 1));
            PartnerStatusLabel.Text = Translations.Get("colleague");
        }
    }

    private void OnNewMessageReceived(ChatMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_messages.Any(m => m.Id == message.Id))
            {
                // Check if this message is from the current user
                var currentUsername = Preferences.Get("username", "");
                if (!string.IsNullOrEmpty(currentUsername) &&
                    message.Sender?.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) == true)
                {
                    message.FromCurrentUser = true;
                }

                _messages.Add(message);
                MessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End);
            }
        });
    }

    private async Task LoadMessagesAsync()
    {
        try
        {
            var messages = await _apiService.GetChatMessagesAsync(_partnerId);
            _messages.Clear();

            foreach (var msg in messages.OrderBy(m => m.Id))
            {
                _messages.Add(msg);
            }

            if (_messages.Count > 0)
            {
                await Task.Delay(100);
                MessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMessages error: {ex.Message}");
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Hinweis", "Bitte Nachricht eingeben", "OK");
            return;
        }

        SendButton.IsEnabled = false;

        try
        {
            var response = await _apiService.SendChatMessageAsync(text, _partnerId);

            if (response.Success && response.Message != null)
            {
                var existing = _messages.FirstOrDefault(m => m.Id == response.Message.Id);
                if (existing != null)
                {
                    existing.FromCurrentUser = true;
                    var idx = _messages.IndexOf(existing);
                    _messages.RemoveAt(idx);
                    _messages.Insert(idx, existing);
                }
                else
                {
                    response.Message.FromCurrentUser = true;
                    _messages.Add(response.Message);
                }
                MessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End);
                MessageEntry.Text = "";
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
            SendButton.IsEnabled = true;
        }
    }

    private async void OnPreviewClicked(object sender, EventArgs e)
    {
        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Hinweis", "Bitte Nachricht eingeben", "OK");
            return;
        }

        MessageEntry.Unfocus();
        await Task.Delay(300);

        PreviewButton.IsEnabled = false;

        try
        {
            var response = await _apiService.PreviewTranslationAsync(text, _partnerId);

            if (response.Success)
            {
                PreviewOriginalLabel.Text = text;
                PreviewTranslatedLabel.Text = response.Translated ?? text;
                PreviewBackLabel.Text = response.BackTranslated ?? "";

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
            PreviewButton.IsEnabled = true;
        }
    }

    private void OnClosePreviewClicked(object sender, EventArgs e)
    {
        TranslationPreview.IsVisible = false;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainTabs/ChatListPage");
    }
}
