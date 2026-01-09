using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// User information from authentication
/// </summary>
public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
}
