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

    private static void Log(string msg)
    {
        var line = $"[TODAY] {msg}";
        System.Diagnostics.Debug.WriteLine(line);
        _ = Task.Run(() => ApiService.WriteLog(line));
    }

    public TodayPage()
    {
        Log("Constructor START");
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
        Log("Constructor DONE");
    }

    protected override async void OnAppearing()
    {
        Log("OnAppearing START");
        base.OnAppearing();

        Log("Subscribe OnTaskUpdate");
        _webSocketService.OnTaskUpdate += OnTaskUpdate;

        Log("Header.InitializeAsync fire-and-forget");
        _ = Header.InitializeAsync();

        Log("Header.SetPageTitle");
        Header.SetPageTitle("today");

        Log("WebSocket fire-and-forget");
        _ = App.InitializeWebSocketAsync();

        Log("LoadTasksAsync fire-and-forget");
        _ = LoadTasksAsync();

        Log("OnAppearing DONE");
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
        Log("LoadTasksAsync START");
        try
        {
            Log("GetTodayDataAsync START");
            var data = await _apiService.GetTodayDataAsync();
            Log($"GetTodayDataAsync DONE: {data?.Tasks?.Count ?? 0} tasks");
            _tasks = data.Tasks;

            // Apply translations for page-specific elements
            NoTasksLabel.Text = Translations.Get("no_tasks");

            Log("BuildTaskGrid START");
            BuildTaskGrid();
            Log("BuildTaskGrid DONE");
        }
        catch (Exception ex)
        {
            Log($"LoadTasksAsync ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"LoadTasks error: {ex.Message}");
            // Don't use DisplayAlertAsync in fire-and-forget - it deadlocks iOS Shell navigation
            NoTasksLabel.Text = Translations.Get("connection_error");
            EmptyStateView.IsVisible = true;
            TaskRefreshView.IsVisible = false;
        }
        Log("LoadTasksAsync END");
    }

    private void BuildTaskGrid()
    {
        TasksStackLayout.Children.Clear();

        if (_tasks.Count == 0)
        {
            Log("BuildTaskGrid: no tasks, showing empty state");
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
            await DisplayAlertAsync(
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
