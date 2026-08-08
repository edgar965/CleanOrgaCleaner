namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Ukrainische Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TexteUk : LanguagePack
{
    public TexteUk() : base("uk", "Українська", "UA", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
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
        ["new_task"] = "Нове завдання",
        ["notes"] = "Нотатки",
        ["report_problem"] = "Повідомити про проблему",
        ["edit_problem"] = "Редагувати проблему",
        ["edit_note"] = "Редагувати нотатку",
        ["name"] = "Назва",
        ["delete"] = "Видалити",
        ["no_problems"] = "Немає проблем",
        ["description"] = "Опис",
        ["photos"] = "Фотографії",
        ["add_photo"] = "Додати фото",
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
        ["create_auftrag"] = "Нове завдання",
        ["edit_auftrag"] = "Редагувати завдання",
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
    };
}
