namespace CleanOrgaCleaner.Views;

/// <summary>
/// Fotos zur Aufgabenbeschreibung in der Ansicht einer zugewiesenen Aufgabe.
///
/// Sie sind eine Anweisung des Bueros und werden hier ausschliesslich angezeigt:
/// kein Hinzufuegen, kein Loeschen. Antippen zeigt das Foto formatfuellend.
/// Der Server weist Aenderungen zusaetzlich mit 403 ab.
/// </summary>
public partial class AufgabePage
{
    private async Task AufgabeFotosLadenAsync()
    {
        try
        {
            var eintraege = await _apiService.GetTaskItemsAsync(_taskId, "aufgabe");

            var fotos = eintraege
                .Where(e => e.Photos != null)
                .SelectMany(e => e.Photos!)
                .Where(f => !string.IsNullOrEmpty(f.Url))
                .ToList();

            AufgabeFotosStack.Children.Clear();

            foreach (var foto in fotos)
            {
                var adresse = foto.Url!;
                var bild = new Image
                {
                    Source = ImageSource.FromUri(new Uri(adresse)),
                    Aspect = Aspect.AspectFill,
                    WidthRequest = 80,
                    HeightRequest = 80
                };

                var kachel = new Border
                {
                    Content = bild,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke = Color.FromArgb("#e0e0e0"),
                    StrokeThickness = 1,
                    Padding = 0,
                    Margin = new Thickness(0, 0, 8, 8)
                };

                var tippen = new TapGestureRecognizer();
                tippen.Tapped += async (s, e) => await FotoGrossZeigenAsync(adresse);
                kachel.GestureRecognizers.Add(tippen);

                AufgabeFotosStack.Children.Add(kachel);
            }

            AufgabeFotosBlock.IsVisible = fotos.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Laden fehlgeschlagen: {ex.Message}");
            AufgabeFotosBlock.IsVisible = false;
        }
    }

    /// <summary>Foto formatfuellend anzeigen - reine Ansicht, kein Bearbeiten.</summary>
    private async Task FotoGrossZeigenAsync(string adresse)
    {
        var bild = new Image
        {
            Source = ImageSource.FromUri(new Uri(adresse)),
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var seite = new ContentPage
        {
            BackgroundColor = Color.FromArgb("#EB000000"),
            Content = new Grid { Children = { bild } }
        };

        var schliessen = new TapGestureRecognizer();
        schliessen.Tapped += async (s, e) => await Navigation.PopModalAsync();
        bild.GestureRecognizers.Add(schliessen);

        await Navigation.PushModalAsync(seite);
    }
}
