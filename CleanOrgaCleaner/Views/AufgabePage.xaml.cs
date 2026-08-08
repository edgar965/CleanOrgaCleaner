using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Ansicht einer zugewiesenen Aufgabe.
///
/// Die Seite ist in Teile zerlegt:
/// AufgabePage.Tabs.cs (Reiter), AufgabePage.Status.cs (Start/Beenden),
/// AufgabePage.Checkliste.cs, AufgabePage.Meldungen.cs (Probleme/Notizen),
/// AufgabePage.Dialog.cs und AufgabePage.DialogFotos.cs (Eingabedialog),
/// AufgabePage.Protokoll.cs, AufgabePage.Fotos.cs (Fotos des Büros).
/// </summary>
[QueryProperty(nameof(TaskId), "taskId")]
public partial class AufgabePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly BildAnzeige _bilder;
    private readonly FotoAufnahme _fotoAufnahme;

    private int _taskId;
    private CleaningTask? _task;

    /// <summary>Meldungen, nach denen die Aufgabe neu geladen werden muss.</summary>
    private static readonly HashSet<string> NeuLaden = new()
    {
        "task_created", "task_updated", "task_deleted",
        "assignment_update", "aufgabe_update",
        "image_list_update", "problem_update", "problem_delete"
    };

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
        _bilder = new BildAnzeige(this, "AufgabePage");
        _fotoAufnahme = new FotoAufnahme(this);
        ErzeugeTabLeiste();
    }

    public AufgabePage(int taskId) : this() { _taskId = taskId; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _ = Header.InitializeAsync();
            Header.SetPageTitle("today");

            // -= vor += : bei doppeltem OnAppearing ohne OnDisappearing sonst
            // mehrfach abonniert -> jedes Update triggert mehrere Reloads
            _webSocketService.OnTaskUpdate -= OnTaskUpdate;
            _webSocketService.OnTaskUpdate += OnTaskUpdate;

            ApplyTranslations();
            _ = LadeAufgabeAsync();
        }
        catch (Exception ex)
        {
            // Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnTaskUpdate -= OnTaskUpdate;
    }

    private void OnTaskUpdate(string updateType)
    {
        System.Diagnostics.Debug.WriteLine($"[AufgabePage] Task update received: {updateType}");
        if (NeuLaden.Contains(updateType))
            UiSicher.AufMainThread(() => LadeAufgabeAsync(), "AufgabePage");
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("task");

        // Reiter
        TabAufgabeButton.Text = t("task_tab");
        TabProblemeButton.Text = t("problems_tab");
        TabAnmerkungenButton.Text = t("notes");
        TabLogsButton.Text = t("log");

        // Knöpfe
        AddProblemButton.Text = $"+ {t("report_problem")}";
        AddAnmerkungButton.Text = $"+ {t("add_note").ToUpper()}";

        // Hinweise bei leeren Listen
        NoTaskDescriptionLabel.Text = t("no_task_description");
        AufgabeFotosLabel.Text = t("photos");
        NoProblemsLabel.Text = t("no_problems");
        NoAnmerkungenLabel.Text = t("no_notes");
        NoLogsLabel.Text = t("no_logs");

        // Eingabedialog
        ImageListDescriptionDialogNameLabel.Text = $"{t("name")} *";
        ImageListDescriptionDialogDescLabel.Text = t("description");
        ImageListDescriptionDialogPhotosLabel.Text = t("photos");
        ImageListDescriptionDialogTakePhotoButton.Text = t("camera");
        ImageListDescriptionDialogPickPhotoButton.Text = t("gallery");
        SaveImageListDescriptionDialogButton.Text = t("save");
        CancelImageListDescriptionDialogButton.Text = t("cancel");

        // Rückfrage beim Abschließen
        CompleteTaskTitle.Text = t("task_completed");
        CompleteTaskMessage.Text = t("task_completed_question");
        CancelCompleteTaskButton.Text = t("no");
        ConfirmCompleteTaskButton.Text = t("yes");
    }

    private async Task LadeAufgabeAsync()
    {
        try
        {
            // Zuerst den bereits geladenen Zwischenspeicher nutzen (kein voller
            // Reload pro Klick). Fällt automatisch auf Nachladen zurück, wenn
            // die Aufgabe dort nicht liegt.
            _task = await _apiService.GetAufgabeDetailAsync(_taskId, forceRefresh: false);
            if (_task == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AufgabePage] Aufgabe {_taskId} nicht gefunden");
                UiSicher.AufMainThread(() => ZurueckAsync(), "AufgabePage");
                return;
            }

            TaskNameLabel.Text = $"{_task.ApartmentName} {AufgabenartName.Uebersetzt(_task.DisplayName)}";

            var beschreibung = UebersetzteAufgabe();
            NoticeLabel.Text = beschreibung;
            NoticeLabel.IsVisible = !string.IsNullOrEmpty(beschreibung);
            NoTaskDescriptionLabel.IsVisible = string.IsNullOrEmpty(beschreibung);

            AktualisiereStartStopKnopf();
            BuildProblems();
            BuildAnmerkungen();
            _ = AufgabeFotosLadenAsync();
            _tabs.Zeige(_tabs.Aktiv);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Laden: {ex.Message}");
        }
    }

    /// <summary>
    /// Aufgabenbeschreibung in der Sprache der Arbeitskraft; ohne hinterlegte
    /// Übersetzung bleibt der deutsche Text stehen.
    /// </summary>
    private string UebersetzteAufgabe()
    {
        if (_task == null) return string.Empty;

        var sprache = Translations.CurrentLanguage;
        if (sprache == "de") return _task.Aufgabe ?? string.Empty;

        if (_task.AufgabeTranslated != null
            && _task.AufgabeTranslated.TryGetValue(sprache, out string? uebersetzt)
            && !string.IsNullOrEmpty(uebersetzt))
            return uebersetzt;

        return _task.Aufgabe ?? string.Empty;
    }

    /// <summary>Zur vorigen Seite zurück - darf nie werfen.</summary>
    private async Task ZurueckAsync()
    {
        try { await Shell.Current.GoToAsync(".."); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Zurück: {ex.Message}");
            try
            {
                if (Navigation.NavigationStack.Count > 1)
                    await Navigation.PopAsync();
            }
            catch (Exception zweiterFehler)
            {
                System.Diagnostics.Debug.WriteLine($"[AufgabePage] Zurück (Ausweichweg): {zweiterFehler.Message}");
            }
        }
    }
}
