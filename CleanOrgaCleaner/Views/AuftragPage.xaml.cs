using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Eigene Aufträge anlegen und bearbeiten.
///
/// Die Seite ist in Teile zerlegt: AuftragPage.Bearbeiten.cs (Dialog, Zuweisen,
/// Speichern, Löschen), AuftragPage.Anmerkungen.cs und
/// AuftragPage.AnmerkungDialog.cs (Notizen), AuftragPage.Protokoll.cs,
/// AuftragPage.Fotos.cs (Fotos zur Aufgabe).
/// </summary>
public partial class AuftragPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly FotoAufnahme _fotoAufnahme;
    private readonly BildAnzeige _bilder;

    private List<Auftrag> _tasks = new();
    private List<ApartmentInfo> _apartments = new();
    private List<AufgabenartInfo> _aufgabenarten = new();
    private List<CleanerAssignmentInfo> _cleaners = new();

    private Auftrag? _currentTask;
    private bool _isNewTask = true;
    private string _currentStatus = "imported";
    private TaskAssignments _assignments = LeereZuweisung();

    public AuftragPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
        _fotoAufnahme = new FotoAufnahme(this);
        _bilder = new BildAnzeige(this, "AuftragPage");
        ErzeugeTabLeiste();
    }

    private static TaskAssignments LeereZuweisung()
        => new() { Cleaning = new List<int>(), Check = null, Repare = new List<int>() };

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await Header.InitializeAsync();
            Header.SetPageTitle("new_task");
            ApplyTranslations();

            // -= vor += : bei doppeltem OnAppearing ohne OnDisappearing sonst
            // doppelte Registrierung
            _webSocketService.OnTaskUpdate -= OnTaskUpdate;
            _webSocketService.OnTaskUpdate += OnTaskUpdate;

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            // Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
    }

    /// <summary>Meldungen, nach denen die Liste nicht mehr stimmen kann.</summary>
    private static readonly HashSet<string> ListeNeuLaden = new()
    {
        "task_created", "task_updated", "task_deleted", "assignment_update", "aufgabe_update"
    };

    /// <summary>
    /// Änderungen von anderer Seite (z. B. das Büro hängt ein Foto an).
    /// Über die Leitung kommt nur die Meldung - die Bilder holt die Seite nach.
    /// </summary>
    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[AuftragPage] Task update received: {updateType}");

        if (updateType == "image_list_update")
        {
            // Nur die Fotoreihe der offenen Aufgabe nachladen. Ein kompletter
            // Neuaufbau würde den geöffneten Dialog samt noch nicht
            // gespeicherter Eingaben verwerfen.
            if (!_isNewTask && _currentTask != null && TaskPopupOverlay.IsVisible)
                UiSicher.AufMainThread(() => AufgabeFotosLadenAsync(_currentTask.Id), "AuftragPage");
            return;
        }

        // Liste nur auffrischen, solange kein Dialog offen ist
        if (ListeNeuLaden.Contains(updateType) && !TaskPopupOverlay.IsVisible)
            UiSicher.AufMainThread(() => LoadDataAsync(), "AuftragPage");
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;

        NewTaskButton.Text = "+ " + t("create_auftrag");
        EmptyLabel.Text = t("no_my_tasks");

        // Reiter
        TabDetails.Text = t("details_tab");
        TabAnmerkungen.Text = t("notes_tab");
        TabAssign.Text = t("assign_tab");
        TabLogs.Text = t("log");
        LabelLogs.Text = t("log");
        NoLogsLabel.Text = t("no_logs");

        // Eingabefelder
        LabelTaskName.Text = t("task_name_required");
        LabelApartment.Text = t("apartment");
        LabelDate.Text = t("date_required");
        LabelTaskType.Text = t("task_type");
        LabelHint.Text = t("task_tab");
        TaskHinweisEditor.Placeholder = t("optional_hint");
        LabelAufgabeFotos.Text = t("photos");
        AddAufgabeFotoButton.Text = "📷 " + t("add_photo");
        LabelAssignCleaners.Text = t("assign_cleaners");
        AddAnmerkungButton.Text = t("add_note");
        NoAnmerkungenLabel.Text = t("no_notes");

        // Fußzeile
        BtnCancel.Text = t("cancel");
        BtnSave.Text = t("save");
        BtnDelete.Text = t("delete_task");

        // Notiz-Dialog
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
        var t = Translations.Get;
        try
        {
            var daten = await _apiService.GetAuftragsDataAsync();
            if (!daten.Success)
            {
                await DisplayAlertAsync(t("error"), daten.Error ?? t("connection_error"), t("ok"));
                return;
            }

            _tasks = daten.Tasks ?? new List<Auftrag>();
            _apartments = daten.Apartments ?? new List<ApartmentInfo>();
            _aufgabenarten = daten.Aufgabenarten ?? new List<AufgabenartInfo>();
            _cleaners = (daten.Cleaners ?? new List<CleanerInfo>())
                .Select(c => new CleanerAssignmentInfo(c))
                .ToList();

            ZeigeAuftragsliste();
            FuelleAuswahlfelder();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Laden: {ex.Message}");
            await DisplayAlertAsync(t("error"), t("connection_error"), t("ok"));
        }
    }

    private void ZeigeAuftragsliste()
    {
        bool leer = _tasks.Count == 0;
        EmptyStateView.IsVisible = leer;
        TaskRefreshView.IsVisible = !leer;

        if (!leer)
            TasksCollectionView.ItemsSource = _tasks;
    }

    private void FuelleAuswahlfelder()
    {
        ApartmentPicker.ItemsSource = _apartments;
        ApartmentPicker.ItemDisplayBinding = new Binding("Name");

        AufgabenartPicker.ItemsSource = _aufgabenarten;
        AufgabenartPicker.ItemDisplayBinding = new Binding("Name");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try { await LoadDataAsync(); }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Auffrischen: {ex.Message}");
        }
        finally
        {
            TaskRefreshView.IsRefreshing = false;
        }
    }

    private void OnTaskSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Auftrag auftrag) return;

        OeffneBearbeiten(auftrag);
        TasksCollectionView.SelectedItem = null;
    }

    private void OnNewTaskClicked(object sender, EventArgs e) => OeffneNeuenAuftrag();
}
