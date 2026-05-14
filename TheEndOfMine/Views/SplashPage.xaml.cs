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
        base.OnAppearing();

        await Task.Delay(3000);

        await Shell.Current.GoToAsync("//intro");
    }
}
