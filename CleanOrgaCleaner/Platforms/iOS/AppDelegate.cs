using Foundation;
using UIKit;
using CleanOrgaCleaner.Services;
using Plugin.Firebase.Core;
using Plugin.Firebase.Core.Platforms.iOS;

namespace CleanOrgaCleaner;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		// Firebase MUSS hier initialisiert werden - kanonische, früheste Stelle,
		// VOR base.FinishedLaunching. Nur so ist die Default-FirebaseApp
		// konfiguriert, bevor irgendein Firebase-API (Auth/Firestore/FCM)
		// angefasst wird. Das frühere Lifecycle-Event lief zu spät -> Auth.auth()
		// crashte (SIGTRAP "default app must be configured").
		string diag;
		try
		{
			CrossFirebase.Initialize();
			// FCM-Setup: verbindet den APNs-Token mit Firebase, damit
			// GetTokenAsync() ein Token liefert.
			Plugin.Firebase.CloudMessaging.FirebaseCloudMessagingImplementation.Initialize();
			diag = "OK";
		}
		catch (Exception ex)
		{
			diag = "EX " + ex.GetType().Name + ": " + ex.Message;
			System.Diagnostics.Debug.WriteLine($"[Firebase] iOS-Init FEHLER: {ex}");
		}

		// Diagnose an den Server (unauth /api/crash-report/), damit wir OHNE
		// Gerät sehen, ob die Firebase-Init auf iOS wirklich durchläuft.
		ApiService.WriteServerDiag("firebase-init-ios", diag);

		return base.FinishedLaunching(application, launchOptions);
	}
}
