namespace TheEndOfMine.Models;

public enum Difficulty { Easy, Normal, Hard }

public enum GameStatus { NotStarted, Running, Paused, GameOver }

public class GameState
{
    public Survivor Survivor { get; set; } = new();
    public Difficulty Difficulty { get; set; }
    public GameStatus Status { get; set; } = GameStatus.NotStarted;

    public string StoryTitle { get; set; } = string.Empty;
    public string StorySource { get; set; } = string.Empty;
    public string CurrentChapterTitle { get; set; } = string.Empty;
    public string CurrentChapterAlias { get; set; } = string.Empty;
    public string CurrentChapterImagePath { get; set; } = string.Empty;
    public int CurrentChapter { get; set; } = 1;
    public int MaxChapters { get; set; } = 4;
    public int EventsPerChapter { get; set; } = 8;
    public List<string> CompletedChapterTitles { get; set; } = new();
    public List<GameEvent> GeneratedEvents { get; set; } = new();
    public string StoryArcSummary { get; set; } = string.Empty;
    public List<StoryMemoryEntry> StoryMemory { get; set; } = new();
    public bool IsStoryEnding { get; set; }
    public string GameOverTitle { get; set; } = string.Empty;
    public string GameOverDetail { get; set; } = string.Empty;
    public string DeathCause { get; set; } = string.Empty;
    public string EndingImagePath { get; set; } = string.Empty;

    public int DayCount { get; set; } = 1;
    public int GameMinute { get; set; } = 0;   // 0–1439 (24 ชั่วโมง = 1440 นาที)
    public float Noise { get; set; } = 0f;     // 0 = เงียบ, 100 = ดึงดูดอันตราย
    public float Infection { get; set; } = 0f; // 0 = ปลอดภัย, 100 = ติดเชื้อหนัก

    // ตำแหน่ง event ปัจจุบันใน story (บันทึกไว้ใน save เพื่อเล่นต่อได้)
    public int EventIndex { get; set; } = 0;

    // คำนวณเวลาในเกมเพื่อแสดง UI
    public int Hour => GameMinute / 60;
    public int Minute => GameMinute % 60;
    public string TimeDisplay => $"Day {DayCount}  {Hour:D2}:{Minute:D2}";
}

public class StoryMemoryEntry
{
    public int Chapter { get; set; }
    public int Day { get; set; }
    public string Time { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string ChoiceText { get; set; } = string.Empty;
    public string ResultText { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
