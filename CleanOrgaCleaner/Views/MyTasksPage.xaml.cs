using System.ComponentModel;
using CleanOrgaCleaner.Helpers;
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

    // Image handling
    private List<TaskImageInfo> _taskImages = new();
    private FileResult? _selectedImageFile;
    private TaskImageInfo? _selectedDetailImage;

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
        TabImages.Text = t("images_tab");
        TabAssign.Text = t("assign_tab");
        TabStatus.Text = t("status_tab");
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
        LabelImages.Text = t("images_tab");
        AddImageButton.Text = t("add_image");
        BtnCancel.Text = t("cancel");
        BtnSave.Text = t("save");
        BtnDelete.Text = t("delete_task");
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

                System.Diagnostics.Debug.WriteLine($"[MyTasksPage] Loaded {_cleaners.Count} cleaners");
                foreach (var c in _cleaners)
                {
                    System.Diagnostics.Debug.WriteLine($"[MyTasksPage]   - {c.Id}: {c.Name}");
                }

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
        _taskImages.Clear();

        PopupTitle.Text = Translations.Get("new_task");
        TaskNameEntry.Text = "";
        ApartmentPicker.SelectedIndex = -1;
        TaskDatePicker.Date = DateTime.Today;
        AufgabenartPicker.SelectedIndex = -1;
        TaskHinweisEditor.Text = "";
        StatusImported.IsChecked = true;
        BtnDelete.IsVisible = false;

        // Cleaner werden beim Tab-Klick geladen (iOS-Bug mit unsichtbaren Containern)
        UpdateImagesDisplay();
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

        // Show delete button for existing tasks
        BtnDelete.IsVisible = true;

        // Load images
        LoadTaskImages(task.Id);

        // Cleaner werden beim Tab-Klick geladen (iOS-Bug mit unsichtbaren Containern)
        ShowTab("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private void UpdateCleanersList()
    {
        System.Diagnostics.Debug.WriteLine($"[MyTasksPage] UpdateCleanersList called, _cleaners.Count = {_cleaners.Count}");

        // Manuell aufbauen - zuverlässiger als BindableLayout auf iOS
        CleanersList.Children.Clear();

        foreach (var cleaner in _cleaners)
        {
            cleaner.IsAssigned = _assignments.Cleaning?.Contains(cleaner.Id) ?? false;

            var border = new Border
            {
                Margin = new Thickness(0, 5),
                Padding = new Thickness(12),
                BackgroundColor = Color.FromArgb("#f8f9fa"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }
            };

            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };

            // Avatar
            var avatar = new Border
            {
                WidthRequest = 40,
                HeightRequest = 40,
                BackgroundColor = Color.FromArgb("#4CAF50"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                Content = new Label { Text = cleaner.Initial, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            Grid.SetColumn(avatar, 0);

            // Name
            var nameLabel = new Label { Text = cleaner.Name, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(12, 0, 0, 0) };
            Grid.SetColumn(nameLabel, 1);

            // Button
            var btn = new Button
            {
                Text = "Zuweisen",
                BackgroundColor = cleaner.IsAssigned ? Color.FromArgb("#2196F3") : Colors.White,
                TextColor = cleaner.IsAssigned ? Colors.White : Color.FromArgb("#2196F3"),
                BorderColor = Color.FromArgb("#2196F3"),
                BorderWidth = 2,
                FontSize = 12,
                Padding = new Thickness(12, 6),
                CornerRadius = 8,
                CommandParameter = cleaner.Id
            };
            btn.Clicked += OnAssignToggled;
            Grid.SetColumn(btn, 2);

            grid.Children.Add(avatar);
            grid.Children.Add(nameLabel);
            grid.Children.Add(btn);
            border.Content = grid;
            CleanersList.Children.Add(border);
        }

        System.Diagnostics.Debug.WriteLine($"[MyTasksPage] CleanersList built with {CleanersList.Children.Count} items");
    }

    private void ShowTab(string tab)
    {
        DetailsTabContent.IsVisible = tab == "details";
        ImagesTabContent.IsVisible = tab == "images";
        AssignTabContent.IsVisible = tab == "assign";
        StatusTabContent.IsVisible = tab == "status";

        TabDetails.TextColor = tab == "details" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabDetails.FontAttributes = tab == "details" ? FontAttributes.Bold : FontAttributes.None;
        TabImages.TextColor = tab == "images" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabImages.FontAttributes = tab == "images" ? FontAttributes.Bold : FontAttributes.None;
        TabAssign.TextColor = tab == "assign" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabAssign.FontAttributes = tab == "assign" ? FontAttributes.Bold : FontAttributes.None;
        TabStatus.TextColor = tab == "status" ? Color.FromArgb("#2196F3") : Color.FromArgb("#666");
        TabStatus.FontAttributes = tab == "status" ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnTabDetailsClicked(object sender, EventArgs e) => ShowTab("details");
    private void OnTabImagesClicked(object sender, EventArgs e) => ShowTab("images");
    private void OnTabAssignClicked(object sender, EventArgs e) { ShowTab("assign"); UpdateCleanersList(); }
    private void OnTabStatusClicked(object sender, EventArgs e) => ShowTab("status");
    private void OnAufgabenartChanged(object sender, EventArgs e)
    {
        // No longer need checklist display
    }

    private void OnAssignToggled(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int cleanerId))
        {
            var cleaner = _cleaners.FirstOrDefault(c => c.Id == cleanerId);
            if (cleaner != null)
            {
                cleaner.IsAssigned = !cleaner.IsAssigned;
                if (cleaner.IsAssigned)
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

    #region Delete Task

    private async void OnDeleteTaskClicked(object sender, EventArgs e)
    {
        if (_currentTask == null) return;

        var confirm = await DisplayAlert(
            Translations.Get("delete_task"),
            Translations.Get("confirm_delete_task"),
            Translations.Get("yes"),
            Translations.Get("no"));

        if (!confirm) return;

        var result = await _apiService.DeleteMyTaskAsync(_currentTask.Id);
        if (result.Success)
        {
            TaskPopupOverlay.IsVisible = false;
            await LoadDataAsync();
        }
        else
        {
            await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("task_delete_error"), Translations.Get("ok"));
        }
    }

    #endregion

    #region Image Handling

    private async void LoadTaskImages(int taskId)
    {
        _taskImages.Clear();
        try
        {
            var images = await _apiService.GetTaskImagesAsync(taskId);
            if (images != null)
            {
                _taskImages = images;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTaskImages error: {ex.Message}");
        }
        UpdateImagesDisplay();
    }

    private void UpdateImagesDisplay()
    {
        ImagesStack.Children.Clear();

        if (_taskImages.Count == 0)
        {
            ImagesCountLabel.Text = Translations.Get("no_images");
        }
        else
        {
            ImagesCountLabel.Text = $"{_taskImages.Count} {Translations.Get("images")}";

            foreach (var img in _taskImages)
            {
                var border = new Border
                {
                    WidthRequest = 80,
                    HeightRequest = 80,
                    Margin = new Thickness(0, 0, 8, 8),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Stroke = Color.FromArgb("#e0e0e0")
                };

                var image = new Image
                {
                    Source = img.ThumbnailUrl ?? img.Url,
                    Aspect = Aspect.AspectFill
                };

                border.Content = image;

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OpenImageDetail(img);
                border.GestureRecognizers.Add(tapGesture);

                ImagesStack.Children.Add(border);
            }
        }
    }

    private void OnAddImageClicked(object sender, EventArgs e)
    {
        if (_isNewTask)
        {
            DisplayAlert(Translations.Get("info"), Translations.Get("save_task_first"), Translations.Get("ok"));
            return;
        }

        _selectedImageFile = null;
        ImagePreviewBorder.IsVisible = false;
        ImageNotizEditor.Text = "";
        SaveImageButton.IsEnabled = false;
        ImagePopupOverlay.IsVisible = true;
    }

    private async void OnImageTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                _selectedImageFile = photo;
                ImagePreviewImage.Source = ImageSource.FromFile(photo.FullPath);
                ImagePreviewBorder.IsVisible = true;
                SaveImageButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Camera error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), Translations.Get("camera_error"), Translations.Get("ok"));
        }
    }

    private async void OnImagePickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                _selectedImageFile = photo;
                ImagePreviewImage.Source = ImageSource.FromFile(photo.FullPath);
                ImagePreviewBorder.IsVisible = true;
                SaveImageButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), Translations.Get("gallery_error"), Translations.Get("ok"));
        }
    }

    private async void OnSaveImageClicked(object sender, EventArgs e)
    {
        if (_selectedImageFile == null || _currentTask == null) return;

        try
        {
            // Read original bytes
            using var originalStream = await _selectedImageFile.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream);
            var originalBytes = memoryStream.ToArray();

            // Compress to max 2000px
            var compressedBytes = await ImageHelper.CompressImageAsync(originalBytes);

            // Upload compressed image
            using var uploadStream = new MemoryStream(compressedBytes);
            var result = await _apiService.UploadTaskImageAsync(_currentTask.Id, uploadStream, _selectedImageFile.FileName, ImageNotizEditor.Text);

            if (result.Success)
            {
                ImagePopupOverlay.IsVisible = false;
                LoadTaskImages(_currentTask.Id);
            }
            else
            {
                await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("upload_error"), Translations.Get("ok"));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Upload error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), Translations.Get("upload_error"), Translations.Get("ok"));
        }
    }

    private void OnCancelImageClicked(object sender, EventArgs e)
    {
        ImagePopupOverlay.IsVisible = false;
    }

    private void OnImagePopupBackgroundTapped(object sender, EventArgs e)
    {
        // Don't close on background tap
    }

    private void OpenImageDetail(TaskImageInfo img)
    {
        _selectedDetailImage = img;
        ImageDetailImage.Source = img.Url;
        ImageDetailDatum.Text = img.CreatedAt;
        ImageDetailNotizEditor.Text = img.Note ?? "";
        ImageDetailPopupOverlay.IsVisible = true;
    }

    private async void OnSaveImageDetailClicked(object sender, EventArgs e)
    {
        if (_selectedDetailImage == null) return;

        var result = await _apiService.UpdateTaskImageAsync(_selectedDetailImage.Id, ImageDetailNotizEditor.Text);
        if (result.Success)
        {
            _selectedDetailImage.Note = ImageDetailNotizEditor.Text;
            ImageDetailPopupOverlay.IsVisible = false;
        }
        else
        {
            await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("update_error"), Translations.Get("ok"));
        }
    }

    private async void OnDeleteImageDetailClicked(object sender, EventArgs e)
    {
        if (_selectedDetailImage == null || _currentTask == null) return;

        var confirm = await DisplayAlert(
            Translations.Get("delete_image"),
            Translations.Get("confirm_delete_image"),
            Translations.Get("yes"),
            Translations.Get("no"));

        if (!confirm) return;

        var result = await _apiService.DeleteTaskImageAsync(_selectedDetailImage.Id);
        if (result.Success)
        {
            ImageDetailPopupOverlay.IsVisible = false;
            LoadTaskImages(_currentTask.Id);
        }
        else
        {
            await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("delete_error"), Translations.Get("ok"));
        }
    }

    private void OnCloseImageDetailClicked(object sender, EventArgs e)
    {
        ImageDetailPopupOverlay.IsVisible = false;
    }

    private void OnImageDetailPopupBackgroundTapped(object sender, EventArgs e)
    {
        // Don't close on background tap
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

/// <summary>
/// Helper class for cleaner assignment UI
/// </summary>
public class CleanerAssignmentInfo : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Initial => Name.Length > 0 ? Name.Substring(0, 1).ToUpper() : "?";

    private bool _isAssigned;
    public bool IsAssigned
    {
        get => _isAssigned;
        set { _isAssigned = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssignBgColor)); OnPropertyChanged(nameof(AssignTextColor)); }
    }

    public Color AssignBgColor => IsAssigned ? Color.FromArgb("#2196F3") : Colors.White;
    public Color AssignTextColor => IsAssigned ? Colors.White : Color.FromArgb("#2196F3");

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

/// <summary>
/// Task image information for display
/// </summary>
public class TaskImageInfo
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public string? Note { get; set; }
    public string? CreatedAt { get; set; }
}
