using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Components;

/// <summary>
/// Gemeinsame Kopfleiste aller Seiten: Titel, angemeldete Person,
/// Offline-Hinweis, Arbeitszeit-Knopf und Menü.
///
/// Arbeitszeit liegt in AppHeader.Arbeitszeit.cs, das Menü samt Abmelden in
/// AppHeader.Menue.cs.
/// </summary>
public partial class AppHeader : ContentView
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;

    public AppHeader()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;

        // An-/Abmelden über Loaded/Unloaded statt im Konstruktor: Kopfleisten
        // hängen sonst dauerhaft am WebSocket-Singleton (Speicherleck, und
        // verwaiste Kopfleisten aktualisieren abgebaute Views -> iOS-Crash)
        Loaded += (s, e) =>
        {
            _webSocketService.OnConnectionStatusChanged -= OnConnectionStatusChanged;
            _webSocketService.OnConnectionStatusChanged += OnConnectionStatusChanged;

            // Hinweis mit dem AKTUELLEN Verbindungszustand abgleichen: während
            // die Seite verdeckt/entladen war, verpasste Statuswechsel würden
            // sonst bis zum nächsten Ereignis einen falschen Hinweis zeigen.
            // Nur wenn schon einmal eine Verbindung bestand - sonst blitzt der
            // Hinweis beim allerersten Laden (Verbinden läuft noch) auf.
            if (_webSocketService.WarSchonVerbunden)
                UiSicher.AufMainThread(() => UpdateOfflineBanner(!_webSocketService.IsOnline), "AppHeader");
        };
        Unloaded += (s, e) =>
        {
            _webSocketService.OnConnectionStatusChanged -= OnConnectionStatusChanged;
        };

        // Beim ersten Laden keinen Offline-Hinweis zeigen
        UpdateOfflineBanner(false);
    }

    private static void Log(string msg)
    {
        var line = $"[HEADER] {msg}";
        System.Diagnostics.Debug.WriteLine(line);
        _ = Task.Run(() => ApiService.WriteLog(line));
    }

    /// <summary>
    /// Kopfleiste füllen. Der Arbeitszeit-Status wird bewusst nicht abgewartet,
    /// damit die Seite ohne Netz-Wartezeit erscheint.
    /// </summary>
    public Task InitializeAsync()
    {
        Log("InitializeAsync START");
        ApplyTranslations();
        UpdateUserInfo();
        _ = LoadWorkStatusAsync();
        Log("InitializeAsync DONE");
        return Task.CompletedTask;
    }

    public void ApplyTranslations()
    {
        var t = Translations.Get;

        // Menüpunkte mit Symbol
        MenuTodayButton.Text = "🏠 " + t("today");
        MenuChatButton.Text = "💬 " + t("chat");
        MenuAuftragButton.Text = "📋 " + t("new_task");
        MenuSettingsButton.Text = "⚙️ " + t("settings");
        MenuLogoutButton.Text = "🚪 " + t("logout");

        UpdateWorkButton();

        // Rückfrage beim Beenden der Arbeitszeit
        WorkStopQuestion.Text = t("cleaning_finished");
        WorkStopYesButton.Text = t("yes");
        WorkStopNoButton.Text = t("no");
        WorkStopCancelButton.Text = t("cancel");

        OfflineLabel.Text = t("offline");
    }

    public void SetPageTitle(string titleKey)
    {
        PageTitleLabel.Text = Translations.Get(titleKey);
    }

    private void UpdateUserInfo()
    {
        UserInfoLabel.Text = _apiService.CleanerName ?? Preferences.Get("username", "");
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        UiSicher.AufMainThread(() => UpdateOfflineBanner(!isConnected), "AppHeader");
    }

    public void UpdateOfflineBanner(bool showOffline)
    {
        OfflineBanner.IsVisible = showOffline;
        OfflineSpinner.IsRunning = showOffline;
    }
}
