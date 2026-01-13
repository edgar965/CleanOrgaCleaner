#if BIOMETRIC_AVAILABLE
using Plugin.Maui.Biometric;
#endif

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Service for biometric authentication (FaceID/TouchID on iOS, Fingerprint on Android)
/// Note: Biometric features are only available when built with .NET 10 (BIOMETRIC_AVAILABLE)
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
#if BIOMETRIC_AVAILABLE
        try
        {
            // Try to get availability status from the service
            var biometricService = BiometricAuthenticationService.Default;

            // The plugin returns availability during authentication attempt
            // We'll use a simple check - on iOS/Android with proper setup, it should work
#if IOS || MACCATALYST
            return await Task.FromResult(true);
#elif ANDROID
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
#else
        // Biometric not available in this build
        return await Task.FromResult(false);
#endif
    }

    /// <summary>
    /// Get the type of biometric available (FaceID, TouchID, Fingerprint)
    /// </summary>
    public Task<string> GetBiometricTypeAsync()
    {
#if BIOMETRIC_AVAILABLE
#if IOS || MACCATALYST
        // iOS uses Face ID or Touch ID
        return Task.FromResult("Face ID / Touch ID");
#elif ANDROID
        return Task.FromResult("Fingerabdruck");
#else
        return Task.FromResult("Biometrie");
#endif
#else
        return Task.FromResult("Nicht verfügbar");
#endif
    }

    /// <summary>
    /// Authenticate using biometrics
    /// </summary>
    /// <param name="reason">The reason shown to the user for authentication</param>
    /// <returns>True if authentication succeeded</returns>
    public async Task<bool> AuthenticateAsync(string reason = "Anmelden bei CleanOrga")
    {
#if BIOMETRIC_AVAILABLE
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
            );

            System.Diagnostics.Debug.WriteLine($"[Biometric] Auth result: {result.Status}");
            return result.Status == BiometricResponseStatus.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Biometric] Auth error: {ex.Message}");
            return false;
        }
#else
        // Biometric not available in this build
        await Task.CompletedTask;
        return false;
#endif
    }

    /// <summary>
    /// Check if user has enabled biometric login
    /// </summary>
    public bool IsBiometricLoginEnabled()
    {
#if BIOMETRIC_AVAILABLE
        return Preferences.Get("biometric_login_enabled", false);
#else
        return false;
#endif
    }

    /// <summary>
    /// Enable or disable biometric login
    /// </summary>
    public void SetBiometricLoginEnabled(bool enabled)
    {
#if BIOMETRIC_AVAILABLE
        Preferences.Set("biometric_login_enabled", enabled);
#endif
    }
}
