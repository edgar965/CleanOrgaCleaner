using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Settings page - language selection, user info, logout
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Dictionary<int, string> _languageMap = new()
    {
        { 0, "de" },  // Deutsch
        { 1, "en" },  // English
        { 2, "es" },  // Espanol
        { 3, "ro" },  // Romana
        { 4, "pl" },  // Polski
        { 5, "ru" },  // Russkij
        { 6, "uk" },  // Ukrainska
        { 7, "vi" }   // Tieng Viet
    };

    public SettingsPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyTranslations();
        LoadUserInfo();
        LoadCurrentLanguage();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("settings");

        // Header
        MenuButton.Text = $"{t("settings")} ▼";
        SettingsTitleLabel.Text = t("settings");

        // User Info
        LoggedInAsLabel.Text = t("logged_in_as");

        // Language
        LanguageTitleLabel.Text = t("language");
        LanguagePicker.Title = t("select_language");
        LanguageHintLabel.Text = t("language_hint");

        // App Info
        AppInfoLabel.Text = t("app_info");
        VersionLabel.Text = t("version");
        ServerLabel.Text = t("server");

        // Buttons
        LogoutButton.Text = t("logout");
        ExitButton.Text = t("exit_app");

        // Menu items
        MenuTodayButton.Text = $"🏠 {t("today")}";
        MenuChatButton.Text = $"💬 {t("chat")}";
        MenuSettingsButton.Text = $"⚙️ {t("settings")}";
    }

    // Menu handling
    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private async void OnMenuTodayClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//TodayPage");
    }

    private async void OnMenuChatClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//ChatPage");
    }

    private void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        // Already on settings
    }

    private void LoadUserInfo()
    {
        // Display username from stored preferences
        var username = Preferences.Get("username", "");
        UserNameLabel.Text = string.IsNullOrEmpty(username) ? "Unbekannt" : username;
    }

    private void LoadCurrentLanguage()
    {
        // Get stored language preference
        var storedLang = Preferences.Get("language", "de");

        // Find the index for this language
        var index = _languageMap.FirstOrDefault(x => x.Value == storedLang).Key;

        // Set picker without triggering event
        LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
        LanguagePicker.SelectedIndex = index;
        LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (LanguagePicker.SelectedIndex < 0)
            return;

        var selectedLang = _languageMap.GetValueOrDefault(LanguagePicker.SelectedIndex, "de");

        try
        {
            var response = await _apiService.SetLanguageAsync(selectedLang);

            if (response.Success)
            {
                // Store locally
                Preferences.Set("language", selectedLang);

                await DisplayAlert("Gespeichert",
                    "Sprache wurde geaendert",
                    "OK");
            }
            else
            {
                await DisplayAlert("Fehler",
                    response.Error ?? "Sprache konnte nicht geaendert werden",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetLanguage error: {ex.Message}");
            await DisplayAlert("Fehler", "Verbindungsfehler", "OK");
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Abmelden",
            "Moechtest du dich wirklich abmelden?",
            "Ja", "Nein");

        if (!confirm)
            return;

        try
        {
            // Call logout API
            await _apiService.LogoutAsync();
        }
        catch
        {
            // Ignore errors - we're logging out anyway
        }

        // Clear stored credentials
        Preferences.Remove("property_id");
        Preferences.Remove("username");
        Preferences.Remove("language");
        Preferences.Remove("is_logged_in");
        Preferences.Remove("remember_me");

        // Clear secure storage
        SecureStorage.Remove("password");

        // Disconnect WebSocket
        WebSocketService.Instance.Dispose();

        // Navigate to login page
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnExitClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "App beenden",
            "Moechtest du die App wirklich beenden?",
            "Ja", "Nein");

        if (!confirm)
            return;

        // Disconnect WebSocket
        WebSocketService.Instance.Dispose();

        // Exit the application
        Application.Current?.Quit();
    }
}
