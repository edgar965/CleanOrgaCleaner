using CleanOrgaCleaner.Views.Hilfen;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Reiter im Auftrags-Dialog: Details, Notizen, Zuweisen, Protokoll.
/// </summary>
public partial class AuftragPage
{
    private static readonly Color ReiterAktiv = Color.FromArgb("#2196F3");
    private static readonly Color ReiterPassiv = Color.FromArgb("#e0e0e0");
    private static readonly Color ReiterPassivSchrift = Color.FromArgb("#666");

    private TabLeiste _tabs = null!;

    private void ErzeugeTabLeiste()
    {
        _tabs = new TabLeiste(
            new[]
            {
                new TabEintrag("details", TabDetails, DetailsTabContent),
                new TabEintrag("anmerkungen", TabAnmerkungen, AnmerkungenTabContent),
                new TabEintrag("assign", TabAssign, AssignTabContent),
                new TabEintrag("logs", TabLogs, LogsTabContent)
            },
            aktivHintergrund: ReiterAktiv,
            aktivSchrift: Colors.White,
            passivHintergrund: ReiterPassiv,
            passivSchrift: ReiterPassivSchrift,
            fettWennAktiv: true);

        _tabs.Zeige("details");
    }

    private void OnTabDetailsClicked(object sender, EventArgs e) => _tabs.Zeige("details");

    private void OnTabAnmerkungenClicked(object sender, EventArgs e) => _tabs.Zeige("anmerkungen");

    private void OnTabAssignClicked(object sender, EventArgs e) => _tabs.Zeige("assign");

    private async void OnTabLogsClicked(object sender, EventArgs e)
    {
        _tabs.Zeige("logs");
        try { await LadeProtokollAsync(); }
        catch (Exception ex)
        {
            // async void: ungefangene Exception = App-Crash
            System.Diagnostics.Debug.WriteLine($"[AuftragPage] Protokoll: {ex.Message}");
        }
    }
}
