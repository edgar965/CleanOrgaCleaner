# Push-Notifications für CleanOrga – Konzept

Stand: 2026-07-14 · App-ID `com.cleanorga.cleaner` · MAUI net10 (iOS 15+, Android 5+) ·
Backend Django + Channels + Redis (cleanorga.com), Multi-Tenant (property_1..N)

## 1. Problem & Ziel

Der Chat (und Aufgaben-Updates) laufen heute ausschließlich über den WebSocket
`wss://cleanorga.com/ws/main/`. Die App **trennt den WebSocket im Hintergrund**
(`App.OnAppStopped → DisconnectAsync`), und es gibt **keine Push-Notifications**.
Folge: Eine Nachricht erreicht eine Reinigungskraft nur in Echtzeit, wenn ihre App
gerade im Vordergrund läuft. Das ist die Ursache des gemeldeten Verhaltens
„Chat funktioniert nur, wenn die Handys nah beieinander sind" (= beide schauen
gleichzeitig aufs Gerät).

**Ziel:** Eine neue Chat-Nachricht (und optional: neue/geänderte Aufgabe) erzeugt eine
Systembenachrichtigung auf dem Zielgerät, **auch wenn die App geschlossen oder im
Hintergrund ist**. Tippen auf die Benachrichtigung öffnet den passenden Chat.

## 2. Architektur-Entscheidung: FCM als einheitlicher Kanal

**Empfehlung: Firebase Cloud Messaging (FCM) für beide Plattformen.**
FCM stellt Android nativ zu und leitet iOS-Nachrichten über Apple APNs weiter. Damit
gibt es **einen** Server-Integrationspunkt und **ein** Token-Format statt getrennter
FCM+APNs-Pfade.

- Kosten: FCM ist kostenlos (kein Volumenlimit relevant). Apple Developer Account ist
  vorhanden (App ist im Store). Firebase-Projekt kostenlos.
- Alternative (verworfen): direkter APNs-Weg für iOS + FCM für Android → doppelter
  Server-Code, zwei Zertifikats-/Key-Verwaltungen, kein Mehrwert hier.

```
Neue Chat-Nachricht (Server)
        │
        ├─ WebSocket group_send   → App im Vordergrund zeigt sofort (wie heute)
        │
        └─ FCM-Versand an Token   → Android nativ │ iOS via APNs
                                          │
                                    Systembenachrichtigung
                                          │
                                    Tap → App öffnet Chat mit Absender
```

## 3. Server-Seite (Django)

### 3.1 Datenmodell (pro Property-DB)

Neues Modell `PushToken` – ein Cleaner kann mehrere Geräte haben:

```python
class PushToken(models.Model):
    objects = PropertyAwareManager()
    cleaner   = models.ForeignKey('Cleaner', on_delete=models.CASCADE, related_name='push_tokens')
    token     = models.CharField(max_length=255, unique=True)   # FCM-Registration-Token
    platform  = models.CharField(max_length=10, choices=[('android','Android'),('ios','iOS')])
    aktiv     = models.BooleanField(default=True)
    erstellt  = models.DateTimeField(auto_now_add=True)
    gesehen   = models.DateTimeField(auto_now=True)             # letzte Registrierung/Nutzung
```
Migration isoliert halten (siehe [[project_pending_migrations_gotcha]]).

### 3.2 Token-Registrierung (neuer Endpunkt)

```
POST /mobile/api/push/register/   { "token": "...", "platform": "android" }
POST /mobile/api/push/unregister/ { "token": "..." }        # bei Logout
```
`update_or_create(token=...)` mit Zuordnung zum eingeloggten Cleaner; bei Logout
`aktiv=False` bzw. löschen. Auth über die bestehende Session (LoginRequiredMiddleware).

### 3.3 Versand: `firebase-admin`

```bash
pip install firebase-admin      # ins Server-venv
```
Service-Account-JSON aus der Firebase-Console als **nicht eingecheckte** Datei ablegen
(`/var/www/cleanorga/secrets/fcm-service-account.json`, Pfad über .env).

```python
# webinterface/services/push.py
import firebase_admin
from firebase_admin import credentials, messaging

def _init():
    if not firebase_admin._apps:
        firebase_admin.initialize_app(credentials.Certificate(settings.FCM_CREDENTIALS_PFAD))

def sende_push(cleaner, titel, text, daten: dict):
    _init()
    tokens = list(PushToken.objects.using(alias).filter(cleaner=cleaner, aktiv=True)
                                   .values_list('token', flat=True))
    if not tokens:
        return
    msg = messaging.MulticastMessage(
        tokens=tokens,
        notification=messaging.Notification(title=titel, body=text),
        data={k: str(v) for k, v in daten.items()},      # z.B. {"typ":"chat","partner":"admin"}
        android=messaging.AndroidConfig(priority='high'),
        apns=messaging.APNSConfig(payload=messaging.APNSPayload(
            aps=messaging.Aps(sound='default', content_available=True))),
    )
    antwort = messaging.send_each_for_multicast(msg)
    # Ungültige Tokens deaktivieren (Gerät deinstalliert / Token rotiert)
    for i, r in enumerate(antwort.responses):
        if not r.success and r.exception and 'registration-token-not-registered' in str(r.exception):
            PushToken.objects.using(alias).filter(token=tokens[i]).update(aktiv=False)
```

### 3.4 Integration in den bestehenden Chat-Flow

In `consumers.py handle_chat_message` (und im REST-Sende-Weg `api_cleaner_send_chat`)
**zusätzlich** zum `group_send` einen Push an den Empfänger auslösen:

```python
# nach dem erfolgreichen Speichern/Broadcast:
sende_push(empfaenger_cleaner,
           titel=absender_name,
           text=text[:120],                      # gekürzt, s. Datenschutz
           daten={'typ':'chat', 'partner': absender_id})
```
- **Doppelanzeige vermeiden:** Die App zeigt eine eingehende Push-Notification **nicht**,
  wenn sie gerade im Vordergrund im betreffenden Chat ist (Client-seitige Unterdrückung,
  s. 4.4). Alternativ serverseitig nur pushen, wenn keine aktive WS-Session – aber
  Channels bietet keine einfache Presence-API, daher ist die Client-Unterdrückung
  robuster und einfacher.
- **Aufgaben (optional, Phase 2):** In `broadcast_aufgabe_update` /
  `broadcast_assignment_update` denselben `sende_push` mit `{'typ':'task','task_id':...}`
  ergänzen → „Neue Aufgabe zugewiesen".

## 4. Client-Seite (MAUI)

### 4.1 Bibliothek

**`Plugin.Firebase`** (bewährt für MAUI, deckt Android FCM + iOS APNs-Bridging ab) oder
**`Shiny.Push`**. Empfehlung: `Plugin.Firebase.CloudMessaging` – schlank, aktiv, passt zum
FCM-Unified-Ansatz.

### 4.2 Token-Registrierung

- Beim erfolgreichen Login (nach `LoginPage`-Login) das FCM-Token holen und an
  `POST /mobile/api/push/register/` senden (Plattform mitgeben).
- Bei Token-Rotation (`OnTokenRefresh`) erneut registrieren.
- Bei Logout `POST /mobile/api/push/unregister/` senden und lokal löschen.

### 4.3 Berechtigungen

- **iOS:** `UNUserNotificationCenter.RequestAuthorization` (Alert+Sound+Badge) beim ersten
  Login. Entitlement `aps-environment` + Background Mode `remote-notification` (s. 5.2).
- **Android 13+ (API 33):** Runtime-Permission `POST_NOTIFICATIONS` anfragen; im Manifest
  ergänzen. Notification-Channel „Chat" anlegen (Wichtigkeit „High" für Heads-up).

### 4.4 Empfang & Navigation

```
Push empfangen:
  App im Vordergrund + im betreffenden Chat  → unterdrücken (WS zeigt es ohnehin)
  App im Vordergrund + anderswo              → In-App-Hinweis (wie heute App.OnChatMessageReceived)
  App im Hintergrund/geschlossen             → Systembenachrichtigung
Tap auf Notification:
  data.typ == 'chat' → Shell.Current.GoToAsync($"ChatCurrentPage?partner={data.partner}")
  data.typ == 'task' → AufgabePage öffnen
```
Die vorhandene `App.OnChatMessageReceived`-Logik (TTS/In-App-Popup) bleibt für den
Vordergrundfall; der Push deckt nur Hintergrund/geschlossen ab.

## 5. Plattform-Setup (einmalig)

### 5.1 Firebase / Android
1. Firebase-Projekt „CleanOrga" anlegen, Android-App `com.cleanorga.cleaner` registrieren.
2. `google-services.json` herunterladen → ins MAUI-Projekt (`Platforms/Android`), Build-Action
   `GoogleServicesJson`. **Nicht** ins öffentliche Repo (enthält Projekt-Keys) – über
   lokale Datei/CI-Secret einspielen.
3. Manifest: `POST_NOTIFICATIONS`-Permission + FCM-Service (vom Plugin bereitgestellt).

### 5.2 iOS / APNs
1. Im Apple Developer Portal einen **APNs Auth Key (.p8)** erstellen.
2. In der Firebase-Console (iOS-App `com.cleanorga.cleaner`) den .p8-Key + Key-ID + Team-ID
   hinterlegen → FCM stellt an iOS zu.
3. `GoogleService-Info.plist` ins Projekt (`Platforms/iOS`).
4. `Entitlements.plist`: `aps-environment` = `production`.
5. `Info.plist`: `UIBackgroundModes` → `remote-notification`.
6. Push-Capability im Provisioning-Profil aktivieren.

## 6. Datenschutz / DSGVO

- **Kein voller Nachrichteninhalt** in der Notification (Lockscreen sichtbar). Empfehlung:
  Titel = Absendername, Text gekürzt (≤120 Zeichen) oder nur „Neue Nachricht" – konfigurierbar.
- Das FCM-Token ist einem Cleaner zugeordnet (personenbezogen-nah) → in der
  Datenschutzerklärung aufnehmen; bei Logout/Deinstallation Token deaktivieren.
- Service-Account-JSON und `google-services.json`/`GoogleService-Info.plist` **nie**
  ins Repo (analog zum bereits ausgelagerten Android-Signing-Passwort).

## 7. Aufwand & Phasenplan

| Phase | Inhalt | Aufwand (grob) |
|-------|--------|----------------|
| 1a | Firebase-Projekt + APNs-Key + Plattform-Dateien | 0,5 Tag |
| 1b | Server: PushToken-Modell + Register/Unregister-Endpunkte + firebase-admin + Chat-Integration | 1 Tag |
| 1c | Client: Plugin.Firebase, Token-Registrierung, Permissions, Empfang+Navigation | 1–1,5 Tage |
| 1d | End-to-End-Test (2 Geräte/Emulatoren), Doppelanzeige-Unterdrückung, Token-Rotation | 0,5 Tag |
| 2  | Push auch für Aufgaben-Zuweisung/-Änderung | 0,5 Tag |
| 3  | Feinschliff: Badge-Count, Notification-Channel-Einstellungen, Zustellstatistik | optional |

**MVP (Phase 1) ≈ 3–4 Tage.** Danach funktioniert Chat unabhängig davon, ob die App im
Vordergrund ist.

## 8. Fallstricke & Betrieb

- **Token-Rotation:** FCM-Tokens ändern sich (Neuinstallation, Datenlöschung) → immer bei
  Login neu registrieren, ungültige Tokens serverseitig deaktivieren (s. 3.3).
- **iOS-Hintergrundlimits:** Ohne Push kein Hintergrund-WebSocket auf iOS (System beendet
  ihn) – deshalb ist Push der einzig verlässliche Weg, nicht „WebSocket länger offen halten".
- **Doppelanzeige** (WS + Push) im Vordergrund → Client-seitige Unterdrückung (4.4).
- **Multi-Device:** Ein Cleaner mit mehreren Geräten bekommt auf allen Push (gewollt).
- **Kein Push bei eigener Nachricht:** Nur an den Empfänger senden (der Sende-Code weiß das
  bereits – nur an die Gegenpartei).

## 9. Testbarkeit

- Die bestehende Appium-Testinfrastruktur (`Tests/`) kann Push mit **2 Emulatoren** oder
  1 Emulator + Server-Injektion testen: der `injiziere_admin_nachricht`-Helfer aus
  `test_chat.py` löst bereits den Server-Sendepfad aus – nach der Integration erzeugt
  derselbe Pfad zusätzlich den Push. Testfall CH10/CH11 (Hintergrund-Empfang) wird dann
  von FAIL auf PASS wechseln.
