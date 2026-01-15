using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace CleanOrgaCleaner.Helpers;

/// <summary>
/// Helper class for image compression and resizing
/// </summary>
public static class ImageHelper
{
    private const int MaxDimension = 2000;
    private const float JpegQuality = 0.8f;

    /// <summary>
    /// Compresses and resizes an image to max 2000 pixels on the longest side
    /// </summary>
    public static async Task<byte[]> CompressImageAsync(byte[] imageBytes)
    {
        try
        {
            using var inputStream = new MemoryStream(imageBytes);

#if IOS || MACCATALYST
            return await CompressImageiOSAsync(inputStream);
#elif ANDROID
            return await CompressImageAndroidAsync(inputStream);
#else
            // Fallback: return original if platform not supported
            return imageBytes;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageHelper] Compression failed: {ex.Message}");
            // Return original on error
            return imageBytes;
        }
    }

#if IOS || MACCATALYST
    private static Task<byte[]> CompressImageiOSAsync(Stream inputStream)
    {
        return Task.Run(() =>
        {
            using var uiImage = UIKit.UIImage.LoadFromData(Foundation.NSData.FromStream(inputStream));
            if (uiImage == null)
                throw new Exception("Could not load image");

            var originalWidth = uiImage.Size.Width;
            var originalHeight = uiImage.Size.Height;

            // Calculate new dimensions
            var (newWidth, newHeight) = CalculateNewDimensions((int)originalWidth, (int)originalHeight);

            // Resize if needed
            UIKit.UIImage resizedImage;
            if (newWidth != (int)originalWidth || newHeight != (int)originalHeight)
            {
                var newSize = new CoreGraphics.CGSize(newWidth, newHeight);
                UIKit.UIGraphics.BeginImageContextWithOptions(newSize, false, 1.0f);
                uiImage.Draw(new CoreGraphics.CGRect(0, 0, newWidth, newHeight));
                resizedImage = UIKit.UIGraphics.GetImageFromCurrentImageContext();
                UIKit.UIGraphics.EndImageContext();
            }
            else
            {
                resizedImage = uiImage;
            }

            // Compress to JPEG
            var jpegData = resizedImage.AsJPEG((nfloat)JpegQuality);
            if (jpegData == null)
                throw new Exception("Could not compress image");

            var bytes = new byte[jpegData.Length];
            System.Runtime.InteropServices.Marshal.Copy(jpegData.Bytes, bytes, 0, (int)jpegData.Length);

            System.Diagnostics.Debug.WriteLine($"[ImageHelper] iOS: {originalWidth}x{originalHeight} -> {newWidth}x{newHeight}, Size: {bytes.Length / 1024}KB");

            return bytes;
        });
    }
#endif

#if ANDROID
    private static Task<byte[]> CompressImageAndroidAsync(Stream inputStream)
    {
        return Task.Run(() =>
        {
            using var bitmap = Android.Graphics.BitmapFactory.DecodeStream(inputStream);
            if (bitmap == null)
                throw new Exception("Could not load image");

            var originalWidth = bitmap.Width;
            var originalHeight = bitmap.Height;

            // Calculate new dimensions
            var (newWidth, newHeight) = CalculateNewDimensions(originalWidth, originalHeight);

            // Resize if needed
            Android.Graphics.Bitmap resizedBitmap;
            if (newWidth != originalWidth || newHeight != originalHeight)
            {
                resizedBitmap = Android.Graphics.Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true);
            }
            else
            {
                resizedBitmap = bitmap;
            }

            // Compress to JPEG
            using var outputStream = new MemoryStream();
            resizedBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, (int)(JpegQuality * 100), outputStream);

            var bytes = outputStream.ToArray();

            System.Diagnostics.Debug.WriteLine($"[ImageHelper] Android: {originalWidth}x{originalHeight} -> {newWidth}x{newHeight}, Size: {bytes.Length / 1024}KB");

            if (resizedBitmap != bitmap)
                resizedBitmap.Dispose();

            return bytes;
        });
    }
#endif

    private static (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight)
    {
        if (originalWidth <= MaxDimension && originalHeight <= MaxDimension)
        {
            return (originalWidth, originalHeight);
        }

        double ratio;
        if (originalWidth > originalHeight)
        {
            ratio = (double)MaxDimension / originalWidth;
        }
        else
        {
            ratio = (double)MaxDimension / originalHeight;
        }

        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }
}
