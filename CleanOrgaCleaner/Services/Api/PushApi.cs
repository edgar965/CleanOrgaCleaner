using System.Text.Json;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Alles rund um Push-Mitteilungen: Geräte-Token an- und abmelden sowie das
/// Firebase-Anmeldetoken für den Echtzeit-Posteingang holen.
/// </summary>
public sealed class PushApi
{
    private readonly ApiHttpKern _http;

    public PushApi(ApiHttpKern http) => _http = http;

    /// <summary>Geräte-Token beim Server anmelden. platform: "android" oder "ios".</summary>
    public Task<ApiResponse> MeldeTokenAnAsync(string token, string platform)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync("/mobile/api/push/register/", new { token, platform }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>Geräte-Token abmelden (z.B. beim Logout).</summary>
    public Task<ApiResponse> MeldeTokenAbAsync(string token)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync("/mobile/api/push/unregister/", new { token }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>
    /// Firebase-Anmeldetoken holen (nach dem Login). Damit meldet sich die App
    /// bei Firebase an und darf ihren Posteingang lesen.
    /// Rückgabe: (Token, Arbeitskraft-Id, Firmen-Id, Firestore aktiv) oder null.
    /// </summary>
    public async Task<(string token, int cleanerId, int propertyId, bool firestoreEnabled)?> HoleFirebaseTokenAsync()
    {
        try
        {
            var antwort = await _http.HoleAsync("/mobile/api/firebase-token/").ConfigureAwait(false);
            if (!antwort.Erfolgreich)
                return null;

            using var doc = JsonDocument.Parse(antwort.Text);
            var wurzel = doc.RootElement;
            if (!(wurzel.TryGetProperty("success", out var erfolg) && erfolg.GetBoolean()))
                return null;

            var token = wurzel.TryGetProperty("token", out var t) ? t.GetString() : null;
            if (string.IsNullOrEmpty(token))
                return null;

            var cleanerId = wurzel.TryGetProperty("cleaner_id", out var c) ? c.GetInt32() : 0;
            var propertyId = wurzel.TryGetProperty("property_id", out var p) ? p.GetInt32() : 1;
            // Firestore ist serverseitig umschaltbar; fehlt das Kennzeichen -> an.
            var firestore = !wurzel.TryGetProperty("firestore_enabled", out var f) || f.GetBoolean();
            return (token, cleanerId, propertyId, firestore);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FS] Firebase-Token holen fehlgeschlagen: {ex.Message}");
            return null;
        }
    }
}
