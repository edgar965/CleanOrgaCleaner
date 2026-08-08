using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Fotos zur Aufgabenbeschreibung in der Ansicht einer zugewiesenen Aufgabe.
///
/// Sie sind eine Anweisung des Büros und werden hier ausschließlich angezeigt:
/// kein Hinzufügen, kein Löschen. Antippen zeigt das Foto formatfüllend.
/// Der Server weist Änderungen zusätzlich mit 403 ab.
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
                .Select(f => f.Url)
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();

            AufgabeFotosStack.Children.Clear();
            foreach (var adresse in fotos)
                AufgabeFotosStack.Children.Add(Fotokachel(adresse!));

            AufgabeFotosBlock.IsVisible = fotos.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabeFotos] Laden fehlgeschlagen: {ex.Message}");
            AufgabeFotosBlock.IsVisible = false;
        }
    }

    private View Fotokachel(string adresse)
    {
        var kachel = new Border
        {
            Content = new Image
            {
                Source = ImageSource.FromUri(new Uri(adresse)),
                Aspect = Aspect.AspectFill,
                WidthRequest = 80,
                HeightRequest = 80
            },
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Color.FromArgb("#e0e0e0"),
            StrokeThickness = 1,
            Padding = 0,
            Margin = new Thickness(0, 0, 8, 8)
        };

        var tippen = new TapGestureRecognizer();
        tippen.Tapped += async (s, e) => await _bilder.VollbildModalAsync(adresse);
        kachel.GestureRecognizers.Add(tippen);

        return kachel;
    }
}
