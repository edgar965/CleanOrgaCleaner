using System.Net;
using System.Text.RegularExpressions;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Verwahrt die auth-relevanten Cookies für den WebSocket-Handshake.
///
/// Hintergrund (iOS): der native NSUrlSessionHandler befüllt den managed
/// CookieContainer nicht zuverlässig - der WS-Handshake ging deshalb ohne
/// sessionid raus (403). Darum werden sessionid/ws_auth/csrftoken/property_id
/// direkt aus den Set-Cookie-Kopfzeilen der Login-Antwort gegriffen und in den
/// Preferences abgelegt; der Container ist nur noch Rückfallebene.
/// </summary>
public sealed class WsCookieSpeicher
{
    /// <summary>Cookie-Name -> Preferences-Schlüssel.</summary>
    private static readonly (string Name, string Pref)[] _cookies =
    {
        ("sessionid", "ws_sessionid"),
        ("ws_auth", "ws_auth"),
        ("csrftoken", "ws_csrftoken"),
        ("property_id", "ws_property_id"),
    };

    /// <summary>
    /// Je Cookie-Name ein vorbereiteter Ausdruck - früher wurde das Muster bei
    /// jedem Login neu übersetzt.
    /// </summary>
    private static readonly Regex[] _muster = _cookies
        .Select(c => new Regex(c.Name + @"=([^;,\s]+)", RegexOptions.CultureInvariant))
        .ToArray();

    private readonly ApiHttpKern _http;

    public WsCookieSpeicher(ApiHttpKern http) => _http = http;

    /// <summary>
    /// Liest die auth-relevanten Cookies aus einer Antwort und legt sie ab.
    /// </summary>
    public void UebernehmeAus(HttpWebResponse antwort)
    {
        try
        {
            // 1) Bevorzugt die geparste Cookie-Collection der Antwort
            foreach (Cookie c in antwort.Cookies)
                Speichere(c.Name, c.Value);

            // 2) Zusätzlich roh aus dem Set-Cookie-Header (iOS: Cookies-Collection
            //    kann leer sein, der Header ist aber vorhanden) - robust gegen
            //    zusammengefaltete Kopfzeilen.
            var roh = antwort.Headers["Set-Cookie"];
            if (string.IsNullOrEmpty(roh))
                return;

            for (var i = 0; i < _cookies.Length; i++)
            {
                var treffer = _muster[i].Match(roh);
                if (treffer.Success)
                    Speichere(_cookies[i].Name, treffer.Groups[1].Value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Cookie] Übernahme fehlgeschlagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Cookie-Kopfzeile für WebSocket-Verbindungen. Bevorzugt die beim Login
    /// abgelegten Werte, sonst der managed Container (Android/Desktop).
    /// </summary>
    public string KopfZeile()
    {
        var teile = new List<string>(_cookies.Length);
        foreach (var (name, pref) in _cookies)
        {
            var wert = Preferences.Get(pref, "");
            if (!string.IsNullOrEmpty(wert))
                teile.Add($"{name}={wert}");
        }
        if (teile.Count > 0)
            return string.Join("; ", teile);

        var cookies = _http.LiesCookies();
        if (cookies.Count == 0)
            return "";

        var ausContainer = new List<string>(cookies.Count);
        foreach (Cookie cookie in cookies)
            ausContainer.Add($"{cookie.Name}={cookie.Value}");
        return string.Join("; ", ausContainer);
    }

    /// <summary>
    /// Alles verwerfen: neuer Container, abgelegte WS-Cookies und das
    /// Push-Registriert-Flag entfernen, auf iOS zusätzlich den nativen
    /// Cookie-Speicher leeren (sonst überlebt z.B. eine veraltete property_id
    /// den Benutzerwechsel und landet im WS-Handshake).
    /// </summary>
    public void Leere()
    {
        _http.CookieContainer = new CookieContainer();

        foreach (var (_, pref) in _cookies)
            Preferences.Remove(pref);
        Preferences.Remove("push_registered");

#if IOS
        try
        {
            var speicher = Foundation.NSHttpCookieStorage.SharedStorage;
            var cookies = speicher.Cookies;
            if (cookies != null)
            {
                foreach (var c in cookies)
                    speicher.DeleteCookie(c);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Cookie] Nativer Cookie-Reset fehlgeschlagen: {ex.Message}");
        }
#endif
    }

    private static void Speichere(string name, string wert)
    {
        if (string.IsNullOrEmpty(wert))
            return;
        foreach (var (n, pref) in _cookies)
        {
            if (n == name)
            {
                Preferences.Set(pref, wert);
                return;
            }
        }
    }
}
