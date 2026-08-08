using System.Globalization;
using System.Text.Json.Serialization;
using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Schlanke Chat-Nachricht - wird als Liste pro Konversation gespeichert.
/// Der Kontext (wer mit wem) ergibt sich aus der Speicherstruktur:
/// chats[myId][otherCleanerId].
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

    [JsonPropertyName("link_photo_video")]
    public string? LinkPhotoVideo { get; set; }

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

    /// <summary>
    /// Vom Server NUR in WebSocket-Pushes gesetzt: true = die Verwaltung hat
    /// gesendet. Eindeutigstes Signal zur Thread-Zuordnung (Verwaltungs- vs.
    /// Kollegen-Chat).
    /// </summary>
    [JsonPropertyName("from_admin")]
    public bool FromAdmin { get; set; }

    #region Anzeige-Eigenschaften (XAML-Bindings)

    /// <summary>Alias für IsMine - für XAML-Binding-Kompatibilität.</summary>
    [JsonIgnore]
    public bool FromCurrentUser
    {
        get => IsMine;
        set => IsMine = value;
    }

    /// <summary>Invers zu IsMine - linke Seite der Liste = empfangene Nachrichten.</summary>
    [JsonIgnore]
    public bool IsFromOther => !IsMine;

    /// <summary>
    /// True, wenn es eine Übersetzung gibt.
    /// - Eigene Nachricht: TextTranslated existiert und weicht von Text ab
    /// - Empfangene Nachricht: TextOriginal existiert und weicht von Text ab
    /// </summary>
    [JsonIgnore]
    public bool HasTranslation => IsMine
        ? !string.IsNullOrEmpty(TextTranslated) && TextTranslated != Text
        : !string.IsNullOrEmpty(TextOriginal) && TextOriginal != Text;

    [JsonIgnore]
    public bool HasPhotoVideo => !string.IsNullOrEmpty(LinkPhotoVideo);

    [JsonIgnore]
    public bool HasText => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Vollständige URL zu Foto/Video. Relative Serverpfade werden mit der
    /// aktuellen Basis-URL ergänzt (die Basis kann zur Laufzeit wechseln,
    /// deshalb kein zwischengespeicherter Wert).
    /// </summary>
    [JsonIgnore]
    public string? LinkPhotoVideoUrl
    {
        get
        {
            if (string.IsNullOrEmpty(LinkPhotoVideo)) return null;
            if (LinkPhotoVideo.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return LinkPhotoVideo;
            return ApiService.BaseUrl.TrimEnd('/') + LinkPhotoVideo;
        }
    }

    /// <summary>
    /// Text für die Hauptanzeige - die API liefert bereits den passenden Text
    /// im Feld 'text'.
    /// </summary>
    [JsonIgnore]
    public string DisplayText => Text;

    /// <summary>
    /// Zweitzeile mit Globus-Symbol:
    /// - Eigene Nachricht: die Übersetzung, die die Gegenseite sieht
    /// - Empfangene Nachricht: das Original der Absenderin/des Absenders
    /// </summary>
    [JsonIgnore]
    public string? SecondaryText => HasTranslation
        ? (IsMine ? TextTranslated : TextOriginal)
        : null;

    /// <summary>Zeitstempel für die Chat-Anzeige (unabhängig von der Geräte-Kultur).</summary>
    [JsonIgnore]
    public string DatumZeit => Timestamp.ToString("dd.MM. HH:mm", CultureInfo.InvariantCulture);

    #endregion
}
