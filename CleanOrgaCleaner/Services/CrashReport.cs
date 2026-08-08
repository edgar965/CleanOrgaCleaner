namespace CleanOrgaCleaner.Services;

/// <summary>
/// Ein gespeicherter Absturzbericht (oder eine Diagnosezeile).
/// </summary>
public class CrashReport
{
    /// <summary>Zeitpunkt des Ereignisses (UTC).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Woher der Bericht stammt (Handler bzw. Diagnose-Kennung).</summary>
    public string Source { get; set; } = "";

    /// <summary>Typ der Ausnahme.</summary>
    public string ExceptionType { get; set; } = "";

    /// <summary>Meldung der Ausnahme.</summary>
    public string Message { get; set; } = "";

    /// <summary>Aufrufliste.</summary>
    public string StackTrace { get; set; } = "";

    /// <summary>Meldung der inneren Ausnahme, falls vorhanden.</summary>
    public string? InnerException { get; set; }

    /// <summary>Gerät und Betriebssystem.</summary>
    public string DeviceInfo { get; set; } = "";

    /// <summary>App-Version und Build.</summary>
    public string AppVersion { get; set; } = "";

    /// <summary>True, sobald der Bericht beim Server angekommen ist.</summary>
    public bool Sent { get; set; } = false;
}
