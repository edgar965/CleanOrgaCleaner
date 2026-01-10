using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _autoLoginAttempted = false;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;

        // Load saved credentials
        LoadSavedCredentials();
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

        // Show auto-login state
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Automatisch anmelden...";

        try
        {
            var result = await _apiService.LoginAsync(propertyId, savedUsername, savedPassword);

            if (result.Success)
            {
                // Apply language
                var language = result.CleanerLanguage ?? "de";
                Preferences.Set("language", language);
                Translations.CurrentLanguage = language;

                // Initialize WebSocket
                _ = App.InitializeWebSocketAsync();

                // Navigate to main
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
                return;
            }
            else
            {
                // Auto-login failed, clear saved password
                SecureStorage.Remove("password");
                Preferences.Set("remember_me", false);
                RememberMeCheckbox.IsChecked = false;
                PasswordEntry.Text = "";
                ShowError("Automatische Anmeldung fehlgeschlagen");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Login] Auto-login error: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Anmelden";
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
            ShowError("Bitte alle Felder ausfuellen");
            return;
        }

        if (!int.TryParse(PropertyIdEntry.Text, out int propertyId))
        {
            ShowError("Property ID muss eine Zahl sein");
            return;
        }

        // Show loading state
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Anmelden...";
        ErrorLabel.IsVisible = false;

        try
        {
            var result = await _apiService.LoginAsync(
                propertyId,
                UsernameEntry.Text,
                PasswordEntry.Text);

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

                System.Diagnostics.Debug.WriteLine($"[Login] Language set to: {language}");

                // Initialize WebSocket for chat notifications
                _ = App.InitializeWebSocketAsync();

                // Navigate to main tabs (TodayPage)
                await Shell.Current.GoToAsync("//MainTabs/TodayPage");
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Login fehlgeschlagen");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Fehler: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Anmelden";
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
