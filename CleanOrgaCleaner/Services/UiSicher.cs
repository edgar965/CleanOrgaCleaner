using CleanOrgaCleaner.Localization;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// UI-Hilfsfunktionen, die nach Seiten-Teardown oder während Navigation
/// nicht crashen dürfen. Hintergrund: Timer- und WebSocket-Events feuern
/// auch nach Verlassen einer Seite noch, und eine ungefangene Exception in
/// einem MainThread-Delegate oder async-void-Handler beendet die App hart
/// (iOS-Crashes vom 14.07.2026).
/// </summary>
public static class UiSicher
{
    /// <summary>
    /// Aktion auf dem Main-Thread ausführen, Fehler nur loggen.
    /// </summary>
    public static void AufMainThread(Action aktion, string logTag)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try { aktion(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[{logTag}] UI update error: {ex.Message}"); }
        });
    }

    /// <summary>
    /// Async-Aktion (z.B. Daten-Reload) auf dem Main-Thread, Fehler nur loggen.
    /// </summary>
    public static void AufMainThread(Func<Task> aktion, string logTag)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await aktion(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[{logTag}] UI update error: {ex.Message}"); }
        });
    }

    /// <summary>
    /// Event-Invocation absichern: wirft ein Subscriber, wird nur geloggt
    /// statt den aufrufenden Thread (Main-Thread oder Listener-Loop) zu beenden.
    /// </summary>
    public static void SichererInvoke(Action aktion, string logTag)
    {
        try { aktion(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[{logTag}] handler error: {ex.Message}"); }
    }

    /// <summary>
    /// DisplayAlert ohne Crash-Risiko: Shell.Current/CurrentPage können auf
    /// iOS während Navigation/Teardown null sein - dann wird der Hinweis
    /// verworfen statt die App zu beenden.
    /// </summary>
    public static async Task AlertAsync(string titel, string text, string ok)
    {
        try
        {
            var seite = Shell.Current?.CurrentPage;
            if (seite != null)
                await seite.DisplayAlertAsync(titel, text, ok);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UiSicher] Alert error: {ex.Message}");
        }
    }

    /// <summary>
    /// Standard-Fehlerhinweis (error / network_error_hint / ok) ohne Crash-Risiko.
    /// </summary>
    public static Task FehlerAlertAsync()
        => AlertAsync(Translations.Get("error"), Translations.Get("network_error_hint"), Translations.Get("ok"));
}
