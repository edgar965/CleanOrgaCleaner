using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Fotos zur Aufgabenbeschreibung (Typ 'aufgabe') im Tab "Details".
///
/// Im Dialog "Neue Aufgabe" darf die Arbeitskraft Fotos hinzufuegen, markieren
/// und wieder loeschen. Fotos einer ihr zugewiesenen Aufgabe sind dagegen eine
/// Anweisung des Bueros und werden nur angezeigt - siehe <see cref="AufgabeFotosNurAnsehen"/>.
/// </summary>
public partial class AuftragPage
{
    /// <summary>Neue, noch nicht hochgeladene Fotos.</summary>
    private readonly List<byte[]> _neueAufgabeFotos = new();

    /// <summary>Bereits auf dem Server liegende Fotos (Id + Adresse).</summary>
    private readonly List<(int Id, string Url)> _vorhandeneAufgabeFotos = new();

    /// <summary>Nur-Ansicht: kein Hinzufuegen, kein Loeschen.</summary>
    private bool AufgabeFotosNurAnsehen { get; set; }

    private const int MaxAufgabeFotos = 10;

    // ===== Zuruecksetzen und Laden =====

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
                        _vorhandeneAufgabeFotos.Add((foto.Id, foto.Url));
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

        if (_neueAufgabeFotos.Count + _vorhandeneAufgabeFotos.Count >= MaxAufgabeFotos)
        {
            await DisplayAlert(Translations.Get("error"),
                $"Mehr als {MaxAufgabeFotos} Fotos sind nicht vorgesehen.",
                Translations.Get("ok"));
            return;
        }

        try
        {
            // Die Kamera nur anbieten, wenn das Geraet eine hat - sonst laeuft
            // der Aufruf ins Leere und der Dialog laesst sich nicht mehr
            // schliessen (Emulator ohne Kamera).
            var kameraMoeglich = MediaPicker.Default.IsCaptureSupported;

            var auswahlmoeglichkeiten = kameraMoeglich
                ? new[] { Translations.Get("camera"), Translations.Get("gallery") }
                : new[] { Translations.Get("gallery") };

            var quelle = await DisplayActionSheet(
                Translations.Get("add_photo"),
                Translations.Get("cancel"),
                null,
                auswahlmoeglichkeiten);

            // Abbrechen liefert je nach Plattform den Abbrechen-Text oder null
            if (string.IsNullOrEmpty(quelle) || quelle == Translations.Get("cancel"))
                return;

            byte[]? bytes = null;

            if (quelle == Translations.Get("camera"))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        await OfferOpenSettingsAsync("Kamera");
                        return;
                    }
                }

                try
                {
                    var foto = await MediaPicker.Default.CapturePhotoAsync();
                    if (foto != null)
                    {
                        using var stream = await foto.OpenReadAsync();
                        using var speicher = new MemoryStream();
                        await stream.CopyToAsync(speicher);
                        bytes = speicher.ToArray();
                    }
                }
                catch (FeatureNotSupportedException)
                {
                    await DisplayAlert(Translations.Get("error"),
                        "Kamera nicht verfügbar", Translations.Get("ok"));
                    return;
                }
            }
            else
            {
                try
                {
                    // MediaPicker statt FilePicker: oeffnet den schlanken
                    // System-Fotowaehler statt des Dateimanagers. Der braucht
                    // spuerbar weniger Speicher - auf knappen Geraeten wurde die
                    // App sonst im Hintergrund beendet, waehrend der Waehler offen war.
                    var auswahl = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                    {
                        Title = Translations.Get("add_photo")
                    });
                    if (auswahl != null)
                    {
                        using var stream = await auswahl.OpenReadAsync();
                        using var speicher = new MemoryStream();
                        await stream.CopyToAsync(speicher);
                        bytes = speicher.ToArray();
                    }
                }
                catch (Exception auswahlFehler)
                {
                    // Abbruch oder gar keine App zum Auswaehlen: still zurueck,
                    // damit der Dialog nicht offen haengen bleibt.
                    System.Diagnostics.Debug.WriteLine(
                        $"[AufgabeFotos] Auswahl abgebrochen: {auswahlFehler.Message}");
                    return;
                }
            }

            // Nichts ausgewaehlt (abgebrochen) - einfach zurueck
            if (bytes == null || bytes.Length == 0) return;

            // Markieren: dieselbe Seite wie bei Problemen und Notizen.
            // Das Disappearing MUSS vor dem Oeffnen haengen - sonst kann es
            // verpasst werden und die Seite wartet endlos.
            var markierSeite = new ImageAnnotationPage(bytes);
            var fertig = new TaskCompletionSource<bool>();
            markierSeite.Disappearing += (s, ev) => fertig.TrySetResult(true);

            try
            {
                await Navigation.PushModalAsync(markierSeite);
            }
            catch (Exception oeffnenFehler)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AufgabeFotos] Bildeditor liess sich nicht oeffnen: {oeffnenFehler.Message}");
                return;
            }

            await fertig.Task;

            var ergebnis = markierSeite.WasSaved && markierSeite.AnnotatedImageBytes != null
                ? markierSeite.AnnotatedImageBytes
                : bytes;

            _neueAufgabeFotos.Add(await ImageHelper.CompressImageAsync(ergebnis));
            AufgabeFotosAnzeigen();
        }
        catch (PermissionException)
        {
            await OfferOpenSettingsAsync("Kamera");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Fehler: {ex.Message}");
            await DisplayAlert(Translations.Get("error"), ex.Message, Translations.Get("ok"));
        }
    }

    // ===== Anzeige =====

    private void AufgabeFotosAnzeigen()
    {
        AufgabeFotosStack.Children.Clear();

        foreach (var (id, url) in _vorhandeneAufgabeFotos)
            AufgabeFotosStack.Children.Add(FotoKachel(ImageSource.FromUri(new Uri(url)), id, null));

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

    /// <summary>Eine Kachel; der Loeschknopf entfaellt in der Nur-Ansicht.</summary>
    private View FotoKachel(ImageSource quelle, int? serverId, byte[]? neuesFoto)
    {
        var bild = new Image
        {
            Source = quelle,
            Aspect = Aspect.AspectFill,
            WidthRequest = 72,
            HeightRequest = 72
        };

        var kachel = new Grid { WidthRequest = 72, HeightRequest = 72, Margin = new Thickness(0, 0, 8, 8) };
        kachel.Children.Add(new Border
        {
            Content = bild,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            StrokeThickness = 1,
            Padding = 0
        });

        if (!AufgabeFotosNurAnsehen)
        {
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
        }

        return kachel;
    }

    // ===== Entfernen =====

    private async Task AufgabeFotoEntfernenAsync(int? serverId, byte[]? neuesFoto)
    {
        if (AufgabeFotosNurAnsehen) return;

        var sicher = await DisplayAlert(Translations.Get("delete"),
            "Dieses Foto entfernen?", Translations.Get("yes"), Translations.Get("no"));
        if (!sicher) return;

        if (neuesFoto != null)
        {
            _neueAufgabeFotos.Remove(neuesFoto);
            AufgabeFotosAnzeigen();
            return;
        }

        if (serverId is not int id) return;

        var antwort = await _apiService.DeleteImageListPhotoAsync(id);
        if (antwort.Success)
        {
            _vorhandeneAufgabeFotos.RemoveAll(f => f.Id == id);
            AufgabeFotosAnzeigen();
        }
        else
        {
            await DisplayAlert(Translations.Get("error"),
                antwort.Error ?? "Foto konnte nicht entfernt werden.",
                Translations.Get("ok"));
        }
    }

    // ===== Hochladen =====

    /// <summary>
    /// Laedt die neu aufgenommenen Fotos zur Aufgabe hoch. Wird nach dem
    /// Speichern aufgerufen, wenn die Aufgabe ihre Id hat.
    /// </summary>
    private async Task AufgabeFotosHochladenAsync(int taskId)
    {
        if (_neueAufgabeFotos.Count == 0) return;

        var fotos = _neueAufgabeFotos
            .Select((bytes, i) => ($"aufgabe_foto_{i + 1}.jpg", bytes))
            .ToList();

        try
        {
            var antwort = await _apiService.CreateImageListItemAsync(
                taskId, "aufgabe", "Aufgabenfotos", null, fotos);

            if (antwort?.Success == true)
            {
                _neueAufgabeFotos.Clear();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AufgabeFotos] Upload abgelehnt: {antwort?.Error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Upload fehlgeschlagen: {ex.Message}");
        }
    }
}
