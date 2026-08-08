using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Automatische Anmeldung beim App-Start und die Ersatz-Anmeldung ohne Netz.
/// </summary>
public partial class LoginPage
{
    /// <summary>
    /// Automatische Anmeldung: direkter await, kein WaitAsync, kein
    /// CancellationToken (Modell aus v1.06, das auf iOS stabil läuft).
    /// </summary>
    private async Task TryAutoLoginAsync()
    {
        Log("TryAutoLogin START");

        if (!Preferences.Get("remember_me", false)) { Log("remember_me=false -> skip"); return; }

        var firma = Preferences.Get("property_id", "");
        var benutzer = Preferences.Get("username", "");
        if (string.IsNullOrEmpty(firma) || string.IsNullOrEmpty(benutzer))
        { Log("no saved credentials -> skip"); return; }

        Log($"credentials: prop={firma} user={benutzer}");

        string? kennwort = null;
        try
        {
            kennwort = await SecureStorage.GetAsync("password");
            Log($"SecureStorage DONE (has pw: {!string.IsNullOrEmpty(kennwort)})");
        }
        catch (Exception ex) { Log($"SecureStorage ERROR: {ex.Message}"); }

        if (string.IsNullOrEmpty(kennwort)) { Log("no saved password -> skip"); return; }
        if (!int.TryParse(firma, out int firmaId)) { Log("invalid property_id -> skip"); return; }

        if (!await BiometrieBestandenAsync(benutzer))
            return;

        LoginButton.IsEnabled = false;
        LoginButton.Text = Translations.Get("loading");
        Log("LoginAsync START");

        _navigiert = false;

        try
        {
            var ergebnis = await _apiService.LoginAsync(firmaId, benutzer, kennwort);
            Log($"LoginAsync DONE: success={ergebnis?.Success}");

            // iOS: Anzeige-Thread braucht Luft nach dem Netzaufruf
            await Task.Yield();

            if (ergebnis == null)
            {
                Log("result is null - try offline login");
                await TryOfflineLoginAsync();
                return;
            }

            if (!ergebnis.Success)
            {
                if (NetworkErrorHelper.IsNetworkError(ergebnis.ErrorMessage))
                {
                    Log($"Network error: {ergebnis.ErrorMessage} - try offline login");
                    await TryOfflineLoginAsync();
                    return;
                }

                Log($"FAILED: {ergebnis.ErrorMessage}");
                GespeichertesKennwortVerwerfen();
                ShowError(ergebnis.ErrorMessage ?? Translations.Get("connection_error"));
                return;
            }

            var sprache = SpracheUebernehmen(ergebnis.CleanerLanguage);

            // Für die Anmeldung ohne Netz merken
            _ = OfflineDataService.Instance.SaveLoginStateAsync(
                ergebnis.CleanerName ?? benutzer, sprache, ergebnis.CleanerId);

            await SitzungStartenAsync(biometrieAnbieten: false);
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");

            if (NetworkErrorHelper.IsNetworkError(ex.Message))
            {
                Log("Network exception - try offline login");
                await TryOfflineLoginAsync();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Login] Auto-login error: {ex}");
        }
        finally
        {
            KnopfZuruecksetzenWennNochHier();
            Log("TryAutoLogin END");
        }
    }

    /// <summary>
    /// Ist die Anmeldung per Fingerabdruck/Gesicht eingeschaltet, muss sie erst
    /// bestehen. Liefert false, wenn abgebrochen wurde.
    /// </summary>
    private async Task<bool> BiometrieBestandenAsync(string benutzer)
    {
        bool eingeschaltet = _biometricService.IsBiometricLoginEnabled();
        bool moeglich = await _biometricService.IsBiometricAvailableAsync();
        Log($"biometric: enabled={eingeschaltet}, available={moeglich}");

        if (!eingeschaltet || !moeglich)
            return true;

        var art = await _biometricService.GetBiometricTypeAsync();
        LoginButton.IsEnabled = false;
        LoginButton.Text = $"{art}...";
        Log($"biometric prompt: {art}");

        var bestanden = await _biometricService.AuthenticateAsync($"Anmelden als {benutzer}");
        Log($"biometric result: {bestanden}");

        if (bestanden) return true;

        KnopfFreigeben();
        Log("biometric failed -> abort");
        return false;
    }

    /// <summary>Falsche gespeicherte Zugangsdaten wegräumen.</summary>
    private void GespeichertesKennwortVerwerfen()
    {
        SecureStorage.Remove("password");
        Preferences.Set("remember_me", false);
        RememberMeCheckbox.IsChecked = false;
        PasswordEntry.Text = "";
    }

    /// <summary>
    /// Anmeldung aus dem Zwischenspeicher, wenn der Server nicht erreichbar ist.
    /// </summary>
    private async Task TryOfflineLoginAsync()
    {
        Log("TryOfflineLogin START");

        try
        {
            var stand = await OfflineDataService.Instance.LoadLoginStateAsync();

            string name;
            string sprache;
            int? personId = null;

            if (stand != null)
            {
                name = stand.CleanerName;
                sprache = stand.Language ?? "de";
                personId = stand.CleanerId;
                Log($"Using cached login state: {name}");
            }
            else
            {
                // Ausweichweg: gespeicherte Einstellungen früherer Anmeldungen
                var benutzer = Preferences.Get("username", "");
                sprache = Preferences.Get("language", "de");

                if (string.IsNullOrEmpty(benutzer))
                {
                    Log("No valid offline login state and no saved username");
                    ShowError(Translations.Get("no_connection") + "\n" + Translations.Get("network_error_hint"));
                    return;
                }

                name = benutzer;
                Log($"Using fallback from Preferences: {name}");
                _ = OfflineDataService.Instance.SaveLoginStateAsync(name, sprache, null);
            }

            Log($"Has cached tasks: {OfflineDataService.Instance.HasCachedTasks()}");

            SpracheUebernehmen(sprache);
            _apiService.SetOfflineCleanerInfo(name, personId);
            Preferences.Set("offline_mode", true);

            await Task.Yield();

            // Die Tagesliste holt sich die Aufgaben aus dem Zwischenspeicher
            await NavigiereZurStartseiteAsync();
        }
        catch (Exception ex)
        {
            Log($"TryOfflineLogin EXCEPTION: {ex.Message}");
            ShowError(Translations.Get("no_connection") + "\n" + Translations.Get("network_error_hint"));
        }
        finally
        {
            KnopfZuruecksetzenWennNochHier();
            Log("TryOfflineLogin END");
        }
    }
}
