namespace CleanOrgaCleaner.Services.Ws;

/// <summary>
/// Steuert das erneute Verbinden des WebSockets mit wachsendem Abstand
/// (1s, 2s, 4s ... maximal 30s).
///
/// Es läuft immer nur EINE Kette: der Wächter bleibt über den gesamten Zyklus
/// (Warten + Verbindungsversuch) gehalten, sonst startet ein zweiter Auslöser
/// währenddessen eine parallele Kette.
/// </summary>
public sealed class WsWiederverbinder
{
    private const int ErsterAbstandMs = 1000;
    private const int MaximalerAbstandMs = 30000;

    private readonly Func<Task<bool>> _verbindeVersuch;
    private readonly Func<bool> _istVerbunden;
    private readonly Func<bool> _darfVerbinden;

    // 0 = keine Kette aktiv, 1 = eine läuft (Interlocked-Wächter)
    private int _laeuft;
    private int _versuche;

    public WsWiederverbinder(Func<Task<bool>> verbindeVersuch, Func<bool> istVerbunden, Func<bool> darfVerbinden)
    {
        _verbindeVersuch = verbindeVersuch;
        _istVerbunden = istVerbunden;
        _darfVerbinden = darfVerbinden;
    }

    /// <summary>
    /// Zähler zurücksetzen - erst nach echtem Nachrichtenempfang, denn nur der
    /// beweist, dass die Verbindung trägt. Sonst würde eine flatternde
    /// Verbindung ewig im Sekundentakt neu verbinden.
    /// </summary>
    public void Zuruecksetzen() => _versuche = 0;

    /// <summary>Kette starten (tut nichts, wenn schon eine läuft).</summary>
    public async Task StarteAsync()
    {
        if (!_darfVerbinden())
            return;

        if (Interlocked.CompareExchange(ref _laeuft, 1, 0) != 0)
            return;

        try
        {
            while (_darfVerbinden() && !_istVerbunden())
            {
                _versuche++;
                var abstand = (int)Math.Min(ErsterAbstandMs * Math.Pow(2, _versuche - 1), MaximalerAbstandMs);
                System.Diagnostics.Debug.WriteLine($"WebSocket reconnecting in {abstand}ms (attempt {_versuche})");

                await Task.Delay(abstand).ConfigureAwait(false);

                if (!_darfVerbinden())
                    break;

                if (await _verbindeVersuch().ConfigureAwait(false))
                    break;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _laeuft, 0);
        }

        // Lücke schließen: stirbt der Socket genau zwischen erfolgreichem
        // Verbinden und dem Freigeben des Wächters, wird der Anstoß des
        // endenden Empfängers verworfen. Nach der Freigabe erneut prüfen -
        // sonst bliebe die App dauerhaft offline.
        if (_darfVerbinden() && !_istVerbunden())
            await StarteAsync().ConfigureAwait(false);
    }
}
