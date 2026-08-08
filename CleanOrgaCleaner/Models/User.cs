using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Django-Benutzerkonto, das der Anmelde-Endpunkt zurückliefert.
/// </summary>
public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    /// <summary>True bei Verwaltungsrechten im Django-Backend.</summary>
    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
}
