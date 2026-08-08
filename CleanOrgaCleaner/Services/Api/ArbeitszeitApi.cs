using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Arbeitstag der Arbeitskraft: Beginn, Ende, aktueller Stand.
///
/// Beginn und Ende unterscheiden sich nur im Endpunkt - sie teilen sich
/// deshalb eine gemeinsame Methode (vorher zwei identische Rümpfe).
/// </summary>
public sealed class ArbeitszeitApi
{
    private readonly ApiHttpKern _http;

    public ArbeitszeitApi(ApiHttpKern http) => _http = http;

    /// <summary>Arbeitsbeginn melden.</summary>
    public Task<WorkTimeResponse> BeginneAsync()
        => MeldeAsync("/mobile/api/cleaner/start-time/", "StartWork");

    /// <summary>Arbeitsende melden.</summary>
    public Task<WorkTimeResponse> BeendeAsync()
        => MeldeAsync("/mobile/api/cleaner/end-time/", "EndWork");

    /// <summary>Aktuellen Arbeitszeit-Stand holen (null bei Fehler).</summary>
    public async Task<WorkTimeResponse?> HoleStandAsync()
    {
        try
        {
            var antwort = await _http.HoleAsync("/mobile/api/cleaner/work-status/").ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] GetWorkStatus: {antwort.StatusCode} - {antwort.Auszug()}");
            return antwort.Erfolgreich ? antwort.Deserialisiere<WorkTimeResponse>() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetWorkStatus Fehler: {ex.Message}");
            return null;
        }
    }

    /// <summary>Gemeinsamer Ablauf für Beginn und Ende (Datum als Nutzdaten).</summary>
    private async Task<WorkTimeResponse> MeldeAsync(string pfad, string merker)
    {
        try
        {
            var antwort = await _http.SendeJsonAsync(pfad, new { date = DateTime.Now.ToString("yyyy-MM-dd") }).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] {merker}: {antwort.StatusCode} - {antwort.Auszug()}");

            if (antwort.Erfolgreich)
                return antwort.Deserialisiere<WorkTimeResponse>() ?? new WorkTimeResponse { Success = true };

            return new WorkTimeResponse { Success = false, Error = antwort.Text };
        }
        catch (Exception ex)
        {
            return new WorkTimeResponse { Success = false, Error = ex.Message };
        }
    }
}
