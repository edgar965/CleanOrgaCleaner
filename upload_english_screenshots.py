import jwt
import time
import requests
import os

# API Credentials
KEY_ID = "W9C6VQH2Z7"
ISSUER_ID = "69a6de92-b5cb-47e3-e053-5b8c7c11a4d1"
PRIVATE_KEY = """-----BEGIN PRIVATE KEY-----
MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgi6wWm5QmriFv47Qp
hn5FGpzjl5501zVohV4DA34kfaygCgYIKoZIzj0DAQehRANCAAQFBVe9PpQrowde
hpPf3GMka9IGBOh9L5OWsIHw/HlBdZPjJlJOBI+vwLrTMF9PGOvfcqKR8lFUGJ+U
bTF5bi+W
-----END PRIVATE KEY-----"""

EN_LOC_ID = "018e4006-721e-4554-81d3-52f71a0816e4"

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

# Get screenshot sets for English localization
print("Finding English screenshot sets...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{EN_LOC_ID}/appScreenshotSets",
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
                            "id": EN_LOC_ID
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
