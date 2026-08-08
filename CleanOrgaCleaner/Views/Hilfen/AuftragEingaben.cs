namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Die Eingaben des Auftrags-Dialogs in einem Stück.
///
/// Vorher wurden dieselben sechs Felder an zwei Stellen einzeln eingesammelt
/// und weitergereicht - zusammengehörige Angaben gehören in eine Klasse.
/// </summary>
public sealed class AuftragEingaben
{
    public string Name { get; }
    public string GeplantesDatum { get; }
    public int? ApartmentId { get; }
    public int? AufgabenartId { get; }
    public string? Hinweis { get; }
    public string Status { get; }

    /// <param name="datum">
    /// Datum aus dem Auswahlfeld. Es kann leer sein - dann gilt der heutige Tag,
    /// weil der Server ein geplantes Datum verlangt.
    /// </param>
    public AuftragEingaben(string name, DateTime? datum, int? apartmentId, int? aufgabenartId,
        string? hinweis, string status)
    {
        Name = name;
        GeplantesDatum = $"{datum ?? DateTime.Today:yyyy-MM-dd}";
        ApartmentId = apartmentId;
        AufgabenartId = aufgabenartId;
        Hinweis = hinweis;
        Status = status;
    }

    /// <summary>Ohne Namen lässt sich kein Auftrag anlegen.</summary>
    public bool IstVollstaendig => !string.IsNullOrEmpty(Name);
}
