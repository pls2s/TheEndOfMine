namespace TheEndOfMine;

using TheEndOfMine.Services;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		window.Created += (_, _) => AudioFeedbackService.StartBackgroundLoop();
		window.Resumed += (_, _) => AudioFeedbackService.StartBackgroundLoop();
		window.Stopped += (_, _) => AudioFeedbackService.PauseBackgroundLoop();
		window.Destroying += (_, _) => AudioFeedbackService.StopBackgroundLoop();
		return window;
	}
}
