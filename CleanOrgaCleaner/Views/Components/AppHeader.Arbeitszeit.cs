using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Components;

/// <summary>
/// Arbeitszeit-Knopf der Kopfleiste: Start, Beenden und die Rückfrage dazu.
/// </summary>
public partial class AppHeader
{
    private bool _isWorking;

    /// <summary>Läuft die Arbeitszeit gerade?</summary>
    public bool IsWorking => _isWorking;

    /// <summary>Aktuellen Zustand vom Server holen und den Knopf anpassen.</summary>
    public async Task LoadWorkStatusAsync()
    {
        Log("LoadWorkStatusAsync START");
        try
        {
            var status = await _apiService.GetWorkStatusAsync().ConfigureAwait(false);
            Log($"GetWorkStatusAsync DONE: isWorking={status?.IsWorking}");
            if (status != null)
            {
                _isWorking = status.IsWorking;
                UiSicher.AufMainThread(UpdateWorkButton, "AppHeader");
            }
        }
        catch (Exception ex)
        {
            Log($"LoadWorkStatusAsync ERROR: {ex.Message}");
        }
        Log("LoadWorkStatusAsync END");
    }

    private void UpdateWorkButton()
    {
        WorkToggleButton.Text = Translations.Get(_isWorking ? "stop" : "start");
        WorkToggleButton.BackgroundColor = _isWorking
            ? Color.FromArgb("#E91E63")
            : Color.FromArgb("#4CAF50");
    }

    private async void OnWorkToggleClicked(object sender, EventArgs e)
    {
        try
        {
            if (_isWorking)
            {
                // Beenden erst nach Rückfrage
                WorkStopPopup.IsVisible = true;
                return;
            }

            var result = await _apiService.StartWorkAsync();
            if (result.Success)
            {
                _isWorking = true;
                UpdateWorkButton();
            }
            else if (NetworkErrorHelper.IsNetworkError(result.Error))
            {
                await OhneNetzVormerkenAsync(starten: true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppHeader] Work toggle error: {ex.Message}");
            if (NetworkErrorHelper.IsNetworkError(ex.Message) && !_isWorking)
                await OhneNetzVormerkenAsync(starten: true);
            else
                await UiSicher.FehlerAlertAsync();
        }
    }

    private async void OnWorkStopYesClicked(object sender, EventArgs e)
    {
        try
        {
            if (await _apiService.StopWorkAsync())
            {
                _isWorking = false;
                UpdateWorkButton();
            }
            else
            {
                await OhneNetzVormerkenAsync(starten: false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppHeader] Work stop error: {ex.Message}");
            if (NetworkErrorHelper.IsNetworkError(ex.Message))
                await OhneNetzVormerkenAsync(starten: false);
            else
                await UiSicher.FehlerAlertAsync();
        }
        finally
        {
            WorkStopPopup.IsVisible = false;
        }
    }

    /// <summary>
    /// Ohne Verbindung: Aktion für die spätere Übertragung vormerken, den Knopf
    /// trotzdem sofort umstellen und darauf hinweisen.
    /// </summary>
    private async Task OhneNetzVormerkenAsync(bool starten)
    {
        var warteschlange = OfflineQueueService.Instance;
        if (starten)
            await warteschlange.EnqueueWorkStartAsync();
        else
            await warteschlange.EnqueueWorkStopAsync();

        _isWorking = starten;
        UpdateWorkButton();

        await UiSicher.AlertAsync(
            Translations.Get("no_connection"),
            Translations.Get("saved_offline"),
            Translations.Get("ok"));
    }

    // "Nein" = Reinigung wurde NICHT vollständig beendet -> Arbeit läuft weiter,
    // der Server wird nicht angerufen.
    private void OnWorkStopNoClicked(object sender, EventArgs e) => WorkStopPopup.IsVisible = false;

    private void OnWorkStopCancelClicked(object sender, EventArgs e) => WorkStopPopup.IsVisible = false;

    private void OnWorkStopPopupBackgroundTapped(object sender, EventArgs e) => WorkStopPopup.IsVisible = false;
}
