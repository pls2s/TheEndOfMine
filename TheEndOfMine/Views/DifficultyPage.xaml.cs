using System;
using Microsoft.Maui.Controls;
using TheEndOfMine.Models;
using TheEndOfMine.Data;
using TheEndOfMine.Services;

namespace TheEndOfMine.Views;

public partial class DifficultyPage : ContentPage
{
    private const double ArtworkWidth = 572;
    private const double ArtworkHeight = 944;

    private static readonly Rect EasyButtonArtworkBounds = new(123, 348, 326, 70);
    private static readonly Rect NormalButtonArtworkBounds = new(124, 509, 324, 66);
    private static readonly Rect HardButtonArtworkBounds = new(124, 664, 324, 66);
    private static readonly Rect ConfirmButtonArtworkBounds = new(113, 804, 346, 88);

    private static readonly Rect EasyGlowArtworkBounds = new(128, 354, 316, 58);
    private static readonly Rect NormalGlowArtworkBounds = new(129, 515, 314, 54);
    private static readonly Rect HardGlowArtworkBounds = new(129, 670, 314, 54);

    private Difficulty? _selectedDifficulty = null;
    private CancellationTokenSource? _glowCts;
    private double _lastLayoutWidth;
    private double _lastLayoutHeight;

    public DifficultyPage()
    {
        InitializeComponent();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        if (Math.Abs(width - _lastLayoutWidth) < 0.5 &&
            Math.Abs(height - _lastLayoutHeight) < 0.5)
        {
            return;
        }

        _lastLayoutWidth = width;
        _lastLayoutHeight = height;
        ApplyDifficultyOverlayLayout(width, height);
    }

    private void OnEasyTapped(object sender, EventArgs e) => SelectDifficulty(Difficulty.Easy);
    private void OnNormalTapped(object sender, EventArgs e) => SelectDifficulty(Difficulty.Normal);
    private void OnHardTapped(object sender, EventArgs e) => SelectDifficulty(Difficulty.Hard);

    private void SelectDifficulty(Difficulty difficulty)
    {
        AudioFeedbackService.PlayButtonTap();

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

    private void ApplyDifficultyOverlayLayout(double width, double height)
    {
        SetOverlayBounds(EasyButton, EasyButtonArtworkBounds, width, height);
        SetOverlayBounds(NormalButton, NormalButtonArtworkBounds, width, height);
        SetOverlayBounds(HardButton, HardButtonArtworkBounds, width, height);
        SetOverlayBounds(ConfirmBtn, ConfirmButtonArtworkBounds, width, height);

        SetOverlayBounds(GlowEasy, EasyGlowArtworkBounds, width, height);
        SetOverlayBounds(GlowNormal, NormalGlowArtworkBounds, width, height);
        SetOverlayBounds(GlowHard, HardGlowArtworkBounds, width, height);
    }

    private static void SetOverlayBounds(BindableObject element, Rect artworkBounds, double width, double height)
    {
        AbsoluteLayout.SetLayoutBounds(element, MapArtworkBounds(artworkBounds, width, height));
    }

    private static Rect MapArtworkBounds(Rect artworkBounds, double width, double height)
    {
        var scale = Math.Max(width / ArtworkWidth, height / ArtworkHeight);
        var renderedWidth = ArtworkWidth * scale;
        var renderedHeight = ArtworkHeight * scale;
        var offsetX = (width - renderedWidth) / 2;
        var offsetY = (height - renderedHeight) / 2;

        return new Rect(
            offsetX + artworkBounds.X * scale,
            offsetY + artworkBounds.Y * scale,
            artworkBounds.Width * scale,
            artworkBounds.Height * scale);
    }

    private async void OnConfirmTapped(object sender, EventArgs e)
    {
        if (_selectedDifficulty == null) return;

        AudioFeedbackService.PlayButtonTap();

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
