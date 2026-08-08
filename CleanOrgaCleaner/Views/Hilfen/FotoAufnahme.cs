using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Fotos aufnehmen, auswählen und markieren.
///
/// Vorher stand derselbe Ablauf (Berechtigung prüfen, aufnehmen, Bytes lesen,
/// Markierseite öffnen und auf ihr Ende warten) in fünf Fassungen in
/// AufgabePage, AuftragPage und ChatCurrentPage. Diese Klasse ist die einzige
/// Stelle dafür; die Seiten entscheiden nur noch, was mit den Bytes passiert.
/// </summary>
public sealed class FotoAufnahme
{
    private readonly Page _seite;

    public FotoAufnahme(Page seite) => _seite = seite;

    /// <summary>
    /// Foto mit der Kamera aufnehmen. Liefert null, wenn abgebrochen wurde
    /// oder keine Kamera/Berechtigung vorhanden ist (Hinweis wurde dann
    /// bereits angezeigt).
    /// </summary>
    public async Task<byte[]?> KameraAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await HinweisAsync("Kamera nicht verfügbar");
                return null;
            }

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await BerechtigungFehltAsync("Kamera");
                    return null;
                }
            }

            var foto = await MediaPicker.Default.CapturePhotoAsync();
            return foto == null ? null : await BytesLesenAsync(foto);
        }
        catch (FeatureNotSupportedException)
        {
            await HinweisAsync("Kamera wird auf diesem Gerät nicht unterstützt");
            return null;
        }
        catch (PermissionException)
        {
            await BerechtigungFehltAsync("Kamera");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FotoAufnahme] Kamera: {ex.Message}");
            await HinweisAsync(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Foto über die Dateiauswahl wählen. Schlägt das fehl, wird ein Hinweis
    /// angezeigt und null geliefert.
    /// </summary>
    public async Task<byte[]?> GalerieAsync()
    {
        try
        {
            var auswahl = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Foto auswählen",
                FileTypes = FilePickerFileType.Images
            });
            return auswahl == null ? null : await BytesLesenAsync(auswahl);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FotoAufnahme] Galerie: {ex.Message}");
            await HinweisAsync("Galerie konnte nicht geöffnet werden");
            return null;
        }
    }

    /// <summary>
    /// Foto über den System-Fotowähler wählen. Der braucht spürbar weniger
    /// Speicher als der Dateimanager - auf knappen Geräten wurde die App sonst
    /// im Hintergrund beendet, während die Auswahl offen war. Abbruch und
    /// fehlende Auswahl-App laufen still ins Leere, damit kein Dialog offen
    /// hängen bleibt.
    /// </summary>
    public async Task<byte[]?> FotowaehlerAsync(string titel)
    {
        try
        {
            // PickPhotoAsync gilt als veraltet zugunsten der Mehrfachauswahl -
            // hier ist genau EIN Foto gewollt, deshalb bleibt es dabei.
#pragma warning disable CS0618
            var auswahl = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = titel });
#pragma warning restore CS0618
            return auswahl == null ? null : await BytesLesenAsync(auswahl);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FotoAufnahme] Auswahl abgebrochen: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Markierseite öffnen und auf ihr Ende warten. Liefert das markierte Bild
    /// oder - wenn nicht gespeichert bzw. die Seite nicht zu öffnen war - das
    /// unveränderte Original.
    /// </summary>
    public async Task<byte[]> MarkierenAsync(byte[] bytes)
    {
        // Das Disappearing MUSS vor dem Öffnen hängen, sonst kann es verpasst
        // werden und die Seite wartet endlos.
        var markierSeite = new ImageAnnotationPage(bytes);
        var fertig = new TaskCompletionSource<bool>();
        markierSeite.Disappearing += (s, e) => fertig.TrySetResult(true);

        try
        {
            await _seite.Navigation.PushModalAsync(markierSeite);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FotoAufnahme] Bildeditor ließ sich nicht öffnen: {ex.Message}");
            return bytes;
        }

        await fertig.Task;

        return markierSeite.WasSaved && markierSeite.AnnotatedImageBytes != null
            ? markierSeite.AnnotatedImageBytes
            : bytes;
    }

    /// <summary>
    /// Hinweis auf eine verweigerte Berechtigung mit Sprung in die
    /// App-Einstellungen.
    /// </summary>
    public async Task BerechtigungFehltAsync(string berechtigung)
    {
        try
        {
            var oeffnen = await _seite.DisplayAlertAsync(
                $"{berechtigung}-Berechtigung",
                $"Die {berechtigung}-Berechtigung wurde verweigert.\n\nBitte öffne die App-Einstellungen und aktiviere die Berechtigung unter 'Berechtigungen'.",
                "Einstellungen öffnen",
                "Abbrechen");
            if (oeffnen)
                PermissionHelper.OpenAppSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FotoAufnahme] Berechtigungshinweis: {ex.Message}");
        }
    }

    private async Task HinweisAsync(string text)
    {
        try { await _seite.DisplayAlertAsync(Translations.Get("error"), text, Translations.Get("ok")); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[FotoAufnahme] Hinweis: {ex.Message}"); }
    }

    private static async Task<byte[]> BytesLesenAsync(FileResult datei)
    {
        using var strom = await datei.OpenReadAsync();
        using var speicher = new MemoryStream();
        await strom.CopyToAsync(speicher);
        return speicher.ToArray();
    }
}
