using System.Diagnostics;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Native Push-Benachrichtigungen (FCM) für Android + iOS.
///
/// Ergänzt den WebSocket: eine neue Chat-Nachricht erzeugt serverseitig
/// zusätzlich einen FCM-Push, der das Gerät auch bei geschlossener/
/// hintergründiger App erreicht (WebSocket ist dann getrennt).
///
/// Robust gegen fehlendes Firebase-Setup: jeder Aufruf ist in try/catch
/// gekapselt, sodass eine fehlende/fehlerhafte Konfiguration die App nie
/// zum Absturz bringt - Push wird dann still übersprungen.
/// </summary>
public static class PushService
{
    private static bool _eventsAbonniert;
    private static string? _aktuellesToken;

    /// <summary>
    /// Nach erfolgreichem Login aufrufen: Berechtigung anfragen, Token holen
    /// und beim Server registrieren.
    /// </summary>
    public static async Task InitializeAsync()
    {
        try
        {
            // Ohne initialisiertes Firebase crasht der Zugriff auf
            // CrossFirebaseCloudMessaging.Current auf iOS nativ (SIGTRAP).
            if (!FirebaseStatus.Ready)
            {
                Debug.WriteLine("[Push] Firebase nicht initialisiert - Push übersprungen");
                return;
            }

            if (!_eventsAbonniert)
            {
                CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;
                CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
                _eventsAbonniert = true;
            }

            // Fragt die Push-Berechtigung an (iOS + Android 13+). Wirft eine
            // Exception, wenn Push nicht verfügbar/verweigert ist -> wird vom
            // äußeren try/catch abgefangen und Push still übersprungen.
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().ConfigureAwait(false);

            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync().ConfigureAwait(false);
            await RegistriereTokenAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] Init-Fehler: {ex.Message}");
        }
    }

    /// <summary>
    /// Vom Einstellungen-Button aufrufbar: fordert die Push-Berechtigung an,
    /// holt das Token und registriert es beim Server. Gibt (ok, Status) zurück -
    /// bei Misserfolg enthält Status den konkreten Grund (Berechtigung/APNs/
    /// Firebase), damit der Nutzer/Support sieht, warum kein Push ankommt.
    /// </summary>
    public static async Task<(bool ok, string status)> EnsureRegistrationAsync()
    {
        try
        {
            if (!FirebaseStatus.Ready)
                return (false, "Firebase nicht initialisiert (Start-Konfiguration fehlgeschlagen)");

            if (!_eventsAbonniert)
            {
                CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;
                CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
                _eventsAbonniert = true;
            }

            // Berechtigung anfragen/prüfen (wirft, wenn verweigert/nicht verfügbar)
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync().ConfigureAwait(false);

            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Kein Push-Token erhalten (APNs/Firebase nicht bereit)");

            _aktuellesToken = token;
            var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
            var res = await ApiService.Instance.RegisterPushTokenAsync(token, platform).ConfigureAwait(false);
            Debug.WriteLine($"[Push] EnsureRegistration ({platform}): success={res.Success}");
            return res.Success
                ? (true, "Mitteilungen aktiviert")
                : (false, "Server-Registrierung fehlgeschlagen: " + (res.Error ?? "unbekannt"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Push] EnsureRegistration-Fehler: {ex.Message}");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Aktueller Berechtigungs-Zustand OHNE einen Dialog anzuzeigen (für die
    /// Statusanzeige in den Einstellungen). true = erlaubt, false = nicht,
    /// null = unbekannt (z.B. wenn die Plattform-Abfrage nicht verfügbar ist).
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
    /// Bei Logout aufrufen: aktuelles Token beim Server abmelden, damit das
    /// Gerät keine Pushes mehr für den abgemeldeten Nutzer bekommt.
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

    private static async void OnTokenChanged(object? sender, FCMTokenChangedEventArgs e)
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
        var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
        var res = await ApiService.Instance.RegisterPushTokenAsync(token, platform).ConfigureAwait(false);
        if (res.Success)
            Preferences.Set("push_registered", true);
        Debug.WriteLine($"[Push] Token registriert ({platform}): success={res.Success}");
    }

    /// <summary>
    /// Nutzer tippt auf eine Push-Benachrichtigung -> passenden Chat öffnen.
    /// Datenformat vom Server: { "type": "chat", "partner": "admin"|"&lt;id&gt;" }
    /// </summary>
    private static void OnNotificationTapped(object? sender, FCMNotificationTappedEventArgs e)
    {
        try
        {
            var daten = e.Notification?.Data;
            if (daten == null)
                return;

            daten.TryGetValue("type", out var typ);
            if (typ != "chat")
                return;

            daten.TryGetValue("partner", out var partner);
            if (string.IsNullOrEmpty(partner))
                partner = "admin";

            daten.TryGetValue("partnerName", out var partnerName);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Shell.Current != null)
                    {
                        var route = $"ChatCurrentPage?partner={partner}";
                        if (!string.IsNullOrEmpty(partnerName))
                            route += $"&partnerName={Uri.EscapeDataString(partnerName)}";
                        await Shell.Current.GoToAsync(route);
                    }
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
