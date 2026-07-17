using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SkiaSharp.Views.Maui.Controls.Hosting;
#if ANDROID
using Plugin.Firebase.Core;
using Plugin.Firebase.Core.Platforms.Android;
#elif IOS
using Plugin.Firebase.Core;
using Plugin.Firebase.Core.Platforms.iOS;
#endif

namespace CleanOrgaCleaner;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.RegisterFirebaseServices()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Catch unhandled exceptions to prevent crashes
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"[FATAL] Unhandled: {e.ExceptionObject}");
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"[FATAL] Unobserved task: {e.Exception}");
			e.SetObserved();
		};

		return builder.Build();
	}

	/// <summary>
	/// Initialisiert Firebase (Core) plattformabhängig. Auf Android beim
	/// Activity-OnCreate, auf iOS beim FinishedLaunching. In try/catch gekapselt,
	/// damit eine fehlende Firebase-Konfiguration (z.B. iOS ohne
	/// GoogleService-Info.plist) die App nicht abstürzen lässt.
	/// </summary>
	private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
	{
		builder.ConfigureLifecycleEvents(events =>
		{
#if ANDROID
			events.AddAndroid(android => android.OnCreate((activity, _) =>
			{
				try { CrossFirebase.Initialize(activity); }
				catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Firebase] Android-Init übersprungen: {ex.Message}"); }
			}));
#elif IOS
			events.AddiOS(ios => ios.FinishedLaunching((app, launchOptions) =>
			{
				try
				{
					CrossFirebase.Initialize();
					// iOS-FCM-Setup: verbindet den APNs-Token mit Firebase, damit
					// GetTokenAsync() ein FCM-Token liefert. Fehlte bisher -> iOS
					// registrierte NIE ein Push-Token (0 iOS-Tokens am Server).
					Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.Initialize();
				}
				catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Firebase] iOS-Init übersprungen: {ex.Message}"); }
				return false;
			}));
#endif
		});
		return builder;
	}
}
