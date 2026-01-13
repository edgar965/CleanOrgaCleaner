# CleanOrga - iOS App Store Setup

## Übersicht

Diese Anleitung erklärt, wie du die iOS-App für den App Store vorbereitest.
**Du brauchst nur einmal kurz Zugang zu einem Mac** (ca. 15-30 Min).

---

## Teil 1: Apple Developer Portal (im Browser)

Diese Schritte kannst du auf jedem Computer machen.

### 1.1 App ID erstellen

1. Öffne: https://developer.apple.com/account/resources/identifiers/list
2. Klick auf **"+"** (neuer Identifier)
3. Wähle **"App IDs"** → Continue
4. Wähle **"App"** → Continue
5. Eingaben:
   - **Description:** `CleanOrga`
   - **Bundle ID:** Explicit → `com.cleanorga.cleaner`
6. **Capabilities** aktivieren (optional):
   - Push Notifications (falls benötigt)
7. Klick **"Register"**

### 1.2 App Store Connect - App erstellen

1. Öffne: https://appstoreconnect.apple.com/apps
2. Klick auf **"+"** → **"Neue App"**
3. Eingaben:
   - **Plattform:** iOS
   - **Name:** `CleanOrga`
   - **Primäre Sprache:** German
   - **Bundle-ID:** `com.cleanorga.cleaner` (aus Liste wählen)
   - **SKU:** `cleanorga-ios-001`
4. Klick **"Erstellen"**

---

## Teil 2: Am Mac (einmalig, ~15 Min)

### 2.1 Distribution Certificate erstellen

1. Öffne **Keychain Access** (Schlüsselbundverwaltung)
2. Menü: **Keychain Access → Certificate Assistant → Request a Certificate from a Certificate Authority**
3. Eingaben:
   - **User Email:** deine Apple ID E-Mail
   - **Common Name:** `CleanOrga Distribution`
   - **Request is:** Saved to disk
4. Speichern als `CertificateSigningRequest.certSigningRequest`

5. Gehe zu: https://developer.apple.com/account/resources/certificates/add
6. Wähle **"Apple Distribution"** → Continue
7. Lade die CSR-Datei hoch
8. **Download** das Zertifikat (`distribution.cer`)
9. **Doppelklick** auf die .cer-Datei → wird in Keychain installiert

### 2.2 Zertifikat als P12 exportieren

1. Öffne **Keychain Access**
2. Wähle Kategorie **"My Certificates"** (Meine Zertifikate)
3. Finde **"Apple Distribution: [Dein Name]"**
4. Rechtsklick → **"Export..."**
5. Format: **Personal Information Exchange (.p12)**
6. Speichern als: `CleanOrga_Distribution.p12`
7. **Passwort vergeben** (merken für später!)

### 2.3 Provisioning Profile erstellen

1. Gehe zu: https://developer.apple.com/account/resources/profiles/add
2. Wähle **"App Store"** (unter Distribution) → Continue
3. Wähle App ID: `com.cleanorga.cleaner` → Continue
4. Wähle dein Distribution Certificate → Continue
5. Name: `CleanOrga App Store`
6. **Generate** → **Download**
7. Speichere als: `CleanOrga_AppStore.mobileprovision`

### 2.4 App Store Connect API Key erstellen

1. Gehe zu: https://appstoreconnect.apple.com/access/api
2. Klick auf **"+"** um einen neuen Key zu erstellen
3. Eingaben:
   - **Name:** `CleanOrga GitHub Actions`
   - **Access:** `App Manager`
4. **Generate**
5. **Wichtig - Notiere:**
   - **Issuer ID:** (oben auf der Seite)
   - **Key ID:** (in der Tabelle)
6. **Download API Key** (`.p8` Datei) - NUR EINMAL MÖGLICH!

---

## Teil 3: GitHub Secrets einrichten

### 3.1 Dateien in Base64 umwandeln (am Mac)

Öffne Terminal und führe aus:

```bash
# P12 Certificate
base64 -i CleanOrga_Distribution.p12 | pbcopy
# → In Notizen einfügen als: IOS_P12_BASE64

# Provisioning Profile
base64 -i CleanOrga_AppStore.mobileprovision | pbcopy
# → In Notizen einfügen als: IOS_PROVISIONING_PROFILE_BASE64

# App Store Connect API Key
base64 -i AuthKey_XXXXXXXXXX.p8 | pbcopy
# → In Notizen einfügen als: APP_STORE_CONNECT_API_KEY_BASE64
```

### 3.2 GitHub Secrets hinzufügen

1. Gehe zu: https://github.com/edgar965/CleanOrgaCleaner/settings/secrets/actions
2. Füge diese Secrets hinzu:

| Secret Name | Wert |
|-------------|------|
| `IOS_P12_BASE64` | Base64 des P12-Zertifikats |
| `IOS_P12_PASSWORD` | Passwort das du beim P12-Export gewählt hast |
| `KEYCHAIN_PASSWORD` | Ein beliebiges Passwort (z.B. `build123`) |
| `IOS_CODESIGN_KEY` | `Apple Distribution: [Dein Name/Firma]` |
| `IOS_PROVISIONING_PROFILE_NAME` | `CleanOrga App Store` |
| `IOS_PROVISIONING_PROFILE_BASE64` | Base64 des Provisioning Profiles |
| `APP_STORE_CONNECT_API_KEY_ID` | Key ID von App Store Connect |
| `APP_STORE_CONNECT_ISSUER_ID` | Issuer ID von App Store Connect |
| `APP_STORE_CONNECT_API_KEY_BASE64` | Base64 der .p8 Datei |

---

## Teil 4: Build starten

Nachdem alle Secrets eingerichtet sind:

1. Gehe zu: https://github.com/edgar965/CleanOrgaCleaner/actions
2. Wähle **"iOS Build & Deploy"**
3. Klick **"Run workflow"**
4. Die App wird automatisch gebaut und zu TestFlight hochgeladen!

---

## Teil 5: App Store Eintrag ausfüllen

In App Store Connect (https://appstoreconnect.apple.com):

### Screenshots
- Verwende die gleichen wie für Android
- Formate: iPhone 6.7" (1290x2796), iPhone 6.5" (1284x2778)

### Beschreibung
- Kopiere aus `playstore/store_listing/description_DE.txt`

### Keywords
```
Reinigung,Cleaning,Facility,Management,Aufgaben,Tasks,Team,Housekeeping
```

### Support URL
```
https://cleanorga.com
```

### Privacy Policy URL
```
https://cleanorga.com/static/webinterface/privacy_policy.html
```

---

## Checkliste

- [ ] App ID erstellt
- [ ] App in App Store Connect erstellt
- [ ] Distribution Certificate erstellt
- [ ] P12 exportiert
- [ ] Provisioning Profile erstellt
- [ ] App Store Connect API Key erstellt
- [ ] Alle GitHub Secrets hinzugefügt
- [ ] Build erfolgreich
- [ ] App Store Eintrag ausgefüllt
- [ ] Screenshots hochgeladen
- [ ] Zur Prüfung eingereicht

---

## Fehlerbehebung

**Build schlägt fehl mit "No signing certificate":**
→ Prüfe ob IOS_P12_BASE64 und IOS_P12_PASSWORD korrekt sind

**Build schlägt fehl mit "Provisioning profile not found":**
→ Prüfe ob IOS_PROVISIONING_PROFILE_NAME exakt mit dem Namen in Apple Developer übereinstimmt

**Upload zu TestFlight schlägt fehl:**
→ Prüfe App Store Connect API Key Secrets
