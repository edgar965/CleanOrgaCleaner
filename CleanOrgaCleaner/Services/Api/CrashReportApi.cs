namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Überträgt Absturzberichte und Diagnosezeilen an den Server.
/// Der Endpunkt /api/crash-report/ ist bewusst auch ohne Anmeldung erreichbar -
/// nur so kommen Startabstürze überhaupt an.
/// </summary>
public sealed class CrashReportApi
{
    private readonly ApiHttpKern _http;
    private readonly Sitzung _sitzung;

    public CrashReportApi(ApiHttpKern http, Sitzung sitzung)
    {
        _http = http;
        _sitzung = sitzung;
    }

    /// <summary>Einen Bericht senden. cleanerName überschreibt den Absender.</summary>
    public async Task<bool> SendeAsync(CrashReport bericht, string? cleanerName = null)
    {
        try
        {
            var felder = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("timestamp", bericht.Timestamp.ToString("o")),
                new KeyValuePair<string, string>("source", bericht.Source),
                new KeyValuePair<string, string>("exception_type", bericht.ExceptionType),
                new KeyValuePair<string, string>("message", bericht.Message),
                new KeyValuePair<string, string>("stack_trace", bericht.StackTrace),
                new KeyValuePair<string, string>("inner_exception", bericht.InnerException ?? ""),
                new KeyValuePair<string, string>("device_info", bericht.DeviceInfo),
                new KeyValuePair<string, string>("app_version", bericht.AppVersion),
                // Vor dem Login ist der Name noch leer (Start-Versand) - dann den
                // zuletzt angemeldeten Benutzer nehmen, damit der Bericht
                // zuordenbar bleibt. Diagnosen übergeben ausdrücklich "diag".
                new KeyValuePair<string, string>("cleaner_name", cleanerName ?? ErmittleName()),
            });

            var antwort = await _http.SendeAsync("/api/crash-report/", felder).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Send response: {antwort.StatusCode}");
            return antwort.Erfolgreich;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CrashReport] Send error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Diagnosezeile an den Server schicken - damit lässt sich ohne Gerät
    /// nachvollziehen, ob z.B. die Firebase-Initialisierung durchläuft.
    /// Nutzt denselben Sendeweg wie echte Berichte, damit Feldnamen und
    /// Formate nicht auseinanderlaufen.
    /// </summary>
    public Task<bool> SendeDiagnoseAsync(string kennung, string meldung)
        => SendeAsync(new CrashReport
        {
            Timestamp = DateTime.UtcNow,
            Source = kennung,
            ExceptionType = "Diagnose",
            Message = meldung,
            DeviceInfo = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}",
            AppVersion = $"{AppInfo.VersionString} ({AppInfo.BuildString})",
        }, cleanerName: "diag");

    /// <summary>Absendername: Sitzung, sonst zuletzt gespeicherter Benutzer.</summary>
    private string ErmittleName()
    {
        if (!string.IsNullOrEmpty(_sitzung.Name))
            return _sitzung.Name!;
        try
        {
            // Zentraler Benutzernamen-Zugriff (Main.UserName = Preferences "username")
            if (!string.IsNullOrEmpty(Main.UserName))
                return Main.UserName!;
        }
        catch { }
        return "Unknown";
    }
}
