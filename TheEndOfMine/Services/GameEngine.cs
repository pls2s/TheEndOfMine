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
    private bool _isEventInProgress;
    private GameEvent? _activeEvent;

    // 1 real second = 1 game minute
    private const int TickIntervalMs = 1000;
    private const int EventDurationMinutes = 720;
    private const float PassiveNoiseDecayPerMinute = 0.12f;
    private const float InfectionWarningThreshold = 25f;
    private const float InfectionHpDamagePerMinute = 0.018f;
    private const float InfectionGrowthPerMinute = 0.004f;
    private const float NoisyEventThreshold = 55f;
    private const float FatigueSoftThreshold = 55f;
    private const float FatigueMidThreshold = 70f;
    private const float FatigueHighThreshold = 85f;
    private const float FatigueCriticalThreshold = 95f;
    private const float FatigueStrainHpPerMinute = 0.04f;
    private const float ThirstSoftThreshold = 45f;
    private const float ThirstMidThreshold = 30f;
    private const float ThirstHighThreshold = 15f;
    private const float ThirstCriticalThreshold = 5f;
    private const float ThirstStrainHpPerMinute = 0.025f;

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
        if (CurrentState.Difficulty == Difficulty.Hard)
            _saveService.DeleteDailyCheckpoint();
        else if (CurrentState.GameMinute == 0)
            SaveDailyCheckpoint();
        _gameTimer.Start();
        NotifyStateChanged();
    }

    public void PauseGame()
    {
        if (CurrentState == null) return;
        _gameTimer.Stop();
        CurrentState.Status = GameStatus.Paused;
        NotifyStateChanged();
    }

    public void ResumeGame()
    {
        if (CurrentState?.Status != GameStatus.Paused) return;
        CurrentState.Status = GameStatus.Running;
        _gameTimer.Start();
        NotifyStateChanged();
    }

    public void ForceStoryEndingForDebug()
    {
        if (CurrentState == null) return;

        _gameTimer.Stop();
        _isEventInProgress = false;
        _activeEvent = null;
        ApplyStoryEnding();
        CurrentState.Status = GameStatus.GameOver;
        NotifyStateChanged();
        MainThread.BeginInvokeOnMainThread(() => OnGameOver?.Invoke());
    }

    // ---- Player Actions ----

    // ออกสำรวจ → trigger event ถัดไปใน story
    public async Task GoOutsideAsync()
    {
        if (CurrentState?.Status != GameStatus.Running || _isEventInProgress) return;

        var gameEvent = await _eventService.GetNextEventAsync(CurrentState.EventIndex, CurrentState.GeneratedEvents);
        if (gameEvent == null && CurrentState.GeneratedEvents.Count > 0)
        {
            gameEvent = await AdvanceChapterAsync();
        }

        if (gameEvent != null)
        {
            // เดิน index ไปข้างหน้าก่อน invoke เพื่อไม่ให้ซ้ำถ้า save ระหว่าง event
            _isEventInProgress = true;
            _activeEvent = gameEvent;
            CurrentState.EventIndex++;
            _saveService.SaveCheckpoint(CurrentState);
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
            ApplyStoryEnding();
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

    // พักผ่อน → ลด Fatigue แต่ยังใช้เสบียงตามเวลาที่ผ่านไป
    public void Rest()
    {
        if (CurrentState?.Status != GameStatus.Running) return;

        var resumeTimer = _gameTimer.Enabled;
        _gameTimer.Stop();

        AdvanceTimeWithSurvivalCosts(
            minutes: 240,
            fatigueDelta: -20f,
            hungerDelta: -6f,
            thirstDelta: -8f);
        CheckDeath();

        if (CurrentState.Status == GameStatus.Running)
        {
            _saveService.SaveCheckpoint(CurrentState);
            if (resumeTimer)
                _gameTimer.Start();
        }
        NotifyStateChanged();
    }

    // นอนหลับ → ข้าม 8 ชั่วโมงในเกม, ฟื้นฟู Fatigue เท่านั้น
    public void Sleep()
    {
        if (CurrentState?.Status != GameStatus.Running) return;

        var resumeTimer = _gameTimer.Enabled;
        _gameTimer.Stop();

        AdvanceTimeWithSurvivalCosts(
            minutes: 480,
            fatigueDelta: -60f,
            hungerDelta: -15f,
            thirstDelta: -20f,
            applyHpDamage: false);

        if (CurrentState.Status == GameStatus.Running)
        {
            _saveService.SaveCheckpoint(CurrentState);
            if (resumeTimer)
                _gameTimer.Start();
        }
        NotifyStateChanged();
    }

    // Apply ผลที่ได้จากการเลือกใน event
    public void ApplyEventChoice(EventChoice choice)
    {
        if (CurrentState == null) return;
        _isEventInProgress = false;

        var resolvedEvent = _activeEvent;
        RecordStoryMemory(resolvedEvent, choice);
        _activeEvent = null;

        var rates = _difficultyService.GetDecayRates(CurrentState.Difficulty);
        ApplyStatChange(
            hpDelta: ScaleNegativeEffect(NormalizePositiveHpEffect(choice), rates.DamageMultiplier),
            hungerDelta: ScaleNegativeEffect(choice.HungerEffect, rates.DamageMultiplier),
            thirstDelta: ScaleNegativeEffect(choice.ThirstEffect, rates.DamageMultiplier),
            fatigueDelta: ScaleFatiguePenalty(choice.FatigueEffect, rates.DamageMultiplier)
        );
        ApplyChoiceSurvivalPressure(choice);
        InventoryChoiceEffectService.Apply(CurrentState, choice);

        CheckDeath(BuildChoiceDeathCause(resolvedEvent, choice));
        if (CurrentState.Status == GameStatus.Running)
        {
            AdvanceEventTime();
            ApplyNoiseAmbushRisk();
            CheckDeath();
        }

        if (CurrentState.Status == GameStatus.Running)
            _saveService.SaveCheckpoint(CurrentState);
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
        _isEventInProgress = false;
        _activeEvent = null;
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
        var newDayStarted = AdvanceClockOneMinute();
        var fatiguePressure = GetFatiguePressureMultiplier();
        var thirstPressure = GetThirstPressureMultiplier();

        // ลด stats ตามอัตราของระดับความยาก
        var rates = _difficultyService.GetDecayRates(CurrentState.Difficulty);
        ApplyStatChange(
            hungerDelta: -rates.HungerDecay * fatiguePressure * thirstPressure,
            thirstDelta: -rates.ThirstDecay * thirstPressure,
            fatigueDelta: rates.FatigueDecay * fatiguePressure * thirstPressure
        );

        if (newDayStarted)
            SaveDailyCheckpoint();

        ApplySurvivalDamage();
        DecayEnvironmentalPressure();
        ApplyFatigueStrainDamage();
        ApplyThirstStrainDamage();

        CheckDeath();
        MainThread.BeginInvokeOnMainThread(NotifyStateChanged);
    }

    private int AdvanceEventTime()
    {
        if (CurrentState == null) return 0;

        var minutes = EventDurationMinutes;
        var rates = _difficultyService.GetDecayRates(CurrentState.Difficulty);
        var fatiguePressure = GetFatiguePressureMultiplier();
        var thirstPressure = GetThirstPressureMultiplier();

        AdvanceTimeWithSurvivalCosts(
            minutes,
            fatigueDelta: rates.FatigueDecay * minutes * fatiguePressure * thirstPressure,
            hungerDelta: -rates.HungerDecay * minutes * fatiguePressure * thirstPressure,
            thirstDelta: -rates.ThirstDecay * minutes * thirstPressure);

        return minutes;
    }

    private void ApplySurvivalDamage()
    {
        if (CurrentState == null) return;

        var rates = _difficultyService.GetDecayRates(CurrentState.Difficulty);
        var s = CurrentState.Survivor;
        var damage = 0f;

        if (s.Hunger <= 0)
            damage += rates.StarveHpDecay;
        if (s.Thirst <= 0)
            damage += rates.DehydrateHpDecay;
        if (s.Thirst <= 20)
            damage += 0.03f;
        if (s.Thirst <= 5)
            damage += 0.08f;
        if (CurrentState.Infection >= InfectionWarningThreshold)
            damage += InfectionHpDamagePerMinute * (CurrentState.Infection / 25f);
        if (s.Fatigue >= FatigueHighThreshold)
            damage += 0.03f;
        if (s.Fatigue >= FatigueCriticalThreshold)
            damage += 0.08f;

        if (damage > 0)
            ApplyStatChange(hpDelta: -damage);
    }

    private void AdvanceTimeWithSurvivalCosts(
        int minutes,
        float fatigueDelta,
        float hungerDelta,
        float thirstDelta,
        bool applyHpDamage = true)
    {
        if (CurrentState == null || minutes <= 0) return;

        var fatiguePerMinute = fatigueDelta / minutes;
        var hungerPerMinute = hungerDelta / minutes;
        var thirstPerMinute = thirstDelta / minutes;

        for (var i = 0; i < minutes; i++)
        {
            var newDayStarted = AdvanceClockOneMinute();
            var fatiguePressure = GetFatiguePressureMultiplier();
            var thirstPressure = GetThirstPressureMultiplier();
            var fatigueApplied = fatiguePerMinute >= 0
                ? fatiguePerMinute * fatiguePressure
                : fatiguePerMinute;

            ApplyStatChange(
                hungerDelta: hungerPerMinute * fatiguePressure * thirstPressure,
                thirstDelta: thirstPerMinute * thirstPressure,
                fatigueDelta: fatigueApplied);

            if (newDayStarted)
                SaveDailyCheckpoint();

            if (applyHpDamage)
                ApplySurvivalDamage();
            DecayEnvironmentalPressure();
            if (applyHpDamage)
            {
                ApplyFatigueStrainDamage();
                ApplyThirstStrainDamage();
            }

            if (applyHpDamage && CurrentState.Survivor.HP <= 0)
                break;
        }
    }

    private void ApplyChoiceSurvivalPressure(EventChoice choice)
    {
        if (CurrentState == null) return;

        CurrentState.Noise = Math.Clamp(CurrentState.Noise + EstimateNoiseDelta(choice), 0f, 100f);
        CurrentState.Infection = Math.Clamp(CurrentState.Infection + EstimateInfectionDelta(choice), 0f, 100f);
    }

    private static float EstimateNoiseDelta(EventChoice choice)
    {
        var text = $"{choice.Text} {choice.ResultText}".ToLowerInvariant();
        var noise = 0f;

        if (ContainsAny(text, "ยิง", "ปืน", "gun", "shoot"))
            noise += 32f;
        if (ContainsAny(text, "ทุบ", "พัง", "เตะ", "ตะโกน", "ระเบิด", "กระจก", "smash", "break", "shout"))
            noise += 20f;
        if (ContainsAny(text, "งัด", "แงะ", "ชะแลง", "ชะเเลง", "crowbar"))
            noise += 12f;
        if (ContainsAny(text, "แอบ", "เงียบ", "หลบ", "ย่อง", "ซ่อน", "sneak", "hide"))
            noise -= 12f;

        return noise;
    }

    private static float EstimateInfectionDelta(EventChoice choice)
    {
        var text = $"{choice.Text} {choice.ResultText}".ToLowerInvariant();
        var infection = 0f;

        if (choice.HpEffect < 0 && ContainsAny(text, "กัด", "โดนกัด", "bite", "bitten"))
            infection += 22f;
        if (choice.HpEffect < 0 && ContainsAny(text, "แผล", "เลือด", "สนิม", "สกปรก", "หนอง", "wound", "blood", "rust"))
            infection += 10f;
        if (ContainsAny(text, "ศพ", "ซาก", "ของเสีย", "น้ำเน่า", "เน่า", "corpse", "sewer"))
            infection += 6f;
        if (ContainsAny(text, "รักษา", "ทำแผล", "พันแผล", "ยา", "ปฐมพยาบาล", "treat", "bandage", "medicine"))
            infection -= 14f;

        return infection;
    }

    private void ApplyNoiseAmbushRisk()
    {
        if (CurrentState == null || CurrentState.Noise < NoisyEventThreshold)
            return;

        var risk = Math.Clamp(CurrentState.Noise / 100f, 0f, 1f);
        if (CurrentState.Survivor.Fatigue >= FatigueHighThreshold)
            risk += 0.12f;
        if (CurrentState.Survivor.Fatigue >= FatigueCriticalThreshold)
            risk += 0.18f;

        if (Random.Shared.NextDouble() > risk * 0.45f)
            return;

        ApplyStatChange(hpDelta: -Random.Shared.Next(4, 13), fatigueDelta: 6f);
        if (CurrentState.Survivor.HP <= 0)
            CurrentState.DeathCause = "เสียงที่สะสมไว้ดังพอจะดึงอันตรายเข้ามาใกล้ คุณถูกโจมตีระหว่างเดินทางก่อนจะหนีออกมาได้";

        CurrentState.Noise = Math.Clamp(CurrentState.Noise - 18f, 0f, 100f);
    }

    private void DecayEnvironmentalPressure()
    {
        if (CurrentState == null) return;

        CurrentState.Noise = Math.Clamp(CurrentState.Noise - PassiveNoiseDecayPerMinute, 0f, 100f);

        if (CurrentState.Infection <= 0)
            return;

        var s = CurrentState.Survivor;
        var infectionPressure = 1f;
        if (s.Fatigue >= FatigueHighThreshold)
            infectionPressure += 1f;
        if (s.Thirst <= ThirstMidThreshold)
            infectionPressure += 0.35f;
        if (s.Thirst <= ThirstHighThreshold)
            infectionPressure += 0.35f;

        if (s.Hunger <= 15 || s.Thirst <= 15 || s.Fatigue >= FatigueMidThreshold)
            CurrentState.Infection = Math.Clamp(CurrentState.Infection + InfectionGrowthPerMinute * infectionPressure, 0f, 100f);
        else
            CurrentState.Infection = Math.Clamp(CurrentState.Infection - InfectionGrowthPerMinute * 0.5f, 0f, 100f);
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        return tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static float NormalizePositiveHpEffect(EventChoice choice)
    {
        if (choice.HpEffect <= 0)
            return choice.HpEffect;

        return IsHealingOrNutritionChoice(choice) ? choice.HpEffect : 0f;
    }

    private static float ScaleNegativeEffect(float value, float multiplier)
    {
        return value < 0f ? value * multiplier : value;
    }

    private static float ScaleFatiguePenalty(float value, float multiplier)
    {
        return value > 0f ? value * multiplier : value;
    }

    private static bool IsHealingOrNutritionChoice(EventChoice choice)
    {
        if (choice.HungerEffect > 0 || choice.ThirstEffect > 0)
            return true;

        var text = $"{choice.Text} {choice.ResultText}".ToLowerInvariant();
        return text.Contains("heal", StringComparison.Ordinal) ||
               text.Contains("treat", StringComparison.Ordinal) ||
               text.Contains("med", StringComparison.Ordinal) ||
               text.Contains("medicine", StringComparison.Ordinal) ||
               text.Contains("bandage", StringComparison.Ordinal) ||
               text.Contains("first aid", StringComparison.Ordinal) ||
               text.Contains("eat", StringComparison.Ordinal) ||
               text.Contains("drink", StringComparison.Ordinal) ||
               text.Contains("consume", StringComparison.Ordinal) ||
               text.Contains("รักษา", StringComparison.Ordinal) ||
               text.Contains("ทำแผล", StringComparison.Ordinal) ||
               text.Contains("พันแผล", StringComparison.Ordinal) ||
               text.Contains("ปฐมพยาบาล", StringComparison.Ordinal) ||
               text.Contains("ยา", StringComparison.Ordinal) ||
               text.Contains("ผ้าพันแผล", StringComparison.Ordinal) ||
               text.Contains("กิน", StringComparison.Ordinal) ||
               text.Contains("ดื่ม", StringComparison.Ordinal);
    }

    private void RecordStoryMemory(GameEvent? gameEvent, EventChoice choice)
    {
        if (CurrentState == null || gameEvent == null) return;

        CurrentState.StoryMemory ??= new List<StoryMemoryEntry>();

        var summary = BuildMemorySummary(gameEvent, choice);
        CurrentState.StoryMemory.Add(new StoryMemoryEntry
        {
            Chapter = CurrentState.CurrentChapter,
            Day = CurrentState.DayCount,
            Time = $"{CurrentState.Hour:D2}:{CurrentState.Minute:D2}",
            EventTitle = Truncate(gameEvent.Title, 80),
            ChoiceText = Truncate(choice.Text, 120),
            ResultText = Truncate(choice.ResultText, 180),
            Summary = summary
        });

        if (CurrentState.StoryMemory.Count > 24)
            CurrentState.StoryMemory.RemoveRange(0, CurrentState.StoryMemory.Count - 24);

        CurrentState.StoryArcSummary = BuildStoryArcSummary(CurrentState.StoryMemory);
    }

    private static string BuildMemorySummary(GameEvent gameEvent, EventChoice choice)
    {
        var rewardNames = choice.GetItemRewards()
            .Select(item => string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        var reward = rewardNames.Count == 0
            ? string.Empty
            : $" ได้รับ {string.Join(", ", rewardNames)}.";

        return Truncate($"{gameEvent.Title}: เลือก \"{choice.Text}\" ผลคือ {choice.ResultText}.{reward}", 260);
    }

    private static string BuildStoryArcSummary(IEnumerable<StoryMemoryEntry> memories)
    {
        var recent = memories
            .TakeLast(8)
            .Select(memory => $"บท {memory.Chapter} {memory.Summary}");

        return Truncate(string.Join(" | ", recent), 1200);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value;

        return value[..Math.Max(0, maxLength - 1)].TrimEnd() + "…";
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

    private void CheckDeath(string? causeOverride = null)
    {
        if (CurrentState == null || CurrentState.Survivor.HP > 0) return;

        _gameTimer.Stop();
        ApplyDeathEnding(causeOverride);
        CurrentState.Status = GameStatus.GameOver;
        if (CurrentState.Difficulty == Difficulty.Hard)
            _saveService.DeleteAllSaves();
        else
            _saveService.DeleteSave();
        MainThread.BeginInvokeOnMainThread(() => OnGameOver?.Invoke());
    }

    private void ApplyStoryEnding()
    {
        if (CurrentState == null) return;

        CurrentState.IsStoryEnding = true;
        CurrentState.GameOverTitle = "รอดไปถึงปลายทาง";
        CurrentState.DeathCause = string.Empty;
        CurrentState.EndingImagePath = BuildEndingImagePath();

        var survivorName = string.IsNullOrWhiteSpace(CurrentState.Survivor.Name)
            ? "คุณ"
            : CurrentState.Survivor.Name;
        var lastMemory = CurrentState.StoryMemory.LastOrDefault()?.Summary;
        var context = string.IsNullOrWhiteSpace(lastMemory)
            ? "หลังจากผ่านเมืองที่พังทลายและความเสี่ยงตลอดทาง"
            : $"จากผลของเหตุการณ์ล่าสุด: {lastMemory}";

        CurrentState.GameOverDetail =
            $"{context} {survivorName} ไปถึงสัญญาณช่วยเหลือก่อนเมืองจะปิดตาย เฮลิคอปเตอร์ยกตัวขึ้นเหนือซากตึก ทิ้งเสียงฝูงซอมบี้และถนนมืดไว้เบื้องล่าง การเอาชีวิตรอดครั้งนี้ไม่ได้ลบทุกอย่างที่เสียไป แต่มันพิสูจน์ว่าคุณยังพาตัวเองไปถึงวันพรุ่งนี้ได้";
    }

    private void ApplyDeathEnding(string? causeOverride)
    {
        if (CurrentState == null) return;

        CurrentState.IsStoryEnding = false;
        CurrentState.GameOverTitle = "เสียชีวิต";
        CurrentState.EndingImagePath = string.Empty;

        var cause = !string.IsNullOrWhiteSpace(causeOverride)
            ? causeOverride
            : !string.IsNullOrWhiteSpace(CurrentState.DeathCause)
                ? CurrentState.DeathCause
                : BuildDeathCauseFromState(CurrentState);

        CurrentState.DeathCause = cause;

        var lastMemory = CurrentState.StoryMemory.LastOrDefault()?.Summary;
        CurrentState.GameOverDetail = string.IsNullOrWhiteSpace(lastMemory)
            ? "การเดินทางจบลงก่อนจะพบทางออกจากเมือง"
            : $"เหตุการณ์สุดท้ายก่อนล้มลง: {lastMemory}";
    }

    private string BuildEndingImagePath()
    {
        if (CurrentState == null)
            return "story/ending/ending_helicopter_survivors.png";

        var gender = CurrentState.Survivor.Gender == Gender.Male ? "male" : "female";
        return $"story/{gender}/ending/{gender}_ending_helicopter_survivors.png";
    }

    private static string BuildChoiceDeathCause(GameEvent? gameEvent, EventChoice choice)
    {
        if (choice.HpEffect >= 0)
            return string.Empty;

        var eventTitle = string.IsNullOrWhiteSpace(gameEvent?.Title)
            ? "เหตุการณ์ล่าสุด"
            : gameEvent!.Title;
        var result = string.IsNullOrWhiteSpace(choice.ResultText)
            ? "การตัดสินใจนั้นทำให้บาดเจ็บหนักเกินกว่าจะไปต่อ"
            : choice.ResultText;

        return $"{eventTitle}: คุณเลือก \"{choice.Text}\" แล้วผลลัพธ์รุนแรงถึงชีวิต — {Truncate(result, 180)}";
    }

    private static string BuildDeathCauseFromState(GameState state)
    {
        var s = state.Survivor;

        if (s.Thirst <= 0 && s.Hunger <= 0)
            return "ร่างกายขาดทั้งน้ำและอาหารนานเกินไป เลือดค่อย ๆ ลดลงจนหมดแรงและเสียชีวิต";
        if (s.Thirst <= 0)
            return "ขาดน้ำรุนแรง ร่างกายเริ่มมึน ชีพจรอ่อนลง และเลือดลดลงจนเสียชีวิต";
        if (s.Hunger <= 0)
            return "อดอาหารต่อเนื่องจนร่างกายไม่มีแรงพยุงตัว บาดแผลและความอ่อนล้าทำให้เสียชีวิต";
        if (state.Infection >= InfectionWarningThreshold)
            return "การติดเชื้อในร่างกายลุกลามเกินควบคุม ไข้และบาดแผลทำให้เลือดลดลงจนเสียชีวิต";
        if (state.Noise >= NoisyEventThreshold)
            return "เสียงที่สะสมไว้ดึงอันตรายเข้ามาใกล้เกินไป คุณถูกโจมตีจนบาดเจ็บถึงชีวิต";
        if (s.Fatigue >= FatigueHighThreshold)
            return "ความเหนื่อยล้าสะสมทำให้ตัดสินใจช้าลงและทรุดลงก่อนจะหาที่ปลอดภัยได้";

        return "บาดแผลและความเสี่ยงที่สะสมระหว่างทางทำให้ร่างกายรับไม่ไหวและเสียชีวิต";
    }

    private float GetFatiguePressureMultiplier()
    {
        if (CurrentState == null)
            return 1f;

        var fatigue = CurrentState.Survivor.Fatigue;
        if (fatigue <= FatigueSoftThreshold)
            return 1f;

        if (fatigue >= FatigueCriticalThreshold)
            return 1.55f;

        if (fatigue >= FatigueHighThreshold)
            return 1.35f;

        return 1.15f;
    }

    private float GetFatigueActionPenalty()
    {
        if (CurrentState == null)
            return 0f;

        var fatigue = CurrentState.Survivor.Fatigue;
        if (fatigue < FatigueMidThreshold)
            return 0f;

        return fatigue >= FatigueCriticalThreshold
            ? 10f
            : fatigue >= FatigueHighThreshold
                ? 7f
                : 4f;
    }

    private float GetThirstPressureMultiplier()
    {
        if (CurrentState == null)
            return 1f;

        var thirst = CurrentState.Survivor.Thirst;
        if (thirst > ThirstSoftThreshold)
            return 1f;

        if (thirst <= ThirstCriticalThreshold)
            return 1.6f;

        if (thirst <= ThirstHighThreshold)
            return 1.35f;

        if (thirst <= ThirstMidThreshold)
            return 1.15f;

        return 1f;
    }

    private float GetThirstActionPenalty()
    {
        if (CurrentState == null)
            return 0f;

        var thirst = CurrentState.Survivor.Thirst;
        if (thirst > ThirstMidThreshold)
            return 0f;

        return thirst <= ThirstCriticalThreshold
            ? 10f
            : thirst <= ThirstHighThreshold
                ? 7f
                : 4f;
    }

    private void ApplyFatigueStrainDamage()
    {
        if (CurrentState == null)
            return;

        var fatigue = CurrentState.Survivor.Fatigue;
        if (fatigue < FatigueHighThreshold)
            return;

        var damage = FatigueStrainHpPerMinute;
        if (fatigue >= FatigueCriticalThreshold)
            damage += 0.08f;
        else
            damage += 0.03f;

        if (CurrentState.Survivor.Hunger <= 15 || CurrentState.Survivor.Thirst <= 15)
            damage += 0.02f;

        ApplyStatChange(hpDelta: -damage);
    }

    private void ApplyThirstStrainDamage()
    {
        if (CurrentState == null)
            return;

        var thirst = CurrentState.Survivor.Thirst;
        if (thirst > ThirstHighThreshold)
            return;

        var damage = ThirstStrainHpPerMinute;
        if (thirst <= ThirstCriticalThreshold)
            damage += 0.10f;
        else if (thirst <= ThirstHighThreshold)
            damage += 0.05f;

        if (CurrentState.Survivor.Fatigue >= FatigueHighThreshold)
            damage += 0.02f;

        ApplyStatChange(hpDelta: -damage);
    }

    private bool AdvanceClockOneMinute()
    {
        if (CurrentState == null) return false;

        CurrentState.GameMinute++;
        return NormalizeTime();
    }

    private void SaveDailyCheckpoint()
    {
        if (CurrentState == null ||
            CurrentState.Status != GameStatus.Running ||
            CurrentState.Difficulty == Difficulty.Hard ||
            CurrentState.Survivor.HP <= 0)
        {
            return;
        }

        _saveService.SaveDailyCheckpoint(CurrentState);
    }

    // แปลง GameMinute เกิน 1440 (24 ชั่วโมง) ให้นับเป็นวันใหม่
    private bool NormalizeTime()
    {
        if (CurrentState == null) return false;

        var startedNewDay = false;
        while (CurrentState.GameMinute >= 1440)
        {
            CurrentState.GameMinute -= 1440;
            CurrentState.DayCount++;
            startedNewDay = true;
        }

        return startedNewDay;
    }

    private void NotifyStateChanged()
    {
        if (CurrentState != null)
            OnStateChanged?.Invoke(CurrentState);
    }
}
