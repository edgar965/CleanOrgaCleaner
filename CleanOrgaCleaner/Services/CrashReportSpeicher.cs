using System.Text.Json;
using CleanOrgaCleaner.Json;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Dateiverwaltung der Absturzberichte (crash_reports.json).
///
/// Alle Zugriffe laufen über EINE Sperre - vorher lagen Lesen, Schreiben und
/// Aufräumen verteilt im Dienst und mussten sich die Sperre selbst merken.
/// </summary>
public sealed class CrashReportSpeicher
{
    /// <summary>So viele Berichte werden höchstens aufgehoben.</summary>
    private const int HoechstzahlBerichte = 10;

    /// <summary>So viele bereits gesendete Berichte bleiben erhalten.</summary>
    private const int HoechstzahlGesendet = 5;

    private readonly string _datei;
    private readonly object _sperre = new();

    public CrashReportSpeicher(string datei) => _datei = datei;

    /// <summary>Alle gespeicherten Berichte lesen.</summary>
    public List<CrashReport> Lies()
    {
        lock (_sperre)
        {
            return LiesOhneSperre();
        }
    }

    /// <summary>Bericht anhängen und die Datei auf die Höchstzahl kürzen.</summary>
    public void Ergaenze(CrashReport bericht)
    {
        lock (_sperre)
        {
            var berichte = LiesOhneSperre();
            berichte.Add(bericht);

            if (berichte.Count > HoechstzahlBerichte)
                berichte = berichte.Skip(berichte.Count - HoechstzahlBerichte).ToList();

            SchreibeOhneSperre(berichte);
        }
    }

    /// <summary>Noch nicht gesendete Berichte.</summary>
    public List<CrashReport> LiesOffene()
    {
        lock (_sperre)
        {
            return LiesOhneSperre().Where(b => !b.Sent).ToList();
        }
    }

    /// <summary>
    /// Sende-Kennzeichen in der AKTUELLEN Datei setzen (nicht die alte Liste
    /// zurückschreiben - während des Sendens können neue Berichte dazugekommen
    /// sein). Jeder gesendete Bericht verbraucht genau EINEN Dateieintrag:
    /// zwei feldgleiche Berichte würden sonst beide als gesendet gelten,
    /// obwohl nur einer angekommen ist.
    /// </summary>
    public void MarkiereGesendet(List<CrashReport> gesendet)
    {
        lock (_sperre)
        {
            var aktuell = LiesOhneSperre();
            var offen = new List<CrashReport>(gesendet);

            foreach (var bericht in aktuell)
            {
                if (bericht.Sent)
                    continue;

                var passt = offen.FindIndex(g =>
                    g.Timestamp == bericht.Timestamp &&
                    g.ExceptionType == bericht.ExceptionType &&
                    g.Message == bericht.Message);

                if (passt >= 0)
                {
                    bericht.Sent = true;
                    offen.RemoveAt(passt);
                }
            }

            SchreibeOhneSperre(aktuell);
        }
    }

    /// <summary>Alte, bereits gesendete Berichte entfernen.</summary>
    public void RaeumeAuf()
    {
        try
        {
            lock (_sperre)
            {
                var berichte = LiesOhneSperre();
                var gesendet = berichte.Where(b => b.Sent).OrderByDescending(b => b.Timestamp).Take(HoechstzahlGesendet);
                var offen = berichte.Where(b => !b.Sent);
                var behalten = offen.Concat(gesendet).ToList();

                if (behalten.Count < berichte.Count)
                    SchreibeOhneSperre(behalten);
            }
        }
        catch { }
    }

    private List<CrashReport> LiesOhneSperre()
    {
        try
        {
            if (File.Exists(_datei))
            {
                var json = File.ReadAllText(_datei);
                return JsonSerializer.Deserialize(json, AppJsonContext.Default.ListCrashReport) ?? new List<CrashReport>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to load crash reports: {ex.Message}");
        }
        return new List<CrashReport>();
    }

    private void SchreibeOhneSperre(List<CrashReport> berichte)
    {
        var json = JsonSerializer.Serialize(berichte, AppJsonContext.Default.ListCrashReport);
        File.WriteAllText(_datei, json);
    }
}
