using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class TaskDetailPage : ContentPage
{
    private readonly ApiService _apiService;
    private int _taskId;
    private CleaningTask? _task;
    private List<string> _selectedPhotoPaths = new();

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
    }

    public TaskDetailPage(int taskId) : this() { _taskId = taskId; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTaskAsync();
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
            BuildChecklist();
            NotesEditor.Text = _task.AnmerkungMitarbeiter ?? "";
            BuildProblems();
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
        switch (_task.StateCompleted)
        {
            case "not_started":
                StartStopButton.Text = "Start";
                StartStopButton.BackgroundColor = Color.FromArgb("#9e9e9e");
                StartStopButton.IsEnabled = true;
                break;
            case "started":
                StartStopButton.Text = "Beenden";
                StartStopButton.BackgroundColor = Color.FromArgb("#2196F3");
                StartStopButton.IsEnabled = true;
                break;
            case "completed":
                StartStopButton.Text = "Erledigt";
                StartStopButton.BackgroundColor = Color.FromArgb("#4CAF50");
                StartStopButton.IsEnabled = false;
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
                    var confirm = await DisplayAlert(
                        Translations.Get("task_completed"),
                        Translations.Get("task_completed_question"),
                        Translations.Get("yes"),
                        Translations.Get("no"));
                    if (!confirm) { StartStopButton.IsEnabled = true; return; }
                    newState = "completed";
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

    private void BuildChecklist()
    {
        if (_task?.Checkliste == null || _task.Checkliste.Count == 0)
        { ChecklistFrame.IsVisible = false; return; }

        ChecklistFrame.IsVisible = true;
        ChecklistStack.Children.Clear();
        int checkedCount = 0, totalCount = _task.Checkliste.Count;

        for (int i = 0; i < _task.Checkliste.Count; i++)
        {
            var item = _task.Checkliste[i];
            var isChecked = _task.ChecklistStatus?.GetValueOrDefault(i.ToString(), false) ?? false;
            if (isChecked) checkedCount++;
            var itemIndex = i;
            var checkBox = new CheckBox { IsChecked = isChecked, Color = Color.FromArgb("#4CAF50") };
            checkBox.CheckedChanged += async (s, e) => await OnChecklistItemToggled(itemIndex);
            var label = new Label { Text = item, FontSize = 15, TextColor = Color.FromArgb("#333333"), VerticalOptions = LayoutOptions.Center };
            if (isChecked) { label.TextDecorations = TextDecorations.Strikethrough; label.TextColor = Color.FromArgb("#999999"); }
            var row = new HorizontalStackLayout { Spacing = 10 };
            row.Children.Add(checkBox); row.Children.Add(label);
            ChecklistStack.Children.Add(row);
        }
        ChecklistProgressLabel.Text = $"{checkedCount} / {totalCount} erledigt";
    }

    private async Task OnChecklistItemToggled(int itemIndex)
    {
        try
        {
            var response = await _apiService.ToggleChecklistItemAsync(_taskId, itemIndex);
            if (response.Success)
            {
                if (_task?.ChecklistStatus != null)
                    _task.ChecklistStatus[itemIndex.ToString()] = response.Checked;
                BuildChecklist();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Toggle checklist error: {ex.Message}"); }
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
        _selectedPhotoPaths.Clear(); UpdatePhotoPreview(); CharCountLabel.Text = "0 / 300";
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
            if (!MediaPicker.Default.IsCaptureSupported) { await DisplayAlert("Fehler", "Kamera nicht verfuegbar", "OK"); return; }
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                var localPath = System.IO.Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using var stream = await photo.OpenReadAsync();
                using var newStream = File.OpenWrite(localPath);
                await stream.CopyToAsync(newStream);
                _selectedPhotoPaths.Add(localPath); UpdatePhotoPreview();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Camera error: {ex.Message}"); await DisplayAlert("Fehler", "Kamera konnte nicht geoeffnet werden", "OK"); }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotoAsync();
            if (photos != null)
            {
                var localPath = System.IO.Path.Combine(FileSystem.CacheDirectory, photos.FileName);
                using var stream = await photos.OpenReadAsync();
                using var newStream = File.OpenWrite(localPath);
                await stream.CopyToAsync(newStream);
                _selectedPhotoPaths.Add(localPath); UpdatePhotoPreview();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}"); await DisplayAlert("Fehler", "Galerie konnte nicht geoeffnet werden", "OK"); }
    }

    private void UpdatePhotoPreview()
    {
        PhotoPreviewStack.Children.Clear();
        if (_selectedPhotoPaths.Count == 0) { PhotoPreviewStack.IsVisible = false; PhotoCountLabel.Text = "Keine Fotos ausgewaehlt"; return; }
        PhotoPreviewStack.IsVisible = true; PhotoCountLabel.Text = $"{_selectedPhotoPaths.Count} Foto(s) ausgewaehlt";
        foreach (var path in _selectedPhotoPaths)
        {
            var grid = new Grid { WidthRequest = 70, HeightRequest = 70 };
            var imageContainer = new Border { StrokeShape = new RoundRectangle { CornerRadius = 8 }, Stroke = Colors.Transparent };
            imageContainer.Content = new Image { Source = ImageSource.FromFile(path), Aspect = Aspect.AspectFill };
            grid.Children.Add(imageContainer);
            var deleteBtn = new Button { Text = "X", BackgroundColor = Color.FromArgb("#c62828"), TextColor = Colors.White, FontSize = 10, WidthRequest = 22, HeightRequest = 22, CornerRadius = 11, Padding = 0, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 2, 2, 0) };
            var pathToRemove = path;
            deleteBtn.Clicked += (s, e) => { _selectedPhotoPaths.Remove(pathToRemove); UpdatePhotoPreview(); };
            grid.Children.Add(deleteBtn);
            PhotoPreviewStack.Children.Add(grid);
        }
    }

    private async void OnSaveProblemClicked(object sender, EventArgs e)
    {
        var name = ProblemNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { await DisplayAlert("Fehler", "Bitte gib einen Namen fuer das Problem ein", "OK"); return; }
        var beschreibung = ProblemDescriptionEditor.Text?.Trim();
        ProblemPopupOverlay.IsVisible = false;
        try
        {
            var response = await _apiService.ReportProblemAsync(_taskId, name, beschreibung, _selectedPhotoPaths);
            if (response.Success) { await DisplayAlert("Gemeldet", "Problem wurde gemeldet", "OK"); await LoadTaskAsync(); }
            else await DisplayAlert("Fehler", response.Error ?? "Fehler beim Melden", "OK");
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Report problem error: {ex.Message}"); await DisplayAlert("Fehler", "Problem konnte nicht gemeldet werden", "OK"); }
    }

    private void OnCancelProblemClicked(object sender, EventArgs e) { ProblemPopupOverlay.IsVisible = false; }
}
