using Plugin.Maui.Biometric;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Service for biometric authentication (FaceID/TouchID on iOS, Fingerprint on Android)
/// </summary>
public class BiometricService
{
    private static BiometricService? _instance;
    public static BiometricService Instance => _instance ??= new BiometricService();

    private BiometricService()
    {
    }

    /// <summary>
    /// Check if biometric authentication is available on this device
    /// </summary>
    public async Task<bool> IsBiometricAvailableAsync()
    {
        try
        {
#if IOS || MACCATALYST || ANDROID
            return await Task.FromResult(true);
#else
            return await Task.FromResult(false);
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Biometric] Availability check error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the type of biometric available (FaceID, TouchID, Fingerprint)
    /// </summary>
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

    /// <summary>
    /// Authenticate using biometrics
    /// </summary>
    public async Task<bool> AuthenticateAsync(string reason = "Anmelden bei CleanOrga")
    {
        try
        {
            var request = new AuthenticationRequest
            {
                Title = "CleanOrga",
                Subtitle = reason,
                NegativeText = "Abbrechen"
            };

            var result = await BiometricAuthenticationService.Default.AuthenticateAsync(
                request,
                CancellationToken.None
            ).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"[Biometric] Auth result: {result.Status}");
            return result.Status == BiometricResponseStatus.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Biometric] Auth error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if user has enabled biometric login
    /// </summary>
    public bool IsBiometricLoginEnabled()
    {
        return Preferences.Get("biometric_login_enabled", false);
    }

    /// <summary>
    /// Enable or disable biometric login
    /// </summary>
    public void SetBiometricLoginEnabled(bool enabled)
    {
        Preferences.Set("biometric_login_enabled", enabled);
    }
}
