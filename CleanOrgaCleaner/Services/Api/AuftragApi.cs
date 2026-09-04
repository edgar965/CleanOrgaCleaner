using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Aufträge der Arbeitskraft: Übersicht laden, anlegen, ändern, löschen.
///
/// Anlegen und Ändern schicken denselben Datensatz an unterschiedliche
/// Endpunkte - der Rumpf steht deshalb nur noch einmal hier.
/// </summary>
public sealed class AuftragApi
{
    private readonly ApiHttpKern _http;

    public AuftragApi(ApiHttpKern http) => _http = http;

    /// <summary>Daten der Auftragsseite laden.</summary>
    public async Task<AuftragsPageDataResponse> HoleUebersichtAsync()
    {
        try
        {
            var antwort = await _http.HoleAsync("/mobile/api/my-tasks-data/").ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] GetAuftragsData: {antwort.StatusCode}");

            if (!antwort.Erfolgreich)
                return new AuftragsPageDataResponse { Success = false, Error = $"HTTP {antwort.StatusCode}" };

            return antwort.Deserialisiere<AuftragsPageDataResponse>()
                ?? new AuftragsPageDataResponse { Success = false, Error = "Parse error" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetAuftragsData Fehler: {ex.Message}");
            return new AuftragsPageDataResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Neuen Auftrag anlegen.</summary>
    public Task<ApiResponse> LegeAnAsync(string titel, string geplantesDatum, int? apartmentId,
        int? aufgabenartId, string? hinweis, string status, TaskAssignments? zuordnungen)
        => SchreibeAsync("/mobile/api/task/create/", "CreateAuftrag",
            titel, geplantesDatum, apartmentId, aufgabenartId, hinweis, status, zuordnungen);

    /// <summary>Bestehenden Auftrag ändern.</summary>
    public Task<ApiResponse> AendereAsync(int aufgabenId, string titel, string geplantesDatum, int? apartmentId,
        int? aufgabenartId, string? hinweis, string status, TaskAssignments? zuordnungen)
        => SchreibeAsync($"/mobile/api/task/{aufgabenId}/update/", "UpdateAuftrag",
            titel, geplantesDatum, apartmentId, aufgabenartId, hinweis, status, zuordnungen);

    /// <summary>Auftrag löschen.</summary>
    public async Task<ApiResponse> LoescheAsync(int aufgabenId)
    {
        try
        {
            // Bewusst schlichter StringContent("{}") wie bisher - der Endpunkt
            // erwartet lediglich einen nicht-leeren Rumpf.
            var antwort = await _http.SendeAsync($"/mobile/api/task/{aufgabenId}/delete/",
                new StringContent("{}")).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] DeleteAuftrag: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.Erfolgreich)
                return new ApiResponse { Success = false, Error = $"HTTP {antwort.StatusCode}: {antwort.Text}" };

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] DeleteAuftrag Fehler: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Gemeinsamer Rumpf für Anlegen und Ändern.</summary>
    private async Task<ApiResponse> SchreibeAsync(string pfad, string merker, string titel, string geplantesDatum,
        int? apartmentId, int? aufgabenartId, string? hinweis, string status, TaskAssignments? zuordnungen)
    {
        try
        {
            var daten = new
            {
                // name wird mitgeschickt, damit die Aufgabe in den Weblisten
                // denselben Text traegt wie auf der Kachel.
                name = titel,
                titel = titel,
                planned_date = geplantesDatum,
                apartment_id = apartmentId,
                aufgabenart_id = aufgabenartId,
                aufgabe = hinweis ?? "",
                status = status,
                assignments = zuordnungen ?? new TaskAssignments
                {
                    Cleaning = new List<int>(),
                    Check = null,
                    Repare = new List<int>()
                }
            };

            var antwort = await _http.SendeJsonAsync(pfad, daten).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] {merker}: {antwort.StatusCode} - {antwort.Auszug()}");

            // Django schickt bei abgelaufener Anmeldung eine HTML-Seite - die
            // darf nicht als Erfolg durchgehen.
            if (string.IsNullOrWhiteSpace(antwort.Text) || !antwort.IstJsonObjekt)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = antwort.Erfolgreich ? "Ungueltiges Antwortformat" : $"Server-Fehler: {antwort.StatusText}"
                };
            }

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }
}
