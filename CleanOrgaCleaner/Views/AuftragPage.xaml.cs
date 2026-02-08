using System.ComponentModel;
using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

public partial class AuftragPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Auftrag> _tasks = new();
    private List<ApartmentInfo> _apartments = new();
    private List<AufgabenartInfo> _aufgabenarten = new();
    private List<CleanerAssignmentInfo> _cleaners = new();
    private Auftrag? _currentTask;
    private bool _isNewTask = true;
    private string _currentStatus = "imported";
    private TaskAssignments _assignments = new() { Cleaning = new List<int>(), Check = null, Repare = new List<int>() };

    // Anmerkung handling
    private List<ImageListDescription> _anmerkungen = new();
    private ImageListDescription? _currentAnmerkung;
    private List<byte[]> _pendingAnmerkungPhotos = new();

    public AuftragPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Header.InitializeAsync();
        Header.SetPageTitle("task");
        ApplyTranslations();
        await LoadDataAsync();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        // Page-specific translations (header handled by AppHeader)
        NewTaskButton.Text = "+ " + t("create_auftrag");
        EmptyLabel.Text = t("no_my_tasks");
        TabDetails.Text = t("details_tab");
        TabAnmerkungen.Text = t("notes_tab");
        TabAssign.Text = t("assign_tab");
        TabLogs.Text = t("log");
        LabelLogs.Text = t("log");
        NoLogsLabel.Text = t("no_logs");
        LabelTaskName.Text = t("task_name_required");
        LabelApartment.Text = t("apartment");
        LabelDate.Text = t("date_required");
        LabelTaskType.Text = t("task_type");
        LabelHint.Text = t("task_tab");
        TaskHinweisEditor.Placeholder = t("optional_hint");
        LabelAssignCleaners.Text = t("assign_cleaners");
        AddAnmerkungButton.Text = t("add_note");
        NoAnmerkungenLabel.Text = t("no_notes");
        BtnCancel.Text = t("cancel");
        BtnSave.Text = t("save");
        BtnDelete.Text = t("delete_task");
        // ImageListDescriptionDialog translations
        ImageListDescriptionDialogNameLabel.Text = t("name") + " *";
        ImageListDescriptionDialogDescLabel.Text = t("description");
        ImageListDescriptionDialogPhotosLabel.Text = t("photos");
        ImageListDescriptionDialogTakePhotoButton.Text = t("camera");
        ImageListDescriptionDialogPickPhotoButton.Text = t("gallery");
        CancelImageListDescriptionDialogButton.Text = t("cancel");
        SaveImageListDescriptionDialogButton.Text = t("save");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var data = await _apiService.GetAuftragsDataAsync();
            if (data.Success)
            {
                _tasks = data.Tasks ?? new List<Auftrag>();
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
        if (e.CurrentSelection.FirstOrDefault() is Auftrag task)
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
        _anmerkungen.Clear();

        PopupTitle.Text = Translations.Get("create_auftrag");
        TaskNameEntry.Text = "Reparatur";
        ApartmentPicker.SelectedIndex = -1;
        TaskDatePicker.Date = DateTime.Today;
        TaskHinweisEditor.Text = "";
        _currentStatus = "imported";
        BtnDelete.IsVisible = false;

        // Aufgabenart auf "Reparatur" setzen
        var reparaturArt = _aufgabenarten.FirstOrDefault(a => a.Name == "Reparatur");
        if (reparaturArt != null)
            AufgabenartPicker.SelectedItem = reparaturArt;
        else
            AufgabenartPicker.SelectedIndex = -1;

        UpdateCleanersList();
        UpdateAnmerkungenDisplay();
        ShowTab("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private void OpenEditPopup(Auftrag task)
    {
        _isNewTask = false;
        _currentTask = task;
        _assignments = task.Assignments ?? new TaskAssignments { Cleaning = new List<int>(), Check = null, Repare = new List<int>() };

        PopupTitle.Text = Translations.Get("edit_auftrag");
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
        _currentStatus = task.Status ?? "imported";

        // Show delete button for existing tasks
        BtnDelete.IsVisible = true;

        // Load anmerkungen
        LoadAnmerkungen(task.Id);

        UpdateCleanersList();
        ShowTab("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private void UpdateCleanersList()
    {
        foreach (var c in _cleaners)
        {
            c.IsAssigned = _assignments.Cleaning?.Contains(c.Id) ?? false;
        }
        BindableLayout.SetItemsSource(CleanersList, null);
        BindableLayout.SetItemsSource(CleanersList, _cleaners);
    }

    private void ShowTab(string tab)
    {
        DetailsTabContent.IsVisible = tab == "details";
        AnmerkungenTabContent.IsVisible = tab == "anmerkungen";
        AssignTabContent.IsVisible = tab == "assign";
        LogsTabContent.IsVisible = tab == "logs";

        // Update tab button styling (colored background for active, gray for inactive)
        TabDetails.BackgroundColor = tab == "details" ? Color.FromArgb("#2196F3") : Color.FromArgb("#e0e0e0");
        TabDetails.TextColor = tab == "details" ? Colors.White : Color.FromArgb("#666");
        TabDetails.FontAttributes = tab == "details" ? FontAttributes.Bold : FontAttributes.None;
        TabAnmerkungen.BackgroundColor = tab == "anmerkungen" ? Color.FromArgb("#2196F3") : Color.FromArgb("#e0e0e0");
        TabAnmerkungen.TextColor = tab == "anmerkungen" ? Colors.White : Color.FromArgb("#666");
        TabAnmerkungen.FontAttributes = tab == "anmerkungen" ? FontAttributes.Bold : FontAttributes.None;
        TabAssign.BackgroundColor = tab == "assign" ? Color.FromArgb("#2196F3") : Color.FromArgb("#e0e0e0");
        TabAssign.TextColor = tab == "assign" ? Colors.White : Color.FromArgb("#666");
        TabAssign.FontAttributes = tab == "assign" ? FontAttributes.Bold : FontAttributes.None;
        TabLogs.BackgroundColor = tab == "logs" ? Color.FromArgb("#2196F3") : Color.FromArgb("#e0e0e0");
        TabLogs.TextColor = tab == "logs" ? Colors.White : Color.FromArgb("#666");
        TabLogs.FontAttributes = tab == "logs" ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnTabDetailsClicked(object sender, EventArgs e) => ShowTab("details");
    private void OnTabAnmerkungenClicked(object sender, EventArgs e) => ShowTab("anmerkungen");
    private void OnTabAssignClicked(object sender, EventArgs e) => ShowTab("assign");
    private void OnTabLogsClicked(object sender, EventArgs e) => ShowTab("logs");
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

        var status = _currentStatus;

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
            result = await _apiService.CreateAuftragAsync(name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
        }
        else
        {
            result = await _apiService.UpdateAuftragAsync(_currentTask!.Id, name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
        }

        if (result.Success)
        {
            // Close dialog and refresh data for all cases
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

        var result = await _apiService.DeleteAuftragAsync(_currentTask.Id);
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

    #region Anmerkung Handling

    private async void LoadAnmerkungen(int taskId)
    {
        _anmerkungen.Clear();
        try
        {
            var anmerkungen = await _apiService.GetTaskAnmerkungenAsync(taskId);
            if (anmerkungen != null)
            {
                _anmerkungen = anmerkungen;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadAnmerkungen error: {ex.Message}");
        }
        UpdateAnmerkungenDisplay();
    }

    private void UpdateAnmerkungenDisplay()
    {
        AnmerkungenStack.Children.Clear();
        NoAnmerkungenLabel.IsVisible = _anmerkungen.Count == 0;

        foreach (var item in _anmerkungen)
        {
            var border = new Border
            {
                Padding = 10,
                BackgroundColor = Color.FromArgb("#f8f9fa"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Stroke = Color.FromArgb("#e0e0e0")
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(50)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10
            };

            // Thumbnail or icon
            if (item.Photos != null && item.Photos.Count > 0)
            {
                var photo = item.Photos[0];
                var img = new Image
                {
                    Source = photo.ThumbnailUrl ?? photo.Url,
                    Aspect = Aspect.AspectFill,
                    WidthRequest = 50,
                    HeightRequest = 50
                };
                var imgBorder = new Border
                {
                    Content = img,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                    Stroke = Colors.Transparent
                };
                Grid.SetColumn(imgBorder, 0);
                grid.Children.Add(imgBorder);
            }
            else
            {
                var iconLabel = new Label
                {
                    Text = "\U0001F4DD", // Memo emoji
                    FontSize = 24,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(iconLabel, 0);
                grid.Children.Add(iconLabel);
            }

            // Name and description
            var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            textStack.Children.Add(new Label
            {
                Text = item.Name ?? Translations.Get("note"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#333")
            });
            if (!string.IsNullOrEmpty(item.Description))
            {
                var desc = item.Description.Length > 50 ? item.Description[..50] + "..." : item.Description;
                textStack.Children.Add(new Label
                {
                    Text = desc,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#666")
                });
            }
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            // Arrow
            var arrow = new Label
            {
                Text = ">",
                FontSize = 16,
                TextColor = Color.FromArgb("#999"),
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(arrow, 2);
            grid.Children.Add(arrow);

            border.Content = grid;

            var tapGesture = new TapGestureRecognizer();
            var currentItem = item;
            tapGesture.Tapped += (s, e) => OpenAnmerkungDialog(currentItem);
            border.GestureRecognizers.Add(tapGesture);

            AnmerkungenStack.Children.Add(border);
        }
    }

    private async void OnAddAnmerkungClicked(object sender, EventArgs e)
    {
        // If new task, save it first automatically
        if (_isNewTask)
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

            var status = _currentStatus;

            var result = await _apiService.CreateAuftragAsync(name, plannedDate, apartmentId, aufgabenartId, hinweis, status, _assignments);
            if (!result.Success || !result.TaskId.HasValue)
            {
                await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("task_create_error"), Translations.Get("ok"));
                return;
            }

            // Switch to edit mode
            _isNewTask = false;
            _currentTask = new Auftrag { Id = result.TaskId.Value, Name = name };
            PopupTitle.Text = Translations.Get("edit_auftrag");
            BtnDelete.IsVisible = true;
            _ = LoadDataAsync();
        }

        OpenAnmerkungDialog(null);
    }

    private void OpenAnmerkungDialog(ImageListDescription? anmerkung)
    {
        _currentAnmerkung = anmerkung;
        _pendingAnmerkungPhotos.Clear();

        ImageListDescriptionDialogNameEntry.Text = anmerkung?.Name ?? "";
        ImageListDescriptionDialogDescEditor.Text = anmerkung?.Description ?? "";
        UpdateImageListDescriptionDialogCharCount();
        ImageListDescriptionDialogPhotoPreviewStack.Children.Clear();
        ImageListDescriptionDialogPhotoPreviewStack.IsVisible = false;
        ImageListDescriptionDialogPhotoCountLabel.IsVisible = false;

        if (anmerkung != null)
        {
            ImageListDescriptionDialogTitle.Text = Translations.Get("edit_note");
            // Show existing photos
            if (anmerkung.Photos != null && anmerkung.Photos.Count > 0)
            {
                ImageListDescriptionDialogPhotoPreviewStack.IsVisible = true;
                foreach (var photo in anmerkung.Photos)
                {
                    var img = new Image
                    {
                        Source = photo.ThumbnailUrl ?? photo.Url,
                        Aspect = Aspect.AspectFill,
                        WidthRequest = 60,
                        HeightRequest = 60
                    };
                    var imgBorder = new Border
                    {
                        Content = img,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                        Stroke = Colors.Transparent,
                        Margin = new Thickness(0, 0, 5, 5)
                    };
                    ImageListDescriptionDialogPhotoPreviewStack.Children.Add(imgBorder);
                }
            }
        }
        else
        {
            ImageListDescriptionDialogTitle.Text = Translations.Get("add_note");
        }

        ImageListDescriptionDialog.IsVisible = true;
    }

    private void OnImageListDescriptionDialogDescTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateImageListDescriptionDialogCharCount();
    }

    private void UpdateImageListDescriptionDialogCharCount()
    {
        var len = ImageListDescriptionDialogDescEditor.Text?.Length ?? 0;
        ImageListDescriptionDialogCharCountLabel.Text = $"{len} / 300";
    }

    private async void OnImageListDescriptionDialogTakePhotoClicked(object sender, EventArgs e)
    {
        await CaptureOrPickPhotoForAnmerkung(true);
    }

    private async void OnImageListDescriptionDialogPickPhotoClicked(object sender, EventArgs e)
    {
        await CaptureOrPickPhotoForAnmerkung(false);
    }

    private async Task CaptureOrPickPhotoForAnmerkung(bool useCamera)
    {
        try
        {
            byte[]? bytes = null;

            if (useCamera)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert(Translations.Get("error"), "Kamera nicht verfuegbar", Translations.Get("ok"));
                    return;
                }

                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    if (cameraStatus != PermissionStatus.Granted)
                    {
                        await OfferOpenSettingsAsync("Kamera");
                        return;
                    }
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    bytes = memoryStream.ToArray();
                }
            }
            else
            {
                var options = new PickOptions
                {
                    PickerTitle = "Bild auswaehlen",
                    FileTypes = FilePickerFileType.Images
                };
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    bytes = memoryStream.ToArray();
                }
            }

            if (bytes != null)
            {
                // Open annotation page
                var annotationPage = new ImageAnnotationPage(bytes);
                await Navigation.PushModalAsync(annotationPage);

                var tcs = new TaskCompletionSource<bool>();
                annotationPage.Disappearing += (s, ev) => tcs.TrySetResult(true);
                await tcs.Task;

                // Use annotated image if saved, otherwise original
                var finalBytes = annotationPage.WasSaved && annotationPage.AnnotatedImageBytes != null
                    ? annotationPage.AnnotatedImageBytes
                    : bytes;

                // Compress
                var compressedBytes = await ImageHelper.CompressImageAsync(finalBytes);
                _pendingAnmerkungPhotos.Add(compressedBytes);
                UpdatePendingPhotosPreview();
            }
        }
        catch (PermissionException)
        {
            await OfferOpenSettingsAsync("Kamera");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Photo error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), ex.Message, Translations.Get("ok"));
        }
    }

    private async Task OfferOpenSettingsAsync(string permissionName)
    {
        var openSettings = await DisplayAlert($"{permissionName}-Berechtigung",
            $"Die {permissionName}-Berechtigung wurde verweigert.\n\nBitte oeffne die App-Einstellungen und aktiviere die Berechtigung unter 'Berechtigungen'.",
            "Einstellungen oeffnen", "Abbrechen");
        if (openSettings)
        {
            Services.PermissionHelper.OpenAppSettings();
        }
    }

    private void UpdatePendingPhotosPreview()
    {
        // Add new pending photos to preview
        foreach (var photoBytes in _pendingAnmerkungPhotos.Skip(ImageListDescriptionDialogPhotoPreviewStack.Children.Count - (_currentAnmerkung?.Photos?.Count ?? 0)))
        {
            var img = new Image
            {
                Source = ImageSource.FromStream(() => new MemoryStream(photoBytes)),
                Aspect = Aspect.AspectFill,
                WidthRequest = 60,
                HeightRequest = 60
            };
            var imgBorder = new Border
            {
                Content = img,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Stroke = Colors.Transparent,
                Margin = new Thickness(0, 0, 5, 5)
            };
            ImageListDescriptionDialogPhotoPreviewStack.Children.Add(imgBorder);
        }

        ImageListDescriptionDialogPhotoPreviewStack.IsVisible = ImageListDescriptionDialogPhotoPreviewStack.Children.Count > 0;
        ImageListDescriptionDialogPhotoCountLabel.Text = $"{_pendingAnmerkungPhotos.Count} neue(s) Foto(s)";
        ImageListDescriptionDialogPhotoCountLabel.IsVisible = _pendingAnmerkungPhotos.Count > 0;
    }

    private async void OnSaveImageListDescriptionDialogClicked(object sender, EventArgs e)
    {
        var name = ImageListDescriptionDialogNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert(Translations.Get("error"), Translations.Get("name_required"), Translations.Get("ok"));
            return;
        }

        if (_currentTask == null) return;

        try
        {
            var description = ImageListDescriptionDialogDescEditor.Text ?? "";

            if (_currentAnmerkung != null)
            {
                // Update existing
                var result = await _apiService.UpdateImageListDescriptionAsync(_currentAnmerkung.Id, name, description);
                if (!result.Success)
                {
                    await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("update_error"), Translations.Get("ok"));
                    return;
                }

                // Upload new photos
                foreach (var photoBytes in _pendingAnmerkungPhotos)
                {
                    await _apiService.AddPhotoToImageListDescriptionAsync(_currentAnmerkung.Id, photoBytes);
                }
            }
            else
            {
                // Create new
                var result = await _apiService.CreateTaskAnmerkungAsync(_currentTask.Id, name, description, _pendingAnmerkungPhotos);
                if (!result.Success)
                {
                    await DisplayAlert(Translations.Get("error"), result.Error ?? Translations.Get("create_error"), Translations.Get("ok"));
                    return;
                }
            }

            ImageListDescriptionDialog.IsVisible = false;
            LoadAnmerkungen(_currentTask.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save anmerkung error: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), Translations.Get("save_error"), Translations.Get("ok"));
        }
    }

    private void OnCancelImageListDescriptionDialogClicked(object sender, EventArgs e)
    {
        ImageListDescriptionDialog.IsVisible = false;
    }

    private void OnImageListDescriptionDialogBackgroundTapped(object sender, EventArgs e)
    {
        // Don't close on background tap
    }

    #endregion

    #region Menu Handlers



#endregion
}

/// <summary>
/// Helper class for cleaner assignment UI
/// </summary>
public class CleanerAssignmentInfo : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Avatar { get; set; }
    public string Initial => Name.Length > 0 ? Name.Substring(0, 1).ToUpper() : "?";
    public bool HasAvatar => !string.IsNullOrEmpty(Avatar);
    public bool HasNoAvatar => string.IsNullOrEmpty(Avatar);

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
        Avatar = cleaner.Avatar;
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
