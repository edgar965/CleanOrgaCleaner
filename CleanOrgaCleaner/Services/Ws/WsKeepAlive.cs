using System.Net.WebSockets;
using System.Text;

namespace CleanOrgaCleaner.Services.Ws;

/// <summary>
/// Hält die WebSocket-Verbindung wach.
///
/// iOS trennt untätige Verbindungen nach etwa einer Minute, und das managed
/// KeepAliveInterval wird dort nicht zuverlässig umgesetzt. Deshalb schickt
/// diese Schleife alle 20 Sekunden einen eigenen Ping (der Server antwortet
/// mit "pong"). Sie läuft auf ihrem EIGENEN Socket samt Token - ein
/// Neuverbinden beendet sie sauber.
/// </summary>
public static class WsKeepAlive
{
    private static readonly byte[] _ping = Encoding.UTF8.GetBytes("{\"type\":\"ping\"}");

    /// <summary>Abstand zwischen zwei Pings.</summary>
    private static readonly TimeSpan _abstand = TimeSpan.FromSeconds(20);

    /// <summary>Schleife starten (läuft bis der Socket schließt).</summary>
    public static async Task LaufeAsync(ClientWebSocket socket, CancellationTokenSource cts)
    {
        try
        {
            while (socket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(_abstand, cts.Token).ConfigureAwait(false);
                if (socket.State != WebSocketState.Open || cts.Token.IsCancellationRequested)
                    break;

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
