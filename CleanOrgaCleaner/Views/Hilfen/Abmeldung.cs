using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Räumt beim Abmelden alle lokalen Spuren auf: Server-Abmeldung, Firestore,
/// gespeicherte Zugangsdaten, Zwischenspeicher und Verbindung.
///
/// Jeder Schritt ist einzeln abgesichert: das Abmelden darf nie an einem
/// Teilschritt hängen bleiben, sonst kommt die Arbeitskraft nicht mehr zur
/// Anmeldeseite zurück.
/// </summary>
public static class Abmeldung
{
    /// <summary>Einstellungen, die zur angemeldeten Person gehören.</summary>
    private static readonly string[] ZuLoeschendeEinstellungen =
    {
        "property_id",
        "username",
        "language",
        "is_logged_in",
        "remember_me",
        "biometric_login_enabled",
        "offline_mode"
    };

    public static async Task AufraeumenAsync(ApiService api)
    {
        try { await api.LogoutAsync(); }
        catch { /* Abmeldung läuft ohnehin weiter */ }

        Schritt("Firestore", () => FirestoreChatService.Instance.Stop());

        Schritt("Einstellungen", () =>
        {
            foreach (var name in ZuLoeschendeEinstellungen)
                Preferences.Remove(name);
        });

        Schritt("SecureStorage", () => SecureStorage.Remove("password"));

        Schritt("Offline-Daten", () => OfflineDataService.Instance.ClearAll());

        // Verbindung im Hintergrund trennen: auf iOS blockiert das sonst den
        // Anzeige-Thread.
        Schritt("Verbindung", () => _ = Task.Run(() =>
        {
            try { WebSocketService.Instance.Dispose(); }
            catch { /* beim Abmelden nicht weiter behandelbar */ }
        }));
    }

    private static void Schritt(string name, Action aktion)
    {
        try { aktion(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Abmeldung] {name}: {ex.Message}");
        }
    }
}
