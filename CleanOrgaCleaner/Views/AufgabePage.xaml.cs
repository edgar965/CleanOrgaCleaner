using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class AufgabePage : ContentPage
{
    private readonly ApiService _apiService;
    private int _taskId;
    private CleaningTask? _task;
    private List<(string FileName, byte[] Bytes)> _selectedPhotos = new(); // Store photo bytes for Problem
    private string? _selectedBildPath;
    private byte[]? _selectedBildBytes; // Store bytes in memory for BildStatus upload
    private BildStatus? _currentBildDetail;
    private string _currentTab = "aufgabe"; // Track current tab for state persistence
    private int _problemIdToDelete; // For custom delete popup
    private int _bildIdToDelete; // For custom delete bild popup
    private bool _deleteFromDetailPopup; // Track if delete was triggered from detail popup
    private Problem? _currentProblemDetail; // For problem detail popup
    private int? _editingProblemId; // null = creating new, int = editing existing
    private int? _editingBildId; // null = creating new, int = editing existing

    public string TaskId
    {
        set
        {
            if (int.TryParse(value, out int id))
                _taskId = id;
        }
    }

    public AufgabePage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
    }

    public AufgabePage(int taskId) : this() { _taskId = taskId; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Initialize header (handles translations, user info, work status, offline banner)
        _ = Header.InitializeAsync();
        Header.SetPageTitle("today");

        ApplyTranslations();
        _ = LoadTaskAsync();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("task");

        // Tab Buttons without emojis (cleaner design)
        TabAufgabeButton.Text = t("task_tab");
        TabProblemeButton.Text = t("problems_tab");
        TabAnmerkungenButton.Text = t("note");
        TabLogsButton.Text = t("log");

        // Buttons
        AddProblemButton.Text = $"+ {t("report_problem")}";
        AddAnmerkungButton.Text = $"+ {t("add_note").ToUpper()}";

        // Empty state labels
        NoTaskDescriptionLabel.Text = t("no_task_description");
        NoProblemsLabel.Text = t("no_problems");
        NoAnmerkungenLabel.Text = t("no_notes");
        NoLogsLabel.Text = t("no_logs");

        // Problem Popup
        ProblemPopupTitle.Text = t("report_problem");
        ProblemNameLabel.Text = $"{t("problem_name")} *";
        DescriptionLabel.Text = t("description");
        PhotosLabel.Text = t("photos");
        TakePhotoButton.Text = t("camera");
        PickPhotoButton.Text = t("gallery");
        SaveProblemButton.Text = t("save");
        CancelProblemButton.Text = t("cancel");

        // Anmerkung Popup
        AnmerkungPopupTitle.Text = t("add_note");
        SelectImageLabel.Text = t("image_optional");
        AnmerkungTakePhotoButton.Text = t("camera");
        AnmerkungPickPhotoButton.Text = t("gallery");
        NoteLabel.Text = t("note");
        SaveAnmerkungButton.Text = t("save");
        CancelAnmerkungButton.Text = t("cancel");

        // Bild Detail Popup
        BildDetailTitle.Text = t("image_details");
        BildDetailNoteLabel.Text = $"{t("note")}:";
        CloseBildButton.Text = t("cancel");
        SaveBildDetailButton.Text = t("save");

        // Delete Problem Popup
        DeleteProblemTitle.Text = t("delete_problem_title");
        DeleteProblemMessage.Text = t("delete_problem_confirm");
        CancelDeleteProblemButton.Text = t("cancel");
        ConfirmDeleteProblemButton.Text = t("yes_delete");

        // Delete Bild Popup
        DeleteBildTitle.Text = t("delete_image");
        DeleteBildMessage.Text = t("confirm_delete_image");
        CancelDeleteBildButton.Text = t("cancel");
        ConfirmDeleteBildButton.Text = t("yes_delete");

        // Problem Detail Popup
        ProblemDetailTitle.Text = t("problem_details");
        ProblemDetailPhotosLabel.Text = $"{t("photos")}:";
        CloseProblemDetailButton.Text = t("close");

        // Complete Task Popup
        CompleteTaskTitle.Text = t("task_completed");
        CompleteTaskMessage.Text = t("task_completed_question");
        CancelCompleteTaskButton.Text = t("no");
        ConfirmCompleteTaskButton.Text = t("yes");
    }

    // Tab handling
    private void OnTabAufgabeClicked(object sender, EventArgs e)
    {
        SelectTab("aufgabe");
    }

    private void OnTabProblemeClicked(object sender, EventArgs e)
    {
        SelectTab("probleme");
    }

    private void OnTabAnmerkungenClicked(object sender, EventArgs e)
    {
        SelectTab("anmerkungen");
    }

    private async void OnTabLogsClicked(object sender, EventArgs e)
    {
        SelectTab("logs");
        await LoadLogsAsync();
    }

    private void SelectTab(string tab)
    {
        // Save current tab for state persistence
        _currentTab = tab;

        // Reset all tab buttons to inactive state (dark blue background, white text, no border)
        TabAufgabeButton.BackgroundColor = Color.FromArgb("#1a3a5c");
        TabAufgabeButton.TextColor = Colors.White;
        TabAufgabeButton.BorderWidth = 0;
        TabProblemeButton.BackgroundColor = Color.FromArgb("#1a3a5c");
        TabProblemeButton.TextColor = Colors.White;
        TabProblemeButton.BorderWidth = 0;
        TabAnmerkungenButton.BackgroundColor = Color.FromArgb("#1a3a5c");
        TabAnmerkungenButton.TextColor = Colors.White;
        TabAnmerkungenButton.BorderWidth = 0;
        TabLogsButton.BackgroundColor = Color.FromArgb("#1a3a5c");
        TabLogsButton.TextColor = Colors.White;
        TabLogsButton.BorderWidth = 0;

        // Hide all tab content
        TabAufgabeContent.IsVisible = false;
        TabProblemeContent.IsVisible = false;
        TabAnmerkungenContent.IsVisible = false;
        TabLogsContent.IsVisible = false;

        // Activate selected tab (white background, dark blue text, with border)
        var borderColor = Color.FromArgb("#1a3a5c");
        switch (tab)
        {
            case "aufgabe":
                TabAufgabeButton.BackgroundColor = Colors.White;
                TabAufgabeButton.TextColor = Color.FromArgb("#1a3a5c");
                TabAufgabeButton.BorderColor = borderColor;
                TabAufgabeButton.BorderWidth = 2;
                TabAufgabeContent.IsVisible = true;
                break;
            case "probleme":
                TabProblemeButton.BackgroundColor = Colors.White;
                TabProblemeButton.TextColor = Color.FromArgb("#1a3a5c");
                TabProblemeButton.BorderColor = borderColor;
                TabProblemeButton.BorderWidth = 2;
                TabProblemeContent.IsVisible = true;
                break;
            case "anmerkungen":
                TabAnmerkungenButton.BackgroundColor = Colors.White;
                TabAnmerkungenButton.TextColor = Color.FromArgb("#1a3a5c");
                TabAnmerkungenButton.BorderColor = borderColor;
                TabAnmerkungenButton.BorderWidth = 2;
                TabAnmerkungenContent.IsVisible = true;
                break;
            case "logs":
                TabLogsButton.BackgroundColor = Colors.White;
                TabLogsButton.TextColor = Color.FromArgb("#1a3a5c");
                TabLogsButton.BorderColor = borderColor;
                TabLogsButton.BorderWidth = 2;
                TabLogsContent.IsVisible = true;
                break;
        }
    }

    private async Task LoadTaskAsync()
    {
        try
        {
            _task = await _apiService.GetAufgabeDetailAsync(_taskId);
            if (_task == null)
            {
                // Don't use DisplayAlertAsync in fire-and-forget - it deadlocks iOS Shell navigation
                System.Diagnostics.Debug.WriteLine($"LoadTask: Task {_taskId} not found");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try { await Shell.Current.GoToAsync(".."); } catch { }
                });
                return;
            }

            // Combined apartment and task name (e.g. "02 Reinigung")
            TaskNameLabel.Text = $"{_task.ApartmentName} {_task.DisplayName}";

            // Aufgabe tab content - mit Übersetzung
            string aufgabeText = GetTranslatedAufgabe();
            if (!string.IsNullOrEmpty(aufgabeText))
            {
                NoticeLabel.IsVisible = true;
                NoticeLabel.Text = aufgabeText;
                NoTaskDescriptionLabel.IsVisible = false;
            }
            else
            {
                NoticeLabel.IsVisible = false;
                NoTaskDescriptionLabel.IsVisible = true;
            }

            UpdateStartStopButton();
            BuildProblems();
            LoadBilder();

            // Restore current tab after refresh
            SelectTab(_currentTab);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTask error: {ex.Message}");
            // Don't use DisplayAlertAsync in fire-and-forget - it deadlocks iOS Shell navigation
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

    /// <summary>
    /// Gibt die übersetzte Aufgabenbeschreibung zurück.
    /// Prüft zuerst ob eine gecachte Übersetzung für die aktuelle Sprache vorhanden ist.
    /// </summary>
    private string GetTranslatedAufgabe()
    {
        if (_task == null) return string.Empty;

        // Aktuelle Sprache des Clients
        string currentLang = Translations.CurrentLanguage;

        // Wenn Deutsch, Original zurückgeben
        if (currentLang == "de")
        {
            return _task.Aufgabe ?? string.Empty;
        }

        // Prüfen ob Übersetzung gecached ist
        if (_task.AufgabeTranslated != null &&
            _task.AufgabeTranslated.TryGetValue(currentLang, out string? cached) &&
            !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        // Fallback: Original-Text (noch nicht übersetzt)
        return _task.Aufgabe ?? string.Empty;
    }

    private async void OnStartStopClicked(object sender, EventArgs e)
    {
        if (_task == null) return;

        // If task is started, show custom complete popup instead of DisplayAlertAsync
        if (_task.StateCompleted == "started")
        {
            CompleteTaskPopupOverlay.IsVisible = true;
            return;
        }

        StartStopButton.IsEnabled = false;
        try
        {
            string newState;
            switch (_task.StateCompleted)
            {
                case "not_started":
                    newState = "started";
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
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Status konnte nicht geaendert werden", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
        finally
        {
            StartStopButton.IsEnabled = true;
        }
    }

    private void OnCompleteTaskPopupBackgroundTapped(object sender, EventArgs e)
    {
        CompleteTaskPopupOverlay.IsVisible = false;
    }

    private void OnCancelCompleteTaskClicked(object sender, EventArgs e)
    {
        CompleteTaskPopupOverlay.IsVisible = false;
    }

    private async void OnConfirmCompleteTaskClicked(object sender, EventArgs e)
    {
        CompleteTaskPopupOverlay.IsVisible = false;
        StartStopButton.IsEnabled = false;

        try
        {
            var response = await _apiService.UpdateTaskStateAsync(_taskId, "completed");
            if (response.Success)
            {
                _task!.StateCompleted = response.NewState ?? "completed";
                UpdateStartStopButton();

                // Navigate back on main thread with delay for iOS stability
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Task.Delay(150);
                    try
                    {
                        await Shell.Current.GoToAsync("..");
                    }
                    catch (Exception navEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigation error: {navEx.Message}");
                        // Fallback: try Navigation.PopAsync if Shell navigation fails
                        if (Navigation.NavigationStack.Count > 1)
                        {
                            await Navigation.PopAsync();
                        }
                    }
                });
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Status konnte nicht geaendert werden", "OK");
                StartStopButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
            StartStopButton.IsEnabled = true;
        }
    }

    private void BuildProblems()
    {
        ProblemsStack.Children.Clear();
        if (_task?.Probleme == null || _task.Probleme.Count == 0)
        {
            NoProblemsLabel.IsVisible = true;
            return;
        }
        NoProblemsLabel.IsVisible = false;
        foreach (var problem in _task.Probleme)
            ProblemsStack.Children.Add(CreateProblemView(problem));
    }

    private View CreateProblemView(Problem problem)
    {
        var border = new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#e0e0e0"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = 12 };
        border.Shadow = new Shadow { Brush = Colors.Gray, Offset = new Point(0, 2), Radius = 8, Opacity = 0.1f };

        // Horizontal layout: [Thumbnail] [Text] [X Button]
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        // First photo thumbnail on the left

        if (problem.Fotos != null && problem.Fotos.Count > 0)
        {
            var imgBorder = new Border { StrokeShape = new RoundRectangle { CornerRadius = 10 }, Stroke = Color.FromArgb("#e0e0e0"), WidthRequest = 70, HeightRequest = 70 };
            imgBorder.Content = new Image { Source = problem.Fotos[0], WidthRequest = 70, HeightRequest = 70, Aspect = Aspect.AspectFill };
            grid.Children.Add(imgBorder);
            Grid.SetColumn(imgBorder, 0);
        }

        // Problem name + description in the middle
        var textStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        textStack.Children.Add(new Label { Text = problem.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#333333") });
        if (!string.IsNullOrEmpty(problem.Beschreibung))
            textStack.Children.Add(new Label { Text = problem.Beschreibung, FontSize = 13, TextColor = Color.FromArgb("#666666"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });
        grid.Children.Add(textStack);
        Grid.SetColumn(textStack, 1);

        // Big round X delete button on the right (same red as Stop button)
        var deleteButton = new Button
        {
            Text = "\u2715",
            BackgroundColor = Color.FromArgb("#E91E63"),
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 44,
            HeightRequest = 44,
            CornerRadius = 22,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center
        };
        deleteButton.Clicked += (s, e) => OnDeleteProblem(problem.Id);
        grid.Children.Add(deleteButton);
        Grid.SetColumn(deleteButton, 2);

        // Tap to open detail
        var problemCopy = problem;
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => ShowProblemDetail(problemCopy);
        border.GestureRecognizers.Add(tapGesture);

        border.Content = grid;
        return border;
    }

    private void OnDeleteProblem(int problemId)
    {
        _problemIdToDelete = problemId;
        DeleteProblemPopupOverlay.IsVisible = true;
    }

    private void OnDeleteProblemPopupBackgroundTapped(object sender, EventArgs e)
    {
        DeleteProblemPopupOverlay.IsVisible = false;
    }

    private void OnCancelDeleteProblemClicked(object sender, EventArgs e)
    {
        DeleteProblemPopupOverlay.IsVisible = false;
    }

    private async void OnConfirmDeleteProblemClicked(object sender, EventArgs e)
    {
        DeleteProblemPopupOverlay.IsVisible = false;
        var response = await _apiService.DeleteProblemAsync(_problemIdToDelete);
        if (response.Success) await LoadTaskAsync();
        else await DisplayAlertAsync("Fehler", response.Error ?? "Fehler beim Loeschen", "OK");
    }

    private void ShowProblemDetail(Problem problem)
    {
        _currentProblemDetail = problem;
        _editingProblemId = problem.Id;

        // Use same popup as Add Problem but in edit mode
        ProblemPopupTitle.Text = Translations.Get["edit_problem"];
        ProblemNameEntry.Text = problem.Name ?? "";
        ProblemDescriptionEditor.Text = problem.Beschreibung ?? "";
        CharCountLabel.Text = $"{(problem.Beschreibung?.Length ?? 0)} / 300";
        _selectedPhotos.Clear();
        UpdatePhotoPreview();

        ProblemPopupOverlay.IsVisible = true;
    }

    private void OnProblemDetailPopupBackgroundTapped(object sender, EventArgs e)
    {
        ProblemDetailPopupOverlay.IsVisible = false;
        _currentProblemDetail = null;
    }

    private void OnCloseProblemDetailClicked(object sender, EventArgs e)
    {
        ProblemDetailPopupOverlay.IsVisible = false;
        _currentProblemDetail = null;
    }

    private void OnAddProblemClicked(object sender, EventArgs e)
    {
        _editingProblemId = null;
        ProblemPopupTitle.Text = Translations.Get["report_problem"];
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
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync("Fehler", "Kamera nicht verfügbar", "OK");
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
                var bytes = memoryStream.ToArray();
                var fileName = $"problem_photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                _selectedPhotos.Add((fileName, bytes));
                UpdatePhotoPreview();
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlertAsync("Fehler", "Kamera wird auf diesem Geraet nicht unterstuetzt", "OK");
        }
        catch (PermissionException)
        {
            await OfferOpenSettingsAsync("Kamera");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
    }

    private async Task OfferOpenSettingsAsync(string permissionName)
    {
        var openSettings = await DisplayAlertAsync($"{permissionName}-Berechtigung",
            $"Die {permissionName}-Berechtigung wurde verweigert.\n\nBitte oeffne die App-Einstellungen und aktiviere die Berechtigung unter 'Berechtigungen'.",
            "Einstellungen oeffnen", "Abbrechen");
        if (openSettings)
        {
            Services.PermissionHelper.OpenAppSettings();
        }
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
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}"); await DisplayAlertAsync("Fehler", "Galerie konnte nicht geöffnet werden", "OK"); }
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
        if (string.IsNullOrEmpty(name)) { await DisplayAlertAsync("Fehler", "Bitte gib einen Namen für das Problem ein", "OK"); return; }
        var beschreibung = ProblemDescriptionEditor.Text?.Trim();
        ProblemPopupOverlay.IsVisible = false;
        try
        {
            ApiResponse response;
            if (_editingProblemId.HasValue)
            {
                // Update existing problem
                response = await _apiService.UpdateProblemAsync(_editingProblemId.Value, name, beschreibung);
                if (response.Success) { await DisplayAlertAsync("Gespeichert", "Problem wurde aktualisiert", "OK"); await LoadTaskAsync(); }
                else await DisplayAlertAsync("Fehler", response.Error ?? "Fehler beim Aktualisieren", "OK");
            }
            else
            {
                // Create new problem
                response = await _apiService.ReportProblemWithBytesAsync(_taskId, name, beschreibung, _selectedPhotos);
                if (response.Success) { await DisplayAlertAsync("Gemeldet", "Problem wurde gemeldet", "OK"); await LoadTaskAsync(); }
                else await DisplayAlertAsync("Fehler", response.Error ?? "Fehler beim Melden", "OK");
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Save problem error: {ex.Message}"); await DisplayAlertAsync("Fehler", "Problem konnte nicht gespeichert werden", "OK"); }
        finally { _editingProblemId = null; }
    }

    private void OnCancelProblemClicked(object sender, EventArgs e) { ProblemPopupOverlay.IsVisible = false; }

    // Bilder section
    private string GetAbsoluteImageUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        // If already absolute URL, return as-is
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        // Convert relative URL to absolute
        return $"{ApiService.BaseUrl}{url}";
    }

    private void LoadBilder()
    {
        AnmerkungenStack.Children.Clear();

        if (_task?.Bilder == null || _task.Bilder.Count == 0)
        {
            NoAnmerkungenLabel.IsVisible = true;
            return;
        }
        NoAnmerkungenLabel.IsVisible = false;

        foreach (var bild in _task.Bilder)
        {
            AnmerkungenStack.Children.Add(CreateBildView(bild));
        }
    }

    private View CreateBildView(BildStatus bild)
    {
        var border = new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#e0e0e0"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = 12 };
        border.Shadow = new Shadow { Brush = Colors.Gray, Offset = new Point(0, 2), Radius = 8, Opacity = 0.1f };

        // Horizontal layout: [Thumbnail] [Text] [X Button]
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        // Thumbnail on the left
        var imageUrl = bild.ThumbnailUrl ?? bild.FullUrl ?? bild.Url;
        var imgBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Stroke = Color.FromArgb("#e0e0e0"),
            WidthRequest = 70,
            HeightRequest = 70,
            BackgroundColor = Color.FromArgb("#E0E0E0")
        };
        var image = new Image { WidthRequest = 70, HeightRequest = 70, Aspect = Aspect.AspectFill };

        // Load image asynchronously
        var bildCopy = bild;
        _ = Task.Run(async () =>
        {
            var imageSource = await _apiService.GetImageAsync(imageUrl);
            if (imageSource != null)
                MainThread.BeginInvokeOnMainThread(() => image.Source = imageSource);
        });

        imgBorder.Content = image;
        grid.Children.Add(imgBorder);
        Grid.SetColumn(imgBorder, 0);

        // Note text in the middle
        var textStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        var notizText = !string.IsNullOrWhiteSpace(bild.Notiz) ? bild.Notiz : "(Keine Notiz)";
        textStack.Children.Add(new Label { Text = notizText, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#333333"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 2 });
        textStack.Children.Add(new Label { Text = bild.ErstelltAm ?? "", FontSize = 12, TextColor = Color.FromArgb("#999999") });
        grid.Children.Add(textStack);
        Grid.SetColumn(textStack, 1);

        // Big round X delete button on the right
        var deleteButton = new Button
        {
            Text = "\u2715",
            BackgroundColor = Color.FromArgb("#E91E63"),
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 44,
            HeightRequest = 44,
            CornerRadius = 22,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center
        };
        deleteButton.Clicked += (s, e) => DeleteBild(bildCopy.Id);
        grid.Children.Add(deleteButton);
        Grid.SetColumn(deleteButton, 2);

        // Tap to open detail
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => ShowBildDetail(bildCopy);
        border.GestureRecognizers.Add(tapGesture);

        border.Content = grid;
        return border;
    }

    private async void ShowBildDetail(BildStatus bild)
    {
        _currentBildDetail = bild;
        _editingBildId = bild.Id;

        // Use same popup as Add Note but in edit mode
        AnmerkungPopupTitle.Text = Translations.Get["edit_note"];
        AnmerkungNotizEditor.Text = bild.Notiz ?? "";
        _selectedBildPath = null;
        _selectedBildBytes = null;

        // Show existing image if available
        var imageUrl = bild.FullUrl ?? bild.ThumbnailUrl ?? bild.Url;
        if (!string.IsNullOrEmpty(imageUrl))
        {
            var imageSource = await _apiService.GetImageAsync(imageUrl);
            if (imageSource != null)
            {
                AnmerkungPreviewImage.Source = imageSource;
                AnmerkungPreviewBorder.IsVisible = true;
            }
            else
            {
                AnmerkungPreviewBorder.IsVisible = false;
            }
        }
        else
        {
            AnmerkungPreviewBorder.IsVisible = false;
        }

        AnmerkungPopupOverlay.IsVisible = true;
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
                await DisplayAlertAsync("Gespeichert", "Notiz wurde aktualisiert", "OK");
                await LoadTaskAsync();
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Speichern fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveBildDetail error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Notiz konnte nicht gespeichert werden", "OK");
        }
        finally
        {
            SaveBildDetailButton.Text = "Speichern";
            SaveBildDetailButton.IsEnabled = true;
        }
    }

    private void OnDeleteBildDetailClicked(object sender, EventArgs e)
    {
        if (_currentBildDetail == null) return;

        _bildIdToDelete = _currentBildDetail.Id;
        _deleteFromDetailPopup = true;
        DeleteBildPopupOverlay.IsVisible = true;
    }

    private void DeleteBild(int bildId)
    {
        _bildIdToDelete = bildId;
        _deleteFromDetailPopup = false;
        DeleteBildPopupOverlay.IsVisible = true;
    }

    private void OnDeleteBildPopupBackgroundTapped(object sender, EventArgs e)
    {
        DeleteBildPopupOverlay.IsVisible = false;
    }

    private void OnCancelDeleteBildClicked(object sender, EventArgs e)
    {
        DeleteBildPopupOverlay.IsVisible = false;
    }

    private async void OnConfirmDeleteBildClicked(object sender, EventArgs e)
    {
        DeleteBildPopupOverlay.IsVisible = false;

        try
        {
            var response = await _apiService.DeleteBildStatusAsync(_bildIdToDelete);
            if (response.Success)
            {
                // If triggered from detail popup, close it too
                if (_deleteFromDetailPopup)
                {
                    BildDetailPopupOverlay.IsVisible = false;
                    _currentBildDetail = null;
                }
                await DisplayAlertAsync("Gelöscht", "Bild wurde gelöscht", "OK");
                await LoadTaskAsync();
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Löschen fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteBild error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Bild konnte nicht gelöscht werden", "OK");
        }
    }

    private void OnAddAnmerkungClicked(object sender, EventArgs e)
    {
        _editingBildId = null;
        AnmerkungPopupTitle.Text = Translations.Get["add_note"];
        _selectedBildPath = null;
        _selectedBildBytes = null;
        AnmerkungPreviewBorder.IsVisible = false;
        AnmerkungNotizEditor.Text = "";
        AnmerkungPopupOverlay.IsVisible = true;
    }

    private void OnAnmerkungPopupBackgroundTapped(object sender, EventArgs e) { AnmerkungPopupOverlay.IsVisible = false; }
    private void OnCancelAnmerkungClicked(object sender, EventArgs e) { AnmerkungPopupOverlay.IsVisible = false; }

    private async void OnAnmerkungTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlertAsync("Fehler", "Kamera nicht verfügbar", "OK");
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
                var originalBytes = memoryStream.ToArray();

                _selectedBildBytes = await ImageHelper.CompressImageAsync(originalBytes);
                _selectedBildPath = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

                AnmerkungPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedBildBytes));
                AnmerkungPreviewBorder.IsVisible = true;
            }
        }
        catch (PermissionException)
        {
            await OfferOpenSettingsAsync("Kamera");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
    }

    private async void OnAnmerkungPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // FilePicker für bessere Android Scoped Storage Unterstützung
            var options = new PickOptions
            {
                PickerTitle = "Bild auswählen",
                FileTypes = FilePickerFileType.Images
            };
            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                // Stream direkt lesen
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var originalBytes = memoryStream.ToArray();

                // Komprimiere Bild auf max 2000px
                _selectedBildBytes = await ImageHelper.CompressImageAsync(originalBytes);
                var ext = System.IO.Path.GetExtension(result.FileName) ?? ".jpg";
                _selectedBildPath = $"gallery_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";

                AnmerkungPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedBildBytes));
                AnmerkungPreviewBorder.IsVisible = true;
            }
        }
        catch (PermissionException)
        {
            await DisplayAlertAsync("Berechtigung erforderlich", "Bitte erlaube den Foto-Zugriff in den App-Einstellungen", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Anmerkung Gallery error: {ex.Message}");
            await DisplayAlertAsync("Galerie-Fehler", ex.Message, "OK");
        }
    }

    private async void OnSaveAnmerkungClicked(object sender, EventArgs e)
    {
        var notiz = AnmerkungNotizEditor.Text?.Trim() ?? "";

        // Bei neuem Eintrag: Mindestens Notiz oder Bild erforderlich
        // Bei Bearbeitung: Notiz reicht
        if (!_editingBildId.HasValue && string.IsNullOrEmpty(notiz) && (_selectedBildBytes == null || _selectedBildBytes.Length == 0))
        {
            await DisplayAlertAsync("Fehler", "Bitte gib eine Notiz ein oder wähle ein Bild aus", "OK");
            return;
        }

        // Button deaktivieren während Upload
        SaveAnmerkungButton.IsEnabled = false;
        SaveAnmerkungButton.Text = "Wird gespeichert...";

        try
        {
            ApiResponse response;
            if (_editingBildId.HasValue)
            {
                // Update existing note
                System.Diagnostics.Debug.WriteLine($"OnSaveAnmerkungClicked: Update Bild {_editingBildId.Value}");
                response = await _apiService.UpdateBildStatusAsync(_editingBildId.Value, notiz);
            }
            else
            {
                // Create new note
                System.Diagnostics.Debug.WriteLine($"OnSaveAnmerkungClicked: Start Upload");
                var fileName = _selectedBildPath ?? $"note_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                response = await _apiService.UploadBildStatusBytesAsync(_taskId, _selectedBildBytes, fileName, notiz);
            }

            if (response.Success)
            {
                AnmerkungPopupOverlay.IsVisible = false;
                _selectedBildBytes = null;
                _selectedBildPath = null;
                _editingBildId = null;
                await DisplayAlertAsync("Gespeichert", "Notiz wurde gespeichert", "OK");
                await LoadTaskAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSaveAnmerkungClicked: Speichern fehlgeschlagen - {response.Error}");
                await DisplayAlertAsync("Fehler", response.Error ?? "Speichern fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save Anmerkung error: {ex.Message}\n{ex.StackTrace}");
            await DisplayAlertAsync("Fehler", $"Fehler: {ex.Message}", "OK");
        }
        finally
        {
            _editingBildId = null;
            SaveAnmerkungButton.Text = "Speichern";
            SaveAnmerkungButton.IsEnabled = true;
        }
    }

    // ============================================
    // Protokoll / Logs
    // ============================================

    private async Task LoadLogsAsync()
    {
        try
        {
            LogsStack.Children.Clear();
            NoLogsLabel.IsVisible = false;

            var logs = await _apiService.GetTaskLogsAsync(_taskId);

            if (logs == null || logs.Count == 0)
            {
                NoLogsLabel.IsVisible = true;
                return;
            }

            foreach (var log in logs)
            {
                var logEntry = new Border
                {
                    BackgroundColor = Color.FromArgb("#f8f9fa"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Stroke = Colors.Transparent,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var content = new VerticalStackLayout { Spacing = 4 };

                // Date/Time
                content.Children.Add(new Label
                {
                    Text = log.DatumZeit,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#999999")
                });

                // User
                content.Children.Add(new Label
                {
                    Text = log.User,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#1a3a5c"),
                    FontAttributes = FontAttributes.Bold
                });

                // Text
                content.Children.Add(new Label
                {
                    Text = log.Text,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#333333")
                });

                logEntry.Content = content;
                LogsStack.Children.Add(logEntry);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadLogsAsync error: {ex.Message}");
            NoLogsLabel.Text = Translations.Get("error");
            NoLogsLabel.IsVisible = true;
        }
    }
}
