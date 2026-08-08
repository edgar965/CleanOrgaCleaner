using CleanOrgaCleaner.Localization;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Anmeldung per Fingerabdruck/Gesicht in den Einstellungen.
/// </summary>
public partial class SettingsPage
{
    /// <summary>
    /// Der Bereich erscheint nur auf Geräten, die Biometrie können.
    /// </summary>
    private async Task LadeBiometrieEinstellungAsync()
    {
        try
        {
            var moeglich = await _biometricService.IsBiometricAvailableAsync();
            if (!moeglich)
            {
                BiometricSection.IsVisible = false;
                System.Diagnostics.Debug.WriteLine("[Settings] Biometrie auf diesem Gerät nicht verfügbar");
                return;
            }

            BiometricSection.IsVisible = true;
            BiometricLabel.Text = Translations.Get("biometric_login");

            // Ohne Abmelden des Handlers würde das Setzen selbst eine Abfrage auslösen
            BiometricSwitch.Toggled -= OnBiometricToggled;
            BiometricSwitch.IsToggled = _biometricService.IsBiometricLoginEnabled();
            BiometricSwitch.Toggled += OnBiometricToggled;

            System.Diagnostics.Debug.WriteLine($"[Settings] Biometrie verfügbar, aktiv: {BiometricSwitch.IsToggled}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Biometrie-Einstellung: {ex.Message}");
            BiometricSection.IsVisible = false;
        }
    }

    private async void OnBiometricToggled(object? sender, ToggledEventArgs e)
    {
        try
        {
            if (!e.Value)
            {
                _biometricService.SetBiometricLoginEnabled(false);
                System.Diagnostics.Debug.WriteLine("[Settings] Biometrie-Anmeldung abgeschaltet");
                return;
            }

            // Einschalten nur, wenn sich die Person auch anmelden kann
            var bestaetigt = await _biometricService.AuthenticateAsync("Biometrie aktivieren");
            if (bestaetigt)
            {
                _biometricService.SetBiometricLoginEnabled(true);
                System.Diagnostics.Debug.WriteLine("[Settings] Biometrie-Anmeldung eingeschaltet");
            }
            else
            {
                SchalterZuruecksetzen();
            }
        }
        catch (Exception ex)
        {
            // Biometrie-APIs werfen auf iOS realistisch (Abbruch/Hardware) -
            // async void darf nie werfen; Schalter zurücksetzen.
            System.Diagnostics.Debug.WriteLine($"[Settings] Biometrie umschalten: {ex.Message}");
            SchalterZuruecksetzen();
        }
    }

    /// <summary>
    /// Schalter aus, ohne den Handler erneut auszulösen. Das Wieder-Anmelden
    /// steht im finally, damit der Handler nie dauerhaft abgemeldet bleibt -
    /// auch dann nicht, wenn der Setter selbst wirft.
    /// </summary>
    private void SchalterZuruecksetzen()
    {
        try
        {
            BiometricSwitch.Toggled -= OnBiometricToggled;
            BiometricSwitch.IsToggled = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Schalter zurücksetzen: {ex.Message}");
        }
        finally
        {
            BiometricSwitch.Toggled += OnBiometricToggled;
        }
    }
}
