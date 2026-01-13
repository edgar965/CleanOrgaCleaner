import jwt
import time
import requests

# API Credentials
KEY_ID = "W9C6VQH2Z7"
ISSUER_ID = "69a6de92-b5cb-47e3-e053-5b8c7c11a4d1"
PRIVATE_KEY = """-----BEGIN PRIVATE KEY-----
MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgi6wWm5QmriFv47Qp
hn5FGpzjl5501zVohV4DA34kfaygCgYIKoZIzj0DAQehRANCAAQFBVe9PpQrowde
hpPf3GMka9IGBOh9L5OWsIHw/HlBdZPjJlJOBI+vwLrTMF9PGOvfcqKR8lFUGJ+U
bTF5bi+W
-----END PRIVATE KEY-----"""

VERSION_ID = "09672428-cfa0-40a7-a73a-c6dc88846344"
LOC_ID = "a842fd9e-539c-4f54-ab34-047b622b89c1"

def generate_token():
    now = int(time.time())
    payload = {
        "iss": ISSUER_ID,
        "iat": now,
        "exp": now + 1200,
        "aud": "appstoreconnect-v1"
    }
    token = jwt.encode(payload, PRIVATE_KEY, algorithm="ES256", headers={"kid": KEY_ID, "typ": "JWT"})
    return token

token = generate_token()
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# German metadata
metadata = {
    "description": """CleanOrga - Die intelligente Putzplan-App fuer WGs und Co-Living

Schluss mit Streit ums Putzen! CleanOrga organisiert die Reinigungsaufgaben in Wohngemeinschaften, Co-Living-Gebaeuden und Mehrfamilienhaeusern automatisch und fair.

HAUPTFUNKTIONEN:

Automatischer Putzplan
- Woechentliche Zuweisung von Reinigungsaufgaben
- Faire Rotation zwischen allen Bewohnern
- Uebersichtlicher Kalender mit allen Terminen

Aufgabenverwaltung
- Detaillierte Aufgabenlisten pro Bereich
- Aufgaben einfach abhaken
- Fortschritt in Echtzeit verfolgen

Fotodokumentation
- Vorher/Nachher-Fotos hochladen
- Erledigte Arbeit dokumentieren
- Transparenz fuer alle Bewohner

Kommunikation
- Integrierter Chat zwischen Bewohnern
- Probleme direkt melden
- Schnelle Abstimmung bei Fragen

Sicherheit
- Biometrische Anmeldung (Face ID / Touch ID)
- Sichere Datenuebertragung
- Datenschutz nach DSGVO

IDEAL FUER:
- Wohngemeinschaften (WGs)
- Co-Living-Gebaeude
- Studentenwohnheime
- Mehrfamilienhaeuser
- Reinigungsteams
- Hausverwaltungen

CleanOrga macht Schluss mit vergessenen Putzaufgaben und unfairer Aufgabenverteilung. Laden Sie die App herunter und erleben Sie stressfreies Zusammenwohnen!""",

    "keywords": "Putzplan,WG,Reinigung,Haushalt,Aufgaben,Cleaning,Chores,Co-Living,Putzen,Hausarbeit",

    "promotionalText": "Nie wieder Streit ums Putzen - die smarte Putzplan-App fuer WGs und Co-Living!",

    "whatsNew": "Erste iOS-Version mit allen Funktionen: Putzplan, Aufgabenverwaltung, Chat und biometrische Anmeldung.",

    "supportUrl": "https://schwanenburg.de/CleanOrga",

    "marketingUrl": "https://schwanenburg.de/CleanOrga"
}

print("Updating German (de-DE) localization...")

update_data = {
    "data": {
        "type": "appStoreVersionLocalizations",
        "id": LOC_ID,
        "attributes": {
            "description": metadata["description"],
            "keywords": metadata["keywords"],
            "promotionalText": metadata["promotionalText"],
            "supportUrl": metadata["supportUrl"],
            "marketingUrl": metadata["marketingUrl"]
        }
    }
}

response = requests.patch(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{LOC_ID}",
    headers=headers,
    json=update_data
)

if response.status_code == 200:
    print("German localization updated successfully!")
else:
    print(f"Error: {response.status_code}")
    print(response.text)

# Now create English localization
print("\nCreating English (en-US) localization...")

en_metadata = {
    "description": """CleanOrga - The Smart Cleaning Schedule App for Shared Living

No more arguments about cleaning! CleanOrga automatically and fairly organizes cleaning tasks in shared apartments, co-living buildings, and multi-family homes.

KEY FEATURES:

Automatic Cleaning Schedule
- Weekly assignment of cleaning tasks
- Fair rotation between all residents
- Clear calendar with all appointments

Task Management
- Detailed task lists per area
- Easy task completion tracking
- Real-time progress monitoring

Photo Documentation
- Upload before/after photos
- Document completed work
- Transparency for all residents

Communication
- Built-in chat between residents
- Report issues directly
- Quick coordination on questions

Security
- Biometric login (Face ID / Touch ID)
- Secure data transmission
- GDPR-compliant privacy

PERFECT FOR:
- Shared apartments
- Co-living buildings
- Student dormitories
- Multi-family homes
- Cleaning teams
- Property managers

CleanOrga eliminates forgotten cleaning tasks and unfair task distribution. Download the app and experience stress-free shared living!""",

    "keywords": "cleaning,schedule,roommate,chores,tasks,shared,apartment,co-living,housework,planner",

    "promotionalText": "No more cleaning disputes - the smart cleaning schedule app for shared living!",

    "whatsNew": "First iOS release with all features: cleaning schedule, task management, chat, and biometric login.",

    "supportUrl": "https://schwanenburg.de/CleanOrga",

    "marketingUrl": "https://schwanenburg.de/CleanOrga"
}

en_loc_data = {
    "data": {
        "type": "appStoreVersionLocalizations",
        "attributes": {
            "locale": "en-US",
            "description": en_metadata["description"],
            "keywords": en_metadata["keywords"],
            "promotionalText": en_metadata["promotionalText"],
            "supportUrl": en_metadata["supportUrl"],
            "marketingUrl": en_metadata["marketingUrl"]
        },
        "relationships": {
            "appStoreVersion": {
                "data": {
                    "type": "appStoreVersions",
                    "id": VERSION_ID
                }
            }
        }
    }
}

response = requests.post(
    "https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations",
    headers=headers,
    json=en_loc_data
)

if response.status_code in [200, 201]:
    en_loc_id = response.json()["data"]["id"]
    print(f"English localization created successfully! ID: {en_loc_id}")
elif response.status_code == 409:
    print("English localization already exists, updating...")
    # Get existing localization
    response = requests.get(
        f"https://api.appstoreconnect.apple.com/v1/appStoreVersions/{VERSION_ID}/appStoreVersionLocalizations",
        headers=headers
    )
    localizations = response.json()
    en_loc_id = None
    for loc in localizations.get("data", []):
        if loc["attributes"]["locale"] == "en-US":
            en_loc_id = loc["id"]
            break

    if en_loc_id:
        update_data = {
            "data": {
                "type": "appStoreVersionLocalizations",
                "id": en_loc_id,
                "attributes": {
                    "description": en_metadata["description"],
                    "keywords": en_metadata["keywords"],
                    "promotionalText": en_metadata["promotionalText"],
                    "supportUrl": en_metadata["supportUrl"],
                    "marketingUrl": en_metadata["marketingUrl"]
                }
            }
        }
        response = requests.patch(
            f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{en_loc_id}",
            headers=headers,
            json=update_data
        )
        if response.status_code == 200:
            print("English localization updated successfully!")
        else:
            print(f"Error updating: {response.status_code} - {response.text[:200]}")
else:
    print(f"Error: {response.status_code}")
    print(response.text[:500])

print("\nDone!")
