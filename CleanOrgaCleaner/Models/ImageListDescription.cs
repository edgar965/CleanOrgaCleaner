using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Gemeinsames Modell für Probleme und Anmerkungen (löst die früheren Modelle
/// Problem und BildStatus ab). <see cref="Type"/> unterscheidet beide Fälle.
/// </summary>
public class ImageListDescription
{
    /// <summary>Wert von <see cref="Type"/> für ein gemeldetes Problem.</summary>
    public const string TypProblem = "problem";

    /// <summary>Wert von <see cref="Type"/> für eine Anmerkung.</summary>
    public const string TypAnmerkung = "anmerkung";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>"problem" oder "anmerkung" - siehe <see cref="TypProblem"/>.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("photos")]
    public List<ImageListDescriptionPhoto>? Photos { get; set; }

    [JsonPropertyName("erstellt_am")]
    public string? ErstelltAm { get; set; }

    [JsonPropertyName("erledigt")]
    public bool Erledigt { get; set; }

    /// <summary>True, wenn mindestens ein Foto hängt.</summary>
    [JsonIgnore]
    public bool HasPhotos => Photos is { Count: > 0 };

    /// <summary>True bei einem gemeldeten Problem.</summary>
    [JsonIgnore]
    public bool IsProblem => Type == TypProblem;
}
