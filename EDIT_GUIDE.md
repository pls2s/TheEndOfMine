# Edit Guide - The End of Mine

เอกสารนี้เป็นคู่มือว่า “ถ้าจะแก้อะไร ต้องไปแก้ไฟล์ไหน” สำหรับโปรเจกต์ .NET MAUI เกม The End of Mine

อัปเดตล่าสุดครอบคลุมระบบที่เพิ่งแก้: splash/tap to start, transition หน้าเลือกเพศ, balance ความยาก, event ใช้เวลาครึ่งวัน, status ลดหลังจบ event, drop หลายไอเทม, item reward เข้ากระเป๋า, tutorial, debug button, ending page และ inventory UI

## กฎก่อนแก้

- โค้ดหลักอยู่ใน `TheEndOfMine/`
- UI หลักใช้ XAML คู่กับ `.xaml.cs`
- Logic เกมส่วนใหญ่แยกอยู่ใน `Services/`
- State ที่ต้อง save/load อยู่ใน `Models/`
- Resource ที่ต้องถูก package เข้าแอปอยู่ใน `Resources/`
- อย่าใช้ `Frame` ใน XAML ใหม่ ให้ใช้ `Border` + `Border.StrokeShape` แทน
- หลังแก้ควร build Android ด้วยคำสั่ง:

```bash
dotnet build TheEndOfMine/TheEndOfMine.csproj -f net9.0-android --no-restore
```

หมายเหตุ:

- ตอนนี้ build Android ผ่าน แต่ยังมี warning `XamlC XC0022` เรื่อง binding ไม่มี `x:DataType` ในหลายหน้า
- MacCatalyst อาจ fail จาก Xcode mismatch ได้ ไม่เกี่ยวกับ logic เกม

## หน้าและ Flow หลัก

### Splash / Tap To Start / Intro / สร้างตัวละคร

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/SplashPage.xaml`
- `TheEndOfMine/Views/SplashPage.xaml.cs`
- `TheEndOfMine/Views/IntroPage.xaml`
- `TheEndOfMine/Views/IntroPage.xaml.cs`
- `TheEndOfMine/Resources/Images/splash_page.png`
- `TheEndOfMine/Resources/AppIcon/splash.mp4`
- `TheEndOfMine/TheEndOfMine.csproj`

ใช้แก้:

- รูป splash แรก
- หน้า tap to start
- transition จาก splash ไป intro
- ปุ่มเลือกเพศ
- transition สลับเพศชาย/หญิง
- ช่องกรอกชื่อ
- loading ตอนสร้างเกมใหม่
- การเรียก LLM เพื่อสร้าง Chapter 1

จุดสำคัญ:

- splash ใช้รูปเดียวกับหน้า tap to start เพื่อไม่ให้ภาพกระโดด
- ถ้าจะลดจอดำระหว่าง splash -> tap to start ให้ดู fade/navigation ใน `SplashPage.xaml.cs`
- `IntroPage.xaml.cs` เรียก `LlmGameContentService.GenerateNewGameAsync`
- ตอนเริ่มรอบใหม่จะลบ save เก่าใน `SaveService` และ `GameDatabase`
- ตอนเริ่มรอบใหม่จะล้าง preference `main_tutorial_seen` เพื่อให้ tutorial กลับมาแสดง
- transition จากหน้าเลือกเพศไปหน้า difficulty ใช้ fade ไม่ใช่ fade ดำ

### หน้าเลือกความยาก

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/DifficultyPage.xaml`
- `TheEndOfMine/Views/DifficultyPage.xaml.cs`
- `TheEndOfMine/Services/DifficultyService.cs`
- `TheEndOfMine/Services/GameEngine.cs`

ใช้แก้:

- UI ปุ่ม Easy / Normal / Hard
- ค่าความยากที่บันทึกลง save
- อัตราลด hunger/thirst/fatigue
- damage จากหิว/ขาดน้ำ
- ตัวคูณผลเสียจาก event

ค่าปัจจุบันใน `DifficultyService.GetDecayRates`:

- Easy: decay เบา, `DamageMultiplier = 0.55`
- Normal: ปานกลาง, `DamageMultiplier = 0.85`
- Hard: หนักกว่า, `DamageMultiplier = 1.25`

แนว balance ปัจจุบัน:

- Easy ต้องให้อภัย
- Normal ไม่ควรเกือบตายจากการออกไปครั้งแรก
- Hard ต้องเลือกดี ๆ ถึงผ่าน

## หน้าเล่นหลัก

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/MainPage.xaml`
- `TheEndOfMine/MainPage.xaml.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/Services/GameEngine.cs`

ใช้แก้:

- layout หน้าหลัก
- status panel
- ปุ่ม `GO OUTSIDE`
- ปุ่ม `REST / SLEEP`
- ปุ่ม inventory
- เมนูสามขีด
- ปุ่ม save
- ปุ่ม sound on/off
- ปุ่ม debug ซ่อน
- tutorial overlay
- loading overlay เข้า chapter
- background video ชาย/หญิง

จุดสำคัญ:

- UI binding ส่วนใหญ่ผูกกับ `MainViewModel`
- ปุ่มจริงอยู่ใน `MainPage.xaml`
- handler ของปุ่มอยู่ใน `MainPage.xaml.cs`
- logic เกมจริงอยู่ใน `GameEngine`
- ปุ่ม `GO OUTSIDE` ตอนนี้อยู่ที่ `AbsoluteLayout.LayoutBounds="0.0001, 0.35, 110, 45"`
- MainPage เดิมที่ใช้ `Frame` ถูกเปลี่ยนเป็น `Border` แล้ว

### เมนูสามขีดและปุ่ม Debug ซ่อน

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/MainPage.xaml`
- `TheEndOfMine/MainPage.xaml.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/Services/GameEngine.cs`

เมนูสามขีดมี:

- stop/resume
- save game
- sound on/off
- debug ending button `END` แบบ opacity ต่ำมาก
- debug tutorial button `TUT` แบบ opacity ต่ำมาก

การใช้งาน:

- เปิดเมนู `☰`
- ใต้ปุ่ม sound จะมีปุ่มเล็ก ๆ `END` และ `TUT`
- `END` เรียก `ForceStoryEndingForDebug` เพื่อเด้งไปหน้า ending ทันที
- `TUT` ล้าง `main_tutorial_seen` แล้วเรียก tutorial ใหม่ทันที

ใช้ตอน:

- เทสหน้า ending โดยไม่ต้องเล่นจนจบ
- เทส tutorial ซ้ำหลังเคยข้ามไปแล้ว

## ระบบ Tutorial

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/MainPage.xaml`
- `TheEndOfMine/MainPage.xaml.cs`
- `TheEndOfMine/Views/IntroPage.xaml.cs`

ใช้แก้:

- ข้อความ tutorial
- จำนวน step
- target ที่ highlight
- ปุ่ม next/skip
- flag ไม่ให้แสดงซ้ำ
- debug button เรียก tutorial ซ้ำ

จุดสำคัญ:

- key ที่ใช้จำว่าเคยเห็นแล้วคือ `main_tutorial_seen`
- step อยู่ใน `ShowTutorialIfNeededAsync`
- target ปัจจุบัน:
  - `StatusPanel`
  - `GoOutsideButton`
  - `RestPanel`
  - `InventoryButton`
  - `MenuButton`
- ถ้าเพิ่ม UI ใหม่แล้วอยากให้ tutorial ชี้ ต้องตั้ง `x:Name` ใน XAML ก่อน
- เริ่มเกมใหม่ใน `IntroPage.xaml.cs` จะ `Preferences.Remove("main_tutorial_seen")`
- ถ้าอยากบังคับให้ tutorial ขึ้นในรอบเดิม ให้กด debug `TUT` ในเมนูสามขีด

## ระบบ Save / Load / Autosave

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Services/SaveService.cs`
- `TheEndOfMine/Data/GameDatabase.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/AppShell.xaml.cs`
- `TheEndOfMine/MainPage.xaml.cs`

ใช้แก้:

- save checkpoint
- daily checkpoint
- autosave
- resume เกมเมื่อเปิดแอป
- save ตอนออกจากหน้าเกม
- path ของไฟล์ save
- behavior ตอน game over

จุดสำคัญ:

- `SaveService` เก็บ `checkpoint.json` และ `daily_checkpoint.json`
- `GameDatabase` เก็บ `survivor.json`, `gamestate.json`, `inventory.json`
- ทั้งสองระบบใช้ `FileSystem.AppDataDirectory`
- `AppShell.xaml.cs` ตรวจ save แล้วเข้า `MainPage` อัตโนมัติ
- autosave อยู่ใน `MainViewModel.QueueAutoSave`
- ปุ่ม save เรียก `MainViewModel.SaveGameAsync`
- Hard mode ลบ save เมื่อ game over
- Easy/Normal มี daily checkpoint ถ้ามี save ที่ยังใช้ได้

## ระบบเสียง

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Services/AudioFeedbackService.cs`
- `TheEndOfMine/MainPage.xaml`
- `TheEndOfMine/MainPage.xaml.cs`
- `TheEndOfMine/App.xaml.cs`
- `TheEndOfMine/Resources/Audio/`

ใช้แก้:

- background music
- sound effect ปุ่ม
- sound effect choice
- mute/unmute
- ปุ่มเปิดปิดเสียงในเมนูสามขีด

จุดสำคัญ:

- mute เก็บใน `Preferences` key `audio_muted`
- Android ใช้ `Android.Media.MediaPlayer`
- ไฟล์เสียงต้องเป็น `MauiAsset` ผ่าน `Resources/Audio/**` ใน `.csproj`

## ระบบ LLM / Typhoon / Fallback Story

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Services/LlmGameContentService.cs`
- `TheEndOfMine/Resources/Raw/llm.env`
- `TheEndOfMine/Resources/Raw/llm.env.example`
- `TheEndOfMine/Resources/Raw/story_tree.json`
- `TheEndOfMine/Resources/Raw/events.json`
- `TheEndOfMine/Resources/Raw/story_assets.json`
- `TheEndOfMine/Models/GeneratedGameContent.cs`
- `TheEndOfMine/Models/GameEvent.cs`

ใช้แก้:

- prompt ที่ส่งให้ Typhoon/OpenAI
- model / endpoint / provider
- retry timeout
- schema JSON ที่ LLM ต้องตอบ
- fallback ไป story_tree หรือ events
- normalize เนื้อเรื่อง ไอเทม และรูป
- item drop balance
- item reward หลายชิ้น

จุดสำคัญ:

- provider default คือ Typhoon
- default endpoint คือ `https://api.opentyphoon.ai/v1/chat/completions`
- default model คือ `typhoon-v2.5-30b-a3b-instruct`
- config อยู่ใน `Resources/Raw/llm.env`
- ถ้าไม่มี `TYPHOON_API_KEY` หรือ `LLM_API_KEY` จะ fallback ทันที
- ถ้า Typhoon ล่ม ระบบ fallback ตามลำดับ:
  1. `story_tree.json`
  2. `events.json`
  3. random fallback ในโค้ด
- retry API อยู่ใน `SendLlmRequestWithRetryAsync`
- prompt หลักอยู่ใน `SystemPrompt` และ `BuildPrompt`
- ถ้าจะแก้จำนวน event ต่อ chapter ให้ดู `IntroPage.xaml.cs` ตอนสร้าง `GameState`
- ถ้าจะแก้ chapter ถัดไป ให้ดู `GameEngine.AdvanceChapterAsync`

### Resource Pressure Prompt

ไฟล์:

- `TheEndOfMine/Services/LlmGameContentService.cs`

จุดสำคัญ:

- `BuildResourcePressureRule` เพิ่ม instruction ตามค่าน้ำปัจจุบัน
- ถ้า `Thirst <= 40` จะขอให้ LLM เพิ่มตัวเลือกที่ให้ Water
- ถ้า `Thirst <= 25` จะถือว่าน้ำใกล้หมดมาก และบังคับให้ chapter มีโอกาสเจอน้ำชัดเจน
- ต่อให้ LLM ไม่ทำตาม `EnsureBalancedResourceRewards` จะเติม reward น้ำให้เอง

## ระบบ Event และ Choice

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/EventPopup.xaml`
- `TheEndOfMine/Views/EventPopup.xaml.cs`
- `TheEndOfMine/Services/EventService.cs`
- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Services/EventChoiceInventoryGuard.cs`
- `TheEndOfMine/Services/InventoryChoiceEffectService.cs`
- `TheEndOfMine/Services/LlmGameContentService.cs`
- `TheEndOfMine/Models/GameEvent.cs`

ใช้แก้:

- หน้าตา popup เหตุการณ์
- จำนวน choice
- ผล choice ต่อ status
- การให้ของจาก choice
- การใช้ของจาก choice
- การปรับ choice ตามไอเทมในกระเป๋า
- note ที่แสดงว่าไอเทมช่วยลดผลเสีย
- item reward หลายชิ้นต่อ choice

จุดสำคัญ:

- `EventPopup` แสดง event และเรียก callback เมื่อเลือก choice
- `GameEngine.ApplyEventChoice` คือจุด apply ผล choice
- `InventoryChoiceEffectService` เพิ่ม/ลด item จาก choice
- `EventChoiceInventoryGuard` ตรวจว่า choice ใช้ไอเทมที่มีจริงไหม และลดผลเสียถ้ามีไอเทมเหมาะ
- `GameEvent.EventChoice.ItemReward` ยังรองรับของเก่าแบบ 1 ชิ้น
- `GameEvent.EventChoice.ItemRewards` รองรับของใหม่แบบหลายชิ้น
- ใช้ `choice.GetItemRewards()` ทุกครั้งที่ต้องอ่านของรางวัลทั้งหมด
- `ItemsAdd` จาก `story_tree.json` ตอนนี้แปลงเป็น reward ได้หลายชิ้น สูงสุด 3 ชิ้นต่อ choice
- ถ้า result text บอกว่าได้รับ/เก็บ/หยิบ/พบ/เจอไอเทม แต่ LLM ไม่ส่ง reward object มา `TryCreateMentionedReward` จะสร้าง item reward ให้เอง

### Timing ของ Event

ไฟล์:

- `TheEndOfMine/Services/GameEngine.cs`

จุดสำคัญ:

- `EventDurationMinutes = 720` เท่ากับ 1 event ใช้เวลาครึ่งวันในเกม
- กด `GO OUTSIDE` ตอนนี้ยังไม่ลด status ทันที
- status/เวลา event จะลดหลังผู้เล่นเลือกจบ event ใน `ApplyEventChoice`
- ถ้า choice ทำให้ตาย จะเช็กตายจาก choice ก่อน แล้วค่อยเดินเวลา event ถ้ายังรอด

### Choice Effect Scaling

ไฟล์:

- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Services/DifficultyService.cs`

จุดสำคัญ:

- ผลเสียของ choice ถูก scale ด้วย `DamageMultiplier`
- `ScaleNegativeEffect` ใช้กับ HP/Hunger/Thirst ที่เป็นค่าลบ
- `ScaleFatiguePenalty` ใช้กับ fatigue ที่เป็นค่าบวก
- positive HP จะถูก normalize ด้วย `NormalizePositiveHpEffect` ให้ heal ได้เฉพาะกิน/ดื่ม/รักษาจริง

ถ้าจะเพิ่ม rule ว่าไอเทมอะไรช่วย choice แบบไหน:

- ไปแก้ `EventChoiceInventoryGuard.ItemAdvantageRules`

ตัวอย่าง rule ที่มีแล้ว:

- ไฟฉายช่วยที่มืด/อุโมงค์
- ผ้าพันแผล/ยา ช่วยแผล/โดนกัด
- เชือกช่วยปีน/ข้ามช่องว่าง
- แผนที่/เข็มทิศช่วยเรื่องเส้นทาง
- ไขควง/lockpick/คีมช่วยประตู/ล็อก
- มีดช่วยปะทะ/ซอมบี้

## ระบบ Inventory / Item

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/InventoryPage.xaml`
- `TheEndOfMine/Views/InventoryPage.xaml.cs`
- `TheEndOfMine/Models/Inventory.cs`
- `TheEndOfMine/Models/Item.cs`
- `TheEndOfMine/Services/ItemService.cs`
- `TheEndOfMine/Services/ItemRewardConsistencyService.cs`
- `TheEndOfMine/Services/InventoryChoiceEffectService.cs`
- `TheEndOfMine/Resources/Raw/items.json`

ใช้แก้:

- หน้า inventory
- ปุ่มกลับหน้า inventory
- รายละเอียดไอเทม
- ปุ่มใช้ไอเทม
- effect ของไอเทม
- backpack เพิ่มช่องเก็บของ
- durability
- การ normalize item reward จาก LLM
- การ classify ว่าไอเทมไหนเป็น Food/Water/Medicine/Tool

จุดสำคัญ:

- `Item.cs` คือ schema ของไอเทม
- `ItemEffects` มี field เช่น `hunger_restore`, `thirst_restore`, `hp_restore`, `carry_capacity_bonus`
- `Inventory.cs` จัดการช่องเก็บของ
- `Inventory.AddItem(item, expandIfFull: true)` ใช้สำหรับ reward จาก event เพื่อให้ของที่เนื้อเรื่องให้ไม่หายเพราะกระเป๋าเต็ม
- backpack/container เพิ่มช่องผ่าน `carry_capacity_bonus`
- `InventoryPage.ApplyItemEffects` ใช้ของจากหน้า inventory
- `InventoryChoiceEffectService` เพิ่ม item rewards ทั้งหมดจาก `choice.GetItemRewards()`
- `ItemRewardConsistencyService` บังคับชื่อ/alias/ภาพของ item reward ให้ตรงกับ asset ที่มี
- ปุ่มกลับหน้า inventory ใน `InventoryPage.xaml` เป็น `Border` วงกลมพร้อม `TapGestureRecognizer`

### Item Reward Pool / Drop Variety

ไฟล์:

- `TheEndOfMine/Services/LlmGameContentService.cs`
- `TheEndOfMine/Services/ItemRewardConsistencyService.cs`

จุดสำคัญ:

- `FallbackRewardNames` คือ pool หลักของ item fallback ตอนนี้มี 40+ ชิ้น
- `CommonFallbackRewardNames` คือ pool ของจำเป็นช่วง reward แรก ๆ เช่น น้ำ อาหาร ผ้าพันแผล ยา
- `PickFallbackRewardName` ใช้เลือกของ โดยช่วงแรกเอาของจำเป็นก่อน แล้วค่อยสุ่มจาก pool ใหญ่
- `EnsureMinimumItemRewards` เติม reward รวมต่อ chapter ให้ถึงประมาณ 10-14 ชิ้น
- `EnsureBalancedResourceRewards` เติมน้ำ/อาหารขั้นต่ำ
- `GetWaterRewardTarget` เพิ่ม target น้ำถ้าค่าน้ำผู้เล่นต่ำ
- 1 choice สามารถมี reward ได้สูงสุด 3 ชิ้น

### กันไอเทมไม่ใช่น้ำแต่ถูก classify เป็น Water

ไฟล์:

- `TheEndOfMine/Services/ItemRewardConsistencyService.cs`

จุดสำคัญ:

- profile `water_bottle` ไม่ใช้ keyword `น้ำ` เดี่ยว ๆ แล้ว
- ต้องเป็นคำชัดว่าเป็นน้ำดื่ม เช่น `ขวดน้ำ`, `น้ำดื่ม`, `น้ำสะอาด`, `น้ำกรอง`, `กระติกน้ำ`
- `LooksLikeDrinkableWater` กันคำที่ไม่ควรเป็นน้ำดื่ม เช่น `น้ำมัน`, `น้ำยา`, `กันน้ำ`, `ดำน้ำ`, `ว่ายน้ำ`, `น้ำตาล`, `waterproof`, `oil`

ถ้าจะเพิ่มไอเทมใหม่:

1. เพิ่มข้อมูลใน `Resources/Raw/items.json`
2. เพิ่ม alias ใน `Resources/Raw/story_assets.json` ถ้าต้องมีรูป
3. เพิ่มรูปใน `Resources/Images/story/item/`
4. ถ้าเป็นไอเทมที่ LLM ชอบตอบมั่ว ให้เพิ่ม profile ใน `ItemRewardConsistencyService`
5. ถ้าอยากให้ fallback สุ่มเจอด้วย ให้เพิ่มชื่อไทยใน `FallbackRewardNames`

## ระบบ Backpack / Container

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Models/Inventory.cs`
- `TheEndOfMine/Services/ItemRewardConsistencyService.cs`
- `TheEndOfMine/Views/InventoryPage.xaml.cs`
- `TheEndOfMine/Models/Item.cs`

ใช้แก้:

- จำนวนช่องที่ backpack เพิ่ม
- การแสดง effect ช่องเก็บของ
- logic container

จุดสำคัญ:

- backpack ถูก normalize ให้มี `Effects.IsContainer = true`
- backpack มี `CarryCapacityBonus` อย่างน้อย `4`
- `Inventory.ApplyContainerCapacityBonuses` ใช้คำนวณช่องเพิ่ม
- หน้า inventory แสดง `ช่องเก็บของ +N`

## ระบบ Status / Survival / Death

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Services/DifficultyService.cs`
- `TheEndOfMine/Models/GameState.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/MainPage.xaml`

ใช้แก้:

- วิธีลด hunger/thirst/fatigue
- ความเหนื่อยมีผลต่อการกินทรัพยากรและ damage
- ความกระหายมีผลต่อการกินทรัพยากรและ damage
- damage จากหิว/ขาดน้ำ/ติดเชื้อ
- สาเหตุการตาย
- progress bar บน UI

จุดสำคัญ:

- status ลดตาม timer ปกติและตอน event เดินเวลา
- ตอนนอน `Sleep()` ลดแค่น้ำ/อาหารและฟื้น fatigue โดย `applyHpDamage: false`
- ตอนพัก `Rest()` ยังใช้ `AdvanceTimeWithSurvivalCosts` และเช็ก death ตามปกติ
- `ApplySurvivalDamage` ลด HP เมื่อหิว/ขาดน้ำ/ติดเชื้อ/เหนื่อยหนัก
- `ApplyFatigueStrainDamage` ทำให้เหนื่อยหนักมีผลจริง
- `ApplyThirstStrainDamage` ทำให้ขาดน้ำหนักมีผลจริง
- threshold น้ำปัจจุบันถูกแก้ไม่ให้ตีความกลับด้านแล้ว:
  - soft 45
  - mid 30
  - high 15
  - critical 5

## ระบบ Noise / Infection

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Models/GameState.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/MainPage.xaml`

ใช้แก้:

- วิธีเพิ่ม Noise
- วิธีเพิ่ม Infection
- ambush risk จากเสียง
- infection growth/decay
- progress bar บน UI

จุดสำคัญ:

- Noise/Infection อยู่ใน `GameState`
- `GameEngine.ApplyChoiceSurvivalPressure` ประเมินจาก text ของ choice/result
- ยิงปืน/ทุบ/พัง/ตะโกน เพิ่ม Noise
- โดนกัด/แผลสกปรก/สนิม/ศพ/น้ำเน่า เพิ่ม Infection
- ยา/รักษา/พันแผล ลด Infection
- `ApplyNoiseAmbushRisk` มีโอกาสทำ damage ถ้าเสียงสะสมสูง

## ระบบ Game Over / Ending / Respawn

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/GameOverPage.xaml`
- `TheEndOfMine/Views/GameOverPage.xaml.cs`
- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Services/SaveService.cs`
- `TheEndOfMine/Data/GameDatabase.cs`

ใช้แก้:

- UI หน้า game over
- รูป ending
- ข้อความสรุปการตาย/จบเกม
- daily checkpoint
- hard mode permadeath
- restart / respawn
- debug ending

จุดสำคัญ:

- `ApplyStoryEnding` ตั้ง `IsStoryEnding = true`, `GameOverTitle`, `GameOverDetail`, `EndingImagePath`
- `BuildEndingImagePath` เลือกรูป ending ตามเพศ/alias
- หน้า ending ขยายรูปใน `GameOverPage.xaml` ที่ `EndingImageFrame`
- ตอน story ending จะซ่อน `CauseFrame` เพื่อไม่ให้โชว์กล่อง “ฉากจบ / ใช้ฉากจบ...”
- ตอน death ending ยังแสดง `CauseFrame` เป็นสาเหตุการตาย
- daily checkpoint ถูก save ตอนเริ่มวันใหม่
- Hard mode ลบ save
- Easy/Normal กลับ daily checkpoint ได้ถ้ามี
- debug `END` ใน MainPage เรียก `ForceStoryEndingForDebug`

## ระบบรูป / Asset

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Resources/Images/story/`
- `TheEndOfMine/Resources/Raw/story_assets.json`
- `TheEndOfMine/Services/StoryAssetResolver.cs`
- `TheEndOfMine/Services/ImageGenerationService.cs`
- `TheEndOfMine/TheEndOfMine.csproj`

ใช้แก้:

- รูป chapter
- รูป event
- รูป item
- รูป ending
- alias mapping
- prompt สร้างภาพด้วย Gemini

จุดสำคัญ:

- MAUI image resource ห้าม basename ซ้ำกันในโปรเจกต์
- LLM ส่ง `chapter_alias`, `story_alias`, `item.story_alias`
- `StoryAssetResolver` map alias เป็น path จริง
- `ImageGenerationService` สร้างรูป chapter/event/item ที่ไม่มี image path
- item reward หลายชิ้นต้องใช้ `choice.GetItemRewards()` ใน image generation
- ถ้าเพิ่ม alias ใหม่ ต้องเพิ่มทั้ง `story_assets.json` และไฟล์ภาพ

## ระบบวิดีโอพื้นหลัง

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/MainPage.xaml.cs`
- `TheEndOfMine/Views/IntroPage.xaml.cs`
- `TheEndOfMine/Resources/Raw/main_page_male.mp4`
- `TheEndOfMine/Resources/Raw/main_page_female.mp4`
- `TheEndOfMine/Resources/AppIcon/splash.mp4`
- `TheEndOfMine/TheEndOfMine.csproj`

ใช้แก้:

- วิดีโอหน้าหลักตามเพศ
- splash video หลังเปิดแอป
- path asset ของวิดีโอ

จุดสำคัญ:

- Android ใช้ path `file:///android_asset/<file>.mp4`
- วิดีโอถูกเล่นผ่าน `WebView`
- native splash ของ MAUI ยังเป็นภาพนิ่งเท่านั้น

## Navigation

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/AppShell.xaml`
- `TheEndOfMine/AppShell.xaml.cs`
- `TheEndOfMine/App.xaml.cs`
- หน้า `.xaml.cs` ที่เรียก `Navigation.PushAsync` หรือ `Shell.Current.GoToAsync`

ใช้แก้:

- หน้าแรกของแอป
- route ของ Shell
- resume save เข้า MainPage
- เปลี่ยน flow หน้า

จุดสำคัญ:

- `AppShell.xaml` มี ShellContent: Intro, Difficulty, Main
- `AppShell.xaml.cs` auto-continue ถ้ามี save
- `App.xaml.cs` สร้าง `Window(new AppShell())`

## Config / Project File

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/TheEndOfMine.csproj`
- `TheEndOfMine/Platforms/Android/AndroidManifest.xml`
- `TheEndOfMine/Resources/Raw/llm.env`

ใช้แก้:

- target framework
- app icon
- splash image
- resource include
- permission internet
- package reference

จุดสำคัญ:

- `MauiAsset Include="Resources\Raw\**"` ทำให้ `events.json`, `story_tree.json`, `llm.env`, mp4 ใน Raw ถูก package
- `Resources\AppIcon\splash.mp4` ถูก include เป็น `splash.mp4`
- Android ต้องมี `INTERNET` permission เพื่อเรียก Typhoon/Gemini

## เมื่อเจอปัญหา

### Typhoon API ใช้ไม่ได้

เช็คไฟล์:

- `Resources/Raw/llm.env`
- `Services/LlmGameContentService.cs`
- `Platforms/Android/AndroidManifest.xml`

อาการที่เจอได้:

- ไม่มี key -> fallback ทันที
- `Socket closed` -> emulator/network หลุดระหว่างรอ response
- JSON parse error -> LLM ตอบไม่ตรง schema
- HTTP 401/403 -> key หรือสิทธิ์ผิด

ระบบมี retry ใน `SendLlmRequestWithRetryAsync`

### Tutorial ไม่ขึ้น

เช็คไฟล์:

- `MainPage.xaml.cs`
- `IntroPage.xaml.cs`

จุดที่ควรดู:

- `Preferences` key `main_tutorial_seen`
- `ShowTutorialIfNeededAsync`
- เริ่มเกมใหม่ต้อง `Preferences.Remove("main_tutorial_seen")`
- กด debug `TUT` ในเมนูสามขีดเพื่อเรียกซ้ำได้

### ของในกระเป๋าไม่เพิ่ม/ไม่ลด

เช็คไฟล์:

- `Services/GameEngine.cs`
- `Services/InventoryChoiceEffectService.cs`
- `Models/GameEvent.cs`
- `Services/LlmGameContentService.cs`
- `Services/ItemRewardConsistencyService.cs`

จุดที่ควรดู:

- choice มี `ItemReward` หรือ `ItemRewards` ไหม
- ต้องอ่านผ่าน `choice.GetItemRewards()` เพื่อได้ของครบทุกชิ้น
- `ItemsAdd` จาก story tree ถูกแปลงเป็น reward หรือไม่
- result text บอกว่าได้รับของแต่ไม่มี reward object หรือไม่
- `TryCreateMentionedReward` infer ของจากข้อความได้หรือไม่
- text/result พูดถึงไอเทมตรงกับชื่อใน inventory ไหม
- item เป็น usable หรือ durable tool ไหม
- ถ้ากระเป๋าเต็ม reward จาก event ควรใช้ `expandIfFull: true`

### ไอเทมไม่ใช่น้ำแต่เป็น Water

เช็คไฟล์:

- `Services/ItemRewardConsistencyService.cs`

จุดที่ควรดู:

- profile `water_bottle`
- `LooksLikeDrinkableWater`
- blacklist คำเช่น `น้ำมัน`, `น้ำยา`, `กันน้ำ`, `ดำน้ำ`, `น้ำตาล`

### Drop ไอเทมซ้ำแค่ไม่กี่ชิ้น

เช็คไฟล์:

- `Services/LlmGameContentService.cs`

จุดที่ควรดู:

- `FallbackRewardNames`
- `CommonFallbackRewardNames`
- `PickFallbackRewardName`
- `EnsureMinimumItemRewards`
- `EnsureBalancedResourceRewards`

### เปิดแอปแล้วไม่กลับ save

เช็คไฟล์:

- `AppShell.xaml.cs`
- `Data/GameDatabase.cs`
- `Services/SaveService.cs`
- `ViewModels/MainViewModel.cs`

### UI ไม่ขึ้นหรือ binding ไม่เปลี่ยน

เช็คไฟล์:

- XAML ของหน้านั้น
- `.xaml.cs` ของหน้านั้น
- ViewModel ที่เป็น BindingContext

ถ้าเป็น MainPage:

- `MainPage.xaml`
- `MainPage.xaml.cs`
- `ViewModels/MainViewModel.cs`

### XAML เกี่ยวกับ Frame warning/compatibility

แนวทาง:

- ใช้ `Border` แทน `Frame`
- ใช้ `Stroke` แทน `BorderColor`
- ใช้ `Border.StrokeShape` + `RoundRectangle CornerRadius="N"` แทน `CornerRadius`

ตัวอย่าง:

```xml
<Border BackgroundColor="#151310" Stroke="#4E493F" Padding="10">
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8" />
    </Border.StrokeShape>
</Border>
```

## Quick Map

| อยากแก้ | ไปที่ |
| --- | --- |
| splash / tap to start | `Views/SplashPage.xaml`, `Views/SplashPage.xaml.cs` |
| หน้า intro / เลือกเพศ | `Views/IntroPage.xaml`, `Views/IntroPage.xaml.cs` |
| หน้าเลือกความยาก | `Views/DifficultyPage.xaml`, `Views/DifficultyPage.xaml.cs`, `Services/DifficultyService.cs` |
| หน้าเล่นหลัก | `MainPage.xaml`, `MainPage.xaml.cs` |
| tutorial | `MainPage.xaml`, `MainPage.xaml.cs`, `Views/IntroPage.xaml.cs` |
| debug END/TUT | `MainPage.xaml`, `MainPage.xaml.cs`, `ViewModels/MainViewModel.cs`, `Services/GameEngine.cs` |
| event popup | `Views/EventPopup.xaml`, `Views/EventPopup.xaml.cs` |
| inventory UI | `Views/InventoryPage.xaml`, `Views/InventoryPage.xaml.cs` |
| ปุ่มกลับ inventory | `Views/InventoryPage.xaml` |
| item schema/effect | `Models/Item.cs`, `Resources/Raw/items.json` |
| เพิ่มช่องกระเป๋า | `Models/Inventory.cs`, `Services/ItemRewardConsistencyService.cs` |
| choice ใช้/ให้ไอเทม | `Services/InventoryChoiceEffectService.cs`, `Models/GameEvent.cs` |
| itemRewards หลายชิ้น | `Models/GameEvent.cs`, `Services/InventoryChoiceEffectService.cs`, `Services/LlmGameContentService.cs` |
| choice ปรับตามไอเทม | `Services/EventChoiceInventoryGuard.cs` |
| game loop/status/death | `Services/GameEngine.cs` |
| event ใช้เวลาครึ่งวัน | `Services/GameEngine.cs` |
| balance difficulty | `Services/DifficultyService.cs`, `Services/GameEngine.cs` |
| main UI binding | `ViewModels/MainViewModel.cs` |
| Typhoon/OpenAI | `Services/LlmGameContentService.cs`, `Resources/Raw/llm.env` |
| fallback JSON | `Resources/Raw/story_tree.json`, `Resources/Raw/events.json` |
| item drop pool | `Services/LlmGameContentService.cs` |
| item normalize/classify | `Services/ItemRewardConsistencyService.cs` |
| save/load | `Services/SaveService.cs`, `Data/GameDatabase.cs` |
| audio/mute | `Services/AudioFeedbackService.cs` |
| รูป/alias | `Services/StoryAssetResolver.cs`, `Resources/Raw/story_assets.json` |
| image generation | `Services/ImageGenerationService.cs` |
| game over / ending | `Views/GameOverPage.xaml`, `Views/GameOverPage.xaml.cs`, `Services/GameEngine.cs` |
| project resource | `TheEndOfMine.csproj` |
