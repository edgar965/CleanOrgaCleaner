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
    private readonly WebSocketService _webSocketService;
    private int _taskId;
    private CleaningTask? _task;
    private List<(string FileName, byte[] Bytes)> _selectedPhotos = new(); // Store photo bytes for Problem
    private string? _selectedBildPath;
    private byte[]? _selectedBildBytes; // Store bytes in memory for BildStatus upload
    private BildStatus? _currentBildDetail;
    private string _currentTab = "aufgabe"; // Track current tab for state persistence

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
        _webSocketService = WebSocketService.Instance;
    }

    public AufgabePage(int taskId) : this() { _taskId = taskId; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to connection status
        _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;
        UpdateOfflineBanner(!_webSocketService.IsOnline);

        ApplyTranslations();
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

        // Header
        MenuButton.Text = "\u2261  " + t("menu") + " \u25BC";
        LogoutButton.Text = t("logout");
        DateLabel.Text = DateTime.Now.ToString("dd.MM.yyyy");
        UserInfoLabel.Text = ApiService.Instance.CleanerName ?? Preferences.Get("username", "");

        // Menu - fixed text, no dynamic override
        MenuTodayButton.Text = t("today");
        MenuChatButton.Text = t("chat");
        MenuAuftragButton.Text = t("task");
        MenuSettingsButton.Text = t("settings");

        // Buttons
        CancelButton.Text = t("back");
        AddProblemButton.Text = $"⚠️ {t("report_problem")}";
        AddAnmerkungButton.Text = $"+ {t("add_note").ToUpper()}";

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
    }

    // Menu handling
    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainTabs/TodayPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await ApiService.Instance.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
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

    private async void OnMenuAuftragClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/AuftragPage");
    }

    private async void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/SettingsPage");
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

    private void SelectTab(string tab)
    {
        // Save current tab for state persistence
        _currentTab = tab;

        // Reset all tab buttons
        TabAufgabeButton.BackgroundColor = Color.FromArgb("#e0e0e0");
        TabAufgabeButton.TextColor = Color.FromArgb("#666666");
        TabProblemeButton.BackgroundColor = Color.FromArgb("#e0e0e0");
        TabProblemeButton.TextColor = Color.FromArgb("#666666");
        TabAnmerkungenButton.BackgroundColor = Color.FromArgb("#e0e0e0");
        TabAnmerkungenButton.TextColor = Color.FromArgb("#666666");

        // Hide all tab content
        TabAufgabeContent.IsVisible = false;
        TabProblemeContent.IsVisible = false;
        TabAnmerkungenContent.IsVisible = false;

        // Activate selected tab
        switch (tab)
        {
            case "aufgabe":
                TabAufgabeButton.BackgroundColor = Color.FromArgb("#6c5ce7");
                TabAufgabeButton.TextColor = Colors.White;
                TabAufgabeContent.IsVisible = true;
                break;
            case "probleme":
                TabProblemeButton.BackgroundColor = Color.FromArgb("#6c5ce7");
                TabProblemeButton.TextColor = Colors.White;
                TabProblemeContent.IsVisible = true;
                break;
            case "anmerkungen":
                TabAnmerkungenButton.BackgroundColor = Color.FromArgb("#6c5ce7");
                TabAnmerkungenButton.TextColor = Colors.White;
                TabAnmerkungenContent.IsVisible = true;
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
                await DisplayAlert("Fehler", "Aufgabe nicht gefunden", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            ApartmentLabel.Text = _task.ApartmentName;

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

        Console.WriteLine($"=== LoadBilder START ===");
        Console.WriteLine($"Task ID: {_task?.Id}, Bilder Count: {bilderCount}");

        // Update badge on tab
        if (bilderCount > 0)
        {
            AnmerkungenBadge.IsVisible = true;
            AnmerkungenBadgeLabel.Text = bilderCount.ToString();
        }
        else
        {
            AnmerkungenBadge.IsVisible = false;
        }

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

                AnmerkungPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedBildBytes));
                AnmerkungPreviewBorder.IsVisible = true;
            }
        }
        catch (PermissionException)
        {
            await DisplayAlert("Berechtigung erforderlich", "Bitte erlaube den Kamera-Zugriff in den App-Einstellungen", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Anmerkung Camera error: {ex.Message}");
            await DisplayAlert("Kamera-Fehler", ex.Message, "OK");
        }
    }

    private async void OnAnmerkungPickPhotoClicked(object sender, EventArgs e)
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

                AnmerkungPreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(_selectedBildBytes));
                AnmerkungPreviewBorder.IsVisible = true;
            }
        }
        catch (PermissionException)
        {
            await DisplayAlert("Berechtigung erforderlich", "Bitte erlaube den Foto-Zugriff in den App-Einstellungen", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Anmerkung Gallery error: {ex.Message}");
            await DisplayAlert("Galerie-Fehler", ex.Message, "OK");
        }
    }

    private async void OnSaveAnmerkungClicked(object sender, EventArgs e)
    {
        var notiz = AnmerkungNotizEditor.Text?.Trim() ?? "";

        // Mindestens Notiz oder Bild erforderlich
        if (string.IsNullOrEmpty(notiz) && (_selectedBildBytes == null || _selectedBildBytes.Length == 0))
        {
            await DisplayAlert("Fehler", "Bitte gib eine Notiz ein oder wähle ein Bild aus", "OK");
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
                await DisplayAlert("Gespeichert", "Anmerkung wurde gespeichert", "OK");
                await LoadTaskAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSaveAnmerkungClicked: Upload fehlgeschlagen - {response.Error}");
                await DisplayAlert("Fehler", response.Error ?? "Speichern fehlgeschlagen", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Upload Anmerkung error: {ex.Message}\n{ex.StackTrace}");
            await DisplayAlert("Fehler", $"Fehler: {ex.Message}", "OK");
        }
        finally
        {
            SaveAnmerkungButton.Text = "Speichern";
            SaveAnmerkungButton.IsEnabled = true;
        }
    }
}
