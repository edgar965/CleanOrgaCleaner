using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Components;

public partial class AppHeader : ContentView
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private bool _isWorking = false;

    // Event for menu visibility changes
    public event EventHandler<bool>? MenuVisibilityChanged;

    public AppHeader()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;

        // Don't show offline banner on initial load
        UpdateOfflineBanner(false);
    }

    private static void Log(string msg)
    {
        var line = $"[HEADER] {msg}";
        System.Diagnostics.Debug.WriteLine(line);
        _ = Task.Run(() => ApiService.WriteLog(line));
    }

    public async Task InitializeAsync()
    {
        Log("InitializeAsync START");
        ApplyTranslations();
        Log("ApplyTranslations DONE");
        UpdateUserInfo();
        Log("UpdateUserInfo DONE");
        _ = LoadWorkStatusAsync();
        Log("LoadWorkStatusAsync fire-and-forget");
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

        // Update work button text
        UpdateWorkButton();

        // Work stop popup
        WorkStopQuestion.Text = t("cleaning_finished");
        WorkStopYesButton.Text = t("yes");
        WorkStopNoButton.Text = t("no");
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
        var username = _apiService.CleanerName ?? Preferences.Get("username", "");
        UserInfoLabel.Text = username;
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
        Log("LoadWorkStatusAsync START");
        try
        {
            Log("GetWorkStatusAsync call START");
            var status = await _apiService.GetWorkStatusAsync().ConfigureAwait(false);
            Log($"GetWorkStatusAsync call DONE: isWorking={status?.IsWorking}");
            if (status != null)
            {
                _isWorking = status.IsWorking;
                Log("BeginInvokeOnMainThread for UpdateWorkButton");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Log("UpdateWorkButton START");
                    UpdateWorkButton();
                    Log("UpdateWorkButton DONE");
                });
            }
        }
        catch (Exception ex)
        {
            Log($"LoadWorkStatusAsync ERROR: {ex.Message}");
        }
        Log("LoadWorkStatusAsync END");
    }

    private void UpdateWorkButton()
    {
        if (_isWorking)
        {
            WorkToggleButton.Text = Translations.Get("stop");
            WorkToggleButton.BackgroundColor = Color.FromArgb("#E91E63");
        }
        else
        {
            WorkToggleButton.Text = Translations.Get("start");
            WorkToggleButton.BackgroundColor = Color.FromArgb("#4CAF50");
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
                else if (IsNetworkError(result.Error))
                {
                    // Queue for offline sync
                    await OfflineQueueService.Instance.EnqueueWorkStartAsync();
                    _isWorking = true;
                    UpdateWorkButton();
                    await Shell.Current.CurrentPage.DisplayAlertAsync(
                        Translations.Get("no_connection"),
                        Translations.Get("saved_offline"),
                        Translations.Get("ok"));
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
            if (IsNetworkError(ex.Message) && !_isWorking)
            {
                await OfflineQueueService.Instance.EnqueueWorkStartAsync();
                _isWorking = true;
                UpdateWorkButton();
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));
            }
            else
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    Translations.Get("error"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
            }
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
            else
            {
                // Queue for offline sync
                await OfflineQueueService.Instance.EnqueueWorkStopAsync();
                _isWorking = false;
                UpdateWorkButton();
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));
            }
        }
        catch (Exception ex)
        {
            if (IsNetworkError(ex.Message))
            {
                await OfflineQueueService.Instance.EnqueueWorkStopAsync();
                _isWorking = false;
                UpdateWorkButton();
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));
            }
            else
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    Translations.Get("error"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
            }
        }
        finally
        {
            WorkStopPopup.IsVisible = false;
        }
    }

    private static bool IsNetworkError(string? error)
    {
        if (string.IsNullOrEmpty(error)) return false;
        var lowerError = error.ToLowerInvariant();
        return lowerError.Contains("network") || lowerError.Contains("timeout") ||
               lowerError.Contains("timedout") || lowerError.Contains("connection") ||
               lowerError.Contains("internet") || lowerError.Contains("unreachable") ||
               lowerError.Contains("net_http") || lowerError.Contains("failure") ||
               lowerError.Contains("host") || lowerError.Contains("refused");
    }

    private void OnWorkStopNoClicked(object sender, EventArgs e)
    {
        // "No" = Reinigung wurde NICHT vollständig beendet → Arbeit läuft weiter
        // _isWorking bleibt true, Server wird nicht angerufen
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
        MenuVisibilityChanged?.Invoke(this, MenuOverlayGrid.IsVisible);
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        MenuVisibilityChanged?.Invoke(this, false);
    }

    public void HideMenu()
    {
        MenuOverlayGrid.IsVisible = false;
        MenuVisibilityChanged?.Invoke(this, false);
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

        var confirm = await Shell.Current.CurrentPage.DisplayAlertAsync(
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
        try
        {
            Preferences.Remove("property_id");
            Preferences.Remove("username");
            Preferences.Remove("language");
            Preferences.Remove("is_logged_in");
            Preferences.Remove("remember_me");
            Preferences.Remove("biometric_login_enabled");
            Preferences.Remove("offline_mode");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] Preferences clear error: {ex.Message}");
        }

        // Clear secure storage
        try
        {
            SecureStorage.Remove("password");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] SecureStorage clear error: {ex.Message}");
        }

        // Clear offline cached data
        try
        {
            OfflineDataService.Instance.ClearAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] OfflineDataService clear error: {ex.Message}");
        }

        // Disconnect WebSocket (in background to avoid UI thread issues on iOS)
        try
        {
            _ = Task.Run(() =>
            {
                try
                {
                    WebSocketService.Instance.Dispose();
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] WebSocket dispose error: {ex.Message}");
        }

        // Navigate to login page
        try
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Logout] Navigation error: {ex.Message}");
            // Fallback: try Application.Current
            try
            {
                if (Application.Current?.MainPage is Shell shell)
                {
                    await shell.GoToAsync("//LoginPage");
                }
            }
            catch { }
        }
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
