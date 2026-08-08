using System.Text;
using System.Text.Json;
using CleanOrgaCleaner.Models.Responses;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// An- und Abmeldung am Server.
///
/// Die Antwort wird bewusst mit JsonDocument von Hand gelesen (nicht per
/// Deserialisierung): auf iOS hing der reflektierende Serialisierer im
/// Login-Pfad. Beide Login-Wege (asynchron über HttpClient, synchron über
/// HttpWebRequest) nutzen jetzt DIESELBE Auswertung - vorher war sie zweimal
/// getippt und drohte auseinanderzulaufen.
/// </summary>
public sealed class AuthApi
{
    private readonly ApiHttpKern _http;
    private readonly Sitzung _sitzung;
    private readonly WsCookieSpeicher _cookies;
    private readonly HeartbeatDienst _heartbeat;

    public AuthApi(ApiHttpKern http, Sitzung sitzung, WsCookieSpeicher cookies, HeartbeatDienst heartbeat)
    {
        _http = http;
        _sitzung = sitzung;
        _cookies = cookies;
        _heartbeat = heartbeat;
    }

    /// <summary>
    /// Reiner Login: HTTP-POST + JSON auswerten, ohne Nebenwirkungen
    /// (kein Heartbeat, kein WebSocket). Läuft über Task.Run, damit der
    /// Netzaufruf nicht auf dem UI-Thread liegt (iOS-Anforderung).
    /// </summary>
    public Task<LoginResult> AnmeldenAsync(int propertyId, string username, string password)
    {
        return Task.Run(async () =>
        {
            try
            {
                LoginProtokoll.SchreibeApi($"ENTER prop={propertyId} user={username}");
                var json = JsonSerializer.Serialize(new { property_id = propertyId, username, password });
                LoginProtokoll.SchreibeApi($"done: {json.Length} chars");

                LoginProtokoll.SchreibeApi("PostAsync START -> /mobile/api/login/");
                var antwort = await _http.SendeAsync("/mobile/api/login/",
                    new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                LoginProtokoll.SchreibeApi($"PostAsync DONE -> {antwort.StatusCode}");
                LoginProtokoll.SchreibeApi($"JSON: {antwort.Auszug()}");

                return Auswerten(antwort.Text);
            }
            catch (Exception ex)
            {
                LoginProtokoll.SchreibeApi($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                return new LoginResult { Success = false, ErrorMessage = ex.Message };
            }
        });
    }

    /// <summary>
    /// Komplett synchroner Login über HttpWebRequest - ohne async/await und
    /// ohne SynchronizationContext. Muss aus Task.Run() aufgerufen werden.
    /// Nur dieser Weg kommt an die Set-Cookie-Kopfzeilen der Login-Antwort und
    /// legt die WebSocket-Cookies ab (siehe WsCookieSpeicher).
    /// </summary>
    public LoginResult AnmeldenSync(int propertyId, string username, string password)
    {
        try
        {
            LoginProtokoll.SchreibeApi($"ENTER prop={propertyId} user={username}");
            var json = JsonSerializer.Serialize(new
            {
                property_id = propertyId,
                username = username,
                password = password
            });
            var rumpf = Encoding.UTF8.GetBytes(json);
            LoginProtokoll.SchreibeApi($"HttpWebRequest START -> {ApiHttpKern.BasisUrl}/mobile/api/login/");

#pragma warning disable SYSLIB0014 // HttpWebRequest ist veraltet - Absicht: HttpClient.Send() wirft auf iOS PlatformNotSupportedException
            var anfrage = System.Net.HttpWebRequest.CreateHttp($"{ApiHttpKern.BasisUrl}/mobile/api/login/");
#pragma warning restore SYSLIB0014
            anfrage.Method = "POST";
            anfrage.ContentType = "application/json; charset=utf-8";
            anfrage.Accept = "application/json";
            anfrage.CookieContainer = _http.CookieContainer;
            anfrage.Timeout = 30000;
            anfrage.ContentLength = rumpf.Length;

            using (var strom = anfrage.GetRequestStream())
            {
                strom.Write(rumpf, 0, rumpf.Length);
            }

            var antwortJson = LiesAntwort(anfrage);
            LoginProtokoll.SchreibeApi($"Response -> {antwortJson.Length} chars");

            var ergebnis = Auswerten(antwortJson);
            if (ergebnis.Success)
                _heartbeat.Starte();
            return ergebnis;
        }
        catch (Exception ex)
        {
            LoginProtokoll.SchreibeApi($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return new LoginResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>Abmelden ohne Server-Aufruf (lokaler Zustand).</summary>
    public void Abmelden()
    {
        _heartbeat.Stoppe();
        _sitzung.Leere();
        _cookies.Leere();
    }

    /// <summary>
    /// Abmelden mit Server-Aufruf. WICHTIG: Heartbeat ZUERST stoppen - sonst
    /// feuert der Timer während des Logouts und überschreibt den Offline-Status.
    /// </summary>
    public async Task AbmeldenAsync()
    {
        _heartbeat.Stoppe();

        try
        {
            await _http.SendeOhneInhaltAsync("/mobile/api/logout/").ConfigureAwait(false);
        }
        catch
        {
            // Server-Logout ist optional - lokal wird auf jeden Fall aufgeräumt
        }

        _sitzung.Leere();
        _cookies.Leere();
    }

    /// <summary>Antwort des synchronen Logins lesen (auch im Fehlerfall).</summary>
    private string LiesAntwort(System.Net.HttpWebRequest anfrage)
    {
        try
        {
            using var antwort = (System.Net.HttpWebResponse)anfrage.GetResponse();
            LoginProtokoll.SchreibeApi($"GetResponse DONE -> {antwort.StatusCode}");
            _cookies.UebernehmeAus(antwort);
            using var strom = antwort.GetResponseStream();
            using var leser = new StreamReader(strom);
            return leser.ReadToEnd();
        }
        catch (System.Net.WebException wex) when (wex.Response is System.Net.HttpWebResponse fehlerAntwort)
        {
            LoginProtokoll.SchreibeApi($"HTTP Error -> {fehlerAntwort.StatusCode}");
            using var strom = fehlerAntwort.GetResponseStream();
            using var leser = new StreamReader(strom);
            return leser.ReadToEnd();
        }
    }

    /// <summary>
    /// JSON auswerten, Sitzung/Preferences setzen und das Ergebnis bauen.
    /// Einzige Auswertungsstelle für beide Login-Wege.
    /// </summary>
    private LoginResult Auswerten(string antwortJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(antwortJson);
            var wurzel = doc.RootElement;

            var erfolg = wurzel.TryGetProperty("success", out var erfolgFeld) && erfolgFeld.GetBoolean();
            if (!erfolg)
            {
                var fehler = wurzel.TryGetProperty("error", out var fehlerFeld) ? fehlerFeld.GetString() : null;
                LoginProtokoll.SchreibeApi($"returning FAILED: {fehler}");
                return new LoginResult { Success = false, ErrorMessage = fehler ?? "Login fehlgeschlagen" };
            }

            string? name = null;
            string? sprache = null;
            int? id = null;

            if (wurzel.TryGetProperty("cleaner", out var kraft) && kraft.ValueKind == JsonValueKind.Object)
            {
                name = kraft.TryGetProperty("name", out var nameFeld) ? nameFeld.GetString() : null;
                id = kraft.TryGetProperty("id", out var idFeld) ? idFeld.GetInt32() : null;

                // Avatar immer übernehmen (leer = Standard-Logo)
                var avatar = kraft.TryGetProperty("avatar", out var avatarFeld) ? avatarFeld.GetString() : "";
                Preferences.Set("avatar", avatar ?? "");

                sprache = kraft.TryGetProperty("language", out var sprachFeld) ? sprachFeld.GetString() : null;
                if (!string.IsNullOrEmpty(sprache))
                {
                    Preferences.Set("language", sprache);
                    Localization.Translations.CurrentLanguage = sprache;
                }
            }

            _sitzung.Uebernehme(name, sprache, id);
            LoginProtokoll.SchreibeApi($"Cleaner: {name}, id={id}, lang={sprache}");

            return new LoginResult
            {
                Success = true,
                CleanerName = name,
                CleanerLanguage = sprache,
                CleanerId = id
            };
        }
        catch (Exception ex)
        {
            LoginProtokoll.SchreibeApi($"Auswerten EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            return new LoginResult { Success = false, ErrorMessage = $"JSON Parse Error: {ex.Message}" };
        }
    }
}
