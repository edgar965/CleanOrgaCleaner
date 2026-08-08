namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Rumänische Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TexteRo : LanguagePack
{
    public TexteRo() : base("ro", "Română", "RO", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
    {
        // Navigation
        ["today"] = "Astăzi",
        ["chat"] = "Chat",
        ["settings"] = "Setări",
        ["logout"] = "Deconectare",

        // Chat
        ["message_placeholder"] = "Scrie un mesaj...",
        ["translation_preview"] = "Previzualizare traducere",
        ["your_text"] = "Textul tău",
        ["translation_for_admin"] = "Traducere (pentru admin)",
        ["back_translation"] = "Traducere inversă",

        // Settings
        ["select_language"] = "Selectează limba",
        ["logged_in_as"] = "Conectat ca",
        ["app_info"] = "Info aplicație",
        ["version"] = "Versiune",
        ["server"] = "Server",
        ["language"] = "Limbă",
        ["security"] = "Securitate",
        ["biometric_login"] = "Amprentă / Față",
        ["biometric_hint"] = "Autentificare rapidă și sigură cu biometrie",
        ["select_avatar"] = "Selectează avatar",
        ["avatar_changed"] = "Avatar schimbat",
        ["tap_to_change"] = "Atinge pentru a schimba",
        ["change"] = "Schimbă",

        // Today / Work Time
        ["no_tasks"] = "Nicio sarcină pentru astăzi",
        ["cleaning_finished"] = "Pauză / încheiere timp de lucru?",
        ["yes"] = "Da",
        ["no"] = "Nu",
        ["cancel"] = "Anulare",
        ["ok"] = "OK",

        // Work Time
        ["error"] = "Eroare",
        ["attention"] = "Atenție",
        ["sync_failed_hint"] = "O acțiune înregistrată offline nu a putut fi trimisă și a fost eliminată",
        ["unknown_error"] = "Eroare necunoscută",
        ["start_work_first"] = "Te rugăm să apeși întâi pe 'Începe lucrul'.",

        // Task Status
        ["completed"] = "Finalizat",

        // Task Detail
        ["task"] = "Sarcină",
        ["new_task"] = "Sarcină Nouă",
        ["notes"] = "Note",
        ["report_problem"] = "Raportează problemă",
        ["edit_problem"] = "Editează problemă",
        ["edit_note"] = "Editează notă",
        ["name"] = "Nume",
        ["delete"] = "Șterge",
        ["no_problems"] = "Nicio problemă raportată",
        ["description"] = "Descriere",
        ["photos"] = "Fotografii",
        ["add_photo"] = "Adaugă fotografie",
        ["save"] = "Salvează",
        ["saved"] = "Salvat",
        ["delete_problem_title"] = "Șterge problema",
        ["delete_problem_confirm"] = "Sigur vrei să ștergi această problemă?",
        ["yes_delete"] = "Da, șterge",
        ["problem_reported"] = "Problemă raportată",

        // Images / Notes
        ["add_note"] = "Adaugă notă",
        ["no_notes"] = "Nicio notă",
        ["no_logs"] = "Nicio înregistrare în jurnal",
        ["no_task_description"] = "Nu există descriere a sarcinii",
        ["camera"] = "Cameră",
        ["gallery"] = "Galerie",
        ["note"] = "Notă",

        // Buttons
        ["start"] = "Start",
        ["stop"] = "Stop",

        // General
        ["loading"] = "Se încarcă...",
        ["connection_error"] = "Eroare de conexiune",
        ["no_connection"] = "Fără conexiune",
        ["saved_offline"] = "Salvat. Se va sincroniza când va fi conexiune.",
        ["really_logout"] = "Sigur vrei să te deconectezi?",
        ["task_completed"] = "Finalizează sarcina",
        ["task_completed_question"] = "Sigur vrei să finalizezi această sarcină?",
        ["log"] = "Jurnal",
        ["delete_task"] = "Șterge sarcina",

        // My Tasks
        ["create_auftrag"] = "Sarcină Nouă",
        ["edit_auftrag"] = "Editare Sarcină",
        ["messages"] = "Mesaje",
        ["administration"] = "Administrație",
        ["colleagues"] = "Colegi",
        ["colleague"] = "Coleg",
        ["admin_contact"] = "Administrație",
        ["task_name_required"] = "Nume sarcină *",
        ["apartment"] = "Apartament",
        ["date_required"] = "Data *",
        ["task_type"] = "Tip sarcină",
        ["optional_hint"] = "Descrierea sarcinii...",
        ["assign_cleaners"] = "Atribuie curățători",
        ["cleaning"] = "Curățenie",
        ["check_task"] = "Verificare",
        ["repair"] = "Reparație",
        ["details_tab"] = "Detalii",
        ["task_tab"] = "Sarcină",
        ["problems_tab"] = "Probleme",
        ["notes_tab"] = "Note",
        ["assign_tab"] = "Atribuie",
        ["no_my_tasks"] = "Nicio sarcină proprie",
        ["task_create_error"] = "Eroare la crearea sarcinii",
        ["task_update_error"] = "Eroare la actualizarea sarcinii",
        ["task_delete_error"] = "Eroare la ștergerea sarcinii",
        ["confirm_delete_task"] = "Sigur vrei să ștergi această sarcină?",
        ["update_error"] = "Eroare la actualizare",
        ["delete_error"] = "Eroare la ștergere",
        ["delete_image"] = "Șterge imagine",
        ["confirm_delete_image"] = "Sigur vrei să ștergi această imagine?",

        // Log translations
        ["log_note_added"] = "Notă adăugată",
        ["log_image_deleted"] = "Imagine ștearsă",
        ["log_problem_reported"] = "Problemă raportată",
        ["log_problem_deleted"] = "Problemă ștearsă",
        ["log_task_created"] = "Sarcină creată",
        ["log_task_updated"] = "Sarcină actualizată",
        ["log_repair_task_created"] = "Sarcină de reparație creată",
        ["log_cleaning_assigned_to"] = "Curățenie atribuită la",
        ["log_assignment_removed"] = "Atribuire eliminată",
        ["log_progress"] = "Progres",
        ["log_status_changed"] = "Stare schimbată",
        ["log_checklist_updated"] = "Listă actualizată",
        ["log_not_started"] = "Neînceput",
        ["log_started"] = "Început",
        ["log_completed"] = "Finalizat",

        // Login Screen
        ["login_subtitle"] = "Management curățenie",
        ["login_enterprise_app"] = "Aplicație firmă:",
        ["login_credentials_info"] = "Obține credențialele de la administrator.",
        ["login_new_customers"] = "Clienți noi:",
        ["login_registration_info"] = "Înregistrare prin email: mail@schwanenburg.de",
        ["login_test_usage"] = "Utilizare test:",
        ["login_test_credentials"] = "Property: 1  |  User: tom  |  Parolă: tom",
        ["login_title"] = "Autentificare",
        ["login_property_id"] = "ID Proprietate",
        ["login_username"] = "Utilizator",
        ["login_password"] = "Parolă",
        ["login_remember_me"] = "Rămâi conectat",

        // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
        ["offline"] = "Offline",
        ["create_error"] = "Eroare la creare",
        ["save_error"] = "Eroare la salvare",
        ["delete_chat_title"] = "Șterge conversația",
        ["delete_chat_confirm"] = "Ștergi toate mesajele? Această acțiune nu poate fi anulată.",
        ["delete_image_confirm"] = "Ștergi imaginea din acest mesaj?",
        ["delete_note"] = "Șterge nota",
        ["delete_note_confirm"] = "Sigur vrei să ștergi această notă?",
        ["log_note_created"] = "Notă creată",
        ["message_from"] = "De la",
        ["notifications"] = "Notificări",
        ["push_notifications"] = "Notificări push",
        ["enabled"] = "Activat",
        ["not_enabled"] = "Neactivat",
        ["disabled"] = "Dezactivat",
        ["not_active"] = "Inactiv",
        ["notifications_denied_hint"] = "Notificările nu sunt active. Vă rugăm să le permiteți pentru CleanOrga în setările dispozitivului.",
        ["open_settings"] = "Deschide setările",
        ["name_required"] = "Introdu un nume",
        ["network_error_hint"] = "Eroare de rețea. Conectează-te la WiFi sau date mobile.",
        ["select_image_source"] = "Selectează imaginea",
    };
}
