using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using TheEndOfMine.Models;

namespace TheEndOfMine.Views;

public partial class IntroPage : ContentPage
{
    private Gender _selected = Gender.Female; // เริ่มต้นที่ผู้หญิง

    public IntroPage()
    {
        InitializeComponent();
        StartBlinkingAnimation();

        // โหลดรูปผู้หญิงรอไว้หลังม่าน
        BackgroundImage.Source = "select_char_female.png";
    }

    private void StartBlinkingAnimation()
    {
        var animation = new Animation(v => TapToStartLabel.Opacity = v, 1, 0);
        animation.Commit(this, "BlinkingAnim", length: 1000, repeat: () => true);
    }

    private async void OnTapToStartClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;

        // 1. หยุด Animation กระพริบ
        this.AbortAnimation("BlinkingAnim");

        // 2. หน่วงเวลา 300ms ตามที่ต้องการ
        await Task.Delay(300);

        // 3. บังคับให้ระบบโหลดหน้าตัวละครหญิง (ใช้ Fade สั้นๆ เพื่อกระตุ้นการ Render)
        await ChangeCharacterWithFade(Gender.Female);

        // 4. เปิดม่านหน้า Loading ออก
        await TitleLayer.FadeTo(0, 500, Easing.CubicOut);

        TitleLayer.IsVisible = false;
        TitleLayer.InputTransparent = true;
    }

    // ฟังก์ชันคลิกเลือกตัวละครชาย
    private async void OnMaleClicked(object sender, EventArgs e)
    {
        if (_selected == Gender.Male) return;
        await ChangeCharacterWithFade(Gender.Male);
    }

    // ฟังก์ชันคลิกเลือกตัวละครหญิง
    private async void OnFemaleClicked(object sender, EventArgs e)
    {
        if (_selected == Gender.Female) return;
        await ChangeCharacterWithFade(Gender.Female);
    }

    // ฟังก์ชันทำ Fade สลับรูป
    private async Task ChangeCharacterWithFade(Gender newGender)
    {
        FadeOverlay.InputTransparent = false;
        await FadeOverlay.FadeTo(1, 300, Easing.CubicIn);

        _selected = newGender;
        BackgroundImage.Source = (_selected == Gender.Male) ? "select_char_male.png" : "select_char_female.png";

        await Task.Delay(100);
        await FadeOverlay.FadeTo(0, 300, Easing.CubicOut);
        FadeOverlay.InputTransparent = true;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Name required", "Please enter a survivor name.", "OK");
            return;
        }

        // 1. สร้างข้อมูลตัวละครใหม่และดึงค่าเพศ _selected
        var survivor = new Survivor { Name = name, Gender = _selected };

        // 2. สร้างสถานะเกมเริ่มต้น
        var state = new GameState
        {
            Difficulty = Difficulty.Normal,
            Status = GameStatus.Running,
            DayCount = 1,
            GameMinute = 8 * 60 // 08:00 AM
        };
        var inv = new Inventory();

        // 🚨 3. แก้ตรงนี้! เปลี่ยนจาก SaveService มาใช้ GameDatabase 🚨
        var db = new TheEndOfMine.Data.GameDatabase();
        await db.SaveAsync(survivor, state, inv);

        // 4. ไปหน้าเลือกความยาก
        await Navigation.PushAsync(new DifficultyPage());
    }
}