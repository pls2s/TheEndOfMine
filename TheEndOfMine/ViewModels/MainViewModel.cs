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
    private GameEngine? _engine;
    private GameState? _currentState;

    public MainViewModel()
    {
        _saveService = new SaveService();
        _eventService = new EventService();
        _difficultyService = new DifficultyService();

        GoOutsideCommand = new Command(async () => await GoOutsideAsync());
        RestCommand = new Command(async () => await RestAsync());

        // default values
        HPProgress = 1.0;
        HungerProgress = 1.0;
        ThirstProgress = 1.0;
        FatigueProgress = 0.0;

        // attempt to load existing
        _ = InitializeAsync();
    }

    public ICommand GoOutsideCommand { get; }
    public ICommand RestCommand { get; }

    double _hpProgress;
    public double HPProgress { get => _hpProgress; set { _hpProgress = value; Raise(nameof(HPProgress)); } }

    double _hungerProgress;
    public double HungerProgress { get => _hungerProgress; set { _hungerProgress = value; Raise(nameof(HungerProgress)); } }

    double _thirstProgress;
    public double ThirstProgress { get => _thirstProgress; set { _thirstProgress = value; Raise(nameof(ThirstProgress)); } }

    double _fatigueProgress;
    public double FatigueProgress { get => _fatigueProgress; set { _fatigueProgress = value; Raise(nameof(FatigueProgress)); } }

    string _survivorImage = "survivor_idle.png";
    public string SurvivorImage { get => _survivorImage; set { _survivorImage = value; Raise(nameof(SurvivorImage)); } }

    string _backgroundImage = "male/main_page_male.gif";
    public string BackgroundImage { get => _backgroundImage; set { _backgroundImage = value; Raise(nameof(BackgroundImage)); } }

    int _selectedRestIndex = 0;
    public int SelectedRestIndex { get => _selectedRestIndex; set { _selectedRestIndex = value; Raise(nameof(SelectedRestIndex)); } }

    string _dayTimeDisplay = "DAY 1: 08:00 AM";
    public string DayTimeDisplay { get => _dayTimeDisplay; set { _dayTimeDisplay = value; Raise(nameof(DayTimeDisplay)); } }



    private async Task InitializeAsync()
    {
        // 🚨 1. เปลี่ยนมาใช้ GameDatabase ในการโหลดข้อมูลแทน SaveService 🚨
        var db = new GameDatabase();
        var (survivor, state, inventory) = await db.LoadAsync();

        if (survivor != null && state != null)
        {
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

        // เริ่มเกมด้วยข้อมูลที่โหลดมาถูกต้อง 100%
        _engine.StartGame(_currentState!);
        UpdateFromState(_currentState);
    }

    // unused in this variant
    private void OnStatusUpdated(Survivor s, GameState gs) { }

    private void OnEventOccurred(GameEvent ev)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var navigation = Shell.Current?.Navigation
                    ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
                if (navigation == null) return;

                var chapterLabel = _currentState == null
                    ? "THE END OF MINE"
                    : $"CHAPTER {_currentState.CurrentChapter}: {_currentState.CurrentChapterTitle}";

                await navigation.PushModalAsync(new EventPopup(ev, chapterLabel, choice =>
                {
                    _engine?.ApplyEventChoice(choice);
                    if (_engine?.CurrentState != null)
                        _ = SaveGameDatabaseAsync(_engine.CurrentState);
                }));
            }
            catch { }
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
        if (_engine == null) return;
        await _engine.GoOutsideAsync();
    }

    private async Task RestAsync()
    {
        if (_engine == null) return;
        if (SelectedRestIndex == 1)
            _engine.Sleep();
        else
            _engine.Rest();
        await Task.CompletedTask;
    }

    private void UpdateFromState(GameState? gs)
    {
        if (gs == null) return;
        var s = gs.Survivor;
        HPProgress = s.HP / 100.0;
        HungerProgress = s.Hunger / 100.0;
        ThirstProgress = s.Thirst / 100.0;
        FatigueProgress = s.Fatigue / 100.0;
        DayTimeDisplay = gs.TimeDisplay;

        // 🚨 2. ลบคำว่า male/ และ female/ ออกจาก Path รูปภาพ (ถ้าคุณย้ายไฟล์ออกมารวมกันแล้ว) 🚨
        SurvivorImage = s.Gender == Gender.Male ? "portrait_male.png" : "portrait_female.png";
        BackgroundImage = s.Gender == Gender.Male ? "main_page_male.gif" : "main_page_female.gif";

        Raise(nameof(HPProgress));
        Raise(nameof(HungerProgress));
        Raise(nameof(ThirstProgress));
        Raise(nameof(FatigueProgress));
        Raise(nameof(DayTimeDisplay));
        Raise(nameof(SurvivorImage));
        Raise(nameof(BackgroundImage));
    }

    private void Raise(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public void Dispose() { }
}
