using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Ein Protokolleintrag zur Verlaufsanzeige einer Aufgabe.
/// </summary>
public class LogEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("datum_zeit")]
    public string DatumZeit { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>Wer den Eintrag ausgelöst hat.</summary>
    [JsonPropertyName("user")]
    public string User { get; set; } = "";
}
