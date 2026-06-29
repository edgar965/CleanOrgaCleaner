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
    private readonly WebSocketService _webSocketService;
    private int _taskId;
    private CleaningTask? _task;
    private string _currentTab = "aufgabe";

    // ImageListDescriptionDialog state
    private string _currentItemType = "problem"; // "problem" or "anmerkung"
    private int? _editingItemId; // null = creating new, int = editing existing
    private ImageListDescription? _editingItem; // Full item for accessing existing photos
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
        _webSocketService = WebSocketService.Instance;
    }

    public AufgabePage(int taskId) : this() { _taskId = taskId; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _ = Header.InitializeAsync();
        Header.SetPageTitle("today");

        _webSocketService.OnTaskUpdate += OnTaskUpdate;

        ApplyTranslations();
        _ = LoadTaskAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
    }

    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[AufgabePage] Task update received: {updateType}");
        if (updateType == "task_created" || updateType == "task_updated" || updateType == "task_deleted"
            || updateType == "assignment_update" || updateType == "aufgabe_update"
            || updateType == "image_list_update" || updateType == "problem_update" || updateType == "problem_delete")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadTaskAsync();
            });
        }
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
    private void OnTabChecklisteClicked(object sender, EventArgs e) { SelectTab("checkliste"); BuildCheckliste(); }
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
        TabChecklisteButton.BackgroundColor = Color.FromArgb("#1a3a5c");
        TabChecklisteButton.TextColor = Colors.White;
        TabChecklisteButton.BorderWidth = 0;
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
        TabChecklisteContent.IsVisible = false;
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
            case "checkliste":
                TabChecklisteButton.BackgroundColor = Colors.White;
                TabChecklisteButton.TextColor = Color.FromArgb("#1a3a5c");
                TabChecklisteButton.BorderColor = borderColor;
                TabChecklisteButton.BorderWidth = 2;
                TabChecklisteContent.IsVisible = true;
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
            // Performance: zuerst den bereits geladenen Cache nutzen (kein voller Reload pro Klick).
            // Fällt automatisch auf Nachladen zurück, wenn die Aufgabe nicht im Cache ist.
            _task = await _apiService.GetAufgabeDetailAsync(_taskId, forceRefresh: false);
            if (_task == null)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTask: Task {_taskId} not found");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try { await Shell.Current.GoToAsync(".."); } catch { }
                });
                return;
            }

            TaskNameLabel.Text = $"{_task.ApartmentName} {TranslateTaskType(_task.DisplayName)}";

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

    private string TranslateTaskType(string taskType)
    {
        if (string.IsNullOrEmpty(taskType)) return taskType;

        // Map German task types to translation keys
        var taskTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Reinigung", "cleaning" },
            { "Check", "check_task" },
            { "Reparatur", "repair" },
            { "Putzen", "cleaning" }
        };

        if (taskTypeMap.TryGetValue(taskType, out var key))
        {
            var translated = Translations.Get(key);
            // Return translated if different from key, otherwise original
            return !string.IsNullOrEmpty(translated) && translated != key ? translated : taskType;
        }

        return taskType;
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
            else if (IsNetworkError(response.Error))
            {
                // Queue for offline sync
                await OfflineQueueService.Instance.EnqueueTaskStateChangeAsync(_taskId, newState);
                _task.StateCompleted = newState;
                UpdateStartStopButton();
                await DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));
            }
            else
            {
                await DisplayAlertAsync("Fehler", response.Error ?? "Status konnte nicht geaendert werden", "OK");
            }
        }
        catch (Exception ex)
        {
            if (IsNetworkError(ex.Message))
            {
                string newState = _task.StateCompleted switch
                {
                    "not_started" => "started",
                    "completed" => "not_started",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(newState))
                {
                    await OfflineQueueService.Instance.EnqueueTaskStateChangeAsync(_taskId, newState);
                    _task.StateCompleted = newState;
                    UpdateStartStopButton();
                    await DisplayAlertAsync(
                        Translations.Get("no_connection"),
                        Translations.Get("saved_offline"),
                        Translations.Get("ok"));
                }
            }
            else
            {
                await DisplayAlertAsync(
                    Translations.Get("error"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
            }
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
            else if (IsNetworkError(response.Error))
            {
                // Queue for offline sync
                await OfflineQueueService.Instance.EnqueueTaskStateChangeAsync(_taskId, "completed");
                _task!.StateCompleted = "completed";
                UpdateStartStopButton();
                await DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));

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
            if (IsNetworkError(ex.Message))
            {
                await OfflineQueueService.Instance.EnqueueTaskStateChangeAsync(_taskId, "completed");
                _task!.StateCompleted = "completed";
                UpdateStartStopButton();
                await DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Task.Delay(150);
                    try { await Shell.Current.GoToAsync(".."); }
                    catch { if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync(); }
                });
            }
            else
            {
                await DisplayAlertAsync(
                    Translations.Get("error"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
                StartStopButton.IsEnabled = true;
            }
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
        _editingItem = null;
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
        _editingItem = null;
        ImageListDescriptionDialogTitle.Text = Translations.Get("add_note");
        ImageListDescriptionDialogNameEntry.Text = "";
        ImageListDescriptionDialogDescEditor.Text = "";
        ImageListDescriptionDialogCharCountLabel.Text = "0 / 300";
        _selectedPhotos.Clear();
        UpdateDialogPhotoPreview();
        ImageListDescriptionDialog.IsVisible = true;
    }

    #endregion

    #region Checkliste Tab

    private bool _suppressPutzCheck = false;

    private void BuildCheckliste()
    {
        ChecklisteStack.Children.Clear();
        var entries = _task?.Putzliste;
        if (entries == null || entries.Count == 0)
        {
            NoChecklisteLabel.IsVisible = true;
            ChecklisteKommentarBox.IsVisible = false;
            return;
        }
        NoChecklisteLabel.IsVisible = false;
        // Anmerkung zur gesamten Checkliste
        ChecklisteKommentarEditor.Text = _task?.PutzlisteKommentar ?? "";
        ChecklisteKommentarBox.IsVisible = true;
        // Tabellarisch: Zeilen (Bild | Name | Abgehakt | Bearbeiten), ohne Kopfzeile
        foreach (var entry in entries)
            ChecklisteStack.Children.Add(BuildChecklistRow(entry));
    }

    private async void OnChecklisteKommentarUnfocused(object sender, FocusEventArgs e)
    {
        if (_task == null) return;
        var text = ChecklisteKommentarEditor.Text ?? "";
        if ((_task.PutzlisteKommentar ?? "") == text) return;  // keine Änderung
        var resp = await _apiService.SavePutzlisteChecklistKommentarAsync(_taskId, text);
        if (resp.Success) _task.PutzlisteKommentar = text;
    }

    private ColumnDefinitionCollection _cklCols() => new ColumnDefinitionCollection
    {
        new ColumnDefinition(GridLength.Star),       // Name
        new ColumnDefinition(new GridLength(46)),    // Vorgabebild
        new ColumnDefinition(new GridLength(46)),    // Cleaner-Foto
        new ColumnDefinition(GridLength.Auto),       // Abgehakt
        new ColumnDefinition(GridLength.Auto)        // Bearbeiten (Symbol)
    };

    private View BuildChecklistRow(PutzlisteEintrag entry)
    {
        var g = new Grid { ColumnDefinitions = _cklCols(), Padding = new Thickness(2, 10, 2, 10), ColumnSpacing = 6 };

        // Spalte 1: Name (+ Badge für Anmerkung)
        var nameStack = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        nameStack.Children.Add(new Label { Text = entry.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#222222"), VerticalOptions = LayoutOptions.Center });
        if (!string.IsNullOrWhiteSpace(entry.Kommentar))
            nameStack.Children.Add(new Label { Text = "💬", FontSize = 12, VerticalOptions = LayoutOptions.Center });
        g.Add(nameStack, 0, 0);

        // Spalte 2: erstes Vorgabebild (klickbar -> Vollbild)
        g.Add(CreateRowImageCell(entry.HasBilder ? entry.Bilder![0].Url : null), 1, 0);
        // Spalte 3: erstes Cleaner-Foto (klickbar -> Vollbild)
        g.Add(CreateRowImageCell(entry.HasFotos ? entry.Fotos![0].Url : null), 2, 0);

        // Spalte 4: Abgehakt
        var check = new CheckBox { IsChecked = entry.Checked, Color = Color.FromArgb("#2196F3"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        check.CheckedChanged += async (s, ev) =>
        {
            if (_suppressPutzCheck) return;
            var resp = await _apiService.TogglePutzlisteItemAsync(_taskId, entry.Id);
            if (resp.Success) { entry.Checked = resp.Checked; }
            else
            {
                _suppressPutzCheck = true;
                check.IsChecked = !ev.Value;
                _suppressPutzCheck = false;
                await DisplayAlertAsync("Fehler", "Konnte nicht gespeichert werden", "OK");
            }
        };
        g.Add(check, 3, 0);

        // Spalte 5: Bearbeiten (kompakt, nur Symbol)
        var editBtn = new Button
        {
            Text = "✏️",
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#1a3a5c"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Padding = new Thickness(0),
            WidthRequest = 40,
            HeightRequest = 40,
            VerticalOptions = LayoutOptions.Center
        };
        editBtn.Clicked += (s, ev) => OpenChecklisteDetail(entry);
        g.Add(editBtn, 4, 0);

        return g;
    }

    // Zelle mit einem Bild (oder Platzhalter); Tippen -> Vollbild
    private View CreateRowImageCell(string? url)
    {
        var holder = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            BackgroundColor = Color.FromArgb("#f0f0f0"),
            WidthRequest = 44,
            HeightRequest = 44,
            VerticalOptions = LayoutOptions.Center
        };

        if (string.IsNullOrEmpty(url))
        {
            holder.Content = new Label { Text = "—", FontSize = 16, TextColor = Color.FromArgb("#bbb"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            return holder;
        }

        var image = new Image { Aspect = Aspect.AspectFill };
        _ = Task.Run(async () =>
        {
            var src = await _apiService.GetImageAsync(url);
            if (src != null) MainThread.BeginInvokeOnMainThread(() => image.Source = src);
        });
        holder.Content = image;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => ShowImageFullscreen(url);
        holder.GestureRecognizers.Add(tap);
        return holder;
    }

    private static Label _detailLabel(string text) => new Label
    {
        Text = text, FontSize = 11, FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#999999")
    };

    private static Label _noneLabel(string text) => new Label
    {
        Text = text, FontSize = 13, FontAttributes = FontAttributes.Italic,
        TextColor = Color.FromArgb("#999999")
    };

    // Detail-Popup für einen Checklisten-Eintrag (über der Seite)
    private void OpenChecklisteDetail(PutzlisteEintrag entry)
    {
        if (this.Content is not Grid root) return;

        var overlay = new Grid { BackgroundColor = Color.FromArgb("#801a3a5c"), ZIndex = 5500 };
        void Close() { root.Children.Remove(overlay); BuildCheckliste(); }

        var bg = new BoxView { Color = Colors.Transparent };
        var bgTap = new TapGestureRecognizer();
        bgTap.Tapped += (s, e) => Close();
        bg.GestureRecognizers.Add(bgTap);
        overlay.Children.Add(bg);

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Margin = new Thickness(16),
            VerticalOptions = LayoutOptions.Center,
            MaximumHeightRequest = 640
        };
        var outer = new Grid { RowDefinitions = new RowDefinitionCollection { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };

        // Kopf
        var header = new Grid { Padding = new Thickness(20, 16), BackgroundColor = Color.FromArgb("#1a3a5c") };
        header.Add(new Label { Text = entry.Name, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center }, 0, 0);
        var hclose = new Button { Text = "✕", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, BackgroundColor = Colors.Transparent, WidthRequest = 44, HeightRequest = 44, HorizontalOptions = LayoutOptions.End };
        hclose.Clicked += (s, e) => Close();
        header.Add(hclose, 0, 0);
        outer.Add(header, 0, 0);

        // Inhalt
        var body = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(20) };

        if (entry.HasBeschreibung)
        {
            body.Children.Add(_detailLabel("Beschreibung"));
            body.Children.Add(new Label { Text = entry.Beschreibung, FontSize = 14, TextColor = Color.FromArgb("#444444") });
        }

        body.Children.Add(_detailLabel("Anmerkung"));
        var komm = new Editor
        {
            Text = entry.Kommentar ?? "", HeightRequest = 70, FontSize = 14,
            BackgroundColor = Color.FromArgb("#f5f5f5"), TextColor = Colors.Black,
            Placeholder = "Anmerkung zu diesem Punkt..."
        };
        komm.Unfocused += async (s, e) =>
        {
            var t = komm.Text ?? "";
            if ((entry.Kommentar ?? "") == t) return;
            var r = await _apiService.SavePutzlisteEintragKommentarAsync(_taskId, entry.Id, t);
            if (r.Success) entry.Kommentar = t;
        };
        body.Children.Add(komm);

        body.Children.Add(_detailLabel("Ursprungsbilder (Checkliste)"));
        if (entry.HasBilder)
            body.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = CreatePutzThumbStrip(entry.Bilder!, deletable: false, entry: entry) });
        else
            body.Children.Add(_noneLabel("Keine Vorgabebilder"));

        body.Children.Add(_detailLabel("Fotos vom Cleaner"));
        var fotoStrip = CreatePutzThumbStrip(entry.Fotos ?? new List<PutzlisteBild>(), deletable: true, entry: entry);
        body.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = fotoStrip });

        var addFoto = new Button
        {
            Text = "\U0001F4F7 Foto aufnehmen",
            BackgroundColor = Color.FromArgb("#2196F3"), TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold, CornerRadius = 10, HeightRequest = 48
        };
        addFoto.Clicked += async (s, e) => await OnPutzlisteAddFoto(entry, fotoStrip);
        body.Children.Add(addFoto);

        outer.Add(new ScrollView { Content = body }, 0, 1);
        card.Content = outer;
        overlay.Children.Add(card);

        Grid.SetRowSpan(overlay, 3);
        root.Children.Add(overlay);
    }

    // Vollbild-Ansicht eines Bildes (mit Auth über GetImageAsync)
    private void ShowImageFullscreen(string url)
    {
        if (string.IsNullOrEmpty(url) || this.Content is not Grid root) return;

        var overlay = new Grid { BackgroundColor = Color.FromArgb("#E6000000"), ZIndex = 6000 };
        void Remove() { root.Children.Remove(overlay); }

        var img = new Image { Aspect = Aspect.AspectFit, Margin = new Thickness(16, 60, 16, 60) };
        _ = Task.Run(async () =>
        {
            var src = await _apiService.GetImageAsync(url);
            if (src != null) MainThread.BeginInvokeOnMainThread(() => img.Source = src);
        });

        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => Remove();
        overlay.GestureRecognizers.Add(tap);

        var close = new Button { Text = "✕", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 40, 16, 0) };
        close.Clicked += (s, e) => Remove();

        overlay.Children.Add(img);
        overlay.Children.Add(close);
        Grid.SetRowSpan(overlay, 3);
        root.Children.Add(overlay);
    }

    private HorizontalStackLayout CreatePutzThumbStrip(IEnumerable<PutzlisteBild> bilder, bool deletable, PutzlisteEintrag entry)
    {
        var strip = new HorizontalStackLayout { Spacing = 8 };
        foreach (var b in bilder)
            strip.Children.Add(CreatePutzlisteThumb(b, deletable, entry));
        return strip;
    }

    private View CreatePutzlisteThumb(PutzlisteBild bild, bool deletable, PutzlisteEintrag entry)
    {
        var grid = new Grid { WidthRequest = 84, HeightRequest = 84 };
        var imgBorder = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            BackgroundColor = Color.FromArgb("#E0E0E0"),
            WidthRequest = 84,
            HeightRequest = 84
        };
        var image = new Image { WidthRequest = 84, HeightRequest = 84, Aspect = Aspect.AspectFill };
        var url = bild.Url;
        _ = Task.Run(async () =>
        {
            var src = await _apiService.GetImageAsync(url);
            if (src != null) MainThread.BeginInvokeOnMainThread(() => image.Source = src);
        });
        imgBorder.Content = image;
        grid.Children.Add(imgBorder);

        // Klick auf die ganze Zelle -> Vollbild (robuster als nur aufs Image)
        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => ShowImageFullscreen(url);
        grid.GestureRecognizers.Add(tap);

        if (deletable)
        {
            var del = new Button
            {
                Text = "✕",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Color.FromArgb("#E91E63"),
                TextColor = Colors.White,
                WidthRequest = 22,
                HeightRequest = 22,
                CornerRadius = 11,
                Padding = 0,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start
            };
            del.Clicked += async (s, e) => await OnPutzlisteDeleteFoto(bild, grid, entry);
            grid.Children.Add(del);
        }
        return grid;
    }

    private async Task OnPutzlisteAddFoto(PutzlisteEintrag entry, HorizontalStackLayout fotoStrip)
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
                if (cameraStatus != PermissionStatus.Granted) { await OfferOpenSettingsAsync("Kamera"); return; }
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;

            using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var fileName = $"checkliste_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var resp = await _apiService.UploadPutzlisteFotoAsync(_taskId, entry.Id, fileName, bytes);
            if (!resp.Success)
            {
                await DisplayAlertAsync("Fehler", resp.Error ?? "Upload fehlgeschlagen", "OK");
                return;
            }
            var bild = new PutzlisteBild { Id = resp.Id, Url = resp.Url };
            entry.Fotos ??= new List<PutzlisteBild>();
            entry.Fotos.Add(bild);
            fotoStrip.Children.Add(CreatePutzlisteThumb(bild, deletable: true, entry: entry));
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

    private async Task OnPutzlisteDeleteFoto(PutzlisteBild bild, View thumb, PutzlisteEintrag entry)
    {
        bool confirm = await DisplayAlertAsync("Foto löschen", "Dieses Foto wirklich löschen?", "Löschen", "Abbrechen");
        if (!confirm) return;
        var resp = await _apiService.DeletePutzlisteFotoAsync(bild.Id);
        if (!resp.Success) return;
        if (entry.Fotos != null) entry.Fotos.RemoveAll(f => f.Id == bild.Id);
        if (thumb.Parent is Microsoft.Maui.Controls.Layout layout)
            layout.Children.Remove(thumb);
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
        _editingItem = item;
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
            try
            {
                var response = await _apiService.DeleteImageListItemAsync(itemId);

                if (response.Success)
                {
                    await LoadTaskAsync();
                }
                else if (IsNetworkError(response.Error))
                {
                    await DisplayAlertAsync(
                        Translations.Get("no_connection"),
                        Translations.Get("network_error_hint"),
                        Translations.Get("ok"));
                }
                else
                {
                    await DisplayAlertAsync("Fehler", response.Error ?? "Fehler beim Loeschen", "OK");
                }
            }
            catch (Exception ex)
            {
                if (IsNetworkError(ex.Message))
                {
                    await DisplayAlertAsync(
                        Translations.Get("no_connection"),
                        Translations.Get("network_error_hint"),
                        Translations.Get("ok"));
                }
                else
                {
                    await DisplayAlertAsync("Fehler", ex.Message, "OK");
                }
            }
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

        int existingCount = _editingItem?.Photos?.Count ?? 0;
        int totalCount = existingCount + _selectedPhotos.Count;

        if (totalCount == 0)
        {
            ImageListDescriptionDialogPhotoPreviewStack.IsVisible = false;
            ImageListDescriptionDialogPhotoCountLabel.IsVisible = false;
            return;
        }

        ImageListDescriptionDialogPhotoPreviewStack.IsVisible = true;
        ImageListDescriptionDialogPhotoCountLabel.IsVisible = true;
        ImageListDescriptionDialogPhotoCountLabel.Text = $"{totalCount} Foto(s)";

        // Show existing photos from server (with edit and delete buttons)
        if (_editingItem?.Photos != null)
        {
            foreach (var photo in _editingItem.Photos)
            {
                var photoCopy = photo;

                // Horizontal row: Image | Buttons
                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(new GridLength(140)),
                        new ColumnDefinition(GridLength.Star)
                    },
                    ColumnSpacing = 15
                };

                // Image container (140x140)
                var imageContainer = new Border
                {
                    WidthRequest = 140,
                    HeightRequest = 140,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Stroke = Color.FromArgb("#2196F3"),
                    StrokeThickness = 2
                };
                var image = new Image { Aspect = Aspect.AspectFill };

                // Load image async
                _ = Task.Run(async () =>
                {
                    var imageSource = await _apiService.GetImageAsync(photoCopy.ThumbnailUrl ?? photoCopy.Url);
                    if (imageSource != null)
                        MainThread.BeginInvokeOnMainThread(() => image.Source = imageSource);
                });

                imageContainer.Content = image;

                // Tap to view full size
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await ViewExistingPhotoAsync(photoCopy);
                imageContainer.GestureRecognizers.Add(tapGesture);

                row.Children.Add(imageContainer);
                Grid.SetColumn(imageContainer, 0);

                // Button stack (right side)
                var buttonStack = new VerticalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center
                };

                // Edit button
                var editBtn = new Button
                {
                    Text = "✏ Bearbeiten",
                    BackgroundColor = Color.FromArgb("#FF9800"),
                    TextColor = Colors.White,
                    FontSize = 14,
                    CornerRadius = 8,
                    HeightRequest = 40
                };
                editBtn.Clicked += async (s, e) => await EditExistingPhotoAsync(photoCopy);
                buttonStack.Children.Add(editBtn);

                // Delete button
                var deleteBtn = new Button
                {
                    Text = "🗑 " + Translations.Get("delete"),
                    BackgroundColor = Color.FromArgb("#c62828"),
                    TextColor = Colors.White,
                    FontSize = 14,
                    CornerRadius = 8,
                    HeightRequest = 40
                };
                deleteBtn.Clicked += async (s, e) => await DeleteExistingPhotoAsync(photoCopy);
                buttonStack.Children.Add(deleteBtn);

                row.Children.Add(buttonStack);
                Grid.SetColumn(buttonStack, 1);

                ImageListDescriptionDialogPhotoPreviewStack.Children.Add(row);
            }
        }

        // Show newly added photos (with delete button)
        for (int i = 0; i < _selectedPhotos.Count; i++)
        {
            var photo = _selectedPhotos[i];
            var index = i;

            // Horizontal row: Image | Delete button
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(140)),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 15
            };

            // Image container (140x140)
            var imageContainer = new Border
            {
                WidthRequest = 140,
                HeightRequest = 140,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Stroke = Colors.Transparent
            };
            imageContainer.Content = new Image
            {
                Source = ImageSource.FromStream(() => new MemoryStream(photo.Bytes)),
                Aspect = Aspect.AspectFill
            };

            row.Children.Add(imageContainer);
            Grid.SetColumn(imageContainer, 0);

            // Delete button (right side)
            var deleteBtn = new Button
            {
                Text = "🗑 Löschen",
                BackgroundColor = Color.FromArgb("#c62828"),
                TextColor = Colors.White,
                FontSize = 14,
                CornerRadius = 8,
                HeightRequest = 40,
                VerticalOptions = LayoutOptions.Center
            };
            deleteBtn.Clicked += (s, e) => { _selectedPhotos.RemoveAt(index); UpdateDialogPhotoPreview(); };

            row.Children.Add(deleteBtn);
            Grid.SetColumn(deleteBtn, 1);

            ImageListDescriptionDialogPhotoPreviewStack.Children.Add(row);
        }
    }

    private async Task ViewExistingPhotoAsync(ImageListDescriptionPhoto photo)
    {
        if (string.IsNullOrEmpty(photo.Url)) return;

        try
        {
            // Show full size image in a simple modal
            var imageUrl = photo.Url;
            if (!imageUrl.StartsWith("http"))
            {
                imageUrl = $"{ApiService.BaseUrl}{imageUrl}";
            }

            var imagePage = new ContentPage
            {
                BackgroundColor = Colors.Black,
                Content = new Grid
                {
                    Children =
                    {
                        new Image
                        {
                            Source = imageUrl,
                            Aspect = Aspect.AspectFit,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Button
                        {
                            Text = "✕",
                            FontSize = 24,
                            BackgroundColor = Colors.Transparent,
                            TextColor = Colors.White,
                            WidthRequest = 50,
                            HeightRequest = 50,
                            HorizontalOptions = LayoutOptions.End,
                            VerticalOptions = LayoutOptions.Start,
                            Margin = new Thickness(0, 40, 10, 0)
                        }
                    }
                }
            };

            var closeBtn = (Button)((Grid)imagePage.Content).Children[1];
            closeBtn.Clicked += async (s, e) => await Navigation.PopModalAsync();

            await Navigation.PushModalAsync(imagePage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"View photo error: {ex.Message}");
        }
    }

    private async Task DeleteExistingPhotoAsync(ImageListDescriptionPhoto photo)
    {
        var confirm = await DisplayAlertAsync("Foto löschen", "Möchten Sie dieses Foto wirklich löschen?", "Ja", "Nein");
        if (!confirm) return;

        try
        {
            var result = await _apiService.DeleteImageListPhotoAsync(photo.Id);
            if (result.Success)
            {
                _editingItem?.Photos?.Remove(photo);
                UpdateDialogPhotoPreview();
            }
            else
            {
                await DisplayAlertAsync("Fehler", result.Error ?? "Foto konnte nicht gelöscht werden", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete photo error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Foto konnte nicht gelöscht werden", "OK");
        }
    }

    private async Task EditExistingPhotoAsync(ImageListDescriptionPhoto photo)
    {
        if (string.IsNullOrEmpty(photo.Url)) return;

        try
        {
            // Download image from server - ensure full URL
            var imageUrl = photo.Url;
            if (!imageUrl.StartsWith("http"))
            {
                imageUrl = $"{ApiService.BaseUrl}{imageUrl}";
            }

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

            // Open annotation page
            var annotationPage = new ImageAnnotationPage(imageBytes);
            await Navigation.PushModalAsync(annotationPage);

            var tcs = new TaskCompletionSource<bool>();
            annotationPage.Disappearing += (s, ev) => tcs.TrySetResult(true);
            await tcs.Task;

            // If saved with annotations, delete old and add as new photo
            if (annotationPage.WasSaved && annotationPage.AnnotatedImageBytes != null)
            {
                // Delete old photo via API
                var deleteResult = await _apiService.DeleteImageListPhotoAsync(photo.Id);
                if (!deleteResult.Success)
                {
                    await DisplayAlertAsync("Fehler", deleteResult.Error ?? "Foto konnte nicht ersetzt werden", "OK");
                    return;
                }

                // Add annotated version as new photo
                var fileName = $"edited_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                _selectedPhotos.Add((fileName, annotationPage.AnnotatedImageBytes));

                // Remove from _editingItem.Photos to update UI
                _editingItem?.Photos?.Remove(photo);

                UpdateDialogPhotoPreview();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Edit photo error: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Bild konnte nicht bearbeitet werden", "OK");
        }
    }

    private async void OnSaveImageListDescriptionDialogClicked(object sender, EventArgs e)
    {
        var name = ImageListDescriptionDialogNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlertAsync(Translations.Get("error"), Translations.Get("name_required"), Translations.Get("ok"));
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

                if (success)
                {
                    await LoadTaskAsync();
                }
                else if (IsNetworkError(error))
                {
                    // For updates we can't easily queue, show offline message
                    await DisplayAlertAsync(
                        Translations.Get("no_connection"),
                        Translations.Get("network_error_hint"),
                        Translations.Get("ok"));
                }
                else
                {
                    await DisplayAlertAsync("Fehler", error ?? "Fehler beim Aktualisieren", "OK");
                }
            }
            else
            {
                // Create new - unified for both problem and anmerkung
                var response = await _apiService.CreateImageListItemAsync(_taskId, _currentItemType, name, description, _selectedPhotos);
                success = response.Success;
                error = response.Error;

                if (success)
                {
                    // Only show confirmation for problems, not for anmerkungen
                    if (_currentItemType == "problem")
                    {
                        await DisplayAlertAsync(Translations.Get("saved"), Translations.Get("problem_reported"), Translations.Get("ok"));
                    }
                    await LoadTaskAsync();
                }
                else if (IsNetworkError(error))
                {
                    // Queue for offline sync
                    var photoBytes = _selectedPhotos.Select(p => p.Bytes).ToList();
                    await OfflineQueueService.Instance.EnqueueImageListItemAsync(_taskId, _currentItemType, name, description, photoBytes);
                    await DisplayAlertAsync(
                        Translations.Get("no_connection"),
                        Translations.Get("saved_offline"),
                        Translations.Get("ok"));
                }
                else
                {
                    await DisplayAlertAsync("Fehler", error ?? "Fehler beim Speichern", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            if (IsNetworkError(ex.Message) && !_editingItemId.HasValue)
            {
                // Queue for offline sync
                var photoBytes = _selectedPhotos.Select(p => p.Bytes).ToList();
                await OfflineQueueService.Instance.EnqueueImageListItemAsync(_taskId, _currentItemType, name, description, photoBytes);
                await DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("saved_offline"),
                    Translations.Get("ok"));
            }
            else if (IsNetworkError(ex.Message))
            {
                await DisplayAlertAsync(
                    Translations.Get("no_connection"),
                    Translations.Get("network_error_hint"),
                    Translations.Get("ok"));
            }
            else
            {
                await DisplayAlertAsync("Fehler", "Konnte nicht gespeichert werden", "OK");
            }
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
            { "Anmerkung erstellt", Translations.Get("log_note_created") },
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

    #region Network Error Handling

    private static bool IsNetworkError(string? error)
    {
        if (string.IsNullOrEmpty(error)) return false;
        var lowerError = error.ToLowerInvariant();
        return lowerError.Contains("network") ||
               lowerError.Contains("timeout") ||
               lowerError.Contains("timedout") ||
               lowerError.Contains("connection") ||
               lowerError.Contains("internet") ||
               lowerError.Contains("unreachable") ||
               lowerError.Contains("net_http") ||
               lowerError.Contains("failure") ||
               lowerError.Contains("host") ||
               lowerError.Contains("refused");
    }

    #endregion
}
