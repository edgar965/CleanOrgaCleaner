import jwt
import time
import requests
import json

# App Store Connect API Credentials
KEY_ID = "W9C6VQH2Z7"
ISSUER_ID = "69a6de92-b5cb-47e3-e053-5b8c7c11a4d1"
PRIVATE_KEY = """-----BEGIN PRIVATE KEY-----
MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQgi6wWm5QmriFv47Qp
hn5FGpzjl5501zVohV4DA34kfaygCgYIKoZIzj0DAQehRANCAAQFBVe9PpQrowde
hpPf3GMka9IGBOh9L5OWsIHw/HlBdZPjJlJOBI+vwLrTMF9PGOvfcqKR8lFUGJ+U
bTF5bi+W
-----END PRIVATE KEY-----"""

DE_LOC_ID = "a842fd9e-539c-4f54-ab34-047b622b89c1"
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

# Fetch German localization from App Store
print("Fetching German texts from App Store Connect...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{DE_LOC_ID}",
    headers=headers
)
de_data = response.json()
de_attrs = de_data["data"]["attributes"]

print("\n=== GERMAN (de-DE) ===")
print(f"Description:\n{de_attrs.get('description', 'N/A')}\n")
print(f"Keywords: {de_attrs.get('keywords', 'N/A')}")
print(f"Promotional Text: {de_attrs.get('promotionalText', 'N/A')}")

# Fetch English localization
print("\n\nFetching English texts from App Store Connect...")
response = requests.get(
    f"https://api.appstoreconnect.apple.com/v1/appStoreVersionLocalizations/{EN_LOC_ID}",
    headers=headers
)
en_data = response.json()
en_attrs = en_data["data"]["attributes"]

print("\n=== ENGLISH (en-US) ===")
print(f"Description:\n{en_attrs.get('description', 'N/A')}\n")
print(f"Keywords: {en_attrs.get('keywords', 'N/A')}")
print(f"Promotional Text: {en_attrs.get('promotionalText', 'N/A')}")

# Save texts for Play Store
playstore_texts = {
    "de": {
        "title": "CleanOrga - Putzplan App",
        "short_description": de_attrs.get('promotionalText', ''),
        "full_description": de_attrs.get('description', ''),
        "keywords": de_attrs.get('keywords', '')
    },
    "en": {
        "title": "CleanOrga - Cleaning Schedule",
        "short_description": en_attrs.get('promotionalText', ''),
        "full_description": en_attrs.get('description', ''),
        "keywords": en_attrs.get('keywords', '')
    }
}

# Save to JSON file for reference
with open("D:/Daten/CleanOrga/CleanOrgaCleaner/playstore/store_listing.json", "w", encoding="utf-8") as f:
    json.dump(playstore_texts, f, indent=2, ensure_ascii=False)

print("\n\nTexts saved to playstore/store_listing.json")

# Create Play Store listing files in the format expected by fastlane/play console
import os

# German listing
de_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/playstore/listings/de-DE"
os.makedirs(de_dir, exist_ok=True)

with open(f"{de_dir}/title.txt", "w", encoding="utf-8") as f:
    f.write(playstore_texts["de"]["title"])

with open(f"{de_dir}/short_description.txt", "w", encoding="utf-8") as f:
    f.write(playstore_texts["de"]["short_description"])

with open(f"{de_dir}/full_description.txt", "w", encoding="utf-8") as f:
    f.write(playstore_texts["de"]["full_description"])

print(f"Created German listing files in {de_dir}")

# English listing
en_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/playstore/listings/en-US"
os.makedirs(en_dir, exist_ok=True)

with open(f"{en_dir}/title.txt", "w", encoding="utf-8") as f:
    f.write(playstore_texts["en"]["title"])

with open(f"{en_dir}/short_description.txt", "w", encoding="utf-8") as f:
    f.write(playstore_texts["en"]["short_description"])

with open(f"{en_dir}/full_description.txt", "w", encoding="utf-8") as f:
    f.write(playstore_texts["en"]["full_description"])

print(f"Created English listing files in {en_dir}")

print("\n\nDone! Play Store listing files created.")
print("\nTo update Play Store, you can:")
print("1. Manually copy texts from playstore/listings/ to Google Play Console")
print("2. Or use fastlane supply to automate the upload")
