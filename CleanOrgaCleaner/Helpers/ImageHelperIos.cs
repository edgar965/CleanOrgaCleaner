namespace CleanOrgaCleaner.Helpers;

/// <summary>
/// iOS-/Mac-Catalyst-Teil von <see cref="ImageHelper"/>: UIImage skalieren und
/// als JPEG komprimieren. Der gesamte Inhalt entfällt auf anderen Plattformen.
/// </summary>
public static partial class ImageHelper
{
#if IOS || MACCATALYST
    private static Task<byte[]> KomprimiereIosAsync(Stream eingabe)
    {
        return Task.Run(() =>
        {
            var daten = Foundation.NSData.FromStream(eingabe);
            if (daten == null)
                throw new InvalidOperationException("Bilddaten konnten nicht gelesen werden");

            using var bild = UIKit.UIImage.LoadFromData(daten);
            if (bild == null)
                throw new InvalidOperationException("Bild konnte nicht geladen werden");

            var breite = (int)bild.Size.Width;
            var hoehe = (int)bild.Size.Height;
            var (neueBreite, neueHoehe) = NeueMasse(breite, hoehe);

            UIKit.UIImage skaliert;
            if (neueBreite != breite || neueHoehe != hoehe)
            {
                var groesse = new CoreGraphics.CGSize(neueBreite, neueHoehe);
                var renderer = new UIKit.UIGraphicsImageRenderer(groesse);
                skaliert = renderer.CreateImage(_ =>
                    bild.Draw(new CoreGraphics.CGRect(0, 0, neueBreite, neueHoehe)));
            }
            else
            {
                skaliert = bild;
            }

            var jpeg = skaliert.AsJPEG((nfloat)JpegQualitaet);
            if (jpeg == null)
                throw new InvalidOperationException("Bild konnte nicht komprimiert werden");

            var bytes = new byte[jpeg.Length];
            System.Runtime.InteropServices.Marshal.Copy(jpeg.Bytes, bytes, 0, (int)jpeg.Length);
            jpeg.Dispose();

            // Nur das selbst erzeugte Bild freigeben - das Original raeumt using auf
            if (!ReferenceEquals(skaliert, bild))
                skaliert.Dispose();

            System.Diagnostics.Debug.WriteLine(
                $"[ImageHelper] iOS: {breite}x{hoehe} -> {neueBreite}x{neueHoehe}, {bytes.Length / 1024}KB");

            return bytes;
        });
    }
#endif
}
