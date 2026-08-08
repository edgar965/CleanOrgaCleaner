namespace CleanOrgaCleaner.Views.Hilfen;

/// <summary>
/// Reiterleiste: blendet den gewählten Bereich ein und färbt die Knöpfe.
///
/// Vorher stand die Umschaltung als lange if/switch-Kette je Seite im
/// Code-Behind (AufgabePage rund 70 Zeilen, AuftragPage rund 20) - mit je
/// einer Zeile pro Knopf und Eigenschaft. Hier steht sie einmal.
/// </summary>
public sealed class TabLeiste
{
    private readonly List<TabEintrag> _eintraege;
    private readonly Color _aktivHintergrund;
    private readonly Color _aktivSchrift;
    private readonly Color _passivHintergrund;
    private readonly Color _passivSchrift;
    private readonly Color? _aktivRahmen;
    private readonly bool _fettWennAktiv;

    /// <summary>Kennung des gerade sichtbaren Reiters.</summary>
    public string Aktiv { get; private set; } = "";

    public TabLeiste(
        IEnumerable<TabEintrag> eintraege,
        Color aktivHintergrund,
        Color aktivSchrift,
        Color passivHintergrund,
        Color passivSchrift,
        Color? aktivRahmen = null,
        bool fettWennAktiv = false)
    {
        _eintraege = eintraege.ToList();
        _aktivHintergrund = aktivHintergrund;
        _aktivSchrift = aktivSchrift;
        _passivHintergrund = passivHintergrund;
        _passivSchrift = passivSchrift;
        _aktivRahmen = aktivRahmen;
        _fettWennAktiv = fettWennAktiv;
    }

    /// <summary>Reiter mit dieser Kennung anzeigen, alle anderen ausblenden.</summary>
    public void Zeige(string kennung)
    {
        Aktiv = kennung;

        foreach (var eintrag in _eintraege)
        {
            bool ist = eintrag.Kennung == kennung;

            eintrag.Inhalt.IsVisible = ist;
            eintrag.Knopf.BackgroundColor = ist ? _aktivHintergrund : _passivHintergrund;
            eintrag.Knopf.TextColor = ist ? _aktivSchrift : _passivSchrift;

            if (_aktivRahmen != null)
            {
                eintrag.Knopf.BorderColor = _aktivRahmen;
                eintrag.Knopf.BorderWidth = ist ? 2 : 0;
            }

            if (_fettWennAktiv)
                eintrag.Knopf.FontAttributes = ist ? FontAttributes.Bold : FontAttributes.None;
        }
    }
}
