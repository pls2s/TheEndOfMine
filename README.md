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
- **LLM Generated Story** — เริ่มเกมใหม่แต่ละครั้งจะสร้างเนื้อเรื่อง เหตุการณ์ และไอเทมใหม่ด้วย LLM
- **Inventory System** — กระเป๋าสัมภาระ 4x4 (16 ช่อง)
- **Save/Load** — บันทึกและโหลด checkpoint

## 🛠️ Tech Stack

| Technology | Usage |
|-----------|-------|
| .NET 9 MAUI | Cross-platform framework |
| C# | Programming language |
| SQLite | Local database (save/load) |
| JSON | Event data storage |
| OpenAI-compatible LLM API | Generate new story/events/items on new game |

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

### LLM Setup

เกมจะพยายามเรียก LLM ตอนผู้เล่นกดเริ่มเกมใหม่ เพื่อสร้าง:

- ชื่อเรื่องของรอบนั้น
- เหตุการณ์ 8 เหตุการณ์
- ตัวเลือกพร้อมผลกระทบต่อ HP / Hunger / Thirst / Fatigue
- ไอเทมเริ่มต้น 3 ชิ้น
- ไอเทมรางวัลจากบางตัวเลือก

ตั้งค่า API key ได้ผ่าน environment variable:

```bash
export OPENAI_API_KEY="your_api_key"
export OPENAI_MODEL="gpt-4o-mini"
```

หรือสร้างไฟล์ local config ที่ไม่ถูก commit:

```json
// TheEndOfMine/Resources/Raw/llm_config.local.json
{
  "OPENAI_API_KEY": "your_api_key",
  "OPENAI_MODEL": "gpt-4o-mini"
}
```

ถ้าไม่ได้ตั้งค่า key หรือเรียก LLM ไม่สำเร็จ เกมจะ fallback ไปสร้างเนื้อเรื่องและไอเทมแบบสุ่มภายในแอป เพื่อให้ยังเริ่มเกมใหม่ได้ตามปกติ

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
