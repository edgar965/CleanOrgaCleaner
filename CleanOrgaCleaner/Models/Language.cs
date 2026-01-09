namespace CleanOrgaCleaner.Models;

/// <summary>
/// Supported languages for the app
/// </summary>
public static class Language
{
    /// <summary>
    /// All supported languages with their display names
    /// </summary>
    public static readonly Dictionary<string, string> Supported = new()
    {
        { "de", "Deutsch" },
        { "en", "English" },
        { "es", "Espanol" },
        { "ro", "Romana" },
        { "pl", "Polski" },
        { "ru", "Russkij" },
        { "uk", "Ukrainska" },
        { "vi", "Tieng Viet" }
    };

    /// <summary>
    /// Country flags/codes for each language
    /// </summary>
    public static readonly Dictionary<string, string> Flags = new()
    {
        { "de", "DE" },
        { "en", "GB" },
        { "es", "ES" },
        { "ro", "RO" },
        { "pl", "PL" },
        { "ru", "RU" },
        { "uk", "UA" },
        { "vi", "VN" }
    };

    /// <summary>
    /// Get display name for a language code
    /// </summary>
    public static string GetDisplayName(string code)
    {
        return Supported.GetValueOrDefault(code, code);
    }

    /// <summary>
    /// Get flag code for a language
    /// </summary>
    public static string GetFlag(string code)
    {
        return Flags.GetValueOrDefault(code, code.ToUpper());
    }

    /// <summary>
    /// Get picker display text (e.g., "DE Deutsch")
    /// </summary>
    public static string GetPickerText(string code)
    {
        return $"{GetFlag(code)} {GetDisplayName(code)}";
    }

    /// <summary>
    /// Default language
    /// </summary>
    public const string Default = "de";
}
