namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Wacht darüber, ob über Nacht der Tag gewechselt hat, und meldet das.
///
/// Bleibt die App über Mitternacht offen, zeigt die Tagesliste sonst den
/// Vortag. Die Uhr-Verwaltung gehört nicht in die Seite - deshalb diese Klasse.
/// </summary>
public sealed class DatumswechselWaechter : IDisposable
{
    /// <summary>Prüfabstand: fünf Minuten reichen für einen Tageswechsel.</summary>
    private static readonly TimeSpan Abstand = TimeSpan.FromMinutes(5);

    private readonly Action _beiWechsel;
    private System.Timers.Timer? _uhr;
    private DateTime _stand;

    public DatumswechselWaechter(Action beiWechsel) => _beiWechsel = beiWechsel;

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
        if (DateTime.Today == _stand) return;

        _stand = DateTime.Today;
        // Uhr-Ereignis: eine ungefangene Exception würde den Prozess beenden
        try { _beiWechsel(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatumswechselWaechter] {ex.Message}");
        }
    }
}
