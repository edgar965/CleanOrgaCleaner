using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Today's tasks page - main view after login
/// Shows task grid and work time controls with dropdown menu
/// </summary>
public partial class TodayPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private bool _isWorking = false;
    private List<CleaningTask> _tasks = new();

    public TodayPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;
        _webSocketService.OnTaskUpdate += OnTaskUpdate;
        UpdateOfflineBanner(!_webSocketService.IsOnline);

        // Apply translations
        ApplyTranslations();

        // Ensure WebSocket is connected (for auto-login case)
        _ = App.InitializeWebSocketAsync();

        // Load data
        await LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnConnectionStatusChanged -= OnConnectionStatusChanged;
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateOfflineBanner(!isConnected);
        });
    }

    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[TodayPage] Task update received: {updateType}");

        // Reload tasks when task changes or assignment changes
        if (updateType == "task_created" || updateType == "task_updated" || updateType == "task_deleted"
            || updateType == "assignment_update")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadDataAsync();
            });
        }
    }

    private void UpdateOfflineBanner(bool showOffline)
    {
        OfflineBanner.IsVisible = showOffline;
        OfflineSpinner.IsRunning = showOffline;
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;

        // Header
        MenuButton.Text = "≡";
        LogoutButton.Text = t("logout");
        PageTitleLabel.Text = t("today");

        // Empty state
        NoTasksLabel.Text = t("no_tasks");

        // Menu items
        MenuTodayLabel.Text = t("today");
        MenuChatLabel.Text = t("chat");
        MenuAuftragLabel.Text = t("task");
        MenuSettingsLabel.Text = t("settings");

        System.Diagnostics.Debug.WriteLine($"[TodayPage] Language: {Translations.CurrentLanguage}");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var data = await _apiService.GetTodayDataAsync();
            _tasks = data.Tasks;

            // Update user info
            UserInfoLabel.Text = _apiService.CleanerName ?? Preferences.Get("username", "");

            // Build task grid
            BuildTaskGrid();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadData error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"),
                Translations.Get("connection_error"), Translations.Get("ok"));
        }
    }

    private void BuildTaskGrid()
    {
        TasksStackLayout.Children.Clear();

        if (_tasks.Count == 0)
        {
            EmptyStateView.IsVisible = true;
            TaskRefreshView.IsVisible = false;
            return;
        }

        EmptyStateView.IsVisible = false;
        TaskRefreshView.IsVisible = true;

        foreach (var task in _tasks)
        {
            var taskButton = CreateTaskButton(task);
            TasksStackLayout.Children.Add(taskButton);
        }
    }

    private View CreateTaskButton(CleaningTask task)
    {
        // Full-width button like Django client
        var button = new Button
        {
            Text = string.IsNullOrEmpty(task.ApartmentName)
                ? task.Aufgabenart
                : $"{task.ApartmentName}  {task.Aufgabenart}",
            BackgroundColor = Color.FromArgb("#2196F3"),
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 15,
            Padding = new Thickness(25, 18),
            HorizontalOptions = LayoutOptions.Fill
        };

        // Add border for completed tasks
        if (task.IsCompleted)
        {
            button.BorderColor = Color.FromArgb("#1565c0");
            button.BorderWidth = 3;
        }

        // Shadow effect
        button.Shadow = new Shadow
        {
            Brush = Color.FromArgb("#2196F3"),
            Offset = new Point(0, 3),
            Radius = 10,
            Opacity = 0.3f
        };

        button.Clicked += async (s, e) => await OnTaskTapped(task);

        return button;
    }

    private async Task OnTaskTapped(CleaningTask task)
    {
        // Close menu if open
        MenuOverlayGrid.IsVisible = false;

        // Navigate to task detail using Shell navigation
        await Shell.Current.GoToAsync($"AufgabePage?taskId={task.Id}");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadDataAsync();
        TaskRefreshView.IsRefreshing = false;
    }

    #region Menu Handlers

    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        // Already on TodayPage, just refresh
        await LoadDataAsync();
    }

    private void OnOverlayTapped(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
    }

    private void OnMenuTodayClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        // Already on TodayPage, just refresh
        _ = LoadDataAsync();
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

        var confirm = await DisplayAlert(
            Translations.Get("logout"),
            Translations.Get("really_logout"),
            Translations.Get("yes"),
            Translations.Get("no"));

        if (!confirm)
            return;

        try
        {
            // Call logout API
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
}
