namespace TheEndOfMine;

using TheEndOfMine.ViewModels;
using TheEndOfMine.Data;
using TheEndOfMine.Models;
using TheEndOfMine.Services;
using TheEndOfMine.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly GameDatabase _db;
    private bool _hasLoadedScene;

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

        if (_hasLoadedScene)
        {
            await _vm.ReloadCheckpointAsync();
            return;
        }

        _hasLoadedScene = true;

        // 1. ทำให้แน่ใจว่าหน้าต่าง Loading ปรากฏอยู่และทึบแสง 100%
        LoadingOverlay.IsVisible = true;
        LoadingOverlay.Opacity = 1;
        LoadingProgress.Progress = 0;
        LoadingPercentLabel.Text = "0%";
        await SetLoadingProgressAsync(0.12, "กำลังอ่านข้อมูลผู้รอดชีวิต");

        // 2. โหลดข้อมูล Database
        var (survivor, state, inventory) = await _db.LoadAsync();

        if (state != null)
        {
            LoadingChapterLabel.Text = string.IsNullOrWhiteSpace(state.CurrentChapterTitle)
                ? $"Chapter {state.CurrentChapter}"
                : state.CurrentChapterTitle;
            LoadingTitleLabel.Text = $"ENTERING CHAPTER {state.CurrentChapter}";

            if (!string.IsNullOrWhiteSpace(state.CurrentChapterImagePath))
                LoadingChapterImage.Source = state.CurrentChapterImagePath;
        }

        await SetLoadingProgressAsync(0.34, "กำลังจัดฉาก chapter ปัจจุบัน");

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

        await SetLoadingProgressAsync(0.68, "กำลังเตรียมวิดีโอพื้นหลัง");

        // 3. หน่วงเวลาจำลองให้วิดีโอใน WebView ได้บัฟเฟอร์ภาพขึ้นมาก่อน (ประมาณ 1.5 - 2 วินาที)
        // การใช้ Task.Delay จะทำให้แอปไม่ค้าง และหน้า Loading ยังขยับอยู่
        await Task.Delay(650);
        await SetLoadingProgressAsync(0.9, "กำลังเปิดพื้นที่ปลอดภัย");
        await Task.Delay(250);
        await SetLoadingProgressAsync(1, "พร้อมแล้ว");

        // 4. เฟดหน้า Loading ออกอย่างนุ่มนวล (ใช้เวลา 800 มิลลิวินาที)
        await LoadingOverlay.FadeTo(0, 420, Easing.CubicOut);

        // 5. ปิดการแสดงผลเพื่อไม่ให้ขวางการทัชสกรีนบนปุ่มต่างๆ
        LoadingOverlay.IsVisible = false;
    }

    private async Task SetLoadingProgressAsync(double progress, string detail)
    {
        progress = Math.Clamp(progress, 0, 1);
        LoadingDetailLabel.Text = detail;
        LoadingPercentLabel.Text = $"{(int)Math.Round(progress * 100)}%";
        await LoadingProgress.ProgressTo(progress, 260, Easing.CubicOut);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm?.Dispose();
    }
    private void OnRest4HClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        ConfirmRest(4);
    }

    private void OnRest8HClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        ConfirmRest(8);
    }

    private async void OnInventoryClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await Navigation.PushAsync(new InventoryPage());
    }

    private void OnGoOutsideClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
    }

    private void ConfirmRest(int hours)
    {
        if (_vm != null)
        {
            _vm.SelectedRestIndex = hours == 4 ? 0 : 1;

            if (_vm.RestCommand?.CanExecute(null) == true)
            {
                _vm.RestCommand.Execute(null);
            }
        }
    }
}
