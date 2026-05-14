namespace TheEndOfMine.Views;

using TheEndOfMine.Data;
using TheEndOfMine.Models;
using TheEndOfMine.Services;

public partial class GameOverPage : ContentPage
{
    private readonly SaveService _saveService = new();
    private readonly GameState? _gameOverState;
    private GameState? _dailyCheckpoint;
    private bool _hasLoadedCheckpoint;

    public GameOverPage()
    {
        InitializeComponent();
    }

    public GameOverPage(GameState? gameOverState) : this()
    {
        _gameOverState = gameOverState;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hasLoadedCheckpoint) return;

        _hasLoadedCheckpoint = true;
        _dailyCheckpoint = await _saveService.LoadDailyCheckpointAsync();
        ApplyGameOverSummary(_gameOverState);

        var canRespawn = _gameOverState?.IsStoryEnding != true &&
            _dailyCheckpoint is { Difficulty: not Difficulty.Hard } &&
            _dailyCheckpoint.Survivor.HP > 0;

        RespawnButton.IsVisible = canRespawn;
        if (canRespawn)
        {
            RespawnButton.Text = $"กลับไป 00:00 วันที่ {_dailyCheckpoint!.DayCount}";
            DeathDetailLabel.Text += "\n\nกลับไปยัง daily checkpoint ตอนเริ่มวันนั้น ค่าสถานะและของในกระเป๋าจะย้อนกลับตามเซฟ";
        }
        else if (_gameOverState?.IsStoryEnding == true)
        {
            DeathDetailLabel.Text += "\n\nเรื่องราวจบแล้ว สามารถกลับหน้าเริ่มเกมเพื่อเริ่มรอบใหม่";
        }
        else if (_gameOverState?.Difficulty == Difficulty.Hard)
        {
            DeathDetailLabel.Text += "\n\nโหมด Hard ไม่สามารถกลับไปเกิดใหม่ได้";
        }
        else
        {
            DeathDetailLabel.Text += "\n\nไม่พบ daily checkpoint จึงกลับไปเกิดใหม่ไม่ได้";
        }
    }

    private void ApplyGameOverSummary(GameState? state)
    {
        if (state == null)
        {
            ResultTitleLabel.Text = "การเดินทางครั้งนี้จบลงแล้ว";
            CauseFrame.IsVisible = true;
            CauseTitleLabel.Text = "สาเหตุ";
            CauseLabel.Text = "ไม่พบข้อมูลเหตุการณ์สุดท้าย";
            DeathDetailLabel.Text = "ถ้าต้องการเริ่มใหม่ ให้กลับไปเลือกตัวละครอีกครั้ง";
            return;
        }

        ResultTitleLabel.Text = string.IsNullOrWhiteSpace(state.GameOverTitle)
            ? state.IsStoryEnding ? "รอดไปถึงปลายทาง" : "เสียชีวิต"
            : state.GameOverTitle;

        CauseTitleLabel.Text = state.IsStoryEnding ? "ฉากจบ" : "สาเหตุการตาย";
        CauseFrame.IsVisible = !state.IsStoryEnding;
        CauseLabel.Text = state.IsStoryEnding
            ? "ใช้ฉากจบที่สอดคล้องกับเส้นเรื่องและเพศของตัวละคร"
            : string.IsNullOrWhiteSpace(state.DeathCause)
                ? "บาดแผลและความเสี่ยงที่สะสมระหว่างทางทำให้ร่างกายรับไม่ไหว"
                : state.DeathCause;

        DeathDetailLabel.Text = string.IsNullOrWhiteSpace(state.GameOverDetail)
            ? "ถ้าต้องการเริ่มใหม่ ให้กลับไปเลือกตัวละครอีกครั้ง"
            : state.GameOverDetail;

        if (state.IsStoryEnding && !string.IsNullOrWhiteSpace(state.EndingImagePath))
        {
            EndingImage.Source = state.EndingImagePath;
            EndingImageFrame.IsVisible = true;
        }
    }

    private async void OnRespawnClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();

        var checkpoint = await _saveService.LoadDailyCheckpointAsync() ?? _dailyCheckpoint;
        if (checkpoint == null || checkpoint.Difficulty == Difficulty.Hard)
            return;

        checkpoint.Status = GameStatus.Running;
        checkpoint.GameMinute = 0;
        checkpoint.Survivor.HP = Math.Max(checkpoint.Survivor.HP, 1f);
        _saveService.SaveCheckpoint(checkpoint);

        var db = new GameDatabase();
        await db.SaveAsync(checkpoint.Survivor, checkpoint, checkpoint.Survivor.Inventory);

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new NavigationPage(new TheEndOfMine.MainPage());
        else
            await Navigation.PushAsync(new TheEndOfMine.MainPage());
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();

        _saveService.DeleteAllSaves();
        new GameDatabase().DeleteAllSaves();

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            window.Page = new AppShell();
            return;
        }

        await Navigation.PopToRootAsync();
    }
}
