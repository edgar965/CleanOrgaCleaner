using CleanOrgaCleaner.Helpers;
using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Eingabedialog für Notizen zu einem Auftrag: Text, Fotos, Speichern, Löschen.
/// </summary>
public partial class AuftragPage
{
    /// <summary>Höchstlänge der Beschreibung (auch der Server begrenzt so).</summary>
    private const int MaxBeschreibung = 300;

    /// <summary>Bearbeitete Notiz - null bedeutet: neue Notiz.</summary>
    private ImageListDescription? _currentAnmerkung;

    /// <summary>Neu aufgenommene, noch nicht hochgeladene Fotos.</summary>
    private readonly List<byte[]> _neueNotizFotos = new();

    private void OeffneNotizdialog(ImageListDescription? notiz)
    {
        _currentAnmerkung = notiz;
        _neueNotizFotos.Clear();

        ImageListDescriptionDialogNameEntry.Text = notiz?.Name ?? "Reparatur";
        ImageListDescriptionDialogDescEditor.Text = notiz?.Description ?? "";
        AktualisiereZeichenzahl();

        ImageListDescriptionDialogTitle.Text = Translations.Get(notiz != null ? "edit_note" : "add_note");
        DeleteAnmerkungButton.IsVisible = notiz != null;

        AktualisiereFotovorschau();
        ImageListDescriptionDialog.IsVisible = true;
    }

    private void OnImageListDescriptionDialogDescTextChanged(object sender, TextChangedEventArgs e)
        => AktualisiereZeichenzahl();

    private void AktualisiereZeichenzahl()
    {
        var laenge = ImageListDescriptionDialogDescEditor.Text?.Length ?? 0;
        ImageListDescriptionDialogCharCountLabel.Text = $"{laenge} / {MaxBeschreibung}";
    }

    #region Fotovorschau

    /// <summary>
    /// Vorschau vollständig neu aufbauen: bereits gespeicherte Fotos mit
    /// Löschknopf, danach die neu aufgenommenen. Vorher wurde nur angehängt und
    /// über eine Indexrechnung geraten, was schon in der Liste steht.
    /// </summary>
    private void AktualisiereFotovorschau()
    {
        ImageListDescriptionDialogPhotoPreviewStack.Children.Clear();

        var vorhandene = _currentAnmerkung?.Photos;
        if (vorhandene != null)
        {
            foreach (var foto in vorhandene)
                ImageListDescriptionDialogPhotoPreviewStack.Children.Add(GespeichertesFotoZeile(foto));
        }

        foreach (var bytes in _neueNotizFotos)
            ImageListDescriptionDialogPhotoPreviewStack.Children.Add(NeuesFotoVorschau(bytes));

        ImageListDescriptionDialogPhotoPreviewStack.IsVisible =
            ImageListDescriptionDialogPhotoPreviewStack.Children.Count > 0;
        ImageListDescriptionDialogPhotoCountLabel.Text = $"{_neueNotizFotos.Count} neue(s) Foto(s)";
        ImageListDescriptionDialogPhotoCountLabel.IsVisible = _neueNotizFotos.Count > 0;
    }

    private View GespeichertesFotoZeile(ImageListDescriptionPhoto foto)
    {
        var zeile = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(140)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 15,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var rahmen = new Border
        {
            Content = new Image
            {
                Source = foto.ThumbnailUrl ?? foto.Url,
                Aspect = Aspect.AspectFill,
                WidthRequest = 140,
                HeightRequest = 140
            },
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.LightGray,
            StrokeThickness = 1,
            WidthRequest = 140,
            HeightRequest = 140
        };

        var adresse = foto.Url;
        var tippen = new TapGestureRecognizer();
        tippen.Tapped += (s, e) => _bilder.VollbildUeberlagern(adresse, mitAnmeldung: false);
        rahmen.GestureRecognizers.Add(tippen);

        zeile.Children.Add(rahmen);
        Grid.SetColumn(rahmen, 0);

        var knoepfe = new VerticalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
        var loeschen = new Button
        {
            Text = Translations.Get("delete"),
            FontSize = 14,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#f44336"),
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(15, 0)
        };
        var kennung = foto.Id;
        loeschen.Clicked += async (s, e) => await LoescheNotizfotoAsync(kennung);
        knoepfe.Children.Add(loeschen);

        zeile.Children.Add(knoepfe);
        Grid.SetColumn(knoepfe, 1);

        return zeile;
    }

    private static View NeuesFotoVorschau(byte[] bytes) => new Border
    {
        Content = new Image
        {
            Source = ImageSource.FromStream(() => new MemoryStream(bytes)),
            Aspect = Aspect.AspectFill,
            WidthRequest = 60,
            HeightRequest = 60
        },
        StrokeShape = new RoundRectangle { CornerRadius = 6 },
        Stroke = Colors.Transparent,
        Margin = new Thickness(0, 0, 5, 5)
    };

    private async Task LoescheNotizfotoAsync(int fotoId)
    {
        var t = Translations.Get;
        try
        {
            var sicher = await DisplayAlertAsync(
                t("delete_image"), t("delete_image_confirm"), t("yes"), t("no"));
            if (!sicher) return;

            var antwort = await _apiService.DeleteImageListPhotoAsync(fotoId);
            if (!antwort.Success)
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("delete_error"), t("ok"));
                return;
            }

            if (_currentTask == null) return;

            // Notizen frisch holen und den Dialog mit dem neuen Stand zeigen
            await LadeAnmerkungenAsync(_currentTask.Id);
            var aktualisiert = _anmerkungen.FirstOrDefault(a => a.Id == _currentAnmerkung?.Id);
            if (aktualisiert != null)
                OeffneNotizdialog(aktualisiert);
            else
                ImageListDescriptionDialog.IsVisible = false;
        }
        catch (Exception ex)
        {
            // Aufrufer ist ein async void Clicked-Lambda - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Notizfoto löschen: {ex.Message}");
            await UiSicher.FehlerAlertAsync();
        }
    }

    #endregion

    #region Foto aufnehmen

    private async void OnImageListDescriptionDialogTakePhotoClicked(object sender, EventArgs e)
        => await NotizfotoHinzufuegenAsync(ausKamera: true);

    private async void OnImageListDescriptionDialogPickPhotoClicked(object sender, EventArgs e)
        => await NotizfotoHinzufuegenAsync(ausKamera: false);

    private async Task NotizfotoHinzufuegenAsync(bool ausKamera)
    {
        try
        {
            var bytes = ausKamera
                ? await _fotoAufnahme.KameraAsync()
                : await _fotoAufnahme.GalerieAsync();
            if (bytes == null) return;

            var markiert = await _fotoAufnahme.MarkierenAsync(bytes);
            _neueNotizFotos.Add(await ImageHelper.CompressImageAsync(markiert));
            AktualisiereFotovorschau();
        }
        catch (Exception ex)
        {
            // async void Aufrufer - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Notizfoto: {ex.Message}");
            await DisplayAlertAsync(Translations.Get("error"), ex.Message, Translations.Get("ok"));
        }
    }

    #endregion

    #region Speichern und Löschen

    private async void OnSaveImageListDescriptionDialogClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;

        var name = ImageListDescriptionDialogNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlertAsync(t("error"), t("name_required"), t("ok"));
            return;
        }

        if (_currentTask == null) return;

        var beschreibung = ImageListDescriptionDialogDescEditor.Text ?? "";

        // Ohne Netz: nur neue Notizen lassen sich vormerken
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && _currentAnmerkung == null)
        {
            await NotizVormerkenAsync(name, beschreibung);
            return;
        }

        try
        {
            bool fertig = _currentAnmerkung != null
                ? await AendereNotizAsync(name, beschreibung)
                : await LegeNotizAnAsync(name, beschreibung);
            if (!fertig) return;

            ImageListDescriptionDialog.IsVisible = false;
            await LadeAnmerkungenAsync(_currentTask.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Notiz speichern: {ex.Message}");

            if (!NetworkErrorHelper.IsNetworkError(ex.Message))
            {
                await DisplayAlertAsync(t("error"), t("save_error"), t("ok"));
                return;
            }

            if (_currentAnmerkung == null)
                await NotizVormerkenAsync(name, beschreibung);
            else
                await DisplayAlertAsync(t("no_connection"), t("network_error_hint"), t("ok"));
        }
    }

    private async Task<bool> AendereNotizAsync(string name, string beschreibung)
    {
        var t = Translations.Get;
        var antwort = await _apiService.UpdateImageListDescriptionAsync(_currentAnmerkung!.Id, name, beschreibung);

        if (!antwort.Success)
        {
            // Änderungen lassen sich nicht sinnvoll vormerken - nur hinweisen
            if (NetworkErrorHelper.IsNetworkError(antwort.Error))
                await DisplayAlertAsync(t("no_connection"), t("network_error_hint"), t("ok"));
            else
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("update_error"), t("ok"));
            return false;
        }

        foreach (var bytes in _neueNotizFotos)
            await _apiService.AddPhotoToImageListDescriptionAsync(_currentAnmerkung.Id, bytes);

        return true;
    }

    private async Task<bool> LegeNotizAnAsync(string name, string beschreibung)
    {
        var t = Translations.Get;
        var antwort = await _apiService.CreateTaskAnmerkungAsync(
            _currentTask!.Id, name, beschreibung, _neueNotizFotos);

        if (antwort.Success) return true;

        if (NetworkErrorHelper.IsNetworkError(antwort.Error))
            await NotizVormerkenAsync(name, beschreibung);
        else
            await DisplayAlertAsync(t("error"), antwort.Error ?? t("create_error"), t("ok"));

        return false;
    }

    private async Task NotizVormerkenAsync(string name, string beschreibung)
    {
        var t = Translations.Get;
        await OfflineQueueService.Instance.EnqueueImageListItemAsync(
            _currentTask!.Id, "anmerkung", name, beschreibung, _neueNotizFotos);

        ImageListDescriptionDialog.IsVisible = false;
        await DisplayAlertAsync(t("no_connection"), t("saved_offline"), t("ok"));
    }

    private async void OnDeleteAnmerkungClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;
        try
        {
            if (_currentAnmerkung == null) return;

            // Auch die Rückfrage kann werfen (Seite im Abbau) - async void
            // braucht den Schutz um den kompletten Rumpf
            var sicher = await DisplayAlertAsync(
                t("delete_note"), t("delete_note_confirm"), t("yes"), t("no"));
            if (!sicher) return;

            var antwort = await _apiService.DeleteImageListItemAsync(_currentAnmerkung.Id);
            if (antwort.Success)
            {
                ImageListDescriptionDialog.IsVisible = false;
                if (_currentTask != null)
                    await LadeAnmerkungenAsync(_currentTask.Id);
            }
            else
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("delete_error"), t("ok"));
            }
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] DeleteAnmerkung error: {ex.Message}");
            await UiSicher.FehlerAlertAsync();
        }
    }

    private void OnCancelImageListDescriptionDialogClicked(object sender, EventArgs e)
        => ImageListDescriptionDialog.IsVisible = false;

    /// <summary>
    /// Antippen des Hintergrunds schließt den Dialog bewusst NICHT - sonst
    /// gingen Eingaben durch eine unbeabsichtigte Berührung verloren.
    /// </summary>
    private void OnImageListDescriptionDialogBackgroundTapped(object sender, EventArgs e) { }

    #endregion
}
