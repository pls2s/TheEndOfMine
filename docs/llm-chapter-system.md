# LLM Chapter System

เอกสารนี้สรุประบบสร้างเนื้อเรื่องด้วย LLM สำหรับเกม The End of Mine

## เป้าหมาย

เปลี่ยนจากเนื้อเรื่อง fixed เป็นเนื้อเรื่องที่สร้างใหม่ทุกครั้งที่เริ่มเกม และทำให้หนึ่งรอบเล่นได้นานขึ้นด้วยระบบ chapter

## Provider

ค่าเริ่มต้นใช้ Typhoon API เพราะเหมาะกับภาษาไทยและมี Free Tier สำหรับการใช้งานเบา ๆ

ไฟล์ตั้งค่า local:

```text
TheEndOfMine/Resources/Raw/llm.env
```

ตัวอย่างค่า:

```env
LLM_PROVIDER=typhoon
TYPHOON_API_KEY=tp-your_api_key_here
TYPHOON_MODEL=typhoon-v2.5-30b-a3b-instruct
TYPHOON_ENDPOINT=https://api.opentyphoon.ai/v1/chat/completions

IMAGE_PROVIDER=gemini
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_IMAGE_MODEL=gemini-2.5-flash-image
```

ไฟล์นี้ถูก ignore และไม่ควร commit

## โครงสร้างเกม

หนึ่งรอบมีทั้งหมด 4 chapters:

```text
Chapter 1: ตื่นรอด / ตั้งหลัก
Chapter 2: ออกหาเสบียง / เจอภัยจริง
Chapter 3: เป้าหมายหลัก / ซ่อมวิทยุหรือหาทางหนี
Chapter 4: ทางเลือกสุดท้าย / ไป safe zone หรือจบแบบอื่น
```

แต่ละ chapter มี 8 events:

```text
4 chapters x 8 events = 32 events ต่อรอบ
```

เวลาเล่นโดยประมาณ:

```text
20-45 นาทีต่อรอบ
```

## Scene Assets

ภาพฉากหลักแบ่งเป็น 2 ชุดตามเพศตัวละคร:

```text
Resources/Images/story/female/chapter
Resources/Images/story/female/event
Resources/Images/story/female/ending

Resources/Images/story/male/chapter
Resources/Images/story/male/event
Resources/Images/story/male/ending
```

ใช้รูปคนละชุดสำหรับ chapter scene, event scene และ ending scene ส่วน `Resources/Images/story/ui` ใช้ร่วมกันได้

รายการ alias ของรูปถูกเก็บที่:

```text
Resources/Raw/story_assets.json
```

LLM ต้องส่งแค่ alias เช่น `broken_bridge`, `hospital_supplies`, `water_bottle` แล้ว `StoryAssetResolver` จะ map เป็น path รูปจริงตามเพศตัวละคร เช่น:

```text
story/female/event/female_event_broken_bridge.png
story/male/event/male_event_broken_bridge.png
story/item/item_water_bottle.png
```

ตั้งชื่อไฟล์ให้ basename ไม่ซ้ำกันทั้งโปรเจกต์ เพราะ MAUI จะรวม image resources ตามชื่อไฟล์ปลายทาง แม้อยู่คนละโฟลเดอร์ก็ห้ามซ้ำกัน:

```text
Resources/Images/story/chapter/chapter_<alias>.png
Resources/Images/story/event/event_<gender>_<alias>.png
Resources/Images/story/ending/ending_<alias>.png
Resources/Images/story/item/item_<alias>.png
Resources/Images/story/ui/ui_<alias>.png

Resources/Images/story/female/chapter/female_chapter_<alias>.png
Resources/Images/story/female/event/female_event_<alias>.png
Resources/Images/story/female/ending/female_ending_<alias>.png

Resources/Images/story/male/chapter/male_chapter_<alias>.png
Resources/Images/story/male/event/male_event_<alias>.png
Resources/Images/story/male/ending/male_ending_<alias>.png
```

## Flow

1. ผู้เล่นกดเริ่มเกมใหม่
2. แอปเรียก LLM เพื่อสร้าง Chapter 1
3. LLM ส่งกลับ `storyTitle`, `chapter_alias`, `events`, `startingItems`
4. เกมบันทึก chapter และ events ลง `GameState`
5. ผู้เล่นกดออกสำรวจเพื่อเล่น event ทีละอัน
6. เมื่อเล่นครบ 8 events ของ chapter ปัจจุบัน เกมเรียก LLM เพื่อสร้าง chapter ถัดไป
7. เกม map `story_alias` ของ chapter/event/item เป็น path รูปที่มีอยู่ในแอป
8. ถ้าตั้งค่า image provider ไว้ เกมจะ generate รูป event และรูปไอเทมเฉพาะรายการที่ยังไม่มี `imagePath`
9. Chapter ถัดไปอิงสถานะล่าสุดของผู้เล่น เช่น HP, Hunger, Thirst, Fatigue, inventory, chapter ที่จบแล้ว
10. เมื่อ Chapter 4 จบและ events หมด เกมเข้าสู่สถานะจบเกม

## GameState Fields

ระบบ chapter เก็บ state หลักไว้ใน `GameState`:

```csharp
public string StoryTitle { get; set; }
public string StorySource { get; set; }
public string CurrentChapterTitle { get; set; }
public string CurrentChapterAlias { get; set; }
public string CurrentChapterImagePath { get; set; }
public int CurrentChapter { get; set; }
public int MaxChapters { get; set; }
public int EventsPerChapter { get; set; }
public List<string> CompletedChapterTitles { get; set; }
public List<GameEvent> GeneratedEvents { get; set; }
public int EventIndex { get; set; }
```

## LLM Output Shape

LLM ต้องส่ง JSON object รูปแบบนี้:

```json
{
  "storyTitle": "ชื่อ chapter",
  "chapter_alias": "broken_bridge",
  "events": [
    {
      "id": "evt_01",
      "title": "ชื่อเหตุการณ์",
      "description": "คำอธิบายเหตุการณ์",
      "story_alias": "hospital_supplies",
      "imagePrompt": "English image prompt",
      "imagePath": "",
      "choices": [
        {
          "id": "c1",
          "text": "ข้อความตัวเลือก",
          "hpEffect": 0,
          "hungerEffect": 0,
          "thirstEffect": 0,
          "fatigueEffect": 0,
          "resultText": "ผลลัพธ์หลังเลือก",
          "itemReward": {
            "id": "gen_item_id",
            "name_th": "ชื่อไอเทม",
            "name_en": "Item Name",
            "category": "Food",
            "subcategory": "generated",
            "rarity": "common",
            "weight_kg": 1,
            "trade_value": 1,
            "stackable": false,
            "max_stack": 1,
            "found_in": ["generated_story"],
            "durability_max": 1,
            "effects": {
              "hp_restore": 0,
              "hunger_restore": 0,
              "thirst_restore": 0,
              "fatigue_restore": 0
            },
            "description_th": "คำอธิบายไอเทม",
            "image_prompt": "English item image prompt",
            "image_path": "",
            "story_alias": "water_bottle"
          }
        }
      ]
    }
  ],
  "startingItems": []
}
```

Chapter 1 ต้องมี `startingItems` 3 ชิ้น ส่วน chapter หลังจากนั้น `startingItems` เป็น array ว่าง

## Validation และ Fallback

แอปจะ normalize output ก่อนใช้จริง:

- ต้องมี events ครบตาม `EventsPerChapter`
- แต่ละ event ต้องมี 2 choices
- effect ถูก clamp ให้อยู่ในช่วง `-30` ถึง `30`
- itemReward ส่วนเกินจะถูกตัดให้เหลือในช่วงที่เกมรับได้
- item ที่ field ไม่ครบจะถูกเติมค่า default
- `chapter_alias`, `event.story_alias`, `item.story_alias` ที่ไม่ตรง catalog จะถูก normalize หรือเลือก fallback จาก catalog
- event/item/chapter จะถูกเติม `imagePath` จาก asset ที่มีอยู่แล้วก่อน
- event/item ที่ไม่มี image prompt จะถูกเติม prompt default
- ถ้าไม่มี asset path และ generate รูปสำเร็จ path จะถูกบันทึกใน `imagePath` หรือ `image_path`

ถ้าไม่มี API key, API error, JSON ไม่ถูกต้อง หรือ events ไม่ครบ เกมจะใช้ fallback content ภายในแอปแทน เพื่อให้เริ่มเกมได้เสมอ

## Test Script

ทดสอบ generation โดยไม่ต้องเปิด frontend:

```bash
./scripts/test-llm-content.sh "ทดสอบ" Female
```

ผลลัพธ์จะถูก validate และบันทึกที่:

```text
tmp/generated-story.json
```

`tmp/` ถูก ignore และไม่ควร commit

## ไฟล์ที่เกี่ยวข้อง

```text
TheEndOfMine/Services/LlmGameContentService.cs
TheEndOfMine/Services/StoryAssetResolver.cs
TheEndOfMine/Services/GameEngine.cs
TheEndOfMine/Models/GameState.cs
TheEndOfMine/Models/GeneratedGameContent.cs
TheEndOfMine/Models/StoryAssetCatalog.cs
TheEndOfMine/Views/IntroPage.xaml.cs
scripts/test-llm-content.sh
TheEndOfMine/Resources/Raw/llm.env.example
TheEndOfMine/Resources/Raw/story_assets.json
```
