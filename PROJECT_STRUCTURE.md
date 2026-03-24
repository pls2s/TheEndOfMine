# Project Structure - The End of Mine

รายละเอียดว่าแต่ละไฟล์ใช้ทำอะไร เพื่อให้คนอื่นมาต่อได้ง่าย

---

## Root Files

| ไฟล์ | หน้าที่ |
|------|---------|
| `App.xaml / App.xaml.cs` | จุดเริ่มต้นของแอป สร้าง MainWindow + โหลด AppShell |
| `AppShell.xaml / AppShell.xaml.cs` | กำหนด Navigation ระหว่างหน้า (Shell routing) |
| `MauiProgram.cs` | ตั้งค่า DI container, ลงทะเบียน Services, Fonts, Pages |
| `MainPage.xaml / MainPage.xaml.cs` | หน้า default ของ MAUI template (จะถูกแทนที่ด้วย IntroPage) |
| `TheEndOfMine.csproj` | Project config, NuGet packages, target platforms |

---

## Views/ (หน้าจอ UI)

### `IntroPage.xaml / .xaml.cs`
**หน้า 1 — Intro & สร้างตัวละคร**
- แสดงฉากเปิดเมืองหลังหายนะ
- ให้ผู้เล่นเลือกเพศ (Male / Female) ด้วย 2 ไอคอน
- ช่องใส่ชื่อตัวละคร (Survivor Name)
- ปุ่ม "START ADVENTURE" → ไปหน้า DifficultyPage
- **ต้อง validate**: ชื่อต้องไม่ว่าง, ต้องเลือกเพศ

### `DifficultyPage.xaml / .xaml.cs`
**หน้า 2 — เลือกความยาก**
- แสดง 3 ปุ่ม: EASY (เขียว), NORMAL (เหลือง), HARD (แดง)
- แต่ละปุ่มมีคำอธิบายกฎ (hunger rate, damage, permadeath)
- ปุ่ม "CONFIRM" → บันทึก difficulty แล้วไปหน้า HomePage
- **ต้อง**: ส่ง difficulty ที่เลือกไปให้ DifficultyService

### `HomePage.xaml / .xaml.cs`
**หน้า 3 — หน้าจอหลัก / ห้องพัก**
- **Status Bars** (มุมซ้ายบน): HP, Hunger, Thirst, Fatigue — แถบสีแสดง real-time
- **เวลาในเกม** (ด้านล่าง): "DAY 1: 08:00 AM" — 1 real second = 1 game minute
- **ปุ่ม Pause/Stop** (มุมขวาบน)
- **ปุ่ม Inventory** (มุมขวาบน) → เปิด InventoryPage
- **ปุ่ม "GO OUTSIDE"** (ซ้าย) → trigger EventPopup สุ่มเหตุการณ์
- **ปุ่ม "REST/SLEEP"** (ขวา) → เลือก 4H หรือ 8H → ข้ามเวลา + ลด Fatigue
- **Skills panel**: แสดง EXP ของ Melee, Scavenge, Looting, Fatigue
- **ต้อง**: Timer loop อัปเดตเวลา + ลด hunger/thirst ตาม difficulty

### `EventPopup.xaml / .xaml.cs`
**หน้า 4 — ป๊อปอัพเหตุการณ์**
- พื้นหลังมืดลง (overlay)
- แสดงชื่อ event + รูปภาพ + คำบรรยาย (แทน [SURVIVOR] ด้วยชื่อจริง)
- แสดง 2 ตัวเลือก (choices) เป็นปุ่มใหญ่
- แต่ละ choice มี effect ต่อ status (HP, hunger, items, เวลา, ฯลฯ)
- **ต้อง**: โหลด event จาก EventService → แสดง → apply ผลลัพธ์กลับ GameState

### `InventoryPage.xaml / .xaml.cs`
**หน้า 5 — กระเป๋าสัมภาระ**
- ตาราง 4x4 (16 ช่อง) แสดงไอเทมทั้งหมด
- แตะไอเทม → แสดงรายละเอียด (ชื่อ, rarity, damage/effect)
- ไอเทมประเภท: อาวุธ (มีด, ฯลฯ), อาหาร, น้ำ, บาดแผล
- ใช้ไอเทม (กินอาหาร → เพิ่ม hunger) หรือทิ้งไอเทม
- ปุ่ม "CLOSE INVENTORY" → กลับ HomePage
- **ต้อง**: bind กับ Inventory model, อัปเดต GameState เมื่อใช้ไอเทม

### `GameOverPage.xaml / .xaml.cs`
**หน้า 6 — จบเกม**
- แสดงข้อความ "คุณตาย!" + รูปตัวละครนอนตาย
- แสดง difficulty ที่เล่นอยู่
- ปุ่ม "RESTART ADVENTURE" → กลับ IntroPage เริ่มใหม่
- ปุ่ม "LOAD CHECKPOINT (EASY/NORMAL Only)" → โหลด save ล่าสุด
- **Hard mode**: ลบ save ทั้งหมด (Permadeath)

---

## Models/ (Data Classes)

### `Survivor.cs`
ข้อมูลตัวละครผู้เล่น
```
- Name: string          // ชื่อตัวละคร
- Gender: string        // "Male" / "Female"
- HP: int               // พลังชีวิต (0-100)
- Hunger: int           // ความหิว (0-100, ลดตามเวลา)
- Thirst: int           // ความกระหาย (0-100, ลดตามเวลา)
- Fatigue: int          // ความเหนื่อย (0-100, ลดเมื่อนอน)
```

### `GameState.cs`
สถานะเกมปัจจุบัน
```
- CurrentDay: int       // วันที่ในเกม
- CurrentTime: TimeSpan // เวลาในเกม (08:00 AM เริ่มต้น)
- Difficulty: string    // "Easy" / "Normal" / "Hard"
- Survivor: Survivor    // ตัวละครปัจจุบัน
- Inventory: Inventory  // กระเป๋าสัมภาระ
- Skills: SkillSet      // ค่า skill ต่างๆ
- IsGameOver: bool      // เกมจบหรือยัง
```

### `Item.cs`
ไอเทมแต่ละชิ้น
```
- Id: int
- Name: string          // เช่น "Kitchen Knife"
- Type: string          // "Weapon" / "Food" / "Water" / "Medical"
- Rarity: string        // "Common" / "Uncommon" / "Rare"
- DamageMin: int        // ค่าโจมตีต่ำสุด (ถ้าเป็นอาวุธ)
- DamageMax: int        // ค่าโจมตีสูงสุด
- HungerRestore: int    // ค่าอาหารที่ฟื้น (ถ้าเป็น Food)
- ThirstRestore: int    // ค่าน้ำที่ฟื้น (ถ้าเป็น Water)
- HpRestore: int        // ค่า HP ที่ฟื้น (ถ้าเป็น Medical)
- ImagePath: string     // path รูปไอเทม
```

### `Inventory.cs`
จัดการกระเป๋าสัมภาระ
```
- Items: List<Item>     // ไอเทมทั้งหมด (max 16)
- MaxSlots: int = 16    // จำนวนช่องสูงสุด (4x4)
- AddItem(item): bool   // เพิ่มไอเทม (return false ถ้าเต็ม)
- RemoveItem(item): void
- UseItem(item): void   // ใช้ไอเทม → apply effect → ลบออก
- IsFull: bool
```

### `SkillSet.cs`
ค่า Skill/EXP ของผู้เล่น
```
- MeleeExp: int         // EXP การต่อสู้
- ScavengeExp: int      // EXP การหาของ
- LootingExp: int       // EXP การปล้น
- FatigueExp: int       // EXP ความอดทน
```

### `GameEvent.cs`
เหตุการณ์ที่เกิดขึ้นในเกม
```
- Id: string
- Title: string         // เช่น "LEAVING ROOM..."
- Description: string   // คำบรรยาย (ใช้ [SURVIVOR] แทนชื่อ)
- ImagePath: string     // รูปประกอบ
- Choice1Text: string   // ข้อความตัวเลือก 1
- Choice1Effects: Dictionary<string, int>  // ผลกระทบ (hp: -10, hunger: +5, ฯลฯ)
- Choice2Text: string
- Choice2Effects: Dictionary<string, int>
- Choice2SuccessRate: double  // โอกาสสำเร็จ (เช่น 0.5 = 50%)
```

---

## Services/ (Business Logic)

### `GameEngine.cs`
**แกนหลักของเกม** — จัดการ game loop ทั้งหมด
- เริ่มเกมใหม่ (สร้าง GameState จาก Survivor + Difficulty)
- Game timer: นับเวลาทุก 1 วินาที → +1 นาทีในเกม
- อัปเดต status ตามเวลา (hunger/thirst ลดลง, ตรวจ HP <= 0)
- จัดการ Sleep (ข้ามเวลา 4/8 ชม. → ลด fatigue)
- ตรวจ Game Over conditions
- เรียก EventService เมื่อ Go Outside

### `EventService.cs`
**จัดการเหตุการณ์สุ่ม**
- โหลด events จาก `Resources/Raw/events.json`
- สุ่มเลือก event เมื่อผู้เล่นออกนอกห้อง
- Apply ผลลัพธ์ของ choice ที่เลือก → อัปเดต GameState
- คำนวณ success/fail ของ choice (เช่น 50/50 ในการสู้)

### `DifficultyService.cs`
**จัดการค่าความยากของเกม**
- กำหนด hunger decay rate ตาม difficulty
- กำหนด damage multiplier
- กำหนด respawn / permadeath rule
- ส่งค่าให้ GameEngine ใช้ในการคำนวณ

### `SaveService.cs`
**บันทึก / โหลดเกม**
- Save GameState ลง SQLite (ผ่าน GameDatabase)
- Load checkpoint ล่าสุด
- ลบ save ทั้งหมด (สำหรับ Hard mode permadeath)
- Auto-save ทุกจุดสำคัญ (เช่น หลัง event, หลังนอน)

---

## Data/

### `GameDatabase.cs` (ปัจจุบันชื่อ GameDatabase.cs.cs — ต้องแก้)
**SQLite Database Access**
- สร้าง/เชื่อมต่อ SQLite database
- CRUD operations สำหรับ GameState, Survivor, Inventory
- ใช้ `sqlite-net-pcl` NuGet package (ต้องเพิ่ม)

---

## Resources/

### `Raw/events.json`
**ข้อมูลเหตุการณ์ในเกม** — โครงสร้างตัวอย่าง:
```json
[
  {
    "id": "zombie_encounter",
    "title": "LEAVING ROOM...",
    "description": "[SURVIVOR] กำลังออกจากห้อง... ออกมาจากห้องแล้วเจอกับ ซอมบี้ตัว นึงมันเห็นคุณและกำลังพุ่งมาหาคุณ",
    "choice1Text": "หนีกลับเข้าห้อง",
    "choice1Effects": { "time": 120 },
    "choice2Text": "ใช้มีดทำครัวสู้มัน",
    "choice2Effects": { "meleeExp": 2 },
    "choice2SuccessRate": 0.5
  }
]
```

### `Fonts/` — OpenSans-Regular.ttf, OpenSans-Semibold.ttf
### `Images/` — รูปภาพในเกม (ตัวละคร, ไอเทม, background)
### `AppIcon/` — ไอคอนแอป
### `Splash/` — หน้า splash screen

---

## Navigation Flow

```
IntroPage → DifficultyPage → HomePage ⇄ EventPopup
                                  ↕           ↓
                            InventoryPage   GameOverPage
                                              ↓
                                          IntroPage (restart)
```

---

## NuGet Packages ที่ต้องเพิ่ม

| Package | ใช้ทำอะไร |
|---------|----------|
| `sqlite-net-pcl` | SQLite database สำหรับ save/load |
| `SQLitePCLRaw.bundle_green` | SQLite provider |
| `CommunityToolkit.Maui` | Popup, Behaviors, Converters |
| `CommunityToolkit.Mvvm` | MVVM helpers (ObservableObject, RelayCommand) |

---

## ลำดับการ Implement แนะนำ

1. **Models** → สร้าง data classes ให้ครบก่อน
2. **Services** → DifficultyService → GameEngine → EventService → SaveService
3. **Views** → IntroPage → DifficultyPage → HomePage → EventPopup → InventoryPage → GameOverPage
4. **Navigation** → ตั้งค่า Shell routing ใน AppShell
5. **Data** → GameDatabase + SQLite setup
6. **events.json** → เพิ่มข้อมูล event ทั้งหมด
7. **Testing & Polish**
