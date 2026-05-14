using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using TheEndOfMine.Data;
using TheEndOfMine.Models;
using TheEndOfMine.Services;

namespace TheEndOfMine.Views;

public partial class IntroPage : ContentPage
{
    private Gender _selected = Gender.Female; // เริ่มต้นที่ผู้หญิง
    private readonly LightningStrikeDrawable _lightningDrawable = new();
    private bool _isStarting;
    private bool _isChangingCharacter;
    private TaskCompletionSource<bool>? _alertCompletion;
    private CancellationTokenSource? _startLoadingPulseCts;

    public IntroPage()
    {
        InitializeComponent();
        LightningCanvas.Drawable = _lightningDrawable;
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
        AudioFeedbackService.PlayButtonTap();

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
        if (_selected == Gender.Male || _isChangingCharacter) return;
        AudioFeedbackService.PlayButtonTap();
        await ChangeCharacterWithFade(Gender.Male);
    }

    // ฟังก์ชันคลิกเลือกตัวละครหญิง
    private async void OnFemaleClicked(object sender, EventArgs e)
    {
        if (_selected == Gender.Female || _isChangingCharacter) return;
        AudioFeedbackService.PlayButtonTap();
        await ChangeCharacterWithFade(Gender.Female);
    }

    // ฟังก์ชันทำ Fade สลับรูป
    private async Task ChangeCharacterWithFade(Gender newGender)
    {
        _isChangingCharacter = true;
        try
        {
            var nextSource = GetCharacterImagePath(newGender, "select");

            TransitionImage.Source = nextSource;
            TransitionImage.Opacity = 0;
            TransitionImage.IsVisible = true;

            var lightningTask = PlayLightningStrikeAsync(newGender);
            var fadeOutTask = BackgroundImage.FadeTo(0, 220, Easing.CubicIn);
            var fadeInTask = TransitionImage.FadeTo(1, 220, Easing.CubicOut);
            await Task.WhenAll(fadeOutTask, fadeInTask, lightningTask);

            _selected = newGender;
            BackgroundImage.Source = nextSource;
            BackgroundImage.Opacity = 1;

            TransitionImage.Opacity = 0;
            TransitionImage.IsVisible = false;
        }
        finally
        {
            _isChangingCharacter = false;
        }
    }

    private async Task PlayLightningStrikeAsync(Gender newGender)
    {
        _lightningDrawable.TargetX = newGender == Gender.Male ? 0.34f : 0.66f;
        _lightningDrawable.Regenerate();
        LightningCanvas.Invalidate();

        LightningOverlay.Opacity = 1;
        LightningFlash.Opacity = 0;

        await Task.WhenAll(
            LightningFlash.FadeTo(0.48, 45, Easing.CubicOut),
            LightningCanvas.FadeTo(1, 45, Easing.CubicOut));

        await Task.Delay(45);
        _lightningDrawable.Regenerate(branchOnly: true);
        LightningCanvas.Invalidate();

        await LightningFlash.FadeTo(0.1, 70, Easing.CubicIn);
        await Task.Delay(55);
        await Task.WhenAll(
            LightningFlash.FadeTo(0, 130, Easing.CubicIn),
            LightningOverlay.FadeTo(0, 170, Easing.CubicIn));

        LightningFlash.Opacity = 0;
        LightningOverlay.Opacity = 0;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (_isStarting) return;

        AudioFeedbackService.PlayButtonTap();

        var startButton = sender as Button;
        if (startButton != null) startButton.IsEnabled = false;

        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await ShowGameAlertAsync("ต้องใส่ชื่อ", "กรุณาใส่ชื่อผู้รอดชีวิต");
            if (startButton != null) startButton.IsEnabled = true;
            return;
        }

        try
        {
            _isStarting = true;
            await ShowStartLoadingAsync(GetCharacterImagePath(_selected, "loading"));
            await SetStartLoadingProgressAsync(0.08, "บันทึกชื่อผู้รอดชีวิต");

            // 1. สร้างข้อมูลตัวละครใหม่และดึงค่าเพศ _selected
            var survivor = new Survivor { Name = name, Gender = _selected };
            var contentGenerator = new LlmGameContentService();

            await SetStartLoadingProgressAsync(0.18, "กำลังสร้าง Chapter 1 และเหตุการณ์เริ่มต้น");
            StartStartLoadingPulse();
            var generatedContent = await contentGenerator.GenerateNewGameOpeningAsync(survivor);
            StopStartLoadingPulse();
            await SetStartLoadingProgressAsync(
                0.68,
                "สร้างบทแรกเสร็จแล้ว กำลังจัดฉาก",
                generatedContent.ChapterImagePath);

            await SetStartLoadingProgressAsync(0.78, "กำลังเตรียมไอเทมเริ่มต้น");
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
                GameMinute = 0,
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
            await SetStartLoadingProgressAsync(0.9, "กำลังบันทึกเกม");
            new SaveService().DeleteAllSaves();
            Preferences.Remove("main_tutorial_seen");
            var db = new GameDatabase();
            await db.SaveAsync(survivor, state, inv);
            QueueInitialChapterCompletion(state);

            if (!generatedContent.UsedRemoteLlm)
            {
                var reason = string.IsNullOrWhiteSpace(generatedContent.FallbackReason)
                    ? "ระบบเรียก LLM ไม่สำเร็จ จึงสร้างเนื้อเรื่อง fallback แบบสุ่มให้ก่อน"
                    : generatedContent.FallbackReason;

                await ShowGameAlertAsync(
                    "ใช้เนื้อเรื่อง fallback",
                    reason);
            }

            // 4. ไปหน้าเลือกความยาก
            await SetStartLoadingProgressAsync(1, "พร้อมเข้าสู่เหมืองสุดท้าย");
            await Task.Delay(200);
            await HideStartLoadingAsync();

            var difficultyPage = new DifficultyPage { Opacity = 0 };
            await Navigation.PushAsync(difficultyPage, false);
            await difficultyPage.FadeTo(1, 220, Easing.CubicOut);
        }
        catch
        {
            StopStartLoadingPulse();
            await HideStartLoadingAsync();
            await ShowGameAlertAsync("โหลดไม่สำเร็จ", "สร้างเกมใหม่ไม่สำเร็จ กรุณาลองอีกครั้ง");
        }
        finally
        {
            _isStarting = false;
            if (startButton != null) startButton.IsEnabled = true;
        }
    }

    private static void QueueInitialChapterCompletion(GameState state)
    {
        if (state.GeneratedEvents.Count >= state.EventsPerChapter)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var generator = new LlmGameContentService();
                var continuation = await generator.GenerateCurrentChapterContinuationAsync(state);
                if (continuation.Events.Count == 0)
                    return;

                var db = new GameDatabase();
                var (_, latestState, latestInventory) = await db.LoadAsync();
                if (latestState == null ||
                    latestState.CurrentChapter != state.CurrentChapter ||
                    latestState.GeneratedEvents.Count >= latestState.EventsPerChapter)
                {
                    return;
                }

                var added = GeneratedEventMergeService.AppendEvents(latestState, continuation.Events);
                if (added == 0)
                    return;

                latestState.StorySource = continuation.UsedRemoteLlm ? "llm" : "local_fallback";
                await db.SaveAsync(latestState.Survivor, latestState, latestInventory ?? latestState.Survivor.Inventory);
            }
            catch
            {
                // If background generation fails, GameEngine will finish the chapter on demand.
            }
        });
    }

    private async Task ShowStartLoadingAsync(string imageSource)
    {
        StartLoadingImage.Source = imageSource;
        StartLoadingTitleLabel.Text = "กำลังสร้างโลก";
        StartLoadingDetailLabel.Text = "กำลังเตรียมบทแรก";
        StartLoadingProgress.Progress = 0;
        StartLoadingPercentLabel.Text = "0%";
        StartLoadingOverlay.IsVisible = true;
        StartLoadingOverlay.InputTransparent = false;
        StartLoadingOverlay.Opacity = 0;
        await StartLoadingOverlay.FadeTo(1, 220, Easing.CubicOut);
    }

    private async Task SetStartLoadingProgressAsync(double progress, string detail, string? imageSource = null)
    {
        if (!string.IsNullOrWhiteSpace(imageSource))
            StartLoadingImage.Source = imageSource;

        progress = Math.Clamp(progress, 0, 1);
        StartLoadingDetailLabel.Text = detail;
        StartLoadingPercentLabel.Text = $"{(int)Math.Round(progress * 100)}%";
        await StartLoadingProgress.ProgressTo(progress, 260, Easing.CubicOut);
    }

    private void StartStartLoadingPulse()
    {
        StopStartLoadingPulse();

        _startLoadingPulseCts = new CancellationTokenSource();
        var token = _startLoadingPulseCts.Token;

        _ = Task.Run(async () =>
        {
            var details = new[]
            {
                "กำลังวางโครงเรื่องและฉากเปิด",
                "กำลังสร้างเหตุการณ์ให้ต่อเนื่องกัน",
                "กำลังเลือกไอเทมเริ่มต้นให้เข้ากับเรื่อง",
                "กำลังตรวจทางเลือกและผลลัพธ์",
                "กำลังจัดสมดุลค่าสถานะของผู้รอดชีวิต"
            };

            var index = 0;
            while (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var current = StartLoadingProgress.Progress;
                    if (current < 0.86)
                    {
                        var step = current < 0.54 ? 0.028 : 0.01;
                        var next = Math.Min(0.86, current + step);
                        StartLoadingPercentLabel.Text = $"{(int)Math.Round(next * 100)}%";
                        await StartLoadingProgress.ProgressTo(next, 420, Easing.CubicOut);
                    }

                    StartLoadingDetailLabel.Text = details[index % details.Length];
                });

                index++;

                try
                {
                    await Task.Delay(560, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopStartLoadingPulse()
    {
        if (_startLoadingPulseCts == null)
            return;

        _startLoadingPulseCts.Cancel();
        _startLoadingPulseCts.Dispose();
        _startLoadingPulseCts = null;
    }

    private async Task HideStartLoadingAsync()
    {
        if (!StartLoadingOverlay.IsVisible) return;

        await StartLoadingOverlay.FadeTo(0, 180, Easing.CubicIn);
        StartLoadingOverlay.IsVisible = false;
        StartLoadingOverlay.InputTransparent = true;
    }

    private async Task ShowGameAlertAsync(string title, string message)
    {
        if (_alertCompletion != null)
            return;

        _alertCompletion = new TaskCompletionSource<bool>();
        GameAlertTitleLabel.Text = title;
        GameAlertMessageLabel.Text = message;
        GameAlertOkButton.IsEnabled = true;
        GameAlertOverlay.IsVisible = true;
        GameAlertOverlay.InputTransparent = false;
        GameAlertOverlay.Opacity = 0;

        await GameAlertOverlay.FadeTo(1, 160, Easing.CubicOut);
        await _alertCompletion.Task;
    }

    private async void OnGameAlertOkClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();

        if (_alertCompletion == null)
            return;

        GameAlertOkButton.IsEnabled = false;
        await GameAlertOverlay.FadeTo(0, 120, Easing.CubicIn);
        GameAlertOverlay.IsVisible = false;
        GameAlertOverlay.InputTransparent = true;

        var completion = _alertCompletion;
        _alertCompletion = null;
        completion.TrySetResult(true);
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

    private sealed class LightningStrikeDrawable : IDrawable
    {
        private readonly Random _random = new();
        private readonly List<PointF> _mainBolt = new();
        private readonly List<(PointF From, PointF To)> _branches = new();

        public float TargetX { get; set; } = 0.5f;

        public void Regenerate(bool branchOnly = false)
        {
            if (!branchOnly || _mainBolt.Count == 0)
            {
                _mainBolt.Clear();
                var x = TargetX + RandomRange(-0.05f, 0.05f);
                var y = 0.05f;
                _mainBolt.Add(new PointF(x, y));

                for (var i = 1; i <= 7; i++)
                {
                    var progress = i / 7f;
                    x += RandomRange(-0.07f, 0.07f);
                    y = 0.05f + progress * 0.5f;
                    _mainBolt.Add(new PointF(Math.Clamp(x, 0.12f, 0.88f), y));
                }
            }

            _branches.Clear();
            foreach (var point in _mainBolt.Skip(1).Take(4))
            {
                var direction = _random.Next(0, 2) == 0 ? -1 : 1;
                var end = new PointF(
                    Math.Clamp(point.X + direction * RandomRange(0.07f, 0.16f), 0.05f, 0.95f),
                    Math.Clamp(point.Y + RandomRange(0.03f, 0.12f), 0.05f, 0.68f));
                _branches.Add((point, end));
            }
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_mainBolt.Count < 2)
                Regenerate();

            canvas.SaveState();
            canvas.Alpha = 0.88f;

            DrawBolt(canvas, dirtyRect, _mainBolt, Color.FromArgb("#E8FBFF"), 5.5f);
            DrawBolt(canvas, dirtyRect, _mainBolt, Color.FromArgb("#77D7FF"), 10f, 0.35f);

            foreach (var (from, to) in _branches)
            {
                var branch = new List<PointF> { from, MidPoint(from, to), to };
                DrawBolt(canvas, dirtyRect, branch, Color.FromArgb("#BDEFFF"), 2.8f, 0.78f);
            }

            canvas.RestoreState();
        }

        private void DrawBolt(ICanvas canvas, RectF bounds, IReadOnlyList<PointF> points, Color color, float strokeSize, float alpha = 1f)
        {
            canvas.StrokeColor = color;
            canvas.StrokeSize = strokeSize;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.Alpha = alpha;

            for (var i = 1; i < points.Count; i++)
            {
                var a = ToCanvasPoint(points[i - 1], bounds);
                var b = ToCanvasPoint(points[i], bounds);
                canvas.DrawLine(a.X, a.Y, b.X, b.Y);
            }
        }

        private PointF ToCanvasPoint(PointF point, RectF bounds)
        {
            return new PointF(bounds.X + point.X * bounds.Width, bounds.Y + point.Y * bounds.Height);
        }

        private PointF MidPoint(PointF from, PointF to)
        {
            return new PointF(
                (from.X + to.X) / 2f + RandomRange(-0.025f, 0.025f),
                (from.Y + to.Y) / 2f + RandomRange(-0.025f, 0.025f));
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
