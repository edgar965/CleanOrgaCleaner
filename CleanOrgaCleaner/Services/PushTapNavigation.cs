using System.Diagnostics;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Öffnet nach dem Tippen auf eine Mitteilung den passenden Chat.
/// Datenformat vom Server: { "type": "chat", "partner": "admin"|"&lt;id&gt;" }.
/// </summary>
public static class PushTapNavigation
{
    /// <summary>Zu einer angetippten Mitteilung navigieren.</summary>
    public static void Oeffne(IDictionary<string, string>? daten)
    {
        try
        {
            if (daten == null)
                return;

            daten.TryGetValue("type", out var art);
            if (art != "chat")
                return;

            daten.TryGetValue("partner", out var partner);
            if (string.IsNullOrEmpty(partner))
                partner = "admin";

            daten.TryGetValue("partnerName", out var partnerName);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Shell.Current == null)
                        return;

                    var ziel = $"ChatCurrentPage?partner={partner}";
                    if (!string.IsNullOrEmpty(partnerName))
                        ziel += $"&partnerName={Uri.EscapeDataString(partnerName)}";

                    await Shell.Current.GoToAsync(ziel);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Push] Navigation-Fehler: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] Tap-Fehler: {ex.Message}");
        }
    }
}
