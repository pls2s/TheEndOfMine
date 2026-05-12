# Audio Assets

วางไฟล์เสียงของเกมไว้ในโฟลเดอร์นี้

- `bgm/` เพลงพื้นหลัง
- `sfx/` เสียงเอฟเฟกต์ เช่น คลิก ประตู โจมตี
- `ui/` เสียงของปุ่มและหน้าจอ UI

ไฟล์ในโฟลเดอร์นี้จะถูก bundle เข้าแอปเป็น `MauiAsset` ด้วย path ขึ้นต้น `audio/`
เช่น `Resources/Audio/sfx/click.mp3` จะเปิดในแอปด้วย path `audio/sfx/click.mp3`
