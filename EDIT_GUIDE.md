# Edit Guide - The End of Mine

เอกสารนี้เป็นคู่มือว่า “ถ้าจะแก้อะไร ต้องไปแก้ไฟล์ไหน” สำหรับโปรเจกต์ .NET MAUI เกม The End of Mine

## กฎก่อนแก้

- โค้ดหลักอยู่ใน `TheEndOfMine/`
- UI หลักใช้ XAML คู่กับ `.xaml.cs`
- Logic เกมส่วนใหญ่แยกอยู่ใน `Services/`
- State ที่ต้อง save/load อยู่ใน `Models/`
- Resource ที่ต้องถูก package เข้าแอปอยู่ใน `Resources/`
- หลังแก้ควรทดสอบด้วยคำสั่ง:

```bash
dotnet build TheEndOfMine/TheEndOfMine.csproj -f net9.0-android --no-restore
```

## หน้าและ Flow หลัก

### หน้าเริ่มเกม / splash / สร้างตัวละคร

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/IntroPage.xaml`
- `TheEndOfMine/Views/IntroPage.xaml.cs`
- `TheEndOfMine/Resources/AppIcon/splash.mp4`
- `TheEndOfMine/TheEndOfMine.csproj`

ใช้แก้:

- วิดีโอ splash หลังเปิดแอป
- หน้า tap to start
- ปุ่มเลือกเพศ
- ช่องกรอกชื่อ
- ปุ่มเริ่มเกม
- loading ตอนสร้างเกมใหม่
- การเรียก LLM เพื่อสร้าง Chapter 1

จุดสำคัญ:

- `IntroPage.xaml.cs` เรียก `LlmGameContentService.GenerateNewGameAsync`
- ถ้าเริ่มเกมใหม่ จะลบ save เก่าใน `SaveService` และ `GameDatabase`
- วิดีโอ splash ถูกโหลดผ่าน `WebView` ไม่ใช่ native splash ของ MAUI

### หน้าเลือกความยาก

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/DifficultyPage.xaml`
- `TheEndOfMine/Views/DifficultyPage.xaml.cs`
- `TheEndOfMine/Services/DifficultyService.cs`

ใช้แก้:

- UI ปุ่ม Easy / Normal / Hard
- ค่าความยากที่บันทึกลง save
- อัตราลด hunger/thirst/fatigue
- rule ของ hard mode

### หน้าเล่นหลัก

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/MainPage.xaml`
- `TheEndOfMine/MainPage.xaml.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/Services/GameEngine.cs`

ใช้แก้:

- layout หน้าหลัก
- ปุ่ม `GO OUTSIDE`
- ปุ่ม `REST / SLEEP`
- ปุ่ม inventory
- เมนูสามขีด
- ปุ่ม save
- ปุ่ม sound on/off
- tutorial overlay
- loading overlay เข้า chapter
- background video ชาย/หญิง

จุดสำคัญ:

- UI binding ส่วนใหญ่ผูกกับ `MainViewModel`
- ปุ่มจริงอยู่ใน `MainPage.xaml`
- handler ของปุ่มอยู่ใน `MainPage.xaml.cs`
- logic เกมจริงอยู่ใน `MainViewModel` และ `GameEngine`
- tutorial step-by-step อยู่ใน `MainPage.xaml.cs` ผ่าน `TutorialStep`

## ระบบ Tutorial

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/MainPage.xaml`
- `TheEndOfMine/MainPage.xaml.cs`

ใช้แก้:

- ข้อความ tutorial
- จำนวน step
- target ที่ highlight
- ปุ่ม next/skip
- flag ไม่ให้แสดงซ้ำ

จุดสำคัญ:

- key ที่ใช้จำว่าเคยเห็นแล้วคือ `main_tutorial_seen`
- แก้ step ได้ใน `ShowTutorialIfNeededAsync`
- target เช่น `StatusPanel`, `GoOutsideButton`, `RestPanel`, `InventoryButton`, `MenuButton`
- ถ้าเพิ่ม UI ใหม่แล้วอยากให้ tutorial ชี้ ต้องตั้ง `x:Name` ใน XAML ก่อน

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

จุดสำคัญ:

- `SaveService` เก็บ `checkpoint.json` และ `daily_checkpoint.json`
- `GameDatabase` เก็บ `survivor.json`, `gamestate.json`, `inventory.json`
- ทั้งสองระบบใช้ `FileSystem.AppDataDirectory`
- `AppShell.xaml.cs` ตรวจ save แล้วเข้า `MainPage` อัตโนมัติ
- autosave อยู่ใน `MainViewModel.QueueAutoSave`
- ปุ่ม save เรียก `MainViewModel.SaveGameAsync`

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

จุดสำคัญ:

- provider default คือ Typhoon
- config อยู่ใน `Resources/Raw/llm.env`
- ถ้า Typhoon ล่ม ระบบ fallback ตามลำดับ:
  1. `story_tree.json`
  2. `events.json`
  3. random fallback ในโค้ด
- retry API อยู่ใน `SendLlmRequestWithRetryAsync`
- prompt หลักอยู่ใน `SystemPrompt` และ `BuildPrompt`
- ถ้าจะแก้จำนวน event ต่อ chapter ให้ดู `IntroPage.xaml.cs` ตอนสร้าง `GameState`
- ถ้าจะแก้ chapter ถัดไป ให้ดู `GameEngine.AdvanceChapterAsync`

## ระบบ Event และ Choice

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/EventPopup.xaml`
- `TheEndOfMine/Views/EventPopup.xaml.cs`
- `TheEndOfMine/Services/EventService.cs`
- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Services/EventChoiceInventoryGuard.cs`
- `TheEndOfMine/Services/InventoryChoiceEffectService.cs`
- `TheEndOfMine/Models/GameEvent.cs`

ใช้แก้:

- หน้าตา popup เหตุการณ์
- จำนวน choice
- ผล choice ต่อ status
- การให้ของจาก choice
- การใช้ของจาก choice
- การปรับ choice ตามไอเทมในกระเป๋า
- note ที่แสดงว่าไอเทมช่วยลดผลเสีย

จุดสำคัญ:

- `EventPopup` แสดง event และเรียก callback เมื่อเลือก choice
- `GameEngine.ApplyEventChoice` คือจุด apply ผล choice
- `InventoryChoiceEffectService` เพิ่ม/ลด item จาก choice
- `EventChoiceInventoryGuard` ตรวจว่า choice ใช้ไอเทมที่มีจริงไหม และลดผลเสียถ้ามีไอเทมเหมาะ
- `GameEvent.EventChoice.ItemsAdd` รองรับ `items_add` จาก `story_tree.json`

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
- รายละเอียดไอเทม
- ปุ่มใช้ไอเทม
- effect ของไอเทม
- backpack เพิ่มช่องเก็บของ
- durability
- การ normalize item reward จาก LLM

จุดสำคัญ:

- `Item.cs` คือ schema ของไอเทม
- `ItemEffects` มี field เช่น `hunger_restore`, `thirst_restore`, `hp_restore`, `carry_capacity_bonus`
- `Inventory.cs` จัดการช่องเก็บของ
- backpack/container เพิ่มช่องผ่าน `carry_capacity_bonus`
- `InventoryPage.ApplyItemEffects` ใช้ของจากหน้า inventory
- `InventoryChoiceEffectService` ใช้ของจาก event choice
- `ItemRewardConsistencyService` บังคับชื่อ/alias/ภาพของ item reward ให้ตรงกับ asset ที่มี

ถ้าจะเพิ่มไอเทมใหม่:

1. เพิ่มข้อมูลใน `Resources/Raw/items.json`
2. เพิ่ม alias ใน `Resources/Raw/story_assets.json` ถ้าต้องมีรูป
3. เพิ่มรูปใน `Resources/Images/story/item/`
4. ถ้าเป็นไอเทมที่ LLM ชอบตอบมั่ว ให้เพิ่ม profile ใน `ItemRewardConsistencyService`

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

## ระบบ Noise / Infection / Death

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Models/GameState.cs`
- `TheEndOfMine/ViewModels/MainViewModel.cs`
- `TheEndOfMine/MainPage.xaml`

ใช้แก้:

- วิธีเพิ่ม Noise
- วิธีเพิ่ม Infection
- damage จากหิว/ขาดน้ำ/ติดเชื้อ
- สาเหตุการตาย
- progress bar บน UI

จุดสำคัญ:

- Noise/Infection อยู่ใน `GameState`
- `GameEngine.ApplyChoiceSurvivalPressure` ประเมินจาก text ของ choice/result
- `GameEngine.ApplySurvivalDamage` ลด HP จาก starvation/dehydration/infection
- `GameEngine.CheckDeath` เปลี่ยน status เป็น GameOver
- UI progress อยู่ใน `MainPage.xaml`
- Binding อยู่ใน `MainViewModel.UpdateFromState`

## ระบบ Game Over / Respawn

ไฟล์ที่เกี่ยวข้อง:

- `TheEndOfMine/Views/GameOverPage.xaml`
- `TheEndOfMine/Views/GameOverPage.xaml.cs`
- `TheEndOfMine/Services/GameEngine.cs`
- `TheEndOfMine/Services/SaveService.cs`
- `TheEndOfMine/Data/GameDatabase.cs`

ใช้แก้:

- UI หน้า game over
- ข้อความสรุปการตาย/จบเกม
- daily checkpoint
- hard mode permadeath
- restart / respawn

จุดสำคัญ:

- daily checkpoint ถูก save ตอนเริ่มวันใหม่
- Hard mode ลบ save
- Easy/Normal กลับ daily checkpoint ได้ถ้ามี

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
- alias mapping
- prompt สร้างภาพด้วย Gemini

จุดสำคัญ:

- MAUI image resource ห้าม basename ซ้ำกันในโปรเจกต์
- LLM ส่ง `chapter_alias`, `story_alias`, `item.story_alias`
- `StoryAssetResolver` map alias เป็น path จริง
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

### ของในกระเป๋าไม่เพิ่ม/ไม่ลด

เช็คไฟล์:

- `Services/GameEngine.cs`
- `Services/InventoryChoiceEffectService.cs`
- `Models/GameEvent.cs`
- `Services/LlmGameContentService.cs`

จุดที่ควรดู:

- choice มี `ItemReward` หรือ `ItemsAdd` ไหม
- text/result พูดถึงไอเทมตรงกับชื่อใน inventory ไหม
- item เป็น usable หรือ durable tool ไหม

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

## Quick Map

| อยากแก้ | ไปที่ |
| --- | --- |
| หน้า intro | `Views/IntroPage.xaml`, `Views/IntroPage.xaml.cs` |
| หน้าเล่นหลัก | `MainPage.xaml`, `MainPage.xaml.cs` |
| tutorial | `MainPage.xaml`, `MainPage.xaml.cs` |
| event popup | `Views/EventPopup.xaml`, `Views/EventPopup.xaml.cs` |
| inventory UI | `Views/InventoryPage.xaml`, `Views/InventoryPage.xaml.cs` |
| item schema/effect | `Models/Item.cs`, `Resources/Raw/items.json` |
| เพิ่มช่องกระเป๋า | `Models/Inventory.cs` |
| choice ใช้/ให้ไอเทม | `Services/InventoryChoiceEffectService.cs` |
| choice ปรับตามไอเทม | `Services/EventChoiceInventoryGuard.cs` |
| game loop/status/death | `Services/GameEngine.cs` |
| main UI binding | `ViewModels/MainViewModel.cs` |
| Typhoon/OpenAI | `Services/LlmGameContentService.cs`, `Resources/Raw/llm.env` |
| fallback JSON | `Resources/Raw/story_tree.json`, `Resources/Raw/events.json` |
| save/load | `Services/SaveService.cs`, `Data/GameDatabase.cs` |
| audio/mute | `Services/AudioFeedbackService.cs` |
| รูป/alias | `Services/StoryAssetResolver.cs`, `Resources/Raw/story_assets.json` |
| project resource | `TheEndOfMine.csproj` |

