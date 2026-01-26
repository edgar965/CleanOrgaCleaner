using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace CleanOrgaCleaner;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register audio service
		builder.Services.AddSingleton(AudioManager.Current);

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
}
