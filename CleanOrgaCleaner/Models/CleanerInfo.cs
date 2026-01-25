using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Information about a cleaner for chat list
/// </summary>
public class CleanerInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();

    // Display Avatar if available, otherwise Initial
    public string DisplayAvatar => !string.IsNullOrEmpty(Avatar) ? Avatar : Initial;

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("is_working")]
    public bool IsWorking { get; set; }
}

/// <summary>
/// Response from cleaners list API
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
