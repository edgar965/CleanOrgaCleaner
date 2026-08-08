namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Zustand der angemeldeten Arbeitskraft (Name, Sprache, Id).
///
/// Bewusst eine eigene kleine Klasse statt loser Felder im ApiService: Login,
/// Abmeldung, Spracheinstellung und Crash-Berichte greifen alle darauf zu -
/// so gibt es genau eine Stelle, an der dieser Zustand geändert wird.
/// </summary>
public sealed class Sitzung
{
    /// <summary>Anzeigename der angemeldeten Arbeitskraft.</summary>
    public string? Name { get; private set; }

    /// <summary>Sprachkürzel vom Server (de, en, ...).</summary>
    public string? Sprache { get; private set; }

    /// <summary>Datenbank-Id der Arbeitskraft.</summary>
    public int? Id { get; private set; }

    /// <summary>Nach erfolgreichem Login setzen.</summary>
    public void Uebernehme(string? name, string? sprache, int? id)
    {
        Name = name;
        Sprache = sprache;
        Id = id;
    }

    /// <summary>
    /// Offline-Modus: der Server ist nicht erreichbar, Name/Id kommen aus dem
    /// lokalen Zwischenspeicher.
    /// </summary>
    public void UebernehmeOffline(string name, int? id)
    {
        Name = name;
        Id = id;
        System.Diagnostics.Debug.WriteLine($"[ApiService] Offline cleaner info set: {name}, ID: {id}");
    }

    /// <summary>Nur die Sprache ändern (Einstellungen-Seite).</summary>
    public void SetzeSprache(string sprache) => Sprache = sprache;

    /// <summary>Beim Abmelden alles verwerfen.</summary>
    public void Leere()
    {
        Name = null;
        Sprache = null;
        Id = null;
    }
}
