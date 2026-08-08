using System.Text.Json;

namespace CleanOrgaCleaner.Services.Offline;

/// <summary>
/// Ein nachzuholender Vorgang aus der Offline-Warteschlange.
///
/// Früher lag die Ausführung in einer langen switch-Anweisung mit elf
/// privaten Methoden im OfflineQueueService. Jetzt kennt jede Vorgangsart
/// ihre eigene Klasse; der Dienst kennt nur noch <see cref="AusfuehrenAsync"/>.
/// </summary>
public abstract class WarteschlangenAufgabe
{
    /// <summary>Bereits geparste Nutzdaten des Warteschlangen-Eintrags.</summary>
    protected JsonElement Daten { get; }

    protected WarteschlangenAufgabe(JsonElement daten) => Daten = daten;

    /// <summary>
    /// Vorgang beim Server nachholen. true = erledigt (Eintrag darf weg),
    /// false = erneut versuchen.
    /// </summary>
    public abstract Task<bool> AusfuehrenAsync(ApiService api);

    /// <summary>Pflichtfeld als Text (fehlt es, ist der Eintrag defekt).</summary>
    protected string? PflichtText(string feld) => Daten.GetProperty(feld).GetString();

    /// <summary>Pflichtfeld als Zahl.</summary>
    protected int PflichtZahl(string feld) => Daten.GetProperty(feld).GetInt32();

    /// <summary>Pflichtfeld als Ja/Nein.</summary>
    protected bool PflichtJaNein(string feld) => Daten.GetProperty(feld).GetBoolean();

    /// <summary>Wahlfreies Textfeld (ältere Einträge kennen es evtl. nicht).</summary>
    protected string? Text(string feld, string? standard = null)
        => Daten.TryGetProperty(feld, out var wert) && wert.ValueKind != JsonValueKind.Null
            ? wert.GetString() ?? standard
            : standard;

    /// <summary>Wahlfreies Zahlenfeld.</summary>
    protected int? ZahlOderNull(string feld)
        => Daten.TryGetProperty(feld, out var wert) && wert.ValueKind != JsonValueKind.Null
            ? wert.GetInt32()
            : null;

    /// <summary>Fotos eines Feldes (Base64-Liste) einlesen.</summary>
    protected List<(string, byte[])>? Fotos(string feld)
    {
        if (!Daten.TryGetProperty(feld, out var liste) || liste.ValueKind != JsonValueKind.Array)
            return null;

        var fotos = new List<(string, byte[])>(liste.GetArrayLength());
        var nummer = 0;
        foreach (var eintrag in liste.EnumerateArray())
        {
            fotos.Add(($"photo_{nummer}.jpg", Convert.FromBase64String(eintrag.GetString() ?? "")));
            nummer++;
        }
        return fotos;
    }
}
