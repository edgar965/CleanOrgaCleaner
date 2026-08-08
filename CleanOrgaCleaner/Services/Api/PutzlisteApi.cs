using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Putzliste einer Aufgabe: Einträge abhaken, Anmerkungen und Beweis-Fotos.
/// </summary>
public sealed class PutzlisteApi
{
    private readonly ApiHttpKern _http;

    public PutzlisteApi(ApiHttpKern http) => _http = http;

    /// <summary>Einen Putzlisten-Eintrag abhaken bzw. wieder freigeben.</summary>
    public Task<ChecklistToggleResponse> SchalteEintragAsync(int aufgabenId, int eintragId)
        => _http.FrageAsync(
            () => _http.SendeOhneInhaltAsync($"/mobile/api/task/{aufgabenId}/putzliste/{eintragId}/toggle/"),
            erfolg => new ChecklistToggleResponse { Success = erfolg },
            _ => new ChecklistToggleResponse { Success = false },
            "TogglePutzliste");

    /// <summary>Anmerkung zu einem einzelnen Putzlisten-Eintrag speichern.</summary>
    public Task<ApiResponse> SpeichereEintragKommentarAsync(int aufgabenId, int eintragId, string kommentar)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync($"/mobile/api/task/{aufgabenId}/putzliste/{eintragId}/kommentar/", new { kommentar }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>Anmerkung zur gesamten Checkliste der Aufgabe speichern.</summary>
    public Task<ApiResponse> SpeichereChecklistKommentarAsync(int aufgabenId, string kommentar)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync($"/mobile/api/task/{aufgabenId}/putzliste/kommentar/", new { kommentar }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>Ein Beweis-Foto zu einem Putzlisten-Eintrag hochladen.</summary>
    public async Task<PutzlisteFotoResponse> LadeFotoHochAsync(int aufgabenId, int eintragId, string dateiname, byte[] bytes)
    {
        try
        {
            var antwort = await _http.SendeAsync(
                $"/mobile/api/task/{aufgabenId}/putzliste/{eintragId}/foto/",
                MultipartBauer.MitFoto("foto", dateiname, bytes)).ConfigureAwait(false);

            if (!antwort.Erfolgreich)
                return new PutzlisteFotoResponse { Success = false, Error = $"Server error {antwort.StatusCode}" };

            return antwort.Deserialisiere<PutzlisteFotoResponse>()
                ?? new PutzlisteFotoResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] UploadPutzlisteFoto Fehler: {ex.Message}");
            return new PutzlisteFotoResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Ein Beweis-Foto eines Putzlisten-Eintrags löschen.</summary>
    public Task<ApiResponse> LoescheFotoAsync(int fotoId)
        => _http.FrageAsync(
            () => _http.SendeOhneInhaltAsync($"/mobile/api/task/putzliste/foto/{fotoId}/delete/"),
            erfolg => new ApiResponse { Success = erfolg },
            _ => new ApiResponse { Success = false },
            "DeletePutzlisteFoto");
}
