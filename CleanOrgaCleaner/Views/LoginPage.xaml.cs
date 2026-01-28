using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly BiometricService _biometricService;
    private bool _autoLoginAttempted = false;
    private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
    private readonly List<string> _logLines = new();

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

    private void Log(string msg)
    {
        var ms = _sw.ElapsedMilliseconds;
        var line = $"[{ms,5}ms] {msg}";

        System.Diagnostics.Debug.WriteLine($"[LOGIN-DBG] {line}");

        _logLines.Add(line);
        if (_logLines.Count > 30)
            _logLines.RemoveAt(0);
        DebugLogLabel.Text = string.Join("\n", _logLines);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Register debug callback so ApiService logs show on-screen
        ApiService.DebugLog = (msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() => Log($"[API] {msg}"));
        };

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

    /// <summary>
    /// Auto-Login: v1.06 Modell — direkter await, kein .WaitAsync(), kein CancellationToken.
    /// </summary>
    private async Task TryAutoLoginAsync()
    {
        Log("TryAutoLogin START");

        var rememberMe = Preferences.Get("remember_me", false);
        if (!rememberMe) { Log("remember_me=false -> skip"); return; }

        var savedPropertyId = Preferences.Get("property_id", "");
        var savedUsername = Preferences.Get("username", "");

        if (string.IsNullOrEmpty(savedPropertyId) || string.IsNullOrEmpty(savedUsername))
        { Log("no saved credentials -> skip"); return; }

        Log($"credentials: prop={savedPropertyId} user={savedUsername}");

        string? savedPassword = null;
        try
        {
            Log("SecureStorage.GetAsync START");
            savedPassword = await SecureStorage.GetAsync("password");
            Log($"SecureStorage.GetAsync DONE (has pw: {!string.IsNullOrEmpty(savedPassword)})");
        }
        catch (Exception ex) { Log($"SecureStorage ERROR: {ex.Message}"); }

        if (string.IsNullOrEmpty(savedPassword))
        { Log("no saved password -> skip"); return; }

        if (!int.TryParse(savedPropertyId, out int propertyId))
        { Log("invalid property_id -> skip"); return; }

        // Check if biometric login is enabled and available
        Log("check biometric");
        bool useBiometric = _biometricService.IsBiometricLoginEnabled();
        bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync();
        Log($"biometric: enabled={useBiometric}, available={biometricAvailable}");

        if (useBiometric && biometricAvailable)
        {
            var biometricType = await _biometricService.GetBiometricTypeAsync();
            LoginButton.IsEnabled = false;
            LoginButton.Text = $"{biometricType}...";
            Log($"biometric prompt: {biometricType}");

            var authenticated = await _biometricService.AuthenticateAsync($"Anmelden als {savedUsername}");
            Log($"biometric result: {authenticated}");

            if (!authenticated)
            {
                LoginButton.IsEnabled = true;
                LoginButton.Text = Translations.Get("login_title");
                Log("biometric failed -> abort");
                return;
            }
        }

        // Show auto-login state
        LoginButton.IsEnabled = false;
        LoginButton.Text = Translations.Get("loading");
        Log("LoginAsync START (sync via Task.Run)");

        try
        {
            var result = await _apiService.LoginAsync(propertyId, savedUsername, savedPassword);
            Log($"LoginAsync DONE: success={result?.Success}");

            if (result == null)
            {
                Log("Login result is null");
                ShowError(Translations.Get("connection_error"));
                return;
            }

            if (result.Success)
            {
                Log("Login SUCCESS - applying language");
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;
                Log($"language set to: {language}");

                Log("InitializeWebSocketAsync");
                _ = App.InitializeWebSocketAsync();
                Log("WebSocket fire-and-forget done");

                Log("GoToAsync START");
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                Log("GoToAsync DONE");
                return;
            }
            else
            {
                Log($"Login FAILED: {result.ErrorMessage}");
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RememberMeCheckbox.IsChecked = false;
                PasswordEntry.Text = "";
                ShowError(result.ErrorMessage ?? Translations.Get("connection_error"));
            }
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[Login] Auto-login error: {ex}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = Translations.Get("login_title");
            Log("TryAutoLogin END");
        }
    }

    private void OnRememberMeLabelTapped(object sender, EventArgs e)
    {
        RememberMeCheckbox.IsChecked = !RememberMeCheckbox.IsChecked;
    }

    /// <summary>
    /// Manueller Login: v1.06 Modell — direkter await, kein .WaitAsync(), kein CancellationToken.
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        Log("ManualLogin START");

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
        Log("LoginAsync START");

        try
        {
            var result = await _apiService.LoginAsync(
                propertyId,
                UsernameEntry.Text,
                PasswordEntry.Text);
            Log($"LoginAsync DONE: success={result?.Success}");

            if (result == null)
            {
                Log("Login result is null");
                ShowError(Translations.Get("connection_error"));
                return;
            }

            if (result.Success)
            {
                Log("Login SUCCESS - saving credentials");
                Preferences.Set("property_id", PropertyIdEntry.Text);
                Log("property_id saved");
                Preferences.Set("username", UsernameEntry.Text);
                Log("username saved");
                Preferences.Set("is_logged_in", true);
                Log("is_logged_in saved");

                if (RememberMeCheckbox.IsChecked)
                {
                    Preferences.Set("remember_me", true);
                    try
                    {
                        Log("SecureStorage.SetAsync START");
                        await SecureStorage.SetAsync("password", PasswordEntry.Text);
                        Log("SecureStorage.SetAsync DONE");
                    }
                    catch (Exception ex)
                    {
                        Log($"SecureStorage save error: {ex.Message}");
                    }
                }
                else
                {
                    Preferences.Set("remember_me", false);
                    SecureStorage.Remove("password");
                }

                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;
                Log($"language set to: {language}");

                Log("PromptBiometric START");
                await PromptForBiometricLoginAsync();
                Log("PromptBiometric DONE");

                Log("InitializeWebSocketAsync");
                _ = App.InitializeWebSocketAsync();
                Log("WebSocket fire-and-forget done");

                Log("GoToAsync START");
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                Log("GoToAsync DONE");
            }
            else
            {
                Log($"Login FAILED: {result.ErrorMessage}");
                ShowError(result.ErrorMessage ?? Translations.Get("error"));
            }
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            ShowError($"{Translations.Get("error")}: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = Translations.Get("login_title");
            Log("ManualLogin END");
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async Task PromptForBiometricLoginAsync()
    {
        if (!RememberMeCheckbox.IsChecked)
            return;

        if (_biometricService.IsBiometricLoginEnabled())
            return;

        bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync();
        if (!biometricAvailable)
            return;

        var biometricType = await _biometricService.GetBiometricTypeAsync();

        var enableBiometric = await DisplayAlertAsync(
            biometricType,
            $"Moechten Sie {biometricType} fuer zukuenftige Anmeldungen aktivieren?",
            "Ja",
            "Nein");

        if (enableBiometric)
        {
            var authenticated = await _biometricService.AuthenticateAsync($"{biometricType} einrichten");

            if (authenticated)
            {
                _biometricService.SetBiometricLoginEnabled(true);
                Log($"{biometricType} enabled");
            }
        }
    }
}
