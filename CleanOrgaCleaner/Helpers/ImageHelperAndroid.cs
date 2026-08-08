namespace CleanOrgaCleaner.Helpers;

/// <summary>
/// Android-Teil von <see cref="ImageHelper"/>: Bitmap skalieren und als JPEG
/// komprimieren. Der gesamte Inhalt entfällt auf anderen Plattformen.
/// </summary>
public static partial class ImageHelper
{
#if ANDROID
    private static Task<byte[]> KomprimiereAndroidAsync(Stream eingabe)
    {
        return Task.Run(() =>
        {
            using var bitmap = Android.Graphics.BitmapFactory.DecodeStream(eingabe);
            if (bitmap == null)
                throw new InvalidOperationException("Bild konnte nicht geladen werden");

            var breite = bitmap.Width;
            var hoehe = bitmap.Height;
            var (neueBreite, neueHoehe) = NeueMasse(breite, hoehe);

            var skaliert = neueBreite != breite || neueHoehe != hoehe
                ? Android.Graphics.Bitmap.CreateScaledBitmap(bitmap, neueBreite, neueHoehe, true)
                : bitmap;

            using var ausgabe = new MemoryStream();
            skaliert.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!,
                              (int)(JpegQualitaet * 100), ausgabe);

            var bytes = ausgabe.ToArray();

            System.Diagnostics.Debug.WriteLine(
                $"[ImageHelper] Android: {breite}x{hoehe} -> {neueBreite}x{neueHoehe}, {bytes.Length / 1024}KB");

            if (!ReferenceEquals(skaliert, bitmap))
                skaliert.Dispose();

            return bytes;
        });
    }
#endif
}
