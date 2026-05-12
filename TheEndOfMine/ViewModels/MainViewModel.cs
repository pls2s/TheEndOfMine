using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using TheEndOfMine.Models;
using TheEndOfMine.Services;
using TheEndOfMine.Data;
using TheEndOfMine.Views;

namespace TheEndOfMine.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly SaveService _saveService;
    private readonly EventService _eventService;
    private readonly DifficultyService _difficultyService;
    private readonly Command _goOutsideCommand;
    private GameEngine? _engine;
    private GameState? _currentState;
    private bool _isActionBusy;
    private bool _isEventPopupOpen;
    private bool _isChapterLoading;
    private double _chapterLoadingProgress;
    private string _chapterLoadingTitle = "ENTERING CHAPTER";
    private string _chapterLoadingDetail = "กำลังเตรียม chapter ถัดไป";
    private string _chapterLoadingImage = "story/chapter/chapter_ruined_city_sunset.png";
    private CancellationTokenSource? _chapterLoadingPulseCts;

    public MainViewModel()
    {
        _saveService = new SaveService();
        _eventService = new EventService();
        _difficultyService = new DifficultyService();

        _goOutsideCommand = new Command(async () => await GoOutsideAsync(), () => CanGoOutside);
        GoOutsideCommand = _goOutsideCommand;
        RestCommand = new Command(async () => await RestAsync());

        // default values
        HPProgress = 1.0;
        HungerProgress = 1.0;
        ThirstProgress = 1.0;
        FatigueProgress = 0.0;
        NoiseProgress = 0.0;
        InfectionProgress = 0.0;

        // attempt to load existing
        _ = InitializeAsync();
    }

    public ICommand GoOutsideCommand { get; }
    public ICommand RestCommand { get; }

    public bool CanGoOutside => !IsActionBusy && !_isEventPopupOpen && _engine?.CurrentState?.Status == GameStatus.Running;
    public bool IsPaused => _engine?.CurrentState?.Status == GameStatus.Paused;
    public string StopButtonText => IsPaused ? "RESUME" : "STOP";

    public bool IsActionBusy
    {
        get => _isActionBusy;
        set
        {
            if (_isActionBusy == value) return;
            _isActionBusy = value;
            Raise(nameof(IsActionBusy));
            Raise(nameof(CanGoOutside));
            Raise(nameof(GoOutsideButtonText));
            _goOutsideCommand.ChangeCanExecute();
        }
    }

    public string GoOutsideButtonText => IsActionBusy ? "LOADING..." : "GO OUTSIDE";

    public async Task SaveGameAsync()
    {
        if (_engine?.CurrentState == null)
            return;

        _engine.SaveCheckpoint();
        await SaveGameDatabaseAsync(_engine.CurrentState);
    }

    public async Task ToggleStopAsync()
    {
        if (_engine?.CurrentState == null)
            return;

        if (_engine.CurrentState.Status == GameStatus.Paused)
            _engine.ResumeGame();
        else if (_engine.CurrentState.Status == GameStatus.Running)
            _engine.PauseGame();

        await SaveGameAsync();
        Raise(nameof(IsPaused));
        Raise(nameof(StopButtonText));
        Raise(nameof(CanGoOutside));
        _goOutsideCommand.ChangeCanExecute();
    }

    public bool IsChapterLoading
    {
        get => _isChapterLoading;
        set
        {
            if (_isChapterLoading == value) return;
            _isChapterLoading = value;
            Raise(nameof(IsChapterLoading));
        }
    }

    public double ChapterLoadingProgress
    {
        get => _chapterLoadingProgress;
        set
        {
            _chapterLoadingProgress = Math.Clamp(value, 0, 1);
            Raise(nameof(ChapterLoadingProgress));
            Raise(nameof(ChapterLoadingPercentText));
        }
    }

    public string ChapterLoadingTitle
    {
        get => _chapterLoadingTitle;
        set { _chapterLoadingTitle = value; Raise(nameof(ChapterLoadingTitle)); }
    }

    public string ChapterLoadingDetail
    {
        get => _chapterLoadingDetail;
        set { _chapterLoadingDetail = value; Raise(nameof(ChapterLoadingDetail)); }
    }

    public string ChapterLoadingImage
    {
        get => _chapterLoadingImage;
        set { _chapterLoadingImage = value; Raise(nameof(ChapterLoadingImage)); }
    }

    public string ChapterLoadingPercentText => $"{(int)Math.Round(ChapterLoadingProgress * 100)}%";

    double _hpProgress;
    public double HPProgress { get => _hpProgress; set { _hpProgress = value; Raise(nameof(HPProgress)); } }

    double _hungerProgress;
    public double HungerProgress { get => _hungerProgress; set { _hungerProgress = value; Raise(nameof(HungerProgress)); } }

    double _thirstProgress;
    public double ThirstProgress { get => _thirstProgress; set { _thirstProgress = value; Raise(nameof(ThirstProgress)); } }

    double _fatigueProgress;
    public double FatigueProgress { get => _fatigueProgress; set { _fatigueProgress = value; Raise(nameof(FatigueProgress)); } }

    double _noiseProgress;
    public double NoiseProgress { get => _noiseProgress; set { _noiseProgress = value; Raise(nameof(NoiseProgress)); } }

    double _infectionProgress;
    public double InfectionProgress { get => _infectionProgress; set { _infectionProgress = value; Raise(nameof(InfectionProgress)); } }

    string _survivalPressureDisplay = "เสียง 0%  |  ติดเชื้อ 0%";
    public string SurvivalPressureDisplay { get => _survivalPressureDisplay; set { _survivalPressureDisplay = value; Raise(nameof(SurvivalPressureDisplay)); } }

    string _survivorImage = "survivor_idle.png";
    public string SurvivorImage { get => _survivorImage; set { _survivorImage = value; Raise(nameof(SurvivorImage)); } }

    string _backgroundImage = "male/main_page_male.gif";
    public string BackgroundImage { get => _backgroundImage; set { _backgroundImage = value; Raise(nameof(BackgroundImage)); } }

    int _selectedRestIndex = 0;
    public int SelectedRestIndex { get => _selectedRestIndex; set { _selectedRestIndex = value; Raise(nameof(SelectedRestIndex)); } }

    string _dayTimeDisplay = "DAY 1: 00:00";
    public string DayTimeDisplay { get => _dayTimeDisplay; set { _dayTimeDisplay = value; Raise(nameof(DayTimeDisplay)); } }



    private async Task InitializeAsync()
    {
        // 🚨 1. เปลี่ยนมาใช้ GameDatabase ในการโหลดข้อมูลแทน SaveService 🚨
        var db = new GameDatabase();
        var (survivor, state, inventory) = await db.LoadAsync();

        if (survivor != null && state != null)
        {
            if (state.Status == GameStatus.GameOver && state.Difficulty != Difficulty.Hard)
            {
                var dailyCheckpoint = await _saveService.LoadDailyCheckpointAsync();
                if (dailyCheckpoint != null)
                {
                    dailyCheckpoint.Status = GameStatus.Running;
                    dailyCheckpoint.GameMinute = 0;
                    dailyCheckpoint.Survivor.HP = Math.Max(dailyCheckpoint.Survivor.HP, 1f);
                    await SaveGameDatabaseAsync(dailyCheckpoint);
                    state = dailyCheckpoint;
                    survivor = dailyCheckpoint.Survivor;
                    inventory = dailyCheckpoint.Survivor.Inventory;
                }
            }

            if (inventory != null)
                survivor.Inventory = inventory;

            // ถ้าเจอเซฟผู้หญิง ก็จับคู่ข้อมูลให้ GameState เอาไปใช้ต่อ
            state.Survivor = survivor;
            _currentState = state;
        }
        else
        {
            // กรณีหาเซฟไม่เจอจริงๆ ถึงจะสร้างค่าเริ่มต้น
            var defaultSurvivor = new Survivor { Name = "Player", Gender = Gender.Male };
            _currentState = new GameState { Survivor = defaultSurvivor, Difficulty = Difficulty.Normal, Status = GameStatus.Running };
        }

        _engine = new GameEngine(_eventService, _difficultyService, _saveService);
        _engine.OnStateChanged += (gs) => MainThread.BeginInvokeOnMainThread(() => UpdateFromState(gs));
        _engine.OnEventTriggered += (ev) => OnEventOccurred(ev);
        _engine.OnGameOver += OnGameOverOccurred;

        // เริ่มเกมด้วยข้อมูลที่โหลดมาถูกต้อง 100%
        _engine.StartGame(_currentState!);
        UpdateFromState(_currentState);
        Raise(nameof(CanGoOutside));
        _goOutsideCommand.ChangeCanExecute();
    }

    public async Task ReloadCheckpointAsync()
    {
        var saved = await _saveService.LoadCheckpointAsync();
        if (saved == null || saved.Status != GameStatus.Running)
            return;

        _currentState = saved;
        _engine?.StartGame(saved);
        UpdateFromState(saved);
        Raise(nameof(CanGoOutside));
        _goOutsideCommand.ChangeCanExecute();
    }

    // unused in this variant
    private void OnStatusUpdated(Survivor s, GameState gs) { }

    private void OnEventOccurred(GameEvent ev)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (_isEventPopupOpen) return;

                var navigation = Shell.Current?.Navigation
                    ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
                if (navigation == null) return;

                var chapterLabel = _currentState == null
                    ? "THE END OF MINE"
                    : $"CHAPTER {_currentState.CurrentChapter}: {_currentState.CurrentChapterTitle}";

                _isEventPopupOpen = true;
                Raise(nameof(CanGoOutside));
                _goOutsideCommand.ChangeCanExecute();

                var popup = new EventPopup(ev, chapterLabel, _currentState?.Survivor.Inventory, choice =>
                {
                    _engine?.ApplyEventChoice(choice);
                    if (_engine?.CurrentState is { Status: GameStatus.Running })
                        _ = SaveGameDatabaseAsync(_engine.CurrentState);
                });
                popup.Disappearing += (_, _) =>
                {
                    _isEventPopupOpen = false;
                    Raise(nameof(CanGoOutside));
                    _goOutsideCommand.ChangeCanExecute();
                };

                await navigation.PushModalAsync(popup);
            }
            catch
            {
                _isEventPopupOpen = false;
                Raise(nameof(CanGoOutside));
                _goOutsideCommand.ChangeCanExecute();
            }
        });
    }

    private static async Task SaveGameDatabaseAsync(GameState state)
    {
        var db = new GameDatabase();
        await db.SaveAsync(state.Survivor, state, state.Survivor.Inventory);
    }

    private void OnMessageIssued(string msg) { }

    private void OnPlayerDied() { }

    private async Task GoOutsideAsync()
    {
        if (_engine == null || IsActionBusy) return;

        var isChapterTransition = IsNextChapterTransition(_engine.CurrentState);
        IsActionBusy = true;
        try
        {
            if (isChapterTransition)
            {
                await ShowChapterLoadingAsync(_engine.CurrentState!);
                StartChapterLoadingPulse();
            }

            await _engine.GoOutsideAsync();
            if (_engine.CurrentState != null)
                await SaveGameDatabaseAsync(_engine.CurrentState);

            if (isChapterTransition)
            {
                StopChapterLoadingPulse();
                await CompleteChapterLoadingAsync(_engine.CurrentState);
            }
        }
        finally
        {
            if (isChapterTransition)
            {
                StopChapterLoadingPulse();
                await HideChapterLoadingAsync();
            }

            IsActionBusy = false;
        }
    }

    private async Task RestAsync()
    {
        if (_engine == null) return;
        if (SelectedRestIndex == 1)
            _engine.Sleep();
        else
            _engine.Rest();

        if (_engine.CurrentState is { Status: GameStatus.Running })
            await SaveGameDatabaseAsync(_engine.CurrentState);

        await Task.CompletedTask;
    }

    private void UpdateFromState(GameState? gs)
    {
        if (gs == null) return;
        _currentState = gs;
        var s = gs.Survivor;
        HPProgress = s.HP / 100.0;
        HungerProgress = s.Hunger / 100.0;
        ThirstProgress = s.Thirst / 100.0;
        FatigueProgress = s.Fatigue / 100.0;
        NoiseProgress = gs.Noise / 100.0;
        InfectionProgress = gs.Infection / 100.0;
        SurvivalPressureDisplay = $"เสียง {gs.Noise:0}%  |  ติดเชื้อ {gs.Infection:0}%";
        DayTimeDisplay = gs.TimeDisplay;

        // 🚨 2. ลบคำว่า male/ และ female/ ออกจาก Path รูปภาพ (ถ้าคุณย้ายไฟล์ออกมารวมกันแล้ว) 🚨
        SurvivorImage = s.Gender == Gender.Male ? "portrait_male.png" : "portrait_female.png";
        BackgroundImage = s.Gender == Gender.Male ? "main_page_male.gif" : "main_page_female.gif";

        Raise(nameof(HPProgress));
        Raise(nameof(HungerProgress));
        Raise(nameof(ThirstProgress));
        Raise(nameof(FatigueProgress));
        Raise(nameof(NoiseProgress));
        Raise(nameof(InfectionProgress));
        Raise(nameof(SurvivalPressureDisplay));
        Raise(nameof(DayTimeDisplay));
        Raise(nameof(SurvivorImage));
        Raise(nameof(BackgroundImage));
        Raise(nameof(CanGoOutside));
        Raise(nameof(IsPaused));
        Raise(nameof(StopButtonText));
        _goOutsideCommand.ChangeCanExecute();
    }

    private void OnGameOverOccurred()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (_engine?.CurrentState != null)
                {
                    var db = new GameDatabase();
                    db.HandleDeathPersistence(_engine.CurrentState);
                }

                var navigation = Shell.Current?.Navigation
                    ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
                if (navigation == null) return;

                await navigation.PushAsync(new GameOverPage(_engine?.CurrentState));
            }
            catch { }
        });
    }

    private static bool IsNextChapterTransition(GameState? state)
    {
        return state?.Status == GameStatus.Running
            && state.GeneratedEvents.Count > 0
            && state.EventIndex >= state.GeneratedEvents.Count
            && state.CurrentChapter < state.MaxChapters;
    }

    private async Task ShowChapterLoadingAsync(GameState state)
    {
        ChapterLoadingTitle = $"ENTERING CHAPTER {state.CurrentChapter + 1}";
        ChapterLoadingDetail = "กำลังปิดบทเดิมและสร้างเส้นทางถัดไป";
        ChapterLoadingImage = string.IsNullOrWhiteSpace(state.CurrentChapterImagePath)
            ? "story/chapter/chapter_ruined_city_sunset.png"
            : state.CurrentChapterImagePath;
        ChapterLoadingProgress = 0;
        IsChapterLoading = true;

        await SetChapterLoadingProgressAsync(0.16, "กำลังสรุปผลจาก chapter ก่อนหน้า");
        await SetChapterLoadingProgressAsync(0.31, "กำลังสร้างเหตุการณ์ชุดใหม่");
    }

    private async Task CompleteChapterLoadingAsync(GameState? state)
    {
        if (state != null)
        {
            ChapterLoadingTitle = $"ENTERING CHAPTER {state.CurrentChapter}";
            ChapterLoadingDetail = string.IsNullOrWhiteSpace(state.CurrentChapterTitle)
                ? "chapter ถัดไปพร้อมแล้ว"
                : state.CurrentChapterTitle;

            if (!string.IsNullOrWhiteSpace(state.CurrentChapterImagePath))
                ChapterLoadingImage = state.CurrentChapterImagePath;
        }

        await SetChapterLoadingProgressAsync(0.82, "กำลังจัดฉากและบันทึก checkpoint");
        await SetChapterLoadingProgressAsync(1, "พร้อมเข้าสู่ chapter ใหม่");
        await Task.Delay(220);
    }

    private void StartChapterLoadingPulse()
    {
        StopChapterLoadingPulse();

        _chapterLoadingPulseCts = new CancellationTokenSource();
        var token = _chapterLoadingPulseCts.Token;

        _ = Task.Run(async () =>
        {
            var details = new[]
            {
                "กำลังเขียนเหตุการณ์ให้ต่อเนื่องกับเรื่องเดิม",
                "กำลังตรวจไอเทมและผลลัพธ์ของแต่ละทางเลือก",
                "กำลังเลือกฉากและรูป chapter ถัดไป",
                "กำลังจัดสมดุลเวลา ค่าสถานะ และความเสี่ยง",
                "กำลังตรวจความต่อเนื่องก่อนเข้า chapter ใหม่"
            };

            var index = 0;
            while (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (ChapterLoadingProgress < 0.88)
                    {
                        var step = ChapterLoadingProgress < 0.62 ? 0.025 : 0.009;
                        ChapterLoadingProgress = Math.Min(0.88, ChapterLoadingProgress + step);
                    }

                    ChapterLoadingDetail = details[index % details.Length];
                });

                index++;

                try
                {
                    await Task.Delay(520, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopChapterLoadingPulse()
    {
        if (_chapterLoadingPulseCts == null)
            return;

        _chapterLoadingPulseCts.Cancel();
        _chapterLoadingPulseCts.Dispose();
        _chapterLoadingPulseCts = null;
    }

    private async Task SetChapterLoadingProgressAsync(double progress, string detail)
    {
        ChapterLoadingDetail = detail;
        ChapterLoadingProgress = progress;
        await Task.Delay(180);
    }

    private async Task HideChapterLoadingAsync()
    {
        if (!IsChapterLoading) return;

        await Task.Delay(80);
        IsChapterLoading = false;
        ChapterLoadingProgress = 0;
    }

    private void Raise(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public void Dispose() { }
}
