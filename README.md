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
- **Chapter Progression** — เกมแบ่งเป็น 4 chapters และ generate chapter ถัดไปเมื่อเล่นจบบทก่อนหน้า
- **Inventory System** — กระเป๋าสัมภาระ 4x4 (16 ช่อง)
- **Save/Load** — บันทึกและโหลด checkpoint

## 🛠️ Tech Stack

| Technology | Usage |
|-----------|-------|
| .NET 9 MAUI | Cross-platform framework |
| C# | Programming language |
| SQLite | Local database (save/load) |
| JSON | Event data storage |
| Typhoon / OpenAI-compatible LLM API | Generate new story/events/items on new game |

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

- ชื่อ chapter แรกของรอบนั้น
- เหตุการณ์ 8 เหตุการณ์ต่อ chapter
- ตัวเลือกพร้อมผลกระทบต่อ HP / Hunger / Thirst / Fatigue
- ไอเทมเริ่มต้น 3 ชิ้น
- ไอเทมรางวัลจากบางตัวเลือก

รอบเกมหนึ่งมีทั้งหมด 4 chapters:

```text
Chapter 1: ตื่นรอด / ตั้งหลัก
Chapter 2: ออกหาเสบียง / เจอภัยจริง
Chapter 3: เป้าหมายหลัก / ซ่อมวิทยุหรือหาทางหนี
Chapter 4: ทางเลือกสุดท้าย / ไป safe zone หรือจบแบบอื่น
```

เมื่อผู้เล่นเล่นครบ 8 events ของ chapter ปัจจุบัน เกมจะเรียก LLM เพื่อ generate chapter ถัดไปโดยอิงสถานะล่าสุด เช่น HP, Hunger, Thirst, Fatigue และไอเทมใน inventory

ถ้าตั้งค่า `GEMINI_API_KEY` ไว้ ระบบจะ generate รูปประกอบของ events และรูปไอเทมของ chapter นั้นไว้พร้อมกัน แล้วบันทึก path ลง `imagePath` / `image_path` ของข้อมูลที่ generate ได้ ถ้าไม่ตั้งค่า key เกมจะยังสร้างเนื้อเรื่องและเก็บ image prompt ไว้ก่อน

รูปที่มีอยู่แล้วในโปรเจกต์ถูกจัดผ่าน asset catalog:

```text
TheEndOfMine/Resources/Raw/story_assets.json
```

LLM จะเลือก `chapter_alias`, `event.story_alias`, และ `item.story_alias` จาก catalog นี้ แล้วเกมจะ map เป็น path รูปจริงตามเพศตัวละคร เช่น `story/female/event/female_event_broken_bridge.png` หรือ `story/item/item_water_bottle.png` ถ้ามี asset path แล้ว ระบบจะใช้รูปในเกมก่อนและไม่ generate รูปใหม่ทับ

ตั้งค่า key ในไฟล์ env ที่ไม่ถูก commit:

```bash
cp TheEndOfMine/Resources/Raw/llm.env.example TheEndOfMine/Resources/Raw/llm.env
```

แก้ค่าใน `TheEndOfMine/Resources/Raw/llm.env`:

```env
LLM_PROVIDER=typhoon
TYPHOON_API_KEY=tp-your_api_key_here
TYPHOON_MODEL=typhoon-v2.5-30b-a3b-instruct
TYPHOON_ENDPOINT=https://api.opentyphoon.ai/v1/chat/completions

IMAGE_PROVIDER=gemini
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_IMAGE_MODEL=gemini-2.5-flash-image
```

Typhoon มี Free Tier สำหรับการใช้งานเบา ๆ และเป็น OpenAI-compatible API เหมาะกับการ prototype เกมภาษาไทย ถ้าต้องการกลับไปใช้ OpenAI ให้เปลี่ยน env เป็น:

```env
LLM_PROVIDER=openai
OPENAI_API_KEY=sk-your_api_key_here
OPENAI_MODEL=gpt-5.4-mini
OPENAI_ENDPOINT=https://api.openai.com/v1/responses
```

ถ้าไม่ได้ตั้งค่า key หรือเรียก LLM ไม่สำเร็จ เกมจะ fallback ไปสร้างเนื้อเรื่องและไอเทมแบบสุ่มภายในแอป เพื่อให้ยังเริ่มเกมใหม่ได้ตามปกติ

ทดสอบ LLM generation โดยไม่ต้องเปิด frontend:

```bash
./scripts/test-llm-content.sh "ชื่อผู้รอดชีวิต" Female
```

ผลลัพธ์ chapter แรกจะถูก validate และบันทึกไว้ที่ `tmp/generated-story.json`

รายละเอียดระบบ chapter และ LLM อยู่ใน `docs/llm-chapter-system.md`

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
