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

    // Generation-Zähler gegen das Login/Logout-Race: Stop() erhöht die
    // Generation. Ein StartAsync, dessen SignIn erst NACH einem Stop() fertig
    // wird, erkennt das (Generation passt nicht mehr), meldet sich wieder ab
    // und hängt KEINEN Listener an - sonst würde das Gerät nach dem Logout
    // weiter Chats des alten Kontos empfangen.
    private int _generation;

    /// <summary>Nach dem Login aufrufen: Firebase-Anmeldung + Listener starten.</summary>
    public async Task StartAsync(int cleanerId, int propertyId, string customToken)
    {
        // Ohne initialisiertes Firebase crasht der bloße Zugriff auf
        // CrossFirebaseAuth.Current auf iOS nativ (SIGTRAP) - hart abbrechen.
        if (!FirebaseStatus.Ready)
        {
            Debug.WriteLine("[FS] Firebase nicht initialisiert - Start übersprungen");
            return;
        }

        var meineGeneration = _generation;
        try
        {
            await CrossFirebaseAuth.Current.SignInWithCustomTokenAsync(customToken).ConfigureAwait(false);

            if (meineGeneration != _generation)
            {
                // Während des SignIn kam ein Stop() (Logout) - nicht starten,
                // sondern die gerade erzeugte Anmeldung gleich wieder beenden.
                Debug.WriteLine("[FS] Stop während SignIn - Listener wird nicht gestartet");
                _ = Task.Run(async () =>
                {
                    try { await CrossFirebaseAuth.Current.SignOutAsync().ConfigureAwait(false); }
                    catch (Exception ex) { Debug.WriteLine($"[FS] Nach-Race-SignOut-Fehler: {ex.Message}"); }
                });
                return;
            }

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

    /// <summary>
    /// Listener beenden. Bei Logout mit abmelden=true aufrufen, damit auch die
    /// (vom SDK über Neustarts persistierte) Firebase-Anmeldung entfernt wird.
    /// </summary>
    public void Stop(bool abmelden = true)
    {
        try
        {
            _generation++; // laufende StartAsync-SignIns ungültig machen (Race-Schutz)
            _listener?.Dispose();
            _listener = null;

            // SignOut nur, wenn Firebase initialisiert ist - sonst crasht der
            // Zugriff auf CrossFirebaseAuth.Current auf iOS nativ (SIGTRAP,
            // Login-Crash 1.74). Wenn Ready, IMMER abmelden: Firebase Auth
            // persistiert die Anmeldung über App-Neustarts, ein In-Memory-Flag
            // würde die Abmeldung nach Neustart fälschlich überspringen.
            if (abmelden && FirebaseStatus.Ready)
            {
                _ = Task.Run(async () =>
                {
                    try { await CrossFirebaseAuth.Current.SignOutAsync().ConfigureAwait(false); }
                    catch (Exception ex) { Debug.WriteLine($"[FS] SignOut-Fehler: {ex.Message}"); }
                });
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
