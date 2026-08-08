namespace CleanOrgaCleaner.Localization.Sprachen;

/// <summary>
/// Vietnamesische Oberflächentexte.
/// Eine Datei je Sprache - siehe <see cref="TranslationCatalog"/>.
/// </summary>
internal sealed class TexteVi : LanguagePack
{
    public TexteVi() : base("vi", "Tiếng Việt", "VN", Erstellen()) { }

    private static Dictionary<string, string> Erstellen() => new(StringComparer.Ordinal)
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
        ["new_task"] = "Công việc mới",
        ["notes"] = "Ghi chú",
        ["report_problem"] = "Báo cáo vấn đề",
        ["edit_problem"] = "Chỉnh sửa vấn đề",
        ["edit_note"] = "Chỉnh sửa ghi chú",
        ["name"] = "Tên",
        ["delete"] = "Xóa",
        ["no_problems"] = "Không có vấn đề",
        ["description"] = "Mô tả",
        ["photos"] = "Ảnh",
        ["add_photo"] = "Thêm ảnh",
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
        ["create_auftrag"] = "Công việc mới",
        ["edit_auftrag"] = "Sửa công việc",
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
    };
}
