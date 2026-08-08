using System.Net;
using System.Text;
using System.Text.Json;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Gemeinsamer HTTP-Kern aller API-Klassen.
///
/// Hält genau EINEN HttpClient samt Cookie-Container (die Server-Session der
/// App) und kapselt die früher rund 30-mal kopierte Abfolge
/// "senden -> Text lesen -> deserialisieren -> Fehler in ein Antwortobjekt
/// verwandeln". Die fachlichen Api-Klassen (AuthApi, ChatApi, ...) enthalten
/// dadurch nur noch Pfad, Nutzdaten und Zielmodell.
/// </summary>
public sealed class ApiHttpKern
{
    /// <summary>Basisadresse des Servers - eine einzige Quelle der Wahrheit.</summary>
    public const string BasisUrl = "https://cleanorga.com";

    private readonly HttpClientHandler _handler;
    private readonly HttpClient _client;

    public ApiHttpKern()
    {
        _handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        _client = new HttpClient(_handler)
        {
            BaseAddress = new Uri(BasisUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store");
        _client.DefaultRequestHeaders.Add("Pragma", "no-cache");
    }

    /// <summary>
    /// Cookie-Container der Session. Wird beim Abmelden ausgetauscht und vom
    /// synchronen Login (HttpWebRequest) mitbenutzt, damit beide Wege dieselbe
    /// Session teilen.
    /// </summary>
    public CookieContainer CookieContainer
    {
        get => _handler.CookieContainer;
        set => _handler.CookieContainer = value;
    }

    /// <summary>Cookies dieser Session für einen URI auslesen.</summary>
    public CookieCollection LiesCookies()
        => _handler.CookieContainer.GetCookies(new Uri(BasisUrl));

    /// <summary>GET auf einen (relativen) Pfad; Antwort vollständig gelesen.</summary>
    public async Task<HttpAntwort> HoleAsync(string pfad)
    {
        using var antwort = await _client.GetAsync(pfad).ConfigureAwait(false);
        return await LiesAsync(antwort).ConfigureAwait(false);
    }

    /// <summary>POST mit JSON-Rumpf (Objekt wird serialisiert).</summary>
    public Task<HttpAntwort> SendeJsonAsync(string pfad, object koerper)
        => SendeAsync(pfad, JsonInhalt(koerper));

    /// <summary>POST ohne Rumpf (Server-Endpunkte, die nur den Pfad auswerten).</summary>
    public Task<HttpAntwort> SendeOhneInhaltAsync(string pfad)
        => SendeAsync(pfad, null);

    /// <summary>
    /// POST mit beliebigem Inhalt (Multipart-Upload, Formular). Der Inhalt wird
    /// nach dem Senden freigegeben - früher blieben mehrere
    /// MultipartFormDataContent-Objekte samt Foto-Puffern liegen.
    /// </summary>
    public async Task<HttpAntwort> SendeAsync(string pfad, HttpContent? inhalt)
    {
        try
        {
            using var antwort = await _client.PostAsync(pfad, inhalt).ConfigureAwait(false);
            return await LiesAsync(antwort).ConfigureAwait(false);
        }
        finally
        {
            inhalt?.Dispose();
        }
    }

    /// <summary>
    /// Bytes einer URL laden (Bilder). Gibt null zurück, wenn der Server mit
    /// Fehlerstatus antwortet; Netz-/Transportfehler werden bewusst
    /// weitergeworfen, damit der Aufrufer auf seinen Offline-Cache umschalten
    /// kann.
    /// </summary>
    public async Task<byte[]?> HoleBytesAsync(string url)
    {
        using var antwort = await _client.GetAsync(url).ConfigureAwait(false);
        if (!antwort.IsSuccessStatusCode)
        {
            System.Diagnostics.Debug.WriteLine($"[API] Download fehlgeschlagen: {antwort.StatusCode} ({url})");
            return null;
        }
        return await antwort.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Standardablauf einer API-Methode: Anfrage ausführen, Antwort in das
    /// Zielmodell wandeln, sonst ein Ersatzobjekt bilden.
    /// </summary>
    /// <param name="anfrage">Die auszuführende Anfrage.</param>
    /// <param name="standard">Ersatz, wenn der Server "null" liefert (Argument: HTTP-Erfolg).</param>
    /// <param name="fehler">Ersatz bei Ausnahme/ungültigem JSON (Argument: Meldung).</param>
    /// <param name="merker">Name fürs Debug-Protokoll.</param>
    public async Task<T> FrageAsync<T>(
        Func<Task<HttpAntwort>> anfrage,
        Func<bool, T> standard,
        Func<string, T> fehler,
        string? merker = null)
    {
        try
        {
            var antwort = await anfrage().ConfigureAwait(false);
            if (merker != null)
                System.Diagnostics.Debug.WriteLine($"[API] {merker}: {antwort.StatusCode} - {antwort.Auszug()}");
            return antwort.Deserialisiere<T>() ?? standard(antwort.Erfolgreich);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] {merker ?? "Anfrage"} Fehler: {ex.Message}");
            return fehler(ex.Message);
        }
    }

    /// <summary>JSON-Rumpf aus einem beliebigen Objekt bauen.</summary>
    private static StringContent JsonInhalt(object koerper)
        => new(JsonSerializer.Serialize(koerper), Encoding.UTF8, "application/json");

    /// <summary>Statuszeile + Rumpf einer Antwort einlesen.</summary>
    private static async Task<HttpAntwort> LiesAsync(HttpResponseMessage antwort)
    {
        var text = await antwort.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new HttpAntwort((int)antwort.StatusCode, antwort.IsSuccessStatusCode, text);
    }
}
