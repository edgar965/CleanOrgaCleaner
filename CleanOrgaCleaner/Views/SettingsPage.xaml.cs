using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Settings page - language selection, user info, logout
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService;
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

    // Avatar options - diverse emoji avatars
    private readonly List<string> _avatarOptions = new()
    {
        "", // Logo (default)
        "👨🏻", "👨🏼", "👨🏽", "👨🏾", "👨🏿",
        "👨🏻‍🦰", "👨🏽‍🦰", "👨🏻‍🦱", "👨🏾‍🦱", "👨🏼‍🦳", "👨🏽‍🦲",
        "🧔🏻", "🧔🏽", "🧔🏾",
        "👷🏻‍♂️", "👷🏽‍♂️", "👷🏾‍♂️",
        "🧑🏻‍🔧", "🧑🏽‍🔧", "🧑🏾‍🔧",
        "👨🏻‍💼", "👨🏽‍💼", "👨🏾‍💼",
        "👴🏻", "👴🏽", "👴🏿",
        "👩🏻", "👩🏼", "👩🏽", "👩🏾", "👩🏿",
        "👩🏻‍🦰", "👩🏽‍🦰", "👩🏻‍🦱", "👩🏾‍🦱", "👩🏼‍🦳",
        "👱🏻‍♀️", "👱🏽‍♀️",
        "👷🏻‍♀️", "👷🏽‍♀️", "👷🏾‍♀️",
        "👩🏻‍💼", "👩🏽‍💼", "👩🏾‍💼",
        "👵🏻", "👵🏽", "👵🏿",
        "🧑🏻", "🧑🏼", "🧑🏽", "🧑🏾", "🧑🏿",
        "🧓🏻", "🧓🏽", "🧓🏿"
    };

    public SettingsPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _biometricService = BiometricService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Initialize header (handles translations, user info, work status, offline banner)
        _ = Header.InitializeAsync();
        Header.SetPageTitle("settings");

        ApplyTranslations();
        LoadUserInfo();
        LoadCurrentAvatar();
        LoadCurrentLanguage();
        _ = LoadBiometricSettingsAsync();
    }

    private void LoadCurrentAvatar()
    {
        var avatar = Preferences.Get("avatar", "");
        CurrentAvatarLabel.Text = string.IsNullOrEmpty(avatar) ? "🏠" : avatar;
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("settings");

        // Content
        SettingsTitleLabel.Text = t("settings");

        // User Info
        LoggedInAsLabel.Text = t("logged_in_as");

        // Avatar
        AvatarHintLabel.Text = t("tap_to_change");
        ChangeAvatarButton.Text = t("change");

        // Language
        LanguageTitleLabel.Text = t("language");
        LanguagePicker.Title = t("select_language");

        // Biometric / Security
        BiometricTitleLabel.Text = t("security");
        BiometricHintLabel.Text = t("biometric_hint");

        // App Info
        AppInfoLabel.Text = t("app_info");
        VersionLabel.Text = t("version");
        VersionValueLabel.Text = Main.Version;
        ServerLabel.Text = t("server");
    }

    private void LoadUserInfo()
    {
        // Display username from stored preferences
        var username = Preferences.Get("username", "");
        UserNameLabel.Text = string.IsNullOrEmpty(username) ? "Unbekannt" : username;
        // Also set username in avatar section (like Django client)
        AvatarUsernameLabel.Text = string.IsNullOrEmpty(username) ? "Unbekannt" : username;
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

        var t = Translations.Get;
        try
        {
            var response = await _apiService.SetLanguageAsync(selectedLang);

            if (response.Success)
            {
                // Store locally and update Translations
                Preferences.Set("language", selectedLang);
                Localization.Translations.CurrentLanguage = selectedLang;

                // Refresh UI with new language
                ApplyTranslations();
                Header.ApplyTranslations();
            }
            else
            {
                await DisplayAlertAsync(t("error"),
                    response.Error ?? t("unknown_error"),
                    t("ok"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetLanguage error: {ex.Message}");
            await DisplayAlertAsync(t("error"), t("connection_error"), t("ok"));
        }
    }

    private async void OnChangeAvatarClicked(object? sender, EventArgs e)
    {
        var t = Translations.Get;

        // Build display list for action sheet (show emoji or "Logo" for empty)
        var displayOptions = _avatarOptions.Select(a => string.IsNullOrEmpty(a) ? "🏠 Logo" : a).ToArray();

        var result = await DisplayActionSheetAsync(
            t("select_avatar"),
            t("cancel"),
            null,
            displayOptions);

        if (result == null || result == t("cancel"))
            return;

        // Find the selected avatar
        var selectedIndex = Array.IndexOf(displayOptions, result);
        if (selectedIndex < 0 || selectedIndex >= _avatarOptions.Count)
            return;

        var selectedAvatar = _avatarOptions[selectedIndex];

        try
        {
            var response = await _apiService.SetAvatarAsync(selectedAvatar);

            if (response.Success)
            {
                // Store locally
                Preferences.Set("avatar", selectedAvatar);

                // Update display
                CurrentAvatarLabel.Text = string.IsNullOrEmpty(selectedAvatar) ? "🏠" : selectedAvatar;

                await DisplayAlertAsync(t("saved"), t("avatar_changed"), t("ok"));
            }
            else
            {
                await DisplayAlertAsync(t("error"),
                    response.Error ?? t("unknown_error"),
                    t("ok"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetAvatar error: {ex.Message}");
            await DisplayAlertAsync(t("error"), t("connection_error"), t("ok"));
        }
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

                // Use translated text for biometric label
                BiometricLabel.Text = Translations.Get("biometric_login");

                // Load current setting without triggering event
                BiometricSwitch.Toggled -= OnBiometricToggled;
                BiometricSwitch.IsToggled = _biometricService.IsBiometricLoginEnabled();
                BiometricSwitch.Toggled += OnBiometricToggled;

                System.Diagnostics.Debug.WriteLine($"[Settings] Biometric available, enabled: {BiometricSwitch.IsToggled}");
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
