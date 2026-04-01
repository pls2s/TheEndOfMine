namespace TheEndOfMine.Models;

public class Inventory
{
    public const int Columns = 4;
    public int Rows { get; private set; }
    public int Capacity => Columns * Rows;

    public List<Item?> Slots { get; set; }

    public Inventory(int rows = 4)
    {
        Rows = rows;
        Slots = new List<Item?>(new Item?[Capacity]);
    }

    public bool IsFull => Slots.All(s => s != null);

    public bool AddItem(Item item)
    {
        int index = Slots.IndexOf(null);
        if (index == -1) return false;
        Slots[index] = item;
        return true;
    }

    public bool RemoveItem(Item item)
    {
        int index = Slots.IndexOf(item);
        if (index == -1) return false;
        Slots[index] = null;
        return true;
    }

    // เพิ่มแถวใหม่ 1 แถว (4 ช่อง)
    public void ExpandRow()
    {
        Rows++;
        Slots.AddRange(new Item?[Columns]);
    }

    public IEnumerable<Item> GetItems() => Slots.Where(s => s != null)!;
}
