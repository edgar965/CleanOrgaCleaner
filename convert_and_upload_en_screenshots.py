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

EN_LOC_ID = "018e4006-721e-4554-81d3-52f71a0816e4"

# iOS sizes
IOS_SIZES = {
    "67": (1290, 2796),  # iPhone 6.7"
    "65": (1284, 2778),  # iPhone 6.5"
}

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

# Source and output directories
src_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/screenshots_en"
out_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/screenshots_en_ios"
os.makedirs(out_dir, exist_ok=True)

# Screenshots to process (skip temp.png and use only the good ones)
screenshots = [
    "02_today.png",      # Today view - good for main screenshot
    "04_detail.png",     # Task detail
    "05_chat.png",       # Chat list
    "06_chat_detail.png" # Chat detail
]

print("Converting screenshots to iOS sizes...")

for filename in screenshots:
    src_path = os.path.join(src_dir, filename)
    if not os.path.exists(src_path):
        print(f"  Skipping {filename} - not found")
        continue

    img = Image.open(src_path)
    base_name = filename.replace(".png", "")

    for size_suffix, (width, height) in IOS_SIZES.items():
        # Resize maintaining aspect ratio and then crop/pad to exact size
        img_ratio = img.width / img.height
        target_ratio = width / height

        if img_ratio > target_ratio:
            # Image is wider - fit by height
            new_height = height
            new_width = int(height * img_ratio)
        else:
            # Image is taller - fit by width
            new_width = width
            new_height = int(width / img_ratio)

        resized = img.resize((new_width, new_height), Image.LANCZOS)

        # Center crop to exact size
        left = (new_width - width) // 2
        top = (new_height - height) // 2
        cropped = resized.crop((left, top, left + width, top + height))

        out_filename = f"{base_name}_{size_suffix}.png"
        out_path = os.path.join(out_dir, out_filename)
        cropped.save(out_path, "PNG")
        print(f"  Created {out_filename}")

print("\nUploading to App Store Connect (English localization)...")

token = generate_token()
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# First delete existing screenshots
print("Checking for existing screenshots...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{EN_LOC_ID}/appScreenshotSets",
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

# Now upload new screenshots
DISPLAY_TYPES = {
    "APP_IPHONE_67": "_67.png",
    "APP_IPHONE_65": "_65.png",
}

# Regenerate token
token = generate_token()
headers["Authorization"] = f"Bearer {token}"

# Get screenshot sets (they should already exist from previous uploads)
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{EN_LOC_ID}/appScreenshotSets",
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
