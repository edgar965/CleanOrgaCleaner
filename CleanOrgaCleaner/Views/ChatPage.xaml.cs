using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Chat page for messaging with admin
/// Supports translations and real-time updates via WebSocket
/// </summary>
public partial class ChatPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly ObservableCollection<ChatMessage> _messages;

    public ChatPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _messages = new ObservableCollection<ChatMessage>();
        MessagesCollection.ItemsSource = _messages;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

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
                _messages.Insert(0, pending);
            }
        }

        // Connect WebSocket for real-time updates
        WebSocketService.Instance.OnChatMessageReceived += OnNewMessageReceived;
        await WebSocketService.Instance.ConnectChatAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        WebSocketService.Instance.OnChatMessageReceived -= OnNewMessageReceived;
    }

    private void OnNewMessageReceived(ChatMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Check if message already exists (avoid duplicates)
            if (!_messages.Any(m => m.Id == message.Id))
            {
                // Add to collection (messages displayed newest first)
                _messages.Insert(0, message);
            }
        });
    }

    private async Task LoadMessagesAsync()
    {
        try
        {
            var messages = await _apiService.GetChatMessagesAsync();
            _messages.Clear();

            // Add messages (newest first)
            foreach (var msg in messages.OrderByDescending(m => m.Id))
            {
                _messages.Add(msg);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadMessages error: {ex.Message}");
        }
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = MessageEditor.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Hinweis", "Bitte Nachricht eingeben", "OK");
            return;
        }

        SendButton.IsEnabled = false;

        try
        {
            var response = await _apiService.SendChatMessageAsync(text);

            if (response.Success && response.Message != null)
            {
                // Add to local collection
                _messages.Insert(0, response.Message);
                MessageEditor.Text = "";
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
        var text = MessageEditor.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Hinweis", "Bitte Nachricht eingeben", "OK");
            return;
        }

        PreviewButton.IsEnabled = false;

        try
        {
            var response = await _apiService.PreviewTranslationAsync(text);

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
}
