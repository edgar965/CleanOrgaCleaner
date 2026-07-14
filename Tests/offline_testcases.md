# Offline-Testfälle CleanOrgaCleaner (MAUI-App)

Stand: 2026-07-14 · Zielversion 1.67 · Testzugang: Property 1, User `tom`, Passwort `tom`
Netzwerk-Simulation im Android-Emulator: `adb shell svc wifi disable|enable` + `adb shell svc data disable|enable`.

| # | Titel | Vorbedingung | Schritte | Erwartet | Automatisiert |
|---|-------|--------------|----------|----------|---------------|
| TC01 | Offline-Kaltstart mit Cache | Online eingeloggt, Aufgabenliste wurde geladen (Cache gefüllt), App beendet | Netz aus → App starten | Offline-Login greift, gecachte Aufgaben werden angezeigt, Offline-Banner sichtbar, kein Crash | ja |
| TC02 | Offline-Kaltstart ohne Cache | App-Daten gelöscht (frische Installation) | Netz aus → App starten → Login versuchen | Saubere Fehlermeldung („keine Verbindung"), kein Crash, kein Hänger | ja |
| TC03 | Online→Offline auf der Aufgabenliste | Eingeloggt, Aufgabenliste sichtbar | Netz aus → Pull-to-Refresh | Gecachte Aufgaben bleiben sichtbar (werden NICHT durch leere Liste ersetzt), Offline-Banner an | ja |
| TC04 | Cache überlebt Offline-Refresh (Regression Fix „Cache-Zerstörung") | TC03 durchgeführt | App beenden (weiter offline) → App starten | Aufgaben weiterhin sichtbar — `cached_tasks.json` wurde durch den fehlgeschlagenen Refresh nicht überschrieben | ja |
| TC05 | Arbeitsbeginn offline | Eingeloggt, offline | „Start"-Button im Header tippen | Hinweis „offline gespeichert", Button wechselt auf „Beenden"; Aktion liegt in der Offline-Queue | ja |
| TC06 | Offline-Queue synct nach Reconnect | TC05 durchgeführt | Netz an → ~15 s warten | Queue wird verarbeitet; Arbeitsbeginn ist am Server registriert (DB-Prüfung `Arbeitstag`/work-status) | ja (Server-Check per SSH) |
| TC07 | Chat offline an Admin | Eingeloggt, offline, Chat mit Admin geöffnet | Nachricht senden | Hinweis offline/Queue; nach Reconnect kommt die Nachricht beim Admin an (DB-Prüfung ChatMessage) | ja (Server-Check) |
| TC08 | Chat offline an Kollegen (Regression Fix „falscher Empfänger") | Eingeloggt, offline, Chat mit einem KOLLEGEN geöffnet | Nachricht senden → Netz an → Sync abwarten | Nachricht ist beim Kollegen (receiver=Kollege), NICHT beim Admin | ja (Server-Check) |
| TC09 | Offline-Banner reagiert auf Netzwechsel | Eingeloggt, online | Netz aus → warten → Netz an → warten | Banner erscheint nach Trennung und verschwindet nach Reconnect | ja |
| TC10 | Beschreibungsänderung kommt nach Reconnect an | Eingeloggt, Aufgabe des Cleaners existiert | Netz aus → am Server Beschreibung der Aufgabe ändern → Netz an → warten | Aufgabenliste/Detail zeigt nach Reconnect die neue Beschreibung (WebSocket/Reload) | ja (Server-Änderung per SSH) |
| TC11 | Netz-Flattern (Regression Fix „Connect-Race") | Eingeloggt | Netz 5× im Abstand von ~3 s aus/an → 30 s warten | App stabil, kein Crash, Banner-Zustand am Ende korrekt (online) | ja |
| TC12 | Task-Status offline ändern | Eingeloggt, Aufgabe vorhanden, Arbeit gestartet, offline | Aufgabe öffnen (aus Cache) → Start/Erledigt setzen | „Offline gespeichert"; nach Reconnect Status am Server aktualisiert | teilweise (abhängig von Task-Daten des Testtags) |
| TC13 | Bildcache offline (Regression Fix „GetHashCode") | Aufgabe mit Fotos wurde online geöffnet (Bilder gecacht), App-Neustart | Offline dieselbe Aufgabe öffnen | Fotos werden aus dem Cache angezeigt (Cache-Datei wird nach Neustart wiedergefunden) | manuell (visuelle Prüfung) |
| TC14 | Queue-Retry-Limit | Queue-Item, das dauerhaft fehlschlägt (z.B. Session serverseitig beendet) | 10+ Sync-Versuche provozieren | Item wird nach 10 Versuchen verworfen (kein Endlos-Resend), App stabil | manuell/Unit |

## Automatisierungs-Architektur

- **Appium 3 + UiAutomator2-Driver**, Python-Client (`Appium-Python-Client`), Emulator `pixel_7_-_api_36`.
- Element-Zugriff über Android-Klassen + Texte (MAUI vergibt keine AutomationIds): Login-Felder = `EditText`-Reihenfolge (Property, User, Passwort), Buttons über Text/Description.
- Netzwerk: `adb shell svc wifi disable && adb shell svc data disable` (bzw. `enable`).
- Server-Verifikation (TC06–TC08, TC10): per SSH auf cleanorga.com, Django-ORM-Einzeiler gegen `property_1`.
- Testdaten-Hygiene: Chat-Testnachrichten tragen den Präfix `[APPIUM-TEST]` und werden nach dem Lauf serverseitig gelöscht.
