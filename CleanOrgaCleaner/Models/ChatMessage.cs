using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Chat message between cleaner and admin
/// </summary>
public class ChatMessage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("text_translated")]
    public string? TextTranslated { get; set; }

    [JsonPropertyName("source_lang")]
    public string? SourceLang { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("datum_zeit")]
    public string? DatumZeit { get; set; }

    [JsonPropertyName("from_cleaner")]
    public bool FromCleaner { get; set; }

    #region UI Properties

    /// <summary>
    /// Does this message have a translation?
    /// </summary>
    public bool HasTranslation => !string.IsNullOrEmpty(TextTranslated) && TextTranslated != Text;

    /// <summary>
    /// Text to display (translated if available)
    /// </summary>
    public string DisplayText => HasTranslation ? TextTranslated! : Text;

    /// <summary>
    /// Original text (only if translated)
    /// </summary>
    public string? OriginalText => HasTranslation ? Text : null;

    /// <summary>
    /// Background color based on sender
    /// </summary>
    public Color BackgroundColor => FromCleaner
        ? Color.FromArgb("#e3f2fd")  // Light blue for cleaner
        : Color.FromArgb("#f8f9fa"); // Light gray for admin

    /// <summary>
    /// Border color based on sender
    /// </summary>
    public Color BorderColor => FromCleaner
        ? Color.FromArgb("#9c27b0")  // Purple for cleaner
        : Color.FromArgb("#2196F3"); // Blue for admin

    /// <summary>
    /// Formatted date/time for display
    /// </summary>
    public string DisplayDateTime => DatumZeit ?? Timestamp ?? "";

    #endregion
}
