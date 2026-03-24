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
- **Random Events** — เหตุการณ์สุ่มพร้อมตัวเลือกที่ส่งผลต่อสถานะ
- **Inventory System** — กระเป๋าสัมภาระ 4x4 (16 ช่อง)
- **Save/Load** — บันทึกและโหลด checkpoint

## 🛠️ Tech Stack

| Technology | Usage |
|-----------|-------|
| .NET 9 MAUI | Cross-platform framework |
| C# | Programming language |
| SQLite | Local database (save/load) |
| JSON | Event data storage |

## 📱 Supported Platforms

- Android
- iOS
- macOS (Catalyst)

## 🏗️ Project Structure

```
TheEndOfMine/
├── Models/          # Data classes
│   ├── Survivor.cs
│   ├── GameState.cs
│   ├── Item.cs
│   ├── Inventory.cs
│   ├── SkillSet.cs
│   └── GameEvent.cs
├── Views/           # UI Pages
│   ├── IntroPage.xaml         # สร้างตัวละคร
│   ├── DifficultyPage.xaml    # เลือกความยาก
│   ├── HomePage.xaml          # หน้าจอหลัก
│   ├── EventPopup.xaml        # เหตุการณ์สุ่ม
│   ├── InventoryPage.xaml     # กระเป๋าสัมภาระ
│   └── GameOverPage.xaml      # จบเกม
├── Services/        # Business Logic
│   ├── GameEngine.cs
│   ├── EventService.cs
│   ├── DifficultyService.cs
│   └── SaveService.cs
├── Data/            # Database
│   └── GameDatabase.cs
└── Resources/
    └── Raw/events.json        # Event data
```

## 🚀 Build & Run

```bash
# Android
dotnet build -t:Run -f net9.0-android

# iOS Simulator
dotnet build -t:Run -f net9.0-ios

# macOS
dotnet build -t:Run -f net9.0-maccatalyst
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
