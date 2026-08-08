namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Ein bereits auf dem Server liegendes Foto: Kennung und Adresse.
///
/// Vorher lagen diese beiden Angaben als namenloses Wertepaar in der Seite -
/// zusammengehörige Angaben gehören in eine Klasse.
/// </summary>
public sealed class ServerFoto
{
    public int Id { get; }
    public string Url { get; }

    public ServerFoto(int id, string url)
    {
        Id = id;
        Url = url;
    }
}
