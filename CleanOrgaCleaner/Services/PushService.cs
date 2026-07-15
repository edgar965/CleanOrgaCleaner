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
