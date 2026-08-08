using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Arbeitskraft in der Chat-Liste (Kurzform mit Avatar und Ungelesen-Zähler).
/// </summary>
public class CleanerInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("is_working")]
    public bool IsWorking { get; set; }

    /// <summary>
    /// Kein Server-Feld: markiert den synthetischen Verwaltungs-Eintrag (erster
    /// Eintrag der Chat-Liste), damit die Tap-Navigation zwischen Verwaltung und
    /// Kollegin/Kollege unterscheidet.
    /// </summary>
    [JsonIgnore]
    public bool IsAdmin { get; set; }

    /// <summary>Erster Buchstabe des Namens - Platzhalter, wenn kein Avatar da ist.</summary>
    [JsonIgnore]
    public string Initial => Name.Length == 0 ? "?" : char.ToUpperInvariant(Name[0]).ToString();

    /// <summary>Name mit großem Anfangsbuchstaben.</summary>
    [JsonIgnore]
    public string DisplayName => Name.Length == 0 ? "" : char.ToUpperInvariant(Name[0]) + Name[1..];

    /// <summary>Avatar, wenn vorhanden - sonst der Initial-Buchstabe.</summary>
    [JsonIgnore]
    public string DisplayAvatar => string.IsNullOrEmpty(Avatar) ? Initial : Avatar;
}
