using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Anmeldeseite.
///
/// Die automatische Anmeldung (inkl. Offline-Anmeldung) liegt in
/// LoginPage.AutoAnmeldung.cs, die Anmeldung von Hand in LoginPage.Anmeldung.cs.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly BiometricService _biometricService;
    private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();

    private bool _autoLoginAttempted;

    // true sobald zur TodayPage navigiert wurde: danach dürfen die Controls
    // dieser Seite nicht mehr angefasst werden - ein Button-Update auf der
    // gerade verlassenen Seite löst auf iOS einen Layout-Pass auf abgebauten
    // Views aus (NullReferenceException in Button.LayoutButton, Crashes 14.07.2026)
    private bool _navigiert;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _biometricService = BiometricService.Instance;

        // Version dynamisch aus der echten Build-Version (statt hartcodiert)
        try
        {
            VersionLabel.Text = $"v{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login] Version: {ex.Message}");
            VersionLabel.Text = "";
        }

        Translations.LoadFromPreferences();
        ApplyTranslations();

        _ = LadeGespeicherteZugangsdatenAsync();
    }

    private void ApplyTranslations()
    {
        SubtitleLabel.Text = Translations.Get("login_subtitle");

        // Eingabefelder
        PropertyIdLabel.Text = Translations.Get("login_property_id");
        UsernameLabel.Text = Translations.Get("login_username");
        PasswordLabel.Text = Translations.Get("login_password");
        RememberMeLabel.Text = Translations.Get("login_remember_me");
        LoginButton.Text = Translations.Get("login_title");

        // Hinweistexte
        EnterpriseAppLabel.Text = Translations.Get("login_enterprise_app");
        CredentialsInfoLabel.Text = Translations.Get("login_credentials_info");
        NewCustomersLabel.Text = Translations.Get("login_new_customers");
        RegistrationInfoLabel.Text = Translations.Get("login_registration_info");
        TestUsageLabel.Text = Translations.Get("login_test_usage");
        TestCredentialsLabel.Text = Translations.Get("login_test_credentials");
    }

    private void Log(string msg)
    {
        var zeile = $"[LOGIN] [{_sw.ElapsedMilliseconds}ms] {msg}";
        System.Diagnostics.Debug.WriteLine(zeile);
        // Datei-Protokoll nebenher, damit die Anmeldung nicht wartet
        _ = Task.Run(() => ApiService.WriteLog(zeile));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Seite ist (wieder) sichtbar, z.B. nach Abmelden: Knopf-Zustand
            // zurücksetzen, den das finally der Anmeldewege nach erfolgreicher
            // Navigation bewusst nicht mehr anfasst (iOS-Layout-Crash-Fix)
            _navigiert = false;
            KnopfFreigeben();

            // Neues Datei-Protokoll starten (löscht die alte Datei)
            ApiService.InitFileLogging();

            // Automatische Anmeldung nur einmal je App-Start versuchen
            if (!_autoLoginAttempted)
            {
                _autoLoginAttempted = true;
                await TryAutoLoginAsync();
            }
        }
        catch (Exception ex)
        {
            // async void Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[LoginPage] OnAppearing error: {ex.Message}");
        }
    }

    /// <summary>Gespeicherte Zugangsdaten in die Felder übernehmen.</summary>
    private async Task LadeGespeicherteZugangsdatenAsync()
    {
        var firma = Preferences.Get("property_id", "");
        var benutzer = Preferences.Get("username", "");
        var merken = Preferences.Get("remember_me", true);  // bei Neuinstallation an

        if (!string.IsNullOrEmpty(firma))
            PropertyIdEntry.Text = firma;
        if (!string.IsNullOrEmpty(benutzer))
            UsernameEntry.Text = benutzer;

        RememberMeCheckbox.IsChecked = merken;

        if (!merken) return;

        try
        {
            var kennwort = await SecureStorage.GetAsync("password");
            if (!string.IsNullOrEmpty(kennwort))
                PasswordEntry.Text = kennwort;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login] SecureStorage error: {ex.Message}");
        }
    }

    private void OnRememberMeLabelTapped(object sender, EventArgs e)
    {
        RememberMeCheckbox.IsChecked = !RememberMeCheckbox.IsChecked;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    /// <summary>Anmeldeknopf wieder benutzbar machen.</summary>
    private void KnopfFreigeben()
    {
        LoginButton.IsEnabled = true;
        LoginButton.Text = Translations.Get("login_title");
    }
}
