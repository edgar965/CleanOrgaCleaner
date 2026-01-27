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

    // ================================================================
    // v1.06 THREADING MODEL: Alles auf Main Thread mit async/await.
    // Jeder await gibt dem iOS RunLoop Luft → UI bleibt responsive.
    // KEIN Task.Run, KEIN ConfigureAwait(false), KEIN InvokeOnMainThreadAsync.
    // ================================================================

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

    /// <summary>
    /// Debug-Log auf dem Login-Screen anzeigen.
    /// Läuft auf Main Thread → direkter UI-Zugriff.
    /// </summary>
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
    /// Auto-Login: Läuft auf Main Thread (v1.06 Modell).
    /// Jeder await gibt dem RunLoop Luft.
    /// </summary>
    private async Task TryAutoLoginAsync()
    {
        Log("TryAutoLogin START");

        var rememberMe = Preferences.Get("remember_me", false);
        if (!rememberMe) { Log("remember_me=false → skip"); return; }

        var savedPropertyId = Preferences.Get("property_id", "");
        var savedUsername = Preferences.Get("username", "");

        if (string.IsNullOrEmpty(savedPropertyId) || string.IsNullOrEmpty(savedUsername))
        { Log("no saved credentials → skip"); return; }

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
        { Log("no saved password → skip"); return; }

        if (!int.TryParse(savedPropertyId, out int propertyId))
        { Log("invalid property_id → skip"); return; }

        // Check biometric
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
                Log("biometric failed → abort");
                return;
            }
        }

        // Auto-login
        LoginButton.IsEnabled = false;
        LoginButton.Text = Translations.Get("loading");
        Log("LoginAsync START");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
            var result = await _apiService.LoginAsync(propertyId, savedUsername, savedPassword)
                .WaitAsync(cts.Token);
            Log($"LoginAsync DONE: success={result?.Success}");

            if (result?.Success == true)
            {
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;

                _ = App.InitializeWebSocketAsync();

                Log("GoToAsync START");
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                Log("GoToAsync DONE");
                return;
            }
            else
            {
                Log($"login failed: {result?.ErrorMessage}");
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RememberMeCheckbox.IsChecked = false;
                PasswordEntry.Text = "";
                ShowError(result?.ErrorMessage ?? Translations.Get("connection_error"));
            }
        }
        catch (OperationCanceledException)
        {
            Log($"TIMEOUT after {LoginTimeoutSeconds}s");
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
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
    /// Manueller Login: v1.06 Modell — alles auf Main Thread mit async/await.
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
            var result = await _apiService.LoginAsync(
                propertyId,
                UsernameEntry.Text,
                PasswordEntry.Text)
                .WaitAsync(cts.Token);
            Log($"LoginAsync DONE: success={result?.Success}");

            if (result.Success)
            {
                // Save credentials
                Preferences.Set("property_id", PropertyIdEntry.Text);
                Preferences.Set("username", UsernameEntry.Text);
                Preferences.Set("is_logged_in", true);

                if (RememberMeCheckbox.IsChecked)
                {
                    Preferences.Set("remember_me", true);
                    try
                    {
                        await SecureStorage.SetAsync("password", PasswordEntry.Text);
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
                Log($"language: {language}");

                // Biometric prompt (v1.06 Modell)
                Log("PromptBiometric START");
                await PromptForBiometricLoginAsync();
                Log("PromptBiometric DONE");

                // WebSocket
                _ = App.InitializeWebSocketAsync();

                // Navigate
                Log("GoToAsync START");
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                Log("GoToAsync DONE");
            }
            else
            {
                Log($"login failed: {result.ErrorMessage}");
                ShowError(result.ErrorMessage ?? Translations.Get("error"));
            }
        }
        catch (OperationCanceledException)
        {
            Log($"TIMEOUT after {LoginTimeoutSeconds}s");
            ShowError($"Login-Timeout nach {LoginTimeoutSeconds}s. Server antwortet nicht.");
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

        var enableBiometric = await DisplayAlert(
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
