using CleanOrgaCleaner.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Detailfenster eines Checklisten-Punkts: Beschreibung, eigene Anmerkung,
/// Vorgabebilder und eigene Fotos.
/// </summary>
public partial class AufgabePage
{
    private void OeffneChecklistenDetail(PutzlisteEintrag eintrag)
    {
        if (Content is not Grid wurzel) return;

        var ueberlagerung = new Grid { BackgroundColor = Color.FromArgb("#801a3a5c"), ZIndex = 5500 };
        void Schliessen()
        {
            wurzel.Children.Remove(ueberlagerung);
            BuildCheckliste();   // geänderte Anmerkung/Fotos in der Zeile zeigen
        }

        var hintergrund = new BoxView { Color = Colors.Transparent };
        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => Schliessen();
        hintergrund.GestureRecognizers.Add(tippen);
        ueberlagerung.Children.Add(hintergrund);

        var karte = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Margin = new Thickness(16),
            VerticalOptions = LayoutOptions.Center,
            MaximumHeightRequest = 640
        };

        var aufbau = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        aufbau.Add(Kopfzeile(eintrag.Name, Schliessen), 0, 0);
        aufbau.Add(new ScrollView { Content = Detailinhalt(eintrag) }, 0, 1);

        karte.Content = aufbau;
        ueberlagerung.Children.Add(karte);

        Grid.SetRowSpan(ueberlagerung, Math.Max(1, wurzel.RowDefinitions.Count));
        wurzel.Children.Add(ueberlagerung);
    }

    private static View Kopfzeile(string titel, Action schliessen)
    {
        var kopf = new Grid { Padding = new Thickness(20, 16), BackgroundColor = Color.FromArgb("#1a3a5c") };
        kopf.Add(new Label
        {
            Text = titel,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center
        }, 0, 0);

        var kreuz = new Button
        {
            Text = "✕",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 44,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.End
        };
        kreuz.Clicked += (s, e) => schliessen();
        kopf.Add(kreuz, 0, 0);

        return kopf;
    }

    private View Detailinhalt(PutzlisteEintrag eintrag)
    {
        var inhalt = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(20) };

        if (eintrag.HasBeschreibung)
        {
            inhalt.Children.Add(Abschnittstitel("Beschreibung"));
            inhalt.Children.Add(new Label
            {
                Text = eintrag.Beschreibung,
                FontSize = 14,
                TextColor = Color.FromArgb("#444444")
            });
        }

        inhalt.Children.Add(Abschnittstitel("Anmerkung"));
        inhalt.Children.Add(Anmerkungsfeld(eintrag));

        inhalt.Children.Add(Abschnittstitel("Ursprungsbilder (Checkliste)"));
        if (eintrag.HasBilder)
            inhalt.Children.Add(new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = Bilderreihe(eintrag.Bilder!, loeschbar: false, eintrag)
            });
        else
            inhalt.Children.Add(Leerhinweis("Keine Vorgabebilder"));

        inhalt.Children.Add(Abschnittstitel("Fotos der Arbeitskraft"));
        var eigeneReihe = Bilderreihe(eintrag.Fotos ?? new List<PutzlisteBild>(), loeschbar: true, eintrag);
        inhalt.Children.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = eigeneReihe });

        var aufnehmen = new Button
        {
            Text = "\U0001F4F7 Foto aufnehmen",
            BackgroundColor = Color.FromArgb("#2196F3"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 10,
            HeightRequest = 48
        };
        aufnehmen.Clicked += async (s, e) => await ChecklistenFotoAufnehmenAsync(eintrag, eigeneReihe);
        inhalt.Children.Add(aufnehmen);

        return inhalt;
    }

    private View Anmerkungsfeld(PutzlisteEintrag eintrag)
    {
        var feld = new Editor
        {
            Text = eintrag.Kommentar ?? "",
            HeightRequest = 70,
            FontSize = 14,
            BackgroundColor = Color.FromArgb("#f5f5f5"),
            TextColor = Colors.Black,
            Placeholder = "Anmerkung zu diesem Punkt..."
        };

        feld.Unfocused += async (s, e) =>
        {
            try
            {
                var text = feld.Text ?? "";
                if ((eintrag.Kommentar ?? "") == text) return;

                var antwort = await _apiService.SavePutzlisteEintragKommentarAsync(_taskId, eintrag.Id, text);
                if (antwort.Success) eintrag.Kommentar = text;
            }
            catch (Exception ex)
            {
                // Unfocus feuert auch beim Wegnavigieren - nie werfen lassen
                System.Diagnostics.Debug.WriteLine($"[AufgabePage] Punkt-Anmerkung: {ex.Message}");
            }
        };

        return feld;
    }

    private static Label Abschnittstitel(string text) => new()
    {
        Text = text,
        FontSize = 11,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#999999")
    };

    private static Label Leerhinweis(string text) => new()
    {
        Text = text,
        FontSize = 13,
        FontAttributes = FontAttributes.Italic,
        TextColor = Color.FromArgb("#999999")
    };

    private HorizontalStackLayout Bilderreihe(IEnumerable<PutzlisteBild> bilder, bool loeschbar, PutzlisteEintrag eintrag)
    {
        var reihe = new HorizontalStackLayout { Spacing = 8 };
        foreach (var bild in bilder)
            reihe.Children.Add(Bildkachel(bild, loeschbar, eintrag));
        return reihe;
    }

    private View Bildkachel(PutzlisteBild bild, bool loeschbar, PutzlisteEintrag eintrag)
    {
        var kachel = new Grid { WidthRequest = 84, HeightRequest = 84 };

        var anzeige = new Image { WidthRequest = 84, HeightRequest = 84, Aspect = Aspect.AspectFill };
        _bilder.Laden(anzeige, bild.Url);

        kachel.Children.Add(new Border
        {
            Content = anzeige,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            BackgroundColor = Color.FromArgb("#E0E0E0"),
            WidthRequest = 84,
            HeightRequest = 84
        });

        // Antippen der ganzen Zelle -> Vollbild (robuster als nur auf dem Bild)
        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => _bilder.VollbildUeberlagern(bild.Url);
        kachel.GestureRecognizers.Add(tippen);

        if (loeschbar)
        {
            var loeschen = new Button
            {
                Text = "✕",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Color.FromArgb("#E91E63"),
                TextColor = Colors.White,
                WidthRequest = 22,
                HeightRequest = 22,
                CornerRadius = 11,
                Padding = 0,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start
            };
            loeschen.Clicked += async (s, e) => await ChecklistenFotoEntfernenAsync(bild, kachel, eintrag);
            kachel.Children.Add(loeschen);
        }

        return kachel;
    }

    private async Task ChecklistenFotoAufnehmenAsync(PutzlisteEintrag eintrag, HorizontalStackLayout reihe)
    {
        try
        {
            var bytes = await _fotoAufnahme.KameraAsync();
            if (bytes == null) return;

            var dateiname = $"checkliste_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var antwort = await _apiService.UploadPutzlisteFotoAsync(_taskId, eintrag.Id, dateiname, bytes);
            if (!antwort.Success)
            {
                await DisplayAlertAsync("Fehler", antwort.Error ?? "Upload fehlgeschlagen", "OK");
                return;
            }

            var bild = new PutzlisteBild { Id = antwort.Id, Url = antwort.Url };
            eintrag.Fotos ??= new List<PutzlisteBild>();
            eintrag.Fotos.Add(bild);
            reihe.Children.Add(Bildkachel(bild, loeschbar: true, eintrag));
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Checklisten-Foto: {ex.Message}");
            await DisplayAlertAsync("Fehler", ex.Message, "OK");
        }
    }

    private async Task ChecklistenFotoEntfernenAsync(PutzlisteBild bild, View kachel, PutzlisteEintrag eintrag)
    {
        try
        {
            bool sicher = await DisplayAlertAsync("Foto löschen", "Dieses Foto wirklich löschen?", "Löschen", "Abbrechen");
            if (!sicher) return;

            var antwort = await _apiService.DeletePutzlisteFotoAsync(bild.Id);
            if (!antwort.Success) return;

            eintrag.Fotos?.RemoveAll(f => f.Id == bild.Id);
            if (kachel.Parent is Microsoft.Maui.Controls.Layout reihe)
                reihe.Children.Remove(kachel);
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Foto löschen: {ex.Message}");
        }
    }
}
