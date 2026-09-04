using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models.Responses;

/// <summary>
/// Antwort für die Seite "eigene Aufgaben": Aufgaben plus die Stammdaten für
/// die Auswahllisten (Apartments, Aufgabenarten, Arbeitskräfte).
/// </summary>
public class AuftragsPageDataResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("tasks")]
    public List<Auftrag>? Tasks { get; set; }

    [JsonPropertyName("apartments")]
    public List<ApartmentInfo>? Apartments { get; set; }

    [JsonPropertyName("aufgabenarten")]
    public List<AufgabenartInfo>? Aufgabenarten { get; set; }

    /// <summary>
    /// Vorauswahl der Firma (Konfiguration → Aufgaben, Spalte "Default").
    /// Ist keine gesetzt, kommt hier null - dann bleibt es beim bisherigen
    /// Verhalten (keine Vorauswahl, Aufgabenname "Reparatur").
    /// </summary>
    [JsonPropertyName("standard_aufgabenart")]
    public AufgabenartInfo? StandardAufgabenart { get; set; }

    [JsonPropertyName("cleaners")]
    public List<CleanerInfo>? Cleaners { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
