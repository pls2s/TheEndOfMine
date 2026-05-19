using Microsoft.Maui.Controls;

namespace TheEndOfMine.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            await Task.Delay(3000);

            if (Application.Current?.Windows.FirstOrDefault()?.Page is Shell)
            {
                await Shell.Current.GoToAsync("//intro");
                return;
            }

            await Navigation.PushAsync(new IntroPage(), false);
        }
        catch (Exception ex)
        {
            App.LogStartupException("SplashPage.OnAppearing", ex);

            try
            {
                var window = Application.Current?.Windows.FirstOrDefault();
                if (window != null)
                    window.Page = new NavigationPage(new IntroPage());
            }
            catch (Exception nestedEx)
            {
                App.LogStartupException("SplashPage.Recover", nestedEx);
                // Keep the splash visible rather than crashing on startup.
            }
        }
    }
}
