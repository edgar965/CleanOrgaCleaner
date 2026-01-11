namespace CleanOrgaCleaner.Localization;

/// <summary>
/// UI translations for all supported languages
/// Based on Django mobile translations.js
/// </summary>
public static class Translations
{
    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["de"] = new Dictionary<string, string>
        {
            // Navigation
            ["overview"] = "Übersicht",
            ["today"] = "Heute",
            ["chat"] = "Chat",
            ["settings"] = "Einstellungen",
            ["logout"] = "Abmelden",

            // Chat
            ["chat_title"] = "Chat",
            ["message_placeholder"] = "Nachricht eingeben...",
            ["send"] = "Senden",
            ["preview"] = "Vorschau",
            ["no_messages"] = "Noch keine Nachrichten",
            ["new_message"] = "Neue Nachricht",
            ["translation_preview"] = "Übersetzungsvorschau",
            ["your_text"] = "Dein Text",
            ["translation_for_admin"] = "Übersetzung (für Admin)",
            ["back_translation"] = "Rückübersetzung",

            // Einstellungen
            ["settings_title"] = "Einstellungen",
            ["select_language"] = "Sprache auswählen",
            ["logged_in_as"] = "Angemeldet als",
            ["app_info"] = "App Info",
            ["version"] = "Version",
            ["server"] = "Server",
            ["language"] = "Sprache",
            ["language_hint"] = "Die Sprache wird für Chat-Übersetzungen verwendet",
            ["language_saved"] = "Sprache wurde geändert",
            ["exit_app"] = "App beenden",

            // Heute / Arbeitszeit
            ["start_work"] = "Start Arbeit",
            ["stop_work"] = "Stop Arbeit",
            ["working_since"] = "Arbeitet seit",
            ["no_tasks"] = "Keine Aufgaben für heute",
            ["cleaning_finished"] = "Wurde die Reinigung vollständig beendet?",
            ["yes"] = "Ja",
            ["no"] = "Nein",
            ["cancel"] = "Abbrechen",
            ["ok"] = "OK",

            // Arbeitszeit
            ["work_ended"] = "Arbeitszeit beendet!",
            ["total_hours"] = "Gesamtstunden",
            ["error"] = "Fehler",
            ["unknown_error"] = "Unbekannter Fehler",
            ["start_work_first"] = "Bitte klicke zuerst auf 'Start Arbeit', damit die Arbeitszeit richtig erfasst wird.",
            ["tasks_today"] = "Aufgaben heute",
            ["hours"] = "Stunden",
            ["work_started"] = "Arbeit gestartet",
            ["work_could_not_start"] = "Arbeit konnte nicht gestartet werden",
            ["work_could_not_end"] = "Arbeit konnte nicht beendet werden",

            // Task Status
            ["status"] = "Status",
            ["open"] = "Offen",
            ["running"] = "Läuft",
            ["done"] = "Fertig",
            ["not_started"] = "Nicht gestartet",
            ["started"] = "Gestartet",
            ["completed"] = "Abgeschlossen",

            // Task Detail
            ["task"] = "Aufgabe",
            ["important_notice"] = "Wichtiger Hinweis",
            ["notes"] = "Anmerkungen",
            ["my_notes"] = "Meine Anmerkung",
            ["enter_note"] = "Notizen zur Aufgabe...",
            ["note_placeholder"] = "Anmerkung eingeben...",
            ["checklist"] = "Checkliste",
            ["x_of_y_completed"] = "erledigt",
            ["problems"] = "Probleme",
            ["report_problem"] = "Problem melden",
            ["delete"] = "Löschen",
            ["no_problems"] = "Keine Probleme gemeldet",
            ["problem_name"] = "Name des Problems",
            ["description"] = "Beschreibung",
            ["description_optional"] = "Beschreibung (optional)",
            ["photos"] = "Fotos",
            ["multiple_photos_hint"] = "Mehrere Fotos möglich",
            ["save"] = "Speichern",
            ["saved"] = "Gespeichert",
            ["delete_problem_title"] = "Problem löschen",
            ["delete_problem_confirm"] = "Möchtest du dieses Problem wirklich löschen?",
            ["yes_delete"] = "Ja, löschen",
            ["problem_reported"] = "Problem wurde gemeldet",

            // Bilder
            ["images"] = "Bilder",
            ["image_gallery"] = "Bilder Galerie",
            ["add_image"] = "Bild hinzufügen",
            ["camera"] = "Kamera",
            ["gallery"] = "Galerie",
            ["image_details"] = "Bild Details",
            ["note_for_image"] = "Notiz zum Bild...",
            ["no_photos_selected"] = "Keine Fotos ausgewählt",
            ["select_image"] = "Bild auswählen",
            ["note"] = "Notiz",

            // Buttons
            ["start"] = "Start",
            ["stop"] = "Beenden",

            // Allgemein
            ["loading"] = "Laden...",
            ["connection_error"] = "Verbindungsfehler",
            ["task_not_found"] = "Aufgabe nicht gefunden",
            ["notes_saved"] = "Notizen wurden gespeichert",
            ["status_changed"] = "Status wurde geändert",
            ["hint"] = "Hinweis",
            ["please_enter_message"] = "Bitte Nachricht eingeben",
            ["no_translation_needed"] = "Keine Übersetzung nötig",
            ["message_could_not_send"] = "Nachricht konnte nicht gesendet werden",
            ["really_logout"] = "Möchtest du dich wirklich abmelden?",
            ["task_completed"] = "Aufgabe abschließen",
            ["task_completed_question"] = "Möchtest du diese Aufgabe wirklich abschließen?"
        },

        ["en"] = new Dictionary<string, string>
        {
            ["overview"] = "Overview",
            ["today"] = "Today",
            ["chat"] = "Chat",
            ["settings"] = "Settings",
            ["logout"] = "Logout",

            ["chat_title"] = "Chat",
            ["message_placeholder"] = "Enter message...",
            ["send"] = "Send",
            ["preview"] = "Preview",
            ["no_messages"] = "No messages yet",
            ["new_message"] = "New message",
            ["translation_preview"] = "Translation preview",
            ["your_text"] = "Your text",
            ["translation_for_admin"] = "Translation (for admin)",
            ["back_translation"] = "Back translation",

            ["settings_title"] = "Settings",
            ["select_language"] = "Select language",
            ["logged_in_as"] = "Logged in as",
            ["app_info"] = "App Info",
            ["version"] = "Version",
            ["server"] = "Server",
            ["language"] = "Language",
            ["language_hint"] = "The language is used for chat translations",
            ["language_saved"] = "Language changed",
            ["exit_app"] = "Exit app",

            ["start_work"] = "Start Work",
            ["stop_work"] = "Stop Work",
            ["working_since"] = "Working since",
            ["no_tasks"] = "No tasks for today",
            ["cleaning_finished"] = "Was the cleaning completed?",
            ["yes"] = "Yes",
            ["no"] = "No",
            ["cancel"] = "Cancel",
            ["ok"] = "OK",

            ["work_ended"] = "Work time ended!",
            ["total_hours"] = "Total hours",
            ["error"] = "Error",
            ["unknown_error"] = "Unknown error",
            ["start_work_first"] = "Please click 'Start Work' first so that the working time is recorded correctly.",
            ["tasks_today"] = "Tasks today",
            ["hours"] = "hours",
            ["work_started"] = "Work started",
            ["work_could_not_start"] = "Could not start work",
            ["work_could_not_end"] = "Could not end work",

            ["status"] = "Status",
            ["open"] = "Open",
            ["running"] = "Running",
            ["done"] = "Done",
            ["not_started"] = "Not started",
            ["started"] = "Started",
            ["completed"] = "Completed",

            ["task"] = "Task",
            ["important_notice"] = "Important notice",
            ["notes"] = "Notes",
            ["my_notes"] = "My notes",
            ["enter_note"] = "Notes for task...",
            ["note_placeholder"] = "Enter note...",
            ["checklist"] = "Checklist",
            ["x_of_y_completed"] = "completed",
            ["problems"] = "Problems",
            ["report_problem"] = "Report problem",
            ["delete"] = "Delete",
            ["no_problems"] = "No problems reported",
            ["problem_name"] = "Problem name",
            ["description"] = "Description",
            ["description_optional"] = "Description (optional)",
            ["photos"] = "Photos",
            ["multiple_photos_hint"] = "Multiple photos possible",
            ["save"] = "Save",
            ["saved"] = "Saved",
            ["delete_problem_title"] = "Delete problem",
            ["delete_problem_confirm"] = "Do you really want to delete this problem?",
            ["yes_delete"] = "Yes, delete",
            ["problem_reported"] = "Problem reported",

            // Images
            ["images"] = "Images",
            ["image_gallery"] = "Image Gallery",
            ["add_image"] = "Add image",
            ["camera"] = "Camera",
            ["gallery"] = "Gallery",
            ["image_details"] = "Image details",
            ["note_for_image"] = "Note for image...",
            ["no_photos_selected"] = "No photos selected",
            ["select_image"] = "Select image",
            ["note"] = "Note",

            // Buttons
            ["start"] = "Start",
            ["stop"] = "Finish",

            ["loading"] = "Loading...",
            ["connection_error"] = "Connection error",
            ["task_not_found"] = "Task not found",
            ["notes_saved"] = "Notes saved",
            ["status_changed"] = "Status changed",
            ["hint"] = "Hint",
            ["please_enter_message"] = "Please enter message",
            ["no_translation_needed"] = "No translation needed",
            ["message_could_not_send"] = "Message could not be sent",
            ["really_logout"] = "Do you really want to logout?",
            ["task_completed"] = "Complete task",
            ["task_completed_question"] = "Do you really want to complete this task?"
        },

        ["es"] = new Dictionary<string, string>
        {
            ["overview"] = "Resumen",
            ["today"] = "Hoy",
            ["chat"] = "Chat",
            ["settings"] = "Configuración",
            ["logout"] = "Cerrar sesión",
            ["start_work"] = "Iniciar trabajo",
            ["stop_work"] = "Parar trabajo",
            ["no_tasks"] = "Sin tareas para hoy",
            ["yes"] = "Sí",
            ["no"] = "No",
            ["cancel"] = "Cancelar",
            ["ok"] = "OK",
            ["error"] = "Error",
            ["save"] = "Guardar",
            ["saved"] = "Guardado",
            ["delete"] = "Eliminar",
            ["loading"] = "Cargando...",
            ["send"] = "Enviar",
            ["preview"] = "Vista previa",
            ["status"] = "Estado",
            ["open"] = "Abierto",
            ["running"] = "En curso",
            ["done"] = "Terminado",
            ["checklist"] = "Lista de verificación",
            ["problems"] = "Problemas",
            ["notes"] = "Notas"
        },

        ["ro"] = new Dictionary<string, string>
        {
            ["overview"] = "Prezentare",
            ["today"] = "Astăzi",
            ["chat"] = "Chat",
            ["settings"] = "Setări",
            ["logout"] = "Deconectare",
            ["start_work"] = "Începe lucrul",
            ["stop_work"] = "Oprește lucrul",
            ["no_tasks"] = "Nicio sarcină pentru astăzi",
            ["yes"] = "Da",
            ["no"] = "Nu",
            ["cancel"] = "Anulare",
            ["ok"] = "OK",
            ["error"] = "Eroare",
            ["save"] = "Salvează",
            ["saved"] = "Salvat",
            ["delete"] = "Șterge",
            ["loading"] = "Se încarcă...",
            ["send"] = "Trimite",
            ["preview"] = "Previzualizare",
            ["status"] = "Stare",
            ["open"] = "Deschis",
            ["running"] = "În desfășurare",
            ["done"] = "Terminat",
            ["checklist"] = "Listă de verificare",
            ["problems"] = "Probleme",
            ["notes"] = "Note"
        },

        ["pl"] = new Dictionary<string, string>
        {
            ["overview"] = "Przegląd",
            ["today"] = "Dzisiaj",
            ["chat"] = "Czat",
            ["settings"] = "Ustawienia",
            ["logout"] = "Wyloguj",
            ["start_work"] = "Rozpocznij pracę",
            ["stop_work"] = "Zakończ pracę",
            ["no_tasks"] = "Brak zadań na dziś",
            ["yes"] = "Tak",
            ["no"] = "Nie",
            ["cancel"] = "Anuluj",
            ["ok"] = "OK",
            ["error"] = "Błąd",
            ["save"] = "Zapisz",
            ["saved"] = "Zapisano",
            ["delete"] = "Usuń",
            ["loading"] = "Ładowanie...",
            ["send"] = "Wyślij",
            ["preview"] = "Podgląd",
            ["status"] = "Stan",
            ["open"] = "Otwarty",
            ["running"] = "W trakcie",
            ["done"] = "Gotowe",
            ["checklist"] = "Lista kontrolna",
            ["problems"] = "Problemy",
            ["notes"] = "Notatki"
        },

        ["ru"] = new Dictionary<string, string>
        {
            ["overview"] = "Обзор",
            ["today"] = "Сегодня",
            ["chat"] = "Чат",
            ["settings"] = "Настройки",
            ["logout"] = "Выход",
            ["start_work"] = "Начать работу",
            ["stop_work"] = "Закончить работу",
            ["no_tasks"] = "Нет задач на сегодня",
            ["yes"] = "Да",
            ["no"] = "Нет",
            ["cancel"] = "Отмена",
            ["ok"] = "OK",
            ["error"] = "Ошибка",
            ["save"] = "Сохранить",
            ["saved"] = "Сохранено",
            ["delete"] = "Удалить",
            ["loading"] = "Загрузка...",
            ["send"] = "Отправить",
            ["preview"] = "Предпросмотр",
            ["status"] = "Статус",
            ["open"] = "Открыто",
            ["running"] = "В работе",
            ["done"] = "Готово",
            ["checklist"] = "Контрольный список",
            ["problems"] = "Проблемы",
            ["notes"] = "Заметки"
        },

        ["uk"] = new Dictionary<string, string>
        {
            ["overview"] = "Огляд",
            ["today"] = "Сьогодні",
            ["chat"] = "Чат",
            ["settings"] = "Налаштування",
            ["logout"] = "Вихід",
            ["start_work"] = "Почати роботу",
            ["stop_work"] = "Закінчити роботу",
            ["no_tasks"] = "Немає завдань на сьогодні",
            ["yes"] = "Так",
            ["no"] = "Ні",
            ["cancel"] = "Скасувати",
            ["ok"] = "OK",
            ["error"] = "Помилка",
            ["save"] = "Зберегти",
            ["saved"] = "Збережено",
            ["delete"] = "Видалити",
            ["loading"] = "Завантаження...",
            ["send"] = "Надіслати",
            ["preview"] = "Попередній перегляд",
            ["status"] = "Статус",
            ["open"] = "Відкрито",
            ["running"] = "В роботі",
            ["done"] = "Готово",
            ["checklist"] = "Контрольний список",
            ["problems"] = "Проблеми",
            ["notes"] = "Нотатки"
        },

        ["vi"] = new Dictionary<string, string>
        {
            ["overview"] = "Tổng quan",
            ["today"] = "Hôm nay",
            ["chat"] = "Trò chuyện",
            ["settings"] = "Cài đặt",
            ["logout"] = "Đăng xuất",
            ["start_work"] = "Bắt đầu làm việc",
            ["stop_work"] = "Kết thúc làm việc",
            ["no_tasks"] = "Không có công việc hôm nay",
            ["yes"] = "Có",
            ["no"] = "Không",
            ["cancel"] = "Hủy",
            ["ok"] = "OK",
            ["error"] = "Lỗi",
            ["save"] = "Lưu",
            ["saved"] = "Đã lưu",
            ["delete"] = "Xóa",
            ["loading"] = "Đang tải...",
            ["send"] = "Gửi",
            ["preview"] = "Xem trước",
            ["status"] = "Trạng thái",
            ["open"] = "Mở",
            ["running"] = "Đang chạy",
            ["done"] = "Hoàn thành",
            ["checklist"] = "Danh sách kiểm tra",
            ["problems"] = "Vấn đề",
            ["notes"] = "Ghi chú"
        }
    };

    /// <summary>
    /// Current language code (default: de)
    /// </summary>
    public static string CurrentLanguage { get; set; } = "de";

    /// <summary>
    /// Get translated string for a key
    /// </summary>
    public static string Get(string key)
    {
        // Try current language
        if (_translations.TryGetValue(CurrentLanguage, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
                return value;
        }

        // Fallback to German
        if (_translations.TryGetValue("de", out var deDict))
        {
            if (deDict.TryGetValue(key, out var value))
                return value;
        }

        // Return key if not found
        return key;
    }

    /// <summary>
    /// Shortcut for Get()
    /// </summary>
    public static string T(string key) => Get(key);

    /// <summary>
    /// Load language from preferences
    /// </summary>
    public static void LoadFromPreferences()
    {
        CurrentLanguage = Preferences.Get("language", "de");
    }

    /// <summary>
    /// Save current language to preferences
    /// </summary>
    public static void SaveToPreferences()
    {
        Preferences.Set("language", CurrentLanguage);
    }

    /// <summary>
    /// Check if a language is supported
    /// </summary>
    public static bool IsSupported(string langCode)
    {
        return _translations.ContainsKey(langCode);
    }

    /// <summary>
    /// Get all supported language codes
    /// </summary>
    public static IEnumerable<string> SupportedLanguages => _translations.Keys;
}
