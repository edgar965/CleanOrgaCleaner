using CleanOrgaCleaner.Localization;
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

    private async Task TryAutoLoginAsync()
    {
        var rememberMe = Preferences.Get("remember_me", false);
        if (!rememberMe) return;

        var savedPropertyId = Preferences.Get("property_id", "");
        var savedUsername = Preferences.Get("username", "");

        if (string.IsNullOrEmpty(savedPropertyId) || string.IsNullOrEmpty(savedUsername))
            return;

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
        bool useBiometric = _biometricService.IsBiometricLoginEnabled();
        bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync();

        if (useBiometric && biometricAvailable)
        {
            // Prompt for biometric authentication
            var biometricType = await _biometricService.GetBiometricTypeAsync();
            LoginButton.IsEnabled = false;
            LoginButton.Text = $"{biometricType}...";

            var authenticated = await _biometricService.AuthenticateAsync($"Anmelden als {savedUsername}");

            if (!authenticated)
            {
                // User cancelled or failed biometric - stay on login page
                LoginButton.IsEnabled = true;
                LoginButton.Text = Translations.Get("login_title");
                return;
            }
        }

        // Show auto-login state
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Auto-Login [1]...";

        try
        {
            var result = await _apiService.LoginAsync(propertyId, savedUsername, savedPassword);
            LoginButton.Text = "Auto-Login [2]...";

            if (result.Success)
            {
                // Apply language
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;

                LoginButton.Text = "Auto-Login [3]...";

                // Navigate using Dispatcher to avoid iOS deadlock
                await NavigateToMainAsync();
                return;
            }
            else
            {
                // Auto-login failed, clear saved password
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RememberMeCheckbox.IsChecked = false;
                PasswordEntry.Text = "";
                ShowError(Translations.Get("connection_error"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login] Auto-login error: {ex.Message}");
            ShowError($"Auto-login: {ex.Message}");
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

        // Show loading state
        LoginButton.IsEnabled = false;
        LoginButton.Text = Translations.Get("loading");
        ErrorLabel.IsVisible = false;

        try
        {
            LoginButton.Text = "Login [1]...";
            var result = await _apiService.LoginAsync(
                propertyId,
                UsernameEntry.Text,
                PasswordEntry.Text);

            LoginButton.Text = "Login [2]...";

            if (result.Success)
            {
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

                LoginButton.Text = "Login [3]...";

                // Skip biometric prompt on iOS - DisplayAlert during login flow can deadlock
                // Biometric will be prompted on next manual login if needed
                #if !IOS
                await PromptForBiometricLoginAsync();
                #endif

                LoginButton.Text = "Login [4]...";

                // Navigate using Dispatcher to avoid iOS deadlock
                await NavigateToMainAsync();
            }
            else
            {
                ShowError(result.ErrorMessage ?? Translations.Get("error"));
            }
        }
        catch (Exception ex)
        {
            ShowError($"{Translations.Get("error")}: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = Translations.Get("login_title");
        }
    }

    /// <summary>
    /// Navigate to main page with timeout and fallback strategies.
    /// Uses Dispatcher to avoid iOS UIKit deadlocks during Shell navigation.
    /// </summary>
    private async Task NavigateToMainAsync()
    {
        try
        {
            LoginButton.Text = "Navigate [5]...";

            // Strategy: Use Dispatcher.DispatchAsync to ensure clean UI thread state
            // This avoids deadlocks where GoToAsync blocks the main thread
            // while other async continuations are pending
            var navigated = false;
            var tcs = new TaskCompletionSource<bool>();

            // Timeout: if navigation doesn't complete in 10 seconds, try fallback
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            cts.Token.Register(() =>
            {
                if (!navigated)
                    tcs.TrySetResult(false);
            });

            Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                    navigated = true;
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] GoToAsync error: {ex.Message}");
                    tcs.TrySetResult(false);
                }
            });

            var success = await tcs.Task;

            if (success)
            {
                LoginButton.Text = "Navigate [6] OK";
                // Initialize WebSocket AFTER navigation completes
                _ = App.InitializeWebSocketAsync();
            }
            else
            {
                LoginButton.Text = "Navigate TIMEOUT - retry...";
                System.Diagnostics.Debug.WriteLine("[Login] Navigation timeout - trying Application.Current.MainPage fallback");

                // Fallback: replace entire MainPage (bypasses Shell navigation)
                await Task.Delay(500);
                try
                {
                    Application.Current!.MainPage = new AppShell();
                    await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                    _ = App.InitializeWebSocketAsync();
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[Login] Fallback navigation error: {ex2.Message}");
                    ShowError($"Navigation error: {ex2.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login] NavigateToMainAsync error: {ex.Message}");
            ShowError($"Navigate: {ex.Message}");
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
