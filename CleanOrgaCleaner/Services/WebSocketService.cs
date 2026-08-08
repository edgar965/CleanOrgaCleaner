using System.Net.WebSockets;
using System.Text;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services.Ws;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Echtzeit-Verbindung zum Server (Chat und Aufgaben über EINE Verbindung).
///
/// Die Klasse hält nur noch Verbindung und Empfangsschleife. Neuverbinden,
/// Keepalive und das Auswerten der Nachrichten stecken in eigenen Klassen
/// unter Services/Ws.
/// </summary>
public class WebSocketService : IDisposable
{
    private const string WsBasisUrl = "wss://cleanorga.com";

    private static readonly Lazy<WebSocketService> _instanz = new(() => new WebSocketService());

    /// <summary>Die eine Instanz der App.</summary>
    public static WebSocketService Instance => _instanz.Value;

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;

    // Serialisiert das Verbinden: Neuverbinden, App-Rückkehr und Login können
    // sonst parallel verbinden und sich gegenseitig den Socket wegnehmen
    private readonly SemaphoreSlim _verbindeSperre = new(1, 1);

    private readonly WsWiederverbinder _wiederverbinder;
    private readonly WsNachrichtenVerteiler _verteiler;

    private volatile bool _istOnline;
    private volatile bool _sollWiederverbinden = true;

    /// <summary>Neue Chat-Nachricht eingetroffen.</summary>
    public event Action<ChatMessage>? OnChatMessageReceived;

    /// <summary>Aufgaben haben sich geändert (Art der Änderung als Text).</summary>
    public event Action<string>? OnTaskUpdate;

    /// <summary>Verbindungszustand hat sich geändert.</summary>
    public event Action<bool>? OnConnectionStatusChanged;

    /// <summary>True, solange die Verbindung steht.</summary>
    public bool IsOnline => _istOnline;

    /// <summary>
    /// True, sobald in dieser App-Sitzung mindestens einmal eine Verbindung
    /// bestand. Verhindert, dass der Offline-Hinweis beim allerersten Laden
    /// (Verbindungsaufbau läuft noch) fälschlich aufblitzt.
    /// </summary>
    public bool WarSchonVerbunden { get; private set; }

    private WebSocketService()
    {
        _wiederverbinder = new WsWiederverbinder(
            VerbindeEinmalAsync,
            () => _socket?.State == WebSocketState.Open,
            () => _sollWiederverbinden && !App.IsInBackground);

        _verteiler = new WsNachrichtenVerteiler(
            nachricht => UiSicher.AufMainThread(() => OnChatMessageReceived?.Invoke(nachricht), "WS"),
            art => UiSicher.AufMainThread(() => OnTaskUpdate?.Invoke(art), "WS"));
    }

    /// <summary>
    /// Mit dem Endpunkt /ws/main/ verbinden; bei Misserfolg startet die
    /// (einzige) Wiederverbindungs-Kette.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (!await VerbindeEinmalAsync().ConfigureAwait(false))
            await _wiederverbinder.StarteAsync().ConfigureAwait(false);
    }

    /// <summary>Alter Name für <see cref="ConnectAsync"/> (Chat-Seiten).</summary>
    public Task ConnectChatAsync() => ConnectAsync();

    /// <summary>Verbindung erzwingen (z.B. bei Rückkehr ins Netz).</summary>
    public async Task ReconnectAsync()
    {
        _sollWiederverbinden = true;
        _wiederverbinder.Zuruecksetzen();
        await ConnectAsync().ConfigureAwait(false);
    }

    /// <summary>Verbindung beenden (kein automatisches Wiederverbinden mehr).</summary>
    public async Task DisconnectAsync()
    {
        _sollWiederverbinden = false;
        _cts?.Cancel();

        if (_socket?.State == WebSocketState.Open)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
            }
            catch { }
        }

        SetzeOnline(false);
    }

    /// <summary>
    /// Aufgaben-Update von außen auslösen, z.B. nach App-Rückkehr: während die
    /// App im Hintergrund war, war die Verbindung getrennt - Änderungen aus
    /// dieser Zeit kamen nie an.
    /// </summary>
    public void NotifyTaskUpdate(string type = "task_updated")
        => UiSicher.AufMainThread(() => OnTaskUpdate?.Invoke(type), "WS");

    /// <summary>
    /// Chat-Nachricht von außen einspeisen (Firestore-Empfang) - dieselbe
    /// Meldung wie über den WebSocket, damit die Oberfläche unverändert bleibt.
    /// Doppelte Nachrichten fangen die Seiten per Id-Abgleich ab.
    /// </summary>
    public void NotifyChatMessage(ChatMessage message)
    {
        if (message == null)
            return;
        UiSicher.AufMainThread(() => OnChatMessageReceived?.Invoke(message), "FS");
    }

    /// <summary>
    /// Ein einzelner Verbindungsversuch (per Sperre serialisiert), OHNE
    /// Folge-Versuche - die steuert allein der Wiederverbinder.
    /// </summary>
    private async Task<bool> VerbindeEinmalAsync()
    {
        if (_socket?.State == WebSocketState.Open)
            return true;

        var fehlgeschlagen = false;
        await _verbindeSperre.WaitAsync().ConfigureAwait(false);
        try
        {
            // Doppelprüfung nach der Sperre: ein paralleler Aufruf kann die
            // Verbindung inzwischen aufgebaut haben
            if (_socket?.State == WebSocketState.Open)
                return true;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _socket?.Dispose();
            _socket = new ClientWebSocket();
            // Keepalive: iOS trennt untätige Verbindungen nach etwa einer Minute.
            // Der managed Wert wird dort nicht zuverlässig umgesetzt, daher
            // zusätzlich ein eigener Ping (WsKeepAlive).
            _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            var cookies = ApiService.Instance.GetCookieHeader();
            if (!string.IsNullOrEmpty(cookies))
            {
                _socket.Options.SetRequestHeader("Cookie", cookies);
                _socket.Options.SetRequestHeader("Origin", "https://cleanorga.com");
            }

            var adresse = new Uri($"{WsBasisUrl}/ws/main/");
            System.Diagnostics.Debug.WriteLine($"Connecting to WebSocket: {adresse}");

            // Zeitgrenze für iOS - sonst hängt der Aufbau ewig
            using var zeitgrenze = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var verbunden = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, zeitgrenze.Token);

            await _socket.ConnectAsync(adresse, verbunden.Token).ConfigureAwait(false);

            if (_socket.State == WebSocketState.Open)
            {
                var warOffline = !_istOnline;
                // VOR dem Melden setzen: die Oberfläche prüft im Handler, ob
                // schon einmal eine Verbindung bestand
                WarSchonVerbunden = true;
                SetzeOnline(true);
                System.Diagnostics.Debug.WriteLine("WebSocket connected (unified)");

                _ = EmpfangeAsync();
                _ = WsKeepAlive.LaufeAsync(_socket, _cts);

                if (warOffline)
                    _ = HoleWarteschlangeNachAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket error: {ex.Message}");
            SetzeOnline(false);
            fehlgeschlagen = true;
        }
        finally
        {
            _verbindeSperre.Release();
        }

        return !fehlgeschlagen;
    }

    /// <summary>Empfangsschleife des aktuellen Sockets.</summary>
    private async Task EmpfangeAsync()
    {
        var puffer = new byte[4096];
        var bauer = new StringBuilder();

        // Lokale Kopien: greift ein Neuverbinden die Felder an, liest dieser
        // (alte) Empfänger weiter seinen EIGENEN Socket und endet sauber.
        var socket = _socket;
        var cts = _cts;
        if (socket == null || cts == null)
            return;

        try
        {
            while (socket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                var ergebnis = await socket.ReceiveAsync(new ArraySegment<byte>(puffer), cts.Token).ConfigureAwait(false);

                if (ergebnis.MessageType == WebSocketMessageType.Close)
                {
                    System.Diagnostics.Debug.WriteLine("WebSocket closed by server");
                    break;
                }

                // Erster echter Empfang = Verbindung trägt -> Abstand zurücksetzen
                _wiederverbinder.Zuruecksetzen();

                bauer.Append(Encoding.UTF8.GetString(puffer, 0, ergebnis.Count));

                if (ergebnis.EndOfMessage)
                {
                    var nachricht = bauer.ToString();
                    bauer.Clear();
                    _verteiler.Verteile(nachricht);
                }
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("WebSocket listening cancelled");
            return; // nach Abbruch NICHT neu verbinden
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebSocket listen error: {ex.Message}");
            if (App.IsInBackground)
                return;
        }

        // Nur reagieren, wenn dieser Empfänger noch der aktive ist. Ein durch
        // Neuverbinden ersetzter Empfänger darf weder den Zustand ändern noch
        // eine weitere Kette auslösen.
        if (!ReferenceEquals(socket, _socket))
            return;

        SetzeOnline(false);
        await _wiederverbinder.StarteAsync().ConfigureAwait(false);
    }

    /// <summary>Wartende Offline-Vorgänge nachholen.</summary>
    private static async Task HoleWarteschlangeNachAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[WebSocket] Processing offline queue...");
            await OfflineQueueService.Instance.ProcessQueueAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebSocket] Error processing offline queue: {ex.Message}");
        }
    }

    /// <summary>Zustand setzen und die Oberfläche benachrichtigen.</summary>
    private void SetzeOnline(bool online)
    {
        _istOnline = online;
        UiSicher.SichererInvoke(() => OnConnectionStatusChanged?.Invoke(online), "WS");
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _socket?.Dispose();
        _verbindeSperre.Dispose();
    }
}
