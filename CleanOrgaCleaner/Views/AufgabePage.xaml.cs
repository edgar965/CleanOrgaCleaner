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
    private string _currentTab = "aufgabe";

    // ImageListDescriptionDialog state
    private string _currentItemType = "problem"; // "problem" or "anmerkung"
    private int? _editingItemId; // null = creating new, int = editing existing
    private List<(string FileName, byte[] Bytes)> _selectedPhotos = new();

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

        _ = Header.InitializeAsync();
        Header.SetPageTitle("today");

        ApplyTranslations();
        _ = LoadTaskAsync();
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("task");

        // Tab Buttons
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

        // ImageListDescriptionDialog
        ImageListDescriptionDialogNameLabel.Text = $"{t("name")} *";
        ImageListDescriptionDialogDescLabel.Text = t("description");
        ImageListDescriptionDialogPhotosLabel.Text = t("photos");
        ImageListDescriptionDialogTakePhotoButton.Text = t("camera");
        ImageListDescriptionDialogPickPhotoButton.Text = t("gallery");
        SaveImageListDescriptionDialogButton.Text = t("save");
        CancelImageListDescriptionDialogButton.Text = t("cancel");

        // Complete Task Popup
        CompleteTaskTitle.Text = t("task_completed");
        CompleteTaskMessage.Text = t("task_completed_question");
        CancelCompleteTaskButton.Text = t("no");
        ConfirmCompleteTaskButton.Text = t("yes");
    }

    // Tab handling
    private void OnTabAufgabeClicked(object sender, EventArgs e) => SelectTab("aufgabe");
    private void OnTabProblemeClicked(object sender, EventArgs e) => SelectTab("probleme");
    private void OnTabAnmerkungenClicked(object sender, EventArgs e) => SelectTab("anmerkungen");
    private async void OnTabLogsClicked(object sender, EventArgs e)
    {
        SelectTab("logs");
        await LoadLogsAsync();
    }

    private void SelectTab(string tab)
    {
        _currentTab = tab;

        // Reset all tab buttons
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

        // Activate selected tab
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
                System.Diagnostics.Debug.WriteLine($"LoadTask: Task {_taskId} not found");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try { await Shell.Current.GoToAsync(".."); } catch { }
                });
                return;
            }

            TaskNameLabel.Text = $"{_task.ApartmentName} {_task.DisplayName}";

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
            BuildAnmerkungen();
            SelectTab(_currentTab);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTask error: {ex.Message}");
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
                StartStopButton.IsEnabled = true;
                break;
        }
    }

    private string GetTranslatedAufgabe()
    {
        if (_task == null) return string.Empty;
        string currentLang = Translations.CurrentLanguage;
        if (currentLang == "de") return _task.Aufgabe ?? string.Empty;
        if (_task.AufgabeTranslated != null && _task.AufgabeTranslated.TryGetValue(currentLang, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;
        return _task.Aufgabe ?? string.Empty;
    }

    private async void OnStartStopClicked(object sender, EventArgs e)
    {
        if (_task == null) return;

        if (_task.StateCompleted == "started")
        {
            CompleteTaskPopupOverlay.IsVisible = true;
            return;
        }

        StartStopButton.IsEnabled = false;
        try
        {
            string newState = _task.StateCompleted switch
            {
                "not_started" => "started",
                "completed" => "not_started",
                _ => ""
            };
            if (string.IsNullOrEmpty(newState)) return;

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

    private void OnCompleteTaskPopupBackgroundTapped(object sender, EventArgs e) => CompleteTaskPopupOverlay.IsVisible = false;
    private void OnCancelCompleteTaskClicked(object sender, EventArgs e) => CompleteTaskPopupOverlay.IsVisible = false;

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

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Task.Delay(150);
                    try { await Shell.Current.GoToAsync(".."); }
                    catch { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); }
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

    #region Problems Tab

    private void BuildProblems()
    {
        ProblemsStack.Children.Clear();
        var problems = _task?.Problems;

        if (problems == null || problems.Count == 0)
        {
            NoProblemsLabel.IsVisible = true;
            return;
        }
        NoProblemsLabel.IsVisible = false;

        foreach (var problem in problems)
            ProblemsStack.Children.Add(CreateImageListDescriptionView(problem));
    }

    private void OnAddProblemClicked(object sender, EventArgs e)
    {
        _currentItemType = "problem";
        _editingItemId = null;
        ImageListDescriptionDialogTitle.Text = Translations.Get("report_problem");
        ImageListDescriptionDialogNameEntry.Text = "";
        ImageListDescriptionDialogDescEditor.Text = "";
        ImageListDescriptionDialogCharCountLabel.Text = "0 / 300";
        _selectedPhotos.Clear();
        UpdateDialogPhotoPreview();
        ImageListDescriptionDialog.IsVisible = true;
    }

    #endregion

    #region Anmerkungen Tab

    private void BuildAnmerkungen()
    {
        AnmerkungenStack.Children.Clear();
        var anmerkungen = _task?.Anmerkungen;

        if (anmerkungen == null || anmerkungen.Count == 0)
        {
            NoAnmerkungenLabel.IsVisible = true;
            return;
        }
        NoAnmerkungenLabel.IsVisible = false;

        foreach (var anmerkung in anmerkungen)
            AnmerkungenStack.Children.Add(CreateImageListDescriptionView(anmerkung));
    }

    private void OnAddAnmerkungClicked(object sender, EventArgs e)
    {
        _currentItemType = "anmerkung";
        _editingItemId = null;
        ImageListDescriptionDialogTitle.Text = Translations.Get("add_note");
        ImageListDescriptionDialogNameEntry.Text = "";
        ImageListDescriptionDialogDescEditor.Text = "";
        ImageListDescriptionDialogCharCountLabel.Text = "0 / 300";
        _selectedPhotos.Clear();
        UpdateDialogPhotoPreview();
        ImageListDescriptionDialog.IsVisible = true;
    }

    #endregion

    #region ImageListDescription View

    private View CreateImageListDescriptionView(ImageListDescription item)
    {
        var border = new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#e0e0e0"), StrokeShape = new RoundRectangle { CornerRadius = 12 }, Padding = 12 };
        border.Shadow = new Shadow { Brush = Colors.Gray, Offset = new Point(0, 2), Radius = 8, Opacity = 0.1f };

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

        // First photo thumbnail
        if (item.HasPhotos && item.Photos != null)
        {
            var imageUrl = item.Photos[0].ThumbnailUrl ?? item.Photos[0].Url;
            var imgBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Stroke = Color.FromArgb("#e0e0e0"),
                WidthRequest = 70,
                HeightRequest = 70,
                BackgroundColor = Color.FromArgb("#E0E0E0")
            };
            var image = new Image { WidthRequest = 70, HeightRequest = 70, Aspect = Aspect.AspectFill };

            _ = Task.Run(async () =>
            {
                var imageSource = await _apiService.GetImageAsync(imageUrl);
                if (imageSource != null)
                    MainThread.BeginInvokeOnMainThread(() => image.Source = imageSource);
            });

            imgBorder.Content = image;
            grid.Children.Add(imgBorder);
            Grid.SetColumn(imgBorder, 0);
        }

        // Name + description
        var textStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        textStack.Children.Add(new Label { Text = item.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#333333") });
        if (!string.IsNullOrEmpty(item.Description))
            textStack.Children.Add(new Label { Text = item.Description, FontSize = 13, TextColor = Color.FromArgb("#666666"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });
        grid.Children.Add(textStack);
        Grid.SetColumn(textStack, 1);

        // Delete button
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
        var itemId = item.Id;
        var itemType = item.Type;
        deleteButton.Clicked += async (s, e) => await DeleteImageListDescription(itemId, itemType);
        grid.Children.Add(deleteButton);
        Grid.SetColumn(deleteButton, 2);

        // Tap to open dialog
        var itemCopy = item;
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => ShowImageListDescriptionInDialog(itemCopy);
        border.GestureRecognizers.Add(tapGesture);

        border.Content = grid;
        return border;
    }

    private void ShowImageListDescriptionInDialog(ImageListDescription item)
    {
        _currentItemType = item.Type;
        _editingItemId = item.Id;
        ImageListDescriptionDialogTitle.Text = item.IsProblem ? Translations.Get("edit_problem") : Translations.Get("edit_note");
        ImageListDescriptionDialogNameEntry.Text = item.Name ?? "";
        ImageListDescriptionDialogDescEditor.Text = item.Description ?? "";
        ImageListDescriptionDialogCharCountLabel.Text = $"{(item.Description?.Length ?? 0)} / 300";
        _selectedPhotos.Clear();
        UpdateDialogPhotoPreview();
        ImageListDescriptionDialog.IsVisible = true;
    }

    private async Task DeleteImageListDescription(int itemId, string type)
    {
        string title = type == "problem" ? Translations.Get("delete_problem_title") : Translations.Get("delete_image");
        string message = type == "problem" ? Translations.Get("delete_problem_confirm") : Translations.Get("confirm_delete_image");

        bool confirm = await DisplayAlert(title, message, Translations.Get("yes_delete"), Translations.Get("cancel"));
        if (confirm)
        {
            var response = await _apiService.DeleteImageListItemAsync(itemId);

            if (response.Success)
                await LoadTaskAsync();
            else
                await DisplayAlertAsync("Fehler", response.Error ?? "Fehler beim Loeschen", "OK");
        }
    }

    #endregion

    #region ImageListDescriptionDialog Handlers

    private void OnImageListDescriptionDialogBackgroundTapped(object sender, EventArgs e) => ImageListDescriptionDialog.IsVisible = false;
    private void OnCancelImageListDescriptionDialogClicked(object sender, EventArgs e) => ImageListDescriptionDialog.IsVisible = false;

    private void OnImageListDescriptionDialogDescTextChanged(object sender, TextChangedEventArgs e)
    {
        var length = e.NewTextValue?.Length ?? 0;
        ImageListDescriptionDialogCharCountLabel.Text = $"{length} / 300";
        if (length > 300)
            ImageListDescriptionDialogDescEditor.Text = e.NewTextValue?.Substring(0, 300);
    }

    private async void OnImageListDescriptionDialogTakePhotoClicked(object sender, EventArgs e)
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

                try
                {
                    var annotationPage = new ImageAnnotationPage(bytes);
                    await Navigation.PushModalAsync(annotationPage);

                    var tcs = new TaskCompletionSource<bool>();
                    annotationPage.Disappearing += (s, ev) => tcs.TrySetResult(true);
                    await tcs.Task;

                    var finalBytes = annotationPage.WasSaved && annotationPage.AnnotatedImageBytes != null
                        ? annotationPage.AnnotatedImageBytes
                        : bytes;

                    var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                    _selectedPhotos.Add((fileName, finalBytes));
                    UpdateDialogPhotoPreview();
                }
                catch
                {
                    var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                    _selectedPhotos.Add((fileName, bytes));
                    UpdateDialogPhotoPreview();
                }
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
            Services.PermissionHelper.OpenAppSettings();
    }

    private async void OnImageListDescriptionDialogPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var options = new PickOptions { PickerTitle = "Foto auswählen", FileTypes = FilePickerFileType.Images };
            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                var annotationPage = new ImageAnnotationPage(bytes);
                await Navigation.PushModalAsync(annotationPage);

                var tcs = new TaskCompletionSource<bool>();
                annotationPage.Disappearing += (s2, e2) => tcs.TrySetResult(true);
                await tcs.Task;

                var finalBytes = annotationPage.WasSaved && annotationPage.AnnotatedImageBytes != null
                    ? annotationPage.AnnotatedImageBytes
                    : bytes;

                var ext = System.IO.Path.GetExtension(result.FileName) ?? ".jpg";
                var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                _selectedPhotos.Add((fileName, finalBytes));
                UpdateDialogPhotoPreview();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Galerie konnte nicht geöffnet werden", "OK");
        }
    }

    private void UpdateDialogPhotoPreview()
    {
        ImageListDescriptionDialogPhotoPreviewStack.Children.Clear();
        if (_selectedPhotos.Count == 0)
        {
            ImageListDescriptionDialogPhotoPreviewStack.IsVisible = false;
            ImageListDescriptionDialogPhotoCountLabel.IsVisible = false;
            return;
        }

        ImageListDescriptionDialogPhotoPreviewStack.IsVisible = true;
        ImageListDescriptionDialogPhotoCountLabel.IsVisible = true;
        ImageListDescriptionDialogPhotoCountLabel.Text = $"{_selectedPhotos.Count} Foto(s)";

        for (int i = 0; i < _selectedPhotos.Count; i++)
        {
            var photo = _selectedPhotos[i];
            var index = i;
            var grid = new Grid { WidthRequest = 70, HeightRequest = 70 };
            var imageContainer = new Border { StrokeShape = new RoundRectangle { CornerRadius = 8 }, Stroke = Colors.Transparent };
            imageContainer.Content = new Image { Source = ImageSource.FromStream(() => new MemoryStream(photo.Bytes)), Aspect = Aspect.AspectFill };
            grid.Children.Add(imageContainer);
            var deleteBtn = new Button { Text = "X", BackgroundColor = Color.FromArgb("#c62828"), TextColor = Colors.White, FontSize = 10, WidthRequest = 22, HeightRequest = 22, CornerRadius = 11, Padding = 0, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 2, 2, 0) };
            deleteBtn.Clicked += (s, e) => { _selectedPhotos.RemoveAt(index); UpdateDialogPhotoPreview(); };
            grid.Children.Add(deleteBtn);
            ImageListDescriptionDialogPhotoPreviewStack.Children.Add(grid);
        }
    }

    private async void OnSaveImageListDescriptionDialogClicked(object sender, EventArgs e)
    {
        var name = ImageListDescriptionDialogNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlertAsync("Fehler", "Bitte gib einen Namen ein", "OK");
            return;
        }

        var description = ImageListDescriptionDialogDescEditor.Text?.Trim();
        ImageListDescriptionDialog.IsVisible = false;

        try
        {
            bool success;
            string? error;

            if (_editingItemId.HasValue)
            {
                // Update existing
                var response = await _apiService.UpdateImageListItemAsync(_editingItemId.Value, name, description);
                success = response.Success;
                error = response.Error;

                if (success) await LoadTaskAsync();
                else await DisplayAlertAsync("Fehler", error ?? "Fehler beim Aktualisieren", "OK");
            }
            else
            {
                // Create new - unified for both problem and anmerkung
                var response = await _apiService.CreateImageListItemAsync(_taskId, _currentItemType, name, description, _selectedPhotos);
                success = response.Success;
                error = response.Error;

                if (success)
                {
                    string confirmMsg = _currentItemType == "problem" ? "Problem wurde gemeldet" : "Anmerkung wurde gespeichert";
                    await DisplayAlertAsync("Gespeichert", confirmMsg, "OK");
                    await LoadTaskAsync();
                }
                else await DisplayAlertAsync("Fehler", error ?? "Fehler beim Speichern", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Konnte nicht gespeichert werden", "OK");
        }
        finally
        {
            _editingItemId = null;
        }
    }

    #endregion

    #region Logs Tab

    private string TranslateLogText(string text)
    {
        var translations = new Dictionary<string, string>
        {
            { "Anmerkung hinzugefügt", Translations.Get("log_note_added") },
            { "Bild gelöscht", Translations.Get("log_image_deleted") },
            { "Problem gemeldet", Translations.Get("log_problem_reported") },
            { "Problem gelöscht", Translations.Get("log_problem_deleted") },
            { "Aufgabe erstellt", Translations.Get("log_task_created") },
            { "Aufgabe aktualisiert", Translations.Get("log_task_updated") },
            { "Reparatur-Aufgabe erstellt", Translations.Get("log_repair_task_created") },
            { "Reinigung zugewiesen an", Translations.Get("log_cleaning_assigned_to") },
            { "Zuweisung entfernt", Translations.Get("log_assignment_removed") },
            { "Fortschritt:", Translations.Get("log_progress") + ":" },
            { "Status geändert:", Translations.Get("log_status_changed") + ":" },
            { "Checkliste aktualisiert", Translations.Get("log_checklist_updated") },
            { "Nicht gestartet", Translations.Get("log_not_started") },
            { "Gestartet", Translations.Get("log_started") },
            { "Abgeschlossen", Translations.Get("log_completed") }
        };

        var result = text;
        foreach (var kvp in translations)
        {
            if (!string.IsNullOrEmpty(kvp.Value) && kvp.Value != kvp.Key)
                result = result.Replace(kvp.Key, kvp.Value);
        }
        return result;
    }

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
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Stroke = Colors.Transparent,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var content = new VerticalStackLayout { Spacing = 4 };
                content.Children.Add(new Label { Text = log.DatumZeit, FontSize = 12, TextColor = Color.FromArgb("#999999") });
                content.Children.Add(new Label { Text = log.User, FontSize = 12, TextColor = Color.FromArgb("#1a3a5c"), FontAttributes = FontAttributes.Bold });
                content.Children.Add(new Label { Text = TranslateLogText(log.Text), FontSize = 14, TextColor = Color.FromArgb("#333333") });

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

    #endregion
}
