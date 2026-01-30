using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
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
        DeleteBildButton.Text = t("delete");
        CloseBildButton.Text = t("cancel");
        SaveBildDetailButton.Text = t("save");

        // Delete Problem Popup
        DeleteProblemTitle.Text = t("delete_problem_title");
        DeleteProblemMessage.Text = t("delete_problem_confirm");
        CancelDeleteProblemButton.Text = t("cancel");
        ConfirmDeleteProblemButton.Text = t("yes_delete");

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
            var response = await _apiService.ReportProblemWithBytesAsync(_taskId, name, beschreibung, _selectedPhotos);
            if (response.Success) { await DisplayAlertAsync("Gemeldet", "Problem wurde gemeldet", "OK"); await LoadTaskAsync(); }
            else await DisplayAlertAsync("Fehler", response.Error ?? "Fehler beim Melden", "OK");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Report problem error: {ex.Message}"); await DisplayAlertAsync("Fehler", "Problem konnte nicht gemeldet werden", "OK"); }
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

    private async void LoadBilder()
    {
        AnmerkungenStack.Children.Clear();
        var bilderCount = _task?.Bilder?.Count ?? 0;

        if (_task?.Bilder == null || _task.Bilder.Count == 0)
        {
            Console.WriteLine($"LoadBilder: No images found, exiting");
            NoAnmerkungenLabel.IsVisible = true;
            return;
        }
        NoAnmerkungenLabel.IsVisible = false;

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
                    Text = "X",
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
                        Text = "N",
                        FontSize = 10
                    }
                };
                grid.Children.Add(notizIndicator);
            }

            grid.Children.Add(imageBorder);
            grid.Children.Add(deleteBtn);
            AnmerkungenStack.Children.Add(grid);
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

    private async void OnDeleteBildDetailClicked(object sender, EventArgs e)
    {
        if (_currentBildDetail == null) return;

        var confirm = await DisplayAlertAsync("Bild löschen", "Möchtest du dieses Bild wirklich löschen?", "Ja, löschen", "Abbrechen");
        if (!confirm) return;

        try
        {
            var response = await _apiService.DeleteBildStatusAsync(_currentBildDetail.Id);
            if (response.Success)
            {
                BildDetailPopupOverlay.IsVisible = false;
                _currentBildDetail = null;
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
            System.Diagnostics.Debug.WriteLine($"DeleteBildDetail error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Bild konnte nicht gelöscht werden", "OK");
        }
    }

    private async Task DeleteBild(int bildId)
    {
        var confirm = await DisplayAlertAsync("Bild löschen", "Möchtest du dieses Bild wirklich löschen?", "Ja, löschen", "Abbrechen");
        if (!confirm) return;

        try
        {
            var response = await _apiService.DeleteBildStatusAsync(bildId);
            if (response.Success)
            {
                await DisplayAlertAsync("Gelöscht", "Bild wurde gelöscht", "OK");
                await LoadTaskAsync(); // Refresh
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

        // Mindestens Notiz oder Bild erforderlich
        if (string.IsNullOrEmpty(notiz) && (_selectedBildBytes == null || _selectedBildBytes.Length == 0))
        {
            await DisplayAlertAsync("Fehler", "Bitte gib eine Notiz ein oder wähle ein Bild aus", "OK");
            return;
        }

        // Button deaktivieren während Upload
        SaveAnmerkungButton.IsEnabled = false;
        SaveAnmerkungButton.Text = "Wird gespeichert...";

        try
        {
            System.Diagnostics.Debug.WriteLine($"OnSaveAnmerkungClicked: Start Upload");
            var fileName = _selectedBildPath ?? $"note_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var response = await _apiService.UploadBildStatusBytesAsync(_taskId, _selectedBildBytes, fileName, notiz);

            if (response.Success)
            {
                AnmerkungPopupOverlay.IsVisible = false;
                _selectedBildBytes = null;
                _selectedBildPath = null;
                await DisplayAlertAsync("Gespeichert", "Anmerkung wurde gespeichert", "OK");
                await LoadTaskAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSaveAnmerkungClicked: Upload fehlgeschlagen - {response.Error}");
                await DisplayAlertAsync("Fehler", response.Error ?? "Speichern fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Upload Anmerkung error: {ex.Message}\n{ex.StackTrace}");
            await DisplayAlertAsync("Fehler", $"Fehler: {ex.Message}", "OK");
        }
        finally
        {
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

                // Left border accent
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(new GridLength(4)), new ColumnDefinition(GridLength.Star) } };

                var accent = new BoxView { Color = Color.FromArgb("#667eea"), VerticalOptions = LayoutOptions.Fill };
                grid.Children.Add(accent);
                Grid.SetColumn(accent, 0);

                var content = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(10, 0, 0, 0) };

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
                    TextColor = Color.FromArgb("#667eea"),
                    FontAttributes = FontAttributes.Bold
                });

                // Text
                content.Children.Add(new Label
                {
                    Text = log.Text,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#333333")
                });

                grid.Children.Add(content);
                Grid.SetColumn(content, 1);

                logEntry.Content = grid;
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
