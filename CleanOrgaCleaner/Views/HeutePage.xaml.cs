using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

public partial class HeutePage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isWorking = false;

    public HeutePage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            var data = await _apiService.GetTodayDataAsync();
            TasksCollection.ItemsSource = data.Tasks;

            // Update work status from server
            _isWorking = data.WorkStatus.IsWorking;
            if (_isWorking)
            {
                StartWorkButton.IsVisible = false;
                EndWorkButton.IsVisible = true;
                WorkStatusLabel.Text = $"Arbeit gestartet um {data.WorkStatus.StartTime}";
            }
            else
            {
                StartWorkButton.IsVisible = true;
                EndWorkButton.IsVisible = false;
                WorkStatusLabel.Text = "";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", $"Aufgaben konnten nicht geladen werden: {ex.Message}", "OK");
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadTasksAsync();
        TaskRefreshView.IsRefreshing = false;
    }

    private async void OnStartWorkClicked(object sender, EventArgs e)
    {
        StartWorkButton.IsEnabled = false;

        var success = await _apiService.StartWorkAsync();
        if (success)
        {
            _isWorking = true;
            StartWorkButton.IsVisible = false;
            EndWorkButton.IsVisible = true;
            WorkStatusLabel.Text = $"Arbeit gestartet um {DateTime.Now:HH:mm}";
        }
        else
        {
            await DisplayAlert("Fehler", "Arbeit konnte nicht gestartet werden", "OK");
        }

        StartWorkButton.IsEnabled = true;
    }

    private async void OnEndWorkClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Arbeit beenden",
            "Moechtest du die Arbeit wirklich beenden?",
            "Ja", "Nein");

        if (!confirm) return;

        EndWorkButton.IsEnabled = false;

        var success = await _apiService.EndWorkAsync();
        if (success)
        {
            _isWorking = false;
            StartWorkButton.IsVisible = true;
            EndWorkButton.IsVisible = false;
            WorkStatusLabel.Text = "Arbeit beendet";
        }
        else
        {
            await DisplayAlert("Fehler", "Arbeit konnte nicht beendet werden", "OK");
        }

        EndWorkButton.IsEnabled = true;
    }

    private async void OnTaskTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CleaningTask task)
        {
            var action = await DisplayActionSheet(
                task.ApartmentName,
                "Abbrechen",
                null,
                "Als 'In Arbeit' markieren",
                "Als 'Erledigt' markieren",
                "Details anzeigen");

            switch (action)
            {
                case "Als 'In Arbeit' markieren":
                    await UpdateTaskStatus(task.Id, "in_progress");
                    break;
                case "Als 'Erledigt' markieren":
                    await UpdateTaskStatus(task.Id, "completed");
                    break;
                case "Details anzeigen":
                    // TODO: Navigate to detail page
                    break;
            }
        }
    }

    private async Task UpdateTaskStatus(int taskId, string status)
    {
        var success = await _apiService.UpdateTaskStatusAsync(taskId, status);
        if (success)
        {
            await LoadTasksAsync();
        }
        else
        {
            await DisplayAlert("Fehler", "Status konnte nicht aktualisiert werden", "OK");
        }
    }

    private async void OnChatClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Chat", "Chat-Funktion kommt bald!", "OK");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            "Einstellungen",
            "Abbrechen",
            null,
            "Abmelden");

        if (action == "Abmelden")
        {
            Preferences.Remove("property_id");
            Preferences.Remove("username");
            Application.Current!.MainPage = new NavigationPage(new LoginPage());
        }
    }
}
