using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Ein Gesprächsverlauf (Verwaltung oder Kollegin/Kollege).
///
/// Senden und Übersetzungsvorschau liegen in ChatCurrentPage.Senden.cs,
/// die Bilder in ChatCurrentPage.Fotos.cs.
/// </summary>
public partial class ChatCurrentPage : ContentPage, IQueryAttributable
{
    private readonly ApiService _apiService;
    private readonly WebSocketService _webSocketService;
    private readonly ObservableCollection<ChatMessage> _messages = new();

    private string _partnerId = "admin";
    private string _partnerName = "Admin";
    private string _partnerAvatar = "";

    public ChatCurrentPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _webSocketService = WebSocketService.Instance;
        MessagesCollection.ItemsSource = _messages;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("partner", out var partner))
        {
            _partnerId = partner?.ToString() ?? "admin";
            _partnerName = _partnerId == "admin" ? "Admin" : "Kollege";
        }
        if (query.TryGetValue("partnerName", out var name))
            _partnerName = name?.ToString() ?? _partnerName;
        if (query.TryGetValue("partnerAvatar", out var avatar))
            _partnerAvatar = avatar?.ToString() ?? "";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Kopfleiste kümmert sich um Übersetzungen, Person, Arbeitszeit und Offline-Hinweis
            _ = Header.InitializeAsync();
            Header.SetPageTitle("chat");

            ZeigePartner();
            ApplyTranslations();

            // SOFORT abonnieren (nicht erst nach dem Netz-Roundtrip in
            // LadeMitVorgemerktenAsync): sonst läuft bei schnellem
            // Zurücknavigieren das '-=' in OnDisappearing VOR dem '+=' und
            // die tote Seite bleibt dauerhaft am Singleton abonniert
            _webSocketService.OnChatMessageReceived -= OnNewMessageReceived;
            _webSocketService.OnChatMessageReceived += OnNewMessageReceived;

            // Nach App-Resume/Neuverbindung Verlauf nachladen: Nachrichten, die
            // ankamen, während das Gerät gesperrt war (Verbindung getrennt),
            // würden auf der bereits offenen Seite sonst NIE erscheinen -
            // OnAppearing feuert bei App-Resume nicht.
            _webSocketService.OnConnectionStatusChanged -= OnVerbindungGeaendert;
            _webSocketService.OnConnectionStatusChanged += OnVerbindungGeaendert;

            // Laden nicht abwarten, damit die Seite sofort erscheint
            _ = LadeMitVorgemerktenAsync();
        }
        catch (Exception ex)
        {
            // Lifecycle-Handler: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _webSocketService.OnChatMessageReceived -= OnNewMessageReceived;
        _webSocketService.OnConnectionStatusChanged -= OnVerbindungGeaendert;
    }

    private void ApplyTranslations()
    {
        var t = Translations.Get;
        Title = t("chat");
        MessageEntry.Placeholder = t("message_placeholder");

        TranslationPreviewTitle.Text = t("translation_preview");
        YourTextLabel.Text = t("your_text") + ":";
        TranslationForAdminLabel.Text = t("translation_for_admin") + ":";
        BackTranslationLabel.Text = t("back_translation") + ":";
    }

    /// <summary>Kopfzeile des Gesprächs: Name, Symbol und Farbe.</summary>
    private void ZeigePartner()
    {
        PartnerNameLabel.Text = _partnerName;

        if (!string.IsNullOrEmpty(_partnerAvatar))
        {
            PartnerInitial.Text = _partnerAvatar;
            PartnerInitial.FontSize = 28; // größer für Emoji
        }
        else
        {
            PartnerInitial.Text = _partnerName.Length > 0 ? _partnerName.Substring(0, 1).ToUpper() : "?";
            PartnerInitial.FontSize = 20;
        }

        bool istVerwaltung = _partnerId == "admin";
        PartnerAvatar.Background = Verlauf(
            istVerwaltung ? "#667eea" : "#4CAF50",
            istVerwaltung ? "#764ba2" : "#45a049");
        PartnerStatusLabel.Text = Translations.Get(istVerwaltung ? "admin_contact" : "colleague");
    }

    private static LinearGradientBrush Verlauf(string von, string bis) => new(
        new GradientStopCollection
        {
            new GradientStop(Color.FromArgb(von), 0),
            new GradientStop(Color.FromArgb(bis), 1)
        },
        new Point(0, 0),
        new Point(1, 1));

    #region Nachrichten laden

    private async Task LadeMitVorgemerktenAsync()
    {
        if (App.PendingChatMessage != null)
            await Task.Delay(500);

        await LadeNachrichtenAsync();

        var vorgemerkt = App.PendingChatMessage;
        if (vorgemerkt == null)
        {
            _ = _webSocketService.ConnectChatAsync();
            return;
        }

        App.PendingChatMessage = null;

        // Nur anhängen, wenn die vorgemerkte Nachricht zu DIESEM Verlauf gehört -
        // App.OnChatMessageReceived merkt JEDE eingehende Nachricht vor, auch
        // aus fremden Gesprächen.
        Anhaengen(vorgemerkt, scrollen: false);

        _ = _webSocketService.ConnectChatAsync();
    }

    private async Task LadeNachrichtenAsync()
    {
        try
        {
            var geladen = await _apiService.GetChatMessagesAsync(_partnerId);

            // Zusammenführen statt Leeren+Neuaufbau: eine live über die
            // Verbindung während des Ladens eingetroffene Nachricht wird sonst
            // durch Clear() ausgelöscht. Ist die Liste leer, entspricht das dem
            // vollständigen Erstladen.
            var vorhanden = new HashSet<int>(_messages.Select(m => m.Id));
            foreach (var nachricht in geladen.OrderBy(m => m.Id))
            {
                if (vorhanden.Add(nachricht.Id))
                    _messages.Add(nachricht);
            }

            if (_messages.Count > 0)
            {
                await Task.Delay(100);
                ScrolleAnsEnde();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Laden: {ex.Message}");
        }
    }

    /// <summary>
    /// Verbindung wieder da (App-Resume oder Neuverbindung): verpasste
    /// Nachrichten nachladen. Das Laden dedupliziert über die Nachrichten-Id.
    /// </summary>
    private void OnVerbindungGeaendert(bool verbunden)
    {
        if (!verbunden) return;
        UiSicher.AufMainThread(() => LadeMitVorgemerktenAsync(), "ChatCurrentPage");
    }

    private void OnNewMessageReceived(ChatMessage message)
    {
        // Nur anhängen (kein Clear()+Reload - das blankte den Chat bei
        // Netzwerkfehlern aus), und nur wenn die Nachricht hierher gehört.
        UiSicher.AufMainThread(() => Anhaengen(message, scrollen: true), "ChatCurrentPage");
    }

    /// <summary>Nachricht anhängen, wenn sie hierher gehört und noch fehlt.</summary>
    private void Anhaengen(ChatMessage nachricht, bool scrollen)
    {
        if (!GehoertInDiesenThread(nachricht)) return;
        if (_messages.Any(m => m.Id == nachricht.Id)) return;

        _messages.Add(nachricht);
        if (scrollen)
            ScrolleAnsEnde();
    }

    /// <summary>
    /// Gehört eine eingehende Nachricht in den GERADE offenen Verlauf?
    /// Die Verbindung liefert alle Gespräche. Zuordnung über from_admin
    /// (eindeutiges Server-Signal) bzw. cleaner_id == eigene Id => Verwaltung.
    /// Im Zweifel anzeigen - lieber zu viel als eine verschluckte Nachricht.
    /// </summary>
    private bool GehoertInDiesenThread(ChatMessage message)
    {
        var meineId = _apiService.CleanerId;
        bool? istVerwaltung = message.FromAdmin ? true
            : (meineId.HasValue && message.CleanerId.HasValue) ? (message.CleanerId == meineId)
            : (bool?)null;

        if (istVerwaltung == null) return true; // nicht bestimmbar -> anzeigen

        return _partnerId == "admin"
            ? istVerwaltung.Value
            : (!istVerwaltung.Value && message.CleanerId?.ToString() == _partnerId);
    }

    /// <summary>Ans Ende der Liste springen - darf nie werfen.</summary>
    private void ScrolleAnsEnde()
    {
        if (_messages.Count == 0) return;
        try { MessagesCollection.ScrollTo(_messages.Count - 1, position: ScrollToPosition.End); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatCurrentPage] Scrollen: {ex.Message}");
        }
    }

    #endregion
}
