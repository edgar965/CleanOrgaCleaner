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

    // Serialisiert ALLE Auth-/Listener-Operationen (StartAsync-Kern und den
    // asynchronen Teil von Stop). Zusätzlich beansprucht JEDE Operation
    // (Start UND Stop) per Interlocked eine neue Generation. Eine Operation,
    // deren Generation beim Ausführen nicht mehr die aktuelle ist, weiß: nach
    // ihr wurde bereits etwas Neueres angestoßen - sie räumt dann nur sich
    // selbst auf (Start: eigenes SignIn rückgängig) bzw. tut gar nichts
    // (Stop: die neuere Operation ist zuständig). So kann ein verspäteter
    // Stop-Task nie die Session/den Listener eines neueren Logins zerstören.
    private readonly SemaphoreSlim _authSperre = new(1, 1);
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

        var meineGeneration = Interlocked.Increment(ref _generation);
        await _authSperre.WaitAsync().ConfigureAwait(false);
        try
        {
            if (meineGeneration != Volatile.Read(ref _generation))
            {
                // Schon vor dem SignIn kam ein Stop() (Logout) - gar nicht anmelden.
                Debug.WriteLine("[FS] Stop vor SignIn - Start abgebrochen");
                return;
            }

            await CrossFirebaseAuth.Current.SignInWithCustomTokenAsync(customToken).ConfigureAwait(false);

            if (meineGeneration != Volatile.Read(ref _generation))
            {
                // Während des SignIn kam ein Stop() (Logout) - keinen Listener
                // starten und die gerade erzeugte Anmeldung wieder beenden.
                // Wir halten die Sperre: eine neuere Anmeldung kann erst danach
                // beginnen und wird hier also nicht zerstört.
                Debug.WriteLine("[FS] Stop während SignIn - Listener wird nicht gestartet");
                await SignOutSicherAsync().ConfigureAwait(false);
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
        finally
        {
            _authSperre.Release();
        }
    }

    /// <summary>
    /// Bei Logout (oder serverseitig deaktiviertem Firestore) aufrufen:
    /// Listener beenden + Firebase-Anmeldung entfernen. Die Abmeldung läuft
    /// asynchron unter derselben Sperre wie StartAsync und ist generation-
    /// geprüft: Hat inzwischen ein NEUERER Login angemeldet, wird dessen
    /// Session nicht zerstört.
    /// </summary>
    public void Stop()
    {
        var meineGeneration = Interlocked.Increment(ref _generation);

        _ = Task.Run(async () =>
        {
            await _authSperre.WaitAsync().ConfigureAwait(false);
            try
            {
                // Ist inzwischen ein NEUERER Start/Stop gelaufen, ist der
                // zuständig - dieser verspätete Stop darf weder den (neuen)
                // Listener anfassen noch die (neue) Anmeldung zerstören.
                if (meineGeneration != Volatile.Read(ref _generation))
                    return;

                _listener?.Dispose();
                _listener = null;

                // SignOut nur, wenn Firebase initialisiert ist - sonst crasht
                // der Zugriff auf CrossFirebaseAuth.Current auf iOS nativ
                // (SIGTRAP, Login-Crash 1.74). Wenn Ready, IMMER abmelden:
                // Firebase Auth persistiert die Anmeldung über App-Neustarts,
                // ein In-Memory-Flag würde die Abmeldung nach Neustart
                // fälschlich überspringen.
                if (FirebaseStatus.Ready)
                {
                    await SignOutSicherAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FS] Stop-Fehler: {ex.Message}");
            }
            finally
            {
                _authSperre.Release();
            }
        });
    }

    /// <summary>SignOut mit geschlucktem Fehler (gemeinsamer Helper).</summary>
    private static async Task SignOutSicherAsync()
    {
        try { await CrossFirebaseAuth.Current.SignOutAsync().ConfigureAwait(false); }
        catch (Exception ex) { Debug.WriteLine($"[FS] SignOut-Fehler: {ex.Message}"); }
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
