# CleanOrga - Google Play Store Anleitung

## Vorbereitete Dateien

| Datei | Pfad | Beschreibung |
|-------|------|--------------|
| **AAB-Datei** | `CleanOrgaCleaner\bin\Release\net10.0-android\com.cleanorga.cleaner-Signed.aab` | App-Bundle für Upload |
| **Screenshots** | `playstore\screenshots\final\` | 4 Screenshots |
| **Feature Graphic** | `playstore\feature_graphic.html` | Im Browser öffnen & Screenshot machen |
| **Beschreibung DE** | `playstore\store_listing\description_DE.txt` | Deutsche Texte |
| **Beschreibung EN** | `playstore\store_listing\description_EN.txt` | Englische Texte |
| **Privacy Policy** | https://cleanorga.com/static/webinterface/privacy_policy.html | Online verfügbar |

---

## Schritt-für-Schritt Anleitung

### 1. Google Play Console öffnen
- URL: https://play.google.com/console
- Mit Google-Konto anmelden
- Falls noch nicht registriert: $25 Entwicklergebühr zahlen

### 2. App erstellen
1. Klick auf **"App erstellen"** (Create app)
2. **App-Name:** `CleanOrga`
3. **Standardsprache:** Deutsch
4. **App oder Spiel:** App
5. **Kostenlos oder kostenpflichtig:** Kostenlos
6. Erklärungen akzeptieren → **"App erstellen"**

### 3. Dashboard - Aufgaben erledigen

#### 3.1 App-Zugriff einrichten
- Pfad: Dashboard → App-Zugriff
- Auswählen: **"Alle Funktionen sind ohne spezielle Zugangsdaten verfügbar"**
  - ODER: Test-Zugangsdaten bereitstellen:
    - URL: https://cleanorga.com
    - Property: 3
    - Benutzer: tania
    - Passwort: (dein Test-Passwort)

#### 3.2 Anzeigen
- Pfad: Dashboard → Anzeigen
- Auswählen: **"Nein, meine App enthält keine Werbung"**

#### 3.3 Inhaltsfreigabe
- Pfad: Dashboard → Inhaltsfreigabe
- Fragebogen ausfüllen:
  - Gewalt: Nein
  - Sexuelle Inhalte: Nein
  - Sprache: Nein
  - Kontrollierte Substanzen: Nein
  - usw. → alles "Nein"
- Ergebnis sollte sein: **PEGI 3** oder **USK 0**

#### 3.4 Zielgruppe
- Pfad: Dashboard → Zielgruppe und Inhalt
- **Zielgruppe:** 18+ (keine Kinder-App)
- App ist NICHT für Kinder konzipiert

#### 3.5 Datenschutzerklärung
- Pfad: Dashboard → Datenschutzerklärung
- URL eingeben: `https://cleanorga.com/static/webinterface/privacy_policy.html`

### 4. Store-Eintrag erstellen

#### 4.1 Haupt-Store-Eintrag
- Pfad: Darstellung im Store → Haupt-Store-Eintrag

**App-Name:**
```
CleanOrga
```

**Kurzbeschreibung:** (aus description_DE.txt kopieren)
```
Reinigungsmanagement für Teams - Aufgaben, Zeiterfassung & Kommunikation
```

**Vollständige Beschreibung:** (aus description_DE.txt kopieren)

#### 4.2 Grafiken hochladen

**App-Symbol:**
- Bereits in der AAB enthalten (wird automatisch extrahiert)

**Feature-Grafik (1024x500):**
1. `playstore\feature_graphic.html` im Browser öffnen
2. Screenshot des lila Banners machen (1024x500)
3. Hochladen

**Screenshots (mindestens 2):**
- Alle 4 Bilder aus `playstore\screenshots\final\` hochladen

### 5. Release erstellen

#### 5.1 Produktions-Track
- Pfad: Release → Produktion
- Klick auf **"Neuen Release erstellen"**

#### 5.2 App-Integrität
- **Von Google verwaltete Signatur:** JA (empfohlen)
- Hochladen: `com.cleanorga.cleaner-Signed.aab`

#### 5.3 Release-Name
```
0.55 (9)
```

#### 5.4 Versionshinweise
```
Erste Veröffentlichung von CleanOrga:
- Aufgabenverwaltung für Reinigungsteams
- Zeiterfassung
- Foto-Dokumentation
- Team-Chat
- Mehrsprachig (8 Sprachen)
```

### 6. Review einreichen
1. Alle Checklisten-Punkte grün?
2. Klick auf **"Release prüfen"**
3. Klick auf **"Rollout für Produktion starten"**

---

## Nach dem Einreichen

- **Review-Zeit:** 1-7 Tage (erste App kann länger dauern)
- **Status prüfen:** Dashboard → Veröffentlichungsübersicht
- **Bei Ablehnung:** Grund lesen und korrigieren

---

## Wichtige URLs

| Was | URL |
|-----|-----|
| Play Console | https://play.google.com/console |
| Privacy Policy | https://cleanorga.com/static/webinterface/privacy_policy.html |
| CleanOrga Server | https://cleanorga.com |

---

## Keystore-Backup (WICHTIG!)

Diese Daten sicher aufbewahren - ohne sie sind keine Updates möglich!

```
Pfad: D:\Daten\CleanOrga\CleanOrgaCleaner\signing\cleanorga-release.keystore
Alias: cleanorga
Passwort: Rotron#2
```

**Empfehlung:** Keystore-Datei auf externem Laufwerk oder Cloud sichern!
