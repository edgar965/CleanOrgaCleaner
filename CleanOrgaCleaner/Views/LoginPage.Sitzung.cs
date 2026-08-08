using CleanOrgaCleaner.Localization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views;

/// <summary>
/// Gemeinsamer Abschluss aller Anmeldewege: Sprache setzen, Dienste starten,
/// zur Tagesliste wechseln.
///
/// Vorher stand dieser Ablauf zweimal fast identisch in der automatischen und
/// in der manuellen Anmeldung.
/// </summary>
public partial class LoginPage
{
    /// <summary>Zielseite nach erfolgreicher Anmeldung.</summary>
    private const string Startseite = "//MainTabs/TodayPage";

    /// <summary>Sprache der Person übernehmen und merken.</summary>
    private string SpracheUebernehmen(string? sprache)
    {
        var gewaehlt = sprache ?? "de";
        Preferences.Set("language", gewaehlt);
        Translations.CurrentLanguage = gewaehlt;
        Log($"language={gewaehlt}");
        return gewaehlt;
    }

    /// <summary>
    /// Hintergrunddienste starten und zur Tagesliste wechseln.
    ///
    /// Die Task.Yield-Aufrufe sind Absicht: iOS braucht zwischen den Schritten
    /// Luft auf dem Anzeige-Thread, sonst friert die Anmeldung ein.
    /// </summary>
    private async Task SitzungStartenAsync(bool biometrieAnbieten)
    {
        await Task.Yield();

        Log("StartHeartbeat");
        _apiService.StartHeartbeat();

        await Task.Yield();

        if (biometrieAnbieten)
        {
            Log("PromptBiometric START");
            await BiometrieAnbietenAsync();
            Log("PromptBiometric DONE");
            await Task.Yield();
        }

        Log("InitializeWebSocketAsync");
        _ = App.InitializeWebSocketAsync();
        _ = PushService.InitializeAsync();

        // Liegengebliebene Absturzberichte nebenher senden
        CrashReportService.Instance.TrySendPendingReportsInBackground();

        await Task.Yield();

        await NavigiereZurStartseiteAsync();
    }

    private async Task NavigiereZurStartseiteAsync()
    {
        Log("GoToAsync START");
        _navigiert = true;
        await Shell.Current.GoToAsync(Startseite);
        Log("GoToAsync DONE");
    }

    /// <summary>
    /// Knopf nur zurücksetzen, solange die Seite noch sichtbar ist - nach der
    /// Navigation würde das auf iOS auf abgebaute Views zugreifen.
    /// </summary>
    private void KnopfZuruecksetzenWennNochHier()
    {
        if (!_navigiert)
            KnopfFreigeben();
    }
}
