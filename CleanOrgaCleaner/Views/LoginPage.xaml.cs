using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models.Responses;
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Attempt auto-login only once per app session
        if (!_autoLoginAttempted)
        {
            _autoLoginAttempted = true;
            // Fire-and-forget auf Background-Thread → UI bleibt responsive
            _ = Task.Run(() => TryAutoLoginAsync());
        }
    }

    /// <summary>
    /// Sicheres UI-Update von jedem Thread aus.
    /// </summary>
    private void RunOnUI(Action action)
    {
        MainThread.BeginInvokeOnMainThread(action);
    }

    /// <summary>
    /// Debug-Log direkt auf dem Login-Screen anzeigen.
    /// Zeigt Thread-ID, MainThread ja/nein, Timestamp.
    /// </summary>
    private void Log(string msg)
    {
        var tid = Environment.CurrentManagedThreadId;
        var isMain = MainThread.IsMainThread;
        var ms = _sw.ElapsedMilliseconds;
        var line = $"[{ms,5}ms] T{tid}{(isMain ? " UI" : "   ")} | {msg}";

        System.Diagnostics.Debug.WriteLine($"[LOGIN-DBG] {line}");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _logLines.Add(line);
            // Max 30 Zeilen behalten
            if (_logLines.Count > 30)
                _logLines.RemoveAt(0);
            DebugLogLabel.Text = string.Join("\n", _logLines);
        });
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

    /// <summary>
    /// Auto-Login: Läuft komplett auf Background-Thread (via Task.Run in OnAppearing).
    /// ALLE UI-Zugriffe via RunOnUI / MainThread.InvokeOnMainThreadAsync.
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
        UpdateStatus("Lade Zugangsdaten...");

        string? savedPassword = null;
        try
        {
            Log("SecureStorage.GetAsync START");
            savedPassword = await SecureStorage.GetAsync("password").ConfigureAwait(false);
            Log($"SecureStorage.GetAsync DONE (has pw: {!string.IsNullOrEmpty(savedPassword)})");
        }
        catch (Exception ex) { Log($"SecureStorage ERROR: {ex.Message}"); }

        if (string.IsNullOrEmpty(savedPassword))
        { Log("no saved password → skip"); return; }

        if (!int.TryParse(savedPropertyId, out int propertyId))
        { Log("invalid property_id → skip"); return; }

        Log("Pruefe Biometrie...");
        UpdateStatus("Pruefe Biometrie...");
        bool useBiometric = _biometricService.IsBiometricLoginEnabled();
        Log($"biometric enabled: {useBiometric}");

        bool biometricAvailable = await _biometricService.IsBiometricAvailableAsync().ConfigureAwait(false);
        Log($"biometric available: {biometricAvailable}");

        if (useBiometric && biometricAvailable)
        {
            var biometricType = await _biometricService.GetBiometricTypeAsync().ConfigureAwait(false);
            Log($"biometric type: {biometricType}");
            RunOnUI(() =>
            {
                LoginButton.IsEnabled = false;
                LoginButton.Text = "⏳";
            });
            UpdateStatus($"{biometricType}...");

            Log("biometric AuthenticateAsync START (MainThread)");
            var authenticated = await MainThread.InvokeOnMainThreadAsync(
                () => _biometricService.AuthenticateAsync($"Anmelden als {savedUsername}"));
            Log($"biometric AuthenticateAsync DONE: {authenticated}");

            if (!authenticated)
            {
                RunOnUI(() =>
                {
                    LoginButton.IsEnabled = true;
                    LoginButton.Text = Translations.Get("login_title");
                    StatusLabel.IsVisible = false;
                });
                Log("biometric failed → abort");
                return;
            }
        }

        RunOnUI(() =>
        {
            LoginButton.IsEnabled = false;
            LoginButton.Text = "⏳";
        });
        UpdateStatus("Auto-Login...");

        try
        {
            Log("LoginAsync START");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
            var result = await _apiService.LoginAsync(propertyId, savedUsername, savedPassword,
                step =>
                {
                    Log($"progress: {step}");
                    UpdateStatus(step);
                }).WaitAsync(cts.Token).ConfigureAwait(false);
            Log($"LoginAsync DONE: success={result?.Success}, name={result?.CleanerName}");

            if (result?.Success == true)
            {
                UpdateStatus("Login OK - lade App...");
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;

                Log("GoToAsync START (MainThread)");
                await MainThread.InvokeOnMainThreadAsync(
                    () => Shell.Current.GoToAsync("//MainTabs/TodayPage"));
                Log("GoToAsync DONE");
                _ = App.InitializeWebSocketAsync();
                return;
            }
            else if (result == null)
            {
                Log("result=null (timeout) → navigate anyway");
                await MainThread.InvokeOnMainThreadAsync(
                    () => Shell.Current.GoToAsync("//MainTabs/TodayPage"));
                return;
            }
            else
            {
                Log($"login failed: {result.ErrorMessage}");
                UpdateStatus($"Fehler: {result.ErrorMessage}");
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RunOnUI(() =>
                {
                    RememberMeCheckbox.IsChecked = false;
                    PasswordEntry.Text = "";
                    ShowError(result.ErrorMessage ?? Translations.Get("connection_error"));
                });
            }
        }
        catch (OperationCanceledException)
        {
            Log("TIMEOUT → navigate anyway");
            UpdateStatus("Timeout - lade App...");
            await MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.GoToAsync("//MainTabs/TodayPage"));
            return;
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            UpdateStatus($"Fehler: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.GoToAsync("//MainTabs/TodayPage"));
            return;
        }
        finally
        {
            RunOnUI(() =>
            {
                LoginButton.IsEnabled = true;
                LoginButton.Text = Translations.Get("login_title");
            });
            Log("TryAutoLogin END");
        }
    }

    private void OnRememberMeLabelTapped(object sender, EventArgs e)
    {
        RememberMeCheckbox.IsChecked = !RememberMeCheckbox.IsChecked;
    }

    /// <summary>
    /// Manueller Login: Validierung auf UI-Thread, dann alles auf Background-Thread.
    /// </summary>
    private void OnLoginClicked(object sender, EventArgs e)
    {
        Log("ManualLogin START");

        // Validate inputs (auf UI-Thread - schnell)
        if (string.IsNullOrWhiteSpace(PropertyIdEntry.Text) ||
            string.IsNullOrWhiteSpace(UsernameEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            Log("validation failed: empty fields");
            ShowError(Translations.Get("error"));
            return;
        }

        if (!int.TryParse(PropertyIdEntry.Text, out int propertyId))
        {
            Log("validation failed: invalid property_id");
            ShowError(Translations.Get("error"));
            return;
        }

        // UI-Werte lesen (auf UI-Thread)
        var uname = UsernameEntry.Text;
        var pwd = PasswordEntry.Text;
        var propText = PropertyIdEntry.Text;
        var rememberMe = RememberMeCheckbox.IsChecked;
        Log($"credentials: prop={propText} user={uname} remember={rememberMe}");

        // UI deaktivieren (auf UI-Thread)
        LoginButton.IsEnabled = false;
        LoginButton.Text = "⏳";
        ErrorLabel.IsVisible = false;
        UpdateStatus("Login...");
        Log("UI disabled, starting background work");

        // Alles Weitere auf Background-Thread → UI bleibt responsive
        _ = Task.Run(async () =>
        {
            Log("Background thread started");
            try
            {
                Log("LoginAsync START");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LoginTimeoutSeconds));
                var result = await _apiService.LoginAsync(propertyId, uname, pwd,
                    step =>
                    {
                        Log($"progress: {step}");
                        UpdateStatus(step);
                    }).WaitAsync(cts.Token).ConfigureAwait(false);
                Log($"LoginAsync DONE: success={result?.Success}, name={result?.CleanerName}");

                if (result.Success)
                {
                    UpdateStatus("Login OK - lade App...");

                    // Credentials speichern
                    Log("saving credentials");
                    Preferences.Set("property_id", propText);
                    Preferences.Set("username", uname);
                    Preferences.Set("is_logged_in", true);

                    if (rememberMe)
                    {
                        Preferences.Set("remember_me", true);
                        try
                        {
                            Log("SecureStorage.SetAsync START");
                            await SecureStorage.SetAsync("password", pwd).ConfigureAwait(false);
                            Log("SecureStorage.SetAsync DONE");
                        }
                        catch (Exception ex)
                        {
                            Log($"SecureStorage save ERROR: {ex.Message}");
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
                    Log($"language set: {language}");

                    // Biometric-Prompt + Navigation auf MainThread
                    Log("BiometricPrompt + GoToAsync START (MainThread)");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        Log("BiometricPrompt START");
                        await PromptForBiometricLoginAsync();
                        Log("BiometricPrompt DONE, GoToAsync START");
                        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                        Log("GoToAsync DONE");
                    });
                    Log("Navigation complete");
                    _ = App.InitializeWebSocketAsync();
                }
                else
                {
                    Log($"login failed: {result.ErrorMessage}");
                    RunOnUI(() =>
                    {
                        UpdateStatus($"Fehler: {result.ErrorMessage}");
                        ShowError(result.ErrorMessage ?? Translations.Get("error"));
                    });
                }
            }
            catch (OperationCanceledException)
            {
                Log($"TIMEOUT after {LoginTimeoutSeconds}s");
                RunOnUI(() =>
                {
                    UpdateStatus($"Timeout nach {LoginTimeoutSeconds}s");
                    ShowError($"Login-Timeout nach {LoginTimeoutSeconds}s. Server antwortet nicht.");
                });
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                RunOnUI(() =>
                {
                    UpdateStatus($"Fehler: {ex.Message}");
                    ShowError($"{Translations.Get("error")}: {ex.Message}");
                });
            }
            finally
            {
                RunOnUI(() =>
                {
                    LoginButton.IsEnabled = true;
                    LoginButton.Text = Translations.Get("login_title");
                });
                Log("ManualLogin END");
            }
        });
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
