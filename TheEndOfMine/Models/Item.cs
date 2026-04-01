namespace TheEndOfMine.Models;

public enum ItemType { Food, Water, Medicine, Weapon, Tool, Misc }

public class Item
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    // ผลเมื่อใช้ไอเทม
    public float HpRestore { get; set; }
    public float HungerRestore { get; set; }
    public float ThirstRestore { get; set; }
    public float FatigueRestore { get; set; }

    public bool IsUsable => HpRestore != 0 || HungerRestore != 0 || ThirstRestore != 0 || FatigueRestore != 0;
}
