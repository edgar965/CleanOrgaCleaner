namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Ein Reiter einer <see cref="TabLeiste"/>: Kennung, Knopf und der Bereich,
/// der dazu eingeblendet wird.
/// </summary>
public sealed class TabEintrag
{
    public string Kennung { get; }
    public Button Knopf { get; }
    public View Inhalt { get; }

    public TabEintrag(string kennung, Button knopf, View inhalt)
    {
        Kennung = kennung;
        Knopf = knopf;
        Inhalt = inhalt;
    }
}
