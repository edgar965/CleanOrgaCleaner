namespace CleanOrgaCleaner.Services;

/// <summary>
/// Fängt Abstürze ab, hält sie lokal fest und schickt sie an den Server.
///
/// Die Dateiverwaltung steckt in <see cref="CrashReportSpeicher"/>, das
/// Versenden in der Api-Schicht - hier bleiben nur die Handler und der
/// Sendelauf.
/// </summary>
public class CrashReportService
{
    private static readonly Lazy<CrashReportService> _instanz = new(() => new CrashReportService());

    /// <summary>Die eine Instanz der App.</summary>
    public static CrashReportService Instance => _instanz.Value;

    private readonly CrashReportSpeicher _speicher;

    // Einer nach dem anderen: Start-Versand und Sofort-Versand würden sonst
    // dieselben offenen Berichte laden und doppelt schicken.
    private readonly SemaphoreSlim _sendeSperre = new(1, 1);

    private CrashReportService()
    {
        _speicher = new CrashReportSpeicher(Path.Combine(FileSystem.AppDataDirectory, "crash_reports.json"));
    }

    /// <summary>Handler einhängen - im App-Konstruktor aufrufen.</summary>
    public void Initialize()
    {
        AppDomain.CurrentDomain.UnhandledException += BeiUnbehandelterAusnahme;
        TaskScheduler.UnobservedTaskException += BeiUnbeobachteterTaskAusnahme;

        System.Diagnostics.Debug.WriteLine("[CrashReport] Crash handlers initialized");
    }

    /// <summary>Alle gespeicherten Berichte.</summary>
    public List<CrashReport> LoadCrashReports() => _speicher.Lies();

    /// <summary>Bericht ablegen und sofort einen Sendeversuch anstoßen.</summary>
    public void SaveCrashReport(Exception ex, string source)
    {
        try
        {
            _speicher.Ergaenze(new CrashReport
            {
                Timestamp = DateTime.UtcNow,
                Source = source,
                ExceptionType = ex.GetType().FullName ?? "Unknown",
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? "",
                InnerException = ex.InnerException?.Message,
                DeviceInfo = ErmittleGeraet(),
                AppVersion = ErmittleVersion()
            });

            System.Diagnostics.Debug.WriteLine($"[CrashReport] Saved crash report: {ex.Message}");

            // Sofortversuch: bei UnobservedTaskException läuft die App weiter
            // (SetObserved), dann kommt der Bericht noch in dieser Sitzung durch.
            // Bei einem tödlichen Absturz schlägt es fehl - dann greift der
            // Start-Versand beim nächsten App-Start.
            TrySendPendingReportsInBackground();
        }
        catch (Exception speicherFehler)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to save crash report: {speicherFehler.Message}");
        }
    }

    /// <summary>
    /// Hintergrund-Versand ohne Warten - der EINE Weg, den alle Aufrufstellen
    /// (App-Start, nach SaveCrashReport, Anmeldeseite) nutzen sollen.
    /// </summary>
    public void TrySendPendingReportsInBackground()
    {
        _ = Task.Run(async () =>
        {
            try { await SendPendingReportsAsync().ConfigureAwait(false); }
            catch { }
        });
    }

    /// <summary>Offene Berichte an den Server schicken.</summary>
    public async Task SendPendingReportsAsync()
    {
        await _sendeSperre.WaitAsync().ConfigureAwait(false);
        try
        {
            var offene = _speicher.LiesOffene();
            if (offene.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[CrashReport] No pending reports to send");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CrashReport] Sending {offene.Count} crash report(s)");

            var api = ApiService.Instance;
            var gesendet = new List<CrashReport>();

            foreach (var bericht in offene)
            {
                try
                {
                    if (await api.SendCrashReportAsync(bericht).ConfigureAwait(false))
                    {
                        gesendet.Add(bericht);
                        System.Diagnostics.Debug.WriteLine($"[CrashReport] Sent report from {bericht.Timestamp}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to send report: {ex.Message}");
                }
            }

            _speicher.MarkiereGesendet(gesendet);
            _speicher.RaeumeAuf();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] SendPendingReportsAsync error: {ex.Message}");
        }
        finally
        {
            _sendeSperre.Release();
        }
    }

    private void BeiUnbehandelterAusnahme(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            SaveCrashReport(ex, "AppDomain.UnhandledException");
    }

    private void BeiUnbeobachteterTaskAusnahme(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        SaveCrashReport(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved(); // App nicht beenden lassen
    }

    private static string ErmittleGeraet()
    {
        try
        {
            return $"{DeviceInfo.Platform} {DeviceInfo.VersionString}, {DeviceInfo.Manufacturer} {DeviceInfo.Model}";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string ErmittleVersion()
    {
        try
        {
            return $"{AppInfo.VersionString} ({AppInfo.BuildString})";
        }
        catch
        {
            return "Unknown";
        }
    }
}
