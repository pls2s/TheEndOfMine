# LLM Main Arc And Side Quest Design

เอกสารนี้สรุปแนวคิดระบบเนื้อเรื่อง 2 ชั้นของ The End of Mine

```text
Main Arc = เส้นเรื่องหลักของรอบนั้น
Side Quest = ภารกิจย่อยที่เกิดระหว่างทาง
```

## Goal

ให้ LLM สร้างโครงเรื่องหลักไว้ก่อน แล้วสร้าง quest ย่อยตามสถานะผู้เล่นระหว่างทาง

ระบบนี้ช่วยให้เกม:

- เล่นได้นานขึ้น
- แต่ละรอบต่างกันจริง
- side quest ผูกกับสิ่งที่ผู้เล่นเจอ
- ending คำนวณจากการตัดสินใจระหว่างทางได้

## Main Arc

Main Arc ถูกสร้างตอนเริ่มเกมใหม่พร้อม Chapter 1

ควรมีข้อมูลหลัก:

- title
- goal
- chapters 4 บท
- main objective ของแต่ละ chapter
- possible flags
- ending conditions

ตัวอย่าง:

```json
{
  "mainArc": {
    "title": "สัญญาณสุดท้าย",
    "goal": "ซ่อมวิทยุและตามหาจุดอพยพ",
    "chapters": [
      {
        "chapter": 1,
        "title": "บ้านที่ไม่ปลอดภัย",
        "mainObjective": "หาเสบียงพื้นฐานและออกจากบ้าน",
        "possibleFlags": ["found_water", "found_radio_part"]
      }
    ],
    "endingConditions": [
      "radio_repaired",
      "map_found",
      "survivor_helped",
      "supplies_low"
    ]
  }
}
```

## Side Quest

Side Quest เป็น quest สั้น ๆ ความยาว 2-4 events

ควรมีข้อมูลหลัก:

- id
- title
- trigger
- objective
- events
- rewards
- flagsSet

ตัวอย่าง:

```json
{
  "sideQuest": {
    "id": "sq_find_battery",
    "title": "แบตเตอรี่ก้อนสุดท้าย",
    "trigger": "player_has_radio && !has_battery",
    "objective": "หาแบตเตอรี่จากร้านซ่อมเครื่องใช้ไฟฟ้า",
    "events": [],
    "rewards": ["battery_pack"],
    "flagsSet": ["battery_found"]
  }
}
```

## Trigger Ideas

Side quest ควรเกิดจากสถานะจริงของเกม

```text
Thirst ต่ำมาก -> quest หาแหล่งน้ำ
Hunger ต่ำมาก -> quest หาอาหาร
HP ต่ำ -> quest หายาหรือที่พัก
มี radio แต่ไม่มี battery -> quest หาแบตเตอรี่
มี map -> quest เปิดเส้นทางลัด
ช่วย NPC -> quest คุ้มกัน NPC
มีแผลติดเชื้อ -> quest หา antiseptic
chapter 3 แล้วเสบียงต่ำ -> quest เสี่ยงค้นคลังของ
```

## Runtime Flow

```text
เริ่มเกมใหม่
-> LLM generate mainArc + Chapter 1
-> เล่น main events
-> ทุก 2-3 events หรือเมื่อ status เปลี่ยนหนัก
-> เกมประเมินว่า spawn sideQuest ได้ไหม
-> ถ้าได้ เรียก LLM generate sideQuest 2-4 events
-> เล่น sideQuest หรือข้าม
-> set flags / reward / consequence
-> กลับไป main chapter
-> chapter หมด เรียก LLM generate chapter ถัดไป โดยส่ง mainArc + flags
-> chapter 4 จบ คำนวณ ending
```

## Rules

- active side quest พร้อมกันไม่เกิน 1-2 อัน
- side quest หมดอายุเมื่อจบ chapter
- side quest ไม่ควรบล็อก main story เสมอ
- บาง side quest ควรกระทบ ending
- side quest ต้องมี reward หรือ consequence ชัดเจน

## Flags

Flags คือ memory ของ story

```text
found_radio
battery_found
radio_repaired
found_map
helped_child
abandoned_survivor
stole_supplies
infected_wound
safe_zone_known
low_supplies_warning
```

## Ending Logic

Ending ควรดูหลายปัจจัย:

- main objective สำเร็จหรือไม่
- flags จาก side quest
- HP / Hunger / Thirst / Fatigue
- item สำคัญที่มีอยู่
- การตัดสินใจทางศีลธรรม

ตัวอย่าง:

```text
Good Ending:
radio_repaired && safe_zone_known && HP > 40

Survival Ending:
safe_zone_known && supplies_low

Bad Ending:
HP <= 0 || no_safe_zone_path

Secret Ending:
helped_child && radio_repaired && found_map
```

## Future Data Shape

ถ้าจะ implement ภายหลัง อาจเพิ่มใน `GameState`:

```csharp
public MainArc? MainArc { get; set; }
public List<SideQuest> ActiveSideQuests { get; set; } = new();
public List<SideQuest> CompletedSideQuests { get; set; } = new();
public HashSet<string> StoryFlags { get; set; } = new();
```

## Prompt Context

เวลาขอให้ LLM สร้าง side quest ควรส่ง:

- main arc title
- current chapter
- current objective
- player stats
- inventory summary
- existing flags
- recent choices summary
- active side quests

คำสั่งหลัก:

```text
สร้าง side quest 2-4 events
ต้องผูกกับสถานะปัจจุบัน
ต้องมี reward หรือ consequence
ต้อง set flags อย่างน้อย 1 ตัว
ห้ามเปลี่ยนเป้าหมายหลักของ main arc
```

## Recommended Order

1. เพิ่ม `MainArc` model
2. ให้เริ่มเกม generate `mainArc + chapter 1`
3. เพิ่ม `StoryFlags` ใน `GameState`
4. เพิ่ม side quest evaluator แบบ rule-based ก่อน
5. ให้ LLM generate side quest เมื่อ rule trigger
6. เพิ่ม UI แสดง active side quest
7. ใช้ flags คำนวณ ending

ควรทำหลัง chapter-based generation เสถียรแล้ว เพราะ side quest จะพึ่ง state ของ chapter และ event progression
