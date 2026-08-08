using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Chat: Verlauf holen, senden, Anhänge hoch- und wieder wegladen, Übersetzung
/// vorab ansehen, Verlauf löschen.
/// </summary>
public sealed class ChatApi
{
    private readonly ApiHttpKern _http;

    public ChatApi(ApiHttpKern http) => _http = http;

    /// <summary>Nachrichten eines Gesprächs laden (leer bei Fehler).</summary>
    public async Task<List<ChatMessage>> HoleNachrichtenAsync(string partnerId = "admin")
    {
        try
        {
            var antwort = await _http.HoleAsync($"/mobile/api/chat/messages/?partner_id={partnerId}").ConfigureAwait(false);
            if (antwort.Erfolgreich)
                return antwort.Deserialisiere<ChatMessagesResponse>()?.Messages ?? new List<ChatMessage>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] HoleNachrichten Fehler: {ex.Message}");
        }
        return new List<ChatMessage>();
    }

    /// <summary>Nachricht senden (optional mit Anhang-Pfad).</summary>
    public Task<ChatSendResponse> SendeAsync(string text, string empfaengerId = "admin", string? anhang = null)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync("/mobile/api/chat/send/", new
            {
                text = text,
                receiver_id = empfaengerId,
                link_photo_video = anhang ?? ""
            }),
            erfolg => new ChatSendResponse { Success = erfolg },
            meldung => new ChatSendResponse { Success = false, Error = meldung },
            "SendChat");

    /// <summary>Bild/Video für den Chat hochladen.</summary>
    public async Task<ChatImageUploadResponse> LadeAnhangHochAsync(Stream strom, string dateiname)
    {
        try
        {
            var formular = new MultipartFormDataContent();
            var inhalt = new StreamContent(strom);
            inhalt.Headers.ContentType = new MediaTypeHeaderValue(MimeTyp.Fuer(dateiname));
            formular.Add(inhalt, "image", dateiname);

            var antwort = await _http.SendeAsync("/api/chat/upload-image/", formular).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] UploadChatImage: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.IstJsonObjekt)
            {
                return new ChatImageUploadResponse
                {
                    Success = false,
                    Error = antwort.KeineBerechtigung
                        ? "Keine Berechtigung. Bitte neu anmelden."
                        : $"Server-Fehler: {antwort.StatusText}"
                };
            }

            return antwort.Deserialisiere<ChatImageUploadResponse>()
                ?? new ChatImageUploadResponse { Success = false, Error = "Invalid response" };
        }
        catch (Exception ex)
        {
            return new ChatImageUploadResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Noch nicht versendeten Anhang wieder löschen.</summary>
    public Task<ApiResponse> LoescheAnhangAsync(string pfad)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync("/api/chat/delete-image/", new { path = pfad }),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });

    /// <summary>Anhang einer bereits gesendeten Nachricht löschen.</summary>
    public async Task<ApiResponse> LoescheNachrichtenAnhangAsync(int nachrichtId)
    {
        try
        {
            var antwort = await _http.SendeAsync($"/api/chat/message/{nachrichtId}/delete-image/",
                new StringContent("{}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"[API] DeleteMessageImage: {antwort.StatusCode} - {antwort.Auszug()}");

            if (!antwort.IstJsonObjekt)
            {
                if (antwort.KeineBerechtigung)
                    return new ApiResponse { Success = false, Error = "Keine Berechtigung. Bitte neu anmelden." };
                if (antwort.NichtGefunden)
                    return new ApiResponse { Success = false, Error = "Nachricht nicht gefunden." };
                return new ApiResponse { Success = false, Error = $"Server-Fehler: {antwort.StatusText}" };
            }

            return antwort.Deserialisiere<ApiResponse>() ?? new ApiResponse { Success = antwort.Erfolgreich };
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] JSON-Fehler: {ex.Message}");
            return new ApiResponse { Success = false, Error = "Ungültige Server-Antwort" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] DeleteMessageImage Fehler: {ex.Message}");
            return new ApiResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>Übersetzung einer Nachricht vorab ansehen.</summary>
    public Task<TranslationPreviewResponse> ZeigeUebersetzungAsync(string text, string? empfaengerId = null)
        => _http.FrageAsync(
            () => _http.SendeJsonAsync("/mobile/api/chat/preview-translation/", new { text = text, receiver_id = empfaengerId }),
            _ => new TranslationPreviewResponse { Success = false },
            meldung => new TranslationPreviewResponse { Success = false, Message = meldung });

    /// <summary>
    /// Gesprächsverlauf löschen. partner_id MUSS mit - sonst löscht der Server
    /// den Standard-Chat (Verwaltung) statt des geöffneten.
    /// </summary>
    public Task<ApiResponse> LoescheVerlaufAsync(string empfaengerId = "admin")
        => _http.FrageAsync(
            () => _http.SendeOhneInhaltAsync($"/mobile/api/chat/delete/?partner_id={Uri.EscapeDataString(empfaengerId)}"),
            erfolg => new ApiResponse { Success = erfolg },
            meldung => new ApiResponse { Success = false, Error = meldung });
}
