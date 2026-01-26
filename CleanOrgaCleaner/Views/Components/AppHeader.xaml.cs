using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Components;

public partial class AppHeader : ContentView
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private bool _isWorking = false;

    public AppHeader()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;

        // Don't show offline banner on initial load - wait for connection attempt
        // The banner will be shown/hidden by OnConnectionStatusChanged event
        UpdateOfflineBanner(false);
    }

    public async Task InitializeAsync()
    {
        ApplyTranslations();
        UpdateUserInfo();
        _ = LoadWorkStatusAsync();
    }

    public void ApplyTranslations()
    {
        var t = Translations.Get;

        // Menu items with emojis
        MenuTodayButton.Text = "🏠 " + t("today");
        MenuChatButton.Text = "💬 " + t("chat");
        MenuAuftragButton.Text = "📋 " + t("task");
        MenuSettingsButton.Text = "⚙️ " + t("settings");
        MenuLogoutButton.Text = "🚪 " + t("logout");

        // Work stop popup
        WorkStopQuestion.Text = t("cleaning_finished");
        WorkStopYesButton.Text = "✓ " + t("yes");
        WorkStopNoButton.Text = "✗ " + t("no");
        WorkStopCancelButton.Text = t("cancel");

        // Offline label
        OfflineLabel.Text = t("offline");
    }

    public void SetPageTitle(string titleKey)
    {
        PageTitleLabel.Text = Translations.Get(titleKey);
    }

    public void SetPageTitleDirect(string title)
    {
        PageTitleLabel.Text = title;
    }

    public void UpdateUserInfo()
    {
        // Show only property ID number
        var propertyId = Preferences.Get("property_id", "");
        UserInfoLabel.Text = propertyId;
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateOfflineBanner(!isConnected);
        });
    }

    public void UpdateOfflineBanner(bool showOffline)
    {
        OfflineBanner.IsVisible = showOffline;
        OfflineSpinner.IsRunning = showOffline;
    }

    #region Work Status

    public async Task LoadWorkStatusAsync()
    {
        try
        {
            var status = await _apiService.GetWorkStatusAsync();
            if (status != null)
            {
                _isWorking = status.IsWorking;
                UpdateWorkButton();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppHeader] Work status error: {ex.Message}");
        }
    }

    private void UpdateWorkButton()
    {
        if (_isWorking)
        {
            WorkToggleButton.Text = "⏹️ Stop";
            WorkToggleButton.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            WorkToggleButton.Text = "▶️ Start";
            WorkToggleButton.BackgroundColor = Color.FromArgb("#2196F3");
        }
    }

    private async void OnWorkToggleClicked(object sender, EventArgs e)
    {
        try
        {
            if (!_isWorking)
            {
                // Start work
                var result = await _apiService.StartWorkAsync();
                if (result.Success)
                {
                    _isWorking = true;
                    UpdateWorkButton();
                }
            }
            else
            {
                // Show stop work popup
                WorkStopPopup.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppHeader] Work toggle error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert(
                Translations.Get("error"),
                ex.Message,
                Translations.Get("ok"));
        }
    }

    private async void OnWorkStopYesClicked(object sender, EventArgs e)
    {
        try
        {
            var success = await _apiService.StopWorkAsync();
            if (success)
            {
                _isWorking = false;
                UpdateWorkButton();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                Translations.Get("error"),
                ex.Message,
                Translations.Get("ok"));
        }
        finally
        {
            WorkStopPopup.IsVisible = false;
        }
    }

    private void OnWorkStopNoClicked(object sender, EventArgs e)
    {
        _isWorking = false;
        UpdateWorkButton();
        WorkStopPopup.IsVisible = false;
    }

    private void OnWorkStopCancelClicked(object sender, EventArgs e)
    {
        WorkStopPopup.IsVisible = false;
    }

    private void OnWorkStopPopupBackgroundTapped(object sender, EventArgs e)
    {
        WorkStopPopup.IsVisible = false;
    }

    #endregion

    #region Menu Handlers

    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
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

    private async void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/SettingsPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            Translations.Get("logout"),
            Translations.Get("really_logout"),
            Translations.Get("yes"),
            Translations.Get("no"));

        if (!confirm)
            return;

        try
        {
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

    #endregion

    // Public method to close menu from outside
    public void CloseMenu()
    {
        MenuOverlayGrid.IsVisible = false;
    }

    // Public property to check if work is active
    public bool IsWorking => _isWorking;
}
