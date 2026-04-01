namespace TheEndOfMine.Models;

public class EventChoice
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    // ผลกระทบต่อ stats (บวก = ดี, ลบ = เสีย)
    public float HpEffect { get; set; }
    public float HungerEffect { get; set; }
    public float ThirstEffect { get; set; }
    public float FatigueEffect { get; set; }

    public Item? ItemReward { get; set; }
    public string ResultText { get; set; } = string.Empty;
}

public class GameEvent
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<EventChoice> Choices { get; set; } = new();
}
