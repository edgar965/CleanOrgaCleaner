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

        // Set page title
        PageTitleLabel.Text = Translations.Get("today");

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

        // Update UI texts based on current language
        LogoutButton.Text = t("logout");
        NoTasksLabel.Text = t("no_tasks");

        // Menu items - no emojis, fixed text
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
        TasksFlexLayout.Children.Clear();

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
            var taskCard = CreateTaskCard(task);
            TasksFlexLayout.Children.Add(taskCard);
        }
    }

    private View CreateTaskCard(CleaningTask task)
    {
        // Card container
        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
            Padding = 0,
            Margin = new Thickness(5),
            WidthRequest = 160,
            HeightRequest = 120,
            Shadow = new Shadow { Brush = Colors.Gray, Offset = new Point(2, 2), Radius = 5, Opacity = 0.3f }
        };

        // Add border for completed tasks
        if (task.IsCompleted)
        {
            border.Stroke = Colors.Black;
        }

        var grid = new Grid
        {
            Padding = 15
        };

        // Background color from task type
        grid.BackgroundColor = task.TaskColor;

        var stack = new VerticalStackLayout
        {
            Spacing = 5,
            VerticalOptions = LayoutOptions.Center
        };

        // Apartment name
        var nameLabel = new Label
        {
            Text = task.ApartmentName,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center
        };
        stack.Children.Add(nameLabel);

        // Task type badge
        var badgeBorder = new Border
        {
            BackgroundColor = Color.FromRgba(255, 255, 255, 77),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(8, 4),
            HorizontalOptions = LayoutOptions.Center
        };

        var badgeLabel = new Label
        {
            Text = task.Aufgabenart,
            FontSize = 12,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center
        };
        badgeBorder.Content = badgeLabel;
        stack.Children.Add(badgeBorder);

        // Status indicator
        if (task.IsStarted)
        {
            var statusLabel = new Label
            {
                Text = Translations.Get("running") + "...",
                FontSize = 11,
                TextColor = Colors.White,
                Opacity = 0.8,
                HorizontalTextAlignment = TextAlignment.Center
            };
            stack.Children.Add(statusLabel);
        }
        else if (task.IsCompleted)
        {
            var statusLabel = new Label
            {
                Text = "✓ " + Translations.Get("done"),
                FontSize = 11,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center
            };
            stack.Children.Add(statusLabel);
        }

        grid.Children.Add(stack);
        border.Content = grid;

        // Tap gesture
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) => await OnTaskTapped(task);
        border.GestureRecognizers.Add(tapGesture);

        return border;
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
