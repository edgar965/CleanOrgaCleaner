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
    private DateTime _pageLoadDate;
    private System.Timers.Timer? _dateCheckTimer;

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

        // Speichere das aktuelle Datum für Datumswechsel-Check
        _pageLoadDate = DateTime.Today;
        StartDateCheckTimer();

        Log("Subscribe OnTaskUpdate");
        // -= vor += : bei doppeltem OnAppearing ohne OnDisappearing (iOS-
        // Modal/Alert über der Seite) sonst mehrfach abonniert
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
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
        StopDateCheckTimer();
    }

    private void StartDateCheckTimer()
    {
        StopDateCheckTimer();
        _dateCheckTimer = new System.Timers.Timer(5 * 60 * 1000); // 5 Minuten
        _dateCheckTimer.Elapsed += OnDateCheckTimerElapsed;
        _dateCheckTimer.AutoReset = true;
        _dateCheckTimer.Start();
        Log("Datumswechsel-Check Timer gestartet (alle 5 Min)");
    }

    private void StopDateCheckTimer()
    {
        if (_dateCheckTimer != null)
        {
            _dateCheckTimer.Stop();
            _dateCheckTimer.Elapsed -= OnDateCheckTimerElapsed;
            _dateCheckTimer.Dispose();
            _dateCheckTimer = null;
        }
    }

    private void OnDateCheckTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (DateTime.Today != _pageLoadDate)
        {
            Log($"Datumswechsel erkannt: {_pageLoadDate:d} -> {DateTime.Today:d}");
            _pageLoadDate = DateTime.Today;
            UiSicher.AufMainThread(async () =>
            {
                await LoadTasksAsync();
                await Header.LoadWorkStatusAsync();
            }, "TodayPage");
        }
    }

    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[TodayPage] Task update received: {updateType}");

        // Reload tasks when task changes, assignment changes, or problem changes
        if (updateType == "task_created" || updateType == "task_updated" || updateType == "task_deleted"
            || updateType == "assignment_update" || updateType == "aufgabe_update"
            || updateType == "image_list_update" || updateType == "problem_update" || updateType == "problem_delete")
        {
            UiSicher.AufMainThread(() => LoadTasksAsync(), "TodayPage");
        }
    }

    private async Task LoadTasksAsync()
    {
        Log("LoadTasksAsync START");
        try
        {
            Log("GetTodayDataAsync call START");
            var data = await _apiService.GetTodayDataAsync().ConfigureAwait(false);
            Log($"GetTodayDataAsync call DONE: {data?.Tasks?.Count ?? 0} tasks");

            Log("Setting _tasks");
            _tasks = data?.Tasks ?? new List<CleaningTask>();
            Log($"_tasks set: {_tasks.Count}");

            // Cache tasks for offline use
            Log("Caching tasks for offline");
            _ = OfflineDataService.Instance.SaveTasksAsync(_tasks);

            // Clear offline mode flag since we're online
            Preferences.Set("offline_mode", false);

            // UI updates must be on main thread
            Log("MainThread.BeginInvoke for UI");
            UiSicher.AufMainThread(() =>
            {
                Log("UI update START");
                NoTasksLabel.Text = Translations.Get("no_tasks");
                Log("BuildTaskGrid START");
                BuildTaskGrid();
                Log("BuildTaskGrid DONE");
                Log("UI update DONE");
            }, "TodayPage");
        }
        catch (Exception ex) when (ex is ServerAntwortFehler || ex is System.Text.Json.JsonException)
        {
            // Server hat GEANTWORTET (Fehlerstatus ODER 200 mit Nicht-JSON-Body,
            // z.B. Proxy-/Captive-Portal-/Deploy-Seite): NICHT auf den evtl.
            // tagealten Cache zurückfallen - sonst arbeitet die Reinigungskraft
            // nach einem falschen Tagesplan.
            Log($"LoadTasksAsync SERVER ERROR: {ex.Message}");
            UiSicher.AufMainThread(() =>
            {
                NoTasksLabel.Text = Translations.Get("connection_error");
                if (_tasks.Count == 0)
                {
                    EmptyStateView.IsVisible = true;
                    TaskRefreshView.IsVisible = false;
                }
            }, "TodayPage");
        }
        catch (Exception ex)
        {
            // Alles andere ist Transportebene (kein Netz, Timeout, DNS, Socket)
            // -> Offline-Cache. Klassifikation über den Exception-TYP, nicht
            // über fehleranfälliges String-Matching der Message.
            Log($"LoadTasksAsync NETWORK ERROR: {ex.Message}");
            await LoadCachedTasksAsync();
        }
        Log("LoadTasksAsync END");
    }

    private async Task LoadCachedTasksAsync()
    {
        Log("LoadCachedTasksAsync START");
        try
        {
            // Allow stale tasks (from yesterday) in offline mode
            var cachedTasks = await OfflineDataService.Instance.LoadCachedTasksAsync(allowStale: true);

            if (cachedTasks != null && cachedTasks.Count > 0)
            {
                Log($"Loaded {cachedTasks.Count} cached tasks");
                _tasks = cachedTasks;

                UiSicher.AufMainThread(() =>
                {
                    NoTasksLabel.Text = Translations.Get("no_tasks");
                    BuildTaskGrid();

                    // Show offline indicator in header
                    Header.UpdateOfflineBanner(true);
                }, "TodayPage");
            }
            else
            {
                Log("No cached tasks available");
                UiSicher.AufMainThread(() =>
                {
                    NoTasksLabel.Text = Translations.Get("no_connection") + "\n" + Translations.Get("network_error_hint");
                    EmptyStateView.IsVisible = true;
                    TaskRefreshView.IsVisible = false;
                }, "TodayPage");
            }
        }
        catch (Exception ex)
        {
            Log($"LoadCachedTasksAsync ERROR: {ex.Message}");
            UiSicher.AufMainThread(() =>
            {
                NoTasksLabel.Text = Translations.Get("connection_error");
                EmptyStateView.IsVisible = true;
                TaskRefreshView.IsVisible = false;
            }, "TodayPage");
        }
        Log("LoadCachedTasksAsync END");
    }


    private void BuildTaskGrid()
    {
        Log("BuildTaskGrid: clearing children");
        TasksStackLayout.Children.Clear();

        if (_tasks.Count == 0)
        {
            Log("BuildTaskGrid: no tasks, showing empty state");
            EmptyStateView.IsVisible = true;
            TaskRefreshView.IsVisible = false;
            return;
        }

        Log($"BuildTaskGrid: building {_tasks.Count} tasks");
        EmptyStateView.IsVisible = false;
        TaskRefreshView.IsVisible = true;

        for (int i = 0; i < _tasks.Count; i++)
        {
            var task = _tasks[i];
            Log($"BuildTaskGrid: task {i+1}/{_tasks.Count} id={task.Id}");
            var taskButton = CreateTaskButton(task);
            TasksStackLayout.Children.Add(taskButton);
        }
        Log("BuildTaskGrid: all tasks added");
    }

    private View CreateTaskButton(CleaningTask task)
    {
        // Full-width button like Django client
        var isCompleted = task.IsCompleted;
        var taskLabel = string.IsNullOrEmpty(task.ApartmentName)
            ? task.Aufgabenart
            : $"{task.ApartmentName}  {task.Aufgabenart}";

        var button = new Button
        {
            Text = isCompleted ? $"✓ {taskLabel}" : taskLabel,
            BackgroundColor = Color.FromArgb(isCompleted ? "#4CAF50" : "#2196F3"),
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 15,
            Padding = new Thickness(25, 18),
            HorizontalOptions = LayoutOptions.Fill
        };

        // Shadow effect
        button.Shadow = new Shadow
        {
            Brush = Color.FromArgb(isCompleted ? "#4CAF50" : "#2196F3"),
            Offset = new Point(0, 3),
            Radius = 10,
            Opacity = isCompleted ? 0.35f : 0.3f
        };

        button.Clicked += async (s, e) => await OnTaskTapped(task);

        return button;
    }

    private async Task OnTaskTapped(CleaningTask task)
    {
        try
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
            if (Shell.Current == null) return;
            await Shell.Current.GoToAsync($"AufgabePage?taskId={task.Id}");
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            Log($"OnTaskTapped error: {ex.Message}");
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await LoadTasksAsync();
            await Header.LoadWorkStatusAsync();
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            Log($"OnRefreshing error: {ex.Message}");
        }
        finally
        {
            try { TaskRefreshView.IsRefreshing = false; } catch { }
        }
    }
}
