using System;
using Microsoft.Maui.Controls;
using TheEndOfMine.Models;
using TheEndOfMine.Data;

namespace TheEndOfMine.Views;

public partial class DifficultyPage : ContentPage
{
    private Difficulty _selected = Difficulty.Normal;

    public DifficultyPage()
    {
        InitializeComponent();
    }

    // เมื่อกดที่โหมดต่างๆ ให้เลือกและเปิด Pop-up ทันที
    private void OnEasyTapped(object sender, EventArgs e) => OpenConfirmation(Difficulty.Easy);
    private void OnNormalTapped(object sender, EventArgs e) => OpenConfirmation(Difficulty.Normal);
    private void OnHardTapped(object sender, EventArgs e) => OpenConfirmation(Difficulty.Hard);

    private async void OpenConfirmation(Difficulty d)
    {
        _selected = d;
        DifficultyLabel.Text = $"{d.ToString().ToUpper()} MODE";

        if (d == Difficulty.Easy) DifficultyLabel.TextColor = Color.FromArgb("#7ACC7A");
        else if (d == Difficulty.Normal) DifficultyLabel.TextColor = Color.FromArgb("#D4A017");
        else if (d == Difficulty.Hard) DifficultyLabel.TextColor = Color.FromArgb("#CC4444");

        // 1. เปิดเลเยอร์ม่านดำก่อน (ไม่ต้องรอ)
        ConfirmPopup.IsVisible = true;
        ConfirmPopup.Opacity = 0;
        _ = ConfirmPopup.FadeTo(1, 200); // ทำม่านดำค่อยๆ โผล่มา

        // 2. สั่งให้ตัว Pop-up เลื่อนลงมาจากข้างบน
        // จาก -1000 กลับมาที่ตำแหน่ง 0 (กึ่งกลางจอ)
        await PopupFrame.TranslateTo(0, 0, 400, Easing.SpringOut);
    }

    private async void OnCancelPopupClicked(object sender, EventArgs e)
    {
        // 1. สั่งให้ Pop-up เลื่อนกลับขึ้นไปข้างบน
        await PopupFrame.TranslateTo(0, -1000, 300, Easing.CubicIn);

        // 2. ค่อยๆ จางม่านดำลง
        await ConfirmPopup.FadeTo(0, 200);

        // 3. ปิด IsVisible
        ConfirmPopup.IsVisible = false;
    }

    private async void OnAcceptPopupClicked(object sender, EventArgs e)
    {
        // เล่นแอนิเมชันขาออกก่อนปิด
        await PopupFrame.TranslateTo(0, -1000, 300, Easing.CubicIn);
        ConfirmPopup.IsVisible = false;

        // --- ส่วนของ Logic การ Save ข้อมูลเหมือนเดิมที่คุณเขียนไว้ ---
        var db = new GameDatabase();
        var (surv, state, inv) = await db.LoadAsync();
        if (state == null) state = new GameState();
        state.Difficulty = _selected;
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