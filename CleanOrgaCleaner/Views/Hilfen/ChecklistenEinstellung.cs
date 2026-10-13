namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Ob der Checkliste-Reiter bei Aufgaben angezeigt wird - rein lokale
/// Geräte-Einstellung (kein Server-Zustand nötig). Standard: aus.
/// </summary>
public static class ChecklistenEinstellung
{
    private const string PreferenceKey = "show_checklist";

    public static bool Aktiv => Preferences.Get(PreferenceKey, false);

    public static void Setzen(bool aktiv) => Preferences.Set(PreferenceKey, aktiv);
}
