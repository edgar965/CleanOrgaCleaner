namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Spanische Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TexteEs : LanguagePack
{
    public TexteEs() : base("es", "Español", "ES", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
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
        ["new_task"] = "Nueva Tarea",
        ["notes"] = "Notas",
        ["report_problem"] = "Reportar problema",
        ["edit_problem"] = "Editar problema",
        ["edit_note"] = "Editar nota",
        ["name"] = "Nombre",
        ["delete"] = "Eliminar",
        ["no_problems"] = "Sin problemas reportados",
        ["description"] = "Descripción",
        ["photos"] = "Fotos",
        ["add_photo"] = "Añadir foto",
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
        ["create_auftrag"] = "Nueva Tarea",
        ["edit_auftrag"] = "Editar Tarea",
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
    };
}
