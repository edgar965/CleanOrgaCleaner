using System.Text.Json.Serialization;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Schlanke Chat-Nachricht - wird als Liste pro Konversation gespeichert
/// Kontext (wer mit wem) ergibt sich aus der Speicherstruktur: chats[myId][otherCleanerId]
/// </summary>
public class ChatMessage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("text_translated")]
    public string? TextTranslated { get; set; }

    [JsonPropertyName("text_original")]
    public string? TextOriginal { get; set; }

    [JsonPropertyName("photos")]
    public List<string>? Photos { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("is_mine")]
    public bool IsMine { get; set; }

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("sender_name")]
    public string? SenderName { get; set; }

    [JsonPropertyName("cleaner_id")]
    public int? CleanerId { get; set; }


    #region UI Properties

    /// <summary>
    /// Alias für IsMine - für XAML-Binding Kompatibilität
    /// </summary>
    [JsonIgnore]
    public bool FromCurrentUser
    {
        get => IsMine;
        set => IsMine = value;
    }

    /// <summary>
    /// Invertiert von IsMine - für XAML-Binding (linke Seite = empfangene Nachrichten)
    /// </summary>
    [JsonIgnore]
    public bool IsFromOther => !IsMine;

    /// <summary>
    /// True wenn es eine Übersetzung gibt
    /// - Eigene Nachricht: TextTranslated existiert und unterscheidet sich von Text
    /// - Empfangene Nachricht: TextOriginal existiert und unterscheidet sich von Text
    /// </summary>
    [JsonIgnore]
    public bool HasTranslation => IsMine
        ? !string.IsNullOrEmpty(TextTranslated) && TextTranslated != Text
        : !string.IsNullOrEmpty(TextOriginal) && TextOriginal != Text;

    [JsonIgnore]
    public bool HasPhotos => Photos?.Count > 0;

    /// <summary>
    /// Text für Hauptanzeige - API sendet bereits den richtigen Text im 'text' Feld
    /// </summary>
    [JsonIgnore]
    public string DisplayText => Text;

    /// <summary>
    /// Sekundärer Text mit Globe-Icon:
    /// - Eigene Nachricht: Übersetzung die Empfänger sieht (TextTranslated)
    /// - Empfangene Nachricht: Original des Senders (TextOriginal)
    /// </summary>
    [JsonIgnore]
    public string? SecondaryText => HasTranslation
        ? (IsMine ? TextTranslated : TextOriginal)
        : null;

    /// <summary>
    /// Alias für Kompatibilität - zeigt Original bei empfangenen Nachrichten
    /// </summary>
    [JsonIgnore]
    public string? OriginalText => !IsMine && HasTranslation ? TextOriginal : null;

    [JsonIgnore]
    public string DisplayTime => Timestamp.ToString("HH:mm");

    [JsonIgnore]
    public string DisplayDate => Timestamp.ToString("dd.MM.yyyy");

    /// <summary>
    /// Formatierter Timestamp für Chat-Anzeige
    /// </summary>
    [JsonIgnore]
    public string DatumZeit => Timestamp.ToString("dd.MM. HH:mm");

    [JsonIgnore]
    public Color BackgroundColor => IsMine
        ? Color.FromArgb("#dcf8c6")   // Grün für eigene
        : Color.FromArgb("#ffffff");  // Weiß für empfangene

    #endregion
}
