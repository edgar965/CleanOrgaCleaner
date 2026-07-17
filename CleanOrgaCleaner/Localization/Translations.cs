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
            ["task"] = "Auftrag",
            ["notes"] = "Anmerkungen",
            ["report_problem"] = "Problem melden",
            ["edit_problem"] = "Problem bearbeiten",
            ["edit_note"] = "Anmerkung bearbeiten",
            ["delete"] = "Löschen",
            ["no_problems"] = "Keine Probleme gemeldet",
            ["description"] = "Beschreibung",
            ["photos"] = "Fotos",
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
            ["create_auftrag"] = "Auftrag erstellen",
            ["edit_auftrag"] = "Auftrag bearbeiten",
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
        },

        ["en"] = new Dictionary<string, string>
        {
            ["today"] = "Today",
            ["chat"] = "Chat",
            ["settings"] = "Settings",
            ["logout"] = "Logout",

            ["message_placeholder"] = "Enter message...",
            ["message_from"] = "From",
            ["notifications"] = "Notifications",
            ["push_notifications"] = "Push notifications",
            ["enabled"] = "Enabled",
            ["not_enabled"] = "Not enabled",
            ["disabled"] = "Disabled",
            ["not_active"] = "Not active",
            ["notifications_denied_hint"] = "Notifications are not active. Please allow them for CleanOrga in the device settings.",
            ["open_settings"] = "Open settings",
            ["translation_preview"] = "Translation preview",
            ["your_text"] = "Your text",
            ["translation_for_admin"] = "Translation (for admin)",
            ["back_translation"] = "Back translation",
            ["delete_image_confirm"] = "Remove image from this message?",
            ["delete_note"] = "Delete note",
            ["delete_note_confirm"] = "Do you really want to delete this note?",
            ["select_image_source"] = "Select image",

            ["select_language"] = "Select language",
            ["logged_in_as"] = "Logged in as",
            ["app_info"] = "App Info",
            ["version"] = "Version",
            ["server"] = "Server",
            ["language"] = "Language",
            ["security"] = "Security",
            ["biometric_login"] = "Fingerprint / Face",
            ["biometric_hint"] = "Quick and secure login with biometrics",
            ["select_avatar"] = "Select avatar",
            ["avatar_changed"] = "Avatar changed",
            ["tap_to_change"] = "Tap to change",
            ["change"] = "Change",

            ["no_tasks"] = "No tasks for today",
            ["cleaning_finished"] = "Pause / end work time?",
            ["yes"] = "Yes",
            ["no"] = "No",
            ["cancel"] = "Cancel",
            ["ok"] = "OK",

            ["error"] = "Error",
            ["attention"] = "Attention",
            ["sync_failed_hint"] = "An action recorded offline could not be sent and was discarded",
            ["unknown_error"] = "Unknown error",
            ["start_work_first"] = "Please click 'Start Work' first so that the working time is recorded correctly.",

            ["completed"] = "Completed",

            ["task"] = "Task",
            ["notes"] = "Notes",
            ["report_problem"] = "Report problem",
            ["edit_problem"] = "Edit problem",
            ["edit_note"] = "Edit note",
            ["delete"] = "Delete",
            ["no_problems"] = "No problems reported",
            ["description"] = "Description",
            ["photos"] = "Photos",
            ["save"] = "Save",
            ["saved"] = "Saved",
            ["delete_problem_title"] = "Delete problem",
            ["delete_problem_confirm"] = "Do you really want to delete this problem?",
            ["yes_delete"] = "Yes, delete",
            ["problem_reported"] = "Problem reported",

            // Images / Notes
            ["add_note"] = "Add note",
            ["no_notes"] = "No notes",
            ["no_logs"] = "No log entries available",
            ["no_task_description"] = "No task description available",
            ["camera"] = "Camera",
            ["gallery"] = "Gallery",
            ["note"] = "Note",

            // Buttons
            ["start"] = "Start",
            ["stop"] = "Finish",

            ["loading"] = "Loading...",
            ["connection_error"] = "Connection error",
            ["no_connection"] = "No connection",
            ["network_error_hint"] = "Network error. Please connect to WiFi or mobile data.",
            ["saved_offline"] = "Saved. Will sync when connected.",
            ["really_logout"] = "Do you really want to logout?",
            ["task_completed"] = "Complete task",
            ["task_completed_question"] = "Do you really want to complete this task?",
            ["log"] = "Log",
            ["delete_task"] = "Delete task",

            // Neue Aufgabe / My Tasks
            ["create_auftrag"] = "Create Order",
            ["edit_auftrag"] = "Edit Order",
            ["messages"] = "Messages",
            ["administration"] = "Administration",
            ["colleagues"] = "Colleagues",
            ["colleague"] = "Colleague",
            ["admin_contact"] = "Administration",
            ["task_name_required"] = "Task name *",
            ["apartment"] = "Apartment",
            ["date_required"] = "Date *",
            ["task_type"] = "Task type",
            ["optional_hint"] = "Task description...",
            ["assign_cleaners"] = "Assign cleaners",
            ["cleaning"] = "Cleaning",
            ["check_task"] = "Check",
            ["repair"] = "Repair",
            ["details_tab"] = "Details",
            ["task_tab"] = "Task",
            ["problems_tab"] = "Problems",
            ["notes_tab"] = "Notes",
            ["assign_tab"] = "Assign",
            ["no_my_tasks"] = "No own tasks",
            ["task_create_error"] = "Error creating task",
            ["task_update_error"] = "Error updating task",
            ["task_delete_error"] = "Error deleting task",
            ["confirm_delete_task"] = "Do you really want to delete this task?",
            ["update_error"] = "Update error",
            ["delete_error"] = "Delete error",
            ["delete_image"] = "Delete image",
            ["confirm_delete_image"] = "Do you really want to delete this image?",

            // Validation messages
            ["name_required"] = "Please enter a name",
            ["name"] = "Name",

            // Log translations
            ["log_note_added"] = "Note added",
            ["log_note_created"] = "Note created",
            ["log_image_deleted"] = "Image deleted",
            ["log_problem_reported"] = "Problem reported",
            ["log_problem_deleted"] = "Problem deleted",
            ["log_task_created"] = "Task created",
            ["log_task_updated"] = "Task updated",
            ["log_repair_task_created"] = "Repair task created",
            ["log_cleaning_assigned_to"] = "Cleaning assigned to",
            ["log_assignment_removed"] = "Assignment removed",
            ["log_progress"] = "Progress",
            ["log_status_changed"] = "Status changed",
            ["log_checklist_updated"] = "Checklist updated",
            ["log_not_started"] = "Not started",
            ["log_started"] = "Started",
            ["log_completed"] = "Completed",

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
            ["login_remember_me"] = "Stay logged in",

            // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
            ["offline"] = "Offline",
            ["create_error"] = "Create failed",
            ["save_error"] = "Save failed",
            ["delete_chat_title"] = "Delete chat",
            ["delete_chat_confirm"] = "Delete all messages? This action cannot be undone.",
        },

        ["es"] = new Dictionary<string, string>
        {
            // Navigation
            ["today"] = "Hoy",
            ["chat"] = "Chat",
            ["settings"] = "Configuración",
            ["logout"] = "Cerrar sesión",

            // Chat
            ["message_placeholder"] = "Escribe un mensaje...",
            ["translation_preview"] = "Vista previa de traducción",
            ["your_text"] = "Tu texto",
            ["translation_for_admin"] = "Traducción (para admin)",
            ["back_translation"] = "Traducción inversa",

            // Settings
            ["select_language"] = "Seleccionar idioma",
            ["logged_in_as"] = "Conectado como",
            ["app_info"] = "Info de la App",
            ["version"] = "Versión",
            ["server"] = "Servidor",
            ["language"] = "Idioma",
            ["security"] = "Seguridad",
            ["biometric_login"] = "Huella / Rostro",
            ["biometric_hint"] = "Inicio de sesión rápido y seguro con biometría",
            ["select_avatar"] = "Seleccionar avatar",
            ["avatar_changed"] = "Avatar cambiado",
            ["tap_to_change"] = "Toca para cambiar",
            ["change"] = "Cambiar",

            // Today / Work Time
            ["no_tasks"] = "Sin tareas para hoy",
            ["cleaning_finished"] = "¿Pausar / terminar el tiempo de trabajo?",
            ["yes"] = "Sí",
            ["no"] = "No",
            ["cancel"] = "Cancelar",
            ["ok"] = "OK",

            // Work Time
            ["error"] = "Error",
            ["attention"] = "Atención",
            ["sync_failed_hint"] = "Una acción registrada sin conexión no se pudo enviar y se descartó",
            ["unknown_error"] = "Error desconocido",
            ["start_work_first"] = "Por favor haz clic en 'Iniciar trabajo' primero.",

            // Task Status
            ["completed"] = "Completado",

            // Task Detail
            ["task"] = "Tarea",
            ["notes"] = "Notas",
            ["report_problem"] = "Reportar problema",
            ["edit_problem"] = "Editar problema",
            ["edit_note"] = "Editar nota",
            ["name"] = "Nombre",
            ["delete"] = "Eliminar",
            ["no_problems"] = "Sin problemas reportados",
            ["description"] = "Descripción",
            ["photos"] = "Fotos",
            ["save"] = "Guardar",
            ["saved"] = "Guardado",
            ["delete_problem_title"] = "Eliminar problema",
            ["delete_problem_confirm"] = "¿Realmente quieres eliminar este problema?",
            ["yes_delete"] = "Sí, eliminar",
            ["problem_reported"] = "Problema reportado",

            // Images / Notes
            ["add_note"] = "Añadir nota",
            ["no_notes"] = "Sin notas",
            ["no_logs"] = "Sin entradas de protocolo",
            ["no_task_description"] = "Sin descripción de tarea disponible",
            ["camera"] = "Cámara",
            ["gallery"] = "Galería",
            ["note"] = "Nota",

            // Buttons
            ["start"] = "Iniciar",
            ["stop"] = "Detener",

            // General
            ["loading"] = "Cargando...",
            ["connection_error"] = "Error de conexión",
            ["no_connection"] = "Sin conexión",
            ["saved_offline"] = "Guardado. Se sincronizará cuando haya conexión.",
            ["really_logout"] = "¿Realmente quieres cerrar sesión?",
            ["task_completed"] = "Completar tarea",
            ["task_completed_question"] = "¿Realmente quieres completar esta tarea?",
            ["log"] = "Registro",
            ["delete_task"] = "Eliminar tarea",

            // My Tasks
            ["create_auftrag"] = "Crear Pedido",
            ["edit_auftrag"] = "Editar Pedido",
            ["messages"] = "Mensajes",
            ["administration"] = "Administración",
            ["colleagues"] = "Colegas",
            ["colleague"] = "Colega",
            ["admin_contact"] = "Administración",
            ["task_name_required"] = "Nombre de tarea *",
            ["apartment"] = "Apartamento",
            ["date_required"] = "Fecha *",
            ["task_type"] = "Tipo de tarea",
            ["optional_hint"] = "Descripción de la tarea...",
            ["assign_cleaners"] = "Asignar limpiadores",
            ["cleaning"] = "Limpieza",
            ["check_task"] = "Verificar",
            ["repair"] = "Reparación",
            ["details_tab"] = "Detalles",
            ["task_tab"] = "Tarea",
            ["problems_tab"] = "Problemas",
            ["notes_tab"] = "Notas",
            ["assign_tab"] = "Asignar",
            ["no_my_tasks"] = "Sin tareas propias",
            ["task_create_error"] = "Error al crear la tarea",
            ["task_update_error"] = "Error al actualizar la tarea",
            ["task_delete_error"] = "Error al eliminar la tarea",
            ["confirm_delete_task"] = "¿Realmente quieres eliminar esta tarea?",
            ["update_error"] = "Error al actualizar",
            ["delete_error"] = "Error al eliminar",
            ["delete_image"] = "Eliminar imagen",
            ["confirm_delete_image"] = "¿Realmente quieres eliminar esta imagen?",

            // Log translations
            ["log_note_added"] = "Nota añadida",
            ["log_image_deleted"] = "Imagen eliminada",
            ["log_problem_reported"] = "Problema reportado",
            ["log_problem_deleted"] = "Problema eliminado",
            ["log_task_created"] = "Tarea creada",
            ["log_task_updated"] = "Tarea actualizada",
            ["log_repair_task_created"] = "Tarea de reparación creada",
            ["log_cleaning_assigned_to"] = "Limpieza asignada a",
            ["log_assignment_removed"] = "Asignación eliminada",
            ["log_progress"] = "Progreso",
            ["log_status_changed"] = "Estado cambiado",
            ["log_checklist_updated"] = "Lista actualizada",
            ["log_not_started"] = "No iniciado",
            ["log_started"] = "Iniciado",
            ["log_completed"] = "Completado",

            // Login Screen
            ["login_subtitle"] = "Gestión de limpieza",
            ["login_enterprise_app"] = "App empresarial:",
            ["login_credentials_info"] = "Obtén tus credenciales de tu administrador.",
            ["login_new_customers"] = "Nuevos clientes:",
            ["login_registration_info"] = "Registro por email: mail@schwanenburg.de",
            ["login_test_usage"] = "Uso de prueba:",
            ["login_test_credentials"] = "Property: 1  |  User: tom  |  Contraseña: tom",
            ["login_title"] = "Iniciar sesión",
            ["login_property_id"] = "ID de propiedad",
            ["login_username"] = "Usuario",
            ["login_password"] = "Contraseña",
            ["login_remember_me"] = "Mantener sesión",

            // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
            ["offline"] = "Sin conexión",
            ["create_error"] = "Error al crear",
            ["save_error"] = "Error al guardar",
            ["delete_chat_title"] = "Eliminar chat",
            ["delete_chat_confirm"] = "¿Eliminar todos los mensajes? Esta acción no se puede deshacer.",
            ["delete_image_confirm"] = "¿Eliminar la imagen de este mensaje?",
            ["delete_note"] = "Eliminar nota",
            ["delete_note_confirm"] = "¿Seguro que quieres eliminar esta nota?",
            ["log_note_created"] = "Nota creada",
            ["message_from"] = "De",
            ["notifications"] = "Notificaciones",
            ["push_notifications"] = "Notificaciones push",
            ["enabled"] = "Activado",
            ["not_enabled"] = "No activado",
            ["disabled"] = "Desactivado",
            ["not_active"] = "No activo",
            ["notifications_denied_hint"] = "Las notificaciones no están activas. Permítelas para CleanOrga en los ajustes del dispositivo.",
            ["open_settings"] = "Abrir ajustes",
            ["name_required"] = "Por favor, introduce un nombre",
            ["network_error_hint"] = "Error de red. Conéctate a WiFi o datos móviles.",
            ["select_image_source"] = "Seleccionar imagen",
        },

        ["ro"] = new Dictionary<string, string>
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
            ["notes"] = "Note",
            ["report_problem"] = "Raportează problemă",
            ["edit_problem"] = "Editează problemă",
            ["edit_note"] = "Editează notă",
            ["name"] = "Nume",
            ["delete"] = "Șterge",
            ["no_problems"] = "Nicio problemă raportată",
            ["description"] = "Descriere",
            ["photos"] = "Fotografii",
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
            ["create_auftrag"] = "Creare Comandă",
            ["edit_auftrag"] = "Editare Comandă",
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
        },

        ["pl"] = new Dictionary<string, string>
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
            ["notes"] = "Notatki",
            ["report_problem"] = "Zgłoś problem",
            ["edit_problem"] = "Edytuj problem",
            ["edit_note"] = "Edytuj notatkę",
            ["name"] = "Nazwa",
            ["delete"] = "Usuń",
            ["no_problems"] = "Brak zgłoszonych problemów",
            ["description"] = "Opis",
            ["photos"] = "Zdjęcia",
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
            ["create_auftrag"] = "Utwórz Zlecenie",
            ["edit_auftrag"] = "Edytuj Zlecenie",
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
        },

        ["ru"] = new Dictionary<string, string>
        {
            // Navigation
            ["today"] = "Сегодня",
            ["chat"] = "Чат",
            ["settings"] = "Настройки",
            ["logout"] = "Выход",

            // Chat
            ["message_placeholder"] = "Введите сообщение...",
            ["translation_preview"] = "Предпросмотр перевода",
            ["your_text"] = "Ваш текст",
            ["translation_for_admin"] = "Перевод (для админа)",
            ["back_translation"] = "Обратный перевод",

            // Settings
            ["select_language"] = "Выбрать язык",
            ["logged_in_as"] = "Вы вошли как",
            ["app_info"] = "Информация о приложении",
            ["version"] = "Версия",
            ["server"] = "Сервер",
            ["language"] = "Язык",
            ["security"] = "Безопасность",
            ["biometric_login"] = "Отпечаток / Лицо",
            ["biometric_hint"] = "Быстрый и безопасный вход с биометрией",
            ["select_avatar"] = "Выбрать аватар",
            ["avatar_changed"] = "Аватар изменён",
            ["tap_to_change"] = "Нажмите для изменения",
            ["change"] = "Изменить",

            // Today / Work Time
            ["no_tasks"] = "Нет задач на сегодня",
            ["cleaning_finished"] = "Пауза / завершить рабочее время?",
            ["yes"] = "Да",
            ["no"] = "Нет",
            ["cancel"] = "Отмена",
            ["ok"] = "OK",

            // Work Time
            ["error"] = "Ошибка",
            ["attention"] = "Внимание",
            ["sync_failed_hint"] = "Действие, записанное офлайн, не удалось отправить, оно отклонено",
            ["unknown_error"] = "Неизвестная ошибка",
            ["start_work_first"] = "Пожалуйста, сначала нажмите 'Начать работу'.",

            // Task Status
            ["completed"] = "Завершено",

            // Task Detail
            ["task"] = "Задача",
            ["notes"] = "Заметки",
            ["report_problem"] = "Сообщить о проблеме",
            ["edit_problem"] = "Редактировать проблему",
            ["edit_note"] = "Редактировать заметку",
            ["name"] = "Название",
            ["delete"] = "Удалить",
            ["no_problems"] = "Нет проблем",
            ["description"] = "Описание",
            ["photos"] = "Фотографии",
            ["save"] = "Сохранить",
            ["saved"] = "Сохранено",
            ["delete_problem_title"] = "Удалить проблему",
            ["delete_problem_confirm"] = "Вы уверены, что хотите удалить эту проблему?",
            ["yes_delete"] = "Да, удалить",
            ["problem_reported"] = "Проблема сообщена",

            // Images / Notes
            ["add_note"] = "Добавить заметку",
            ["no_notes"] = "Нет заметок",
            ["no_logs"] = "Нет записей в журнале",
            ["no_task_description"] = "Нет описания задачи",
            ["camera"] = "Камера",
            ["gallery"] = "Галерея",
            ["note"] = "Заметка",

            // Buttons
            ["start"] = "Старт",
            ["stop"] = "Стоп",

            // General
            ["loading"] = "Загрузка...",
            ["connection_error"] = "Ошибка соединения",
            ["no_connection"] = "Нет соединения",
            ["saved_offline"] = "Сохранено. Синхронизируется при подключении.",
            ["really_logout"] = "Вы уверены, что хотите выйти?",
            ["task_completed"] = "Завершить задачу",
            ["task_completed_question"] = "Вы уверены, что хотите завершить эту задачу?",
            ["log"] = "Журнал",
            ["delete_task"] = "Удалить задачу",

            // My Tasks
            ["create_auftrag"] = "Создать Заказ",
            ["edit_auftrag"] = "Редактировать Заказ",
            ["messages"] = "Сообщения",
            ["administration"] = "Администрация",
            ["colleagues"] = "Коллеги",
            ["colleague"] = "Коллега",
            ["admin_contact"] = "Администрация",
            ["task_name_required"] = "Название задачи *",
            ["apartment"] = "Квартира",
            ["date_required"] = "Дата *",
            ["task_type"] = "Тип задачи",
            ["optional_hint"] = "Описание задачи...",
            ["assign_cleaners"] = "Назначить уборщиков",
            ["cleaning"] = "Уборка",
            ["check_task"] = "Проверка",
            ["repair"] = "Ремонт",
            ["details_tab"] = "Детали",
            ["task_tab"] = "Задача",
            ["problems_tab"] = "Проблемы",
            ["notes_tab"] = "Заметки",
            ["assign_tab"] = "Назначить",
            ["no_my_tasks"] = "Нет своих задач",
            ["task_create_error"] = "Ошибка при создании задачи",
            ["task_update_error"] = "Ошибка при обновлении задачи",
            ["task_delete_error"] = "Ошибка при удалении задачи",
            ["confirm_delete_task"] = "Вы уверены, что хотите удалить эту задачу?",
            ["update_error"] = "Ошибка обновления",
            ["delete_error"] = "Ошибка удаления",
            ["delete_image"] = "Удалить изображение",
            ["confirm_delete_image"] = "Вы уверены, что хотите удалить это изображение?",

            // Log translations
            ["log_note_added"] = "Заметка добавлена",
            ["log_image_deleted"] = "Изображение удалено",
            ["log_problem_reported"] = "Проблема сообщена",
            ["log_problem_deleted"] = "Проблема удалена",
            ["log_task_created"] = "Задача создана",
            ["log_task_updated"] = "Задача обновлена",
            ["log_repair_task_created"] = "Ремонтная задача создана",
            ["log_cleaning_assigned_to"] = "Уборка назначена",
            ["log_assignment_removed"] = "Назначение удалено",
            ["log_progress"] = "Прогресс",
            ["log_status_changed"] = "Статус изменён",
            ["log_checklist_updated"] = "Список обновлён",
            ["log_not_started"] = "Не начато",
            ["log_started"] = "Начато",
            ["log_completed"] = "Завершено",

            // Login Screen
            ["login_subtitle"] = "Управление уборкой",
            ["login_enterprise_app"] = "Корпоративное приложение:",
            ["login_credentials_info"] = "Получите данные для входа у администратора.",
            ["login_new_customers"] = "Новые клиенты:",
            ["login_registration_info"] = "Регистрация по email: mail@schwanenburg.de",
            ["login_test_usage"] = "Тестовое использование:",
            ["login_test_credentials"] = "Property: 1  |  User: tom  |  Пароль: tom",
            ["login_title"] = "Вход",
            ["login_property_id"] = "ID объекта",
            ["login_username"] = "Пользователь",
            ["login_password"] = "Пароль",
            ["login_remember_me"] = "Запомнить меня",

            // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
            ["offline"] = "Офлайн",
            ["create_error"] = "Ошибка при создании",
            ["save_error"] = "Ошибка при сохранении",
            ["delete_chat_title"] = "Удалить чат",
            ["delete_chat_confirm"] = "Удалить все сообщения? Это действие нельзя отменить.",
            ["delete_image_confirm"] = "Удалить изображение из этого сообщения?",
            ["delete_note"] = "Удалить заметку",
            ["delete_note_confirm"] = "Действительно удалить эту заметку?",
            ["log_note_created"] = "Заметка создана",
            ["message_from"] = "От",
            ["notifications"] = "Уведомления",
            ["push_notifications"] = "Push-уведомления",
            ["enabled"] = "Включено",
            ["not_enabled"] = "Не включено",
            ["disabled"] = "Отключено",
            ["not_active"] = "Не активно",
            ["notifications_denied_hint"] = "Уведомления не активны. Разрешите их для CleanOrga в настройках устройства.",
            ["open_settings"] = "Открыть настройки",
            ["name_required"] = "Пожалуйста, введите имя",
            ["network_error_hint"] = "Ошибка сети. Подключитесь к WiFi или мобильному интернету.",
            ["select_image_source"] = "Выбрать изображение",
        },

        ["uk"] = new Dictionary<string, string>
        {
            // Navigation
            ["today"] = "Сьогодні",
            ["chat"] = "Чат",
            ["settings"] = "Налаштування",
            ["logout"] = "Вихід",

            // Chat
            ["message_placeholder"] = "Введіть повідомлення...",
            ["translation_preview"] = "Попередній перегляд перекладу",
            ["your_text"] = "Ваш текст",
            ["translation_for_admin"] = "Переклад (для адміна)",
            ["back_translation"] = "Зворотний переклад",

            // Settings
            ["select_language"] = "Вибрати мову",
            ["logged_in_as"] = "Ви увійшли як",
            ["app_info"] = "Інформація про додаток",
            ["version"] = "Версія",
            ["server"] = "Сервер",
            ["language"] = "Мова",
            ["security"] = "Безпека",
            ["biometric_login"] = "Відбиток / Обличчя",
            ["biometric_hint"] = "Швидкий і безпечний вхід з біометрією",
            ["select_avatar"] = "Вибрати аватар",
            ["avatar_changed"] = "Аватар змінено",
            ["tap_to_change"] = "Натисніть для зміни",
            ["change"] = "Змінити",

            // Today / Work Time
            ["no_tasks"] = "Немає завдань на сьогодні",
            ["cleaning_finished"] = "Пауза / завершити робочий час?",
            ["yes"] = "Так",
            ["no"] = "Ні",
            ["cancel"] = "Скасувати",
            ["ok"] = "OK",

            // Work Time
            ["error"] = "Помилка",
            ["attention"] = "Увага",
            ["sync_failed_hint"] = "Дію, записану офлайн, не вдалося надіслати, її відхилено",
            ["unknown_error"] = "Невідома помилка",
            ["start_work_first"] = "Будь ласка, спочатку натисніть 'Почати роботу'.",

            // Task Status
            ["completed"] = "Завершено",

            // Task Detail
            ["task"] = "Завдання",
            ["notes"] = "Нотатки",
            ["report_problem"] = "Повідомити про проблему",
            ["edit_problem"] = "Редагувати проблему",
            ["edit_note"] = "Редагувати нотатку",
            ["name"] = "Назва",
            ["delete"] = "Видалити",
            ["no_problems"] = "Немає проблем",
            ["description"] = "Опис",
            ["photos"] = "Фотографії",
            ["save"] = "Зберегти",
            ["saved"] = "Збережено",
            ["delete_problem_title"] = "Видалити проблему",
            ["delete_problem_confirm"] = "Ви впевнені, що хочете видалити цю проблему?",
            ["yes_delete"] = "Так, видалити",
            ["problem_reported"] = "Проблему повідомлено",

            // Images / Notes
            ["add_note"] = "Додати нотатку",
            ["no_notes"] = "Немає нотаток",
            ["no_logs"] = "Немає записів у журналі",
            ["no_task_description"] = "Немає опису завдання",
            ["camera"] = "Камера",
            ["gallery"] = "Галерея",
            ["note"] = "Нотатка",

            // Buttons
            ["start"] = "Старт",
            ["stop"] = "Стоп",

            // General
            ["loading"] = "Завантаження...",
            ["connection_error"] = "Помилка з'єднання",
            ["no_connection"] = "Немає з'єднання",
            ["saved_offline"] = "Збережено. Синхронізується при підключенні.",
            ["really_logout"] = "Ви впевнені, що хочете вийти?",
            ["task_completed"] = "Завершити завдання",
            ["task_completed_question"] = "Ви впевнені, що хочете завершити це завдання?",
            ["log"] = "Журнал",
            ["delete_task"] = "Видалити завдання",

            // My Tasks
            ["create_auftrag"] = "Створити Замовлення",
            ["edit_auftrag"] = "Редагувати Замовлення",
            ["messages"] = "Повідомлення",
            ["administration"] = "Адміністрація",
            ["colleagues"] = "Колеги",
            ["colleague"] = "Колега",
            ["admin_contact"] = "Адміністрація",
            ["task_name_required"] = "Назва завдання *",
            ["apartment"] = "Квартира",
            ["date_required"] = "Дата *",
            ["task_type"] = "Тип завдання",
            ["optional_hint"] = "Опис завдання...",
            ["assign_cleaners"] = "Призначити прибиральників",
            ["cleaning"] = "Прибирання",
            ["check_task"] = "Перевірка",
            ["repair"] = "Ремонт",
            ["details_tab"] = "Деталі",
            ["task_tab"] = "Завдання",
            ["problems_tab"] = "Проблеми",
            ["notes_tab"] = "Нотатки",
            ["assign_tab"] = "Призначити",
            ["no_my_tasks"] = "Немає власних завдань",
            ["task_create_error"] = "Помилка при створенні завдання",
            ["task_update_error"] = "Помилка при оновленні завдання",
            ["task_delete_error"] = "Помилка при видаленні завдання",
            ["confirm_delete_task"] = "Ви впевнені, що хочете видалити це завдання?",
            ["update_error"] = "Помилка оновлення",
            ["delete_error"] = "Помилка видалення",
            ["delete_image"] = "Видалити зображення",
            ["confirm_delete_image"] = "Ви впевнені, що хочете видалити це зображення?",

            // Log translations
            ["log_note_added"] = "Замітку додано",
            ["log_image_deleted"] = "Зображення видалено",
            ["log_problem_reported"] = "Проблему повідомлено",
            ["log_problem_deleted"] = "Проблему видалено",
            ["log_task_created"] = "Завдання створено",
            ["log_task_updated"] = "Завдання оновлено",
            ["log_repair_task_created"] = "Ремонтне завдання створено",
            ["log_cleaning_assigned_to"] = "Прибирання призначено",
            ["log_assignment_removed"] = "Призначення видалено",
            ["log_progress"] = "Прогрес",
            ["log_status_changed"] = "Статус змінено",
            ["log_checklist_updated"] = "Список оновлено",
            ["log_not_started"] = "Не розпочато",
            ["log_started"] = "Розпочато",
            ["log_completed"] = "Завершено",

            // Login Screen
            ["login_subtitle"] = "Управління прибиранням",
            ["login_enterprise_app"] = "Корпоративний додаток:",
            ["login_credentials_info"] = "Отримайте дані для входу від адміністратора.",
            ["login_new_customers"] = "Нові клієнти:",
            ["login_registration_info"] = "Реєстрація по email: mail@schwanenburg.de",
            ["login_test_usage"] = "Тестове використання:",
            ["login_test_credentials"] = "Property: 1  |  User: tom  |  Пароль: tom",
            ["login_title"] = "Вхід",
            ["login_property_id"] = "ID об'єкта",
            ["login_username"] = "Користувач",
            ["login_password"] = "Пароль",
            ["login_remember_me"] = "Запам'ятати мене",

            // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
            ["offline"] = "Офлайн",
            ["create_error"] = "Помилка під час створення",
            ["save_error"] = "Помилка під час збереження",
            ["delete_chat_title"] = "Видалити чат",
            ["delete_chat_confirm"] = "Видалити всі повідомлення? Цю дію не можна скасувати.",
            ["delete_image_confirm"] = "Видалити зображення з цього повідомлення?",
            ["delete_note"] = "Видалити нотатку",
            ["delete_note_confirm"] = "Справді видалити цю нотатку?",
            ["log_note_created"] = "Нотатку створено",
            ["message_from"] = "Від",
            ["notifications"] = "Сповіщення",
            ["push_notifications"] = "Push-сповіщення",
            ["enabled"] = "Увімкнено",
            ["not_enabled"] = "Не увімкнено",
            ["disabled"] = "Вимкнено",
            ["not_active"] = "Не активно",
            ["notifications_denied_hint"] = "Сповіщення не активні. Дозвольте їх для CleanOrga в налаштуваннях пристрою.",
            ["open_settings"] = "Відкрити налаштування",
            ["name_required"] = "Будь ласка, введіть назву",
            ["network_error_hint"] = "Помилка мережі. Підключіться до WiFi або мобільного інтернету.",
            ["select_image_source"] = "Вибрати зображення",
        },

        ["vi"] = new Dictionary<string, string>
        {
            // Navigation
            ["today"] = "Hôm nay",
            ["chat"] = "Trò chuyện",
            ["settings"] = "Cài đặt",
            ["logout"] = "Đăng xuất",

            // Chat
            ["message_placeholder"] = "Nhập tin nhắn...",
            ["translation_preview"] = "Xem trước bản dịch",
            ["your_text"] = "Văn bản của bạn",
            ["translation_for_admin"] = "Bản dịch (cho admin)",
            ["back_translation"] = "Dịch ngược",

            // Settings
            ["select_language"] = "Chọn ngôn ngữ",
            ["logged_in_as"] = "Đăng nhập với",
            ["app_info"] = "Thông tin ứng dụng",
            ["version"] = "Phiên bản",
            ["server"] = "Máy chủ",
            ["language"] = "Ngôn ngữ",
            ["security"] = "Bảo mật",
            ["biometric_login"] = "Vân tay / Khuôn mặt",
            ["biometric_hint"] = "Đăng nhập nhanh và an toàn bằng sinh trắc học",
            ["select_avatar"] = "Chọn avatar",
            ["avatar_changed"] = "Đã thay đổi avatar",
            ["tap_to_change"] = "Nhấn để thay đổi",
            ["change"] = "Thay đổi",

            // Today / Work Time
            ["no_tasks"] = "Không có công việc hôm nay",
            ["cleaning_finished"] = "Tạm dừng / kết thúc giờ làm?",
            ["yes"] = "Có",
            ["no"] = "Không",
            ["cancel"] = "Hủy",
            ["ok"] = "OK",

            // Work Time
            ["error"] = "Lỗi",
            ["attention"] = "Chú ý",
            ["sync_failed_hint"] = "Một hành động ghi ngoại tuyến không thể gửi và đã bị hủy",
            ["unknown_error"] = "Lỗi không xác định",
            ["start_work_first"] = "Vui lòng nhấn 'Bắt đầu làm việc' trước.",

            // Task Status
            ["completed"] = "Đã hoàn thành",

            // Task Detail
            ["task"] = "Công việc",
            ["notes"] = "Ghi chú",
            ["report_problem"] = "Báo cáo vấn đề",
            ["edit_problem"] = "Chỉnh sửa vấn đề",
            ["edit_note"] = "Chỉnh sửa ghi chú",
            ["name"] = "Tên",
            ["delete"] = "Xóa",
            ["no_problems"] = "Không có vấn đề",
            ["description"] = "Mô tả",
            ["photos"] = "Ảnh",
            ["save"] = "Lưu",
            ["saved"] = "Đã lưu",
            ["delete_problem_title"] = "Xóa vấn đề",
            ["delete_problem_confirm"] = "Bạn có chắc muốn xóa vấn đề này?",
            ["yes_delete"] = "Có, xóa",
            ["problem_reported"] = "Đã báo cáo vấn đề",

            // Images / Notes
            ["add_note"] = "Thêm ghi chú",
            ["no_notes"] = "Không có ghi chú",
            ["no_logs"] = "Không có nhật ký",
            ["no_task_description"] = "Không có mô tả công việc",
            ["camera"] = "Máy ảnh",
            ["gallery"] = "Thư viện",
            ["note"] = "Ghi chú",

            // Buttons
            ["start"] = "Bắt đầu",
            ["stop"] = "Dừng",

            // General
            ["loading"] = "Đang tải...",
            ["connection_error"] = "Lỗi kết nối",
            ["no_connection"] = "Không có kết nối",
            ["saved_offline"] = "Đã lưu. Sẽ đồng bộ khi có kết nối.",
            ["really_logout"] = "Bạn có chắc muốn đăng xuất?",
            ["task_completed"] = "Hoàn thành công việc",
            ["task_completed_question"] = "Bạn có chắc muốn hoàn thành công việc này?",
            ["log"] = "Nhật ký",
            ["delete_task"] = "Xóa công việc",

            // My Tasks
            ["create_auftrag"] = "Tạo Đơn hàng",
            ["edit_auftrag"] = "Sửa Đơn hàng",
            ["messages"] = "Tin nhắn",
            ["administration"] = "Quản trị",
            ["colleagues"] = "Đồng nghiệp",
            ["colleague"] = "Đồng nghiệp",
            ["admin_contact"] = "Quản trị",
            ["task_name_required"] = "Tên công việc *",
            ["apartment"] = "Căn hộ",
            ["date_required"] = "Ngày *",
            ["task_type"] = "Loại công việc",
            ["optional_hint"] = "Mô tả công việc...",
            ["assign_cleaners"] = "Phân công",
            ["cleaning"] = "Dọn dẹp",
            ["check_task"] = "Kiểm tra",
            ["repair"] = "Sửa chữa",
            ["details_tab"] = "Chi tiết",
            ["task_tab"] = "Công việc",
            ["problems_tab"] = "Vấn đề",
            ["notes_tab"] = "Ghi chú",
            ["assign_tab"] = "Phân công",
            ["no_my_tasks"] = "Không có công việc riêng",
            ["task_create_error"] = "Lỗi khi tạo công việc",
            ["task_update_error"] = "Lỗi khi cập nhật công việc",
            ["task_delete_error"] = "Lỗi khi xóa công việc",
            ["confirm_delete_task"] = "Bạn có chắc muốn xóa công việc này?",
            ["update_error"] = "Lỗi cập nhật",
            ["delete_error"] = "Lỗi xóa",
            ["delete_image"] = "Xóa ảnh",
            ["confirm_delete_image"] = "Bạn có chắc muốn xóa ảnh này?",

            // Log translations
            ["log_note_added"] = "Đã thêm ghi chú",
            ["log_image_deleted"] = "Đã xóa ảnh",
            ["log_problem_reported"] = "Đã báo cáo sự cố",
            ["log_problem_deleted"] = "Đã xóa sự cố",
            ["log_task_created"] = "Đã tạo công việc",
            ["log_task_updated"] = "Đã cập nhật công việc",
            ["log_repair_task_created"] = "Đã tạo công việc sửa chữa",
            ["log_cleaning_assigned_to"] = "Dọn dẹp được giao cho",
            ["log_assignment_removed"] = "Đã xóa phân công",
            ["log_progress"] = "Tiến độ",
            ["log_status_changed"] = "Trạng thái đã thay đổi",
            ["log_checklist_updated"] = "Đã cập nhật danh sách",
            ["log_not_started"] = "Chưa bắt đầu",
            ["log_started"] = "Đã bắt đầu",
            ["log_completed"] = "Hoàn thành",

            // Login Screen
            ["login_subtitle"] = "Quản lý dọn dẹp",
            ["login_enterprise_app"] = "Ứng dụng doanh nghiệp:",
            ["login_credentials_info"] = "Nhận thông tin đăng nhập từ quản trị viên.",
            ["login_new_customers"] = "Khách hàng mới:",
            ["login_registration_info"] = "Đăng ký qua email: mail@schwanenburg.de",
            ["login_test_usage"] = "Sử dụng thử:",
            ["login_test_credentials"] = "Property: 1  |  User: tom  |  Mật khẩu: tom",
            ["login_title"] = "Đăng nhập",
            ["login_property_id"] = "ID tài sản",
            ["login_username"] = "Tên đăng nhập",
            ["login_password"] = "Mật khẩu",
            ["login_remember_me"] = "Ghi nhớ đăng nhập",

            // Ergänzt: fehlende/neue Schlüssel (Sync-Prüfung 2026-07-14)
            ["offline"] = "Ngoại tuyến",
            ["create_error"] = "Lỗi khi tạo",
            ["save_error"] = "Lỗi khi lưu",
            ["delete_chat_title"] = "Xóa cuộc trò chuyện",
            ["delete_chat_confirm"] = "Xóa tất cả tin nhắn? Hành động này không thể hoàn tác.",
            ["delete_image_confirm"] = "Xóa hình ảnh khỏi tin nhắn này?",
            ["delete_note"] = "Xóa ghi chú",
            ["delete_note_confirm"] = "Bạn có chắc muốn xóa ghi chú này?",
            ["log_note_created"] = "Đã tạo ghi chú",
            ["message_from"] = "Từ",
            ["notifications"] = "Thông báo",
            ["push_notifications"] = "Thông báo đẩy",
            ["enabled"] = "Đã bật",
            ["not_enabled"] = "Chưa bật",
            ["disabled"] = "Đã tắt",
            ["not_active"] = "Không hoạt động",
            ["notifications_denied_hint"] = "Thông báo chưa hoạt động. Vui lòng cho phép CleanOrga trong cài đặt thiết bị.",
            ["open_settings"] = "Mở cài đặt",
            ["name_required"] = "Vui lòng nhập tên",
            ["network_error_hint"] = "Lỗi mạng. Vui lòng kết nối WiFi hoặc dữ liệu di động.",
            ["select_image_source"] = "Chọn hình ảnh",
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
