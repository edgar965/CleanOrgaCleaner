using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Eingabedialog für Probleme und Notizen: anlegen, ändern, speichern.
/// Die Fotovorschau darin liegt in AufgabePage.DialogFotos.cs.
/// </summary>
public partial class AufgabePage
{
    /// <summary>Höchstlänge der Beschreibung (auch der Server begrenzt so).</summary>
    private const int MaxBeschreibung = 300;

    /// <summary>"problem" oder "anmerkung" - bestimmt, was gespeichert wird.</summary>
    private string _dialogArt = "problem";

    /// <summary>null = neuer Eintrag, sonst die Kennung des bearbeiteten Eintrags.</summary>
    private int? _dialogKennung;

    /// <summary>Bearbeiteter Eintrag - für den Zugriff auf bereits vorhandene Fotos.</summary>
    private ImageListDescription? _dialogEintrag;

    /// <summary>Neu aufgenommene, noch nicht hochgeladene Fotos.</summary>
    private readonly List<NeuesFoto> _neueFotos = new();

    private void OeffneDialogFuerNeuenEintrag(string art, string titel)
    {
        _dialogArt = art;
        _dialogKennung = null;
        _dialogEintrag = null;

        ImageListDescriptionDialogTitle.Text = titel;
        SetzeDialogfelder(name: "", beschreibung: "");
        ZeigeDialog();
    }

    private void OeffneDialogFuerEintrag(ImageListDescription eintrag)
    {
        _dialogArt = eintrag.Type;
        _dialogKennung = eintrag.Id;
        _dialogEintrag = eintrag;

        ImageListDescriptionDialogTitle.Text = Translations.Get(eintrag.IsProblem ? "edit_problem" : "edit_note");
        SetzeDialogfelder(eintrag.Name ?? "", eintrag.Description ?? "");
        ZeigeDialog();
    }

    private void SetzeDialogfelder(string name, string beschreibung)
    {
        ImageListDescriptionDialogNameEntry.Text = name;
        ImageListDescriptionDialogDescEditor.Text = beschreibung;
        ImageListDescriptionDialogCharCountLabel.Text = $"{beschreibung.Length} / {MaxBeschreibung}";
        _neueFotos.Clear();
    }

    private void ZeigeDialog()
    {
        AktualisiereFotovorschau();
        ImageListDescriptionDialog.IsVisible = true;
    }

    private void OnImageListDescriptionDialogBackgroundTapped(object sender, EventArgs e)
        => ImageListDescriptionDialog.IsVisible = false;

    private void OnCancelImageListDescriptionDialogClicked(object sender, EventArgs e)
        => ImageListDescriptionDialog.IsVisible = false;

    private void OnImageListDescriptionDialogDescTextChanged(object sender, TextChangedEventArgs e)
    {
        var laenge = e.NewTextValue?.Length ?? 0;
        ImageListDescriptionDialogCharCountLabel.Text = $"{laenge} / {MaxBeschreibung}";

        if (laenge > MaxBeschreibung)
            ImageListDescriptionDialogDescEditor.Text = e.NewTextValue?.Substring(0, MaxBeschreibung);
    }

    private async void OnImageListDescriptionDialogTakePhotoClicked(object sender, EventArgs e)
        => await FotoHinzufuegenAsync(ausKamera: true);

    private async void OnImageListDescriptionDialogPickPhotoClicked(object sender, EventArgs e)
        => await FotoHinzufuegenAsync(ausKamera: false);

    private async Task FotoHinzufuegenAsync(bool ausKamera)
    {
        try
        {
            var bytes = ausKamera
                ? await _fotoAufnahme.KameraAsync()
                : await _fotoAufnahme.GalerieAsync();
            if (bytes == null) return;

            var fertig = await _fotoAufnahme.MarkierenAsync(bytes);
            _neueFotos.Add(NeuesFoto.MitZeitstempel("photo", fertig));
            AktualisiereFotovorschau();
        }
        catch (Exception ex)
        {
            // async void Aufrufer - nie werfen lassen
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Foto hinzufügen: {ex.Message}");
            await DisplayAlertAsync(Translations.Get("error"), ex.Message, Translations.Get("ok"));
        }
    }

    private async void OnSaveImageListDescriptionDialogClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;

        var name = ImageListDescriptionDialogNameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlertAsync(t("error"), t("name_required"), t("ok"));
            return;
        }

        var beschreibung = ImageListDescriptionDialogDescEditor.Text?.Trim();
        ImageListDescriptionDialog.IsVisible = false;

        try
        {
            if (_dialogKennung.HasValue)
                await AendereEintragAsync(_dialogKennung.Value, name, beschreibung);
            else
                await LegeEintragAnAsync(name, beschreibung);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Speichern: {ex.Message}");

            if (NetworkErrorHelper.IsNetworkError(ex.Message) && !_dialogKennung.HasValue)
                await VormerkenAsync(name, beschreibung);
            else if (NetworkErrorHelper.IsNetworkError(ex.Message))
                await DisplayAlertAsync(t("no_connection"), t("network_error_hint"), t("ok"));
            else
                await DisplayAlertAsync(t("error"), t("save_error"), t("ok"));
        }
        finally
        {
            _dialogKennung = null;
        }
    }

    private async Task AendereEintragAsync(int kennung, string name, string? beschreibung)
    {
        var t = Translations.Get;
        var antwort = await _apiService.UpdateImageListItemAsync(kennung, name, beschreibung);

        if (antwort.Success)
        {
            await LadeAufgabeAsync();
        }
        else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
        {
            // Änderungen lassen sich nicht sinnvoll vormerken - nur hinweisen
            await DisplayAlertAsync(t("no_connection"), t("network_error_hint"), t("ok"));
        }
        else
        {
            await DisplayAlertAsync(t("error"), antwort.Error ?? t("update_error"), t("ok"));
        }
    }

    private async Task LegeEintragAnAsync(string name, string? beschreibung)
    {
        var t = Translations.Get;
        var antwort = await _apiService.CreateImageListItemAsync(
            _taskId, _dialogArt, name, beschreibung, NeuesFoto.FuerUebertragung(_neueFotos));

        if (antwort.Success)
        {
            // Bestätigung nur bei Problemen, nicht bei Notizen
            if (_dialogArt == "problem")
                await DisplayAlertAsync(t("saved"), t("problem_reported"), t("ok"));

            await LadeAufgabeAsync();
        }
        else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
        {
            await VormerkenAsync(name, beschreibung);
        }
        else
        {
            await DisplayAlertAsync(t("error"), antwort.Error ?? t("save_error"), t("ok"));
        }
    }

    /// <summary>Ohne Verbindung: neuen Eintrag für die spätere Übertragung vormerken.</summary>
    private async Task VormerkenAsync(string name, string? beschreibung)
    {
        var t = Translations.Get;
        await OfflineQueueService.Instance.EnqueueImageListItemAsync(
            _taskId, _dialogArt, name, beschreibung, NeuesFoto.NurDaten(_neueFotos));

        await DisplayAlertAsync(t("no_connection"), t("saved_offline"), t("ok"));
    }
}
