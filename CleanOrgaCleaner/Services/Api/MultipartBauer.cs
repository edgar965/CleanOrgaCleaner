using System.Net.Http.Headers;

namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Baut die Multipart-Formulare für Foto-Uploads.
///
/// Vorher stand in fünf Methoden derselbe Block aus ByteArrayContent,
/// MediaTypeHeaderValue("image/jpeg") und Add(...) - inklusive der Gefahr,
/// den Inhaltstyp an einer Stelle zu vergessen.
/// </summary>
public static class MultipartBauer
{
    /// <summary>Formular mit genau einem Foto.</summary>
    public static MultipartFormDataContent MitFoto(string feldname, string dateiname, byte[] bytes)
    {
        var formular = new MultipartFormDataContent();
        FuegeFotoHinzu(formular, feldname, dateiname, bytes);
        return formular;
    }

    /// <summary>
    /// Formular mit Name/Beschreibung und beliebig vielen Fotos.
    /// </summary>
    public static MultipartFormDataContent MitTextUndFotos(
        string name,
        string? beschreibung,
        bool beschreibungImmer,
        string fotoFeldname,
        IReadOnlyList<(string Dateiname, byte[] Bytes)>? fotos)
    {
        var formular = new MultipartFormDataContent
        {
            { new StringContent(name), "name" }
        };

        if (beschreibungImmer || !string.IsNullOrEmpty(beschreibung))
            formular.Add(new StringContent(beschreibung ?? ""), "description");

        if (fotos != null)
        {
            foreach (var foto in fotos)
                FuegeFotoHinzu(formular, fotoFeldname, foto.Dateiname, foto.Bytes);
        }
        return formular;
    }

    private static void FuegeFotoHinzu(MultipartFormDataContent formular, string feldname, string dateiname, byte[] bytes)
    {
        var inhalt = new ByteArrayContent(bytes);
        inhalt.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        formular.Add(inhalt, feldname, dateiname);
    }
}
