using SQLite;
using System.Text.Json;
using CleanOrgaCleaner.Services.Offline;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Warteschlange für Vorgänge, die ohne Netz entstanden sind (SQLite).
///
/// Diese Klasse kümmert sich nur noch um Speichern, Reihenfolge und den
/// Abarbeitungslauf. WIE ein einzelner Vorgang nachgeholt wird, steht in den
/// Klassen unter Services/Offline/Aufgaben - vorher lag beides in einer Datei.
/// </summary>
public class OfflineQueueService : IDisposable
{
    /// <summary>Rang für sofort wichtige Vorgänge (Chat, Arbeitszeit).</summary>
    private const int RangHoch = 1;

    /// <summary>Rang für den Rest (Aufträge, Fotos).</summary>
    private const int RangNormal = 2;

    private static readonly Lazy<OfflineQueueService> _instanz = new(() => new OfflineQueueService());

    /// <summary>Die eine Instanz der App.</summary>
    public static OfflineQueueService Instance => _instanz.Value;

    private readonly string _dbPfad;
    private readonly SemaphoreSlim _laufSperre = new(1, 1);
    private SQLiteAsyncConnection? _datenbank;

    /// <summary>Anzahl der wartenden Einträge hat sich geändert.</summary>
    public event Action<int>? OnQueueCountChanged;

    /// <summary>Ein Eintrag wurde erfolgreich nachgeholt.</summary>
    public event Action<OfflineQueueItem>? OnItemSynced;

    private OfflineQueueService()
    {
        _dbPfad = Path.Combine(FileSystem.AppDataDirectory, "offline_queue.db");
    }

    /// <summary>Datenbank öffnen und Tabelle anlegen (mehrfach aufrufbar).</summary>
    public async Task InitializeAsync()
    {
        if (_datenbank != null) return;

        _datenbank = new SQLiteAsyncConnection(_dbPfad);
        await _datenbank.CreateTableAsync<OfflineQueueItem>().ConfigureAwait(false);
        System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Initialized at {_dbPfad}");
    }

    #region Einreihen

    /// <summary>Chat-Nachricht einreihen (Empfänger wird mitgespeichert).</summary>
    public Task EnqueueChatMessageAsync(string message, string receiverId = "admin")
        => ReiheEinAsync("chat", new { message, receiver = receiverId }, RangHoch);

    /// <summary>Bildlisten-Eintrag (Problem/Anmerkung) mit Fotos einreihen.</summary>
    public Task EnqueueImageListItemAsync(int taskId, string itemType, string name, string? description, List<byte[]>? photos)
        => ReiheEinAsync("image_list_item", new
        {
            taskId,
            itemType,
            name,
            description,
            photos = photos?.Select(Convert.ToBase64String).ToList(),
            timestamp = DateTime.UtcNow
        }, RangNormal);

    /// <summary>Anlegen eines Auftrags einreihen.</summary>
    public Task EnqueueTaskCreateAsync(string name, string? plannedDate, int? apartmentId, int? aufgabenartId,
        string? hinweis, string status, object? assignments)
        => ReiheEinAsync("task_create", new
        {
            name, plannedDate, apartmentId, aufgabenartId, hinweis, status, assignments,
            timestamp = DateTime.UtcNow
        }, RangNormal);

    /// <summary>Ändern eines Auftrags einreihen.</summary>
    public Task EnqueueTaskUpdateAsync(int taskId, string name, string? plannedDate, int? apartmentId,
        int? aufgabenartId, string? hinweis, string status, object? assignments)
        => ReiheEinAsync("task_update", new
        {
            taskId, name, plannedDate, apartmentId, aufgabenartId, hinweis, status, assignments,
            timestamp = DateTime.UtcNow
        }, RangNormal);

    /// <summary>Arbeitsbeginn einreihen.</summary>
    public Task EnqueueWorkStartAsync()
        => ReiheEinAsync("work_start", new { timestamp = DateTime.UtcNow }, RangHoch);

    /// <summary>Arbeitsende einreihen.</summary>
    public Task EnqueueWorkStopAsync()
        => ReiheEinAsync("work_stop", new { timestamp = DateTime.UtcNow }, RangHoch);

    /// <summary>Zustandswechsel einer Aufgabe einreihen.</summary>
    public Task EnqueueTaskStateChangeAsync(int taskId, string newState)
        => ReiheEinAsync("task_state", new { taskId, newState, timestamp = DateTime.UtcNow }, RangHoch);

    /// <summary>Gemeinsames Einreihen: Nutzdaten serialisieren, ablegen, melden.</summary>
    private async Task ReiheEinAsync(string vorgangsart, object nutzdaten, int rang)
    {
        await InitializeAsync().ConfigureAwait(false);

        var eintrag = new OfflineQueueItem
        {
            OperationType = vorgangsart,
            Payload = JsonSerializer.Serialize(nutzdaten),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Priority = rang
        };

        await _datenbank!.InsertAsync(eintrag).ConfigureAwait(false);
        System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Enqueued {vorgangsart} (ID: {eintrag.Id})");

        await MeldeAnzahlAsync().ConfigureAwait(false);
    }

    #endregion

    #region Abfragen und Abarbeiten

    /// <summary>Anzahl wartender Einträge.</summary>
    public async Task<int> GetQueueCountAsync()
    {
        await InitializeAsync().ConfigureAwait(false);
        return await _datenbank!.Table<OfflineQueueItem>().CountAsync().ConfigureAwait(false);
    }

    /// <summary>Wartende Einträge nach Rang und Alter.</summary>
    public async Task<List<OfflineQueueItem>> GetPendingItemsAsync()
    {
        await InitializeAsync().ConfigureAwait(false);
        return await _datenbank!.Table<OfflineQueueItem>()
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Warteschlange abarbeiten (nach Rückkehr ins Netz). Läuft nie doppelt.
    /// </summary>
    public async Task ProcessQueueAsync()
    {
        if (!await _laufSperre.WaitAsync(0).ConfigureAwait(false))
        {
            System.Diagnostics.Debug.WriteLine("[OfflineQueue] Already processing");
            return;
        }

        try
        {
            var eintraege = await GetPendingItemsAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Processing {eintraege.Count} items");

            foreach (var eintrag in eintraege)
            {
                await VerarbeiteAsync(eintrag).ConfigureAwait(false);

                // Kleine Pause zwischen den Einträgen
                await Task.Delay(100).ConfigureAwait(false);
            }

            await MeldeAnzahlAsync().ConfigureAwait(false);
        }
        finally
        {
            _laufSperre.Release();
        }
    }

    /// <summary>Einen Eintrag nachholen und je nach Ausgang löschen oder behalten.</summary>
    private async Task VerarbeiteAsync(OfflineQueueItem eintrag)
    {
        try
        {
            var aufgabe = WarteschlangenFabrik.Erzeuge(eintrag);
            if (aufgabe == null)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Unknown operation type: {eintrag.OperationType}");
                await _datenbank!.DeleteAsync(eintrag).ConfigureAwait(false);
                return;
            }

            if (await aufgabe.AusfuehrenAsync(ApiService.Instance).ConfigureAwait(false))
            {
                await _datenbank!.DeleteAsync(eintrag).ConfigureAwait(false);
                UiSicher.SichererInvoke(() => OnItemSynced?.Invoke(eintrag), "Queue");
                System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Synced {eintrag.OperationType} (ID: {eintrag.Id})");
                return;
            }

            // Fehlversuch: Eintrag BLEIBT in der Warteschlange und wird beim
            // nächsten Verbinden erneut versucht. Bewusst KEIN automatisches
            // Verwerfen: ein Misserfolg kann Transportfehler, ein vorübergehendes
            // 5xx (Deploy/Überlast) ODER eine echte Ablehnung sein - auf dieser
            // Ebene nicht unterscheidbar. Für eine App, deren Offline-Vorgänge
            // Arbeitszeiten und Problemmeldungen sind, wiegt "nie Daten verlieren"
            // schwerer als "Warteschlange immer kurz". RetryCount dient der Diagnose.
            await MerkeFehlversuchAsync(eintrag, $"Fehlversuch {eintrag.RetryCount + 1} ({eintrag.OperationType})").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await MerkeFehlversuchAsync(eintrag, ex.Message).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineQueue] Failed {eintrag.OperationType}: {ex.Message}");
        }
    }

    private async Task MerkeFehlversuchAsync(OfflineQueueItem eintrag, string meldung)
    {
        eintrag.RetryCount++;
        eintrag.LastError = meldung;
        await _datenbank!.UpdateAsync(eintrag).ConfigureAwait(false);
    }

    /// <summary>Aktuelle Anzahl an die Oberfläche melden.</summary>
    private async Task MeldeAnzahlAsync()
    {
        var anzahl = await GetQueueCountAsync().ConfigureAwait(false);
        UiSicher.SichererInvoke(() => OnQueueCountChanged?.Invoke(anzahl), "Queue");
    }

    #endregion

    /// <summary>
    /// Datenbank schliessen - der Dienst bleibt danach benutzbar.
    ///
    /// Die Lauf-Sperre wird bewusst NICHT freigegeben: Auch dieser Dienst ist
    /// ein Singleton, und Dispose() läuft im Betrieb (Abmelden). Eine
    /// weggeworfene Sperre hätte jedes spätere Abarbeiten der Warteschlange
    /// mit ObjectDisposedException beendet - offline abgesetzte Nachrichten
    /// und Arbeitszeiten wären liegen geblieben. Gleiche Falle wie im
    /// WebSocketService, siehe dort.
    /// </summary>
    public void Dispose()
    {
        // Schließen läuft asynchron weiter; ein blockierendes Warten hier
        // könnte den UI-Thread festsetzen.
        _ = _datenbank?.CloseAsync();
    }
}
