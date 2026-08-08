using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort der API mit allen Arbeitskräften für die Chat-Liste.
/// </summary>
public class CleanersListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("cleaners")]
    public List<CleanerInfo> Cleaners { get; set; } = new();

    [JsonPropertyName("admin_avatar")]
    public string AdminAvatar { get; set; } = "";
}
