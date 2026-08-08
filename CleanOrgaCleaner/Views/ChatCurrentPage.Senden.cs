using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Senden, Übersetzungsvorschau und Löschen eines Gesprächs.
/// </summary>
public partial class ChatCurrentPage
{
    private async void OnSendClicked(object sender, EventArgs e)
    {
        var text = MessageEntry.Text?.Trim() ?? "";

        // Mindestens Text oder Bild erforderlich
        if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(_selectedImagePath))
        {
            await DisplayAlertAsync("Hinweis", "Bitte Nachricht eingeben oder Bild auswählen", "OK");
            return;
        }

        await TastaturSchliessenAsync();
        SendButton.IsEnabled = false;

        try
        {
            var antwort = await _apiService.SendChatMessageAsync(text, _partnerId, _selectedImagePath);

            if (antwort.Success && antwort.Message != null)
            {
                UebernehmeGesendete(antwort.Message);
                MessageEntry.Text = "";
                ClearSelectedImage();
            }
            else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
            {
                await OhneNetzVormerkenAsync(text);
            }
            else
            {
                await DisplayAlertAsync("Fehler",
                    antwort.Error ?? "Nachricht konnte nicht gesendet werden", "OK");
            }
        }
        catch (Exception ex)
        {
            if (NetworkErrorHelper.IsNetworkError(ex.Message))
                await OhneNetzVormerkenAsync(text);
            else
                await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Tastatur schließen. Unfocus() allein schließt die Android-Tastatur NICHT
    /// zuverlässig -> zusätzlich HideSoftInputAsync (per Appium-Test UI01
    /// verifiziert). Sonst bleibt sie nach dem Senden offen.
    /// </summary>
    private async Task TastaturSchliessenAsync()
    {
        MessageEntry.Unfocus();
        try { await MessageEntry.HideSoftInputAsync(CancellationToken.None); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Tastatur: {ex.Message}");
        }
    }

    /// <summary>
    /// Die vom Server bestätigte Nachricht in die Liste bringen. Kam sie über
    /// die Verbindung schon an, wird sie ersetzt - ChatMessage meldet keine
    /// Änderungen, deshalb Entfernen und wieder Einfügen.
    /// </summary>
    private void UebernehmeGesendete(ChatMessage gesendet)
    {
        var vorhanden = _messages.FirstOrDefault(m => m.Id == gesendet.Id);
        if (vorhanden != null)
        {
            vorhanden.FromCurrentUser = true;
            var stelle = _messages.IndexOf(vorhanden);
            _messages.RemoveAt(stelle);
            _messages.Insert(stelle, vorhanden);
        }
        else
        {
            gesendet.FromCurrentUser = true;
            _messages.Add(gesendet);
        }

        ScrolleAnsEnde();
    }

    /// <summary>
    /// Ohne Verbindung: reine Textnachrichten vormerken und sofort anzeigen.
    /// Bilder lassen sich nicht vormerken - dann nur der Hinweis.
    /// </summary>
    private async Task OhneNetzVormerkenAsync(string text)
    {
        if (string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(_selectedImagePath))
        {
            await DisplayAlertAsync(
                Translations.Get("no_connection"),
                Translations.Get("network_error_hint"),
                Translations.Get("ok"));
            return;
        }

        await OfflineQueueService.Instance.EnqueueChatMessageAsync(text, _partnerId);

        // Vorläufiger Eintrag mit negativer Id, damit er nicht mit einer
        // Server-Nachricht kollidiert
        _messages.Add(new ChatMessage
        {
            Id = -Random.Shared.Next(1, int.MaxValue),
            Text = text,
            Timestamp = DateTime.Now,
            FromCurrentUser = true
        });
        ScrolleAnsEnde();
        MessageEntry.Text = "";

        await DisplayAlertAsync(
            Translations.Get("no_connection"),
            Translations.Get("saved_offline"),
            Translations.Get("ok"));
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
            var antwort = await _apiService.PreviewTranslationAsync(text, _partnerId);
            if (antwort.Success)
            {
                PreviewOriginalLabel.Text = text;
                PreviewTranslatedLabel.Text = antwort.Translated ?? text;
                PreviewBackLabel.Text = antwort.BackTranslated ?? "";
                TranslationPreview.IsVisible = true;
            }
            else
            {
                await DisplayAlertAsync("Info", antwort.Message ?? "Keine Übersetzung nötig", "OK");
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

    private void OnClosePreviewClicked(object sender, EventArgs e) => TranslationPreview.IsVisible = false;

    /// <summary>Alle Nachrichten dieses Gesprächs löschen.</summary>
    private async void OnDeleteChatClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;

        DeleteChatButton.IsEnabled = false;
        try
        {
            var bestaetigt = await DisplayAlertAsync(
                t("delete_chat_title"), t("delete_chat_confirm"), t("yes"), t("no"));
            if (!bestaetigt) return;

            var antwort = await _apiService.DeleteChatMessagesAsync(_partnerId);
            if (antwort.Success)
                _messages.Clear();
            else
                await DisplayAlertAsync("Fehler", antwort.Error ?? "Nachrichten konnten nicht gelöscht werden", "OK");
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
        finally
        {
            DeleteChatButton.IsEnabled = true;
        }
    }
}
