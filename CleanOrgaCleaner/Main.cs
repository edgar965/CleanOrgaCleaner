using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner;

/// <summary>
/// Main application class - handles app-wide state and initialization
/// </summary>
public static class Main
{
    /// <summary>
    /// Server URL
    /// </summary>
    public const string ServerUrl = "cleanorga.com";

    /// <summary>
    /// Is the user currently logged in?
    /// </summary>
    public static bool IsLoggedIn => Preferences.Get("is_logged_in", false);

    /// <summary>
    /// Current user's name
    /// </summary>
    public static string? UserName => Preferences.Get("username", null);

    /// <summary>
    /// Current property ID
    /// </summary>
    public static string? PropertyId => Preferences.Get("property_id", null);

    /// <summary>
    /// Current language code
    /// </summary>
    public static string Language
    {
        get => Translations.CurrentLanguage;
        set
        {
            Translations.CurrentLanguage = value;
            Translations.SaveToPreferences();
        }
    }

    /// <summary>
    /// Initialize the application
    /// Called at app startup
    /// </summary>
    public static void Initialize()
    {
        // Load language from preferences
        Translations.LoadFromPreferences();

        System.Diagnostics.Debug.WriteLine($"[Main] Initialized - Version {AppInfo.Current.VersionString}");
        System.Diagnostics.Debug.WriteLine($"[Main] Language: {Language}");
        System.Diagnostics.Debug.WriteLine($"[Main] LoggedIn: {IsLoggedIn}");
    }

    /// <summary>
    /// Clear login state
    /// </summary>
    public static async Task ClearLoginAsync()
    {
        // Nur is_logged_in löschen - property_id und username behalten für "Anmeldedaten merken"
        Preferences.Remove("is_logged_in");

        // Clear API service session (sends offline status to server)
        await ApiService.Instance.LogoutAsync();

        // Disconnect WebSocket
        WebSocketService.Instance.Dispose();

        System.Diagnostics.Debug.WriteLine("[Main] Login cleared");
    }

}
