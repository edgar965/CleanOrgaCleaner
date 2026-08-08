using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Stammdaten einer Arbeitskraft, wie der Server sie bei der Anmeldung liefert.
/// </summary>
public class Cleaner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>Sprachcode der Oberfläche, z. B. "de".</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "de";

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
}
