namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Ermittelt den Inhaltstyp anhand der Dateiendung (Chat-Anhänge).
/// </summary>
public static class MimeTyp
{
    /// <summary>Inhaltstyp zu einem Dateinamen; unbekannt -> octet-stream.</summary>
    public static string Fuer(string dateiname)
        => Path.GetExtension(dateiname).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
}
