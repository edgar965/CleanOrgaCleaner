namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Ein noch nicht hochgeladenes Foto: Dateiname und Bilddaten.
///
/// Vorher lagen diese beiden Angaben als namenloses Wertepaar in den Seiten -
/// zusammengehörige Angaben gehören in eine Klasse.
/// </summary>
public sealed class NeuesFoto
{
    public string Dateiname { get; }
    public byte[] Daten { get; }

    public NeuesFoto(string dateiname, byte[] daten)
    {
        Dateiname = dateiname;
        Daten = daten;
    }

    /// <summary>Dateiname mit Zeitstempel, damit nichts überschrieben wird.</summary>
    public static NeuesFoto MitZeitstempel(string praefix, byte[] daten, string endung = ".jpg")
        => new($"{praefix}_{DateTime.Now:yyyyMMdd_HHmmss}{endung}", daten);

    /// <summary>Form, die der Server-Zugriff erwartet.</summary>
    public static List<(string FileName, byte[] Bytes)> FuerUebertragung(IEnumerable<NeuesFoto> fotos)
        => fotos.Select(f => (f.Dateiname, f.Daten)).ToList();

    /// <summary>Nur die Bilddaten - für die Warteschlange ohne Verbindung.</summary>
    public static List<byte[]> NurDaten(IEnumerable<NeuesFoto> fotos)
        => fotos.Select(f => f.Daten).ToList();
}
