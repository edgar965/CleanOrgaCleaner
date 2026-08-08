using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort des Anmelde-Endpunkts.
/// </summary>
public class LoginResponse : ServerAntwort
{
    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("cleaner")]
    public Cleaner? Cleaner { get; set; }
}
