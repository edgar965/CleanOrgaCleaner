using System.Text.Json;
using System.Text.Json.Serialization;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Json;

/// <summary>
/// Quellcode-generierter JSON-Kontext. Vermeidet Reflexion zur Laufzeit
/// (Voraussetzung für den AOT-Build auf iOS).
///
/// WICHTIG: Jeder Typ, der mit <c>AppJsonContext.Default.Options</c>
/// (de)serialisiert wird, muss hier eingetragen sein - sonst schlägt der Aufruf
/// zur Laufzeit fehl. Verschachtelte Typen (z. B. Listenelemente) erzeugt der
/// Generator selbst.
/// </summary>
// Allgemeine Antworten
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(HeartbeatResponse))]
[JsonSerializable(typeof(LoginResponse))]
// Aufgaben
[JsonSerializable(typeof(TodayDataResponse))]
[JsonSerializable(typeof(TaskStateResponse))]
[JsonSerializable(typeof(ChecklistToggleResponse))]
[JsonSerializable(typeof(LogsResponse))]
[JsonSerializable(typeof(TaskImagesResponse))]
[JsonSerializable(typeof(AuftragsPageDataResponse))]
[JsonSerializable(typeof(TaskAssignments))]
// Probleme / Anmerkungen
[JsonSerializable(typeof(ImageListDescription))]
[JsonSerializable(typeof(ImageListDescriptionPhoto))]
[JsonSerializable(typeof(List<ImageListDescription>))]
[JsonSerializable(typeof(List<ImageListDescriptionPhoto>))]
[JsonSerializable(typeof(ImageListDescriptionResponse))]
[JsonSerializable(typeof(ImageListDescriptionDeleteResponse))]
[JsonSerializable(typeof(ImageListItemsResponse))]
// Putzliste
[JsonSerializable(typeof(PutzlisteEintrag))]
[JsonSerializable(typeof(PutzlisteBild))]
[JsonSerializable(typeof(List<PutzlisteEintrag>))]
[JsonSerializable(typeof(List<PutzlisteBild>))]
[JsonSerializable(typeof(PutzlisteFotoResponse))]
// Chat
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(ChatMessagesResponse))]
[JsonSerializable(typeof(ChatSendResponse))]
[JsonSerializable(typeof(ChatImageUploadResponse))]
[JsonSerializable(typeof(TranslationPreviewResponse))]
[JsonSerializable(typeof(CleanersListResponse))]
// Arbeitszeit
[JsonSerializable(typeof(WorkTimeResponse))]
// Offline-Ablage und Absturzberichte
[JsonSerializable(typeof(CrashReport))]
[JsonSerializable(typeof(List<CrashReport>))]
[JsonSerializable(typeof(TaskCacheData))]
[JsonSerializable(typeof(LoginStateCache))]
// Rohzugriff für handgeschriebenes Parsen
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppJsonContext : JsonSerializerContext
{
}
