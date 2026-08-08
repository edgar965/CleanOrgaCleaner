using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Tagesliste - die Startseite nach der Anmeldung.
///
/// Der Aufbau der Aufgaben-Knöpfe liegt in TodayPage.Kacheln.cs.
/// </summary>
public partial class TodayPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly DatumswechselWaechter _datumswaechter;
    private List<CleaningTask> _tasks = new();

    private static void Log(string msg)
    {
        var line = $"[TODAY] {msg}";
        System.Diagnostics.Debug.WriteLine(line);
        _ = Task.Run(() => ApiService.WriteLog(line));
    }

    public TodayPage()
    {
        Log("Constructor START");
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
        _datumswaechter = new DatumswechselWaechter(NeuerTagErkannt);
        Log("Constructor DONE");
    }

    protected override void OnAppearing()
    {
        Log("OnAppearing START");
        base.OnAppearing();

        _datumswaechter.Starten();

        // -= vor += : bei doppeltem OnAppearing ohne OnDisappearing (iOS-
        // Modal/Alert über der Seite) sonst mehrfach abonniert
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
        _webSocketService.OnTaskUpdate += OnTaskUpdate;

        _ = Header.InitializeAsync();
        Header.SetPageTitle("today");

        _ = App.InitializeWebSocketAsync();
        _ = LoadTasksAsync();

        Log("OnAppearing DONE");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
        _datumswaechter.Beenden();
    }

    /// <summary>Über Nacht offen geblieben: Tagesliste und Arbeitszeit neu holen.</summary>
    private void NeuerTagErkannt()
    {
        Log($"Datumswechsel erkannt: {DateTime.Today:d}");
        UiSicher.AufMainThread(async () =>
        {
            await LoadTasksAsync();
            await Header.LoadWorkStatusAsync();
        }, "TodayPage");
    }

    /// <summary>Meldungen, nach denen die Tagesliste nicht mehr stimmen kann.</summary>
    private static readonly HashSet<string> ListeNeuLaden = new()
    {
        "task_created", "task_updated", "task_deleted",
        "assignment_update", "aufgabe_update",
        "image_list_update", "problem_update", "problem_delete"
    };

    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[TodayPage] Task update received: {updateType}");
        if (ListeNeuLaden.Contains(updateType))
            UiSicher.AufMainThread(() => LoadTasksAsync(), "TodayPage");
    }

    private async Task LoadTasksAsync()
    {
        Log("LoadTasksAsync START");
        try
        {
            var data = await _apiService.GetTodayDataAsync().ConfigureAwait(false);
            _tasks = data?.Tasks ?? new List<CleaningTask>();
            Log($"GetTodayDataAsync DONE: {_tasks.Count} Aufgaben");

            // Für den Offline-Betrieb merken
            _ = OfflineDataService.Instance.SaveTasksAsync(_tasks);
            Preferences.Set("offline_mode", false);

            UiSicher.AufMainThread(() =>
            {
                NoTasksLabel.Text = Translations.Get("no_tasks");
                BuildTaskGrid();
            }, "TodayPage");
        }
        catch (Exception ex) when (ex is ServerAntwortFehler || ex is System.Text.Json.JsonException)
        {
            // Server hat GEANTWORTET (Fehlerstatus ODER 200 mit Nicht-JSON-Body,
            // z.B. Proxy-/Captive-Portal-/Deploy-Seite): NICHT auf den evtl.
            // tagealten Zwischenspeicher zurückfallen - sonst arbeitet die
            // Arbeitskraft nach einem falschen Tagesplan.
            Log($"LoadTasksAsync SERVER ERROR: {ex.Message}");
            UiSicher.AufMainThread(() =>
            {
                NoTasksLabel.Text = Translations.Get("connection_error");
                if (_tasks.Count == 0)
                    ZeigeLeerzustand();
            }, "TodayPage");
        }
        catch (Exception ex)
        {
            // Alles andere ist Transportebene (kein Netz, Timeout, DNS, Socket)
            // -> Zwischenspeicher. Klassifikation über den Exception-TYP, nicht
            // über fehleranfälliges String-Matching der Message.
            Log($"LoadTasksAsync NETWORK ERROR: {ex.Message}");
            await LoadCachedTasksAsync();
        }
        Log("LoadTasksAsync END");
    }

    private async Task LoadCachedTasksAsync()
    {
        Log("LoadCachedTasksAsync START");
        try
        {
            // Ohne Netz auch Aufgaben von gestern zulassen
            var gespeichert = await OfflineDataService.Instance.LoadCachedTasksAsync(allowStale: true);

            if (gespeichert != null && gespeichert.Count > 0)
            {
                Log($"{gespeichert.Count} Aufgaben aus dem Zwischenspeicher");
                _tasks = gespeichert;

                UiSicher.AufMainThread(() =>
                {
                    NoTasksLabel.Text = Translations.Get("no_tasks");
                    BuildTaskGrid();
                    Header.UpdateOfflineBanner(true);
                }, "TodayPage");
            }
            else
            {
                Log("Kein Zwischenspeicher vorhanden");
                UiSicher.AufMainThread(() =>
                {
                    NoTasksLabel.Text = Translations.Get("no_connection") + "\n" + Translations.Get("network_error_hint");
                    ZeigeLeerzustand();
                }, "TodayPage");
            }
        }
        catch (Exception ex)
        {
            Log($"LoadCachedTasksAsync ERROR: {ex.Message}");
            UiSicher.AufMainThread(() =>
            {
                NoTasksLabel.Text = Translations.Get("connection_error");
                ZeigeLeerzustand();
            }, "TodayPage");
        }
        Log("LoadCachedTasksAsync END");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await LoadTasksAsync();
            await Header.LoadWorkStatusAsync();
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            Log($"OnRefreshing error: {ex.Message}");
        }
        finally
        {
            try { TaskRefreshView.IsRefreshing = false; }
            catch (Exception ex) { Log($"OnRefreshing reset: {ex.Message}"); }
        }
    }
}
