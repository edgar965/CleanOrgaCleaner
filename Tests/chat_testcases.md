# Chat-Testfälle CleanOrgaCleaner (MAUI-App)

Stand: 2026-07-14 · Zielversion 1.67 · Testzugang: Property 1, `tom`/`tom` (Cleaner, id 9)
Zweiter Gesprächspartner für Kollegen-Tests: ein weiterer Cleaner in Property 1 (per Django ermittelt).

## Wichtige Architektur-Erkenntnis (Ursache des gemeldeten Fehlers)

Der Chat läuft über **einen WebSocket** (`wss://cleanorga.com/ws/main/`). Es gibt **keine Push-Notifications**
(kein Firebase/FCM, kein APNs). Die App **trennt den WebSocket, sobald sie in den Hintergrund geht**
(`App.OnAppStopped → DisconnectAsync`). Folge: Echtzeit-Empfang funktioniert **nur, solange die App im
Vordergrund läuft**. Genau das erklärt „funktioniert nur, wenn die Handys nah beieinander sind" — dann
schauen beide gleichzeitig aufs Gerät (beide Apps aktiv). Legt einer das Handy weg (App im Hintergrund,
Bildschirm aus), bekommt er die Nachricht erst beim nächsten Öffnen der App (dann lädt die Historie nach).
TC07 und TC10 reproduzieren das.

## Testfälle

| # | Titel | Vorbedingung | Schritte | Erwartet | Automatisiert |
|---|-------|--------------|----------|----------|---------------|
| CH01 | Chat-Liste lädt | Eingeloggt, online | Chat-Tab öffnen | Liste zeigt „Admin" + Kollegen, kein Crash | ja |
| CH02 | Nachricht an Admin senden | Chat mit Admin offen | Text eingeben, senden | Nachricht erscheint rechts (eigene), am Server gespeichert (ChatMessage-Count steigt) | ja (Server-Check) |
| CH03 | Chat-Historie lädt | Es existieren Nachrichten mit Admin | Chat mit Admin öffnen | Vorherige Nachrichten werden chronologisch angezeigt | ja |
| CH04 | Eingehende Nachricht in Echtzeit (Vordergrund) | Chat mit Admin offen, App im Vordergrund, online | Nachricht serverseitig als Admin an tom injizieren (Django) | Nachricht erscheint links **ohne** App-Neustart (WebSocket-Push) | ja (Server-Injektion) |
| CH05 | Nachricht landet im richtigen Thread (Regression Fix 2) | Chat mit **Kollegen X** offen | Server injiziert eine Nachricht vom **Admin** an tom | Die Admin-Nachricht erscheint **nicht** im Kollegen-Thread; nach Wechsel in den Admin-Chat ist sie dort | ja (Server-Injektion) |
| CH06 | Chat mit Kollegen senden | Chat mit Kollege offen | Nachricht senden | Kommt beim richtigen Kollegen an (receiver == Kollege, DB-Check), nicht beim Admin | ja (Server-Check) |
| CH07 | Offline senden → Queue → Reconnect | Chat mit Admin offen, offline | Nachricht senden → Netz an → warten | „offline gespeichert"; nach Reconnect ist die Nachricht am Server (Fix 8: richtiger Empfänger) | ja (Server-Check) |
| CH08 | Chat löschen betrifft richtigen Partner (Regression Fix 9) | Nachrichten mit Admin UND Kollege vorhanden | Im Kollegen-Chat „löschen" | Nur Kollegen-Nachrichten weg, Admin-Chat unberührt (DB-Check partner_id) | ja (Server-Check) |
| CH09 | Verbindungsabbruch während Chat | Chat offen, online | Netz aus → Nachricht senden → Netz an | Kein Crash; Nachricht wird gequeued und nach Reconnect zugestellt | ja |
| CH10 | Hintergrund verpasst Nachricht, Nachladen beim Öffnen (reproduziert Kernproblem) | tom eingeloggt, Chat mit Admin | App in den Hintergrund (Home) → Server injiziert Nachricht → App wieder in den Vordergrund | Echtzeit-Empfang im Hintergrund schlägt fehl (WebSocket getrennt), ABER beim Zurückkehren wird die Nachricht aus der Historie nachgeladen und angezeigt | ja (Server-Injektion + Backgrounding) |
| CH11 | Nachricht im Hintergrund → keine Systembenachrichtigung (Doku des Ist-Zustands) | wie CH10 | Nachricht injizieren während App im Hintergrund | **Keine** Push/Notification erscheint (bestätigt fehlende Push-Integration) — Beleg für den Fix-Bedarf | ja (Beobachtung) |
| CH12 | Netz-Flattern während Chat offen | Chat offen | Netz 5× aus/an | App stabil, WebSocket verbindet neu, kein Doppel-Listener/Crash (Regression Fix 6/7) | ja |

## Automatisierung

- Server-Injektion (CH04/CH05/CH10): per SSH einen Django-ORM-Aufruf, der eine `ChatMessage` vom Admin an
  `tom` erzeugt und über den Channel-Layer an die `ws/main`-Gruppe pusht (gleicher Pfad wie ein echter
  Admin-Chat). Alternativ über den bestehenden Admin-Chat-Sende-Endpunkt mit Admin-Session.
- Kollegen-Ermittlung: `Cleaner.objects.using('property_1').exclude(name='tom').first()`.
- Alle Testnachrichten mit Präfix `[APPIUM-TEST]`, danach serverseitig löschen.
- Hintergrund/Vordergrund: `adb shell input keyevent KEYCODE_HOME` bzw. App per `am start` reaktivieren.
