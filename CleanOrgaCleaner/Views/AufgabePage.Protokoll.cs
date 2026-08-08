using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter "Protokoll": was an dieser Aufgabe wann passiert ist.
/// </summary>
public partial class AufgabePage
{
    private async Task LadeProtokollAsync()
    {
        try
        {
            LogsStack.Children.Clear();
            NoLogsLabel.IsVisible = false;

            var eintraege = await _apiService.GetTaskLogsAsync(_taskId);
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
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Protokoll laden: {ex.Message}");
            NoLogsLabel.Text = Translations.Get("error");
            NoLogsLabel.IsVisible = true;
        }
    }
}
