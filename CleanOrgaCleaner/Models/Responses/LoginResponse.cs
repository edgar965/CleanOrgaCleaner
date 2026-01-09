using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Response from the login API endpoint
/// </summary>
public class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("cleaner")]
    public Cleaner? Cleaner { get; set; }
}

/// <summary>
/// Result object for login operation (used internally)
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CleanerName { get; set; }
    public string? CleanerLanguage { get; set; }
}
