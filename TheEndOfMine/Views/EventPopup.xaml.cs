using TheEndOfMine.Models;

namespace TheEndOfMine.Views;

/// <summary>
/// หน้า EventPopup — popup แสดงเหตุการณ์สุ่มเมื่อกด GO OUTSIDE
///
/// วิธีใช้ (จากฝั่งผู้เรียก):
///   var popup = new EventPopup(gameEvent, survivorName);
///   await Navigation.PushModalAsync(popup, animated: false);
///   var choice = await popup.Result;   // รอผู้เล่นกดเลือก, คืน null ถ้าปิดเอง
///
/// องค์ประกอบบนการ์ด (card.png):
///   1. TitleLabel       — ชื่อ event
///   2. EventImage       — รูปประกอบเหตุการณ์
///   3. DescriptionLabel — คำบรรยาย
/// ปุ่ม 2 ปุ่มด้านล่าง (btn.png) ข้อความตั้งจาก ev.Choices[0..1].Text
/// </summary>
public partial class EventPopup : ContentPage
{
    // ใช้ TaskCompletionSource เพื่อให้ caller สามารถ await ผลลัพธ์ได้
    private readonly TaskCompletionSource<EventChoice?> _resultTcs = new();

    private GameEvent? _event;

    // ค่า default ใช้เมื่อ ev.ImagePath ว่าง (กันรูปหาย)
    private const string DefaultEventImage = "icon_game.png";

    // ค่า animation ของปุ่ม (กดแล้วจะหดเล็กน้อยแล้วคืนตามที่ต้องการ)
    private const double PressScale = 0.93;
    private const uint   PressDurationMs = 80;

    public EventPopup()
    {
        InitializeComponent();
    }

    /// <summary>สร้าง popup พร้อมข้อมูล event ที่จะแสดง</summary>
    /// <param name="ev">ข้อมูลเหตุการณ์ (จาก EventService)</param>
    /// <param name="survivorName">ชื่อตัวละคร (ใช้แทน [SURVIVOR] ใน description)</param>
    public EventPopup(GameEvent ev, string survivorName = "") : this()
    {
        Bind(ev, survivorName);
    }

    /// <summary>Task ที่ caller รอผลลัพธ์การเลือกของผู้เล่น</summary>
    public Task<EventChoice?> Result => _resultTcs.Task;

    /// <summary>เปลี่ยนข้อมูลที่แสดงบน popup ภายหลังก็ได้</summary>
    public void Bind(GameEvent ev, string survivorName = "")
    {
        _event = ev;

        TitleLabel.Text = ev.Title;
        DescriptionLabel.Text = string.IsNullOrEmpty(survivorName)
            ? ev.Description
            : ev.Description.Replace("[SURVIVOR]", survivorName);

        EventImage.Source = string.IsNullOrWhiteSpace(ev.ImagePath)
            ? DefaultEventImage
            : ev.ImagePath;

        // ปุ่มที่ 1
        if (ev.Choices.Count >= 1)
        {
            Choice1Label.Text = ev.Choices[0].Text;
            Choice1Btn.IsVisible = true;
        }
        else
        {
            Choice1Btn.IsVisible = false;
        }

        // ปุ่มที่ 2
        if (ev.Choices.Count >= 2)
        {
            Choice2Label.Text = ev.Choices[1].Text;
            Choice2Btn.IsVisible = true;
        }
        else
        {
            Choice2Btn.IsVisible = false;
        }
    }

    // ---- ปุ่มเลือก ----

    private async void OnChoice1Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is View v) await PressAnimation(v);
        await SelectChoiceAsync(0);
    }

    private async void OnChoice2Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is View v) await PressAnimation(v);
        await SelectChoiceAsync(1);
    }

    // animation ตอนกดปุ่ม: หดเล็กน้อย → คืนสู่ขนาดเดิม
    private static async Task PressAnimation(View view)
    {
        await view.ScaleTo(PressScale, PressDurationMs, Easing.CubicIn);
        await view.ScaleTo(1.0,        PressDurationMs, Easing.CubicOut);
    }

    private async Task SelectChoiceAsync(int index)
    {
        if (_event == null || index < 0 || index >= _event.Choices.Count) return;

        var choice = _event.Choices[index];
        _resultTcs.TrySetResult(choice);

        // ปิด popup กลับไปหน้าเดิมทันที (ไม่ใส่ animation จะได้ไม่กระตุก)
        await Navigation.PopModalAsync(animated: false);
    }

    // กดปุ่ม Back ของระบบ → ไม่ปิด เพราะต้องเลือกก่อน
    protected override bool OnBackButtonPressed() => true;

    // ถ้า popup ถูกปิดด้วยวิธีอื่น (เช่นโดน pop จากภายนอก) ให้คืน null
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _resultTcs.TrySetResult(null);
    }
}
