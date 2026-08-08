using System.Diagnostics;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Native Mitteilungen (FCM) für Android und iOS.
///
/// Ergänzt die Echtzeit-Verbindung: eine neue Chat-Nachricht erzeugt
/// serverseitig zusätzlich eine Mitteilung, die das Gerät auch bei
/// geschlossener App erreicht.
///
/// Robust gegen fehlendes Firebase-Setup: jeder Aufruf ist gekapselt, sodass
/// eine fehlerhafte Konfiguration die App nie zum Absturz bringt - Mitteilungen
/// werden dann still übersprungen.
/// </summary>
public static class PushService
{
    private static bool _eventsAbonniert;
    private static string? _aktuellesToken;

    /// <summary>
    /// Nach erfolgreichem Login aufrufen: Berechtigung anfragen, Token holen
    /// und beim Server anmelden.
    /// </summary>
    public static async Task InitializeAsync()
    {
        try
        {
            // Ohne initialisiertes Firebase stürzt der Zugriff auf
            // CrossFirebaseCloudMessaging.Current auf iOS nativ ab (SIGTRAP).
            if (!FirebaseStatus.Ready)
            {
                Debug.WriteLine("[Push] Firebase nicht initialisiert - Push übersprungen");
                return;
            }

            AbonniereEreignisse();
            var token = await HoleTokenAsync().ConfigureAwait(false);
            await RegistriereTokenAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] Init-Fehler: {ex.Message}");
        }
    }

    /// <summary>
    /// Aus den Einstellungen aufrufbar: Berechtigung anfragen, Token holen und
    /// beim Server anmelden. Bei Misserfolg nennt der Status den konkreten
    /// Grund, damit sichtbar ist, warum keine Mitteilung ankommt.
    /// </summary>
    public static async Task<(bool ok, string status)> EnsureRegistrationAsync()
    {
        try
        {
            if (!FirebaseStatus.Ready)
                return (false, "Firebase nicht initialisiert (Start-Konfiguration fehlgeschlagen)");

            AbonniereEreignisse();

            var token = await HoleTokenAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Kein Push-Token erhalten (APNs/Firebase nicht bereit)");

            _aktuellesToken = token;
            var plattform = Plattform();
            var antwort = await ApiService.Instance.RegisterPushTokenAsync(token, plattform).ConfigureAwait(false);
            Debug.WriteLine($"[Push] EnsureRegistration ({plattform}): success={antwort.Success}");

            return antwort.Success
                ? (true, "Mitteilungen aktiviert")
                : (false, "Server-Registrierung fehlgeschlagen: " + (antwort.Error ?? "unbekannt"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] EnsureRegistration-Fehler: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Aktueller Berechtigungs-Zustand OHNE Dialog (für die Statusanzeige).
    /// true = erlaubt, false = nicht, null = unbekannt.
    /// </summary>
    public static async Task<bool?> IstErlaubtAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>().ConfigureAwait(false);
            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] Status-Abfrage nicht verfügbar: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Beim Abmelden aufrufen: Token beim Server abmelden, damit das Gerät
    /// keine Mitteilungen mehr für den abgemeldeten Nutzer bekommt.
    /// </summary>
    public static async Task UnregisterAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_aktuellesToken))
                return;

            await ApiService.Instance.UnregisterPushTokenAsync(_aktuellesToken).ConfigureAwait(false);
            Debug.WriteLine("[Push] Token abgemeldet");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] Unregister-Fehler: {ex.Message}");
        }
    }

    /// <summary>Ereignisse genau einmal abonnieren.</summary>
    private static void AbonniereEreignisse()
    {
        if (_eventsAbonniert)
            return;

        CrossFirebaseCloudMessaging.Current.TokenChanged += BeiTokenWechsel;
        CrossFirebaseCloudMessaging.Current.NotificationTapped += BeiMitteilungAngetippt;
        _eventsAbonniert = true;
    }

    /// <summary>
    /// Berechtigung prüfen/anfragen (iOS + Android 13+) und Token holen.
    /// Wirft, wenn Mitteilungen nicht verfügbar oder verweigert sind - das
    /// fangen die Aufrufer ab.
    /// </summary>
    private static async Task<string?> HoleTokenAsync()
    {
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().ConfigureAwait(false);
        return await CrossFirebaseCloudMessaging.Current.GetTokenAsync().ConfigureAwait(false);
    }

    /// <summary>Plattformkürzel für den Server.</summary>
    private static string Plattform() => DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";

    private static async void BeiTokenWechsel(object? sender, FCMTokenChangedEventArgs e)
    {
        try
        {
            await RegistriereTokenAsync(e.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] TokenChanged-Fehler: {ex.Message}");
        }
    }

    private static async Task RegistriereTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        _aktuellesToken = token;
        var plattform = Plattform();
        var antwort = await ApiService.Instance.RegisterPushTokenAsync(token, plattform).ConfigureAwait(false);
        if (antwort.Success)
            Preferences.Set("push_registered", true);
        Debug.WriteLine($"[Push] Token registriert ({plattform}): success={antwort.Success}");
    }

    private static void BeiMitteilungAngetippt(object? sender, FCMNotificationTappedEventArgs e)
        => PushTapNavigation.Oeffne(e.Notification?.Data);
}
