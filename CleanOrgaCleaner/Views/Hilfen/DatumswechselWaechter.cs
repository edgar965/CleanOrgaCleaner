namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Hält die Tagesansicht aktuell: meldet einen Datumswechsel und frischt die
/// Liste regelmäßig auf.
///
/// Der Datumsteil verhindert, dass eine über Nacht offene App noch den Vortag
/// zeigt.
///
/// Die Auffrischung ist ein Sicherheitsnetz: Am 09.08.2026 erreichten
/// Zuweisungen ein iPhone gar nicht mehr, weil die Singleton-Dienste beim
/// Abmelden ihre Sperre freigegeben hatten und danach keine WebSocket-
/// Verbindung mehr zustande kam (siehe WebSocketService.Dispose). Das ist
/// behoben; bleibt eine Meldung aus welchem Grund auch immer aus, ist die
/// Liste dank dieser Auffrischung höchstens fünf Minuten alt.
/// </summary>
public sealed class DatumswechselWaechter : IDisposable
{
    /// <summary>Prüfabstand. Deckt Datumswechsel und Auffrischung ab.</summary>
    private static readonly TimeSpan Abstand = TimeSpan.FromMinutes(5);

    private readonly Action _beiWechsel;
    private readonly Action? _beiAuffrischung;
    private System.Timers.Timer? _uhr;
    private DateTime _stand;

    /// <param name="beiWechsel">Läuft, wenn ein neuer Tag begonnen hat.</param>
    /// <param name="beiAuffrischung">
    /// Läuft bei jedem Takt. Gedacht zum Nachladen der Liste, damit eine
    /// verlorene Live-Meldung die Ansicht nicht dauerhaft veralten lässt.
    /// </param>
    public DatumswechselWaechter(Action beiWechsel, Action? beiAuffrischung = null)
    {
        _beiWechsel = beiWechsel;
        _beiAuffrischung = beiAuffrischung;
    }

    /// <summary>Datum merken und die Uhr starten (ein laufender Wächter wird ersetzt).</summary>
    public void Starten()
    {
        Beenden();
        _stand = DateTime.Today;
        _uhr = new System.Timers.Timer(Abstand.TotalMilliseconds) { AutoReset = true };
        _uhr.Elapsed += Pruefe;
        _uhr.Start();
    }

    public void Beenden()
    {
        if (_uhr == null) return;
        _uhr.Stop();
        _uhr.Elapsed -= Pruefe;
        _uhr.Dispose();
        _uhr = null;
    }

    public void Dispose() => Beenden();

    private void Pruefe(object? sender, System.Timers.ElapsedEventArgs e)
    {
        // Uhr-Ereignis: eine ungefangene Exception würde den Prozess beenden.
        if (DateTime.Today != _stand)
        {
            _stand = DateTime.Today;
            Sicher(_beiWechsel, "Datumswechsel");
            return;     // der Wechsel lädt die Liste ohnehin neu
        }

        if (_beiAuffrischung != null)
            Sicher(_beiAuffrischung, "Auffrischung");
    }

    private static void Sicher(Action aktion, string zweck)
    {
        try { aktion(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatumswechselWaechter/{zweck}] {ex.Message}");
        }
    }
}
