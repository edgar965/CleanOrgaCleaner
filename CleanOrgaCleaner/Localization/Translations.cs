namespace CleanOrgaCleaner.Localization;

/// <summary>
/// Zugriff auf die Oberflächentexte. Die Texte selbst liegen je Sprache in einer
/// eigenen Klasse unter <c>Localization/Sprachen</c> und werden über den
/// <see cref="TranslationCatalog"/> gefunden.
///
/// Fallback-Kette bei fehlendem Schlüssel: aktuelle Sprache -> Englisch -> Deutsch
/// -> Schlüssel selbst. Das aktive Sprachpaket wird zwischengespeichert, damit
/// <see cref="Get"/> ohne Sprach-Lookup auskommt (wird pro Bildschirmaufbau
/// dutzendfach aufgerufen).
/// </summary>
public static class Translations
{
    private static readonly LanguagePack? _fallback = TranslationCatalog.Finden(TranslationCatalog.FallbackCode);
    private static readonly LanguagePack? _zweitFallback = TranslationCatalog.Finden(TranslationCatalog.ZweitFallbackCode);

    private static string _currentLanguage = TranslationCatalog.FallbackCode;
    private static LanguagePack? _aktiv = _fallback;

    /// <summary>Schlüssel, unter dem die Sprache in den Preferences liegt.</summary>
    private const string PreferenceKey = "language";

    /// <summary>
    /// Aktueller Sprachcode (Standard: en). Beim Setzen wird das passende
    /// Sprachpaket einmalig aufgelöst; kennt der Katalog den Code nicht, greift in
    /// <see cref="Get"/> weiterhin die Fallback-Kette (wie bisher).
    /// </summary>
    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            var paket = TranslationCatalog.Finden(value);
            _currentLanguage = paket?.Code ?? value;
            _aktiv = paket;
        }
    }

    /// <summary>Übersetzten Text zu einem Schlüssel liefern.</summary>
    public static string Get(string key)
    {
        if (_aktiv != null && _aktiv.TryGetText(key, out var text))
            return text;

        if (_fallback != null && _fallback.TryGetText(key, out var englisch))
            return englisch;

        if (_zweitFallback != null && _zweitFallback.TryGetText(key, out var deutsch))
            return deutsch;

        return key;
    }

    /// <summary>
    /// Sprache aus den Preferences laden; sonst die Gerätesprache erkennen.
    /// </summary>
    public static void LoadFromPreferences()
    {
        var gespeichert = Preferences.Get(PreferenceKey, "");
        if (!string.IsNullOrEmpty(gespeichert) && IsSupported(gespeichert))
        {
            CurrentLanguage = gespeichert;
            return;
        }

        var geraetesprache = GeraeteSpracheErmitteln();
        CurrentLanguage = IsSupported(geraetesprache) ? geraetesprache : TranslationCatalog.FallbackCode;
        System.Diagnostics.Debug.WriteLine(
            $"[Translations] Gerätesprache erkannt: {geraetesprache}, aktiv: {CurrentLanguage}");
    }

    /// <summary>Aktuelle Sprache in den Preferences sichern.</summary>
    public static void SaveToPreferences()
    {
        Preferences.Set(PreferenceKey, CurrentLanguage);
    }

    /// <summary>True, wenn die Sprache unterstützt wird.</summary>
    public static bool IsSupported(string langCode) => TranslationCatalog.Unterstuetzt(langCode);

    /// <summary>Alle unterstützten Sprachcodes.</summary>
    public static IEnumerable<string> SupportedLanguages => TranslationCatalog.Codes;

    /// <summary>
    /// Gerätesprache bestimmen: erst die UI-Kultur (maßgeblich für Oberflächentexte),
    /// sonst die Formatkultur.
    /// </summary>
    private static string GeraeteSpracheErmitteln()
    {
        try
        {
            var uiSprache = System.Globalization.CultureInfo.CurrentUICulture
                .TwoLetterISOLanguageName.ToLowerInvariant();
            if (IsSupported(uiSprache)) return uiSprache;

            return System.Globalization.CultureInfo.CurrentCulture
                .TwoLetterISOLanguageName.ToLowerInvariant();
        }
        catch
        {
            return TranslationCatalog.FallbackCode;
        }
    }
}
