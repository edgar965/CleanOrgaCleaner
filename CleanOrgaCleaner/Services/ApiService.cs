using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services.Api;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Einstieg in die Server-Kommunikation (Singleton, damit die Session-Cookies
/// über alle Seiten erhalten bleiben).
///
/// Diese Klasse enthält KEINE HTTP-Logik mehr: sie stellt nur die gewohnte
/// Oberfläche bereit und reicht an die zuständigen Api-Klassen weiter
/// (AuthApi, AufgabenApi, ChatApi, ...). Alle teilen sich denselben
/// <see cref="ApiHttpKern"/> und damit dieselbe Anmeldung.
/// </summary>
public class ApiService
{
    /// <summary>Basisadresse des Servers (auch von Seiten für Medien-Adressen genutzt).</summary>
    public const string BaseUrl = ApiHttpKern.BasisUrl;

    private static readonly Lazy<ApiService> _instanz = new(() => new ApiService());

    /// <summary>Die eine Instanz der App.</summary>
    public static ApiService Instance => _instanz.Value;

    private readonly ApiHttpKern _http;
    private readonly Sitzung _sitzung;
    private readonly WsCookieSpeicher _cookies;
    private readonly HeartbeatDienst _heartbeat;
    private readonly AuthApi _auth;
    private readonly AufgabenApi _aufgaben;
    private readonly ArbeitszeitApi _arbeitszeit;
    private readonly PutzlisteApi _putzliste;
    private readonly AuftragApi _auftraege;
    private readonly ImageListApi _bildlisten;
    private readonly ChatApi _chat;
    private readonly EinstellungenApi _einstellungen;
    private readonly PushApi _push;
    private readonly BildApi _bilder;
    private readonly CrashReportApi _berichte;

    private ApiService()
    {
        _http = new ApiHttpKern();
        _sitzung = new Sitzung();
        _cookies = new WsCookieSpeicher(_http);
        _heartbeat = new HeartbeatDienst(_http);
        _auth = new AuthApi(_http, _sitzung, _cookies, _heartbeat);
        _aufgaben = new AufgabenApi(_http);
        _arbeitszeit = new ArbeitszeitApi(_http);
        _putzliste = new PutzlisteApi(_http);
        _auftraege = new AuftragApi(_http);
        _bildlisten = new ImageListApi(_http);
        _chat = new ChatApi(_http);
        _einstellungen = new EinstellungenApi(_http, _sitzung);
        _push = new PushApi(_http);
        _bilder = new BildApi(_http);
        _berichte = new CrashReportApi(_http, _sitzung);
    }

    #region Sitzung und Protokoll

    /// <summary>Name der angemeldeten Arbeitskraft.</summary>
    public string? CleanerName => _sitzung.Name;
    /// <summary>Sprachkürzel der angemeldeten Arbeitskraft.</summary>
    public string? CleanerLanguage => _sitzung.Sprache;
    /// <summary>Id der angemeldeten Arbeitskraft.</summary>
    public int? CleanerId => _sitzung.Id;
    /// <summary>True, solange die Echtzeit-Verbindung steht.</summary>
    public bool IsOnline => WebSocketService.Instance.IsOnline;

    /// <summary>Offline-Modus: Angaben aus dem lokalen Bestand setzen.</summary>
    public void SetOfflineCleanerInfo(string cleanerName, int? cleanerId)
        => _sitzung.UebernehmeOffline(cleanerName, cleanerId);

    /// <summary>Neues Login-Protokoll beginnen.</summary>
    public static void InitFileLogging() => LoginProtokoll.Beginne();
    /// <summary>Login-Protokoll des vorherigen Laufs.</summary>
    public static string? GetPreviousLogs() => LoginProtokoll.LiesVorherige();
    /// <summary>Zeile ins Login-Protokoll schreiben.</summary>
    public static void WriteLog(string msg) => LoginProtokoll.Schreibe(msg);

    /// <summary>Diagnosezeile an den Server schicken (ohne Rückmeldung).</summary>
    public static void WriteServerDiag(string tag, string message)
        => _ = Task.Run(() => WriteServerDiagAsync(tag, message));

    /// <summary>Diagnosezeile mit Erfolgsrückmeldung (für "schon gemeldet"-Kennzeichen).</summary>
    public static async Task<bool> WriteServerDiagAsync(string tag, string message)
    {
        try
        {
            return await Instance._berichte.SendeDiagnoseAsync(tag, message).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Anmeldung

    /// <summary>Anmelden (asynchron, ohne Nebenwirkungen).</summary>
    public Task<LoginResult> LoginAsync(int propertyId, string username, string password)
        => _auth.AnmeldenAsync(propertyId, username, password);
    /// <summary>Anmelden (synchron; startet zusätzlich den Heartbeat).</summary>
    public LoginResult LoginSync(int propertyId, string username, string password)
        => _auth.AnmeldenSync(propertyId, username, password);
    /// <summary>Abmelden ohne Server-Aufruf.</summary>
    public void Logout() => _auth.Abmelden();
    /// <summary>Abmelden mit Server-Aufruf.</summary>
    public Task LogoutAsync() => _auth.AbmeldenAsync();
    /// <summary>Cookie-Kopfzeile für den WebSocket-Handshake.</summary>
    public string GetCookieHeader() => _cookies.KopfZeile();
    /// <summary>Heartbeat starten (nach erfolgreichem Login).</summary>
    public void StartHeartbeat() => _heartbeat.Starte();
    /// <summary>Heartbeat stoppen (beim Abmelden).</summary>
    public void StopHeartbeat() => _heartbeat.Stoppe();

    #endregion

    #region Aufgaben und Arbeitszeit

    /// <summary>Tagesdaten laden (wirft bei Server-/Netzfehler).</summary>
    public Task<TodayDataResponse> GetTodayDataAsync() => _aufgaben.HoleTagesdatenAsync();
    /// <summary>Eine Aufgabe laden.</summary>
    public Task<CleaningTask?> GetAufgabeDetailAsync(int taskId, bool forceRefresh = true)
        => _aufgaben.HoleAufgabeAsync(taskId, forceRefresh);
    /// <summary>Arbeitsbeginn melden.</summary>
    public Task<WorkTimeResponse> StartWorkAsync() => _arbeitszeit.BeginneAsync();
    /// <summary>Arbeitsende melden.</summary>
    public Task<WorkTimeResponse> EndWorkAsync() => _arbeitszeit.BeendeAsync();
    /// <summary>Arbeitszeit-Stand holen.</summary>
    public Task<WorkTimeResponse?> GetWorkStatusAsync() => _arbeitszeit.HoleStandAsync();

    /// <summary>Arbeitsende melden, nur Erfolg/Misserfolg.</summary>
    public async Task<bool> StopWorkAsync()
        => (await _arbeitszeit.BeendeAsync().ConfigureAwait(false)).Success;

    /// <summary>Aufgabenzustand setzen.</summary>
    public Task<TaskStateResponse> UpdateTaskStateAsync(int taskId, string state)
        => _aufgaben.SetzeZustandAsync(taskId, state);
    /// <summary>Aufgabe starten.</summary>
    public Task<TaskStateResponse> StartTaskAsync(int taskId) => _aufgaben.SetzeZustandAsync(taskId, "started");
    /// <summary>Aufgabe abschließen.</summary>
    public Task<TaskStateResponse> StopTaskAsync(int taskId) => _aufgaben.SetzeZustandAsync(taskId, "completed");
    /// <summary>Checklisten-Eintrag umschalten.</summary>
    public Task<ChecklistToggleResponse> ToggleChecklistItemAsync(int taskId, int itemIndex)
        => _aufgaben.SchalteChecklisteAsync(taskId, itemIndex);
    /// <summary>Notiz zur Aufgabe speichern.</summary>
    public Task<ApiResponse> SaveTaskNoteAsync(int taskId, string note)
        => _aufgaben.SpeichereNotizAsync(taskId, note);
    /// <summary>Protokoll einer Aufgabe.</summary>
    public Task<List<LogEntry>> GetTaskLogsAsync(int taskId) => _aufgaben.HoleProtokollAsync(taskId);
    /// <summary>Fotos einer Aufgabe (mit vollständigen Adressen).</summary>
    public Task<List<TaskImageDto>> GetTaskImagesAsync(int taskId) => _aufgaben.HoleFotosAsync(taskId);

    #endregion

    #region Putzliste

    /// <summary>Putzlisten-Eintrag abhaken.</summary>
    public Task<ChecklistToggleResponse> TogglePutzlisteItemAsync(int taskId, int eintragId)
        => _putzliste.SchalteEintragAsync(taskId, eintragId);
    /// <summary>Anmerkung zu einem Putzlisten-Eintrag speichern.</summary>
    public Task<ApiResponse> SavePutzlisteEintragKommentarAsync(int taskId, int eintragId, string kommentar)
        => _putzliste.SpeichereEintragKommentarAsync(taskId, eintragId, kommentar);
    /// <summary>Anmerkung zur gesamten Checkliste speichern.</summary>
    public Task<ApiResponse> SavePutzlisteChecklistKommentarAsync(int taskId, string kommentar)
        => _putzliste.SpeichereChecklistKommentarAsync(taskId, kommentar);
    /// <summary>Beweis-Foto hochladen.</summary>
    public Task<PutzlisteFotoResponse> UploadPutzlisteFotoAsync(int taskId, int eintragId, string fileName, byte[] bytes)
        => _putzliste.LadeFotoHochAsync(taskId, eintragId, fileName, bytes);
    /// <summary>Beweis-Foto löschen.</summary>
    public Task<ApiResponse> DeletePutzlisteFotoAsync(int fotoId) => _putzliste.LoescheFotoAsync(fotoId);

    #endregion

    #region Aufträge

    /// <summary>Daten der Auftragsseite laden.</summary>
    public Task<AuftragsPageDataResponse> GetAuftragsDataAsync() => _auftraege.HoleUebersichtAsync();
    /// <summary>Neuen Auftrag anlegen.</summary>
    public Task<ApiResponse> CreateAuftragAsync(string titel, string plannedDate, int? apartmentId,
        int? aufgabenartId, string? hinweis, string status, TaskAssignments? assignments)
        => _auftraege.LegeAnAsync(titel, plannedDate, apartmentId, aufgabenartId, hinweis, status, assignments);
    /// <summary>Auftrag ändern.</summary>
    public Task<ApiResponse> UpdateAuftragAsync(int taskId, string titel, string plannedDate, int? apartmentId,
        int? aufgabenartId, string? hinweis, string status, TaskAssignments? assignments)
        => _auftraege.AendereAsync(taskId, titel, plannedDate, apartmentId, aufgabenartId, hinweis, status, assignments);
    /// <summary>Auftrag löschen.</summary>
    public Task<ApiResponse> DeleteAuftragAsync(int taskId) => _auftraege.LoescheAsync(taskId);

    #endregion

    #region Bildlisten (Probleme und Anmerkungen)

    /// <summary>Eintrag mit Fotos anlegen.</summary>
    public Task<ImageListDescriptionResponse> CreateImageListItemAsync(int taskId, string itemType, string name,
        string? description, List<(string FileName, byte[] Bytes)>? photos)
        => _bildlisten.LegeAnAsync(taskId, itemType, name, description, photos);
    /// <summary>Eintrag ändern.</summary>
    public Task<ImageListDescriptionResponse> UpdateImageListItemAsync(int itemId, string name, string? description)
        => _bildlisten.AendereAsync(itemId, name, description);
    /// <summary>Eintrag ändern (schlichte Antwort).</summary>
    public Task<ApiResponse> UpdateImageListDescriptionAsync(int id, string name, string description)
        => _bildlisten.AendereEinfachAsync(id, name, description);
    /// <summary>Eintrag löschen.</summary>
    public Task<ImageListDescriptionDeleteResponse> DeleteImageListItemAsync(int itemId)
        => _bildlisten.LoescheAsync(itemId);
    /// <summary>Einzelnes Foto eines Eintrags löschen.</summary>
    public Task<ApiResponse> DeleteImageListPhotoAsync(int photoId) => _bildlisten.LoescheFotoAsync(photoId);
    /// <summary>Weiteres Foto an einen Eintrag anhängen.</summary>
    public Task<ApiResponse> AddPhotoToImageListDescriptionAsync(int id, byte[] photoBytes)
        => _bildlisten.HaengeFotoAnAsync(id, photoBytes);
    /// <summary>Anmerkung mit Fotos anlegen.</summary>
    public Task<ApiResponse> CreateTaskAnmerkungAsync(int taskId, string name, string description, List<byte[]> photos)
        => _bildlisten.LegeAnmerkungAnAsync(taskId, name, description, photos);
    /// <summary>Einträge eines Typs laden ('problem', 'anmerkung', 'aufgabe').</summary>
    public Task<List<ImageListDescription>> GetTaskItemsAsync(int taskId, string itemType)
        => _bildlisten.HoleEintraegeAsync(taskId, itemType);
    /// <summary>Anmerkungen einer Aufgabe laden.</summary>
    public Task<List<ImageListDescription>> GetTaskAnmerkungenAsync(int taskId)
        => _bildlisten.HoleEintraegeAsync(taskId, ImageListDescription.TypAnmerkung);

    #endregion

    #region Chat

    /// <summary>Nachrichten eines Gesprächs laden.</summary>
    public Task<List<ChatMessage>> GetChatMessagesAsync(string partnerId = "admin")
        => _chat.HoleNachrichtenAsync(partnerId);
    /// <summary>Nachricht senden.</summary>
    public Task<ChatSendResponse> SendChatMessageAsync(string text, string receiverId = "admin", string? linkPhotoVideo = null)
        => _chat.SendeAsync(text, receiverId, linkPhotoVideo);
    /// <summary>Anhang hochladen.</summary>
    public Task<ChatImageUploadResponse> UploadChatImageAsync(Stream imageStream, string fileName)
        => _chat.LadeAnhangHochAsync(imageStream, fileName);
    /// <summary>Noch nicht gesendeten Anhang löschen.</summary>
    public Task<ApiResponse> DeleteChatImageAsync(string path) => _chat.LoescheAnhangAsync(path);
    /// <summary>Anhang einer gesendeten Nachricht löschen.</summary>
    public Task<ApiResponse> DeleteMessageImageAsync(int messageId) => _chat.LoescheNachrichtenAnhangAsync(messageId);
    /// <summary>Übersetzung vorab ansehen.</summary>
    public Task<TranslationPreviewResponse> PreviewTranslationAsync(string text, string? receiverId = null)
        => _chat.ZeigeUebersetzungAsync(text, receiverId);
    /// <summary>Gesprächsverlauf löschen.</summary>
    public Task<ApiResponse> DeleteChatMessagesAsync(string receiverId = "admin")
        => _chat.LoescheVerlaufAsync(receiverId);

    #endregion

    #region Einstellungen, Mitteilungen, Bilder, Berichte

    /// <summary>Sprache setzen.</summary>
    public Task<ApiResponse> SetLanguageAsync(string language) => _einstellungen.SetzeSpracheAsync(language);
    /// <summary>Avatar setzen.</summary>
    public Task<ApiResponse> SetAvatarAsync(string avatar) => _einstellungen.SetzeAvatarAsync(avatar);
    /// <summary>Liste der Arbeitskräfte.</summary>
    public Task<CleanersListResponse?> GetCleanersListAsync() => _einstellungen.HoleArbeitskraefteAsync();
    /// <summary>Push-Token anmelden.</summary>
    public Task<ApiResponse> RegisterPushTokenAsync(string token, string platform)
        => _push.MeldeTokenAnAsync(token, platform);
    /// <summary>Push-Token abmelden.</summary>
    public Task<ApiResponse> UnregisterPushTokenAsync(string token) => _push.MeldeTokenAbAsync(token);
    /// <summary>Firebase-Anmeldetoken holen.</summary>
    public Task<(string token, int cleanerId, int propertyId, bool firestoreEnabled)?> GetFirebaseTokenAsync()
        => _push.HoleFirebaseTokenAsync();
    /// <summary>Bild mit Anmeldung laden.</summary>
    public Task<ImageSource?> GetImageAsync(string url) => _bilder.HoleBildAsync(url);
    /// <summary>Absturzbericht senden.</summary>
    public Task<bool> SendCrashReportAsync(CrashReport report, string? cleanerName = null)
        => _berichte.SendeAsync(report, cleanerName);

    #endregion
}
