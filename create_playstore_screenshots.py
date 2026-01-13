from PIL import Image
import os

# Source directory with Android screenshots
src_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/screenshots_en"

# Output directory for Play Store
out_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/playstore/screenshots"
os.makedirs(out_dir, exist_ok=True)

# Play Store recommended size for phone screenshots
# Minimum: 320px, Maximum: 3840px
# Recommended: 1080x1920 (9:16 aspect ratio)
TARGET_WIDTH = 1080
TARGET_HEIGHT = 1920

# Screenshots to process
screenshots = [
    ("02_today.png", "01_today.png"),
    ("04_detail.png", "02_detail.png"),
    ("05_chat.png", "03_chat.png"),
    ("06_chat_detail.png", "04_chat_detail.png"),
]

print("Creating Play Store screenshots...")
print(f"Target size: {TARGET_WIDTH}x{TARGET_HEIGHT}")

for src_name, dst_name in screenshots:
    src_path = os.path.join(src_dir, src_name)

    if not os.path.exists(src_path):
        print(f"  Skipping {src_name} - not found")
        continue

    img = Image.open(src_path)
    print(f"\n  Processing {src_name} ({img.size[0]}x{img.size[1]})")

    # Calculate resize to fit target while maintaining aspect ratio
    img_ratio = img.width / img.height
    target_ratio = TARGET_WIDTH / TARGET_HEIGHT

    if img_ratio > target_ratio:
        # Image is wider - fit by height, then crop width
        new_height = TARGET_HEIGHT
        new_width = int(TARGET_HEIGHT * img_ratio)
    else:
        # Image is taller - fit by width, then crop height
        new_width = TARGET_WIDTH
        new_height = int(TARGET_WIDTH / img_ratio)

    # Resize
    resized = img.resize((new_width, new_height), Image.LANCZOS)

    # Center crop to exact target size
    left = (new_width - TARGET_WIDTH) // 2
    top = (new_height - TARGET_HEIGHT) // 2
    cropped = resized.crop((left, top, left + TARGET_WIDTH, top + TARGET_HEIGHT))

    # Save
    dst_path = os.path.join(out_dir, dst_name)
    cropped.save(dst_path, "PNG")
    print(f"  Created {dst_name} ({TARGET_WIDTH}x{TARGET_HEIGHT})")

print(f"\n\nPlay Store screenshots saved to: {out_dir}")
print("\nTo upload to Google Play Console:")
print("1. Go to https://play.google.com/console")
print("2. Select your app")
print("3. Go to 'Store presence' > 'Main store listing'")
print("4. Scroll to 'Phone screenshots'")
print("5. Upload the PNG files from the screenshots folder")
