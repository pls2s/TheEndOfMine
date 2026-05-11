using System.Timers;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;


public class GameEngine
{
    private readonly EventService _eventService;
    private readonly DifficultyService _difficultyService;
    private readonly SaveService _saveService;
    private readonly LlmGameContentService _llmContentService;

    private readonly System.Timers.Timer _gameTimer;

    // 1 real second = 1 game minute
    private const int TickIntervalMs = 1000;

    // ---- Public State ----
    public GameState? CurrentState { get; private set; }
    public Survivor? Player => CurrentState?.Survivor;
    public bool IsRunning => _gameTimer.Enabled;

    // ---- Events for UI ----
    public event Action<GameState>? OnStateChanged;
    public event Action<GameEvent>? OnEventTriggered;
    public event Action? OnGameOver;
    public event Action? OnCheckpointLoaded;

    public GameEngine(
        EventService eventService,
        DifficultyService difficultyService,
        SaveService saveService,
        LlmGameContentService? llmContentService = null)
    {
        _eventService = eventService;
        _difficultyService = difficultyService;
        _saveService = saveService;
        _llmContentService = llmContentService ?? new LlmGameContentService();

        _gameTimer = new System.Timers.Timer(TickIntervalMs);
        _gameTimer.Elapsed += OnTimerTick;
        _gameTimer.AutoReset = true;
    }

    // ---- Game Lifecycle ----

    public void StartGame(Survivor survivor, Difficulty difficulty)
    {
        StartGame(new GameState
        {
            Survivor = survivor,
            Difficulty = difficulty,
            Status = GameStatus.Running,
            DayCount = 1,
            GameMinute = 0
        });
    }

    public void StartGame(GameState state)
    {
        CurrentState = state;
        CurrentState.Status = GameStatus.Running;
        _saveService.SaveCheckpoint(CurrentState);
        _gameTimer.Start();
        NotifyStateChanged();
    }

    public void PauseGame()
    {
        if (CurrentState == null) return;
        _gameTimer.Stop();
        CurrentState.Status = GameStatus.Paused;
    }

    public void ResumeGame()
    {
        if (CurrentState?.Status != GameStatus.Paused) return;
        CurrentState.Status = GameStatus.Running;
        _gameTimer.Start();
    }

    // ---- Player Actions ----

    // ออกสำรวจ → เหนื่อยขึ้น + trigger event ถัดไปใน story
    public async Task GoOutsideAsync()
    {
        if (CurrentState?.Status != GameStatus.Running) return;

        ApplyStatChange(fatigueDelta: 5f);

        var gameEvent = await _eventService.GetNextEventAsync(CurrentState.EventIndex, CurrentState.GeneratedEvents);
        if (gameEvent == null && CurrentState.GeneratedEvents.Count > 0)
        {
            gameEvent = await AdvanceChapterAsync();
        }

        if (gameEvent != null)
        {
            // เดิน index ไปข้างหน้าก่อน invoke เพื่อไม่ให้ซ้ำถ้า save ระหว่าง event
            CurrentState.EventIndex++;
            MainThread.BeginInvokeOnMainThread(() => OnEventTriggered?.Invoke(gameEvent));
        }

        NotifyStateChanged();
    }

    private async Task<GameEvent?> AdvanceChapterAsync()
    {
        if (CurrentState == null) return null;

        if (CurrentState.CurrentChapter >= CurrentState.MaxChapters)
        {
            _gameTimer.Stop();
            CurrentState.Status = GameStatus.GameOver;
            MainThread.BeginInvokeOnMainThread(() => OnGameOver?.Invoke());
            return null;
        }

        if (!string.IsNullOrWhiteSpace(CurrentState.CurrentChapterTitle))
            CurrentState.CompletedChapterTitles.Add(CurrentState.CurrentChapterTitle);

        var nextChapter = await _llmContentService.GenerateNextChapterAsync(CurrentState);
        CurrentState.CurrentChapter++;
        CurrentState.CurrentChapterTitle = nextChapter.StoryTitle;
        CurrentState.CurrentChapterAlias = nextChapter.ChapterAlias ?? string.Empty;
        CurrentState.CurrentChapterImagePath = nextChapter.ChapterImagePath;
        CurrentState.GeneratedEvents = nextChapter.Events;
        CurrentState.EventIndex = 0;
        CurrentState.StorySource = nextChapter.UsedRemoteLlm ? "llm" : "local_fallback";

        _saveService.SaveCheckpoint(CurrentState);
        return await _eventService.GetNextEventAsync(CurrentState.EventIndex, CurrentState.GeneratedEvents);
    }

    // พักผ่อน → ลด Fatigue เล็กน้อย
    public void Rest()
    {
        if (CurrentState?.Status != GameStatus.Running) return;

        ApplyStatChange(fatigueDelta: -20f, hungerDelta: 3f, thirstDelta: 3f);
        NotifyStateChanged();
    }

    // นอนหลับ → ข้าม 8 ชั่วโมงในเกม, ฟื้นฟู HP/Fatigue + save checkpoint
    public void Sleep()
    {
        if (CurrentState?.Status != GameStatus.Running) return;

        // ข้ามเวลา 480 นาทีในเกม (8 ชั่วโมง)
        CurrentState.GameMinute += 480;
        NormalizeTime();

        ApplyStatChange(hpDelta: 20f, fatigueDelta: -60f, hungerDelta: -15f, thirstDelta: -20f);

        _saveService.SaveCheckpoint(CurrentState);
        NotifyStateChanged();
    }

    // Apply ผลที่ได้จากการเลือกใน event
    public void ApplyEventChoice(EventChoice choice)
    {
        if (CurrentState == null) return;

        ApplyStatChange(
            hpDelta: choice.HpEffect,
            hungerDelta: choice.HungerEffect,
            thirstDelta: choice.ThirstEffect,
            fatigueDelta: choice.FatigueEffect
        );

        if (choice.ItemReward != null)
            CurrentState.Survivor.Inventory.AddItem(choice.ItemReward);

        _saveService.SaveCheckpoint(CurrentState);
        CheckDeath();
        NotifyStateChanged();
    }

    // ---- Save / Load ----

    public void SaveCheckpoint()
    {
        if (CurrentState != null)
            _saveService.SaveCheckpoint(CurrentState);
    }

    public async Task LoadCheckpointAsync()
    {
        var saved = await _saveService.LoadCheckpointAsync();
        if (saved == null)
        {
            OnGameOver?.Invoke();
            return;
        }

        CurrentState = saved;
        CurrentState.Status = GameStatus.Running;
        _gameTimer.Start();
        MainThread.BeginInvokeOnMainThread(() => OnCheckpointLoaded?.Invoke());
        NotifyStateChanged();
    }

    // ---- Private Helpers ----

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        if (CurrentState?.Status != GameStatus.Running) return;

        // เดินหน้า 1 นาทีในเกม
        CurrentState.GameMinute++;
        NormalizeTime();

        // ลด stats ตามอัตราของระดับความยาก
        var rates = _difficultyService.GetDecayRates(CurrentState.Difficulty);
        ApplyStatChange(
            hungerDelta: -rates.HungerDecay,
            thirstDelta: -rates.ThirstDecay,
            fatigueDelta: rates.FatigueDecay
        );

        // Hunger หรือ Thirst = 0 → ลด HP
        if (CurrentState.Survivor.Hunger <= 0)
            ApplyStatChange(hpDelta: -rates.StarveHpDecay);
        if (CurrentState.Survivor.Thirst <= 0)
            ApplyStatChange(hpDelta: -rates.DehydrateHpDecay);

        CheckDeath();
        MainThread.BeginInvokeOnMainThread(NotifyStateChanged);
    }

    private void ApplyStatChange(
        float hpDelta = 0f, float hungerDelta = 0f,
        float thirstDelta = 0f, float fatigueDelta = 0f)
    {
        if (CurrentState == null) return;
        var s = CurrentState.Survivor;
        s.HP      = Math.Clamp(s.HP      + hpDelta,      0f, 100f);
        s.Hunger  = Math.Clamp(s.Hunger  + hungerDelta,  0f, 100f);
        s.Thirst  = Math.Clamp(s.Thirst  + thirstDelta,  0f, 100f);
        s.Fatigue = Math.Clamp(s.Fatigue + fatigueDelta, 0f, 100f);
    }

    private void CheckDeath()
    {
        if (CurrentState?.Survivor.HP > 0) return;

        _gameTimer.Stop();
        CurrentState!.Status = GameStatus.GameOver;

        if (CurrentState.Difficulty == Difficulty.Hard)
        {
            // Permadeath — ลบ save แล้วจบเกม
            _saveService.DeleteSave();
            MainThread.BeginInvokeOnMainThread(() => OnGameOver?.Invoke());
        }
        else
        {
            // Easy / Normal — Respawn จาก checkpoint
            _ = RespawnAsync();
        }
    }

    private async Task RespawnAsync()
    {
        var checkpoint = await _saveService.LoadCheckpointAsync();
        if (checkpoint != null)
        {
            CurrentState = checkpoint;
            CurrentState.Status = GameStatus.Running;
            _gameTimer.Start();
            MainThread.BeginInvokeOnMainThread(() => OnCheckpointLoaded?.Invoke());
            NotifyStateChanged();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => OnGameOver?.Invoke());
        }
    }

    // แปลง GameMinute เกิน 1440 (24 ชั่วโมง) ให้นับเป็นวันใหม่
    private void NormalizeTime()
    {
        if (CurrentState == null) return;
        while (CurrentState.GameMinute >= 1440)
        {
            CurrentState.GameMinute -= 1440;
            CurrentState.DayCount++;
        }
    }

    private void NotifyStateChanged()
    {
        if (CurrentState != null)
            OnStateChanged?.Invoke(CurrentState);
    }
}
