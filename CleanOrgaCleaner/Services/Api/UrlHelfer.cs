namespace CleanOrgaCleaner.Services.Api;

/// <summary>
/// Wandelt die relativen Medien-Pfade des Servers in vollständige Adressen.
/// Vorher stand dieselbe "beginnt mit http?"-Prüfung an fünf Stellen.
/// </summary>
public static class UrlHelfer
{
    /// <summary>Relativen Pfad zur vollständigen Adresse ergänzen.</summary>
    public static string Absolut(string pfad)
        => string.IsNullOrEmpty(pfad) || pfad.StartsWith("http")
            ? pfad
            : $"{ApiHttpKern.BasisUrl}{pfad}";

    /// <summary>Wie <see cref="Absolut"/>, behält aber null bei.</summary>
    public static string? AbsolutOderNull(string? pfad)
        => string.IsNullOrEmpty(pfad) ? pfad : Absolut(pfad);
}
