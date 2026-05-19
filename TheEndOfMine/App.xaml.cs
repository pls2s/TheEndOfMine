namespace TheEndOfMine;

using System.Diagnostics;
using TheEndOfMine.Services;
using TheEndOfMine.Views;

public partial class App : Application
{
	public App()
	{
		RegisterGlobalExceptionLogging();

		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			LogStartupException("App.InitializeComponent", ex);
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Page rootPage;

		try
		{
			rootPage = CreateStartupPage();
		}
		catch (Exception ex)
		{
			LogStartupException("CreateStartupPage", ex);
			rootPage = CreateStartupFallbackPage();
		}

		var window = new Window(rootPage);
		window.Created += (_, _) => AudioFeedbackService.StartBackgroundLoop();
		window.Resumed += (_, _) => AudioFeedbackService.StartBackgroundLoop();
		window.Stopped += (_, _) => AudioFeedbackService.PauseBackgroundLoop();
		window.Destroying += (_, _) => AudioFeedbackService.StopBackgroundLoop();
		return window;
	}

	private static Page CreateStartupPage()
	{
		var splashPage = new SplashPage();
		NavigationPage.SetHasNavigationBar(splashPage, false);

		return new NavigationPage(splashPage)
		{
			BarBackgroundColor = Colors.Black,
			BarTextColor = Colors.White
		};
	}

	private static Page CreateStartupFallbackPage()
	{
		var fallbackPage = new ContentPage
		{
			BackgroundColor = Colors.Black
		};
		NavigationPage.SetHasNavigationBar(fallbackPage, false);

		return new NavigationPage(fallbackPage)
		{
			BarBackgroundColor = Colors.Black,
			BarTextColor = Colors.White
		};
	}

	private static void RegisterGlobalExceptionLogging()
	{
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			if (args.ExceptionObject is Exception ex)
				LogStartupException("UnhandledException", ex);
		};

		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			LogStartupException("UnobservedTaskException", args.Exception);
			args.SetObserved();
		};
	}

	internal static void LogStartupException(string context, Exception ex)
	{
		try
		{
			Debug.WriteLine($"[{context}] {ex}");
		}
		catch
		{
			// Diagnostics must never block the app from opening.
		}
	}
}
