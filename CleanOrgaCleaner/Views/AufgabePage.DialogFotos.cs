using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Views.Hilfen;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Fotovorschau im Eingabedialog: bereits gespeicherte Fotos (ansehen,
/// markieren, löschen) und neu aufgenommene (löschen).
/// </summary>
public partial class AufgabePage
{
    private void AktualisiereFotovorschau()
    {
        ImageListDescriptionDialogPhotoPreviewStack.Children.Clear();

        var vorhandene = _dialogEintrag?.Photos;
        int anzahl = (vorhandene?.Count ?? 0) + _neueFotos.Count;

        ImageListDescriptionDialogPhotoPreviewStack.IsVisible = anzahl > 0;
        ImageListDescriptionDialogPhotoCountLabel.IsVisible = anzahl > 0;
        if (anzahl == 0) return;

        ImageListDescriptionDialogPhotoCountLabel.Text = $"{anzahl} Foto(s)";

        if (vorhandene != null)
        {
            foreach (var foto in vorhandene)
                ImageListDescriptionDialogPhotoPreviewStack.Children.Add(GespeichertesFoto(foto));
        }

        foreach (var foto in _neueFotos)
            ImageListDescriptionDialogPhotoPreviewStack.Children.Add(NeuesFotoZeile(foto));
    }

    /// <summary>Grundgerüst einer Vorschauzeile: Bild links, Knöpfe rechts.</summary>
    private static Grid Vorschauzeile() => new()
    {
        ColumnDefinitions = new ColumnDefinitionCollection
        {
            new ColumnDefinition(new GridLength(140)),
            new ColumnDefinition(GridLength.Star)
        },
        ColumnSpacing = 15
    };

    private View GespeichertesFoto(ImageListDescriptionPhoto foto)
    {
        var zeile = Vorschauzeile();

        var rahmen = new Border
        {
            WidthRequest = 140,
            HeightRequest = 140,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#2196F3"),
            StrokeThickness = 2
        };
        var bild = new Image { Aspect = Aspect.AspectFill };
        _bilder.Laden(bild, foto.ThumbnailUrl ?? foto.Url);
        rahmen.Content = bild;

        var tippen = new TapGestureRecognizer();
        tippen.Tapped += async (s, e) => await _bilder.VollbildModalAsync(foto.Url);
        rahmen.GestureRecognizers.Add(tippen);

        zeile.Children.Add(rahmen);
        Grid.SetColumn(rahmen, 0);

        var knoepfe = new VerticalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };

        var bearbeiten = Vorschauknopf("✏ Bearbeiten", "#FF9800");
        bearbeiten.Clicked += async (s, e) => await GespeichertesFotoMarkierenAsync(foto);
        knoepfe.Children.Add(bearbeiten);

        var loeschen = Vorschauknopf("🗑 " + Translations.Get("delete"), "#c62828");
        loeschen.Clicked += async (s, e) => await GespeichertesFotoLoeschenAsync(foto);
        knoepfe.Children.Add(loeschen);

        zeile.Children.Add(knoepfe);
        Grid.SetColumn(knoepfe, 1);

        return zeile;
    }

    private View NeuesFotoZeile(NeuesFoto foto)
    {
        var zeile = Vorschauzeile();

        var rahmen = new Border
        {
            WidthRequest = 140,
            HeightRequest = 140,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Content = new Image
            {
                Source = ImageSource.FromStream(() => new MemoryStream(foto.Daten)),
                Aspect = Aspect.AspectFill
            }
        };
        zeile.Children.Add(rahmen);
        Grid.SetColumn(rahmen, 0);

        var loeschen = Vorschauknopf("🗑 " + Translations.Get("delete"), "#c62828");
        loeschen.VerticalOptions = LayoutOptions.Center;
        loeschen.Clicked += (s, e) =>
        {
            _neueFotos.Remove(foto);
            AktualisiereFotovorschau();
        };
        zeile.Children.Add(loeschen);
        Grid.SetColumn(loeschen, 1);

        return zeile;
    }

    private static Button Vorschauknopf(string text, string farbe) => new()
    {
        Text = text,
        BackgroundColor = Color.FromArgb(farbe),
        TextColor = Colors.White,
        FontSize = 14,
        CornerRadius = 8,
        HeightRequest = 40
    };

    private async Task GespeichertesFotoLoeschenAsync(ImageListDescriptionPhoto foto)
    {
        try
        {
            var sicher = await DisplayAlertAsync("Foto löschen", "Möchten Sie dieses Foto wirklich löschen?", "Ja", "Nein");
            if (!sicher) return;

            var antwort = await _apiService.DeleteImageListPhotoAsync(foto.Id);
            if (antwort.Success)
            {
                _dialogEintrag?.Photos?.Remove(foto);
                AktualisiereFotovorschau();
            }
            else
            {
                await DisplayAlertAsync("Fehler", antwort.Error ?? "Foto konnte nicht gelöscht werden", "OK");
            }
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Foto löschen: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Foto konnte nicht gelöscht werden", "OK");
        }
    }

    /// <summary>
    /// Gespeichertes Foto markieren: das alte wird auf dem Server gelöscht und
    /// die markierte Fassung als neues Foto vorgemerkt.
    /// </summary>
    private async Task GespeichertesFotoMarkierenAsync(ImageListDescriptionPhoto foto)
    {
        if (string.IsNullOrEmpty(foto.Url)) return;

        try
        {
            var bytes = await BildAnzeige.HoleBytesAsync(foto.Url);

            var markiert = await _fotoAufnahme.MarkierenAsync(bytes);
            if (ReferenceEquals(markiert, bytes)) return;   // nicht gespeichert

            var geloescht = await _apiService.DeleteImageListPhotoAsync(foto.Id);
            if (!geloescht.Success)
            {
                await DisplayAlertAsync("Fehler", geloescht.Error ?? "Foto konnte nicht ersetzt werden", "OK");
                return;
            }

            _neueFotos.Add(NeuesFoto.MitZeitstempel("edited", markiert));
            _dialogEintrag?.Photos?.Remove(foto);
            AktualisiereFotovorschau();
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Foto markieren: {ex.Message}");
            await DisplayAlertAsync("Fehler", "Bild konnte nicht bearbeitet werden", "OK");
        }
    }
}
