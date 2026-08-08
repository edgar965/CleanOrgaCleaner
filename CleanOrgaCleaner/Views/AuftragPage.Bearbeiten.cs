using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Auftrags-Dialog: anlegen, bearbeiten, zuweisen, speichern, löschen.
/// </summary>
public partial class AuftragPage
{
    private void OeffneNeuenAuftrag()
    {
        _isNewTask = true;
        _currentTask = null;
        _assignments = LeereZuweisung();
        LeereAnmerkungen();

        PopupTitle.Text = Translations.Get("create_auftrag");
        TaskNameEntry.Text = "Reparatur";
        ApartmentPicker.SelectedIndex = -1;
        AufgabenartPicker.SelectedIndex = -1;
        TaskDatePicker.Date = DateTime.Today;
        TaskHinweisEditor.Text = "";
        _currentStatus = "imported";
        BtnDelete.IsVisible = false;

        // Neue Aufgabe: Fotos dürfen hinzugefügt und wieder entfernt werden
        AufgabeFotosZuruecksetzen(nurAnsehen: false);

        ZeigeArbeitskraefte();
        UpdateAnmerkungenDisplay();
        _tabs.Zeige("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private void OeffneBearbeiten(Auftrag auftrag)
    {
        _isNewTask = false;
        _currentTask = auftrag;
        _assignments = auftrag.Assignments ?? LeereZuweisung();

        PopupTitle.Text = Translations.Get("edit_auftrag");
        TaskNameEntry.Text = auftrag.Name;

        ApartmentPicker.SelectedItem = auftrag.ApartmentId.HasValue
            ? _apartments.FirstOrDefault(a => a.Id == auftrag.ApartmentId.Value)
            : null;
        if (ApartmentPicker.SelectedItem == null) ApartmentPicker.SelectedIndex = -1;

        AufgabenartPicker.SelectedItem = auftrag.AufgabenartId.HasValue
            ? _aufgabenarten.FirstOrDefault(a => a.Id == auftrag.AufgabenartId.Value)
            : null;
        if (AufgabenartPicker.SelectedItem == null) AufgabenartPicker.SelectedIndex = -1;

        if (DateTime.TryParse(auftrag.PlannedDate, out var datum))
            TaskDatePicker.Date = datum;

        TaskHinweisEditor.Text = auftrag.Aufgabe ?? "";
        _currentStatus = auftrag.Status ?? "imported";
        BtnDelete.IsVisible = true;

        LadeAnmerkungen(auftrag.Id);

        // Fotos einer mir zugewiesenen Aufgabe sind eine Anweisung des Büros -
        // dann nur ansehen. Dieselbe Regel setzt der Server durch.
        AufgabeFotosZuruecksetzen(nurAnsehen: IstMirZugewiesen());
        _ = AufgabeFotosLadenAsync(auftrag.Id);

        ZeigeArbeitskraefte();
        _tabs.Zeige("details");
        TaskPopupOverlay.IsVisible = true;
    }

    private bool IstMirZugewiesen()
    {
        if (_apiService.CleanerId is not int meineId) return false;

        return (_assignments.Cleaning?.Contains(meineId) ?? false)
            || _assignments.Check == meineId
            || (_assignments.Repare?.Contains(meineId) ?? false);
    }

    private void ZeigeArbeitskraefte()
    {
        foreach (var person in _cleaners)
            person.IsAssigned = _assignments.Cleaning?.Contains(person.Id) ?? false;

        // Neu zuweisen erzwingt den Neuaufbau der Liste
        CleanersList.ItemsSource = null;
        CleanersList.ItemsSource = _cleaners;
    }

    private void OnAssignToggled(object sender, EventArgs e)
    {
        if (sender is not Button knopf) return;
        if (!int.TryParse(knopf.CommandParameter?.ToString(), out int kennung)) return;

        var person = _cleaners.FirstOrDefault(c => c.Id == kennung);
        if (person == null) return;

        person.IsAssigned = !person.IsAssigned;
        _assignments.Cleaning ??= new List<int>();

        if (person.IsAssigned)
        {
            if (!_assignments.Cleaning.Contains(kennung))
                _assignments.Cleaning.Add(kennung);
        }
        else
        {
            _assignments.Cleaning.Remove(kennung);
        }

        ZeigeArbeitskraefte();
    }

    /// <summary>Die Angaben aus dem Dialog einsammeln.</summary>
    private AuftragEingaben LiesEingaben() => new(
        TaskNameEntry.Text?.Trim() ?? "",
        TaskDatePicker.Date,
        (ApartmentPicker.SelectedItem as ApartmentInfo)?.Id,
        (AufgabenartPicker.SelectedItem as AufgabenartInfo)?.Id,
        TaskHinweisEditor.Text,
        _currentStatus);

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;
        try
        {
            var eingaben = LiesEingaben();
            if (!eingaben.IstVollstaendig)
            {
                await DisplayAlertAsync(t("error"), t("task_name_required"), t("ok"));
                return;
            }

            // Ohne Netz gar nicht erst versuchen
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await VormerkenAsync(eingaben);
                TaskPopupOverlay.IsVisible = false;
                await DisplayAlertAsync("Offline", t("saved"), t("ok"));
                return;
            }

            var antwort = _isNewTask
                ? await _apiService.CreateAuftragAsync(eingaben.Name, eingaben.GeplantesDatum, eingaben.ApartmentId,
                    eingaben.AufgabenartId, eingaben.Hinweis, eingaben.Status, _assignments)
                : await _apiService.UpdateAuftragAsync(_currentTask!.Id, eingaben.Name, eingaben.GeplantesDatum,
                    eingaben.ApartmentId, eingaben.AufgabenartId, eingaben.Hinweis, eingaben.Status, _assignments);

            if (antwort.Success)
            {
                // Fotos anhängen - bei einer neuen Aufgabe kommt die Id erst
                // mit der Antwort zurück.
                var zielId = _isNewTask ? antwort.TaskId : _currentTask?.Id;
                if (zielId is int fotoTaskId)
                    await AufgabeFotosHochladenAsync(fotoTaskId);

                TaskPopupOverlay.IsVisible = false;
                await LoadDataAsync();
            }
            else if (NetworkErrorHelper.IsNetworkError(antwort.Error))
            {
                await VormerkenAsync(eingaben);
                TaskPopupOverlay.IsVisible = false;
                await DisplayAlertAsync(t("no_connection"), t("saved_offline"), t("ok"));
            }
            else
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("task_update_error"), t("ok"));
            }
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception (Timeout, JSON, ...) = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Save error: {ex.Message}");
            await UiSicher.FehlerAlertAsync();
        }
    }

    /// <summary>Ohne Verbindung: Anlegen bzw. Ändern für später vormerken.</summary>
    private async Task VormerkenAsync(AuftragEingaben eingaben)
    {
        var warteschlange = OfflineQueueService.Instance;

        if (_isNewTask)
            await warteschlange.EnqueueTaskCreateAsync(eingaben.Name, eingaben.GeplantesDatum, eingaben.ApartmentId,
                eingaben.AufgabenartId, eingaben.Hinweis, eingaben.Status, _assignments);
        else
            await warteschlange.EnqueueTaskUpdateAsync(_currentTask!.Id, eingaben.Name, eingaben.GeplantesDatum,
                eingaben.ApartmentId, eingaben.AufgabenartId, eingaben.Hinweis, eingaben.Status, _assignments);
    }

    private void OnCancelClicked(object sender, EventArgs e) => TaskPopupOverlay.IsVisible = false;

    private void OnClosePopupClicked(object sender, EventArgs e) => TaskPopupOverlay.IsVisible = false;

    /// <summary>
    /// Antippen des Hintergrunds schließt den Dialog bewusst NICHT - sonst
    /// gingen Eingaben durch eine unbeabsichtigte Berührung verloren.
    /// </summary>
    private void OnPopupOverlayTapped(object sender, EventArgs e) { }

    /// <summary>
    /// Die Aufgabenart wird erst beim Speichern ausgewertet; die Auswahl selbst
    /// verändert nichts an der Anzeige. Der Handler bleibt, weil das
    /// Auswahlfeld in der XAML darauf verweist.
    /// </summary>
    private void OnAufgabenartChanged(object sender, EventArgs e) { }

    private async void OnDeleteTaskClicked(object sender, EventArgs e)
    {
        var t = Translations.Get;
        try
        {
            if (_currentTask == null) return;

            // Auch die Rückfrage kann werfen (Seite im Abbau) - async void
            // braucht den Schutz um den kompletten Rumpf
            var sicher = await DisplayAlertAsync(
                t("delete_task"), t("confirm_delete_task"), t("yes"), t("no"));
            if (!sicher) return;

            var antwort = await _apiService.DeleteAuftragAsync(_currentTask.Id);
            if (antwort.Success)
            {
                TaskPopupOverlay.IsVisible = false;
                await LoadDataAsync();
            }
            else
            {
                await DisplayAlertAsync(t("error"), antwort.Error ?? t("task_delete_error"), t("ok"));
            }
        }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Delete error: {ex.Message}");
            await UiSicher.FehlerAlertAsync();
        }
    }
}
