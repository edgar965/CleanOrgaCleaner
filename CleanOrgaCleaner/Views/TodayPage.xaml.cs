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

        // Subscribe to task updates
        _webSocketService.OnTaskUpdate += OnTaskUpdate;

        // Initialize header (handles translations, user info, work status, offline banner)
        _ = Header.InitializeAsync();
        Header.SetPageTitle("today");

        // Ensure WebSocket is connected (for auto-login case)
        _ = App.InitializeWebSocketAsync();

        // Load tasks (fire-and-forget to not block UI)
        _ = LoadTasksAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
    }

    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[TodayPage] Task update received: {updateType}");

        // Reload tasks when task changes or assignment changes
        if (updateType == "task_created" || updateType == "task_updated" || updateType == "task_deleted"
            || updateType == "assignment_update" || updateType == "aufgabe_update")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadTasksAsync();
            });
        }
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            var data = await _apiService.GetTodayDataAsync();
            _tasks = data.Tasks;

            // Apply translations for page-specific elements
            NoTasksLabel.Text = Translations.Get("no_tasks");

            // Build task grid
            BuildTaskGrid();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasks error: {ex.Message}");
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
        Header.CloseMenu();

        // Check if work is started
        if (!Header.IsWorking)
        {
            await DisplayAlert(
                Translations.Get("attention"),
                Translations.Get("start_work_first"),
                Translations.Get("ok"));
            return;
        }

        // Navigate to task detail using Shell navigation
        await Shell.Current.GoToAsync($"AufgabePage?taskId={task.Id}");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadTasksAsync();
        await Header.LoadWorkStatusAsync();
        TaskRefreshView.IsRefreshing = false;
    }
}
