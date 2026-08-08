using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Views.Hilfen;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Fotos zur Aufgabenbeschreibung (Typ 'aufgabe') im Reiter "Details".
///
/// Im Dialog "Neue Aufgabe" darf die Arbeitskraft Fotos hinzufügen, markieren
/// und wieder löschen. Fotos einer ihr zugewiesenen Aufgabe sind dagegen eine
/// Anweisung des Büros und werden nur angezeigt - siehe <see cref="AufgabeFotosNurAnsehen"/>.
/// </summary>
public partial class AuftragPage
{
    /// <summary>Neue, noch nicht hochgeladene Fotos.</summary>
    private readonly List<byte[]> _neueAufgabeFotos = new();

    /// <summary>Bereits auf dem Server liegende Fotos.</summary>
    private readonly List<ServerFoto> _vorhandeneAufgabeFotos = new();

    /// <summary>Nur-Ansicht: kein Hinzufügen, kein Löschen.</summary>
    private bool AufgabeFotosNurAnsehen { get; set; }

    private const int MaxAufgabeFotos = 10;

    // ===== Zurücksetzen und Laden =====

    private void AufgabeFotosZuruecksetzen(bool nurAnsehen = false)
    {
        _neueAufgabeFotos.Clear();
        _vorhandeneAufgabeFotos.Clear();
        AufgabeFotosNurAnsehen = nurAnsehen;
        AufgabeFotosAnzeigen();
    }

    private async Task AufgabeFotosLadenAsync(int taskId)
    {
        _vorhandeneAufgabeFotos.Clear();
        try
        {
            var eintraege = await _apiService.GetTaskItemsAsync(taskId, "aufgabe");
            foreach (var eintrag in eintraege)
            {
                if (eintrag.Photos == null) continue;
                foreach (var foto in eintrag.Photos)
                {
                    if (!string.IsNullOrEmpty(foto.Url))
                        _vorhandeneAufgabeFotos.Add(new ServerFoto(foto.Id, foto.Url));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Laden fehlgeschlagen: {ex.Message}");
        }
        AufgabeFotosAnzeigen();
    }

    // ===== Foto aufnehmen und markieren =====

    private async void OnAddAufgabeFotoClicked(object sender, EventArgs e)
    {
        if (AufgabeFotosNurAnsehen) return;

        var t = Translations.Get;

        if (_neueAufgabeFotos.Count + _vorhandeneAufgabeFotos.Count >= MaxAufgabeFotos)
        {
            await DisplayAlertAsync(t("error"),
                $"Mehr als {MaxAufgabeFotos} Fotos sind nicht vorgesehen.", t("ok"));
            return;
        }

        try
        {
            // Die Kamera nur anbieten, wenn das Gerät eine hat - sonst läuft der
            // Aufruf ins Leere und der Dialog lässt sich nicht mehr schließen
            // (Emulator ohne Kamera).
            var kameraMoeglich = MediaPicker.Default.IsCaptureSupported;
            var moeglichkeiten = kameraMoeglich
                ? new[] { t("camera"), t("gallery") }
                : new[] { t("gallery") };

            var quelle = await DisplayActionSheetAsync(t("add_photo"), t("cancel"), null, moeglichkeiten);

            // Abbrechen liefert je nach Plattform den Abbrechen-Text oder null
            if (string.IsNullOrEmpty(quelle) || quelle == t("cancel"))
                return;

            var bytes = quelle == t("camera")
                ? await _fotoAufnahme.KameraAsync()
                : await _fotoAufnahme.FotowaehlerAsync(t("add_photo"));

            if (bytes == null || bytes.Length == 0) return;

            // Markieren: dieselbe Seite wie bei Problemen und Notizen
            var markiert = await _fotoAufnahme.MarkierenAsync(bytes);

            _neueAufgabeFotos.Add(await ImageHelper.CompressImageAsync(markiert));
            AufgabeFotosAnzeigen();
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Fehler: {ex.Message}");
            await DisplayAlertAsync(t("error"), ex.Message, t("ok"));
        }
    }

    // ===== Anzeige =====

    private void AufgabeFotosAnzeigen()
    {
        AufgabeFotosStack.Children.Clear();

        foreach (var foto in _vorhandeneAufgabeFotos)
            AufgabeFotosStack.Children.Add(
                FotoKachel(ImageSource.FromUri(new Uri(foto.Url)), foto.Id, null));

        foreach (var bytes in _neueAufgabeFotos)
        {
            var kopie = bytes;
            AufgabeFotosStack.Children.Add(
                FotoKachel(ImageSource.FromStream(() => new MemoryStream(kopie)), null, kopie));
        }

        AddAufgabeFotoButton.IsVisible = !AufgabeFotosNurAnsehen;
        AufgabeFotosHinweis.IsVisible = AufgabeFotosNurAnsehen
            && (_vorhandeneAufgabeFotos.Count > 0 || _neueAufgabeFotos.Count > 0);
    }

    /// <summary>Eine Kachel; der Löschknopf entfällt in der Nur-Ansicht.</summary>
    private View FotoKachel(ImageSource quelle, int? serverId, byte[]? neuesFoto)
    {
        var kachel = new Grid { WidthRequest = 72, HeightRequest = 72, Margin = new Thickness(0, 0, 8, 8) };

        kachel.Children.Add(new Border
        {
            Content = new Image
            {
                Source = quelle,
                Aspect = Aspect.AspectFill,
                WidthRequest = 72,
                HeightRequest = 72
            },
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            StrokeThickness = 1,
            Padding = 0
        });

        if (AufgabeFotosNurAnsehen) return kachel;

        var loeschen = new Button
        {
            Text = "×",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#A6000000"),
            WidthRequest = 24,
            HeightRequest = 24,
            CornerRadius = 12,
            Padding = 0,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start
        };
        loeschen.Clicked += async (s, e) => await AufgabeFotoEntfernenAsync(serverId, neuesFoto);
        kachel.Children.Add(loeschen);

        return kachel;
    }

    // ===== Entfernen =====

    private async Task AufgabeFotoEntfernenAsync(int? serverId, byte[]? neuesFoto)
    {
        if (AufgabeFotosNurAnsehen) return;

        var t = Translations.Get;
        try
        {
            var sicher = await DisplayAlertAsync(t("delete"), "Dieses Foto entfernen?", t("yes"), t("no"));
            if (!sicher) return;

            if (neuesFoto != null)
            {
                _neueAufgabeFotos.Remove(neuesFoto);
                AufgabeFotosAnzeigen();
                return;
            }

            if (serverId is not int kennung) return;

            var antwort = await _apiService.DeleteImageListPhotoAsync(kennung);
            if (antwort.Success)
            {
                _vorhandeneAufgabeFotos.RemoveAll(f => f.Id == kennung);
                AufgabeFotosAnzeigen();
            }
            else
            {
                await DisplayAlertAsync(t("error"),
                    antwort.Error ?? "Foto konnte nicht entfernt werden.", t("ok"));
            }
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Entfernen: {ex.Message}");
        }
    }

    // ===== Hochladen =====

    /// <summary>
    /// Lädt die neu aufgenommenen Fotos zur Aufgabe hoch. Wird nach dem
    /// Speichern aufgerufen, wenn die Aufgabe ihre Id hat.
    /// </summary>
    private async Task AufgabeFotosHochladenAsync(int taskId)
    {
        if (_neueAufgabeFotos.Count == 0) return;

        var fotos = _neueAufgabeFotos
            .Select((bytes, i) => new NeuesFoto($"aufgabe_foto_{i + 1}.jpg", bytes))
            .ToList();

        try
        {
            var antwort = await _apiService.CreateImageListItemAsync(
                taskId, "aufgabe", "Aufgabenfotos", null, NeuesFoto.FuerUebertragung(fotos));

            if (antwort?.Success == true)
                _neueAufgabeFotos.Clear();
            else
                System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Upload abgelehnt: {antwort?.Error}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Upload fehlgeschlagen: {ex.Message}");
        }
    }
}
