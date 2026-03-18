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

            var reports = LoadCrashReports();
            reports.Add(report);

            // Keep only last 10 reports
            if (reports.Count > 10)
            {
                reports = reports.Skip(reports.Count - 10).ToList();
            }

            var json = JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_crashReportFile, json);

            System.Diagnostics.Debug.WriteLine($"[CrashReport] Saved crash report: {ex.Message}");
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
                return JsonSerializer.Deserialize<List<CrashReport>>(json) ?? new List<CrashReport>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to load crash reports: {ex.Message}");
        }
        return new List<CrashReport>();
    }

    /// <summary>
    /// Check if there are pending crash reports
    /// </summary>
    public bool HasPendingReports()
    {
        var reports = LoadCrashReports();
        return reports.Any(r => !r.Sent);
    }

    /// <summary>
    /// Send pending crash reports to server
    /// </summary>
    public async Task SendPendingReportsAsync()
    {
        try
        {
            var reports = LoadCrashReports();
            var pendingReports = reports.Where(r => !r.Sent).ToList();

            if (pendingReports.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[CrashReport] No pending reports to send");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CrashReport] Sending {pendingReports.Count} crash report(s)");

            var apiService = ApiService.Instance;

            foreach (var report in pendingReports)
            {
                try
                {
                    var success = await apiService.SendCrashReportAsync(report);
                    if (success)
                    {
                        report.Sent = true;
                        System.Diagnostics.Debug.WriteLine($"[CrashReport] Sent report from {report.Timestamp}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CrashReport] Failed to send report: {ex.Message}");
                }
            }

            // Save updated reports (with Sent flags)
            var json = JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_crashReportFile, json);

            // Clean up old sent reports (keep only last 5 sent ones)
            CleanupOldReports();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] SendPendingReportsAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Clean up old sent reports
    /// </summary>
    private void CleanupOldReports()
    {
        try
        {
            var reports = LoadCrashReports();
            var sentReports = reports.Where(r => r.Sent).OrderByDescending(r => r.Timestamp).Take(5);
            var unsentReports = reports.Where(r => !r.Sent);
            var keepReports = unsentReports.Concat(sentReports).ToList();

            if (keepReports.Count < reports.Count)
            {
                var json = JsonSerializer.Serialize(keepReports, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_crashReportFile, json);
            }
        }
        catch { }
    }

    /// <summary>
    /// Clear all crash reports
    /// </summary>
    public void ClearAllReports()
    {
        try
        {
            if (File.Exists(_crashReportFile))
            {
                File.Delete(_crashReportFile);
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
