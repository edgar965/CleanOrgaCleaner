using System.Text.Json;
using CleanOrgaCleaner.Json;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Eine bereits vollständig gelesene HTTP-Antwort (Status + Rumpf als Text).
///
/// Früher stand in jeder API-Methode dieselbe Abfolge aus
/// <c>response.StatusCode</c>, <c>ReadAsStringAsync()</c> und
/// <c>JsonSerializer.Deserialize(..., _jsonOptions)</c>. Diese Klasse bündelt
/// das an einer Stelle - inklusive der quelltextgenerierten JSON-Optionen
/// (kein Reflection-Fallback, iOS-AOT-tauglich).
/// </summary>
public sealed class HttpAntwort
{
    /// <summary>Quelltextgenerierte Optionen - genau die von AppJsonContext.</summary>
    private static readonly JsonSerializerOptions _optionen = AppJsonContext.Default.Options;

    /// <summary>HTTP-Statuscode der Antwort (z.B. 200, 403, 500).</summary>
    public int StatusCode { get; }

    /// <summary>True bei 2xx (entspricht HttpResponseMessage.IsSuccessStatusCode).</summary>
    public bool Erfolgreich { get; }

    /// <summary>Vollständiger Antworttext (nie null, notfalls leer).</summary>
    public string Text { get; }

    /// <summary>Statusname wie "Forbidden" - für Meldungen an die Arbeitskraft.</summary>
    public string StatusText => ((System.Net.HttpStatusCode)StatusCode).ToString();

    /// <summary>True bei 403 (abgelaufene Anmeldung).</summary>
    public bool KeineBerechtigung => StatusCode == 403;

    /// <summary>True bei 404.</summary>
    public bool NichtGefunden => StatusCode == 404;

    public HttpAntwort(int statusCode, bool erfolgreich, string? text)
    {
        StatusCode = statusCode;
        Erfolgreich = erfolgreich;
        Text = text ?? "";
    }

    /// <summary>
    /// True, wenn der Rumpf wie ein JSON-Objekt beginnt. Django liefert bei
    /// abgelaufener Session eine HTML-Login-Seite - die darf nicht als
    /// "Erfolg" durchgehen.
    /// </summary>
    public bool IstJsonObjekt => Text.TrimStart().StartsWith("{");

    /// <summary>Gekürzter Text fürs Protokoll (Standard: 200 Zeichen).</summary>
    public string Auszug(int zeichen = 200)
        => Text.Length > zeichen ? Text.Substring(0, zeichen) : Text;

    /// <summary>
    /// Antwort in das Zielmodell wandeln. Wirft (wie bisher) bei ungültigem
    /// JSON eine JsonException - die Aufrufer wandeln sie in ihr jeweiliges
    /// Fehler-Antwortobjekt um.
    /// </summary>
    public T? Deserialisiere<T>() => JsonSerializer.Deserialize<T>(Text, _optionen);
}
