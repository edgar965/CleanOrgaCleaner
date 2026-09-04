using System.Text.Json;
using CleanOrgaCleaner.Json;
using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Services.Offline;

/// <summary>
/// Die Felder eines eingereihten Auftrags - als Klasse mit benannten
/// Eigenschaften statt wiederholter TryGetProperty-Ketten. Anlegen und Ändern
/// nutzen dieselbe Auswertung.
/// </summary>
public sealed class AuftragFelder
{
    public string Titel { get; private set; } = "";
    public string GeplantesDatum { get; private set; } = "";
    public int? ApartmentId { get; private set; }
    public int? AufgabenartId { get; private set; }
    public string? Hinweis { get; private set; }
    public string Status { get; private set; } = "offen";
    public TaskAssignments? Zuordnungen { get; private set; }

    /// <summary>Felder aus den Nutzdaten eines Warteschlangen-Eintrags lesen.</summary>
    public static AuftragFelder Lies(JsonElement daten)
    {
        var felder = new AuftragFelder
        {
            // "name" ist die alte Schreibweise: Eintraege, die vor dem
            // Titelfeld in der Warteschlange lagen, sollen nicht verfallen.
            Titel = LiesText(daten, "titel") ?? LiesText(daten, "name") ?? "",
            GeplantesDatum = LiesText(daten, "plannedDate") ?? "",
            ApartmentId = LiesZahl(daten, "apartmentId"),
            AufgabenartId = LiesZahl(daten, "aufgabenartId"),
            Hinweis = LiesText(daten, "hinweis"),
            Status = LiesText(daten, "status") ?? "offen"
        };

        if (daten.TryGetProperty("assignments", out var zuordnungen) && zuordnungen.ValueKind != JsonValueKind.Null)
        {
            felder.Zuordnungen = JsonSerializer.Deserialize(
                zuordnungen.GetRawText(), AppJsonContext.Default.TaskAssignments);
        }
        return felder;
    }

    private static string? LiesText(JsonElement daten, string feld)
        => daten.TryGetProperty(feld, out var wert) && wert.ValueKind != JsonValueKind.Null
            ? wert.GetString()
            : null;

    private static int? LiesZahl(JsonElement daten, string feld)
        => daten.TryGetProperty(feld, out var wert) && wert.ValueKind != JsonValueKind.Null
            ? wert.GetInt32()
            : null;
}
