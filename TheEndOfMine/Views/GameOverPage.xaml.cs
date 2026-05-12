namespace TheEndOfMine.Views;

using TheEndOfMine.Data;
using TheEndOfMine.Models;
using TheEndOfMine.Services;

public partial class GameOverPage : ContentPage
{
    private readonly SaveService _saveService = new();
    private GameState? _dailyCheckpoint;
    private bool _hasLoadedCheckpoint;

    public GameOverPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hasLoadedCheckpoint) return;

        _hasLoadedCheckpoint = true;
        _dailyCheckpoint = await _saveService.LoadDailyCheckpointAsync();

        var canRespawn = _dailyCheckpoint is { Difficulty: not Difficulty.Hard } &&
            _dailyCheckpoint.Survivor.HP > 0;

        RespawnButton.IsVisible = canRespawn;
        if (canRespawn)
        {
            RespawnButton.Text = $"กลับไป 00:00 วันที่ {_dailyCheckpoint!.DayCount}";
            DeathDetailLabel.Text = "โหลดกลับไปยัง daily checkpoint ตอนเริ่มวันนั้น ค่าสถานะและของในกระเป๋าจะย้อนกลับตามเซฟ";
        }
        else
        {
            DeathDetailLabel.Text = "โหมด Hard หรือไม่พบ daily checkpoint จึงกลับไปเกิดใหม่ไม่ได้";
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
