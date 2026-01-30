using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner;

/// <summary>
/// Main application class - handles app-wide state and initialization
/// </summary>
public static class Main
{
    /// <summary>
    /// App version number
    /// </summary>
    public const string Version = "1.19";

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

        System.Diagnostics.Debug.WriteLine($"[Main] Initialized - Version {Version}");
        System.Diagnostics.Debug.WriteLine($"[Main] Language: {Language}");
        System.Diagnostics.Debug.WriteLine($"[Main] LoggedIn: {IsLoggedIn}");
    }

    /// <summary>
    /// Save login state
    /// </summary>
    public static void SaveLogin(string propertyId, string username, string? language = null)
    {
        Preferences.Set("property_id", propertyId);
        Preferences.Set("username", username);
        Preferences.Set("is_logged_in", true);

        if (!string.IsNullOrEmpty(language))
        {
            Language = language;
        }

        System.Diagnostics.Debug.WriteLine($"[Main] Login saved: {username}");
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

    /// <summary>
    /// Get localized string
    /// Shortcut for Translations.Get()
    /// </summary>
    public static string T(string key) => Translations.Get(key);

    /// <summary>
    /// Navigate to login page
    /// </summary>
    public static async Task NavigateToLogin()
    {
        await ClearLoginAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    /// <summary>
    /// Navigate to main page (TodayPage)
    /// </summary>
    public static async Task NavigateToMain()
    {
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
    }

    /// <summary>
    /// Show error alert with translated text
    /// </summary>
    public static async Task ShowError(Page page, string message)
    {
        await page.DisplayAlertAsync(T("error"), message, T("ok"));
    }

    /// <summary>
    /// Show confirmation dialog with translated text
    /// </summary>
    public static async Task<bool> ShowConfirm(Page page, string title, string message)
    {
        return await page.DisplayAlertAsync(title, message, T("yes"), T("no"));
    }

    /// <summary>
    /// Format date for display (German format)
    /// </summary>
    public static string FormatDate(DateTime date)
    {
        return date.ToString("dd.MM.yyyy");
    }

    /// <summary>
    /// Format time for display (German format)
    /// </summary>
    public static string FormatTime(DateTime time)
    {
        return time.ToString("HH:mm");
    }

    /// <summary>
    /// Format decimal with comma (German format)
    /// </summary>
    public static string FormatDecimal(double value, int decimals = 2)
    {
        return value.ToString($"F{decimals}").Replace(".", ",");
    }
}
