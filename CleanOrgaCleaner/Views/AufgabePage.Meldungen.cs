using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter "Probleme" und "Notizen": beide zeigen dieselbe Art Eintrag
/// (Name, Beschreibung, Fotos) und teilen sich deshalb den Aufbau.
/// </summary>
public partial class AufgabePage
{
    private void BuildProblems()
        => FuelleListe(ProblemsStack, NoProblemsLabel, _task?.Problems);

    private void BuildAnmerkungen()
        => FuelleListe(AnmerkungenStack, NoAnmerkungenLabel, _task?.Anmerkungen);

    private void FuelleListe(Microsoft.Maui.Controls.Layout ziel, Label leerHinweis, List<ImageListDescription>? eintraege)
    {
        ziel.Children.Clear();

        if (eintraege == null || eintraege.Count == 0)
        {
            leerHinweis.IsVisible = true;
            return;
        }

        leerHinweis.IsVisible = false;
        foreach (var eintrag in eintraege)
            ziel.Children.Add(ErzeugeEintrag(eintrag));
    }

    private void OnAddProblemClicked(object sender, EventArgs e)
        => OeffneDialogFuerNeuenEintrag("problem", Translations.Get("report_problem"));

    private void OnAddAnmerkungClicked(object sender, EventArgs e)
        => OeffneDialogFuerNeuenEintrag("anmerkung", Translations.Get("add_note"));

    /// <summary>Karte mit Vorschaubild, Text und Löschknopf; Antippen öffnet den Dialog.</summary>
    private View ErzeugeEintrag(ImageListDescription eintrag)
    {
        var karte = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#e0e0e0"),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = 12,
            Shadow = new Shadow { Brush = Colors.Gray, Offset = new Point(0, 2), Radius = 8, Opacity = 0.1f }
        };

        var raster = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };

        var vorschau = ErstesFoto(eintrag);
        if (vorschau != null)
        {
            raster.Children.Add(vorschau);
            Grid.SetColumn(vorschau, 0);
        }

        var texte = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        texte.Children.Add(new Label
        {
            Text = eintrag.Name,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333333")
        });
        if (!string.IsNullOrEmpty(eintrag.Description))
            texte.Children.Add(new Label
            {
                Text = eintrag.Description,
                FontSize = 13,
                TextColor = Color.FromArgb("#666666"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            });
        raster.Children.Add(texte);
        Grid.SetColumn(texte, 1);

        var loeschen = new Button
        {
            Text = "✕",
            BackgroundColor = Color.FromArgb("#E91E63"),
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            WidthRequest = 44,
            HeightRequest = 44,
            CornerRadius = 22,
            Padding = 0,
            VerticalOptions = LayoutOptions.Center
        };
        var kennung = eintrag.Id;
        var art = eintrag.Type;
        loeschen.Clicked += async (s, e) => await LoescheEintragAsync(kennung, art);
        raster.Children.Add(loeschen);
        Grid.SetColumn(loeschen, 2);

        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => OeffneDialogFuerEintrag(eintrag);
        karte.GestureRecognizers.Add(tippen);

        karte.Content = raster;
        return karte;
    }

    /// <summary>Vorschaubild des ersten Fotos - null, wenn es keins gibt.</summary>
    private View? ErstesFoto(ImageListDescription eintrag)
    {
        var foto = eintrag.Photos?.FirstOrDefault();
        var adresse = foto?.ThumbnailUrl ?? foto?.Url;
        if (string.IsNullOrEmpty(adresse)) return null;

        var bild = new Image { WidthRequest = 70, HeightRequest = 70, Aspect = Aspect.AspectFill };
        _bilder.Laden(bild, adresse);

        return new Border
        {
            Content = bild,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Stroke = Color.FromArgb("#e0e0e0"),
            WidthRequest = 70,
            HeightRequest = 70,
            BackgroundColor = Color.FromArgb("#E0E0E0")
        };
    }

    private async Task LoescheEintragAsync(int kennung, string art)
    {
        var t = Translations.Get;
        bool istProblem = art == "problem";

        try
        {
            bool sicher = await DisplayAlertAsync(
                t(istProblem ? "delete_problem_title" : "delete_image"),
                t(istProblem ? "delete_problem_confirm" : "confirm_delete_image"),
                t("yes_delete"),
                t("cancel"));
            if (!sicher) return;

            var antwort = await _apiService.DeleteImageListItemAsync(kennung);
            if (antwort.Success)
            {
                await LadeAufgabeAsync();
            }
            else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
            {
                await DisplayAlertAsync(t("no_connection"), t("network_error_hint"), t("ok"));
            }
            else
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("delete_error"), t("ok"));
            }
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Eintrag löschen: {ex.Message}");
            if (NetworkErrorHelper.IsNetworkError(ex.Message))
                await DisplayAlertAsync(t("no_connection"), t("network_error_hint"), t("ok"));
            else
                await DisplayAlertAsync(t("error"), ex.Message, t("ok"));
        }
    }
}
