# The End of Mine 🎮

เกม Survival Odyssey แบบ text-based บน .NET MAUI — เอาตัวรอดในโลกหลังหายนะ

## 📖 เกี่ยวกับโปรเจค

**The End of Mine** เป็นเกมเอาตัวรอดที่ผู้เล่นต้องบริหารทรัพยากร (HP, ความหิว, ความกระหาย, ความเหนื่อย) สำรวจโลกภายนอก เผชิญเหตุการณ์สุ่ม และตัดสินใจเพื่อเอาชีวิตรอดให้นานที่สุด

> รายงานนี้เป็นส่วนหนึ่งของวิชา **CS356 Mobile Application Development I**
> ภาคเรียนที่ 2 ปีการศึกษา 2568
> สาขาวิทยาการคอมพิวเตอร์ คณะเทคโนโลยีสารสนเทศและนวัตกรรม มหาวิทยาลัยกรุงเทพ

## 🎮 Game Features

- **สร้างตัวละคร** — เลือกเพศ ตั้งชื่อ Survivor
- **3 ระดับความยาก** — Easy (Respawn) / Normal / Hard (Permadeath)
- **ระบบเวลา** — 1 วินาทีจริง = 1 นาทีในเกม
- **Status Management** — HP, Hunger, Thirst, Fatigue ลดลงตามเวลา
- **Random Events (EventPopup)** — เหตุการณ์สุ่มพร้อม 2 ตัวเลือกที่ส่งผลต่อสถานะ
- **Inventory System (InventoryPage)** — กระเป๋าสัมภาระ 4×4 (16 ช่อง) แตะดูรายละเอียดได้
- **Save/Load** — บันทึกและโหลด checkpoint ผ่าน SQLite

## 🖼️ UI / Asset Convention

หน้า popup ในเกมใช้ asset เดียวกันเพื่อความ consistent:

| Asset | ใช้ทำอะไร |
|-------|-----------|
| `card.png` | กรอบการ์ดของ EventPopup (ชื่อ event / รูป / คำบรรยาย) |
| `btn.png` | พื้นหลังปุ่ม (choice ใน EventPopup, ปุ่ม CLOSE INVENTORY) |
| `backpack.png` | พื้นหลังกระเป๋า InventoryPage (4×4 grid + detail ในภาพเดียว) |
| `hp_icon.png` / `food_icon.png` / `water_icon.png` / `stamina_icon.png` | ไอคอน status + ใช้เป็น fallback ของไอเทมประเภทเดียวกัน |

**Layout pattern**: ขนาด card / ปุ่ม / ช่อง inventory เป็น **px ตายตัวทั้งหมด** เพื่อให้หน้าตาเหมือนกันทุกเหตุการณ์ ไม่ขึ้นกับความยาวของข้อมูล (ดู `CLAUDE.md` ส่วน "Layout convention")

## 🛠️ Tech Stack

| Technology | Usage |
|-----------|-------|
| .NET 9 MAUI | Cross-platform framework |
| C# | Programming language |
| SQLite | Local database (save/load) |
| JSON | Event + Item data storage |

## 📱 Supported Platforms

- Android
- iOS
- macOS (Catalyst)
- Windows

## 🏗️ Project Structure

```
TheEndOfMine/
├── Models/                    # Data classes
│   ├── Survivor.cs            # ผู้เล่น (HP / Hunger / Thirst / Fatigue / Inventory / Skills)
│   ├── GameState.cs           # state เกมปัจจุบัน
│   ├── Item.cs                # ItemDatabase / Item / ItemEffects (+ IconPath)
│   ├── Inventory.cs           # 4×4 grid (16 slots)
│   ├── SkillSet.cs
│   ├── GameEvent.cs           # event + choices (+ ImagePath)
│   └── StoryTree.cs
├── Views/                     # UI Pages
│   ├── IntroPage.xaml         # สร้างตัวละคร
│   ├── DifficultyPage.xaml    # เลือกความยาก
│   ├── HomePage.xaml          # หน้าจอหลัก (template, ยังไม่ใช้)
│   ├── EventPopup.xaml        # popup เหตุการณ์สุ่ม
│   ├── InventoryPage.xaml     # กระเป๋าสัมภาระ
│   └── GameOverPage.xaml      # จบเกม
├── ViewModels/
│   └── MainViewModel.cs       # state + commands ของหน้า MainPage
├── Services/                  # Business Logic
│   ├── GameEngine.cs          # game loop หลัก
│   ├── EventService.cs        # โหลด events.json
│   ├── ItemService.cs         # โหลด items.json
│   ├── DifficultyService.cs
│   └── SaveService.cs
├── Data/
│   └── GameDatabase.cs        # SQLite save/load
├── MainPage.xaml              # หน้าจอเกมหลัก (ปุ่ม GO OUTSIDE / REST / INVENTORY)
└── Resources/
    ├── Images/                # card.png / btn.png / backpack.png / icons
    └── Raw/
        ├── events.json
        ├── items.json
        ├── story_tree.json
        └── *.mp4              # background video
```

## 🔁 Navigation Flow

```
IntroPage → DifficultyPage → MainPage ⇄ EventPopup        (modal)
                                ⇅
                           InventoryPage                  (modal)
```

- **EventPopup** เปิดเมื่อกด **GO OUTSIDE** ใน MainPage (ผ่าน `MainViewModel.GoOutsideCommand`)
- **InventoryPage** เปิดเมื่อกด **INVENTORY** (ปุ่มภาพมุมขวาบน)
- ทั้งคู่เปิดด้วย `PushModalAsync(animated: false)` ปิดด้วย `PopModalAsync(animated: false)`
- กลับมา MainPage **ไม่เจอหน้าโหลด** (มี flag `_hasLoadedOnce`)

## 🧪 Demo Inventory (สำหรับทดสอบ layout)

เปิด INVENTORY ครั้งแรกที่ยังไม่มีไอเทมจริง — จะแสดง **demo 16 ช่อง** อัตโนมัติ (weapon / food / drink / medicine / rest)

ปิด demo เมื่อระบบเก็บไอเทมจริงพร้อมแล้ว: `MainPage.xaml.cs` → เปลี่ยน `useDemoIfEmpty: true` → `false`

## 🚀 Build & Run

```bash
# Android
dotnet build -t:Run -f net9.0-android

# iOS Simulator
dotnet build -t:Run -f net9.0-ios

# macOS
dotnet build -t:Run -f net9.0-maccatalyst

# Windows
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

## 👥 สมาชิกในกลุ่ม

| ชื่อ | รหัสนักศึกษา |
|------|-------------|
| นาย ชัยวัฒน์ บรรลือศักดิ์ | 1660704337 |
| นางสาว พัชราภรณ์ สกุลณีย์ | 1660705417 |
| นางสาว ศุภมาส จิ้วงาม | 1660707231 |
| นาย พีรวุฒิ นุชเกิด | 1660707660 |
| นาย พชร ต่อโชติ | 1660707702 |

## 📋 อาจารย์ที่ปรึกษา

อาจารย์สราวุธิ ราษฎร์นิยม
