using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter "Protokoll" im Auftrags-Dialog.
///
/// Der Reiter war zwar vorhanden und übersetzt, wurde aber nie gefüllt - er
/// blieb dauerhaft leer.
/// </summary>
public partial class AuftragPage
{
    private async Task LadeProtokollAsync()
    {
        LeereProtokoll();

        // Ein noch nicht gespeicherter Auftrag hat kein Protokoll
        if (_isNewTask || _currentTask == null)
        {
            NoLogsLabel.IsVisible = true;
            return;
        }

        try
        {
            var eintraege = await _apiService.GetTaskLogsAsync(_currentTask.Id);
            if (eintraege == null || eintraege.Count == 0)
            {
                NoLogsLabel.IsVisible = true;
                return;
            }

            // Übersetzungstabelle EINMAL je Durchgang aufbauen, nicht je Zeile
            var uebersetzer = new LogTextUebersetzer();
            foreach (var eintrag in eintraege)
                LogsStack.Children.Add(
                    ProtokollAnsicht.Zeile(eintrag.DatumZeit, eintrag.User, uebersetzer.Uebersetzen(eintrag.Text)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Protokoll laden: {ex.Message}");
            NoLogsLabel.Text = Translations.Get("error");
            NoLogsLabel.IsVisible = true;
        }
    }

    /// <summary>
    /// Protokollzeilen entfernen - der Hinweis "keine Einträge" liegt im selben
    /// Bereich und muss stehen bleiben.
    /// </summary>
    private void LeereProtokoll()
    {
        for (int i = LogsStack.Children.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(LogsStack.Children[i], NoLogsLabel))
                LogsStack.Children.RemoveAt(i);
        }
        NoLogsLabel.IsVisible = false;
    }
}
