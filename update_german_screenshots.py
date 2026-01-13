import jwt
import time
import requests
import os
from PIL import Image

# API Credentials
KEY_ID = "W9C6VQH2Z7"
ISSUER_ID = "69a6de92-b5cb-47e3-e053-5b8c7c11a4d1"
PRIVATE_KEY = """-----BEGIN PRIVATE KEY-----
MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgi6wWm5QmriFv47Qp
hn5FGpzjl5501zVohV4DA34kfaygCgYIKoZIzj0DAQehRANCAAQFBVe9PpQrowde
hpPf3GMka9IGBOh9L5OWsIHw/HlBdZPjJlJOBI+vwLrTMF9PGOvfcqKR8lFUGJ+U
bTF5bi+W
-----END PRIVATE KEY-----"""

DE_LOC_ID = "a842fd9e-539c-4f54-ab34-047b622b89c1"

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

# Use the same iOS screenshots we created for English
# They work for German too since the task names are in German anyway
out_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/screenshots_en_ios"

DISPLAY_TYPES = {
    "APP_IPHONE_67": "_67.png",
    "APP_IPHONE_65": "_65.png",
}

print("Updating German screenshots with chat views...")

token = generate_token()
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# First delete existing screenshots
print("Checking for existing screenshots...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{DE_LOC_ID}/appScreenshotSets",
    headers=headers
)
screenshot_sets = response.json()

for ss_set in screenshot_sets.get("data", []):
    set_id = ss_set["id"]
    display_type = ss_set["attributes"]["screenshotDisplayType"]
    print(f"  Deleting existing screenshots in {display_type}...")

    # Get screenshots in this set
    resp = requests.get(
        f"https://api.appstoreconnect.apple.com/v1/appScreenshotSets/{set_id}/appScreenshots",
        headers=headers
    )
    for screenshot in resp.json().get("data", []):
        del_resp = requests.delete(
            f"https://api.appstoreconnect.apple.com/v1/appScreenshots/{screenshot['id']}",
            headers=headers
        )
        if del_resp.status_code in [200, 204]:
            print(f"    Deleted {screenshot['id']}")

# Regenerate token
token = generate_token()
headers["Authorization"] = f"Bearer {token}"

# Get screenshot sets
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{DE_LOC_ID}/appScreenshotSets",
    headers=headers
)
screenshot_sets = response.json()
print(f"Found {len(screenshot_sets.get('data', []))} existing screenshot sets")

for display_type, suffix in DISPLAY_TYPES.items():
    existing_set = None
    for s in screenshot_sets.get("data", []):
        if s["attributes"]["screenshotDisplayType"] == display_type:
            existing_set = s
            break

    if not existing_set:
        print(f"\n{display_type}: No existing set found, skipping...")
        continue

    print(f"\nUsing existing set for {display_type}: {existing_set['id']}")

    set_id = existing_set["id"]

    # Find and upload screenshots for this size
    ios_screenshots = sorted([f for f in os.listdir(out_dir) if f.endswith(suffix)])

    for idx, filename in enumerate(ios_screenshots):
        filepath = os.path.join(out_dir, filename)
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
