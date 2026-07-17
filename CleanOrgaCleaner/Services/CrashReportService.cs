using System.Text.Json;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Service for capturing, storing and sending crash reports
/// </summary>
public class CrashReportService
{
    private static CrashReportService? _instance;
    public static CrashReportService Instance => _instance ??= new CrashReportService();

    private readonly string _crashReportPath;
    private readonly string _crashReportFile;

    // Serialisiert Sende-Läufe: Startup-Send und Sofort-Send nach SaveCrashReport
    // können sonst parallel dieselben ungesendeten Reports laden und doppelt posten.
    private readonly SemaphoreSlim _sendeSperre = new(1, 1);

    // Schützt Lese-/Schreibzugriffe auf crash_reports.json.
    private readonly object _dateiSperre = new();

    private CrashReportService()
    {
        _crashReportPath = FileSystem.AppDataDirectory;
        _crashReportFile = Path.Combine(_crashReportPath, "crash_reports.json");
    }

    /// <summary>
    /// Initialize crash handlers - call this in App.xaml.cs
    /// </summary>
    public void Initialize()
    {
        // Catch unhandled exceptions in the app domain
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Catch unhandled exceptions in tasks
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        System.Diagnostics.Debug.WriteLine("[CrashReport] Crash handlers initialized");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            SaveCrashReport(ex, "AppDomain.UnhandledException");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        SaveCrashReport(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved(); // Prevent app termination
    }

    /// <summary>
    /// Save a crash report to local storage
    /// </summary>
    public void SaveCrashReport(Exception ex, string source)
    {
        try
        {
            var report = new CrashReport
            {
                Timestamp = DateTime.UtcNow,
                Source = source,
                ExceptionType = ex.GetType().FullName ?? "Unknown",
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? "",
                InnerException = ex.InnerException?.Message,
                DeviceInfo = GetDeviceInfo(),
                AppVersion = GetAppVersion()
            };

            lock (_dateiSperre)
            {
                var reports = LoadCrashReports();
                reports.Add(report);

                // Keep only last 10 reports
                if (reports.Count > 10)
                {
                    reports = reports.Skip(reports.Count - 10).ToList();
                }

                var json = JsonSerializer.Serialize(reports, Json.AppJsonContext.Default.ListCrashReport);
                File.WriteAllText(_crashReportFile, json);
            }

            System.Diagnostics.Debug.WriteLine($"[CrashReport] Saved crash report: {ex.Message}");

            // Sofort-Sendeversuch: Bei UnobservedTaskException laeuft die App
            // weiter (SetObserved), dann kommt der Report noch in dieser Session
            // durch. Bei einem toedlichen Crash schlaegt es fehl - dann greift
            // der Startup-Send beim naechsten App-Start (siehe App-Konstruktor).
            TrySendPendingReportsInBackground();
        }
        catch (Exception saveEx)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to save crash report: {saveEx.Message}");
        }
    }

    /// <summary>
    /// Load existing crash reports from local storage
    /// </summary>
    public List<CrashReport> LoadCrashReports()
    {
        try
        {
            if (File.Exists(_crashReportFile))
            {
                var json = File.ReadAllText(_crashReportFile);
                return JsonSerializer.Deserialize(json, Json.AppJsonContext.Default.ListCrashReport) ?? new List<CrashReport>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to load crash reports: {ex.Message}");
        }
        return new List<CrashReport>();
    }

    /// <summary>
    /// Fire-and-forget-Hintergrund-Send - der EINE Weg, den alle Aufrufstellen
    /// (App-Start, nach SaveCrashReport, LoginPage) nutzen sollen.
    /// </summary>
    public void TrySendPendingReportsInBackground()
    {
        _ = Task.Run(async () =>
        {
            try { await SendPendingReportsAsync(); }
            catch { }
        });
    }

    /// <summary>
    /// Send pending crash reports to server
    /// </summary>
    public async Task SendPendingReportsAsync()
    {
        // Single-Flight: parallele Läufe (App-Start + Sofort-Send) würden
        // dieselben ungesendeten Reports laden und doppelt posten.
        await _sendeSperre.WaitAsync().ConfigureAwait(false);
        try
        {
            List<CrashReport> pendingReports;
            lock (_dateiSperre)
            {
                pendingReports = LoadCrashReports().Where(r => !r.Sent).ToList();
            }

            if (pendingReports.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[CrashReport] No pending reports to send");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CrashReport] Sending {pendingReports.Count} crash report(s)");

            var apiService = ApiService.Instance;
            var gesendet = new List<CrashReport>();

            foreach (var report in pendingReports)
            {
                try
                {
                    var success = await apiService.SendCrashReportAsync(report);
                    if (success)
                    {
                        gesendet.Add(report);
                        System.Diagnostics.Debug.WriteLine($"[CrashReport] Sent report from {report.Timestamp}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to send report: {ex.Message}");
                }
            }

            // Sent-Flags in der AKTUELLEN Datei setzen (nicht die alte Liste
            // zurückschreiben - während des Sendens kann SaveCrashReport neue
            // Reports angehängt haben, die sonst verloren gingen).
            lock (_dateiSperre)
            {
                var aktuell = LoadCrashReports();
                foreach (var r in aktuell)
                {
                    if (!r.Sent && gesendet.Any(g =>
                            g.Timestamp == r.Timestamp &&
                            g.ExceptionType == r.ExceptionType &&
                            g.Message == r.Message))
                    {
                        r.Sent = true;
                    }
                }
                var json = JsonSerializer.Serialize(aktuell, Json.AppJsonContext.Default.ListCrashReport);
                File.WriteAllText(_crashReportFile, json);
            }

            // Clean up old sent reports (keep only last 5 sent ones)
            CleanupOldReports();
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

    /// <summary>
    /// Clean up old sent reports
    /// </summary>
    private void CleanupOldReports()
    {
        try
        {
            lock (_dateiSperre)
            {
                var reports = LoadCrashReports();
                var sentReports = reports.Where(r => r.Sent).OrderByDescending(r => r.Timestamp).Take(5);
                var unsentReports = reports.Where(r => !r.Sent);
                var keepReports = unsentReports.Concat(sentReports).ToList();

                if (keepReports.Count < reports.Count)
                {
                    var json = JsonSerializer.Serialize(keepReports, Json.AppJsonContext.Default.ListCrashReport);
                    File.WriteAllText(_crashReportFile, json);
                }
            }
        }
        catch { }
    }

    private string GetDeviceInfo()
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

    private string GetAppVersion()
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

/// <summary>
/// Crash report data model
/// </summary>
public class CrashReport
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string Message { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public string? InnerException { get; set; }
    public string DeviceInfo { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public bool Sent { get; set; } = false;
}
