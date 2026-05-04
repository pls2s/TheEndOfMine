namespace TheEndOfMine;

using TheEndOfMine.ViewModels;
using TheEndOfMine.Data;
using TheEndOfMine.Models;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly GameDatabase _db;

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

        var (survivor, state, inventory) = await _db.LoadAsync();

        if (survivor != null && _vm != null)
        {
            // 1. เลือกชื่อไฟล์ MP4 ตามเพศ
            // (ต้องมั่นใจว่าไฟล์ main_page_male.mp4 และ main_page_female.mp4 อยู่ในโฟลเดอร์ Resources/Raw นะครับ)
            string videoFile = survivor.Gender == Gender.Male ? "main_page_male.mp4" : "main_page_female.mp4";

            // 2. สร้างโค้ด HTML เพื่อสั่งให้เล่นวิดีโอแบบเต็มจอ วนลูป และปิดเสียง
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

            // 3. สั่งให้ WebView แสดงผลวิดีโอ
            BgWebView.Source = htmlSource;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm?.Dispose();
    }
}