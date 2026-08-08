using CleanOrgaCleaner.Localization;

namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Übersetzt die vom Server auf Deutsch gelieferten Protokolltexte.
///
/// Vorher baute die Seite die Ersetzungstabelle für JEDE Protokollzeile neu auf
/// (16 Wörterbuch-Einträge mal Zeilenzahl). Jetzt entsteht sie einmal je
/// Durchgang; die Seite legt dafür ein Exemplar dieser Klasse an.
/// </summary>
public sealed class LogTextUebersetzer
{
    /// <summary>Deutsche Vorlage aus der Datenbank -> Schlüssel der Übersetzung.</summary>
    private static readonly (string Vorlage, string Schluessel)[] Zuordnung =
    {
        ("Anmerkung hinzugefügt", "log_note_added"),
        ("Anmerkung erstellt", "log_note_created"),
        ("Bild gelöscht", "log_image_deleted"),
        ("Problem gemeldet", "log_problem_reported"),
        ("Problem gelöscht", "log_problem_deleted"),
        ("Aufgabe erstellt", "log_task_created"),
        ("Aufgabe aktualisiert", "log_task_updated"),
        ("Reparatur-Aufgabe erstellt", "log_repair_task_created"),
        ("Reinigung zugewiesen an", "log_cleaning_assigned_to"),
        ("Zuweisung entfernt", "log_assignment_removed"),
        ("Fortschritt:", "log_progress"),
        ("Status geändert:", "log_status_changed"),
        ("Checkliste aktualisiert", "log_checklist_updated"),
        ("Nicht gestartet", "log_not_started"),
        ("Gestartet", "log_started"),
        ("Abgeschlossen", "log_completed")
    };

    /// <summary>Vorlagen mit Doppelpunkt: der gehört nicht zur Übersetzung.</summary>
    private static readonly HashSet<string> MitDoppelpunkt = new() { "log_progress", "log_status_changed" };

    private readonly List<(string Vorlage, string Ersatz)> _ersetzungen = new();

    public LogTextUebersetzer()
    {
        foreach (var (vorlage, schluessel) in Zuordnung)
        {
            var ersatz = Translations.Get(schluessel);
            if (MitDoppelpunkt.Contains(schluessel))
                ersatz += ":";

            // Nur ersetzen, wenn die Übersetzung wirklich etwas anderes sagt
            if (!string.IsNullOrEmpty(ersatz) && ersatz != vorlage)
                _ersetzungen.Add((vorlage, ersatz));
        }
    }

    public string Uebersetzen(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var ergebnis = text;
        foreach (var (vorlage, ersatz) in _ersetzungen)
            ergebnis = ergebnis.Replace(vorlage, ersatz);
        return ergebnis;
    }
}
