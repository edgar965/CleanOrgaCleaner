using System.Diagnostics;
using System.Globalization;
using CleanOrgaCleaner.Models;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace CleanOrgaCleaner.Services;

/// <summary>
/// Echtzeit-Chat-Empfang über Firestore (ersetzt den fragilen WebSocket-Empfang).
///
/// Nach dem Login: mit Firebase-Custom-Token anmelden und einen Snapshot-Listener
/// auf den eigenen Posteingang legen. Neue Nachrichten werden in das App-Modell
/// ChatMessage umgewandelt und über denselben Event ausgeliefert, den bisher der
/// WebSocket nutzte (WebSocketService.NotifyChatMessage) - die UI bleibt unverändert.
///
/// Läuft parallel zum WebSocket; doppelte Nachrichten fängt die UI per Id-Dedup ab.
/// Reconnect/Offline/Hintergrund managt das Firestore-SDK selbst.
/// </summary>
public class FirestoreChatService
{
    private static FirestoreChatService? _instance;
    public static FirestoreChatService Instance => _instance ??= new FirestoreChatService();

    private IDisposable? _listener;
    private bool _ersterSnapshot = true;

    // Ob wir tatsächlich per Firebase-Auth angemeldet sind. NUR dann darf Stop()
    // SignOutAsync() aufrufen. CrossFirebaseAuth.Current -> Auth.auth() macht auf
    // iOS einen fatalError (SIGTRAP), wenn die Default-FirebaseApp (noch) nicht
    // konfiguriert ist - dieser Zugriff darf also nie "blind" passieren.
    private bool _angemeldet;

    /// <summary>Nach dem Login aufrufen: Firebase-Anmeldung + Listener starten.</summary>
    public async Task StartAsync(int cleanerId, int propertyId, string customToken)
    {
        try
        {
            await CrossFirebaseAuth.Current.SignInWithCustomTokenAsync(customToken).ConfigureAwait(false);
            _angemeldet = true;

            _listener?.Dispose();
            _ersterSnapshot = true;
            var pfad = $"properties/{propertyId}/inbox/{cleanerId}/messages";

            _listener = CrossFirebaseFirestore.Current
                .GetCollection(pfad)
                .AddSnapshotListener<FsChatDoc>(
                    snapshot =>
                    {
                        // Der erste Snapshot liefert den Bestand - den nicht als
                        // "neue" Nachricht behandeln (History lädt die App per HTTP).
                        if (_ersterSnapshot)
                        {
                            _ersterSnapshot = false;
                            return;
                        }
                        foreach (var change in snapshot.DocumentChanges)
                        {
                            if (change.ChangeType != DocumentChangeType.Added)
                                continue;
                            var doc = change.DocumentSnapshot.Data;
                            if (doc == null)
                                continue;
                            WebSocketService.Instance.NotifyChatMessage(ToChatMessage(doc));
                        }
                    },
                    ex => Debug.WriteLine($"[FS] Listener-Fehler: {ex.Message}"));

            Debug.WriteLine($"[FS] Listener aktiv: {pfad}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FS] Start-Fehler: {ex.Message}");
        }
    }

    /// <summary>Bei Logout aufrufen: Listener beenden + Firebase abmelden.</summary>
    public void Stop()
    {
        try
        {
            _listener?.Dispose();
            _listener = null;

            // NUR abmelden, wenn wir vorher wirklich angemeldet waren. Sonst
            // würde der bloße Zugriff auf CrossFirebaseAuth.Current (Auth.auth())
            // die App auf iOS mit fatalError/SIGTRAP killen (Default-FirebaseApp
            // nicht konfiguriert). Genau das war der Login-Crash von 1.74.
            if (_angemeldet)
            {
                _angemeldet = false;
                _ = CrossFirebaseAuth.Current.SignOutAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FS] Stop-Fehler: {ex.Message}");
        }
    }

    private static ChatMessage ToChatMessage(FsChatDoc d)
    {
        DateTime ts;
        if (!DateTime.TryParse(d.Timestamp, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out ts))
        {
            ts = DateTime.Now;
        }
        return new ChatMessage
        {
            Id = (int)d.Id,
            Text = d.Text ?? "",
            TextTranslated = d.TextTranslated,
            TextOriginal = d.TextOriginal,
            LinkPhotoVideo = d.LinkPhotoVideo,
            Timestamp = ts,
            IsMine = d.IsMine,
            IsRead = d.IsRead,
            SenderName = d.SenderName,
            CleanerId = d.CleanerId == 0 ? (int?)null : (int)d.CleanerId,
            FromAdmin = d.FromAdmin,
        };
    }
}
