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
    private string _partnerAvatar = "";
    private string? _selectedImagePath;
    private string? _selectedImageLocalPath;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("partner"))
        {
            _partnerId = query["partner"]?.ToString() ?? "admin";
            _partnerName = _partnerId == "admin" ? "Admin" : "Kollege";
        }
        if (query.ContainsKey("partnerName"))
        {
            _partnerName = query["partnerName"]?.ToString() ?? _partnerName;
        }
        if (query.ContainsKey("partnerAvatar"))
        {
            _partnerAvatar = query["partnerAvatar"]?.ToString() ?? "";
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
        _ = Header.InitializeAsync();
        Header.SetPageTitle("chat");

        // Update partner header
        UpdatePartnerHeader();

        ApplyTranslations();

        // Load messages (fire-and-forget to not block UI)
        _ = LoadMessagesWithPendingAsync();
    }

    private async Task LoadMessagesWithPendingAsync()
    {
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
        _ = _webSocketService.ConnectChatAsync();
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

        // Show avatar emoji if available, otherwise show initial
        if (!string.IsNullOrEmpty(_partnerAvatar))
        {
            PartnerInitial.Text = _partnerAvatar;
            PartnerInitial.FontSize = 28; // Larger for emoji
        }
        else
        {
            PartnerInitial.Text = _partnerName.Length > 0 ? _partnerName.Substring(0, 1).ToUpper() : "?";
            PartnerInitial.FontSize = 20; // Normal size for letter
        }

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
                // API/WebSocket sendet is_mine korrekt
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

            // API sendet is_mine korrekt basierend auf der Perspektive des Benutzers
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
        var text = MessageEntry.Text?.Trim() ?? "";

        // Mindestens Text oder Bild erforderlich
        if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(_selectedImagePath))
        {
            await DisplayAlertAsync("Hinweis", "Bitte Nachricht eingeben oder Bild auswählen", "OK");
            return;
        }

        SendButton.IsEnabled = false;

        try
        {
            var response = await _apiService.SendChatMessageAsync(text, _partnerId, _selectedImagePath);

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

                // Bild zurücksetzen
                ClearSelectedImage();
            }
            else
            {
                await DisplayAlertAsync("Fehler",
                    response.Error ?? "Nachricht konnte nicht gesendet werden",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
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
            await DisplayAlertAsync("Hinweis", "Bitte Nachricht eingeben", "OK");
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
                await DisplayAlertAsync("Info",
                    response.Message ?? "Keine Uebersetzung noetig",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
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


    /// <summary>
    /// Delete all messages in this chat
    /// </summary>
    private async void OnDeleteChatClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;
        var confirm = await DisplayAlert(
            t("delete_chat_title") ?? "Chat löschen",
            t("delete_chat_confirm") ?? "Alle Nachrichten löschen? Diese Aktion kann nicht rückgängig gemacht werden.",
            t("yes") ?? "Ja",
            t("no") ?? "Nein");

        if (!confirm) return;

        DeleteChatButton.IsEnabled = false;
        try
        {
            var response = await _apiService.DeleteChatMessagesAsync(_partnerId);
            if (response.Success)
            {
                _messages.Clear();
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Nachrichten konnten nicht gelöscht werden", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
        finally
        {
            DeleteChatButton.IsEnabled = true;
        }
    }

    private Task DisplayAlertAsync(string title, string message, string cancel)
    {
        return DisplayAlert(title, message, cancel);
    }

    #region Photo Handling

    private byte[]? _selectedImageBytes;

    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // Zeige Auswahl: Kamera oder Galerie
            var action = await DisplayActionSheet(
                Translations.Get("select_image_source") ?? "Bild auswählen",
                Translations.Get("cancel") ?? "Abbrechen",
                null,
                "📷 " + (Translations.Get("camera") ?? "Kamera"),
                "🖼️ " + (Translations.Get("gallery") ?? "Galerie"));

            if (string.IsNullOrEmpty(action) || action == (Translations.Get("cancel") ?? "Abbrechen"))
                return;

            if (action.Contains("📷") || action.Contains("Kamera") || action.Contains("Camera"))
            {
                await TakePhotoAsync();
            }
            else
            {
                await PickPhotoFromGalleryAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Photo error: {ex.Message}");
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Kamera - identisch mit ImageListDescription Implementation
    /// </summary>
    private async Task TakePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync("Fehler", "Kamera nicht verfügbar", "OK");
                return;
            }

            // Explizite Berechtigung prüfen
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    await OfferOpenSettingsAsync("Kamera");
                    return;
                }
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                // Annotation öffnen
                await OpenAnnotationAndUploadAsync(bytes);
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlertAsync("Fehler", "Kamera wird auf diesem Gerät nicht unterstützt", "OK");
        }
        catch (PermissionException)
        {
            await OfferOpenSettingsAsync("Kamera");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Galerie - verwendet FilePicker wie in ImageListDescription (vermeidet Metadaten-Fehler)
    /// </summary>
    private async Task PickPhotoFromGalleryAsync()
    {
        try
        {
            var options = new PickOptions { PickerTitle = "Foto auswählen", FileTypes = FilePickerFileType.Images };
            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                // Annotation öffnen
                await OpenAnnotationAndUploadAsync(bytes);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Galerie konnte nicht geöffnet werden", "OK");
        }
    }

    /// <summary>
    /// Öffnet Annotation und lädt Bild hoch
    /// </summary>
    private async Task OpenAnnotationAndUploadAsync(byte[] imageBytes)
    {
        try
        {
            // Annotation-Seite öffnen
            var annotationPage = new ImageAnnotationPage(imageBytes);
            await Navigation.PushModalAsync(annotationPage);

            // Warten bis Seite geschlossen wird
            var tcs = new TaskCompletionSource<bool>();
            annotationPage.Disappearing += (s, ev) => tcs.TrySetResult(true);
            await tcs.Task;

            // Annotierte oder originale Bytes verwenden
            var finalBytes = annotationPage.WasSaved && annotationPage.AnnotatedImageBytes != null
                ? annotationPage.AnnotatedImageBytes
                : imageBytes;

            // Hochladen
            var fileName = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            using var uploadStream = new MemoryStream(finalBytes);
            var response = await _apiService.UploadChatImageAsync(uploadStream, fileName);

            if (response.Success && !string.IsNullOrEmpty(response.Path))
            {
                _selectedImagePath = response.Path;
                _selectedImageBytes = finalBytes;

                // Vorschau anzeigen
                PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(finalBytes));
                ImagePreviewContainer.IsVisible = true;
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Bild konnte nicht hochgeladen werden", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Annotation/Upload error: {ex.Message}");
            // Bei Fehler trotzdem hochladen ohne Annotation
            try
            {
                var fileName = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                using var uploadStream = new MemoryStream(imageBytes);
                var response = await _apiService.UploadChatImageAsync(uploadStream, fileName);

                if (response.Success && !string.IsNullOrEmpty(response.Path))
                {
                    _selectedImagePath = response.Path;
                    _selectedImageBytes = imageBytes;
                    PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                    ImagePreviewContainer.IsVisible = true;
                }
            }
            catch
            {
                await DisplayAlertAsync("Fehler", "Bild konnte nicht hochgeladen werden", "OK");
            }
        }
    }

    private async Task OfferOpenSettingsAsync(string permissionName)
    {
        var openSettings = await DisplayAlert($"{permissionName}-Berechtigung",
            $"Die {permissionName}-Berechtigung wurde verweigert.\n\nBitte öffne die App-Einstellungen und aktiviere die Berechtigung.",
            "Einstellungen öffnen", "Abbrechen");
        if (openSettings)
            Services.PermissionHelper.OpenAppSettings();
    }

    private async void OnRemoveImageClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            try
            {
                await _apiService.DeleteChatImageAsync(_selectedImagePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete image error: {ex.Message}");
            }
        }
        ClearSelectedImage();
    }

    private void ClearSelectedImage()
    {
        _selectedImagePath = null;
        _selectedImageLocalPath = null;
        PreviewImage.Source = null;
        ImagePreviewContainer.IsVisible = false;
    }

    private async void OnAnnotateImageClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedImageLocalPath))
        {
            await DisplayAlertAsync("Fehler", "Kein Bild ausgewählt", "OK");
            return;
        }

        try
        {
            AnnotateImageButton.IsEnabled = false;

            // Bild laden
            byte[] imageBytes;
            if (_selectedImageLocalPath.StartsWith("http"))
            {
                // Remote image - download first
                using var httpClient = new HttpClient();
                imageBytes = await httpClient.GetByteArrayAsync(_selectedImageLocalPath);
            }
            else
            {
                // Local file
                imageBytes = await File.ReadAllBytesAsync(_selectedImageLocalPath);
            }

            // Annotation-Seite öffnen
            var annotationPage = new ImageAnnotationPage(imageBytes);
            await Navigation.PushModalAsync(annotationPage);

            // Warten bis Seite geschlossen wird
            var tcs = new TaskCompletionSource<bool>();
            annotationPage.Disappearing += (s, args) => tcs.TrySetResult(true);
            await tcs.Task;

            // Prüfen ob gespeichert wurde
            if (annotationPage.WasSaved && annotationPage.AnnotatedImageBytes != null)
            {
                // Annotiertes Bild hochladen
                using var stream = new MemoryStream(annotationPage.AnnotatedImageBytes);
                var response = await _apiService.UploadChatImageAsync(stream, "annotated.jpg");

                if (response.Success && !string.IsNullOrEmpty(response.Path))
                {
                    // Altes Bild löschen (optional)
                    if (!string.IsNullOrEmpty(_selectedImagePath))
                    {
                        try { await _apiService.DeleteChatImageAsync(_selectedImagePath); }
                        catch { /* ignore */ }
                    }

                    // Neuen Pfad setzen
                    _selectedImagePath = response.Path;

                    // Lokale Vorschau aktualisieren
                    PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(annotationPage.AnnotatedImageBytes));
                }
                else
                {
                    await DisplayAlertAsync("Fehler", response.Error ?? "Annotiertes Bild konnte nicht hochgeladen werden", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Annotation error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Fehler bei der Bildbearbeitung", "OK");
        }
        finally
        {
            AnnotateImageButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Löscht ein Bild aus einer gesendeten Nachricht
    /// </summary>
    private async Task DeleteMessageImageAsync(int messageId)
    {
        var t = Translations.Get;
        var confirm = await DisplayAlert(
            t("delete_image") ?? "Bild löschen",
            t("delete_image_confirm") ?? "Bild aus dieser Nachricht entfernen?",
            t("yes") ?? "Ja",
            t("no") ?? "Nein");

        if (!confirm) return;

        try
        {
            var response = await _apiService.DeleteMessageImageAsync(messageId);
            if (response.Success)
            {
                var message = _messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    // Wenn Nachricht nur Bild hatte (kein Text), komplett entfernen
                    if (string.IsNullOrEmpty(message.Text))
                    {
                        _messages.Remove(message);
                    }
                    else
                    {
                        // Nachricht hatte Text + Bild: nur Bild entfernen
                        var idx = _messages.IndexOf(message);
                        var updatedMessage = new ChatMessage
                        {
                            Id = message.Id,
                            Text = message.Text,
                            TextTranslated = message.TextTranslated,
                            TextOriginal = message.TextOriginal,
                            LinkPhotoVideo = null,
                            Timestamp = message.Timestamp,
                            IsMine = message.IsMine,
                            IsRead = message.IsRead,
                            Sender = message.Sender,
                            SenderName = message.SenderName,
                            CleanerId = message.CleanerId
                        };
                        _messages.RemoveAt(idx);
                        _messages.Insert(idx, updatedMessage);
                    }
                }
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Bild konnte nicht gelöscht werden", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete message image error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Fehler beim Löschen des Bildes", "OK");
        }
    }

    /// <summary>
    /// Event handler für Delete-Button auf Nachrichtenbildern
    /// </summary>
    public async void OnDeleteMessageImageClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int messageId)
        {
            await DeleteMessageImageAsync(messageId);
        }
    }

    #endregion
}
