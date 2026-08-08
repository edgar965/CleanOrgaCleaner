using System.Collections.Concurrent;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Aufgaben des Tages: laden, Zustand ändern, Checkliste, Notiz, Protokoll, Fotos.
/// </summary>
public sealed class AufgabenApi
{
    private readonly ApiHttpKern _http;

    /// <summary>
    /// Zwischenspeicher der zuletzt geladenen Aufgaben.
    /// ConcurrentDictionary, weil auf Thread-Pool-Threads geschrieben und
    /// parallel gelesen wird - ein normales Dictionary kann dabei intern
    /// korrumpieren.
    /// </summary>
    private readonly ConcurrentDictionary<int, CleaningTask> _speicher = new();

    public AufgabenApi(ApiHttpKern http) => _http = http;

    /// <summary>
    /// Tagesdaten laden. Fehler werden NICHT geschluckt, sondern weitergeworfen -
    /// nur so kann die Seite auf den Offline-Zwischenspeicher zurückfallen,
    /// statt eine leere Liste anzuzeigen (und den Speicher zu überschreiben).
    /// </summary>
    public Task<TodayDataResponse> HoleTagesdatenAsync()
    {
        // iOS: auf dem Thread-Pool ausführen, sonst blockiert der UI-Thread
        return Task.Run(async () =>
        {
            try
            {
                LoginProtokoll.SchreibeApi("GetTodayDataAsync ENTER");
                var antwort = await _http.HoleAsync($"/mobile/api/today-data/?_={DateTime.Now.Ticks}").ConfigureAwait(false);
                LoginProtokoll.SchreibeApi($"GetAsync DONE: {antwort.StatusCode}");

                if (!antwort.Erfolgreich)
                {
                    LoginProtokoll.SchreibeApi($"GetTodayDataAsync FAILED: {antwort.StatusCode}");
                    // Eigener Typ: der Server hat GEANTWORTET (kein Netzproblem).
                    // Die Seite unterscheidet darüber Server- von Netzfehlern,
                    // statt fehleranfällig auf Meldungstexte zu prüfen.
                    throw new ServerAntwortFehler(antwort.StatusCode);
                }

                LoginProtokoll.SchreibeApi($"ReadAsStringAsync DONE: {antwort.Text.Length} chars");
                var daten = antwort.Deserialisiere<TodayDataResponse>() ?? new TodayDataResponse();
                LoginProtokoll.SchreibeApi($"Deserialize DONE: {daten.Tasks?.Count ?? 0} tasks");

                _speicher.Clear();
                foreach (var aufgabe in daten.Tasks ?? new List<CleaningTask>())
                    _speicher[aufgabe.Id] = aufgabe;

                LoginProtokoll.SchreibeApi("GetTodayDataAsync SUCCESS");
                return daten;
            }
            catch (Exception ex)
            {
                LoginProtokoll.SchreibeApi($"GetTodayDataAsync ERROR: {ex.Message}");
                throw;
            }
        });
    }

    /// <summary>
    /// Eine Aufgabe holen. Standardmäßig werden die Tagesdaten frisch geladen,
    /// damit auch neue Fotos dabei sind.
    /// </summary>
    public async Task<CleaningTask?> HoleAufgabeAsync(int aufgabenId, bool frischLaden = true)
    {
        if (!frischLaden && _speicher.TryGetValue(aufgabenId, out var gespeichert))
            return gespeichert;

        try
        {
            _speicher.Clear();
            await HoleTagesdatenAsync().ConfigureAwait(false);
            if (_speicher.TryGetValue(aufgabenId, out var aufgabe))
                return aufgabe;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] HoleAufgabe Fehler: {ex.Message}");
        }
        return null;
    }

    /// <summary>Aufgabenzustand setzen (started, completed, ...).</summary>
    public Task<TaskStateResponse> SetzeZustandAsync(int aufgabenId, string zustand)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync($"/api/task/{aufgabenId}/state/", new { state_completed = zustand }),
            erfolg => new TaskStateResponse { Success = erfolg },
            meldung => new TaskStateResponse { Success = false, Error = meldung },
            "UpdateTaskState");

    /// <summary>Checklisten-Eintrag umschalten.</summary>
    public Task<ChecklistToggleResponse> SchalteChecklisteAsync(int aufgabenId, int eintragIndex)
        => _http.FrageAsync(
            () => _http.SendeOhneInhaltAsync($"/mobile/api/task/{aufgabenId}/checklist/{eintragIndex}/toggle/"),
            erfolg => new ChecklistToggleResponse { Success = erfolg },
            _ => new ChecklistToggleResponse { Success = false },
            "ToggleChecklist");

    /// <summary>Notiz der Arbeitskraft zur Aufgabe speichern.</summary>
    public Task<ApiResponse> SpeichereNotizAsync(int aufgabenId, string notiz)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync($"/mobile/api/task/{aufgabenId}/notiz/", new { anmerkung_mitarbeiter = notiz }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>Protokolleinträge einer Aufgabe.</summary>
    public async Task<List<LogEntry>> HoleProtokollAsync(int aufgabenId)
    {
        var antwort = await _http.FrageAsync(
            () => _http.HoleAsync($"/api/task/{aufgabenId}/logs/"),
            _ => new LogsResponse(),
            _ => new LogsResponse(),
            "GetTaskLogs").ConfigureAwait(false);
        return antwort.Logs ?? new List<LogEntry>();
    }

    /// <summary>
    /// Fotos einer Aufgabe (mit vollständigen Adressen). Liefert die
    /// Server-Modelle - der Dienst kennt bewusst keine Oberflächen-Typen mehr.
    /// </summary>
    public async Task<List<TaskImageDto>> HoleFotosAsync(int aufgabenId)
    {
        var antwort = await _http.FrageAsync(
            () => _http.HoleAsync($"/api/task/{aufgabenId}/images/"),
            _ => new TaskImagesResponse(),
            _ => new TaskImagesResponse(),
            "GetTaskImages").ConfigureAwait(false);

        if (antwort.Images == null)
            return new List<TaskImageDto>();

        foreach (var bild in antwort.Images)
        {
            bild.Url = UrlHelfer.Absolut(bild.Url);
            bild.ThumbnailUrl = UrlHelfer.AbsolutOderNull(bild.ThumbnailUrl);
        }
        return antwort.Images;
    }
}
