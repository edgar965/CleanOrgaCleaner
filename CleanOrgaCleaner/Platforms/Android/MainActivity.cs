using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Activity;
using AndroidX.Core.View;

namespace CleanOrgaCleaner;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Enable edge-to-edge display for Android 15+ (SDK 35)
        // This ensures backward compatibility and proper inset handling
        EdgeToEdge.Enable(this);

        // Handle window insets for proper content positioning
        if (Window != null)
        {
            WindowCompat.SetDecorFitsSystemWindows(Window, false);
        }
    }

    protected override void OnDestroy()
    {
        try
        {
            base.OnDestroy();
        }
        catch (System.ObjectDisposedException)
        {
            // Known MAUI bug: Entry fields fire focus change during destroy
            // when ServiceProvider is already disposed. Safe to ignore.
        }
    }
}
