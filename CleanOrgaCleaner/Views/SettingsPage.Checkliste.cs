using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Schalter "Checkliste anzeigen" in den Einstellungen - rein lokal
/// (Preferences), kein Server-Aufruf nötig.
/// </summary>
public partial class SettingsPage
{
    // Verhindert, dass das programmatische Setzen des Schalters beim Laden
    // den Toggled-Handler auslöst und sich selbst wieder speichert.
    private bool _checklisteSetzenLaeuft;

    private void LadeChecklistenEinstellung()
    {
        _checklisteSetzenLaeuft = true;
        ShowChecklistSwitch.IsToggled = ChecklistenEinstellung.Aktiv;
        _checklisteSetzenLaeuft = false;
    }

    private void OnShowChecklistToggled(object? sender, ToggledEventArgs e)
    {
        if (_checklisteSetzenLaeuft)
            return;

        ChecklistenEinstellung.Setzen(e.Value);
    }
}
