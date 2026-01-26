using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace CleanOrgaCleaner;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
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
