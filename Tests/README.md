# CleanOrgaCleaner – Appium-Testsuiten

Automatisierte UI-Tests der MAUI-App gegen einen Android-Emulator. Alle Tests liegen
in **diesem Verzeichnis** und sind jederzeit über `run_all.py` aufrufbar.

## Inhalt

| Datei | Zweck |
|-------|-------|
| `common.py` | Gemeinsame Helfer (adb, Netz-Toggle, Appium-Treiber, Login, Menü-Navigation, Server-Verifikation, Protokoll) |
| `test_offline.py` | Offline-Verhalten (Cache, Queue, Reconnect, Banner) — 12 Fälle |
| `test_chat.py` | Chat (senden, empfangen, Echtzeit, Hintergrund, offline) — 8 Fälle, mit Server-Injektion |
| `test_funktionen.py` | Übrige Client-Funktionen (Navigation, Arbeitszeit, Auftrag, Einstellungen, Logout) — 11 Fälle |
| `run_all.py` | Runner für alle Suiten nacheinander |
| `offline_testcases.md` / `chat_testcases.md` | Fachliche Testfall-Beschreibungen |
| `screenshots/` | Automatisch abgelegte Screenshots je Testschritt |

## Voraussetzungen (einmalig)

1. **Android-Emulator** (Pixel 7, API 36) läuft als `emulator-5554`:
   ```powershell
   & "$env:ANDROID_HOME\emulator\emulator.exe" -avd pixel_7_-_api_36
   ```
2. **Appium 3 + UiAutomator2-Treiber**:
   ```powershell
   npm install -g appium
   appium driver install uiautomator2
   pip install Appium-Python-Client
   appium            # Server auf 127.0.0.1:4723 laufen lassen
   ```
3. **Debug-APK MIT eingebetteten Assemblies** bauen und installieren
   (WICHTIG: ohne `EmbedAssembliesIntoApk` überlebt die App kein `pm clear`):
   ```powershell
   cd ..\CleanOrgaCleaner
   dotnet build -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=
   adb install -r bin\Debug\net10.0-android\com.cleanorga.cleaner-Signed.apk
   ```
4. **SSH-Zugang** zu `root@91.99.235.72` (für Server-Verifikation der Chat-/Offline-Fälle).

## Aufruf

```powershell
python run_all.py              # alle Suiten
python run_all.py offline      # nur Offline
python run_all.py chat         # nur Chat
python run_all.py funktionen   # nur Funktionen
python test_offline.py         # einzelne Suite direkt
```

## Testzugang

Property 1, User `tom` / Passwort `tom` (Cleaner id 9, offizieller Test-Account).
Für Chat: Admin id 11, Kollege `aylin` id 3. Alle Test-Chatnachrichten tragen den
Präfix `[APPIUM-TEST]` und werden nach jedem Lauf serverseitig gelöscht.

## Bekannte Grenzen

- **Ein Emulator, sequenziell**: Die Suiten teilen sich Emulator + Appium-Session und
  laufen nacheinander.
- **Chat im Hintergrund (CH10/CH11)**: belegt die fehlende Push-Notification-Architektur.
  Echtzeit-Empfang funktioniert nur bei App im Vordergrund (WebSocket wird im Hintergrund
  getrennt). Das ist ein dokumentierter Ist-Zustand, kein Testfehler.
- **Menü-Navigation** läuft über das Hamburger-Overlay im Header (keine untere Tab-Bar).
