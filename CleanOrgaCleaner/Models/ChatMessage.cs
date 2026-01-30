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

    [JsonPropertyName("from_current_user")]
    public bool FromCurrentUser { get; set; }

    [JsonPropertyName("is_from_admin")]
    public bool IsFromAdmin { get; set; }

    #region UI Properties

    /// <summary>
    /// Is this message from someone else (not the current user)?
    /// </summary>
    public bool IsFromOther => !FromCurrentUser;

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
    public Color BackgroundColor => FromCurrentUser
        ? Color.FromArgb("#dcf8c6")  // Green for own messages
        : Color.FromArgb("#ffffff"); // White for others

    /// <summary>
    /// Border color based on sender
    /// </summary>
    public Color BorderColor => FromCurrentUser
        ? Color.FromArgb("#25D366")  // Green for own messages
        : Color.FromArgb("#cccccc"); // Gray for others

    /// <summary>
    /// Formatted date/time for display
    /// </summary>
    public string DisplayDateTime => DatumZeit ?? Timestamp ?? "";

    #endregion
}
