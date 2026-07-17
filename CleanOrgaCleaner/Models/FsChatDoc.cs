using Plugin.Firebase.Firestore;

namespace CleanOrgaCleaner.Models;

/// <summary>
/// Firestore-Abbild einer Chat-Nachricht im Posteingang
/// (properties/{pid}/inbox/{cleanerId}/messages/{id}). Die Feldnamen (snake_case)
/// entsprechen exakt dem, was Django via to_dict() schreibt - identisch zum
/// bisherigen WebSocket-Broadcast. Wird per Snapshot-Listener empfangen und in
/// das App-Modell ChatMessage umgewandelt.
///
/// [Preserve(AllMembers)] schuetzt Ctor + Properties vor dem Linker/Trimming,
/// damit die reflexionsbasierte Firestore-Deserialisierung im Release/AOT-Build
/// funktioniert.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public sealed class FsChatDoc : IFirestoreObject
{
    public FsChatDoc()
    {
        // parameterloser Ctor fuer Firestore-Deserialisierung
    }

    [FirestoreProperty("id")]
    public long Id { get; set; }

    [FirestoreProperty("text")]
    public string Text { get; set; } = "";

    [FirestoreProperty("text_translated")]
    public string TextTranslated { get; set; }

    [FirestoreProperty("text_original")]
    public string TextOriginal { get; set; }

    [FirestoreProperty("link_photo_video")]
    public string LinkPhotoVideo { get; set; }

    [FirestoreProperty("timestamp")]
    public string Timestamp { get; set; }

    [FirestoreProperty("is_mine")]
    public bool IsMine { get; set; }

    [FirestoreProperty("is_read")]
    public bool IsRead { get; set; }

    [FirestoreProperty("sender_name")]
    public string SenderName { get; set; }

    [FirestoreProperty("cleaner_id")]
    public long CleanerId { get; set; }

    [FirestoreProperty("from_admin")]
    public bool FromAdmin { get; set; }
}
