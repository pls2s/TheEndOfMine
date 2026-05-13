namespace TheEndOfMine;

using TheEndOfMine.Data;
using TheEndOfMine.Models;

public partial class AppShell : Shell
{
	private bool _hasCheckedSavedGame;

	public AppShell()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_hasCheckedSavedGame)
			return;

		_hasCheckedSavedGame = true;

		var (_, state, _) = await new GameDatabase().LoadAsync();
		if (state?.Status is not (GameStatus.Running or GameStatus.Paused))
			return;

		await GoToAsync("//MainPage");
	}
}
