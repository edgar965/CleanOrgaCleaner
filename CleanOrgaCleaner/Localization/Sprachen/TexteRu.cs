namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Russische Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TexteRu : LanguagePack
{
    public TexteRu() : base("ru", "Русский", "RU", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
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
        ["new_task"] = "Новая задача",
        ["notes"] = "Заметки",
        ["report_problem"] = "Сообщить о проблеме",
        ["edit_problem"] = "Редактировать проблему",
        ["edit_note"] = "Редактировать заметку",
        ["name"] = "Название",
        ["delete"] = "Удалить",
        ["no_problems"] = "Нет проблем",
        ["description"] = "Описание",
        ["photos"] = "Фотографии",
        ["add_photo"] = "Добавить фото",
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
        ["create_auftrag"] = "Новая задача",
        ["edit_auftrag"] = "Редактировать задачу",
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
    };
}
