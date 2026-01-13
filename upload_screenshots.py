import jwt
import time
import requests
import os
import json

# API Credentials
KEY_ID = "W9C6VQH2Z7"
ISSUER_ID = "69a6de92-b5cb-47e3-e053-5b8c7c11a4d1"
PRIVATE_KEY = """-----BEGIN PRIVATE KEY-----
MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgi6wWm5QmriFv47Qp
hn5FGpzjl5501zVohV4DA34kfaygCgYIKoZIzj0DAQehRANCAAQFBVe9PpQrowde
hpPf3GMka9IGBOh9L5OWsIHw/HlBdZPjJlJOBI+vwLrTMF9PGOvfcqKR8lFUGJ+U
bTF5bi+W
-----END PRIVATE KEY-----"""

APP_ID = "6757746774"
VERSION_ID = "09672428-cfa0-40a7-a73a-c6dc88846344"

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

# Get localizations for this version
print("Finding localizations...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersions/{VERSION_ID}/appStoreVersionLocalizations",
    headers=headers
)
localizations = response.json()

if not localizations.get("data"):
    print("No localizations found, creating German localization...")
    loc_data = {
        "data": {
            "type": "appStoreVersionLocalizations",
            "attributes": {
                "locale": "de-DE",
                "description": "CleanOrga ist die perfekte App fuer die Organisation von Reinigungsaufgaben in Wohngemeinschaften und Co-Living-Gebaeuden.\n\nFUNKTIONEN:\n- Uebersichtlicher Putzplan mit woechentlicher Zuweisung\n- Aufgaben abhaken und Fortschritt verfolgen\n- Fotos von erledigten Aufgaben hochladen\n- Chat-Funktion fuer schnelle Kommunikation\n- Biometrische Anmeldung (Face ID / Touch ID)\n- Mehrsprachig\n\nIdeal fuer Hausverwalter, WG-Bewohner und Reinigungsteams.",
                "keywords": "Putzplan,Reinigung,WG,Haushalt,Aufgaben,Cleaning,Chores,Co-Living,Team",
                "marketingUrl": "https://schwanenburg.de/CleanOrga",
                "supportUrl": "https://schwanenburg.de/CleanOrga",
                "promotionalText": "Effiziente Reinigungsplanung fuer WGs und Co-Living."
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
        json=loc_data
    )
    if response.status_code not in [200, 201]:
        print(f"Error creating localization: {response.status_code}")
        print(response.text)
        exit(1)
    else:
        localizations = {"data": [response.json()["data"]]}
        print("Created German localization")

loc_id = localizations["data"][0]["id"]
locale = localizations["data"][0]["attributes"]["locale"]
print(f"Using localization: {locale} (ID: {loc_id})")

# Get screenshot sets
print("\nFinding screenshot sets...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{loc_id}/appScreenshotSets",
    headers=headers
)
screenshot_sets = response.json()
print(f"Found {len(screenshot_sets.get('data', []))} screenshot sets")

# Display types we need
DISPLAY_TYPES = {
    "APP_IPHONE_67": "_67.png",
    "APP_IPHONE_65": "_65.png",
}

for display_type, suffix in DISPLAY_TYPES.items():
    existing_set = None
    for s in screenshot_sets.get("data", []):
        if s["attributes"]["screenshotDisplayType"] == display_type:
            existing_set = s
            break

    if not existing_set:
        print(f"\nCreating screenshot set for {display_type}...")
        set_data = {
            "data": {
                "type": "appScreenshotSets",
                "attributes": {
                    "screenshotDisplayType": display_type
                },
                "relationships": {
                    "appStoreVersionLocalization": {
                        "data": {
                            "type": "appStoreVersionLocalizations",
                            "id": loc_id
                        }
                    }
                }
            }
        }
        response = requests.post(
            "https://api.appstoreconnect.apple.com/v1/appScreenshotSets",
            headers=headers,
            json=set_data
        )
        if response.status_code not in [200, 201]:
            print(f"Error creating set: {response.status_code} - {response.text[:200]}")
            continue
        existing_set = response.json()["data"]
        print(f"Created set: {existing_set['id']}")
    else:
        print(f"\nUsing existing set for {display_type}: {existing_set['id']}")

    set_id = existing_set["id"]

    screenshot_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/screenshots"
    screenshots = sorted([f for f in os.listdir(screenshot_dir) if f.endswith(suffix)])

    for idx, filename in enumerate(screenshots):
        filepath = os.path.join(screenshot_dir, filename)
        filesize = os.path.getsize(filepath)

        print(f"  Uploading {filename} ({filesize} bytes)...")

        reserve_data = {
            "data": {
                "type": "appScreenshots",
                "attributes": {
                    "fileName": filename,
                    "fileSize": filesize
                },
                "relationships": {
                    "appScreenshotSet": {
                        "data": {
                            "type": "appScreenshotSets",
                            "id": set_id
                        }
                    }
                }
            }
        }

        response = requests.post(
            "https://api.appstoreconnect.apple.com/v1/appScreenshots",
            headers=headers,
            json=reserve_data
        )

        if response.status_code not in [200, 201]:
            print(f"    Error reserving: {response.status_code} - {response.text[:200]}")
            continue

        screenshot_data = response.json()["data"]
        screenshot_id = screenshot_data["id"]
        upload_ops = screenshot_data["attributes"]["uploadOperations"]

        with open(filepath, "rb") as f:
            file_data = f.read()

        for op in upload_ops:
            upload_headers = {h["name"]: h["value"] for h in op["requestHeaders"]}
            offset = op["offset"]
            length = op["length"]
            chunk = file_data[offset:offset+length]

            upload_response = requests.put(
                op["url"],
                headers=upload_headers,
                data=chunk
            )

            if upload_response.status_code not in [200, 201]:
                print(f"    Upload failed: {upload_response.status_code}")

        commit_data = {
            "data": {
                "type": "appScreenshots",
                "id": screenshot_id,
                "attributes": {
                    "uploaded": True,
                    "sourceFileChecksum": screenshot_data["attributes"]["sourceFileChecksum"]
                }
            }
        }

        response = requests.patch(
            f"https://api.appstoreconnect.apple.com/v1/appScreenshots/{screenshot_id}",
            headers=headers,
            json=commit_data
        )

        if response.status_code == 200:
            print(f"    OK")
        else:
            print(f"    Commit: {response.status_code}")

print("\nDone!")
