namespace TheEndOfMine;

using Microsoft.Maui.Storage;
using TheEndOfMine.ViewModels;
using TheEndOfMine.Data;
using TheEndOfMine.Models;
using TheEndOfMine.Services;
using TheEndOfMine.Views;

public partial class MainPage : ContentPage
{
    private const string TutorialSeenPreferenceKey = "main_tutorial_seen";

    private readonly MainViewModel _vm;
    private readonly GameDatabase _db;
    private readonly TutorialDimDrawable _tutorialDimDrawable = new();
    private bool _hasLoadedScene;
    private bool _isStopPopupAnimating;
    private int _tutorialStepIndex;
    private TutorialStep[] _tutorialSteps = [];

    public MainPage()
    {
        InitializeComponent();
        TutorialDimCanvas.Drawable = _tutorialDimDrawable;

        _db = new GameDatabase();
        _vm = new MainViewModel();
        BindingContext = _vm;
        UpdateSoundToggleButton();
    }

    protected override async void OnAppearing()
    {
        try
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
                {
                    LoadingChapterImage.Source = state.CurrentChapterImagePath;
                }
            }

            await SetLoadingProgressAsync(0.34, "กำลังจัดฉาก chapter ปัจจุบัน");

            BgImage.Source = GetMainPageBackgroundPath(survivor ?? state?.Survivor);

            await SetLoadingProgressAsync(0.68, "กำลังเตรียมฉากพื้นหลัง");

            await Task.Delay(650);
            await SetLoadingProgressAsync(0.9, "กำลังเปิดพื้นที่ปลอดภัย");
            await Task.Delay(250);
            await SetLoadingProgressAsync(1, "พร้อมแล้ว");

            // 3. เฟดหน้า Loading ออกอย่างนุ่มนวล
            await LoadingOverlay.FadeTo(0, 420, Easing.CubicOut);

            // 4. ปิดการแสดงผลเพื่อไม่ให้ขวางการทัชสกรีนบนปุ่มต่างๆ
            LoadingOverlay.IsVisible = false;
            await ShowTutorialIfNeededAsync();
        }
        catch (Exception ex)
        {
            App.LogStartupException("MainPage.OnAppearing", ex);

            LoadingDetailLabel.Text = "โหลดข้อมูลไม่สำเร็จ แต่ยังเข้าเกมต่อได้";
            LoadingPercentLabel.Text = "100%";
            LoadingProgress.Progress = 1;
            LoadingOverlay.IsVisible = false;
        }
    }

    private async Task SetLoadingProgressAsync(double progress, string detail)
    {
        progress = Math.Clamp(progress, 0, 1);
        LoadingDetailLabel.Text = detail;
        LoadingPercentLabel.Text = $"{(int)Math.Round(progress * 100)}%";
        await LoadingProgress.ProgressTo(progress, 260, Easing.CubicOut);
    }

    private static string GetMainPageBackgroundPath(Survivor? survivor)
    {
        return survivor?.Gender == Gender.Male
            ? "story/main/main_page_male_bg.png"
            : "story/main/main_page_female_bg.png";
    }

    private async Task ShowTutorialIfNeededAsync()
    {
        if (Preferences.Get(TutorialSeenPreferenceKey, false))
            return;

        _tutorialSteps =
        [
            new TutorialStep(StatusPanel, "ค่าสถานะ", "แถบนี้คือ HP, อาหาร, น้ำ, ความเหนื่อย, เสียง และการติดเชื้อ ถ้าค่าบางอย่างแย่ลง การออกสำรวจจะเสี่ยงขึ้น"),
            new TutorialStep(GoOutsideButton, "ออกสำรวจ", "กด GO OUTSIDE เพื่อเดินหน้าเนื้อเรื่องและเจอเหตุการณ์ใหม่ ตัวเลือกบางอย่างจะดีขึ้นถ้าคุณมีไอเทมที่เหมาะ"),
            new TutorialStep(RestPanel, "พัก / นอน", "REST และ SLEEP ลดความเหนื่อย แต่เวลาจะเดินต่อ อาหารและน้ำอาจลดลงตามเวลาที่ผ่านไป"),
            new TutorialStep(InventoryButton, "กระเป๋า", "เปิด INVENTORY เพื่อดูของ ใช้อาหาร น้ำ ยา หรือดูไอเทมที่ช่วยปลดล็อก/ลดผลเสียของตัวเลือกใน event"),
            new TutorialStep(MenuButton, "เมนูสามขีด", "เปิดเมนูนี้เพื่อ SAVE GAME, เปิด/ปิดเสียง หรือหยุดเกม เกมมี Auto Save อยู่แล้ว แต่กด save เองได้")
        ];

        _tutorialStepIndex = 0;
        TutorialOverlay.IsVisible = true;
        TutorialOverlay.InputTransparent = false;
        TutorialOverlay.Opacity = 0;
        await ShowTutorialStepAsync();
        await TutorialOverlay.FadeTo(1, 180, Easing.CubicOut);
    }

    private async void OnTutorialNextClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        if (_tutorialStepIndex >= _tutorialSteps.Length - 1)
        {
            await FinishTutorialAsync();
            return;
        }

        _tutorialStepIndex++;
        await ShowTutorialStepAsync();
    }

    private async void OnTutorialSkipClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await FinishTutorialAsync();
    }

    private async Task ShowTutorialStepAsync()
    {
        if (_tutorialSteps.Length == 0)
            return;

        var step = _tutorialSteps[_tutorialStepIndex];
        TutorialStepLabel.Text = $"{_tutorialStepIndex + 1}/{_tutorialSteps.Length}";
        TutorialTitleLabel.Text = step.Title;
        TutorialBodyLabel.Text = step.Body;
        TutorialNextButton.Text = _tutorialStepIndex >= _tutorialSteps.Length - 1 ? "เริ่มเล่น" : "ถัดไป";

        await WaitForTutorialLayoutAsync(step.Target);
        MoveTutorialHighlight(step.Target);
        await TutorialHighlight.ScaleTo(1.04, 140, Easing.CubicOut);
        await TutorialHighlight.ScaleTo(1, 110, Easing.CubicIn);
    }

    private async Task WaitForTutorialLayoutAsync(VisualElement target)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var root = (VisualElement)Content;
            if (root.Width > 0 && root.Height > 0 && target.Width > 0 && target.Height > 0)
                return;

            await Task.Delay(50);
        }
    }

    private void MoveTutorialHighlight(VisualElement target)
    {
        var root = (VisualElement)Content;
        var x = target.X;
        var y = target.Y;
        var parent = target.Parent as Element;

        while (parent is VisualElement visualParent && parent != root)
        {
            x += visualParent.X;
            y += visualParent.Y;
            parent = visualParent.Parent;
        }

        const double padding = 8;
        var width = Math.Max(48, target.Width + padding * 2);
        var height = Math.Max(38, target.Height + padding * 2);
        var left = Math.Max(6, x - padding);
        var top = Math.Max(6, y - padding);

        TutorialHighlight.WidthRequest = width;
        TutorialHighlight.HeightRequest = height;
        TutorialHighlight.TranslationX = left;
        TutorialHighlight.TranslationY = top;

        MoveTutorialDimLayer(root, left, top, width, height);

        TutorialPointerLabel.TranslationX = Math.Min(Math.Max(6, left), Math.Max(6, root.Width - 72));
        TutorialPointerLabel.TranslationY = top > 36 ? top - 34 : top + height + 8;

        MoveTutorialCardAwayFromTarget(root, top, height);
    }

    private void MoveTutorialDimLayer(VisualElement root, double left, double top, double width, double height)
    {
        var rootWidth = Math.Max(0, root.Width);
        var rootHeight = Math.Max(0, root.Height);

        _tutorialDimDrawable.SetHole(rootWidth, rootHeight, left, top, width, height);
        TutorialDimCanvas.Invalidate();
    }

    private void MoveTutorialCardAwayFromTarget(VisualElement root, double targetTop, double targetHeight)
    {
        const double margin = 20;
        const double bottomSafeArea = 28;
        var cardHeight = TutorialCard.Height > 0 ? TutorialCard.Height : 220;
        var targetCenterY = targetTop + targetHeight / 2;
        var showBelow = targetCenterY < root.Height * 0.46;

        var cardTop = showBelow
            ? targetTop + targetHeight + 22
            : targetTop - cardHeight - 22;

        if (cardTop < margin)
            cardTop = margin;

        var maxTop = Math.Max(margin, root.Height - cardHeight - bottomSafeArea);
        if (cardTop > maxTop)
            cardTop = maxTop;

        TutorialCard.TranslationY = cardTop;
    }

    private async Task FinishTutorialAsync()
    {
        Preferences.Set(TutorialSeenPreferenceKey, true);
        await TutorialOverlay.FadeTo(0, 130, Easing.CubicIn);
        TutorialOverlay.IsVisible = false;
        TutorialOverlay.InputTransparent = true;
    }

    private sealed record TutorialStep(VisualElement Target, string Title, string Body);

    private sealed class TutorialDimDrawable : Microsoft.Maui.Graphics.IDrawable
    {
        private readonly Microsoft.Maui.Graphics.Color _dimColor =
            Microsoft.Maui.Graphics.Color.FromArgb("#99000000");

        private float _left;
        private float _top;
        private float _right;
        private float _bottom;
        private bool _hasHole;

        public void SetHole(double viewWidth, double viewHeight, double left, double top, double width, double height)
        {
            if (viewWidth <= 0 || viewHeight <= 0 || width <= 0 || height <= 0)
            {
                _hasHole = false;
                return;
            }

            _hasHole = true;
            _left = (float)Math.Clamp(left, 0, viewWidth);
            _top = (float)Math.Clamp(top, 0, viewHeight);
            _right = (float)Math.Clamp(left + width, _left, viewWidth);
            _bottom = (float)Math.Clamp(top + height, _top, viewHeight);
        }

        public void Draw(Microsoft.Maui.Graphics.ICanvas canvas, Microsoft.Maui.Graphics.RectF dirtyRect)
        {
            if (!_hasHole)
                return;

            canvas.FillColor = _dimColor;

            var fullWidth = dirtyRect.Width;
            var fullHeight = dirtyRect.Height;
            var right = Math.Min(_right, fullWidth);
            var bottom = Math.Min(_bottom, fullHeight);

            canvas.FillRectangle(0, 0, fullWidth, _top);
            canvas.FillRectangle(0, bottom, fullWidth, Math.Max(0, fullHeight - bottom));
            canvas.FillRectangle(0, _top, _left, Math.Max(0, bottom - _top));
            canvas.FillRectangle(right, _top, Math.Max(0, fullWidth - right), Math.Max(0, bottom - _top));
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _vm.SaveGameAsync();
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
        await HideGameMenuAsync();
        await Navigation.PushAsync(new InventoryPage());
    }

    private async void OnGameMenuClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();

        if (GameMenuPanel.IsVisible)
            await HideGameMenuAsync();
        else
            await ShowGameMenuAsync();
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await _vm.ToggleStopAsync();
        await HideGameMenuAsync();
        await ShowStopStatusPopupAsync(_vm.IsPaused);
    }

    private async void OnStopPopupCloseClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await HideStopStatusPopupAsync();
    }

    private async void OnStopPopupResumeClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        if (_vm.IsPaused)
            await _vm.ToggleStopAsync();

        await HideStopStatusPopupAsync();
    }

    private async void OnSaveGameClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        SaveGameButton.IsEnabled = false;
        await _vm.SaveGameAsync();
        SaveGameButton.Text = "SAVED";
        await SaveGameButton.ScaleTo(0.94, 70, Easing.CubicIn);
        await SaveGameButton.ScaleTo(1, 90, Easing.CubicOut);
        await Task.Delay(650);
        SaveGameButton.Text = "SAVE GAME";
        SaveGameButton.IsEnabled = true;
        await HideGameMenuAsync();
    }

    private async void OnSoundToggleClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        AudioFeedbackService.ToggleMuted();
        UpdateSoundToggleButton();
        await HideGameMenuAsync();
    }

    private async void OnDebugEndingClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await HideGameMenuAsync();
        _vm.ForceStoryEndingForDebug();
    }

    private async void OnDebugTutorialClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await HideGameMenuAsync();
        Preferences.Remove(TutorialSeenPreferenceKey);
        await ShowTutorialIfNeededAsync();
    }

    private void UpdateSoundToggleButton()
    {
        SoundToggleButton.Text = AudioFeedbackService.IsMuted ? "SOUND OFF" : "SOUND ON";
    }

    private void OnGoOutsideClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        _ = HideGameMenuAsync();
    }

    private async Task ShowGameMenuAsync()
    {
        GameMenuPanel.IsVisible = true;
        GameMenuPanel.Opacity = 0;
        GameMenuPanel.TranslationY = -8;

        await Task.WhenAll(
            GameMenuPanel.FadeTo(1, 120, Easing.CubicOut),
            GameMenuPanel.TranslateTo(0, 0, 120, Easing.CubicOut));
    }

    private async Task HideGameMenuAsync()
    {
        if (!GameMenuPanel.IsVisible)
            return;

        await Task.WhenAll(
            GameMenuPanel.FadeTo(0, 90, Easing.CubicIn),
            GameMenuPanel.TranslateTo(0, -8, 90, Easing.CubicIn));

        GameMenuPanel.IsVisible = false;
    }

    private async Task ShowStopStatusPopupAsync(bool isPaused)
    {
        if (_isStopPopupAnimating)
            return;

        StopStatusTitleLabel.Text = isPaused ? "หยุดเกมแล้ว" : "กลับมาเล่นต่อแล้ว";
        StopStatusDetailLabel.Text = isPaused
            ? "เวลาในเกมถูกพักไว้ชั่วคราว คุณยังสามารถบันทึกเกมหรือกดเล่นต่อได้"
            : "เวลาในเกมกลับมาเดินต่อแล้ว";
        StopPopupResumeButton.IsVisible = isPaused;
        StopPopupResumeButton.IsEnabled = isPaused;
        StopPopupCloseButton.Text = isPaused ? "ปิด" : "ตกลง";

        StopStatusOverlay.IsVisible = true;
        StopStatusOverlay.InputTransparent = false;
        StopStatusOverlay.Opacity = 0;
        StopStatusCard.Scale = 0.94;

        _isStopPopupAnimating = true;
        try
        {
            await Task.WhenAll(
                StopStatusOverlay.FadeTo(1, 140, Easing.CubicOut),
                StopStatusCard.ScaleTo(1, 140, Easing.CubicOut));
        }
        finally
        {
            _isStopPopupAnimating = false;
        }

        if (!isPaused)
        {
            await Task.Delay(700);
            await HideStopStatusPopupAsync();
        }
    }

    private async Task HideStopStatusPopupAsync()
    {
        if (!StopStatusOverlay.IsVisible || _isStopPopupAnimating)
            return;

        _isStopPopupAnimating = true;
        try
        {
            await Task.WhenAll(
                StopStatusOverlay.FadeTo(0, 110, Easing.CubicIn),
                StopStatusCard.ScaleTo(0.96, 110, Easing.CubicIn));
        }
        finally
        {
            StopStatusOverlay.IsVisible = false;
            StopStatusOverlay.InputTransparent = true;
            _isStopPopupAnimating = false;
        }
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
