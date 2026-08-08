namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Deutsche Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TexteDe : LanguagePack
{
    public TexteDe() : base("de", "Deutsch", "DE", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
    {
        // Navigation
        ["today"] = "Heute",
        ["chat"] = "Chat",
        ["settings"] = "Einstellungen",
        ["logout"] = "Abmelden",

        // Chat
        ["message_placeholder"] = "Nachricht eingeben...",
        ["message_from"] = "Von",
        ["notifications"] = "Mitteilungen",
        ["push_notifications"] = "Push-Mitteilungen",
        ["enabled"] = "Aktiviert",
        ["not_enabled"] = "Nicht aktiviert",
        ["disabled"] = "Deaktiviert",
        ["not_active"] = "Nicht aktiv",
        ["notifications_denied_hint"] = "Mitteilungen sind nicht aktiv. Bitte in den Geräte-Einstellungen für CleanOrga erlauben.",
        ["open_settings"] = "Einstellungen öffnen",
        ["translation_preview"] = "Übersetzungsvorschau",
        ["your_text"] = "Dein Text",
        ["translation_for_admin"] = "Übersetzung (für Admin)",
        ["back_translation"] = "Rückübersetzung",
        ["delete_image_confirm"] = "Bild aus dieser Nachricht entfernen?",
        ["delete_note"] = "Anmerkung löschen",
        ["delete_note_confirm"] = "Möchtest du diese Anmerkung wirklich löschen?",
        ["select_image_source"] = "Bild auswählen",

        // Einstellungen
        ["select_language"] = "Sprache auswählen",
        ["logged_in_as"] = "Angemeldet als",
        ["app_info"] = "App Info",
        ["version"] = "Version",
        ["server"] = "Server",
        ["language"] = "Sprache",
        ["security"] = "Sicherheit",
        ["biometric_login"] = "Fingerabdruck / Gesicht",
        ["biometric_hint"] = "Schnelle und sichere Anmeldung mit Biometrie",
        ["select_avatar"] = "Avatar wählen",
        ["avatar_changed"] = "Avatar wurde geändert",
        ["tap_to_change"] = "Tippen zum Ändern",
        ["change"] = "Ändern",

        // Heute / Arbeitszeit
        ["no_tasks"] = "Keine Aufgaben für heute",
        ["cleaning_finished"] = "Arbeitszeit unterbrechen / beenden?",
        ["yes"] = "Ja",
        ["no"] = "Nein",
        ["cancel"] = "Abbrechen",
        ["ok"] = "OK",

        // Arbeitszeit
        ["error"] = "Fehler",
        ["attention"] = "Achtung",
        ["sync_failed_hint"] = "Eine offline erfasste Aktion konnte nicht gesendet werden und wurde verworfen",
        ["unknown_error"] = "Unbekannter Fehler",
        ["start_work_first"] = "Bitte klicke zuerst auf 'Start Arbeit', damit die Arbeitszeit richtig erfasst wird.",

        // Task Status
        ["completed"] = "Abgeschlossen",

        // Task Detail
        ["task"] = "Aufgabe",
        ["new_task"] = "Neue Aufgabe",
        ["notes"] = "Anmerkungen",
        ["report_problem"] = "Problem melden",
        ["edit_problem"] = "Problem bearbeiten",
        ["edit_note"] = "Anmerkung bearbeiten",
        ["delete"] = "Löschen",
        ["no_problems"] = "Keine Probleme gemeldet",
        ["description"] = "Beschreibung",
        ["photos"] = "Fotos",
        ["add_photo"] = "Foto hinzufügen",
        ["save"] = "Speichern",
        ["saved"] = "Gespeichert",
        ["delete_problem_title"] = "Problem löschen",
        ["delete_problem_confirm"] = "Möchtest du dieses Problem wirklich löschen?",
        ["yes_delete"] = "Ja, löschen",
        ["problem_reported"] = "Problem wurde gemeldet",

        // Bilder / Anmerkungen
        ["add_note"] = "Anmerkung hinzufügen",
        ["no_notes"] = "Keine Anmerkungen",
        ["no_logs"] = "Keine Protokolleinträge vorhanden",
        ["no_task_description"] = "Keine Aufgabenbeschreibung vorhanden",
        ["camera"] = "Kamera",
        ["gallery"] = "Galerie",
        ["note"] = "Anmerkung",

        // Buttons
        ["start"] = "Start",
        ["stop"] = "Beenden",

        // Allgemein
        ["loading"] = "Laden...",
        ["connection_error"] = "Verbindungsfehler",
        ["no_connection"] = "Keine Verbindung",
        ["network_error_hint"] = "Netzwerkfehler. Bitte verbinden Sie sich mit WLAN oder mobilen Daten.",
        ["saved_offline"] = "Gespeichert. Wird bei Verbindung synchronisiert.",
        ["really_logout"] = "Möchtest du dich wirklich abmelden?",
        ["task_completed"] = "Aufgabe abschließen",
        ["task_completed_question"] = "Möchtest du diese Aufgabe wirklich abschließen?",
        ["log"] = "Log",
        ["delete_task"] = "Aufgabe löschen",

        // Neue Aufgabe / My Tasks
        ["create_auftrag"] = "Neue Aufgabe",
        ["edit_auftrag"] = "Aufgabe bearbeiten",
        ["messages"] = "Nachrichten",
        ["administration"] = "Verwaltung",
        ["colleagues"] = "Kollegen",
        ["colleague"] = "Kollege",
        ["admin_contact"] = "Verwaltung",
        ["task_name_required"] = "Aufgabenname *",
        ["apartment"] = "Apartment",
        ["date_required"] = "Datum *",
        ["task_type"] = "Aufgabenart",
        ["optional_hint"] = "Beschreibung der Aufgabe...",
        ["assign_cleaners"] = "Cleaner zuweisen",
        ["cleaning"] = "Putzen",
        ["check_task"] = "Check",
        ["repair"] = "Reparatur",
        ["details_tab"] = "Details",
        ["task_tab"] = "Aufgabe",
        ["problems_tab"] = "Probleme",
        ["notes_tab"] = "Anmerkungen",
        ["assign_tab"] = "Zuweisen",
        ["no_my_tasks"] = "Keine eigenen Aufgaben",
        ["task_create_error"] = "Fehler beim Erstellen der Aufgabe",
        ["task_update_error"] = "Fehler beim Aktualisieren der Aufgabe",
        ["task_delete_error"] = "Fehler beim Löschen der Aufgabe",
        ["confirm_delete_task"] = "Möchtest du diese Aufgabe wirklich löschen?",
        ["update_error"] = "Fehler beim Aktualisieren",
        ["delete_error"] = "Fehler beim Löschen",
        ["delete_image"] = "Bild löschen",
        ["confirm_delete_image"] = "Möchtest du dieses Bild wirklich löschen?",

        // Validation messages
        ["name_required"] = "Bitte gib einen Namen ein",
        ["name"] = "Name",

        // Log translations
        ["log_note_added"] = "Anmerkung hinzugefügt",
        ["log_note_created"] = "Anmerkung erstellt",
        ["log_image_deleted"] = "Bild gelöscht",
        ["log_problem_reported"] = "Problem gemeldet",
        ["log_problem_deleted"] = "Problem gelöscht",
        ["log_task_created"] = "Aufgabe erstellt",
        ["log_task_updated"] = "Aufgabe aktualisiert",
        ["log_repair_task_created"] = "Reparatur-Aufgabe erstellt",
        ["log_cleaning_assigned_to"] = "Reinigung zugewiesen an",
        ["log_assignment_removed"] = "Zuweisung entfernt",
        ["log_progress"] = "Fortschritt",
        ["log_status_changed"] = "Status geändert",
        ["log_checklist_updated"] = "Checkliste aktualisiert",
        ["log_not_started"] = "Nicht gestartet",
        ["log_started"] = "Gestartet",
        ["log_completed"] = "Abgeschlossen",

        // Login Screen
        ["login_subtitle"] = "Reinigungsmanagement",
        ["login_enterprise_app"] = "Firmen-App:",
        ["login_credentials_info"] = "Ihre Zugangsdaten erhalten Sie von Ihrem Administrator.",
        ["login_new_customers"] = "Neukunden:",
        ["login_registration_info"] = "Registrierung per E-Mail an: mail@schwanenburg.de",
        ["login_test_usage"] = "Test-Nutzung:",
        ["login_test_credentials"] = "Property: 1  |  User: tom  |  Passwort: tom",
        ["login_title"] = "Anmelden",
        ["login_property_id"] = "Property ID",
        ["login_username"] = "Benutzername",
        ["login_password"] = "Passwort",
        ["login_remember_me"] = "Angemeldet bleiben",

        // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
        ["offline"] = "Offline",
        ["create_error"] = "Fehler beim Erstellen",
        ["save_error"] = "Fehler beim Speichern",
        ["delete_chat_title"] = "Chat löschen",
        ["delete_chat_confirm"] = "Alle Nachrichten löschen? Diese Aktion kann nicht rückgängig gemacht werden.",
    };
}
