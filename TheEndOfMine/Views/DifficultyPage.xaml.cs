using System;
using Microsoft.Maui.Controls;
using TheEndOfMine.Models;
using TheEndOfMine.Data;

namespace TheEndOfMine.Views;

public partial class DifficultyPage : ContentPage
{
    private Difficulty? _selectedDifficulty = null;
    private CancellationTokenSource? _glowCts;

    public DifficultyPage()
    {
        InitializeComponent();
    }

    private void OnEasyTapped(object sender, EventArgs e) => SelectDifficulty(Difficulty.Easy);
    private void OnNormalTapped(object sender, EventArgs e) => SelectDifficulty(Difficulty.Normal);
    private void OnHardTapped(object sender, EventArgs e) => SelectDifficulty(Difficulty.Hard);

    private void SelectDifficulty(Difficulty difficulty)
    {
        _selectedDifficulty = difficulty;
        ConfirmBtn.IsVisible = true;

        // ซ่อนกรอบทองทุกปุ่มก่อน
        GlowEasy.Opacity = 0;
        GlowNormal.Opacity = 0;
        GlowHard.Opacity = 0;

        // เลือกกรอบของปุ่มที่กด
        Border activeGlow = difficulty switch
        {
            Difficulty.Easy => GlowEasy,
            Difficulty.Normal => GlowNormal,
            Difficulty.Hard => GlowHard,
            _ => GlowEasy
        };

        // หยุด animation เก่า แล้วเริ่มใหม่
        _glowCts?.Cancel();
        _glowCts = new CancellationTokenSource();
        StartGlowPulse(activeGlow, _glowCts.Token);
    }

    private async void StartGlowPulse(Border glow, CancellationToken token)
    {
        await glow.FadeTo(0.9, 300, Easing.SinIn);
    }

    private async void OnConfirmTapped(object sender, EventArgs e)
    {
        if (_selectedDifficulty == null) return;

        await ConfirmBtn.ScaleTo(0.93, 80, Easing.CubicIn);
        await ConfirmBtn.ScaleTo(1.0, 80, Easing.CubicOut);

        var db = new GameDatabase();
        var (surv, state, inv) = await db.LoadAsync();

        if (state == null) state = new GameState();
        state.Difficulty = _selectedDifficulty.Value;

        await db.SaveAsync(surv ?? new Survivor(), state, inv ?? new Inventory());

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