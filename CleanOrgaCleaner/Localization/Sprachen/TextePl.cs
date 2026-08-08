namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Polnische Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TextePl : LanguagePack
{
    public TextePl() : base("pl", "Polski", "PL", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
    {
        // Navigation
        ["today"] = "Dzisiaj",
        ["chat"] = "Czat",
        ["settings"] = "Ustawienia",
        ["logout"] = "Wyloguj",

        // Chat
        ["message_placeholder"] = "Napisz wiadomość...",
        ["translation_preview"] = "Podgląd tłumaczenia",
        ["your_text"] = "Twój tekst",
        ["translation_for_admin"] = "Tłumaczenie (dla admina)",
        ["back_translation"] = "Tłumaczenie wsteczne",

        // Settings
        ["select_language"] = "Wybierz język",
        ["logged_in_as"] = "Zalogowany jako",
        ["app_info"] = "Info o aplikacji",
        ["version"] = "Wersja",
        ["server"] = "Serwer",
        ["language"] = "Język",
        ["security"] = "Bezpieczeństwo",
        ["biometric_login"] = "Odcisk palca / Twarz",
        ["biometric_hint"] = "Szybkie i bezpieczne logowanie biometryczne",
        ["select_avatar"] = "Wybierz awatar",
        ["avatar_changed"] = "Awatar zmieniony",
        ["tap_to_change"] = "Dotknij, aby zmienić",
        ["change"] = "Zmień",

        // Today / Work Time
        ["no_tasks"] = "Brak zadań na dziś",
        ["cleaning_finished"] = "Przerwa / zakończenie czasu pracy?",
        ["yes"] = "Tak",
        ["no"] = "Nie",
        ["cancel"] = "Anuluj",
        ["ok"] = "OK",

        // Work Time
        ["error"] = "Błąd",
        ["attention"] = "Uwaga",
        ["sync_failed_hint"] = "Akcja zapisana offline nie mogła zostać wysłana i została odrzucona",
        ["unknown_error"] = "Nieznany błąd",
        ["start_work_first"] = "Proszę najpierw kliknąć 'Rozpocznij pracę'.",

        // Task Status
        ["completed"] = "Ukończone",

        // Task Detail
        ["task"] = "Zadanie",
        ["new_task"] = "Nowe Zadanie",
        ["notes"] = "Notatki",
        ["report_problem"] = "Zgłoś problem",
        ["edit_problem"] = "Edytuj problem",
        ["edit_note"] = "Edytuj notatkę",
        ["name"] = "Nazwa",
        ["delete"] = "Usuń",
        ["no_problems"] = "Brak zgłoszonych problemów",
        ["description"] = "Opis",
        ["photos"] = "Zdjęcia",
        ["add_photo"] = "Dodaj zdjęcie",
        ["save"] = "Zapisz",
        ["saved"] = "Zapisano",
        ["delete_problem_title"] = "Usuń problem",
        ["delete_problem_confirm"] = "Na pewno chcesz usunąć ten problem?",
        ["yes_delete"] = "Tak, usuń",
        ["problem_reported"] = "Problem zgłoszony",

        // Images / Notes
        ["add_note"] = "Dodaj notatkę",
        ["no_notes"] = "Brak notatek",
        ["no_logs"] = "Brak wpisów w protokole",
        ["no_task_description"] = "Brak opisu zadania",
        ["camera"] = "Aparat",
        ["gallery"] = "Galeria",
        ["note"] = "Notatka",

        // Buttons
        ["start"] = "Start",
        ["stop"] = "Stop",

        // General
        ["loading"] = "Ładowanie...",
        ["connection_error"] = "Błąd połączenia",
        ["no_connection"] = "Brak połączenia",
        ["saved_offline"] = "Zapisano. Zsynchronizuje się po połączeniu.",
        ["really_logout"] = "Na pewno chcesz się wylogować?",
        ["task_completed"] = "Zakończ zadanie",
        ["task_completed_question"] = "Na pewno chcesz zakończyć to zadanie?",
        ["log"] = "Dziennik",
        ["delete_task"] = "Usuń zadanie",

        // My Tasks
        ["create_auftrag"] = "Nowe Zadanie",
        ["edit_auftrag"] = "Edytuj Zadanie",
        ["messages"] = "Wiadomości",
        ["administration"] = "Administracja",
        ["colleagues"] = "Koledzy",
        ["colleague"] = "Kolega",
        ["admin_contact"] = "Administracja",
        ["task_name_required"] = "Nazwa zadania *",
        ["apartment"] = "Apartament",
        ["date_required"] = "Data *",
        ["task_type"] = "Typ zadania",
        ["optional_hint"] = "Opis zadania...",
        ["assign_cleaners"] = "Przypisz sprzątaczy",
        ["cleaning"] = "Sprzątanie",
        ["check_task"] = "Sprawdzenie",
        ["repair"] = "Naprawa",
        ["details_tab"] = "Szczegóły",
        ["task_tab"] = "Zadanie",
        ["problems_tab"] = "Problemy",
        ["notes_tab"] = "Notatki",
        ["assign_tab"] = "Przypisz",
        ["no_my_tasks"] = "Brak własnych zadań",
        ["task_create_error"] = "Błąd przy tworzeniu zadania",
        ["task_update_error"] = "Błąd przy aktualizacji zadania",
        ["task_delete_error"] = "Błąd przy usuwaniu zadania",
        ["confirm_delete_task"] = "Na pewno chcesz usunąć to zadanie?",
        ["update_error"] = "Błąd aktualizacji",
        ["delete_error"] = "Błąd usuwania",
        ["delete_image"] = "Usuń obraz",
        ["confirm_delete_image"] = "Na pewno chcesz usunąć ten obraz?",

        // Log translations
        ["log_note_added"] = "Notatka dodana",
        ["log_image_deleted"] = "Obraz usunięty",
        ["log_problem_reported"] = "Problem zgłoszony",
        ["log_problem_deleted"] = "Problem usunięty",
        ["log_task_created"] = "Zadanie utworzone",
        ["log_task_updated"] = "Zadanie zaktualizowane",
        ["log_repair_task_created"] = "Zadanie naprawcze utworzone",
        ["log_cleaning_assigned_to"] = "Sprzątanie przypisane do",
        ["log_assignment_removed"] = "Przypisanie usunięte",
        ["log_progress"] = "Postęp",
        ["log_status_changed"] = "Status zmieniony",
        ["log_checklist_updated"] = "Lista zaktualizowana",
        ["log_not_started"] = "Nierozpoczęte",
        ["log_started"] = "Rozpoczęte",
        ["log_completed"] = "Zakończone",

        // Login Screen
        ["login_subtitle"] = "Zarządzanie sprzątaniem",
        ["login_enterprise_app"] = "Aplikacja firmowa:",
        ["login_credentials_info"] = "Uzyskaj dane logowania od administratora.",
        ["login_new_customers"] = "Nowi klienci:",
        ["login_registration_info"] = "Rejestracja przez email: mail@schwanenburg.de",
        ["login_test_usage"] = "Użycie testowe:",
        ["login_test_credentials"] = "Property: 1  |  User: tom  |  Hasło: tom",
        ["login_title"] = "Logowanie",
        ["login_property_id"] = "ID nieruchomości",
        ["login_username"] = "Użytkownik",
        ["login_password"] = "Hasło",
        ["login_remember_me"] = "Zapamiętaj mnie",

        // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
        ["offline"] = "Offline",
        ["create_error"] = "Błąd podczas tworzenia",
        ["save_error"] = "Błąd podczas zapisywania",
        ["delete_chat_title"] = "Usuń czat",
        ["delete_chat_confirm"] = "Usunąć wszystkie wiadomości? Tej akcji nie można cofnąć.",
        ["delete_image_confirm"] = "Usunąć obraz z tej wiadomości?",
        ["delete_note"] = "Usuń notatkę",
        ["delete_note_confirm"] = "Czy na pewno usunąć tę notatkę?",
        ["log_note_created"] = "Notatka utworzona",
        ["message_from"] = "Od",
        ["notifications"] = "Powiadomienia",
        ["push_notifications"] = "Powiadomienia push",
        ["enabled"] = "Włączone",
        ["not_enabled"] = "Niewłączone",
        ["disabled"] = "Wyłączone",
        ["not_active"] = "Nieaktywne",
        ["notifications_denied_hint"] = "Powiadomienia nie są aktywne. Zezwól na nie dla CleanOrga w ustawieniach urządzenia.",
        ["open_settings"] = "Otwórz ustawienia",
        ["name_required"] = "Podaj nazwę",
        ["network_error_hint"] = "Błąd sieci. Połącz się z WiFi lub danymi mobilnymi.",
        ["select_image_source"] = "Wybierz obraz",
    };
}
