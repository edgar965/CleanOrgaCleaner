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
        var logLine = $"[LOGIN] [{_sw.ElapsedMilliseconds}ms] {msg}";
        System.Diagnostics.Debug.WriteLine(logLine);

        // Fire-and-forget file logging (non-blocking)
        _ = Task.Run(() => ApiService.WriteLog(logLine));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Zeige Logs vom letzten Start (falls vorhanden)
        var previousLogs = ApiService.GetPreviousLogs();
        if (!string.IsNullOrEmpty(previousLogs))
        {
            DebugLogLabel.Text = "=== LOGS VOM LETZTEN START ===\n" + previousLogs + "\n=== AKTUELLER START ===\n";
        }

        // Starte neues File-Logging (löscht alte Datei)
        ApiService.InitFileLogging();

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
        var rememberMe = Preferences.Get("remember_me", true);  // Default true bei Neuinstallation

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
        Log("LoginAsync START");

        try
        {
            // 1. Pure login (runs on thread pool)
            var result = await _apiService.LoginAsync(propertyId, savedUsername, savedPassword);
            Log($"LoginAsync DONE: success={result?.Success}");

            // iOS: UI thread braucht Zeit zum Atmen nach async call
            await Task.Yield();
            Log("after Yield");

            if (result == null)
            {
                Log("result is null");
                ShowError(Translations.Get("connection_error"));
                return;
            }

            if (!result.Success)
            {
                Log($"FAILED: {result.ErrorMessage}");
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RememberMeCheckbox.IsChecked = false;
                PasswordEntry.Text = "";
                ShowError(result.ErrorMessage ?? Translations.Get("connection_error"));
                return;
            }

            // 2. Login OK - setup on UI thread, step by step
            Log("setting language");
            var language = result.CleanerLanguage ?? "de";
            Preferences.Set("language", language);
            Translations.CurrentLanguage = language;
            Log($"language={language}");

            // iOS: UI thread atmen lassen
            await Task.Yield();

            Log("StartHeartbeat");
            _apiService.StartHeartbeat();
            Log("StartHeartbeat DONE");

            // iOS: UI thread atmen lassen
            await Task.Yield();

            Log("InitializeWebSocketAsync");
            _ = App.InitializeWebSocketAsync();
            Log("WebSocket started");

            // iOS: UI thread atmen lassen vor Navigation
            await Task.Yield();

            Log("GoToAsync START");
            await Shell.Current.GoToAsync("//MainTabs/TodayPage");
            Log("GoToAsync DONE");
            return;
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

        // Merke ursprünglichen Checkbox-Status
        var originalRememberMe = RememberMeCheckbox.IsChecked;
        Log($"RememberMe original={originalRememberMe}");

        // Workaround: Checkbox temporär auf true setzen (scheint iOS async zu stabilisieren)
        if (!RememberMeCheckbox.IsChecked)
        {
            RememberMeCheckbox.IsChecked = true;
            Log("RememberMe forced true");
            await Task.Delay(10);
            Log("delay after checkbox");
        }

        Log("LoginAsync START");

        try
        {
            var result = await _apiService.LoginAsync(
                propertyId,
                UsernameEntry.Text,
                PasswordEntry.Text);
            Log($"LoginAsync DONE: success={result?.Success}");

            // iOS: UI thread braucht Zeit zum Atmen nach async call
            await Task.Yield();
            Log("after Yield");

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

                // iOS: UI thread atmen lassen
                await Task.Yield();

                Log("StartHeartbeat");
                _apiService.StartHeartbeat();
                Log("StartHeartbeat DONE");

                // iOS: UI thread atmen lassen
                await Task.Yield();

                Log("PromptBiometric START");
                await PromptForBiometricLoginAsync();
                Log("PromptBiometric DONE");

                // iOS: UI thread atmen lassen
                await Task.Yield();

                Log("InitializeWebSocketAsync");
                _ = App.InitializeWebSocketAsync();
                Log("WebSocket fire-and-forget done");

                // iOS: UI thread atmen lassen vor Navigation
                await Task.Yield();

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
            // Checkbox-Status wiederherstellen
            RememberMeCheckbox.IsChecked = originalRememberMe;
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
        try
        {
            Log("PromptBiometric: check RememberMe");
            if (!RememberMeCheckbox.IsChecked)
            {
                Log("PromptBiometric: RememberMe=false, skip");
                return;
            }

            Log("PromptBiometric: check IsBiometricLoginEnabled");
            if (_biometricService.IsBiometricLoginEnabled())
            {
                Log("PromptBiometric: already enabled, skip");
                return;
            }

            Log("PromptBiometric: IsBiometricAvailableAsync START");
            bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync();
            Log($"PromptBiometric: available={biometricAvailable}");
            if (!biometricAvailable)
                return;

            Log("PromptBiometric: GetBiometricTypeAsync START");
            var biometricType = await _biometricService.GetBiometricTypeAsync();
            Log($"PromptBiometric: type={biometricType}");

            Log("PromptBiometric: DisplayAlert START");
            var enableBiometric = await DisplayAlert(
                biometricType,
                $"Moechten Sie {biometricType} fuer zukuenftige Anmeldungen aktivieren?",
                "Ja",
                "Nein");
            Log($"PromptBiometric: user chose={enableBiometric}");

            if (enableBiometric)
            {
                Log("PromptBiometric: AuthenticateAsync START");
                var authenticated = await _biometricService.AuthenticateAsync($"{biometricType} einrichten");
                Log($"PromptBiometric: authenticated={authenticated}");

                if (authenticated)
                {
                    _biometricService.SetBiometricLoginEnabled(true);
                    Log($"{biometricType} enabled");
                }
            }
            Log("PromptBiometric: DONE");
        }
        catch (Exception ex)
        {
            Log($"PromptBiometric EXCEPTION: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
