namespace CleanOrgaCleaner.Services;

/// <summary>
/// Zentrales "Firebase ist initialisiert"-Flag.
///
/// Wird genau einmal gesetzt: iOS in AppDelegate.FinishedLaunching, Android im
/// OnCreate-Lifecycle - jeweils NUR wenn CrossFirebase.Initialize() ohne
/// Exception durchlief. JEDER Zugriff auf CrossFirebaseAuth / CrossFirebase-
/// Firestore / CrossFirebaseCloudMessaging muss vorher dieses Flag prüfen:
/// Auf iOS endet der Zugriff bei nicht konfigurierter Default-FirebaseApp in
/// einem nativen fatalError (SIGTRAP), den kein C#-try/catch abfängt - das war
/// der Login-Crash von App 1.74.
/// </summary>
public static class FirebaseStatus
{
    public static volatile bool Ready;
}
