using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Push-Mitteilungen in den Einstellungen: Zustand anzeigen, an- und abschalten.
/// </summary>
public partial class SettingsPage
{
    // Verhindert, dass das programmatische Setzen des Schalters (Statusanzeige)
    // den Toggled-Handler auslöst.
    private bool _mitteilungenSetzenLaeuft;

    private void SetzeMitteilungsSchalter(bool an, string status, bool fehler = false)
    {
        _mitteilungenSetzenLaeuft = true;
        NotificationsSwitch.IsToggled = an;
        _mitteilungenSetzenLaeuft = false;

        NotificationsStatusLabel.Text = status;
        NotificationsStatusLabel.TextColor = fehler
            ? Color.FromArgb("#d32f2f")
            : (an ? Color.FromArgb("#00a884") : Color.FromArgb("#888"));
    }

    /// <summary>Zeigt beim Öffnen den aktuellen Mitteilungs-Zustand an.</summary>
    private async Task AktualisiereMitteilungsZustandAsync()
    {
        try
        {
            var erlaubt = await PushService.IstErlaubtAsync();       // null = unbekannt (iOS)
            var registriert = Preferences.Get("push_registered", false);
            bool an = erlaubt ?? registriert;
            var t = Translations.Get;
            SetzeMitteilungsSchalter(an, an ? t("enabled") : t("not_enabled"));
        }
        catch (Exception ex)
        {
            // Reine Statusanzeige - ein Fehler darf die Seite nicht aufhalten
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Mitteilungs-Zustand: {ex.Message}");
        }
    }

    private async void OnNotificationsToggled(object? sender, ToggledEventArgs e)
    {
        if (_mitteilungenSetzenLaeuft)
            return;

        var t = Translations.Get;
        try
        {
            if (e.Value)
                await MitteilungenEinschaltenAsync(t);
            else
                await MitteilungenAusschaltenAsync(t);
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] Mitteilungen umschalten: {ex.Message}");
            SetzeMitteilungsSchalter(false, t("not_active"), fehler: true);
        }
    }

    private async Task MitteilungenEinschaltenAsync(Func<string, string> t)
    {
        NotificationsStatusLabel.Text = "…";
        NotificationsStatusLabel.TextColor = Color.FromArgb("#888");

        var (ok, status) = await PushService.EnsureRegistrationAsync();
        if (ok)
        {
            Preferences.Set("push_registered", true);
            SetzeMitteilungsSchalter(true, t("enabled"));
            return;
        }

        Preferences.Set("push_registered", false);
        // status ist eine technische Diagnose (bewusst unübersetzt)
        SetzeMitteilungsSchalter(false, t("not_active") + ": " + status, fehler: true);

        // Auf iOS lässt sich eine verweigerte Berechtigung nicht erneut per
        // Dialog anfragen -> in die Geräte-Einstellungen leiten.
        bool oeffnen = await DisplayAlertAsync(
            t("notifications"),
            t("notifications_denied_hint"),
            t("open_settings"), t("cancel"));
        if (oeffnen)
        {
            try { AppInfo.Current.ShowSettingsUI(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Geräte-Einstellungen: {ex.Message}");
            }
        }
    }

    private async Task MitteilungenAusschaltenAsync(Func<string, string> t)
    {
        await PushService.UnregisterAsync();
        Preferences.Set("push_registered", false);
        SetzeMitteilungsSchalter(false, t("disabled"));
    }
}
