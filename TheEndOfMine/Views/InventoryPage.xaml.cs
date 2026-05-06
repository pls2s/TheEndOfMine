using TheEndOfMine.Models;

namespace TheEndOfMine.Views;

/// <summary>
/// หน้า InventoryPage — กระเป๋าสัมภาระ (4x4 = 16 ช่อง)
///
/// วิธีใช้ (จากฝั่งผู้เรียก):
///   var page = new InventoryPage(_vm.CurrentInventory);
///   await Navigation.PushModalAsync(page, animated: false);
///
/// องค์ประกอบ:
///   1. SlotsGrid     — ตาราง 4x4 ของ slot (สร้างจาก code-behind ตาม Inventory.Slots)
///   2. DetailCard    — การ์ดรายละเอียด (โผล่ตอนเลือก slot ที่มีไอเทม)
///   3. CloseBtn      — ปุ่ม CLOSE INVENTORY (ปิด popup)
/// </summary>
public partial class InventoryPage : ContentPage
{
    private const int Rows = 4;
    private const int Cols = 4;

    // ใช้เมื่อไอเทมไม่มี IconPath
    private const string DefaultIcon = "hp_icon.png";

    // animation ของปุ่ม
    private const double PressScale = 0.93;
    private const uint   PressDurationMs = 80;

    // สีกรอบ slot ปกติ vs ตอนเลือก
    private static readonly Color SlotBorderNormal   = Color.FromArgb("#55000000");
    private static readonly Color SlotBorderSelected = Color.FromArgb("#FFD4A017");

    private Inventory? _inventory;

    // เก็บ Border ของแต่ละ slot ไว้เปลี่ยนสีกรอบเวลาเลือก
    private readonly Border?[] _slotBorders = new Border?[Rows * Cols];

    private int _selectedIndex = -1;

    public InventoryPage()
    {
        InitializeComponent();
        BuildEmptySlots();
    }

    /// <summary>สร้าง popup พร้อมข้อมูล inventory ที่จะแสดง</summary>
    /// <param name="inventory">inventory จริงของผู้เล่น</param>
    /// <param name="useDemoIfEmpty">true = ถ้า inventory ว่างให้โชว์ demo 16 ช่อง (สำหรับ test layout)</param>
    public InventoryPage(Inventory? inventory, bool useDemoIfEmpty = false) : this()
    {
        if (useDemoIfEmpty && (inventory == null || !inventory.GetItems().Any()))
            inventory = BuildDemoInventory();

        SetInventory(inventory);
    }

    /// <summary>เปลี่ยน inventory ที่แสดงภายหลังก็ได้</summary>
    public void SetInventory(Inventory? inventory)
    {
        _inventory = inventory;
        Refresh();
    }

    // ---- Slot building ----

    private void BuildEmptySlots()
    {
        SlotsGrid.Children.Clear();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                int index = row * Cols + col;

                var icon = new Image
                {
                    Aspect = Aspect.AspectFit,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(6),
                    IsVisible = false,
                };

                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                    Stroke = SlotBorderNormal,
                    StrokeThickness = 1,
                    BackgroundColor = Color.FromArgb("#22000000"),
                    Padding = 0,
                    Content = icon,
                };

                int capturedIndex = index;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnSlotTapped(capturedIndex);
                border.GestureRecognizers.Add(tap);

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                SlotsGrid.Children.Add(border);

                _slotBorders[index] = border;
            }
        }
    }

    // เติมรูปไอเทมลงในแต่ละ slot ตาม Inventory ปัจจุบัน
    private void Refresh()
    {
        for (int i = 0; i < _slotBorders.Length; i++)
        {
            var border = _slotBorders[i];
            if (border == null) continue;
            if (border.Content is not Image icon) continue;

            var item = GetItemAt(i);
            if (item == null)
            {
                icon.IsVisible = false;
                icon.Source = null;
            }
            else
            {
                icon.IsVisible = true;
                icon.Source = string.IsNullOrWhiteSpace(item.IconPath)
                    ? DefaultIcon
                    : item.IconPath;
            }
        }

        // ล้างการเลือกเมื่อ refresh
        ClearSelection();
    }

    private Item? GetItemAt(int index)
    {
        if (_inventory == null) return null;
        if (index < 0 || index >= _inventory.Slots.Count) return null;
        return _inventory.Slots[index];
    }

    // ---- Slot tap ----

    private async void OnSlotTapped(int index)
    {
        var border = _slotBorders[index];
        if (border != null) await PressAnimation(border);

        var item = GetItemAt(index);
        if (item == null)
        {
            // กดช่องว่าง → ซ่อนการ์ดรายละเอียด
            ClearSelection();
            return;
        }

        SelectSlot(index, item);
    }

    private void SelectSlot(int index, Item item)
    {
        // คืนกรอบสีปกติให้ slot เก่า
        if (_selectedIndex >= 0 && _slotBorders[_selectedIndex] is { } prev)
            prev.Stroke = SlotBorderNormal;

        _selectedIndex = index;

        // ตั้งกรอบไฮไลท์ slot ใหม่
        if (_slotBorders[index] is { } cur)
            cur.Stroke = SlotBorderSelected;

        // เติมข้อมูลในการ์ดรายละเอียด
        DetailNameLabel.Text = string.IsNullOrEmpty(item.NameEn) ? item.NameTh : item.NameEn.ToUpper();
        DetailRarityLabel.Text = $"({Capitalize(item.Rarity)})";
        DetailStatsLabel.Text = BuildStatsLine(item);
        DetailCard.IsVisible = true;
    }

    private void ClearSelection()
    {
        if (_selectedIndex >= 0 && _slotBorders[_selectedIndex] is { } prev)
            prev.Stroke = SlotBorderNormal;

        _selectedIndex = -1;
        DetailCard.IsVisible = false;
    }

    // สร้างบรรทัด stats ตามประเภทไอเทม (เลือกจุดเด่นที่สุดมาแสดง)
    private static string BuildStatsLine(Item item)
    {
        var fx = item.Effects;
        if (fx == null) return string.Empty;

        // อาวุธ: Damage min-max
        if (fx.DmgMin.HasValue || fx.DmgMax.HasValue)
            return $"Damage {(int)(fx.DmgMin ?? 0)}-{(int)(fx.DmgMax ?? 0)}";

        // ยา: HP restore
        if (fx.HpRestore.HasValue && fx.HpRestore.Value != 0)
            return $"HP +{(int)fx.HpRestore.Value}";

        // อาหาร: Hunger restore
        if (fx.HungerRestore.HasValue && fx.HungerRestore.Value != 0)
            return $"Hunger +{(int)fx.HungerRestore.Value}";

        // น้ำดื่ม: Thirst restore
        if (fx.ThirstRestore.HasValue && fx.ThirstRestore.Value != 0)
            return $"Thirst +{(int)fx.ThirstRestore.Value}";

        // ของพักผ่อน: Fatigue restore
        if (fx.FatigueRestore.HasValue && fx.FatigueRestore.Value != 0)
            return $"Fatigue -{(int)fx.FatigueRestore.Value}";

        return string.Empty;
    }

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1).ToLower();
    }

    // ---- Close ----

    private async void OnCloseTapped(object? sender, TappedEventArgs e)
    {
        if (sender is View v) await PressAnimation(v);
        await Navigation.PopModalAsync(animated: false);
    }

    // animation ตอนกด: หดเล็กน้อย → คืนสู่ขนาดเดิม
    private static async Task PressAnimation(View view)
    {
        await view.ScaleTo(PressScale, PressDurationMs, Easing.CubicIn);
        await view.ScaleTo(1.0,        PressDurationMs, Easing.CubicOut);
    }

    // ============================================================
    // DEMO INVENTORY (สำหรับ test เท่านั้น)
    // ลบหรือปิด useDemoIfEmpty=false เมื่อระบบเก็บไอเทมจริงพร้อมแล้ว
    // ============================================================
    private static Inventory BuildDemoInventory()
    {
        var inv = new Inventory(rows: 4);

        // 16 ช่อง: weapon / food / drink / medicine / tool ผสมกัน
        var demo = new Item[]
        {
            MakeWeapon ("wpn_0001", "Kitchen Knife",  "common",   10, 15, "icon_game.png"),
            MakeWeapon ("wpn_0002", "Pocket Knife",   "common",    8, 12, "icon_game.png"),
            MakeWeapon ("wpn_0003", "Crowbar",        "uncommon", 15, 20, "icon_game.png"),
            MakeWeapon ("wpn_0004", "Baseball Bat",   "uncommon", 18, 24, "icon_game.png"),

            MakeFood   ("fd_0001",  "Canned Beans",   "common",   25, "food_icon.png"),
            MakeFood   ("fd_0002",  "Energy Bar",     "uncommon", 15, "food_icon.png"),
            MakeFood   ("fd_0003",  "Apple",          "common",   10, "food_icon.png"),
            MakeFood   ("fd_0004",  "Beef Jerky",     "rare",     30, "food_icon.png"),

            MakeDrink  ("dk_0001",  "Water Bottle",   "common",   30, "water_icon.png"),
            MakeDrink  ("dk_0002",  "Soda Can",       "common",   20, "water_icon.png"),
            MakeDrink  ("dk_0003",  "Sport Drink",    "uncommon", 35, "water_icon.png"),

            MakeMed    ("md_0001",  "Bandage",        "uncommon", 20, "hp_icon.png"),
            MakeMed    ("md_0002",  "Painkiller",     "rare",     35, "hp_icon.png"),
            MakeMed    ("md_0003",  "First Aid Kit",  "rare",     50, "hp_icon.png"),

            MakeRest   ("rs_0001",  "Sleeping Bag",   "rare",     40, "stamina_icon.png"),
            MakeWeapon ("wpn_0005", "Flare Gun",      "rare",     25, 40, "icon_game.png"),
        };

        for (int i = 0; i < demo.Length && i < inv.Capacity; i++)
            inv.Slots[i] = demo[i];

        return inv;
    }

    private static Item MakeWeapon(string id, string name, string rarity, int min, int max, string icon) =>
        new()
        {
            Id = id, NameEn = name, Category = "weapon", Rarity = rarity,
            IconPath = icon,
            Effects = new ItemEffects { DmgMin = min, DmgMax = max }
        };

    private static Item MakeFood(string id, string name, string rarity, int restore, string icon) =>
        new()
        {
            Id = id, NameEn = name, Category = "food", Rarity = rarity,
            IconPath = icon,
            Effects = new ItemEffects { HungerRestore = restore }
        };

    private static Item MakeDrink(string id, string name, string rarity, int restore, string icon) =>
        new()
        {
            Id = id, NameEn = name, Category = "drink", Rarity = rarity,
            IconPath = icon,
            Effects = new ItemEffects { ThirstRestore = restore }
        };

    private static Item MakeMed(string id, string name, string rarity, int hp, string icon) =>
        new()
        {
            Id = id, NameEn = name, Category = "medical", Rarity = rarity,
            IconPath = icon,
            Effects = new ItemEffects { HpRestore = hp }
        };

    private static Item MakeRest(string id, string name, string rarity, int fatigue, string icon) =>
        new()
        {
            Id = id, NameEn = name, Category = "tool", Rarity = rarity,
            IconPath = icon,
            Effects = new ItemEffects { FatigueRestore = fatigue }
        };
}
