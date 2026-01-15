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
            ["end_work_question"] = "Möchtest du die Arbeitszeit beenden?",
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
            ["task_completed_question"] = "Möchtest du diese Aufgabe wirklich abschließen?",
            ["log"] = "Protokoll",
            ["delete_task"] = "Aufgabe löschen",
            ["delete_task_confirm"] = "Möchtest du diese Aufgabe wirklich löschen?",

            // Neue Aufgabe / My Tasks
            ["new_task"] = "Neue Aufgabe",
            ["my_tasks"] = "Meine Aufgaben",
            ["back"] = "Zurück",
            ["chat_list"] = "Chat Übersicht",
            ["messages"] = "Nachrichten",
            ["administration"] = "Verwaltung",
            ["office"] = "Büro",
            ["colleagues"] = "Kollegen",
            ["no_colleagues"] = "Keine Kollegen gefunden",
            ["select_contact"] = "Kontakt auswählen",
            ["create_task"] = "Aufgabe erstellen",
            ["edit_task"] = "Aufgabe bearbeiten",
            ["task_name"] = "Aufgabenname",
            ["task_name_required"] = "Aufgabenname *",
            ["apartment"] = "Apartment",
            ["planned_date"] = "Geplantes Datum",
            ["date_required"] = "Datum *",
            ["task_type"] = "Aufgabenart",
            ["note_hint"] = "Hinweis",
            ["optional_hint"] = "Optionaler Hinweis...",
            ["select_option"] = "-- Wählen --",
            ["select_apartment"] = "Apartment wählen",
            ["select_task_type"] = "Aufgabenart wählen",
            ["assign_cleaners"] = "Cleaner zuweisen",
            ["cleaning"] = "Putzen",
            ["check_task"] = "Check",
            ["repair"] = "Reparatur",
            ["status_tab"] = "Status",
            ["checklist_tab"] = "Checkliste",
            ["details_tab"] = "Details",
            ["assign_tab"] = "Zuweisen",
            ["images_tab"] = "Bilder",
            ["status_imported"] = "Nicht zugewiesen",
            ["status_assigned"] = "Zugewiesen",
            ["status_cleaned"] = "Geputzt",
            ["status_checked"] = "Gecheckt",
            ["select_status"] = "Status wählen",
            ["select_checklist_hint"] = "Wähle eine Aufgabenart mit Checkliste",
            ["no_my_tasks"] = "Keine eigenen Aufgaben",
            ["task_created"] = "Aufgabe erstellt",
            ["task_updated"] = "Aufgabe aktualisiert",
            ["task_create_error"] = "Fehler beim Erstellen der Aufgabe",
            ["task_update_error"] = "Fehler beim Aktualisieren der Aufgabe",
            ["task_delete_error"] = "Fehler beim Löschen der Aufgabe",
            ["confirm_delete_task"] = "Möchtest du diese Aufgabe wirklich löschen?",
            ["no_images"] = "Keine Bilder",
            ["info"] = "Info",
            ["save_task_first"] = "Bitte speichere die Aufgabe zuerst",
            ["camera_error"] = "Kamera-Fehler",
            ["gallery_error"] = "Galerie-Fehler",
            ["upload_error"] = "Fehler beim Hochladen",
            ["update_error"] = "Fehler beim Aktualisieren",
            ["delete_error"] = "Fehler beim Löschen",
            ["delete_image"] = "Bild löschen",
            ["confirm_delete_image"] = "Möchtest du dieses Bild wirklich löschen?",

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
            ["login_remember_me"] = "Angemeldet bleiben"
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

            ["end_work_question"] = "Do you want to end working time?",
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
            ["task_completed_question"] = "Do you really want to complete this task?",
            ["log"] = "Log",
            ["delete_task"] = "Delete task",
            ["delete_task_confirm"] = "Do you really want to delete this task?",

            // Neue Aufgabe / My Tasks
            ["new_task"] = "New Task",
            ["my_tasks"] = "My Tasks",
            ["back"] = "Back",
            ["chat_list"] = "Chat Overview",
            ["messages"] = "Messages",
            ["administration"] = "Administration",
            ["office"] = "Office",
            ["colleagues"] = "Colleagues",
            ["no_colleagues"] = "No colleagues found",
            ["select_contact"] = "Select contact",
            ["create_task"] = "Create Task",
            ["edit_task"] = "Edit Task",
            ["task_name"] = "Task name",
            ["task_name_required"] = "Task name *",
            ["apartment"] = "Apartment",
            ["planned_date"] = "Planned date",
            ["date_required"] = "Date *",
            ["task_type"] = "Task type",
            ["note_hint"] = "Note",
            ["optional_hint"] = "Optional note...",
            ["select_option"] = "-- Select --",
            ["select_apartment"] = "Select apartment",
            ["select_task_type"] = "Select task type",
            ["assign_cleaners"] = "Assign cleaners",
            ["cleaning"] = "Cleaning",
            ["check_task"] = "Check",
            ["repair"] = "Repair",
            ["status_tab"] = "Status",
            ["checklist_tab"] = "Checklist",
            ["details_tab"] = "Details",
            ["assign_tab"] = "Assign",
            ["images_tab"] = "Images",
            ["status_imported"] = "Not assigned",
            ["status_assigned"] = "Assigned",
            ["status_cleaned"] = "Cleaned",
            ["status_checked"] = "Checked",
            ["select_status"] = "Select status",
            ["select_checklist_hint"] = "Select a task type with checklist",
            ["no_my_tasks"] = "No own tasks",
            ["task_created"] = "Task created",
            ["task_updated"] = "Task updated",
            ["task_create_error"] = "Error creating task",
            ["task_update_error"] = "Error updating task",
            ["task_delete_error"] = "Error deleting task",
            ["confirm_delete_task"] = "Do you really want to delete this task?",
            ["no_images"] = "No images",
            ["info"] = "Info",
            ["save_task_first"] = "Please save the task first",
            ["camera_error"] = "Camera error",
            ["gallery_error"] = "Gallery error",
            ["upload_error"] = "Upload error",
            ["update_error"] = "Update error",
            ["delete_error"] = "Delete error",
            ["delete_image"] = "Delete image",
            ["confirm_delete_image"] = "Do you really want to delete this image?",

            // Login Screen
            ["login_subtitle"] = "Cleaning Management",
            ["login_enterprise_app"] = "Enterprise App:",
            ["login_credentials_info"] = "Please get your login credentials from your administrator.",
            ["login_new_customers"] = "New Customers:",
            ["login_registration_info"] = "Request registration via email: mail@schwanenburg.de",
            ["login_test_usage"] = "Test Usage:",
            ["login_test_credentials"] = "Property: 1  |  User: tom  |  Password: tom",
            ["login_title"] = "Login",
            ["login_property_id"] = "Property ID",
            ["login_username"] = "Username",
            ["login_password"] = "Password",
            ["login_remember_me"] = "Stay logged in"
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
            ["notes"] = "Notas",
            ["new_task"] = "Nueva Tarea",
            ["my_tasks"] = "Mis Tareas",
            ["back"] = "Volver",
            ["chat_list"] = "Lista de Chats",
            ["messages"] = "Mensajes",
            ["administration"] = "Administración",
            ["office"] = "Oficina",
            ["colleagues"] = "Colegas",
            ["no_colleagues"] = "No se encontraron colegas",
            ["select_contact"] = "Seleccionar contacto",
            ["create_task"] = "Crear Tarea",
            ["edit_task"] = "Editar Tarea",
            ["task_name"] = "Nombre de tarea",
            ["apartment"] = "Apartamento",
            ["planned_date"] = "Fecha planificada",
            ["task_type"] = "Tipo de tarea",
            ["assign_cleaners"] = "Asignar limpiadores",
            ["cleaning"] = "Limpieza",
            ["check_task"] = "Verificar",
            ["repair"] = "Reparación",
            ["status_imported"] = "No asignado",
            ["status_assigned"] = "Asignado",
            ["status_cleaned"] = "Limpiado",
            ["status_checked"] = "Verificado",
            ["no_my_tasks"] = "Sin tareas propias",
            ["task_created"] = "Tarea creada",
            ["task_updated"] = "Tarea actualizada"
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
            ["notes"] = "Note",
            ["new_task"] = "Sarcină Nouă",
            ["my_tasks"] = "Sarcinile Mele",
            ["back"] = "Înapoi",
            ["chat_list"] = "Lista Chaturi",
            ["messages"] = "Mesaje",
            ["administration"] = "Administrație",
            ["office"] = "Birou",
            ["colleagues"] = "Colegi",
            ["create_task"] = "Creare Sarcină",
            ["edit_task"] = "Editare Sarcină",
            ["task_name"] = "Nume sarcină",
            ["apartment"] = "Apartament",
            ["task_type"] = "Tip sarcină",
            ["assign_cleaners"] = "Atribuire curățători",
            ["cleaning"] = "Curățenie",
            ["check_task"] = "Verificare",
            ["repair"] = "Reparație",
            ["status_imported"] = "Neatribuit",
            ["status_assigned"] = "Atribuit",
            ["status_cleaned"] = "Curățat",
            ["status_checked"] = "Verificat",
            ["no_my_tasks"] = "Nicio sarcină proprie",
            ["task_created"] = "Sarcină creată"
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
            ["notes"] = "Notatki",
            ["new_task"] = "Nowe Zadanie",
            ["my_tasks"] = "Moje Zadania",
            ["back"] = "Wstecz",
            ["chat_list"] = "Lista Czatów",
            ["messages"] = "Wiadomości",
            ["administration"] = "Administracja",
            ["office"] = "Biuro",
            ["colleagues"] = "Koledzy",
            ["create_task"] = "Utwórz Zadanie",
            ["edit_task"] = "Edytuj Zadanie",
            ["task_name"] = "Nazwa zadania",
            ["apartment"] = "Apartament",
            ["task_type"] = "Typ zadania",
            ["assign_cleaners"] = "Przypisz sprzątaczy",
            ["cleaning"] = "Sprzątanie",
            ["check_task"] = "Sprawdzenie",
            ["repair"] = "Naprawa",
            ["status_imported"] = "Nieprzypisane",
            ["status_assigned"] = "Przypisane",
            ["status_cleaned"] = "Posprzątane",
            ["status_checked"] = "Sprawdzone",
            ["no_my_tasks"] = "Brak własnych zadań",
            ["task_created"] = "Zadanie utworzone"
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
            ["notes"] = "Заметки",
            ["new_task"] = "Новая Задача",
            ["my_tasks"] = "Мои Задачи",
            ["back"] = "Назад",
            ["chat_list"] = "Список Чатов",
            ["messages"] = "Сообщения",
            ["administration"] = "Администрация",
            ["office"] = "Офис",
            ["colleagues"] = "Коллеги",
            ["create_task"] = "Создать Задачу",
            ["edit_task"] = "Редактировать Задачу",
            ["task_name"] = "Название задачи",
            ["apartment"] = "Квартира",
            ["task_type"] = "Тип задачи",
            ["assign_cleaners"] = "Назначить уборщиков",
            ["cleaning"] = "Уборка",
            ["check_task"] = "Проверка",
            ["repair"] = "Ремонт",
            ["status_imported"] = "Не назначено",
            ["status_assigned"] = "Назначено",
            ["status_cleaned"] = "Убрано",
            ["status_checked"] = "Проверено",
            ["no_my_tasks"] = "Нет своих задач",
            ["task_created"] = "Задача создана"
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
            ["notes"] = "Нотатки",
            ["new_task"] = "Нове Завдання",
            ["my_tasks"] = "Мої Завдання",
            ["back"] = "Назад",
            ["chat_list"] = "Список Чатів",
            ["messages"] = "Повідомлення",
            ["administration"] = "Адміністрація",
            ["office"] = "Офіс",
            ["colleagues"] = "Колеги",
            ["create_task"] = "Створити Завдання",
            ["edit_task"] = "Редагувати Завдання",
            ["task_name"] = "Назва завдання",
            ["apartment"] = "Квартира",
            ["task_type"] = "Тип завдання",
            ["assign_cleaners"] = "Призначити прибиральників",
            ["cleaning"] = "Прибирання",
            ["check_task"] = "Перевірка",
            ["repair"] = "Ремонт",
            ["status_imported"] = "Не призначено",
            ["status_assigned"] = "Призначено",
            ["status_cleaned"] = "Прибрано",
            ["status_checked"] = "Перевірено",
            ["no_my_tasks"] = "Немає власних завдань",
            ["task_created"] = "Завдання створено"
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
            ["notes"] = "Ghi chú",
            ["new_task"] = "Công việc mới",
            ["my_tasks"] = "Công việc của tôi",
            ["back"] = "Quay lại",
            ["chat_list"] = "Danh sách Chat",
            ["messages"] = "Tin nhắn",
            ["administration"] = "Quản trị",
            ["office"] = "Văn phòng",
            ["colleagues"] = "Đồng nghiệp",
            ["create_task"] = "Tạo Công việc",
            ["edit_task"] = "Sửa Công việc",
            ["task_name"] = "Tên công việc",
            ["apartment"] = "Căn hộ",
            ["task_type"] = "Loại công việc",
            ["assign_cleaners"] = "Phân công",
            ["cleaning"] = "Dọn dẹp",
            ["check_task"] = "Kiểm tra",
            ["repair"] = "Sửa chữa",
            ["status_imported"] = "Chưa phân công",
            ["status_assigned"] = "Đã phân công",
            ["status_cleaned"] = "Đã dọn",
            ["status_checked"] = "Đã kiểm tra",
            ["no_my_tasks"] = "Không có công việc riêng",
            ["task_created"] = "Đã tạo công việc"
        }
    };

    /// <summary>
    /// Current language code (default: en)
    /// </summary>
    public static string CurrentLanguage { get; set; } = "en";

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

        // Fallback to English (default language)
        if (_translations.TryGetValue("en", out var enDict))
        {
            if (enDict.TryGetValue(key, out var value))
                return value;
        }

        // Second fallback to German (most complete)
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
    /// Load language from preferences or detect from device
    /// </summary>
    public static void LoadFromPreferences()
    {
        var savedLanguage = Preferences.Get("language", "");
        if (!string.IsNullOrEmpty(savedLanguage) && IsSupported(savedLanguage))
        {
            CurrentLanguage = savedLanguage;
            return;
        }

        // Detect device language - try multiple methods
        string deviceLang = "en";
        try
        {
            // Try CurrentUICulture first (more reliable for UI language)
            deviceLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            if (!IsSupported(deviceLang))
            {
                // Fallback to CurrentCulture
                deviceLang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            }
        }
        catch
        {
            deviceLang = "en";
        }

        CurrentLanguage = IsSupported(deviceLang) ? deviceLang : "en";
        System.Diagnostics.Debug.WriteLine($"[Translations] Device language detected: {deviceLang}, using: {CurrentLanguage}");
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
