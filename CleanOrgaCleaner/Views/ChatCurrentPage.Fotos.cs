using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Bilder im Gespräch: aufnehmen/auswählen, markieren, hochladen und aus
/// gesendeten Nachrichten entfernen.
/// </summary>
public partial class ChatCurrentPage
{
    /// <summary>Serverpfad des hochgeladenen, noch nicht gesendeten Bildes.</summary>
    private string? _selectedImagePath;

    /// <summary>Die Bytes dazu - für erneutes Markieren ohne neuen Download.</summary>
    private byte[]? _selectedImageBytes;

    private FotoAufnahme Fotos => _fotos ??= new FotoAufnahme(this);
    private FotoAufnahme? _fotos;

    private async void OnPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var t = Translations.Get;
            var kamera = "📷 " + t("camera");
            var galerie = "🖼️ " + t("gallery");

            var wahl = await DisplayActionSheetAsync(
                t("select_image_source"), t("cancel"), null, kamera, galerie);

            if (string.IsNullOrEmpty(wahl) || wahl == t("cancel"))
                return;

            var bytes = wahl == kamera
                ? await Fotos.KameraAsync()
                : await Fotos.GalerieAsync();

            if (bytes == null) return;

            await MarkierenUndHochladenAsync(bytes);
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Foto: {ex.Message}");
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
    }

    /// <summary>Bild markieren lassen und hochladen; Ergebnis als Vorschau zeigen.</summary>
    private async Task MarkierenUndHochladenAsync(byte[] bytes)
    {
        var fertig = await Fotos.MarkierenAsync(bytes);
        await HochladenAsync(fertig);
    }

    private async Task HochladenAsync(byte[] bytes)
    {
        try
        {
            var dateiname = $"chat_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            using var strom = new MemoryStream(bytes);
            var antwort = await _apiService.UploadChatImageAsync(strom, dateiname);

            if (antwort.Success && !string.IsNullOrEmpty(antwort.Path))
            {
                _selectedImagePath = antwort.Path;
                _selectedImageBytes = bytes;
                ZeigeVorschau(bytes);
            }
            else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
            {
                await DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
            }
            else
            {
                await DisplayAlertAsync("Fehler", antwort.Error ?? "Bild konnte nicht hochgeladen werden", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Hochladen: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Bild konnte nicht hochgeladen werden", "OK");
        }
    }

    private void ZeigeVorschau(byte[] bytes)
    {
        PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        ImagePreviewContainer.IsVisible = true;
    }

    private async void OnRemoveImageClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            try { await _apiService.DeleteChatImageAsync(_selectedImagePath); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Bild entfernen: {ex.Message}");
            }
        }
        ClearSelectedImage();
    }

    private void ClearSelectedImage()
    {
        _selectedImagePath = null;
        _selectedImageBytes = null;
        PreviewImage.Source = null;
        ImagePreviewContainer.IsVisible = false;
    }

    /// <summary>
    /// Das schon hochgeladene Bild noch einmal markieren. Die Bytes liegen
    /// lokal vor - vorher wurde dafür ein Pfad ausgewertet, der nie gesetzt
    /// wurde, weshalb der Knopf immer "Kein Bild ausgewählt" meldete.
    /// </summary>
    private async void OnAnnotateImageClicked(object sender, EventArgs e)
    {
        if (_selectedImageBytes == null)
        {
            await DisplayAlertAsync("Fehler", "Kein Bild ausgewählt", "OK");
            return;
        }

        AnnotateImageButton.IsEnabled = false;
        try
        {
            var alterPfad = _selectedImagePath;
            var markiert = await Fotos.MarkierenAsync(_selectedImageBytes);

            // Nichts geändert -> nichts hochladen
            if (ReferenceEquals(markiert, _selectedImageBytes)) return;

            await HochladenAsync(markiert);

            // Erst nach erfolgreichem Austausch das alte Bild wegräumen
            if (!string.IsNullOrEmpty(alterPfad) && _selectedImagePath != alterPfad)
            {
                try { await _apiService.DeleteChatImageAsync(alterPfad); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Altes Bild: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Markieren: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Fehler bei der Bildbearbeitung", "OK");
        }
        finally
        {
            AnnotateImageButton.IsEnabled = true;
        }
    }

    /// <summary>Bild aus einer gesendeten Nachricht entfernen.</summary>
    private async void OnDeleteMessageImageClicked(object sender, EventArgs e)
    {
        if (sender is not Button knopf || knopf.CommandParameter is not int nachrichtId)
            return;

        var t = Translations.Get;
        try
        {
            var bestaetigt = await DisplayAlertAsync(
                t("delete_image"), t("delete_image_confirm"), t("yes"), t("no"));
            if (!bestaetigt) return;

            var antwort = await _apiService.DeleteMessageImageAsync(nachrichtId);
            if (!antwort.Success)
            {
                await DisplayAlertAsync("Fehler", antwort.Error ?? "Bild konnte nicht gelöscht werden", "OK");
                return;
            }

            EntferneBildAusListe(nachrichtId);
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Nachrichtenbild löschen: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Fehler beim Löschen des Bildes", "OK");
        }
    }

    /// <summary>
    /// Nachricht ohne Text verschwindet ganz, sonst bleibt nur der Text übrig.
    /// ChatMessage meldet keine Änderungen - deshalb Ersetzen statt Ändern.
    /// </summary>
    private void EntferneBildAusListe(int nachrichtId)
    {
        var nachricht = _messages.FirstOrDefault(m => m.Id == nachrichtId);
        if (nachricht == null) return;

        if (string.IsNullOrEmpty(nachricht.Text))
        {
            _messages.Remove(nachricht);
            return;
        }

        var stelle = _messages.IndexOf(nachricht);
        _messages.RemoveAt(stelle);
        _messages.Insert(stelle, new ChatMessage
        {
            Id = nachricht.Id,
            Text = nachricht.Text,
            TextTranslated = nachricht.TextTranslated,
            TextOriginal = nachricht.TextOriginal,
            LinkPhotoVideo = null,
            Timestamp = nachricht.Timestamp,
            IsMine = nachricht.IsMine,
            IsRead = nachricht.IsRead,
            Sender = nachricht.Sender,
            SenderName = nachricht.SenderName,
            CleanerId = nachricht.CleanerId,
            FromAdmin = nachricht.FromAdmin
        });
    }
}
