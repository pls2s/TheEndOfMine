using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using TheEndOfMine.Models;
using TheEndOfMine.Services;

namespace TheEndOfMine.Views;

public partial class IntroPage : ContentPage
{
    private Gender _selected = Gender.Female; // เริ่มต้นที่ผู้หญิง

    public IntroPage()
    {
        InitializeComponent();
        StartBlinkingAnimation();

        // โหลดรูปผู้หญิงรอไว้หลังม่าน
        BackgroundImage.Source = GetCharacterImagePath(_selected, "select");
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
        BackgroundImage.Source = GetCharacterImagePath(_selected, "select");

        await Task.Delay(100);
        await FadeOverlay.FadeTo(0, 300, Easing.CubicOut);
        FadeOverlay.InputTransparent = true;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        var startButton = sender as Button;
        if (startButton != null) startButton.IsEnabled = false;

        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("ต้องใส่ชื่อ", "กรุณาใส่ชื่อผู้รอดชีวิต", "ตกลง");
            if (startButton != null) startButton.IsEnabled = true;
            return;
        }

        try
        {
            // 1. สร้างข้อมูลตัวละครใหม่และดึงค่าเพศ _selected
            var survivor = new Survivor { Name = name, Gender = _selected };
            var contentGenerator = new LlmGameContentService();
            var generatedContent = await contentGenerator.GenerateNewGameAsync(survivor);

            var inv = new Inventory();
            foreach (var item in generatedContent.StartingItems)
                inv.AddItem(item);

            survivor.Inventory = inv;

            // 2. สร้างสถานะเกมเริ่มต้นพร้อม story ที่ generate แล้ว
            var state = new GameState
            {
                Survivor = survivor,
                Difficulty = Difficulty.Normal,
                Status = GameStatus.Running,
                DayCount = 1,
                GameMinute = 8 * 60, // 08:00 AM
                EventIndex = 0,
                StoryTitle = generatedContent.StoryTitle,
                CurrentChapterTitle = generatedContent.StoryTitle,
                CurrentChapterAlias = generatedContent.ChapterAlias ?? string.Empty,
                CurrentChapterImagePath = generatedContent.ChapterImagePath,
                CurrentChapter = 1,
                MaxChapters = 4,
                EventsPerChapter = 8,
                StorySource = generatedContent.UsedRemoteLlm ? "llm" : "local_fallback",
                GeneratedEvents = generatedContent.Events
            };

            // 3. บันทึกข้อมูลรอบใหม่ลง GameDatabase
            var db = new TheEndOfMine.Data.GameDatabase();
            await db.SaveAsync(survivor, state, inv);

            if (!generatedContent.UsedRemoteLlm)
            {
                await DisplayAlert(
                    "ยังไม่ได้ตั้งค่า GPT",
                    "ยังไม่พบ OPENAI_API_KEY หรือ LLM_API_KEY ระบบจึงสร้างเนื้อเรื่อง fallback แบบสุ่มให้ก่อน",
                    "ตกลง");
            }

            // 4. ไปหน้าเลือกความยาก
            await Navigation.PushAsync(new DifficultyPage());
        }
        finally
        {
            if (startButton != null) startButton.IsEnabled = true;
        }
    }

    private static string GetCharacterImagePath(Gender gender, string kind)
    {
        var folder = gender == Gender.Male ? "story/male" : "story/female";
        var prefix = gender == Gender.Male ? "male" : "female";

        return kind switch
        {
            "select" => $"{folder}/{prefix}_select_char.png",
            "loading" => $"{folder}/{prefix}_loading_screen_portrait.png",
            "difficulty" => $"{folder}/{prefix}_difficulty_select.png",
            "confirm" => $"{folder}/{prefix}_confirm_button.png",
            _ => $"{folder}/{prefix}_select_char.png"
        };
    }
}
