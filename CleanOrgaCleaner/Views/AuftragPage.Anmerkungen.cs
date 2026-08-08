using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter "Notizen" des Auftrags-Dialogs: Liste der Notizen zur Aufgabe.
/// Der Eingabedialog dazu liegt in AuftragPage.AnmerkungDialog.cs.
/// </summary>
public partial class AuftragPage
{
    private List<ImageListDescription> _anmerkungen = new();

    private void LeereAnmerkungen() => _anmerkungen = new List<ImageListDescription>();

    /// <summary>Notizen nachladen; die Anzeige folgt, sobald sie da sind.</summary>
    private void LadeAnmerkungen(int taskId) => _ = LadeAnmerkungenAsync(taskId);

    private async Task LadeAnmerkungenAsync(int taskId)
    {
        LeereAnmerkungen();
        try
        {
            var geladen = await _apiService.GetTaskAnmerkungenAsync(taskId);
            if (geladen != null)
                _anmerkungen = geladen;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Notizen laden: {ex.Message}");
        }
        UpdateAnmerkungenDisplay();
    }

    private void UpdateAnmerkungenDisplay()
    {
        AnmerkungenStack.Children.Clear();
        NoAnmerkungenLabel.IsVisible = _anmerkungen.Count == 0;

        foreach (var eintrag in _anmerkungen)
            AnmerkungenStack.Children.Add(Notizzeile(eintrag));
    }

    private View Notizzeile(ImageListDescription eintrag)
    {
        var raster = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(50)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };

        var vorschau = Vorschaubild(eintrag);
        Grid.SetColumn(vorschau, 0);
        raster.Children.Add(vorschau);

        var texte = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        texte.Children.Add(new Label
        {
            Text = eintrag.Name ?? Translations.Get("note"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#333")
        });
        if (!string.IsNullOrEmpty(eintrag.Description))
        {
            var kurz = eintrag.Description.Length > 50 ? eintrag.Description[..50] + "..." : eintrag.Description;
            texte.Children.Add(new Label { Text = kurz, FontSize = 12, TextColor = Color.FromArgb("#666") });
        }
        Grid.SetColumn(texte, 1);
        raster.Children.Add(texte);

        var pfeil = new Label
        {
            Text = ">",
            FontSize = 16,
            TextColor = Color.FromArgb("#999"),
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(pfeil, 2);
        raster.Children.Add(pfeil);

        var karte = new Border
        {
            Padding = 10,
            BackgroundColor = Color.FromArgb("#f8f9fa"),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Stroke = Color.FromArgb("#e0e0e0"),
            Content = raster
        };

        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => OeffneNotizdialog(eintrag);
        karte.GestureRecognizers.Add(tippen);

        return karte;
    }

    /// <summary>Erstes Foto der Notiz - ohne Foto steht dort ein Symbol.</summary>
    private static View Vorschaubild(ImageListDescription eintrag)
    {
        var foto = eintrag.Photos?.FirstOrDefault();
        if (foto == null)
        {
            return new Label
            {
                Text = "\U0001F4DD",
                FontSize = 24,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
        }

        return new Border
        {
            Content = new Image
            {
                Source = foto.ThumbnailUrl ?? foto.Url,
                Aspect = Aspect.AspectFill,
                WidthRequest = 50,
                HeightRequest = 50
            },
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Stroke = Colors.Transparent
        };
    }

    /// <summary>
    /// Neue Notiz: eine noch nicht gespeicherte Aufgabe wird vorher automatisch
    /// angelegt - sonst gibt es keine Id, an der die Notiz hängen kann.
    /// </summary>
    private async void OnAddAnmerkungClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;
        try
        {
            if (_isNewTask && !await LegeAuftragVorherAnAsync(t))
                return;

            OeffneNotizdialog(null);
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] AddAnmerkung error: {ex.Message}");
            await Services.UiSicher.FehlerAlertAsync();
        }
    }

    private async Task<bool> LegeAuftragVorherAnAsync(Func<string, string> t)
    {
        var eingaben = LiesEingaben();
        if (!eingaben.IstVollstaendig)
        {
            await DisplayAlertAsync(t("error"), t("task_name_required"), t("ok"));
            return false;
        }

        var antwort = await _apiService.CreateAuftragAsync(eingaben.Name, eingaben.GeplantesDatum,
            eingaben.ApartmentId, eingaben.AufgabenartId, eingaben.Hinweis, eingaben.Status, _assignments);

        if (!antwort.Success || !antwort.TaskId.HasValue)
        {
            await DisplayAlertAsync(t("error"), antwort.Error ?? t("task_create_error"), t("ok"));
            return false;
        }

        // Ab jetzt im Bearbeiten-Modus
        _isNewTask = false;
        _currentTask = new Auftrag { Id = antwort.TaskId.Value, Name = eingaben.Name };
        PopupTitle.Text = t("edit_auftrag");
        BtnDelete.IsVisible = true;
        _ = LoadDataAsync();

        return true;
    }
}
