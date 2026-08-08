namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Lädt Bilder mit den Sitzungs-Cookies (geschützte Medien-Adressen) und legt
/// sie zusätzlich im Offline-Zwischenspeicher ab.
///
/// iOS-Besonderheit: ein MemoryStream als ImageSource führt dort zu leeren
/// Bildern - deshalb wird immer über eine Datei gegangen.
/// </summary>
public sealed class BildApi
{
    private readonly ApiHttpKern _http;
    private static bool _tempAufgeraeumt;

    public BildApi(ApiHttpKern http) => _http = http;

    /// <summary>Bild laden; bei Netzproblemen aus dem Zwischenspeicher.</summary>
    public async Task<ImageSource?> HoleBildAsync(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Ursprüngliche (relative) Adresse ist der Schlüssel des Zwischenspeichers
            var schluessel = url;
            var volleUrl = UrlHelfer.Absolut(url);

            try
            {
                var bytes = await _http.HoleBytesAsync(volleUrl).ConfigureAwait(false);
                if (bytes == null)
                    return null;

                // Für den Offline-Betrieb ablegen (nebenläufig)
                _ = OfflineDataService.Instance.CacheImageAsync(schluessel, bytes);

                RaeumeTempAuf();
                var tempDatei = Path.Combine(FileSystem.CacheDirectory, $"img_{Guid.NewGuid():N}.jpg");
                await File.WriteAllBytesAsync(tempDatei, bytes).ConfigureAwait(false);
                return ImageSource.FromFile(tempDatei);
            }
            catch (Exception netzFehler)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Bild-Netzfehler: {netzFehler.Message}");

                var ausSpeicher = OfflineDataService.Instance.GetCachedImagePath(schluessel);
                if (ausSpeicher != null)
                    return ImageSource.FromFile(ausSpeicher);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] HoleBild Fehler: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Einmal je App-Lauf die Zwischendateien früherer Läufe entfernen. Für
    /// jedes angezeigte Bild entsteht eine eigene img_*.jpg - ohne Aufräumen
    /// wächst das Cache-Verzeichnis unbegrenzt.
    /// </summary>
    private static void RaeumeTempAuf()
    {
        if (_tempAufgeraeumt)
            return;
        _tempAufgeraeumt = true;

        _ = Task.Run(() =>
        {
            try
            {
                var grenze = DateTime.UtcNow.AddDays(-1);
                foreach (var datei in Directory.GetFiles(FileSystem.CacheDirectory, "img_*.jpg"))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(datei) < grenze)
                            File.Delete(datei);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] Temp-Aufräumen fehlgeschlagen: {ex.Message}");
            }
        });
    }
}
