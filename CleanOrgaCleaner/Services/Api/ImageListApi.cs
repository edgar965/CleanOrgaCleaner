using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Einträge mit Bildliste und Beschreibung (Probleme, Anmerkungen, Fotos zur
/// Aufgabenbeschreibung) - anlegen, ändern, löschen, laden.
/// </summary>
public sealed class ImageListApi
{
    private readonly ApiHttpKern _http;

    public ImageListApi(ApiHttpKern http) => _http = http;

    /// <summary>Neuen Eintrag mit Fotos anlegen ('problem', 'anmerkung', 'aufgabe').</summary>
    public async Task<ImageListDescriptionResponse> LegeAnAsync(
        int aufgabenId, string typ, string name, string? beschreibung,
        List<(string FileName, byte[] Bytes)>? fotos)
    {
        try
        {
            var formular = MultipartBauer.MitTextUndFotos(name, beschreibung, beschreibungImmer: false, "images", fotos);
            var antwort = await _http.SendeAsync($"/api/task/{aufgabenId}/items/{typ}/create/", formular).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] LegeAn {typ}: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.Erfolgreich)
            {
                return new ImageListDescriptionResponse
                {
                    Success = false,
                    Error = $"Server error {antwort.StatusCode}: {antwort.Auszug(100)}"
                };
            }

            return antwort.Deserialisiere<ImageListDescriptionResponse>()
                ?? new ImageListDescriptionResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] LegeAn Fehler: {ex.Message}");
            return new ImageListDescriptionResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Eintrag ändern (Antwort mit Eintragsdaten).</summary>
    public Task<ImageListDescriptionResponse> AendereAsync(int eintragId, string name, string? beschreibung)
        => _http.FrageAsync(
            () => AendereRohAsync(eintragId, name, beschreibung ?? ""),
            erfolg => new ImageListDescriptionResponse { Success = erfolg },
            meldung => new ImageListDescriptionResponse { Success = false, Error = meldung },
            "UpdateImageListItem");

    /// <summary>
    /// Eintrag ändern (schlichte Erfolg/Fehler-Antwort). Nutzt denselben
    /// Endpunkt wie <see cref="AendereAsync"/> - der Aufrufer braucht hier nur
    /// die Erfolgsmeldung.
    /// </summary>
    public async Task<ApiResponse> AendereEinfachAsync(int eintragId, string name, string beschreibung)
    {
        try
        {
            var antwort = await AendereRohAsync(eintragId, name, beschreibung).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] UpdateImageListDescription: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.Erfolgreich)
                return new ApiResponse { Success = false, Error = $"HTTP {antwort.StatusCode}: {antwort.Text}" };

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] UpdateImageListDescription Fehler: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Eintrag löschen.</summary>
    public Task<ImageListDescriptionDeleteResponse> LoescheAsync(int eintragId)
        => _http.FrageAsync(
            () => _http.SendeOhneInhaltAsync($"/api/image-list/{eintragId}/delete/"),
            erfolg => new ImageListDescriptionDeleteResponse { Success = erfolg },
            meldung => new ImageListDescriptionDeleteResponse { Success = false, Error = meldung },
            "DeleteImageListItem");

    /// <summary>Ein einzelnes Foto eines Eintrags löschen.</summary>
    public Task<ApiResponse> LoescheFotoAsync(int fotoId)
        => _http.FrageAsync(
            () => _http.SendeOhneInhaltAsync($"/api/image-list/photo/{fotoId}/delete/"),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung },
            "DeleteImageListPhoto");

    /// <summary>Ein weiteres Foto an einen bestehenden Eintrag anhängen.</summary>
    public async Task<ApiResponse> HaengeFotoAnAsync(int eintragId, byte[] fotoBytes)
    {
        try
        {
            var antwort = await _http.SendeAsync(
                $"/api/image-list/{eintragId}/add-photo/",
                MultipartBauer.MitFoto("photo", "photo.jpg", fotoBytes)).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] AddPhoto: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.Erfolgreich)
                return new ApiResponse { Success = false, Error = $"HTTP {antwort.StatusCode}: {antwort.Text}" };

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] AddPhoto Fehler: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Anmerkung mit Fotos anlegen (Feldname "photos", fortlaufende Dateinamen).</summary>
    public async Task<ApiResponse> LegeAnmerkungAnAsync(int aufgabenId, string name, string beschreibung, List<byte[]> fotos)
    {
        try
        {
            var benannt = new List<(string, byte[])>(fotos.Count);
            for (var i = 0; i < fotos.Count; i++)
                benannt.Add(($"photo_{i}.jpg", fotos[i]));

            var formular = MultipartBauer.MitTextUndFotos(name, beschreibung, beschreibungImmer: true, "photos", benannt);
            var antwort = await _http.SendeAsync($"/api/task/{aufgabenId}/items/anmerkung/create/", formular).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] LegeAnmerkungAn: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.Erfolgreich)
                return new ApiResponse { Success = false, Error = $"HTTP {antwort.StatusCode}: {antwort.Text}" };

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] LegeAnmerkungAn Fehler: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Einträge eines Typs zu einer Aufgabe laden ('problem', 'anmerkung' oder
    /// 'aufgabe'). Die Foto-Adressen werden vervollständigt.
    /// </summary>
    public async Task<List<ImageListDescription>> HoleEintraegeAsync(int aufgabenId, string typ)
    {
        try
        {
            var antwort = await _http.HoleAsync($"/api/task/{aufgabenId}/items/{typ}/").ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] HoleEintraege {typ} fuer Task {aufgabenId}: {antwort.StatusCode}");

            if (!antwort.Erfolgreich)
                return new List<ImageListDescription>();

            var ergebnis = antwort.Deserialisiere<ImageListItemsResponse>();
            if (ergebnis?.Items == null)
                return new List<ImageListDescription>();

            VervollstaendigeAdressen(ergebnis.Items);
            return ergebnis.Items;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] HoleEintraege Fehler: {ex.Message}");
            return new List<ImageListDescription>();
        }
    }

    /// <summary>Der eine Änderungs-Endpunkt, den beide Änderungs-Methoden nutzen.</summary>
    private Task<HttpAntwort> AendereRohAsync(int eintragId, string name, string beschreibung)
        => _http.SendeJsonAsync($"/api/image-list/{eintragId}/update/", new { name, description = beschreibung });

    /// <summary>Relative Foto-Adressen zu vollständigen ergänzen.</summary>
    private static void VervollstaendigeAdressen(List<ImageListDescription> eintraege)
    {
        foreach (var eintrag in eintraege)
        {
            if (eintrag.Photos == null)
                continue;
            foreach (var foto in eintrag.Photos)
            {
                foto.Url = UrlHelfer.AbsolutOderNull(foto.Url);
                foto.ThumbnailUrl = UrlHelfer.AbsolutOderNull(foto.ThumbnailUrl);
            }
        }
    }
}
