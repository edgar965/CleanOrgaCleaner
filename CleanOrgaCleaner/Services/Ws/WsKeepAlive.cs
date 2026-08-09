using System.Net.WebSockets;
using System.Text;

namespace CleanOrgaCleaner.Services.Ws;

/// <summary>
/// Hält die WebSocket-Verbindung wach UND überwacht, ob sie noch trägt.
///
/// iOS trennt untätige Verbindungen nach etwa einer Minute, und das managed
/// KeepAliveInterval wird dort nicht zuverlässig umgesetzt. Deshalb schickt
/// diese Schleife alle 20 Sekunden einen eigenen Ping (der Server antwortet
/// mit "pong"). Sie läuft auf ihrem EIGENEN Socket samt Token - ein
/// Neuverbinden beendet sie sauber.
///
/// Entscheidend ist die Gegenrichtung: Früher wurde nur gesendet und nie
/// geprüft, ob überhaupt noch etwas ankommt. Eine halb tote Verbindung
/// (Senden klappt, Empfangen nicht - nach Netzwechsel oder Hintergrund auf
/// iOS der Normalfall) blieb deshalb unbemerkt: Der Socket meldete weiter
/// "Open", die Pings gingen ins Leere, und die App bekam weder Chat-
/// Nachrichten noch Aufgaben-Änderungen. Sichtbar wurde das erst beim
/// Neustart, der die Listen ohnehin neu lädt (gemeldet am 09.08.2026:
/// "Aufgabe umgewiesen, erscheint nicht in der App").
/// </summary>
public static class WsKeepAlive
{
    private static readonly byte[] _ping = Encoding.UTF8.GetBytes("{\"type\":\"ping\"}");

    /// <summary>Abstand zwischen zwei Pings.</summary>
    private static readonly TimeSpan _abstand = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Nach dieser Stille gilt die Verbindung als tot. Grosszügig gewählt:
    /// Der Server antwortet auf jeden Ping, es müssen also mehrere Antworten
    /// hintereinander ausbleiben.
    /// </summary>
    private static readonly TimeSpan _hoechsteStille = TimeSpan.FromSeconds(70);

    /// <summary>Schleife starten (läuft bis der Socket schließt).</summary>
    /// <param name="stilleSeitEmpfang">
    /// Liefert, wie lange nichts mehr empfangen wurde. Wird die Spanne zu
    /// gross, bricht die Verbindung ab - der Wiederverbinder baut dann eine
    /// neue auf, und die Seiten laden verpasste Änderungen nach.
    /// </param>
    public static async Task LaufeAsync(ClientWebSocket socket, CancellationTokenSource cts,
                                        Func<TimeSpan>? stilleSeitEmpfang = null)
    {
        try
        {
            while (socket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(_abstand, cts.Token).ConfigureAwait(false);
                if (socket.State != WebSocketState.Open || cts.Token.IsCancellationRequested)
                    break;

                if (stilleSeitEmpfang != null && stilleSeitEmpfang() > _hoechsteStille)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[WS] Seit über einer Minute nichts empfangen - Verbindung gilt als tot");
                    // Abort statt Cancel: Der Empfänger endet dadurch mit einer
                    // Ausnahme und stösst das Neuverbinden an. Ein abgebrochenes
                    // Token würde er als gewolltes Beenden lesen und still enden.
                    try { socket.Abort(); } catch { }
                    break;
                }

                await socket.SendAsync(new ArraySegment<byte>(_ping),
                    WebSocketMessageType.Text, true, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WS] KeepAlive beendet: {ex.Message}");
        }
    }
}
