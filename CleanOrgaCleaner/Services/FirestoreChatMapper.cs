using System.Globalization;
using CleanOrgaCleaner.Models;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Wandelt ein Firestore-Dokument in das App-Modell ChatMessage.
/// Eigene Klasse, damit der Empfangsdienst nur noch Anmeldung und Zuhörer
/// verwaltet.
/// </summary>
public static class FirestoreChatMapper
{
    /// <summary>Dokument in eine Chat-Nachricht übersetzen.</summary>
    public static ChatMessage ZuChatMessage(FsChatDoc doc)
    {
        if (!DateTime.TryParse(doc.Timestamp, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var zeitpunkt))
        {
            zeitpunkt = DateTime.Now;
        }

        return new ChatMessage
        {
            Id = (int)doc.Id,
            Text = doc.Text ?? "",
            TextTranslated = doc.TextTranslated,
            TextOriginal = doc.TextOriginal,
            LinkPhotoVideo = doc.LinkPhotoVideo,
            Timestamp = zeitpunkt,
            IsMine = doc.IsMine,
            IsRead = doc.IsRead,
            SenderName = doc.SenderName,
            CleanerId = doc.CleanerId == 0 ? null : (int?)doc.CleanerId,
            FromAdmin = doc.FromAdmin,
        };
    }
}
