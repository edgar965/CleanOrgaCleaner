using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Cleaner information from the server
/// </summary>
public class Cleaner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "de";

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    // UI helper
    public string DisplayName => string.IsNullOrEmpty(Name) ? "Unbekannt" : Name;
}
