namespace CleanOrgaCleaner.Services;

/// <summary>
/// Cross-platform permission helper with platform-specific implementations
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Opens the app settings page where the user can grant permissions
    /// </summary>
    public static void OpenAppSettings()
    {
#if ANDROID
        OpenAndroidAppSettings();
#else
        // Fallback for other platforms
        AppInfo.ShowSettingsUI();
#endif
    }

#if ANDROID
    private static void OpenAndroidAppSettings()
    {
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            var uri = Android.Net.Uri.FromParts("package", context.PackageName, null);
            intent.SetData(uri);
            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PermissionHelper] Failed to open Android settings: {ex.Message}");
            // Fallback
            AppInfo.ShowSettingsUI();
        }
    }
#endif

    /// <summary>
    /// Checks camera permission and requests if needed. Returns true if granted.
    /// </summary>
    public static async Task<bool> CheckAndRequestCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        System.Diagnostics.Debug.WriteLine($"[PermissionHelper] Camera status: {status}");

        if (status == PermissionStatus.Granted)
            return true;

        // Try to request
        status = await Permissions.RequestAsync<Permissions.Camera>();
        System.Diagnostics.Debug.WriteLine($"[PermissionHelper] Camera after request: {status}");

        return status == PermissionStatus.Granted;
    }
}
