using System.Text.Json;
using System.Text.Json.Serialization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Json;

/// <summary>
/// Source-generated JSON serialization context.
/// Eliminates runtime reflection for System.Text.Json on iOS (AOT).
/// </summary>
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(HeartbeatResponse))]
[JsonSerializable(typeof(TaskStateResponse))]
[JsonSerializable(typeof(ChecklistToggleResponse))]
[JsonSerializable(typeof(LogsResponse))]
[JsonSerializable(typeof(TaskImagesResponse))]
[JsonSerializable(typeof(ChatSendResponse))]
[JsonSerializable(typeof(TranslationPreviewResponse))]
[JsonSerializable(typeof(ChatMessagesResponse))]
[JsonSerializable(typeof(ProblemResponse))]
[JsonSerializable(typeof(WorkTimeResponse))]
[JsonSerializable(typeof(TodayDataResponse))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(AuftragDetailResponse))]
[JsonSerializable(typeof(AuftragsPageDataResponse))]
[JsonSerializable(typeof(CleanersListResponse))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(TaskAssignments))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppJsonContext : JsonSerializerContext
{
}
