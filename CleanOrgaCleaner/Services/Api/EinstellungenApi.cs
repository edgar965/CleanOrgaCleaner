using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Einstellungen der Arbeitskraft (Sprache, Avatar) und die Liste der Kollegen.
/// </summary>
public sealed class EinstellungenApi
{
    private readonly ApiHttpKern _http;
    private readonly Sitzung _sitzung;

    public EinstellungenApi(ApiHttpKern http, Sitzung sitzung)
    {
        _http = http;
        _sitzung = sitzung;
    }

    /// <summary>Sprache setzen; bei Erfolg auch in der laufenden Sitzung.</summary>
    public async Task<ApiResponse> SetzeSpracheAsync(string sprache)
    {
        try
        {
            var antwort = await _http.SendeJsonAsync("/mobile/api/cleaner/language/", new { language = sprache }).ConfigureAwait(false);
            if (antwort.Erfolgreich)
                _sitzung.SetzeSprache(sprache);

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Avatar-Kürzel setzen.</summary>
    public Task<ApiResponse> SetzeAvatarAsync(string avatar)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync("/mobile/api/cleaner/avatar/", new { avatar }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>Liste der Arbeitskräfte (null bei Fehler).</summary>
    public async Task<CleanersListResponse?> HoleArbeitskraefteAsync()
    {
        try
        {
            var antwort = await _http.HoleAsync("/mobile/api/cleaners/").ConfigureAwait(false);
            return antwort.Erfolgreich ? antwort.Deserialisiere<CleanersListResponse>() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] HoleArbeitskraefte Fehler: {ex.Message}");
            return null;
        }
    }
}
