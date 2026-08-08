using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views.Components;

/// <summary>
/// Menü der Kopfleiste: Seitenwechsel und Abmelden.
/// </summary>
public partial class AppHeader
{
    private void OnMenuButtonClicked(object sender, EventArgs e)
        => MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;

    private void OnOverlayTapped(object sender, EventArgs e) => CloseMenu();

    /// <summary>Menü schließen - auch von der Seite aus aufrufbar.</summary>
    public void CloseMenu() => MenuOverlayGrid.IsVisible = false;

    private async void OnLogoTapped(object sender, EventArgs e) => await WechsleZuAsync("//MainTabs/TodayPage");

    private async void OnMenuTodayClicked(object sender, EventArgs e) => await WechsleZuAsync("//MainTabs/TodayPage");

    private async void OnMenuChatClicked(object sender, EventArgs e) => await WechsleZuAsync("//MainTabs/ChatListPage");

    private async void OnMenuAuftragClicked(object sender, EventArgs e) => await WechsleZuAsync("//MainTabs/AuftragPage");

    private async void OnMenuSettingsClicked(object sender, EventArgs e) => await WechsleZuAsync("//MainTabs/SettingsPage");

    /// <summary>
    /// Menü schließen und die Seite wechseln. Shell.Current kann während einer
    /// laufenden Navigation null sein - dann passiert nichts, statt dass die
    /// App am async-void-Handler abstürzt.
    /// </summary>
    private async Task WechsleZuAsync(string ziel)
    {
        CloseMenu();
        try
        {
            if (Shell.Current != null)
                await Shell.Current.GoToAsync(ziel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppHeader] Navigation zu {ziel}: {ex.Message}");
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        CloseMenu();

        // Shell.Current/CurrentPage können während Navigation null sein -
        // dann Abmelden abbrechen statt mit NRE zu crashen (async void!)
        var seite = Shell.Current?.CurrentPage;
        if (seite == null)
            return;

        bool bestaetigt;
        try
        {
            bestaetigt = await seite.DisplayAlertAsync(
                Translations.Get("logout"),
                Translations.Get("really_logout"),
                Translations.Get("yes"),
                Translations.Get("no"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] Rückfrage: {ex.Message}");
            return;
        }

        if (!bestaetigt)
            return;

        await Abmeldung.AufraeumenAsync(_apiService);

        try
        {
            if (Shell.Current == null)
                throw new InvalidOperationException("Keine Shell vorhanden");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] Navigation: {ex.Message}");
            // Ausweichweg über die Anwendung selbst
            try
            {
                if (Application.Current?.Windows.FirstOrDefault()?.Page is Shell shell)
                    await shell.GoToAsync("//LoginPage");
            }
            catch (Exception zweiterFehler)
            {
                System.Diagnostics.Debug.WriteLine($"[Logout] Ausweichweg: {zweiterFehler.Message}");
            }
        }
    }
}
