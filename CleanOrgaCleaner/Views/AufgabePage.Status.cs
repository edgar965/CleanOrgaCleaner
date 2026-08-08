using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Start/Beenden einer Aufgabe samt Rückfrage beim Abschließen.
/// </summary>
public partial class AufgabePage
{
    private const string ZustandOffen = "not_started";
    private const string ZustandLaeuft = "started";
    private const string ZustandFertig = "completed";

    private void AktualisiereStartStopKnopf()
    {
        if (_task == null) return;
        var t = Translations.Get;

        switch (_task.StateCompleted)
        {
            case ZustandOffen:
                StartStopButton.Text = t("start");
                StartStopButton.BackgroundColor = Color.FromArgb("#9e9e9e");
                break;
            case ZustandLaeuft:
                StartStopButton.Text = t("stop");
                StartStopButton.BackgroundColor = Color.FromArgb("#2196F3");
                break;
            case ZustandFertig:
                StartStopButton.Text = t("completed");
                StartStopButton.BackgroundColor = Color.FromArgb("#4CAF50");
                break;
            default:
                return;
        }

        StartStopButton.IsEnabled = true;
    }

    private async void OnStartStopClicked(object sender, EventArgs e)
    {
        if (_task == null) return;

        // Läuft die Aufgabe, erst nachfragen, ob wirklich abgeschlossen wird
        if (_task.StateCompleted == ZustandLaeuft)
        {
            CompleteTaskPopupOverlay.IsVisible = true;
            return;
        }

        var neuerZustand = _task.StateCompleted switch
        {
            ZustandOffen => ZustandLaeuft,
            ZustandFertig => ZustandOffen,
            _ => ""
        };
        if (string.IsNullOrEmpty(neuerZustand)) return;

        await ZustandSetzenAsync(neuerZustand, zurueckNachErfolg: false);
    }

    private void OnCompleteTaskPopupBackgroundTapped(object sender, EventArgs e)
        => CompleteTaskPopupOverlay.IsVisible = false;

    private void OnCancelCompleteTaskClicked(object sender, EventArgs e)
        => CompleteTaskPopupOverlay.IsVisible = false;

    private async void OnConfirmCompleteTaskClicked(object sender, EventArgs e)
    {
        CompleteTaskPopupOverlay.IsVisible = false;
        await ZustandSetzenAsync(ZustandFertig, zurueckNachErfolg: true);
    }

    /// <summary>
    /// Neuen Zustand setzen - über den Server oder, ohne Verbindung,
    /// vorgemerkt für später.
    /// </summary>
    /// <param name="zurueckNachErfolg">
    /// true beim Abschließen: danach zurück zur Tagesliste. Der Knopf wird dann
    /// NICHT wieder freigegeben - ein Zugriff auf die bereits verlassene Seite
    /// löst auf iOS einen Layout-Pass auf abgebauten Views aus.
    /// </param>
    private async Task ZustandSetzenAsync(string neuerZustand, bool zurueckNachErfolg)
    {
        if (_task == null) return;

        StartStopButton.IsEnabled = false;
        bool verlassen = false;

        try
        {
            var antwort = await _apiService.UpdateTaskStateAsync(_taskId, neuerZustand);

            if (antwort.Success)
            {
                _task.StateCompleted = antwort.NewState ?? neuerZustand;
                AktualisiereStartStopKnopf();
                verlassen = zurueckNachErfolg;
            }
            else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
            {
                await OhneNetzVormerkenAsync(neuerZustand);
                verlassen = zurueckNachErfolg;
            }
            else
            {
                await DisplayAlertAsync(Translations.Get("error"),
                    antwort.Error ?? "Status konnte nicht geändert werden",
                    Translations.Get("ok"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Zustand setzen: {ex.Message}");

            if (NetworkErrorHelper.IsNetworkError(ex.Message))
            {
                await OhneNetzVormerkenAsync(neuerZustand);
                verlassen = zurueckNachErfolg;
            }
            else
            {
                await DisplayAlertAsync(
                    Translations.Get("error"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
            }
        }
        finally
        {
            if (!verlassen)
                StartStopButton.IsEnabled = true;
        }

        if (verlassen)
        {
            // Kurz warten, damit die Anzeige den neuen Zustand noch zeigt
            await Task.Delay(150);
            await ZurueckAsync();
        }
    }

    /// <summary>Ohne Verbindung: Zustandswechsel vormerken und sofort anzeigen.</summary>
    private async Task OhneNetzVormerkenAsync(string neuerZustand)
    {
        await OfflineQueueService.Instance.EnqueueTaskStateChangeAsync(_taskId, neuerZustand);

        if (_task != null)
        {
            _task.StateCompleted = neuerZustand;
            AktualisiereStartStopKnopf();
        }

        await DisplayAlertAsync(
            Translations.Get("no_connection"),
            Translations.Get("saved_offline"),
            Translations.Get("ok"));
    }
}
