namespace CleanOrgaCleaner.Helpers;

/// <summary>
/// Verkleinert und komprimiert Fotos vor dem Upload. Die plattformabhängigen
/// Teile stehen in <c>ImageHelperAndroid.cs</c> bzw. <c>ImageHelperIos.cs</c>
/// (partielle Klasse), damit keine Datei plattformfremden Code mitschleppt.
/// </summary>
public static partial class ImageHelper
{
    /// <summary>Längste Kante nach dem Verkleinern (Pixel).</summary>
    private const int MaxKantenlaenge = 2000;

    /// <summary>JPEG-Qualität 0..1.</summary>
    private const float JpegQualitaet = 0.8f;

    /// <summary>
    /// Bild auf maximal 2000 Pixel Kantenlänge verkleinern und als JPEG
    /// komprimieren. Schlägt etwas fehl, kommt das Original zurück - ein Upload
    /// ist wichtiger als die Ersparnis.
    /// </summary>
    public static async Task<byte[]> CompressImageAsync(byte[] imageBytes)
    {
        try
        {
            using var eingabe = new MemoryStream(imageBytes);

#if IOS || MACCATALYST
            return await KomprimiereIosAsync(eingabe).ConfigureAwait(false);
#elif ANDROID
            return await KomprimiereAndroidAsync(eingabe).ConfigureAwait(false);
#else
            // Windows: keine Plattform-Bildbibliothek eingebunden
            return imageBytes;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageHelper] Komprimieren fehlgeschlagen: {ex.Message}");
            return imageBytes;
        }
    }

    /// <summary>
    /// Zielmaße unter Beibehaltung des Seitenverhältnisses. Kleine Bilder
    /// bleiben unverändert.
    /// </summary>
    private static (int Breite, int Hoehe) NeueMasse(int breite, int hoehe)
    {
        if (breite <= MaxKantenlaenge && hoehe <= MaxKantenlaenge)
            return (breite, hoehe);

        double faktor = breite > hoehe
            ? (double)MaxKantenlaenge / breite
            : (double)MaxKantenlaenge / hoehe;

        return ((int)(breite * faktor), (int)(hoehe * faktor));
    }
}
