using System.Security.Cryptography;
using System.Text;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Bilder-Zwischenspeicher auf der Platte.
///
/// Der Dateiname entsteht aus einem SHA256-Kürzel der Adresse: der frühere
/// url.GetHashCode() ist je Prozessstart zufällig - der Speicher wurde nach
/// jedem App-Neustart nie mehr getroffen und wuchs unbegrenzt.
/// </summary>
public sealed class BildCache
{
    /// <summary>Höchstalter einer Datei, bevor sie aufgeräumt wird.</summary>
    private static readonly TimeSpan _hoechstalter = TimeSpan.FromDays(14);

    private readonly string _verzeichnis;
    private bool _aufgeraeumt;

    public BildCache(string verzeichnis) => _verzeichnis = verzeichnis;

    /// <summary>Bild ablegen; gibt den Dateipfad zurück (null bei Fehler).</summary>
    public async Task<string?> SpeichereAsync(string url, byte[] bytes)
    {
        try
        {
            if (string.IsNullOrEmpty(url) || bytes == null || bytes.Length == 0)
                return null;

            RaeumeAuf();

            if (!Directory.Exists(_verzeichnis))
                Directory.CreateDirectory(_verzeichnis);

            var pfad = PfadFuer(url);
            await File.WriteAllBytesAsync(pfad, bytes).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Cached image: {url} -> {pfad}");
            return pfad;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OfflineData] Cache image error: {ex.Message}");
            return null;
        }
    }

    /// <summary>Pfad eines abgelegten Bildes (null, wenn nicht vorhanden).</summary>
    public string? PfadWennVorhanden(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        var pfad = PfadFuer(url);
        return File.Exists(pfad) ? pfad : null;
    }

    /// <summary>Dateiname aus stabilem Kürzel der Adresse.</summary>
    private string PfadFuer(string url)
    {
        var kuerzel = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)), 0, 8);
        var endung = Path.GetExtension(url);
        if (string.IsNullOrEmpty(endung) || endung.Length > 5)
            endung = ".jpg";
        return Path.Combine(_verzeichnis, $"img_{kuerzel}{endung}");
    }

    /// <summary>
    /// Alte Dateien entfernen (einmal je App-Lauf) - räumt auch die verwaisten
    /// Dateien der früheren Namensgebung ab, die nie wieder getroffen werden.
    /// </summary>
    private void RaeumeAuf()
    {
        if (_aufgeraeumt)
            return;
        _aufgeraeumt = true;

        _ = Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(_verzeichnis))
                    return;

                var grenze = DateTime.UtcNow - _hoechstalter;
                foreach (var datei in Directory.GetFiles(_verzeichnis, "img_*"))
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
                System.Diagnostics.Debug.WriteLine($"[OfflineData] Cache cleanup error: {ex.Message}");
            }
        });
    }
}
