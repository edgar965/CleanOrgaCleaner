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
#endif
			// iOS: Firebase-Init passiert NICHT hier, sondern direkt im
			// AppDelegate.FinishedLaunching (kanonische, deterministisch früheste
			// Stelle - VOR base.FinishedLaunching). Das Lifecycle-Event lief zu
			// spaet/uneindeutig, sodass die Default-FirebaseApp bei Auth-Zugriff
			// nicht konfiguriert war (SIGTRAP in Auth.auth()).
		});
		return builder;
	}
}
