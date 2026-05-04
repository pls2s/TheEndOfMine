namespace TheEndOfMine;

using TheEndOfMine.ViewModels;
using TheEndOfMine.Data;
using TheEndOfMine.Models;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly GameDatabase _db;
    private int _selectedRestHours = 0;

    public MainPage()
    {
        InitializeComponent();

        _db = new GameDatabase();
        _vm = new MainViewModel();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. ทำให้แน่ใจว่าหน้าต่าง Loading ปรากฏอยู่และทึบแสง 100%
        LoadingOverlay.IsVisible = true;
        LoadingOverlay.Opacity = 1;

        // 2. โหลดข้อมูล Database 
        var (survivor, state, inventory) = await _db.LoadAsync();

        if (survivor != null && _vm != null)
        {
            string videoFile = survivor.Gender == Gender.Male ? "main_page_male.mp4" : "main_page_female.mp4";

            var htmlSource = new HtmlWebViewSource
            {
                Html = $@"
            <html>
            <head>
                <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
            </head>
            <body style='margin:0; padding:0; background-color:black; overflow:hidden;'>
                <video autoplay loop muted playsinline style='width:100vw; height:100vh; object-fit:cover; pointer-events:none;'>
                    <source src='file:///android_asset/{videoFile}' type='video/mp4'>
                </video>
            </body>
            </html>"
            };

            BgWebView.Source = htmlSource;
        }

        // 3. หน่วงเวลาจำลองให้วิดีโอใน WebView ได้บัฟเฟอร์ภาพขึ้นมาก่อน (ประมาณ 1.5 - 2 วินาที)
        // การใช้ Task.Delay จะทำให้แอปไม่ค้าง และหน้า Loading ยังขยับอยู่
        await Task.Delay(2000);

        // 4. เฟดหน้า Loading ออกอย่างนุ่มนวล (ใช้เวลา 800 มิลลิวินาที)
        await LoadingOverlay.FadeTo(0, 800, Easing.CubicOut);

        // 5. ปิดการแสดงผลเพื่อไม่ให้ขวางการทัชสกรีนบนปุ่มต่างๆ
        LoadingOverlay.IsVisible = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm?.Dispose();
    }
    private void OnRest4HClicked(object sender, EventArgs e)
    {
        if (_selectedRestHours == 4)
        {
            // กดซ้ำครั้งที่ 2 -> ยืนยันการนอน 4 ชั่วโมง
            ConfirmRest(4);
        }
        else
        {
            // กดครั้งแรก -> ไฮไลท์ปุ่ม 4H (เปลี่ยนเป็นสีทอง)
            _selectedRestHours = 4;
            Btn4H.BackgroundColor = Color.FromArgb("#63E5FF");
            Btn4H.TextColor = Colors.Black;

            // รีเซ็ตปุ่ม 8H ให้กลับเป็นปกติ (เผื่อผู้เล่นเปลี่ยนใจ)
            Btn8H.BackgroundColor = Color.FromArgb("#88000000");
            Btn8H.TextColor = Colors.White;
        }
    }

    private void OnRest8HClicked(object sender, EventArgs e)
    {
        if (_selectedRestHours == 8)
        {
            // กดซ้ำครั้งที่ 2 -> ยืนยันการนอน 8 ชั่วโมง
            ConfirmRest(8);
        }
        else
        {
            // กดครั้งแรก -> ไฮไลท์ปุ่ม 8H (เปลี่ยนเป็นสีทอง)
            _selectedRestHours = 8;
            Btn8H.BackgroundColor = Color.FromArgb("#63E5FF");
            Btn8H.TextColor = Colors.Black;

            // รีเซ็ตปุ่ม 4H ให้กลับเป็นปกติ
            Btn4H.BackgroundColor = Color.FromArgb("#88000000");
            Btn4H.TextColor = Colors.White;
        }
    }

    private void ConfirmRest(int hours)
    {
        // 1. คืนค่า UI ให้กลับเป็นสถานะปกติ (เผื่อใช้ในรอบต่อไป)
        _selectedRestHours = 0;
        Btn4H.BackgroundColor = Color.FromArgb("#88000000");
        Btn4H.TextColor = Colors.White;
        Btn8H.BackgroundColor = Color.FromArgb("#88000000");
        Btn8H.TextColor = Colors.White;

        // 2. ส่งค่าไปให้ ViewModel
        if (_vm != null)
        {
            // กำหนด Index ให้ตรงกับระบบเดิม (สมมติ 0 คือ 4H, 1 คือ 8H)
            _vm.SelectedRestIndex = hours == 4 ? 0 : 1;

            // สั่ง Execute Command การพักผ่อน
            if (_vm.RestCommand?.CanExecute(null) == true)
            {
                _vm.RestCommand.Execute(null);
            }
        }
    }
}