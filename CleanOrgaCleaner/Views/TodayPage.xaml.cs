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
        UpdateOfflineBanner(!_webSocketService.IsOnline);

        // Apply translations
        ApplyTranslations();

        // Set date
        DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy");

        // Ensure WebSocket is connected (for auto-login case)
        _ = App.InitializeWebSocketAsync();

        // Load data
        await LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnConnectionStatusChanged -= OnConnectionStatusChanged;
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateOfflineBanner(!isConnected);
        });
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
        WorkButton.Text = _isWorking ? t("stop_work") : t("start_work");
        LogoutButton.Text = t("logout");
        NoTasksLabel.Text = t("no_tasks");

        // Menu button shows current view
        MenuButton.Text = $"{t("today")} ▼";

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

            // Update work status
            _isWorking = data.WorkStatus.IsWorking;
            UpdateWorkButton();

            if (data.WorkStatus.IsWorking && !string.IsNullOrEmpty(data.WorkStatus.StartTime))
            {
                WorkStatusLabel.Text = $"{Translations.Get("working_since")} {data.WorkStatus.StartTime}";
            }
            else
            {
                WorkStatusLabel.Text = "";
            }

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

    private void UpdateWorkButton()
    {
        if (_isWorking)
        {
            WorkButton.Text = Translations.Get("stop_work");
            WorkButton.BackgroundColor = Color.FromArgb("#2196F3");
        }
        else
        {
            WorkButton.Text = Translations.Get("start_work");
            WorkButton.BackgroundColor = Color.FromArgb("#9e9e9e");
        }
    }

    private async void OnWorkButtonClicked(object sender, EventArgs e)
    {
        WorkButton.IsEnabled = false;

        try
        {
            if (_isWorking)
            {
                // Show confirmation dialog with visible cancel button
                var result = await DisplayActionSheet(
                    Translations.Get("cleaning_finished"),
                    null,  // No hidden cancel
                    null,  // No destructive button
                    Translations.Get("yes"),
                    Translations.Get("no"),
                    Translations.Get("cancel"));  // Cancel as visible option

                if (result == Translations.Get("yes"))
                {
                    var response = await _apiService.EndWorkAsync();
                    if (response.Success)
                    {
                        _isWorking = false;
                        UpdateWorkButton();

                        var hours = response.TotalHours?.ToString("F2").Replace(".", ",") ?? "?";
                        WorkStatusLabel.Text = "";

                        await DisplayAlert(Translations.Get("work_ended"),
                            $"{Translations.Get("total_hours")}: {hours}h", Translations.Get("ok"));
                    }
                    else
                    {
                        await DisplayAlert(Translations.Get("error"),
                            response.Error ?? Translations.Get("work_could_not_end"),
                            Translations.Get("ok"));
                    }
                }
            }
            else
            {
                var response = await _apiService.StartWorkAsync();
                if (response.Success)
                {
                    _isWorking = true;
                    UpdateWorkButton();
                    WorkStatusLabel.Text = $"{Translations.Get("working_since")} {response.StartTime}";
                }
                else
                {
                    await DisplayAlert(Translations.Get("error"),
                        response.Error ?? Translations.Get("work_could_not_start"),
                        Translations.Get("ok"));
                }
            }
        }
        finally
        {
            WorkButton.IsEnabled = true;
        }
    }

    private async Task OnTaskTapped(CleaningTask task)
    {
        // Close menu if open
        MenuOverlayGrid.IsVisible = false;

        // Check if work is started
        if (!_isWorking)
        {
            await DisplayAlert(Translations.Get("hint"),
                Translations.Get("start_work_first"),
                Translations.Get("ok"));
            return;
        }

        // Navigate to task detail using Shell navigation
        await Shell.Current.GoToAsync($"TaskDetailPage?taskId={task.Id}");
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
        await Shell.Current.GoToAsync("//MainTabs/ChatPage");
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

        if (confirm)
        {
            // Clear login state
            Preferences.Remove("is_logged_in");
            Preferences.Remove("property_id");
            Preferences.Remove("username");
            Preferences.Remove("remember_me");
            SecureStorage.Remove("password");

            // Clear API service
            _apiService.Logout();

            // Navigate to login
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    #endregion
}
