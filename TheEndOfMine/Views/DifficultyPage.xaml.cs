using System;
using Microsoft.Maui.Controls;
using TheEndOfMine.Models;
using TheEndOfMine.Data;

namespace TheEndOfMine.Views;

public partial class DifficultyPage : ContentPage
{
    public DifficultyPage()
    {
        InitializeComponent();
    }

    private void OnEasyTapped(object sender, EventArgs e) => SaveAndNavigate(Difficulty.Easy);
    private void OnNormalTapped(object sender, EventArgs e) => SaveAndNavigate(Difficulty.Normal);
    private void OnHardTapped(object sender, EventArgs e) => SaveAndNavigate(Difficulty.Hard);

    private async void SaveAndNavigate(Difficulty selectedDifficulty)
    {
        var db = new GameDatabase();
        var (surv, state, inv) = await db.LoadAsync();
        
        if (state == null) state = new GameState();
        state.Difficulty = selectedDifficulty;

        // บันทึกข้อมูล
        await db.SaveAsync(surv ?? new Survivor(), state, inv ?? new Inventory());

        // เปลี่ยนหน้า
        try
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch
        {
            await Navigation.PushAsync(new TheEndOfMine.MainPage());
        }
    }
}