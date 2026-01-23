using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Settings page - language selection, user info, logout
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly BiometricService _biometricService;
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
        _webSocketService = WebSocketService.Instance;
        _biometricService = BiometricService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;
        UpdateOfflineBanner(!_webSocketService.IsOnline);

        ApplyTranslations();
        LoadUserInfo();
        LoadCurrentLanguage();
        await LoadBiometricSettingsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnConnectionStatusChanged -= OnConnectionStatusChanged;
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateOfflineBanner(!isConnected);
        });
    }

    private void UpdateOfflineBanner(bool showOffline)
    {
        OfflineBanner.IsVisible = showOffline;
        OfflineSpinner.IsRunning = showOffline;
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("settings");

        // Header
        HeaderLogoutButton.Text = t("logout");
        PageTitleLabel.Text = t("settings");
        UserInfoLabel.Text = _apiService.CleanerName ?? Preferences.Get("username", "");

        // Content
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
        VersionValueLabel.Text = Main.Version;
        ServerLabel.Text = t("server");

        // Buttons
        LogoutButton.Text = t("logout");
        ExitButton.Text = t("exit_app");

        // Menu items - no emojis
        MenuTodayButton.Text = t("today");
        MenuChatButton.Text = t("chat");
        MenuAuftragButton.Text = t("task");
        MenuSettingsButton.Text = t("settings");
    }

    // Menu handling
    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private async void OnMenuTodayClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
    }

    private async void OnMenuChatClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/ChatListPage");
    }

    private async void OnMenuAuftragClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/AuftragPage");
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
        Preferences.Remove("biometric_login_enabled");

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

    private async Task LoadBiometricSettingsAsync()
    {
        try
        {
            // Check if biometrics are available on this device
            var isAvailable = await _biometricService.IsBiometricAvailableAsync();

            if (isAvailable)
            {
                // Show biometric section
                BiometricSection.IsVisible = true;

                // Get the biometric type name
                var biometricType = await _biometricService.GetBiometricTypeAsync();
                BiometricLabel.Text = biometricType;

                // Load current setting without triggering event
                BiometricSwitch.Toggled -= OnBiometricToggled;
                BiometricSwitch.IsToggled = _biometricService.IsBiometricLoginEnabled();
                BiometricSwitch.Toggled += OnBiometricToggled;

                System.Diagnostics.Debug.WriteLine($"[Settings] Biometric available: {biometricType}, enabled: {BiometricSwitch.IsToggled}");
            }
            else
            {
                // Hide biometric section on devices without biometric capability
                BiometricSection.IsVisible = false;
                System.Diagnostics.Debug.WriteLine("[Settings] Biometric not available on this device");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Error loading biometric settings: {ex.Message}");
            BiometricSection.IsVisible = false;
        }
    }

    private async void OnBiometricToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // User wants to enable biometric - verify they can authenticate
            var authenticated = await _biometricService.AuthenticateAsync("Biometrie aktivieren");

            if (authenticated)
            {
                _biometricService.SetBiometricLoginEnabled(true);
                System.Diagnostics.Debug.WriteLine("[Settings] Biometric login enabled");
            }
            else
            {
                // Authentication failed - revert switch
                BiometricSwitch.Toggled -= OnBiometricToggled;
                BiometricSwitch.IsToggled = false;
                BiometricSwitch.Toggled += OnBiometricToggled;
            }
        }
        else
        {
            // Disable biometric
            _biometricService.SetBiometricLoginEnabled(false);
            System.Diagnostics.Debug.WriteLine("[Settings] Biometric login disabled");
        }
    }
}
