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

APP_ID = "6757746774"

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

# First, get the app info
print("Getting app info...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/apps/{APP_ID}/appInfos",
    headers=headers
)
print(f"Status: {response.status_code}")
app_infos = response.json()

if not app_infos.get("data"):
    print("No app infos found")
    print(response.text)
    exit(1)

app_info_id = app_infos["data"][0]["id"]
print(f"App Info ID: {app_info_id}")

# Get app info localizations
print("\nGetting app info localizations...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appInfos/{app_info_id}/appInfoLocalizations",
    headers=headers
)
localizations = response.json()
print(f"Found {len(localizations.get('data', []))} localizations")

for loc in localizations.get("data", []):
    print(f"  - {loc['attributes']['locale']}: {loc['id']}")

# The app icon is set at the app level, not version level
# We need to check if there's an existing app icon and update it

# Let's try to get the primary locale localization
primary_loc_id = None
for loc in localizations.get("data", []):
    if loc["attributes"]["locale"] == "de-DE":
        primary_loc_id = loc["id"]
        break

if not primary_loc_id and localizations.get("data"):
    primary_loc_id = localizations["data"][0]["id"]

print(f"\nUsing localization: {primary_loc_id}")

# Check for existing app icon
# The app icon is typically managed through appStoreVersions or appInfoLocalizations
# Let's check the available endpoints

# Try to upload via appInfoLocalizations
icon_path = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/icons/appstore_icon_1024.png"
filesize = os.path.getsize(icon_path)
filename = "appstore_icon_1024.png"

print(f"\nIcon file: {icon_path}")
print(f"File size: {filesize} bytes")

# The App Store Connect API doesn't have a direct endpoint for app icons
# App icons are typically uploaded through Xcode or Transporter
# However, we can try the appCustomProductPageVersions or other endpoints

# Let's check if there's an appStoreVersion we can use
print("\nChecking app store versions...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/apps/{APP_ID}/appStoreVersions",
    headers=headers
)
versions = response.json()
print(f"Found {len(versions.get('data', []))} versions")

for ver in versions.get("data", []):
    print(f"  - {ver['attributes']['versionString']}: {ver['attributes']['appStoreState']} (ID: {ver['id']})")

# Unfortunately, the App Store Connect API doesn't support uploading the main app icon
# The app icon must be uploaded through:
# 1. Xcode (in the asset catalog)
# 2. App Store Connect web interface
# 3. Transporter app

print("\n" + "="*60)
print("NOTE: The App Store Connect API does not support uploading")
print("the main app icon directly. You need to upload it via:")
print("")
print("1. App Store Connect Web Interface:")
print("   - Go to https://appstoreconnect.apple.com")
print("   - Select your app (CleanOrga)")
print("   - Go to 'App Information' in the left sidebar")
print("   - Scroll down to find the App Icon section")
print("   - Upload the 1024x1024 PNG icon")
print("")
print("2. Or include it in your next app build:")
print("   - The icon is already in Resources/AppIcon/appicon.png")
print("   - It will be included in the next IPA upload")
print("="*60)

print(f"\nIcon file location: {icon_path}")
