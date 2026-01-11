using System.ComponentModel;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

public partial class MyTasksPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<MyTask> _tasks = new();
    private List<ApartmentInfo> _apartments = new();
    private List<AufgabenartInfo> _aufgabenarten = new();
    private List<CleanerAssignmentInfo> _cleaners = new();
    private MyTask? _currentTask;
    private bool _isNewTask = true;
    private TaskAssignments _assignments = new() { Cleaning = new List<int>(), Check = null, Repare = new List<int>() };

    public MyTasksPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyTranslations();
        await LoadDataAsync();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        MenuButton.Text = t("new_task");
        LogoutButton.Text = t("logout");
        NewTaskButton.Text = "+ " + t("create_task");
        EmptyLabel.Text = t("no_my_tasks");
        MenuTodayBtn.Text = t("today");
        MenuChatBtn.Text = t("chat");
        MenuMyTasksBtn.Text = t("new_task");
        MenuSettingsBtn.Text = t("settings");
        TabDetails.Text = t("details_tab");
        TabAssign.Text = t("assign_tab");
        TabStatus.Text = t("status_tab");
        TabChecklist.Text = t("checklist_tab");
        LabelTaskName.Text = t("task_name_required");
        LabelApartment.Text = t("apartment");
        LabelDate.Text = t("date_required");
        LabelTaskType.Text = t("task_type");
        LabelHint.Text = t("note_hint");
        TaskHinweisEditor.Placeholder = t("optional_hint");
        LabelAssignCleaners.Text = t("assign_cleaners");
        LabelSelectStatus.Text = t("select_status");
        StatusImported.Content = t("status_imported");
        StatusAssigned.Content = t("status_assigned");
        StatusCleaned.Content = t("status_cleaned");
        StatusChecked.Content = t("status_checked");
        LabelChecklist.Text = t("checklist");
        ChecklistEmptyLabel.Text = t("select_checklist_hint");
        BtnCancel.Text = t("cancel");
        BtnSave.Text = t("save");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var data = await _apiService.GetMyTasksDataAsync();
            if (data.Success)
            {
                _tasks = data.Tasks ?? new List<MyTask>();
                _apartments = data.Apartments ?? new List<ApartmentInfo>();
                _aufgabenarten = data.Aufgabenarten ?? new List<AufgabenartInfo>();

                // Convert cleaners to assignment info
                _cleaners = (data.Cleaners ?? new List<CleanerInfo>())
                    .Select(c => new CleanerAssignmentInfo(c))
                    .ToList();

                UpdateTasksList();
                UpdatePickers();
            }
            else
            {
                await DisplayAlert(Translations.Get("error"), data.Error ?? Translations.Get("connection_error"), Translations.Get("ok"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadData error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), Translations.Get("connection_error"), Translations.Get("ok"));
        }
    }

    private void UpdateTasksList()
    {
        if (_tasks.Count == 0)
        {
            EmptyStateView.IsVisible = true;
            TaskRefreshView.IsVisible = false;
        }
        else
        {
            EmptyStateView.IsVisible = false;
            TaskRefreshView.IsVisible = true;
            TasksCollectionView.ItemsSource = _tasks;
        }
    }

    private void UpdatePickers()
    {
        ApartmentPicker.ItemsSource = _apartments;
        ApartmentPicker.ItemDisplayBinding = new Binding("Name");

        AufgabenartPicker.ItemsSource = _aufgabenarten;
        AufgabenartPicker.ItemDisplayBinding = new Binding("Name");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadDataAsync();
        TaskRefreshView.IsRefreshing = false;
    }

    private void OnTaskSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is MyTask task)
        {
            OpenEditPopup(task);
            TasksCollectionView.SelectedItem = null;
        }
    }

    private void OnNewTaskClicked(object sender, EventArgs e)
    {
        OpenNewTaskPopup();
    }

    private void OpenNewTaskPopup()
    {
        _isNewTask = true;
        _currentTask = null;
        _assignments = new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() };

        PopupTitle.Text = Translations.Get("new_task");
        TaskNameEntry.Text = "";
        ApartmentPicker.SelectedIndex = -1;
        TaskDatePicker.Date = DateTime.Today;
        AufgabenartPicker.SelectedIndex = -1;
        TaskHinweisEditor.Text = "";
        StatusImported.IsChecked = true;

        UpdateCleanersList();
        ShowTab("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private void OpenEditPopup(MyTask task)
    {
        _isNewTask = false;
        _currentTask = task;
        _assignments = task.Assignments ?? new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() };

        PopupTitle.Text = Translations.Get("edit_task");
        TaskNameEntry.Text = task.Name;

        // Set apartment
        if (task.ApartmentId.HasValue)
        {
            var apt = _apartments.FirstOrDefault(a => a.Id == task.ApartmentId.Value);
            ApartmentPicker.SelectedItem = apt;
        }
        else
        {
            ApartmentPicker.SelectedIndex = -1;
        }

        // Set date
        if (DateTime.TryParse(task.PlannedDate, out var date))
        {
            TaskDatePicker.Date = date;
        }

        // Set aufgabenart
        if (task.AufgabenartId.HasValue)
        {
            var art = _aufgabenarten.FirstOrDefault(a => a.Id == task.AufgabenartId.Value);
            AufgabenartPicker.SelectedItem = art;
        }
        else
        {
            AufgabenartPicker.SelectedIndex = -1;
        }

        TaskHinweisEditor.Text = task.WichtigerHinweis ?? "";

        // Set status
        switch (task.Status)
        {
            case "assigned": StatusAssigned.IsChecked = true; break;
            case "cleaned": StatusCleaned.IsChecked = true; break;
            case "checked": StatusChecked.IsChecked = true; break;
            default: StatusImported.IsChecked = true; break;
        }

        UpdateCleanersList();
        ShowTab("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private void UpdateCleanersList()
    {
        foreach (var c in _cleaners)
        {
            c.IsCleaningAssigned = _assignments.Cleaning?.Contains(c.Id) ?? false;
            c.IsCheckAssigned = _assignments.Check == c.Id;
            c.IsRepareAssigned = _assignments.Repare?.Contains(c.Id) ?? false;
        }
        CleanersList.ItemsSource = null;
        CleanersList.ItemsSource = _cleaners;
    }

    private void ShowTab(string tab)
    {
        DetailsTabContent.IsVisible = tab == "details";
        AssignTabContent.IsVisible = tab == "assign";
        StatusTabContent.IsVisible = tab == "status";
        ChecklistTabContent.IsVisible = tab == "checklist";

        TabDetails.TextColor = tab == "details" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabDetails.FontAttributes = tab == "details" ? FontAttributes.Bold : FontAttributes.None;
        TabAssign.TextColor = tab == "assign" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabAssign.FontAttributes = tab == "assign" ? FontAttributes.Bold : FontAttributes.None;
        TabStatus.TextColor = tab == "status" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabStatus.FontAttributes = tab == "status" ? FontAttributes.Bold : FontAttributes.None;
        TabChecklist.TextColor = tab == "checklist" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabChecklist.FontAttributes = tab == "checklist" ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnTabDetailsClicked(object sender, EventArgs e) => ShowTab("details");
    private void OnTabAssignClicked(object sender, EventArgs e) => ShowTab("assign");
    private void OnTabStatusClicked(object sender, EventArgs e) => ShowTab("status");
    private void OnTabChecklistClicked(object sender, EventArgs e) => ShowTab("checklist");
private void OnAufgabenartChanged(object sender, EventArgs e)    {        UpdateChecklistDisplay();    }    private void UpdateChecklistDisplay()    {        ChecklistItemsContainer.Children.Clear();                var selectedArt = AufgabenartPicker.SelectedItem as AufgabenartInfo;        if (selectedArt == null || selectedArt.Checkliste == null || selectedArt.Checkliste.Count == 0)        {            ChecklistEmptyLabel.IsVisible = true;            return;        }                ChecklistEmptyLabel.IsVisible = false;                foreach (var item in selectedArt.Checkliste)        {            var checkItem = new Border            {                Padding = new Thickness(12),                BackgroundColor = Color.FromArgb("#f8f9fa"),                StrokeShape = new RoundRectangle { CornerRadius = 10 }            };                        var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = GridLength.Star } } };                        var checkbox = new Border            {                WidthRequest = 24,                HeightRequest = 24,                StrokeShape = new RoundRectangle { CornerRadius = 12 },                Stroke = Color.FromArgb("#ccc"),                StrokeThickness = 2            };                        var label = new Label { Text = item, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(12, 0, 0, 0) };                        grid.Add(checkbox, 0, 0);            grid.Add(label, 1, 0);            checkItem.Content = grid;                        ChecklistItemsContainer.Children.Add(checkItem);        }    }

    private void OnCleaningToggled(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int cleanerId))
        {
            var cleaner = _cleaners.FirstOrDefault(c => c.Id == cleanerId);
            if (cleaner != null)
            {
                cleaner.IsCleaningAssigned = !cleaner.IsCleaningAssigned;
                if (cleaner.IsCleaningAssigned)
                {
                    if (!_assignments.Cleaning!.Contains(cleanerId))
                        _assignments.Cleaning.Add(cleanerId);
                }
                else
                {
                    _assignments.Cleaning!.Remove(cleanerId);
                }
                UpdateCleanersList();
            }
        }
    }

    private void OnCheckToggled(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int cleanerId))
        {
            // Only one checker allowed
            foreach (var c in _cleaners)
            {
                c.IsCheckAssigned = c.Id == cleanerId && !c.IsCheckAssigned;
            }
            _assignments.Check = _cleaners.FirstOrDefault(c => c.IsCheckAssigned)?.Id;
            UpdateCleanersList();
        }
    }

    private void OnRepareToggled(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int cleanerId))
        {
            var cleaner = _cleaners.FirstOrDefault(c => c.Id == cleanerId);
            if (cleaner != null)
            {
                cleaner.IsRepareAssigned = !cleaner.IsRepareAssigned;
                if (cleaner.IsRepareAssigned)
                {
                    if (!_assignments.Repare!.Contains(cleanerId))
                        _assignments.Repare.Add(cleanerId);
                }
                else
                {
                    _assignments.Repare!.Remove(cleanerId);
                }
                UpdateCleanersList();
            }
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var name = TaskNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert(Translations.Get("error"), Translations.Get("task_name_required"), Translations.Get("ok"));
            return;
        }

        var plannedDate = $"{TaskDatePicker.Date:yyyy-MM-dd}";
        int? apartmentId = (ApartmentPicker.SelectedItem as ApartmentInfo)?.Id;
        int? aufgabenartId = (AufgabenartPicker.SelectedItem as AufgabenartInfo)?.Id;
        var hinweis = TaskHinweisEditor.Text;

        var status = "imported";
        if (StatusAssigned.IsChecked) status = "assigned";
        else if (StatusCleaned.IsChecked) status = "cleaned";
        else if (StatusChecked.IsChecked) status = "checked";

        // Check if we're offline
        var isOffline = Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

        if (isOffline)
        {
            // Queue the operation for later sync
            var offlineQueue = OfflineQueueService.Instance;
            if (_isNewTask)
            {
                await offlineQueue.EnqueueTaskCreateAsync(name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
            }
            else
            {
                await offlineQueue.EnqueueTaskUpdateAsync(_currentTask!.Id, name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
            }

            TaskPopupOverlay.IsVisible = false;
            await DisplayAlert("Offline", Translations.Get("saved"), Translations.Get("ok"));
            return;
        }

        ApiResponse result;
        if (_isNewTask)
        {
            result = await _apiService.CreateMyTaskAsync(name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
        }
        else
        {
            result = await _apiService.UpdateMyTaskAsync(_currentTask!.Id, name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
        }

        if (result.Success)
        {
            TaskPopupOverlay.IsVisible = false;
            await LoadDataAsync();
        }
        else
        {
            // If network error, queue for offline sync
            if (result.Error?.Contains("network", StringComparison.OrdinalIgnoreCase) == true ||
                result.Error?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true ||
                result.Error?.Contains("connection", StringComparison.OrdinalIgnoreCase) == true)
            {
                var offlineQueue = OfflineQueueService.Instance;
                if (_isNewTask)
                {
                    await offlineQueue.EnqueueTaskCreateAsync(name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
                }
                else
                {
                    await offlineQueue.EnqueueTaskUpdateAsync(_currentTask!.Id, name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
                }

                TaskPopupOverlay.IsVisible = false;
                await DisplayAlert("Offline", Translations.Get("saved"), Translations.Get("ok"));
            }
            else
            {
                await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("task_update_error"), Translations.Get("ok"));
            }
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        TaskPopupOverlay.IsVisible = false;
    }

    private void OnClosePopupClicked(object sender, EventArgs e)
    {
        TaskPopupOverlay.IsVisible = false;
    }

    private void OnPopupOverlayTapped(object sender, EventArgs e)
    {
        // Don't close on overlay tap for popup
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

    private void OnMenuMyTasksClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        // Already here
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
            Preferences.Remove("is_logged_in");
            Preferences.Remove("property_id");
            Preferences.Remove("username");
            Preferences.Remove("remember_me");
            SecureStorage.Remove("password");
            _apiService.Logout();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    #endregion
}

/// <summary>
/// Helper class for cleaner assignment UI
/// </summary>
public class CleanerAssignmentInfo : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Initial => Name.Length > 0 ? Name.Substring(0, 1).ToUpper() : "?";

    private bool _isCleaningAssigned;
    public bool IsCleaningAssigned
    {
        get => _isCleaningAssigned;
        set { _isCleaningAssigned = value; OnPropertyChanged(); OnPropertyChanged(nameof(CleaningBgColor)); OnPropertyChanged(nameof(CleaningTextColor)); }
    }

    private bool _isCheckAssigned;
    public bool IsCheckAssigned
    {
        get => _isCheckAssigned;
        set { _isCheckAssigned = value; OnPropertyChanged(); OnPropertyChanged(nameof(CheckBgColor)); OnPropertyChanged(nameof(CheckTextColor)); }
    }

    private bool _isRepareAssigned;
    public bool IsRepareAssigned
    {
        get => _isRepareAssigned;
        set { _isRepareAssigned = value; OnPropertyChanged(); OnPropertyChanged(nameof(RepareBgColor)); OnPropertyChanged(nameof(RepareTextColor)); }
    }

    public Color CleaningBgColor => IsCleaningAssigned ? Color.FromArgb("#ffc107") : Colors.White;
    public Color CleaningTextColor => IsCleaningAssigned ? Colors.White : Color.FromArgb("#f57f17");

    public Color CheckBgColor => IsCheckAssigned ? Color.FromArgb("#ff9800") : Colors.White;
    public Color CheckTextColor => IsCheckAssigned ? Colors.White : Color.FromArgb("#e65100");

    public Color RepareBgColor => IsRepareAssigned ? Color.FromArgb("#f44336") : Colors.White;
    public Color RepareTextColor => IsRepareAssigned ? Colors.White : Color.FromArgb("#c62828");

    public CleanerAssignmentInfo(CleanerInfo cleaner)
    {
        Id = cleaner.Id;
        Name = cleaner.Name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
