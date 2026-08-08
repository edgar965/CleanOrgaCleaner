using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter der Aufgabenansicht: Aufgabe, Checkliste, Probleme, Notizen, Protokoll.
/// </summary>
public partial class AufgabePage
{
    private static readonly Color ReiterDunkel = Color.FromArgb("#1a3a5c");

    private TabLeiste _tabs = null!;

    private void ErzeugeTabLeiste()
    {
        _tabs = new TabLeiste(
            new[]
            {
                new TabEintrag("aufgabe", TabAufgabeButton, TabAufgabeContent),
                new TabEintrag("checkliste", TabChecklisteButton, TabChecklisteContent),
                new TabEintrag("probleme", TabProblemeButton, TabProblemeContent),
                new TabEintrag("anmerkungen", TabAnmerkungenButton, TabAnmerkungenContent),
                new TabEintrag("logs", TabLogsButton, TabLogsContent)
            },
            aktivHintergrund: Colors.White,
            aktivSchrift: ReiterDunkel,
            passivHintergrund: ReiterDunkel,
            passivSchrift: Colors.White,
            aktivRahmen: ReiterDunkel);

        _tabs.Zeige("aufgabe");
    }

    private void OnTabAufgabeClicked(object sender, EventArgs e) => _tabs.Zeige("aufgabe");

    private void OnTabChecklisteClicked(object sender, EventArgs e)
    {
        _tabs.Zeige("checkliste");
        BuildCheckliste();
    }

    private void OnTabProblemeClicked(object sender, EventArgs e) => _tabs.Zeige("probleme");

    private void OnTabAnmerkungenClicked(object sender, EventArgs e) => _tabs.Zeige("anmerkungen");

    private async void OnTabLogsClicked(object sender, EventArgs e)
    {
        _tabs.Zeige("logs");
        try { await LadeProtokollAsync(); }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AufgabePage] Protokoll: {ex.Message}");
        }
    }
}
