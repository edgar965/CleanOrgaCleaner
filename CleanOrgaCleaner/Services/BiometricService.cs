using Plugin.Maui.Biometric;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Anmeldung per Biometrie (Face ID/Touch ID auf iOS, Fingerabdruck auf Android).
/// </summary>
public class BiometricService
{
    /// <summary>Schlüssel der Einstellung "Anmeldung per Biometrie".</summary>
    private const string SchluesselAktiv = "biometric_login_enabled";

    private static readonly Lazy<BiometricService> _instanz = new(() => new BiometricService());

    /// <summary>Die eine Instanz der App.</summary>
    public static BiometricService Instance => _instanz.Value;

    private BiometricService()
    {
    }

    /// <summary>Steht auf diesem Gerät überhaupt Biometrie zur Verfügung?</summary>
    public Task<bool> IsBiometricAvailableAsync()
    {
#if IOS || MACCATALYST || ANDROID
        return Task.FromResult(true);
#else
        return Task.FromResult(false);
#endif
    }

    /// <summary>Bezeichnung des Verfahrens für die Oberfläche.</summary>
    public Task<string> GetBiometricTypeAsync()
    {
#if IOS || MACCATALYST
        return Task.FromResult("Face ID / Touch ID");
#elif ANDROID
        return Task.FromResult("Fingerabdruck");
#else
        return Task.FromResult("Biometrie");
#endif
    }

    /// <summary>Biometrische Prüfung durchführen.</summary>
    public async Task<bool> AuthenticateAsync(string reason = "Anmelden bei CleanOrga")
    {
        try
        {
            var anfrage = new AuthenticationRequest
            {
                Title = "CleanOrga",
                Subtitle = reason,
                NegativeText = "Abbrechen"
            };

            var ergebnis = await BiometricAuthenticationService.Default.AuthenticateAsync(
                anfrage, CancellationToken.None).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"[Biometric] Auth result: {ergebnis.Status}");
            return ergebnis.Status == BiometricResponseStatus.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Biometric] Auth error: {ex.Message}");
            return false;
        }
    }

    /// <summary>Hat die Arbeitskraft die biometrische Anmeldung eingeschaltet?</summary>
    public bool IsBiometricLoginEnabled() => Preferences.Get(SchluesselAktiv, false);

    /// <summary>Biometrische Anmeldung ein- oder ausschalten.</summary>
    public void SetBiometricLoginEnabled(bool enabled) => Preferences.Set(SchluesselAktiv, enabled);
}
