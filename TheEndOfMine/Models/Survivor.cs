namespace TheEndOfMine.Models;

public enum Gender { Male, Female }

public class Survivor
{
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }

    // Stats (0–100)
    public float HP { get; set; } = 100f;
    public float Hunger { get; set; } = 100f;
    public float Thirst { get; set; } = 100f;
    public float Fatigue { get; set; } = 0f;   // 0 = สดชื่น, 100 = หมดแรง

    public Inventory Inventory { get; set; } = new();
    public SkillSet Skills { get; set; } = new();

    public bool IsAlive => HP > 0;
}
