using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly BiometricService _biometricService;
    private bool _autoLoginAttempted = false;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _biometricService = BiometricService.Instance;

        // Load language and apply translations
        Translations.LoadFromPreferences();
        ApplyTranslations();

        // Load saved credentials
        LoadSavedCredentials();
    }

    private void ApplyTranslations()
    {
        // Subtitle
        SubtitleLabel.Text = Translations.Get("login_subtitle");

        // Form labels
        PropertyIdLabel.Text = Translations.Get("login_property_id");
        UsernameLabel.Text = Translations.Get("login_username");
        PasswordLabel.Text = Translations.Get("login_password");
        RememberMeLabel.Text = Translations.Get("login_remember_me");
        LoginButton.Text = Translations.Get("login_title");

        // Info text
        EnterpriseAppLabel.Text = Translations.Get("login_enterprise_app");
        CredentialsInfoLabel.Text = Translations.Get("login_credentials_info");
        NewCustomersLabel.Text = Translations.Get("login_new_customers");
        RegistrationInfoLabel.Text = Translations.Get("login_registration_info");
        TestUsageLabel.Text = Translations.Get("login_test_usage");
        TestCredentialsLabel.Text = Translations.Get("login_test_credentials");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Attempt auto-login only once per app session
        if (!_autoLoginAttempted)
        {
            _autoLoginAttempted = true;
            await TryAutoLoginAsync();
        }
    }

    private async void LoadSavedCredentials()
    {
        var savedPropertyId = Preferences.Get("property_id", "");
        var savedUsername = Preferences.Get("username", "");
        var rememberMe = Preferences.Get("remember_me", false);

        if (!string.IsNullOrEmpty(savedPropertyId))
            PropertyIdEntry.Text = savedPropertyId;
        if (!string.IsNullOrEmpty(savedUsername))
            UsernameEntry.Text = savedUsername;

        RememberMeCheckbox.IsChecked = rememberMe;

        // Load saved password if remember me was checked
        if (rememberMe)
        {
            try
            {
                var savedPassword = await SecureStorage.GetAsync("password");
                if (!string.IsNullOrEmpty(savedPassword))
                {
                    PasswordEntry.Text = savedPassword;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login] SecureStorage error: {ex.Message}");
            }
        }
    }

    private const int LoginTimeoutSeconds = 10;

    /// <summary>
    /// Zeigt Fortschritt NUR im StatusLabel an.
    /// Button-Text wird NICHT geändert (iOS-Bug: disabled Button rendert Text unsichtbar).
    /// </summary>
    private void UpdateStatus(string msg)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = msg;
            StatusLabel.IsVisible = true;
        });
    }

    private async Task TryAutoLoginAsync()
    {
        var rememberMe = Preferences.Get("remember_me", false);
        if (!rememberMe) return;

        var savedPropertyId = Preferences.Get("property_id", "");
        var savedUsername = Preferences.Get("username", "");

        if (string.IsNullOrEmpty(savedPropertyId) || string.IsNullOrEmpty(savedUsername))
            return;

        UpdateStatus("Lade Zugangsdaten...");
        string? savedPassword = null;
        try
        {
            savedPassword = await SecureStorage.GetAsync("password");
        }
        catch { }

        if (string.IsNullOrEmpty(savedPassword))
            return;

        if (!int.TryParse(savedPropertyId, out int propertyId))
            return;

        // Check if biometric login is enabled and available
        UpdateStatus("Pruefe Biometrie...");
        bool useBiometric = _biometricService.IsBiometricLoginEnabled();
        bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync();

        if (useBiometric && biometricAvailable)
        {
            var biometricType = await _biometricService.GetBiometricTypeAsync();
            LoginButton.IsEnabled = false;
            LoginButton.Text = "⏳";
            UpdateStatus($"{biometricType}...");

            var authenticated = await _biometricService.AuthenticateAsync($"Anmelden als {savedUsername}");

            if (!authenticated)
            {
                LoginButton.IsEnabled = true;
                LoginButton.Text = Translations.Get("login_title");
                StatusLabel.IsVisible = false;
                return;
            }
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = "⏳";
        UpdateStatus("Auto-Login...");

        try
        {
            // Direct async call (kein Task.Run - vermeidet iOS Threading-Probleme)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
            var loginTask = _apiService.LoginAsync(propertyId, savedUsername, savedPassword,
                step => UpdateStatus(step));

            LoginResult? result = null;
            try
            {
                result = await loginTask.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus($"Timeout nach {LoginTimeoutSeconds}s - lade App...");
            }

            if (result?.Success == true)
            {
                UpdateStatus("Login OK - lade App...");
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;

                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                _ = App.InitializeWebSocketAsync();
                return;
            }
            else if (result == null)
            {
                // Timeout - still go to TodayPage so the app is usable
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                return;
            }
            else
            {
                UpdateStatus($"Fehler: {result.ErrorMessage}");
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RememberMeCheckbox.IsChecked = false;
                PasswordEntry.Text = "";
                ShowError(result.ErrorMessage ?? Translations.Get("connection_error"));
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Fehler: {ex.Message}");
            // On any error, still navigate to TodayPage so the app doesn't get stuck
            await Shell.Current.GoToAsync("//MainTabs/TodayPage");
            return;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = Translations.Get("login_title");
        }
    }

    private void OnRememberMeLabelTapped(object sender, EventArgs e)
    {
        RememberMeCheckbox.IsChecked = !RememberMeCheckbox.IsChecked;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(PropertyIdEntry.Text) ||
            string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError(Translations.Get("error"));
            return;
        }

        if (!int.TryParse(PropertyIdEntry.Text, out int propertyId))
        {
            ShowError(Translations.Get("error"));
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = "⏳";
        ErrorLabel.IsVisible = false;
        UpdateStatus("Login...");

        try
        {
            var uname = UsernameEntry.Text;
            var pwd = PasswordEntry.Text;

            // Direct async call (kein Task.Run - vermeidet iOS Threading-Probleme)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
            var loginTask = _apiService.LoginAsync(propertyId, uname, pwd,
                step => UpdateStatus(step));

            LoginResult? result = null;
            try
            {
                result = await loginTask.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus($"Timeout nach {LoginTimeoutSeconds}s");
                ShowError($"Login-Timeout nach {LoginTimeoutSeconds}s. Server antwortet nicht.");
                return;
            }

            if (result.Success)
            {
                UpdateStatus("Login OK - lade App...");

                // Save credentials
                Preferences.Set("property_id", PropertyIdEntry.Text);
                Preferences.Set("username", UsernameEntry.Text);
                Preferences.Set("is_logged_in", true);

                // Save password securely if remember me is checked
                if (RememberMeCheckbox.IsChecked)
                {
                    Preferences.Set("remember_me", true);
                    try
                    {
                        await SecureStorage.SetAsync("password", PasswordEntry.Text);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Login] SecureStorage save error: {ex.Message}");
                    }
                }
                else
                {
                    Preferences.Set("remember_me", false);
                    SecureStorage.Remove("password");
                }

                // Save and apply cleaner's language
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;

                // Check if we should prompt for Face ID / biometric login
                await PromptForBiometricLoginAsync();

                // Navigate to main tabs
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                _ = App.InitializeWebSocketAsync();
            }
            else
            {
                UpdateStatus($"Fehler: {result.ErrorMessage}");
                ShowError(result.ErrorMessage ?? Translations.Get("error"));
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Fehler: {ex.Message}");
            ShowError($"{Translations.Get("error")}: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = Translations.Get("login_title");
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async Task PromptForBiometricLoginAsync()
    {
        // Only prompt if:
        // 1. Remember Me is checked (we have saved credentials)
        // 2. Biometric is not already enabled
        // 3. Biometric is available on this device
        if (!RememberMeCheckbox.IsChecked)
            return;

        if (_biometricService.IsBiometricLoginEnabled())
            return;

        bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync();
        if (!biometricAvailable)
            return;

        var biometricType = await _biometricService.GetBiometricTypeAsync();

        var enableBiometric = await DisplayAlert(
            biometricType,
            $"Moechten Sie {biometricType} fuer zukuenftige Anmeldungen aktivieren?",
            "Ja",
            "Nein");

        if (enableBiometric)
        {
            // Verify biometric works before enabling
            var authenticated = await _biometricService.AuthenticateAsync($"{biometricType} einrichten");

            if (authenticated)
            {
                _biometricService.SetBiometricLoginEnabled(true);
                System.Diagnostics.Debug.WriteLine($"[Login] {biometricType} enabled after login");
            }
        }
    }
}
