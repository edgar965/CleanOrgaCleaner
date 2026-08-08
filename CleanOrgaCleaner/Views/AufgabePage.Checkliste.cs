using CleanOrgaCleaner.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter "Checkliste": eine Zeile je Punkt mit Vorgabebild, eigenem Foto,
/// Haken und Bearbeiten-Knopf.
/// </summary>
public partial class AufgabePage
{
    /// <summary>Verhindert, dass ein zurückgesetzter Haken erneut speichert.</summary>
    private bool _hakenWirdZurueckgesetzt;

    private void BuildCheckliste()
    {
        ChecklisteStack.Children.Clear();

        var eintraege = _task?.Putzliste;
        if (eintraege == null || eintraege.Count == 0)
        {
            NoChecklisteLabel.IsVisible = true;
            ChecklisteKommentarBox.IsVisible = false;
            return;
        }

        NoChecklisteLabel.IsVisible = false;

        // Anmerkung zur gesamten Checkliste
        ChecklisteKommentarEditor.Text = _task?.PutzlisteKommentar ?? "";
        ChecklisteKommentarBox.IsVisible = true;

        foreach (var eintrag in eintraege)
            ChecklisteStack.Children.Add(ErzeugeChecklistenZeile(eintrag));
    }

    private async void OnChecklisteKommentarUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (_task == null) return;

            var text = ChecklisteKommentarEditor.Text ?? "";
            if ((_task.PutzlisteKommentar ?? "") == text) return;  // keine Änderung

            var antwort = await _apiService.SavePutzlisteChecklistKommentarAsync(_taskId, text);
            if (antwort.Success) _task.PutzlisteKommentar = text;
        }
        catch (Exception ex)
        {
            // Unfocus feuert auch beim Wegnavigieren - async void darf nie werfen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Kommentar speichern: {ex.Message}");
        }
    }

    /// <summary>Spalten: Name | Vorgabebild | eigenes Foto | Haken | Bearbeiten.</summary>
    private static ColumnDefinitionCollection ZeilenSpalten() => new()
    {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(new GridLength(46)),
        new ColumnDefinition(new GridLength(46)),
        new ColumnDefinition(GridLength.Auto),
        new ColumnDefinition(GridLength.Auto)
    };

    private View ErzeugeChecklistenZeile(PutzlisteEintrag eintrag)
    {
        var zeile = new Grid
        {
            ColumnDefinitions = ZeilenSpalten(),
            Padding = new Thickness(2, 10, 2, 10),
            ColumnSpacing = 6
        };

        zeile.Add(NameMitHinweis(eintrag), 0, 0);
        zeile.Add(BildZelle(eintrag.HasBilder ? eintrag.Bilder![0].Url : null), 1, 0);
        zeile.Add(BildZelle(eintrag.HasFotos ? eintrag.Fotos![0].Url : null), 2, 0);
        zeile.Add(Haken(eintrag), 3, 0);
        zeile.Add(BearbeitenKnopf(eintrag), 4, 0);

        return zeile;
    }

    private static View NameMitHinweis(PutzlisteEintrag eintrag)
    {
        var stapel = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        stapel.Children.Add(new Label
        {
            Text = eintrag.Name,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#222222"),
            VerticalOptions = LayoutOptions.Center
        });

        // Sprechblase zeigt: zu diesem Punkt gibt es eine Anmerkung
        if (!string.IsNullOrWhiteSpace(eintrag.Kommentar))
            stapel.Children.Add(new Label { Text = "💬", FontSize = 12, VerticalOptions = LayoutOptions.Center });

        return stapel;
    }

    private View Haken(PutzlisteEintrag eintrag)
    {
        var haken = new CheckBox
        {
            IsChecked = eintrag.Checked,
            Color = Color.FromArgb("#2196F3"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        haken.CheckedChanged += async (s, e) => await HakenGesetztAsync(haken, eintrag, e.Value);
        return haken;
    }

    private async Task HakenGesetztAsync(CheckBox haken, PutzlisteEintrag eintrag, bool neuerWert)
    {
        if (_hakenWirdZurueckgesetzt) return;

        try
        {
            var antwort = await _apiService.TogglePutzlisteItemAsync(_taskId, eintrag.Id);
            if (antwort.Success)
            {
                eintrag.Checked = antwort.Checked;
                return;
            }

            HakenZuruecksetzen(haken, neuerWert);
            await DisplayAlertAsync("Fehler", "Konnte nicht gespeichert werden", "OK");
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Event-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Haken: {ex.Message}");
            HakenZuruecksetzen(haken, neuerWert);
        }
    }

    private void HakenZuruecksetzen(CheckBox haken, bool neuerWert)
    {
        try
        {
            _hakenWirdZurueckgesetzt = true;
            haken.IsChecked = !neuerWert;
        }
        finally
        {
            _hakenWirdZurueckgesetzt = false;
        }
    }

    private View BearbeitenKnopf(PutzlisteEintrag eintrag)
    {
        var knopf = new Button
        {
            Text = "✏️",
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#1a3a5c"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Padding = new Thickness(0),
            WidthRequest = 40,
            HeightRequest = 40,
            VerticalOptions = LayoutOptions.Center
        };
        knopf.Clicked += (s, e) => OeffneChecklistenDetail(eintrag);
        return knopf;
    }

    /// <summary>Kleine Bildzelle; Antippen zeigt das Bild formatfüllend.</summary>
    private View BildZelle(string? adresse)
    {
        var rahmen = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            BackgroundColor = Color.FromArgb("#f0f0f0"),
            WidthRequest = 44,
            HeightRequest = 44,
            VerticalOptions = LayoutOptions.Center
        };

        if (string.IsNullOrEmpty(adresse))
        {
            rahmen.Content = new Label
            {
                Text = "—",
                FontSize = 16,
                TextColor = Color.FromArgb("#bbb"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            return rahmen;
        }

        var bild = new Image { Aspect = Aspect.AspectFill };
        _bilder.Laden(bild, adresse);
        rahmen.Content = bild;

        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => _bilder.VollbildUeberlagern(adresse);
        rahmen.GestureRecognizers.Add(tippen);

        return rahmen;
    }
}
