using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Meldet dem Server regelmäßig "ich bin online".
///
/// Das Intervall gibt der Server in der Antwort vor (ping_interval); der Timer
/// stellt sich darauf ein. Beim Abmelden wird zuerst das Stopp-Kennzeichen
/// gesetzt, damit ein bereits laufender Aufruf den Offline-Status nicht wieder
/// überschreibt.
/// </summary>
public sealed class HeartbeatDienst
{
    private readonly ApiHttpKern _http;

    private System.Timers.Timer? _timer;
    private int _intervallSekunden = 30;
    private volatile bool _gestoppt;

    public HeartbeatDienst(ApiHttpKern http) => _http = http;

    /// <summary>Nach erfolgreichem Login starten.</summary>
    public void Starte()
    {
        Stoppe();
        _gestoppt = false;

        _timer = new System.Timers.Timer(_intervallSekunden * 1000);
        _timer.Elapsed += async (_, _) => await SendeAsync().ConfigureAwait(false);
        _timer.AutoReset = true;
        _timer.Start();

        System.Diagnostics.Debug.WriteLine($"[Heartbeat] Started with interval {_intervallSekunden}s");

        // Ersten Schlag sofort senden
        _ = SendeAsync();
    }

    /// <summary>Beim Abmelden stoppen.</summary>
    public void Stoppe()
    {
        _gestoppt = true; // ZUERST setzen, damit laufende Aufrufe abbrechen

        if (_timer == null)
            return;

        _timer.Stop();
        _timer.Dispose();
        _timer = null;
        System.Diagnostics.Debug.WriteLine("[Heartbeat] Stopped");
    }

    /// <summary>Einen Schlag senden und ein neues Intervall übernehmen.</summary>
    private async Task SendeAsync()
    {
        if (_gestoppt)
        {
            System.Diagnostics.Debug.WriteLine("[Heartbeat] Skipped - logout in progress");
            return;
        }

        try
        {
            var antwort = await _http.HoleAsync("/mobile/api/heartbeat/").ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Response: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.Erfolgreich)
                return;

            var ergebnis = antwort.Deserialisiere<HeartbeatResponse>();
            if (ergebnis == null || ergebnis.PingInterval <= 0 || ergebnis.PingInterval == _intervallSekunden)
                return;

            _intervallSekunden = ergebnis.PingInterval;
            if (_timer != null)
            {
                _timer.Interval = _intervallSekunden * 1000;
                System.Diagnostics.Debug.WriteLine($"[Heartbeat] Interval updated to {_intervallSekunden}s");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Heartbeat] Error: {ex.Message}");
        }
    }
}
