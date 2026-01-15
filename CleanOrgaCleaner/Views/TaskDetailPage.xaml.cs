using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class TaskDetailPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private int _taskId;
    private CleaningTask? _task;
    private List<CleaningTask> _allTasks = new(); // All tasks for the user
    private List<(string FileName, byte[] Bytes)> _selectedPhotos = new(); // Store photo bytes for Problem
    private string? _selectedBildPath;
    private byte[]? _selectedBildBytes; // Store bytes in memory for BildStatus upload
    private BildStatus? _currentBildDetail;

    public string TaskId
    {
        set
        {
            if (int.TryParse(value, out int id))
                _taskId = id;
        }
    }

    public TaskDetailPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
    }

    public TaskDetailPage(int taskId) : this() { _taskId = taskId; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;
        UpdateOfflineBanner(!_webSocketService.IsOnline);

        ApplyTranslations();
        await LoadAllTasksAsync();
        await LoadTaskAsync();
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
        Title = t("task");

        // Menu
        MenuButton.Text = $"{t("task")} ▼";
        MenuTodayButton.Text = $"🏠 {t("today")}";
        MenuChatButton.Text = $"💬 {t("chat")}";
        MenuSettingsButton.Text = $"⚙️ {t("settings")}";

        // Content Labels
        NoticeTitleLabel.Text = t("important_notice");
        MyNotesLabel.Text = t("my_notes");
        NotesEditor.Placeholder = t("note_placeholder");
        ImagesTitleLabel.Text = t("images");
        LogTitleLabel.Text = t("log");

        // Buttons
        CancelButton.Text = t("back");
        AddProblemButton.Text = $"⚠️ {t("report_problem")}";
        AddBildButton.Text = $"+ {t("add_image").ToUpper()}";

        // Problem Popup
        ProblemPopupTitle.Text = t("report_problem");
        ProblemNameLabel.Text = $"{t("problem_name")} *";
        DescriptionLabel.Text = t("description");
        PhotosLabel.Text = t("photos");
        TakePhotoButton.Text = t("camera");
        PickPhotoButton.Text = t("gallery");
        SaveProblemButton.Text = t("save");
        CancelProblemButton.Text = t("cancel");

        // Bild Popup
        BildPopupTitle.Text = t("add_image");
        SelectImageLabel.Text = t("select_image");
        BildTakePhotoButton.Text = t("camera");
        BildPickPhotoButton.Text = t("gallery");
        NoteLabel.Text = t("note");
        SaveBildButton.Text = t("save");
        CancelBildButton.Text = t("cancel");

        // Bild Detail Popup
        BildDetailTitle.Text = t("image_details");
        BildDetailNoteLabel.Text = $"{t("note")}:";
        DeleteBildButton.Text = t("delete");
        CloseBildButton.Text = t("cancel");
        SaveBildDetailButton.Text = t("save");

        // Tasks section label
        TasksSectionLabel.Text = t("tasks_today").ToUpper();
    }

    private async Task LoadAllTasksAsync()
    {
        try
        {
            var todayData = await _apiService.GetTodayDataAsync();
            _allTasks = todayData.Tasks;
            PopulateTasksMenu();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tasks: {ex.Message}");
        }
    }

    private void PopulateTasksMenu()
    {
        TasksMenuStack.Children.Clear();

        if (_allTasks.Count == 0)
        {
            TasksSectionLabel.IsVisible = false;
            return;
        }

        TasksSectionLabel.IsVisible = true;

        foreach (var task in _allTasks)
        {
            // Highlight current task
            var isCurrentTask = task.Id == _taskId;
            var backgroundColor = isCurrentTask ? "#1565C0" : "Transparent";
            var fontAttrs = isCurrentTask ? FontAttributes.Bold : FontAttributes.None;

            var taskButton = new Button
            {
                Text = $"{task.ApartmentName} - {task.Aufgabenart}",
                BackgroundColor = Color.FromArgb(isCurrentTask ? "#1565C0" : "#00000000"),
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = fontAttrs,
                Padding = new Thickness(20, 12),
                HorizontalOptions = LayoutOptions.Fill,
                CommandParameter = task.Id
            };
            taskButton.Clicked += OnTaskMenuItemClicked;
            TasksMenuStack.Children.Add(taskButton);

            // Add separator
            TasksMenuStack.Children.Add(new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#ffffff33")
            });
        }
    }

    private async void OnTaskMenuItemClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int taskId)
        {
            MenuOverlayGrid.IsVisible = false;

            if (taskId != _taskId)
            {
                // Navigate to the selected task
                await Shell.Current.GoToAsync($"TaskDetailPage?taskId={taskId}");
            }
        }
    }

    // Menu handling
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

    private async void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/SettingsPage");
    }

    private async Task LoadTaskAsync()
    {
        try
        {
            _task = await _apiService.GetTaskDetailAsync(_taskId);
            if (_task == null)
            {
                await DisplayAlert("Fehler", "Aufgabe nicht gefunden", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            ApartmentLabel.Text = _task.ApartmentName;
            TaskTypeLabel.Text = _task.Aufgabenart;
            TaskTypeBadge.BackgroundColor = _task.TaskColor;

            if (!string.IsNullOrEmpty(_task.WichtigerHinweis))
            {
                NoticeFrame.IsVisible = true;
                NoticeLabel.Text = _task.WichtigerHinweis;
            }
            else NoticeFrame.IsVisible = false;

            UpdateStartStopButton();
            NotesEditor.Text = _task.AnmerkungMitarbeiter ?? "";
            BuildProblems();
            LoadBilder();
            LoadLogs();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTask error: {ex.Message}");
            await DisplayAlert("Fehler", "Aufgabe konnte nicht geladen werden", "OK");
        }
    }

    private void UpdateStartStopButton()
    {
        if (_task == null) return;
        var t = Translations.Get;
        switch (_task.StateCompleted)
        {
            case "not_started":
                StartStopButton.Text = t("start");
                StartStopButton.BackgroundColor = Color.FromArgb("#9e9e9e");
                StartStopButton.IsEnabled = true;
                break;
            case "started":
                StartStopButton.Text = t("stop");
                StartStopButton.BackgroundColor = Color.FromArgb("#2196F3");
                StartStopButton.IsEnabled = true;
                break;
            case "completed":
                StartStopButton.Text = t("completed");
                StartStopButton.BackgroundColor = Color.FromArgb("#4CAF50");
                StartStopButton.IsEnabled = true;  // Erlaubt Reset zurück zu "not_started"
                break;
        }
    }

    private async void OnStartStopClicked(object sender, EventArgs e)
    {
        if (_task == null) return;
        StartStopButton.IsEnabled = false;
        try
        {
            string newState;
            switch (_task.StateCompleted)
            {
                case "not_started":
                    newState = "started";
                    break;
                case "started":
                    var confirmComplete = await DisplayAlert(
                        Translations.Get("task_completed"),
                        Translations.Get("task_completed_question"),
                        Translations.Get("yes"),
                        Translations.Get("no"));
                    if (!confirmComplete) { StartStopButton.IsEnabled = true; return; }
                    newState = "completed";
                    break;
                case "completed":
                    newState = "not_started";
                    break;
                default: return;
            }

            var response = await _apiService.UpdateTaskStateAsync(_taskId, newState);
            if (response.Success)
            {
                _task.StateCompleted = response.NewState ?? newState;
                UpdateStartStopButton();
                if (newState == "completed") await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Fehler", response.Error ?? "Status konnte nicht geaendert werden", "OK");
                StartStopButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", ex.Message, "OK");
            StartStopButton.IsEnabled = true;
        }
    }

    private async void LoadLogs()
    {
        LogStack.Children.Clear();

        try
        {
            var logs = await _apiService.GetTaskLogsAsync(_taskId);
            if (logs == null || logs.Count == 0)
            {
                LogEmptyLabel.IsVisible = true;
                LogStack.Children.Add(LogEmptyLabel);
                return;
            }

            LogEmptyLabel.IsVisible = false;
            foreach (var log in logs)
            {
                var logBorder = new Border
                {
                    BackgroundColor = Color.FromArgb("#f8f9fa"),
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Stroke = Colors.Transparent,
                    Padding = 12
                };

                // Left border accent
                logBorder.Margin = new Thickness(4, 0, 0, 0);

                var logStack = new VerticalStackLayout { Spacing = 4 };

                var timeLabel = new Label
                {
                    Text = log.DatumZeit,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#999")
                };
                logStack.Children.Add(timeLabel);

                var userLabel = new Label
                {
                    Text = log.User,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#667eea"),
                    FontAttributes = FontAttributes.Bold
                };
                logStack.Children.Add(userLabel);

                var textLabel = new Label
                {
                    Text = log.Text,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#333")
                };
                logStack.Children.Add(textLabel);

                logBorder.Content = logStack;
                LogStack.Children.Add(logBorder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadLogs error: {ex.Message}");
            LogEmptyLabel.Text = "Fehler beim Laden der Logs";
            LogEmptyLabel.IsVisible = true;
            LogStack.Children.Add(LogEmptyLabel);
        }
    }

    private void BuildProblems()
    {
        ProblemsStack.Children.Clear();
        if (_task?.Probleme == null || _task.Probleme.Count == 0) return;
        foreach (var problem in _task.Probleme)
            ProblemsStack.Children.Add(CreateProblemView(problem));
    }

    private View CreateProblemView(Problem problem)
    {
        var border = new Border { BackgroundColor = Color.FromArgb("#ffebee"), Stroke = Color.FromArgb("#f44336"), StrokeShape = new RoundRectangle { CornerRadius = 15 }, Padding = 15 };
        border.Shadow = new Shadow { Brush = Colors.Gray, Offset = new Point(2, 2), Radius = 5, Opacity = 0.1f };
        var stack = new VerticalStackLayout { Spacing = 8 };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        var nameLabel = new Label { Text = problem.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#c62828") };
        header.Children.Add(nameLabel); Grid.SetColumn(nameLabel, 0);
        var deleteButton = new Button { Text = "X", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#c62828"), FontSize = 14, FontAttributes = FontAttributes.Bold, Padding = 0, WidthRequest = 30, HeightRequest = 30 };
        deleteButton.Clicked += async (s, e) => await OnDeleteProblem(problem.Id);
        header.Children.Add(deleteButton); Grid.SetColumn(deleteButton, 1);
        stack.Children.Add(header);
        if (!string.IsNullOrEmpty(problem.Beschreibung))
            stack.Children.Add(new Label { Text = problem.Beschreibung, FontSize = 14, TextColor = Color.FromArgb("#666666") });
        if (problem.Fotos != null && problem.Fotos.Count > 0)
        {
            var photosLayout = new HorizontalStackLayout { Spacing = 8 };
            foreach (var foto in problem.Fotos)
            {
                var imageContainer = new Border { StrokeShape = new RoundRectangle { CornerRadius = 8 }, Stroke = Colors.Transparent };
                imageContainer.Content = new Image { Source = foto, WidthRequest = 70, HeightRequest = 70, Aspect = Aspect.AspectFill };
                photosLayout.Children.Add(imageContainer);
            }
            stack.Children.Add(photosLayout);
        }
        border.Content = stack;
        return border;
    }

    private async Task OnDeleteProblem(int problemId)
    {
        var confirm = await DisplayAlert("Problem loeschen", "Moechtest du dieses Problem wirklich loeschen?", "Ja", "Nein");
        if (confirm)
        {
            var response = await _apiService.DeleteProblemAsync(problemId);
            if (response.Success) await LoadTaskAsync();
            else await DisplayAlert("Fehler", response.Error ?? "Fehler beim Loeschen", "OK");
        }
    }

    private void OnAddProblemClicked(object sender, EventArgs e)
    {
        ProblemNameEntry.Text = ""; ProblemDescriptionEditor.Text = "";
        _selectedPhotos.Clear(); UpdatePhotoPreview(); CharCountLabel.Text = "0 / 300";
        ProblemPopupOverlay.IsVisible = true;
    }

    private void OnPopupBackgroundTapped(object sender, EventArgs e) { ProblemPopupOverlay.IsVisible = false; }

    private void OnDescriptionTextChanged(object sender, TextChangedEventArgs e)
    {
        var length = e.NewTextValue?.Length ?? 0;
        CharCountLabel.Text = $"{length} / 300";
        if (length > 300) ProblemDescriptionEditor.Text = e.NewTextValue?.Substring(0, 300);
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported) { await DisplayAlert("Fehler", "Kamera nicht verfügbar", "OK"); return; }
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var fileName = $"problem_photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                _selectedPhotos.Add((fileName, bytes));
                UpdatePhotoPreview();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Camera error: {ex.Message}"); await DisplayAlert("Fehler", "Kamera konnte nicht geöffnet werden", "OK"); }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // FilePicker für bessere Android Scoped Storage Unterstützung
            var options = new PickOptions
            {
                PickerTitle = "Foto auswählen",
                FileTypes = FilePickerFileType.Images
            };
            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var ext = System.IO.Path.GetExtension(result.FileName) ?? ".jpg";
                var fileName = $"problem_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                _selectedPhotos.Add((fileName, bytes));
                UpdatePhotoPreview();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}"); await DisplayAlert("Fehler", "Galerie konnte nicht geöffnet werden", "OK"); }
    }

    private void UpdatePhotoPreview()
    {
        PhotoPreviewStack.Children.Clear();
        if (_selectedPhotos.Count == 0) { PhotoPreviewStack.IsVisible = false; PhotoCountLabel.Text = "Keine Fotos ausgewählt"; return; }
        PhotoPreviewStack.IsVisible = true; PhotoCountLabel.Text = $"{_selectedPhotos.Count} Foto(s) ausgewählt";
        for (int i = 0; i < _selectedPhotos.Count; i++)
        {
            var photo = _selectedPhotos[i];
            var index = i;
            var grid = new Grid { WidthRequest = 70, HeightRequest = 70 };
            var imageContainer = new Border { StrokeShape = new RoundRectangle { CornerRadius = 8 }, Stroke = Colors.Transparent };
            imageContainer.Content = new Image { Source = ImageSource.FromStream(() => new MemoryStream(photo.Bytes)), Aspect = Aspect.AspectFill };
            grid.Children.Add(imageContainer);
            var deleteBtn = new Button { Text = "X", BackgroundColor = Color.FromArgb("#c62828"), TextColor = Colors.White, FontSize = 10, WidthRequest = 22, HeightRequest = 22, CornerRadius = 11, Padding = 0, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 2, 2, 0) };
            deleteBtn.Clicked += (s, e) => { _selectedPhotos.RemoveAt(index); UpdatePhotoPreview(); };
            grid.Children.Add(deleteBtn);
            PhotoPreviewStack.Children.Add(grid);
        }
    }

    private async void OnSaveProblemClicked(object sender, EventArgs e)
    {
        var name = ProblemNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { await DisplayAlert("Fehler", "Bitte gib einen Namen für das Problem ein", "OK"); return; }
        var beschreibung = ProblemDescriptionEditor.Text?.Trim();
        ProblemPopupOverlay.IsVisible = false;
        try
        {
            var response = await _apiService.ReportProblemWithBytesAsync(_taskId, name, beschreibung, _selectedPhotos);
            if (response.Success) { await DisplayAlert("Gemeldet", "Problem wurde gemeldet", "OK"); await LoadTaskAsync(); }
            else await DisplayAlert("Fehler", response.Error ?? "Fehler beim Melden", "OK");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Report problem error: {ex.Message}"); await DisplayAlert("Fehler", "Problem konnte nicht gemeldet werden", "OK"); }
    }

    private void OnCancelProblemClicked(object sender, EventArgs e) { ProblemPopupOverlay.IsVisible = false; }

    // Cancel button - go back
    private async void OnCancelClicked(object sender, EventArgs e) { await Shell.Current.GoToAsync(".."); }

    // Save notes when editor loses focus
    private async void OnNotesEditorUnfocused(object sender, FocusEventArgs e)
    {
        if (_task == null) return;
        var newNotes = NotesEditor.Text?.Trim() ?? "";
        if (newNotes == (_task.AnmerkungMitarbeiter ?? "")) return;
        try
        {
            NotesStatusLabel.Text = "Speichern...";
            var response = await _apiService.UpdateTaskNotesAsync(_taskId, newNotes);
            if (response.Success)
            {
                _task.AnmerkungMitarbeiter = newNotes;
                NotesStatusLabel.Text = "Gespeichert";
                await Task.Delay(2000);
                NotesStatusLabel.Text = "";
            }
            else NotesStatusLabel.Text = "Fehler: " + (response.Error ?? "");
        }
        catch { NotesStatusLabel.Text = "Fehler beim Speichern"; }
    }

    // Bilder section
    private string GetAbsoluteImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        // If already absolute URL, return as-is
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        // Convert relative URL to absolute
        return $"{ApiService.BaseUrl}{url}";
    }

    private async void LoadBilder()
    {
        BilderStack.Children.Clear();
        var bilderCount = _task?.Bilder?.Count ?? 0;

        Console.WriteLine($"=== LoadBilder START ===");
        Console.WriteLine($"Task ID: {_task?.Id}, Bilder Count: {bilderCount}");

        // Show count in UI
        BilderCountLabel.Text = bilderCount == 0 ? "Keine Bilder" : $"{bilderCount} Bild(er)";

        if (_task?.Bilder == null || _task.Bilder.Count == 0)
        {
            Console.WriteLine($"LoadBilder: No images found, exiting");
            BilderCountLabel.Text = $"DEBUG: Bilder ist {(_task?.Bilder == null ? "NULL" : "LEER")}";
            return;
        }

        foreach (var bild in _task.Bilder)
        {
            System.Diagnostics.Debug.WriteLine($"LoadBilder: Processing Bild {bild.Id}");
            System.Diagnostics.Debug.WriteLine($"  - ThumbnailUrl: '{bild.ThumbnailUrl}'");
            System.Diagnostics.Debug.WriteLine($"  - FullUrl: '{bild.FullUrl}'");
            System.Diagnostics.Debug.WriteLine($"  - Url: '{bild.Url}'");

            // Container mit Bild und Delete-Button
            var grid = new Grid
            {
                WidthRequest = 80,
                HeightRequest = 80,
                Margin = new Thickness(0, 0, 10, 10)
            };

            // Bild - load with authentication
            var imageUrl = bild.ThumbnailUrl ?? bild.FullUrl ?? bild.Url;
            System.Diagnostics.Debug.WriteLine($"  - Final imageUrl: '{imageUrl}'");

            var imageBorder = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#E0E0E0") // Placeholder color
            };

            var image = new Image
            {
                WidthRequest = 80,
                HeightRequest = 80,
                Aspect = Aspect.AspectFill
            };

            // Load image asynchronously with auth
            var bildIdForLog = bild.Id;
            var urlForLog = imageUrl;
            _ = Task.Run(async () =>
            {
                System.Diagnostics.Debug.WriteLine($"GetImageAsync START: Bild {bildIdForLog}, URL: {urlForLog}");
                var imageSource = await _apiService.GetImageAsync(urlForLog);
                if (imageSource != null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetImageAsync SUCCESS: Bild {bildIdForLog}");
                    MainThread.BeginInvokeOnMainThread(() => image.Source = imageSource);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GetImageAsync FAILED: Bild {bildIdForLog} - imageSource is null");
                }
            });

            imageBorder.Content = image;

            // Tap zum Vergrößern
            var tapGesture = new TapGestureRecognizer();
            var bildCopy = bild; // Closure fix
            tapGesture.Tapped += (s, e) => ShowBildDetail(bildCopy);
            imageBorder.GestureRecognizers.Add(tapGesture);

            // Delete-Button (X oben rechts)
            var deleteBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#CC000000"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Stroke = Colors.Transparent,
                WidthRequest = 24,
                HeightRequest = 24,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 4, 4, 0),
                Content = new Label
                {
                    Text = "✕",
                    TextColor = Colors.White,
                    FontSize = 14,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };
            var deleteTap = new TapGestureRecognizer();
            deleteTap.Tapped += async (s, e) => await DeleteBild(bildCopy.Id);
            deleteBtn.GestureRecognizers.Add(deleteTap);

            // Notiz-Indikator (unten links)
            if (!string.IsNullOrWhiteSpace(bild.Notiz))
            {
                var notizIndicator = new Border
                {
                    BackgroundColor = Color.FromArgb("#CCFF9800"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke = Colors.Transparent,
                    Padding = new Thickness(4, 2),
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.End,
                    Margin = new Thickness(4, 0, 0, 4),
                    Content = new Label
                    {
                        Text = "📝",
                        FontSize = 10
                    }
                };
                grid.Children.Add(notizIndicator);
            }

            grid.Children.Add(imageBorder);
            grid.Children.Add(deleteBtn);
            BilderStack.Children.Add(grid);
        }
    }

    private async void ShowBildDetail(BildStatus bild)
    {
        _currentBildDetail = bild;

        // Popup befüllen
        BildDetailDatum.Text = $"Erstellt: {bild.ErstelltAm}";
        BildDetailNotizEditor.Text = bild.Notiz ?? "";
        BildDetailImage.Source = null; // Clear previous

        // Popup anzeigen
        BildDetailPopupOverlay.IsVisible = true;

        // Load image with authentication
        var imageUrl = bild.FullUrl ?? bild.ThumbnailUrl ?? bild.Url;
        var imageSource = await _apiService.GetImageAsync(imageUrl);
        if (imageSource != null)
        {
            BildDetailImage.Source = imageSource;
        }
    }

    private void OnBildDetailPopupBackgroundTapped(object sender, EventArgs e)
    {
        BildDetailPopupOverlay.IsVisible = false;
        _currentBildDetail = null;
    }

    private void OnCloseBildDetailClicked(object sender, EventArgs e)
    {
        BildDetailPopupOverlay.IsVisible = false;
        _currentBildDetail = null;
    }

    private async void OnSaveBildDetailClicked(object sender, EventArgs e)
    {
        if (_currentBildDetail == null) return;

        SaveBildDetailButton.IsEnabled = false;
        SaveBildDetailButton.Text = "Speichern...";

        try
        {
            var notiz = BildDetailNotizEditor.Text?.Trim() ?? "";
            var response = await _apiService.UpdateBildStatusAsync(_currentBildDetail.Id, notiz);

            if (response.Success)
            {
                BildDetailPopupOverlay.IsVisible = false;
                _currentBildDetail = null;
                await DisplayAlert("Gespeichert", "Notiz wurde aktualisiert", "OK");
                await LoadTaskAsync();
            }
            else
            {
                await DisplayAlert("Fehler", response.Error ?? "Speichern fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveBildDetail error: {ex.Message}");
            await DisplayAlert("Fehler", "Notiz konnte nicht gespeichert werden", "OK");
        }
        finally
        {
            SaveBildDetailButton.Text = "Speichern";
            SaveBildDetailButton.IsEnabled = true;
        }
    }

    private async void OnDeleteBildDetailClicked(object sender, EventArgs e)
    {
        if (_currentBildDetail == null) return;

        var confirm = await DisplayAlert("Bild löschen", "Möchtest du dieses Bild wirklich löschen?", "Ja, löschen", "Abbrechen");
        if (!confirm) return;

        try
        {
            var response = await _apiService.DeleteBildStatusAsync(_currentBildDetail.Id);
            if (response.Success)
            {
                BildDetailPopupOverlay.IsVisible = false;
                _currentBildDetail = null;
                await DisplayAlert("Gelöscht", "Bild wurde gelöscht", "OK");
                await LoadTaskAsync();
            }
            else
            {
                await DisplayAlert("Fehler", response.Error ?? "Löschen fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteBildDetail error: {ex.Message}");
            await DisplayAlert("Fehler", "Bild konnte nicht gelöscht werden", "OK");
        }
    }

    private async Task DeleteBild(int bildId)
    {
        var confirm = await DisplayAlert("Bild löschen", "Möchtest du dieses Bild wirklich löschen?", "Ja, löschen", "Abbrechen");
        if (!confirm) return;

        try
        {
            var response = await _apiService.DeleteBildStatusAsync(bildId);
            if (response.Success)
            {
                await DisplayAlert("Gelöscht", "Bild wurde gelöscht", "OK");
                await LoadTaskAsync(); // Refresh
            }
            else
            {
                await DisplayAlert("Fehler", response.Error ?? "Löschen fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteBild error: {ex.Message}");
            await DisplayAlert("Fehler", "Bild konnte nicht gelöscht werden", "OK");
        }
    }

    private void OnAddBildClicked(object sender, EventArgs e)
    {
        _selectedBildPath = null;
        BildPreviewBorder.IsVisible = false;
        BildNotizEditor.Text = "";
        SaveBildButton.IsEnabled = false;
        BildPopupOverlay.IsVisible = true;
    }

    private void OnBildPopupBackgroundTapped(object sender, EventArgs e) { BildPopupOverlay.IsVisible = false; }
    private void OnCancelBildClicked(object sender, EventArgs e) { BildPopupOverlay.IsVisible = false; }

    private async void OnBildTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Fehler", "Kamera nicht verfügbar", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Foto aufnehmen"
            });

            if (photo != null)
            {
                // Stream direkt lesen und Bytes speichern
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var originalBytes = memoryStream.ToArray();

                // Komprimiere Bild auf max 2000px
                _selectedBildBytes = await ImageHelper.CompressImageAsync(originalBytes);
                _selectedBildPath = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

                BildPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedBildBytes));
                BildPreviewBorder.IsVisible = true;
                SaveBildButton.IsEnabled = true;
            }
        }
        catch (PermissionException)
        {
            await DisplayAlert("Berechtigung erforderlich", "Bitte erlaube den Kamera-Zugriff in den App-Einstellungen", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bild Camera error: {ex.Message}");
            await DisplayAlert("Kamera-Fehler", ex.Message, "OK");
        }
    }

    private async void OnBildPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // MediaPicker für iOS Fotos-App Zugriff
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Bild auswählen"
            });

            if (photo != null)
            {
                // Stream direkt lesen
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var originalBytes = memoryStream.ToArray();

                // Komprimiere Bild auf max 2000px
                _selectedBildBytes = await ImageHelper.CompressImageAsync(originalBytes);
                _selectedBildPath = $"gallery_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

                BildPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedBildBytes));
                BildPreviewBorder.IsVisible = true;
                SaveBildButton.IsEnabled = true;
            }
        }
        catch (PermissionException)
        {
            await DisplayAlert("Berechtigung erforderlich", "Bitte erlaube den Foto-Zugriff in den App-Einstellungen", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bild Gallery error: {ex.Message}");
            await DisplayAlert("Galerie-Fehler", ex.Message, "OK");
        }
    }

    private async void OnSaveBildClicked(object sender, EventArgs e)
    {
        if (_selectedBildBytes == null || _selectedBildBytes.Length == 0)
        {
            await DisplayAlert("Fehler", "Kein Bild ausgewählt", "OK");
            return;
        }

        // Button deaktivieren während Upload
        SaveBildButton.IsEnabled = false;
        SaveBildButton.Text = "Wird hochgeladen...";

        try
        {
            System.Diagnostics.Debug.WriteLine($"OnSaveBildClicked: Start Upload, {_selectedBildBytes!.Length} bytes");
            var notiz = BildNotizEditor.Text?.Trim() ?? "";
            var fileName = _selectedBildPath ?? $"image_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var response = await _apiService.UploadBildStatusBytesAsync(_taskId, _selectedBildBytes!, fileName, notiz);

            if (response.Success)
            {
                BildPopupOverlay.IsVisible = false;
                _selectedBildBytes = null;
                _selectedBildPath = null;
                await DisplayAlert("Gespeichert", "Bild wurde hochgeladen", "OK");
                await LoadTaskAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSaveBildClicked: Upload fehlgeschlagen - {response.Error}");
                await DisplayAlert("Fehler", response.Error ?? "Upload fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Upload Bild error: {ex.Message}\n{ex.StackTrace}");
            await DisplayAlert("Fehler", $"Upload-Fehler: {ex.Message}", "OK");
        }
        finally
        {
            SaveBildButton.Text = "Speichern";
            SaveBildButton.IsEnabled = _selectedBildBytes != null;
        }
    }
}
