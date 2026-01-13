from PIL import Image
import os

# Source logo
logo_path = "D:/Daten/CleanOrga/CleanorgaLogo.jpg"
output_dir = "D:/Daten/CleanOrga/CleanOrgaCleaner/appstore/icons"
os.makedirs(output_dir, exist_ok=True)

# Load the logo
img = Image.open(logo_path)
print(f"Original size: {img.size}")

# Convert to RGB if necessary (for PNG output)
if img.mode != 'RGB':
    img = img.convert('RGB')

# App Store requires 1024x1024
# Play Store requires 512x512

# Since the image is not square, we need to crop or pad it
# The image appears to be taller than wide, so let's center crop to square
width, height = img.size
min_dim = min(width, height)

# Center crop to square
left = (width - min_dim) // 2
top = (height - min_dim) // 2
right = left + min_dim
bottom = top + min_dim

img_square = img.crop((left, top, right, bottom))
print(f"Cropped to square: {img_square.size}")

# Create App Store icon (1024x1024)
appstore_icon = img_square.resize((1024, 1024), Image.LANCZOS)
appstore_path = os.path.join(output_dir, "appstore_icon_1024.png")
appstore_icon.save(appstore_path, "PNG")
print(f"Created App Store icon: {appstore_path}")

# Create Play Store icon (512x512)
playstore_icon = img_square.resize((512, 512), Image.LANCZOS)
playstore_path = os.path.join(output_dir, "playstore_icon_512.png")
playstore_icon.save(playstore_path, "PNG")
print(f"Created Play Store icon: {playstore_path}")

# Also create various sizes needed for iOS app
ios_sizes = [
    (180, "Icon-180.png"),    # iPhone @3x
    (120, "Icon-120.png"),    # iPhone @2x
    (167, "Icon-167.png"),    # iPad Pro @2x
    (152, "Icon-152.png"),    # iPad @2x
    (76, "Icon-76.png"),      # iPad @1x
    (40, "Icon-40.png"),      # Spotlight @1x
    (80, "Icon-80.png"),      # Spotlight @2x
    (120, "Icon-120-spotlight.png"),  # Spotlight @3x
    (29, "Icon-29.png"),      # Settings @1x
    (58, "Icon-58.png"),      # Settings @2x
    (87, "Icon-87.png"),      # Settings @3x
]

ios_dir = os.path.join(output_dir, "ios")
os.makedirs(ios_dir, exist_ok=True)

for size, filename in ios_sizes:
    icon = img_square.resize((size, size), Image.LANCZOS)
    icon.save(os.path.join(ios_dir, filename), "PNG")
    print(f"Created iOS icon: {filename} ({size}x{size})")

# Android sizes
android_sizes = [
    (48, "mipmap-mdpi"),
    (72, "mipmap-hdpi"),
    (96, "mipmap-xhdpi"),
    (144, "mipmap-xxhdpi"),
    (192, "mipmap-xxxhdpi"),
]

android_dir = os.path.join(output_dir, "android")
os.makedirs(android_dir, exist_ok=True)

for size, folder in android_sizes:
    folder_path = os.path.join(android_dir, folder)
    os.makedirs(folder_path, exist_ok=True)
    icon = img_square.resize((size, size), Image.LANCZOS)
    icon.save(os.path.join(folder_path, "ic_launcher.png"), "PNG")
    print(f"Created Android icon: {folder}/ic_launcher.png ({size}x{size})")

print("\nAll icons created successfully!")
print(f"\nApp Store icon: {appstore_path}")
print(f"Play Store icon: {playstore_path}")
