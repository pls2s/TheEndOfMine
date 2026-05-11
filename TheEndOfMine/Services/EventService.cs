using System.Text.Json;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

/// <summary>
/// จัดการ event ใน story โดยเดินตามลำดับใน events.json
/// index ปัจจุบันเก็บใน GameState.EventIndex เพื่อให้ save/load ได้
/// </summary>
public class EventService
{
    // cache options ไว้ใช้ซ้ำ ไม่สร้างใหม่ทุกครั้ง
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // เก็บ event ทั้งหมดที่โหลดมา (โหลดครั้งเดียวแล้ว cache ไว้)
    private List<GameEvent>? _events;

    /// <summary>
    /// คืน event ถัดไปตาม index ใน story
    /// ถ้าเล่นจบทุก event แล้วจะคืน null (story จบ)
    /// </summary>
    public async Task<GameEvent?> GetNextEventAsync(int currentIndex, IReadOnlyList<GameEvent>? generatedEvents = null)
    {
        if (generatedEvents is { Count: > 0 })
        {
            if (currentIndex >= generatedEvents.Count) return null;
            return generatedEvents[currentIndex];
        }

        // โหลดครั้งแรก ถ้ายังไม่ได้โหลด
        _events ??= await LoadEventsAsync();
        if (_events == null || _events.Count == 0) return null;

        // ถ้า index เกินจำนวน event แสดงว่า story จบแล้ว
        if (currentIndex >= _events.Count) return null;

        return _events[currentIndex];
    }

    /// <summary>
    /// ตรวจว่า story จบแล้วหรือยัง
    /// </summary>
    public async Task<bool> IsStoryCompleteAsync(int currentIndex, IReadOnlyList<GameEvent>? generatedEvents = null)
    {
        if (generatedEvents is { Count: > 0 })
            return currentIndex >= generatedEvents.Count;

        _events ??= await LoadEventsAsync();
        return _events == null || currentIndex >= _events.Count;
    }

    /// <summary>
    /// อ่านไฟล์ events.json จาก app package แล้ว deserialize เป็น List&lt;GameEvent&gt;
    /// คืนค่า null ถ้าไฟล์ไม่มีหรือ JSON ผิดรูปแบบ
    /// </summary>
    private static async Task<List<GameEvent>?> LoadEventsAsync()
    {
        try
        {
            // เปิดไฟล์จาก Resources/Raw/
            using var stream = await FileSystem.OpenAppPackageFileAsync("events.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            // แปลง JSON → List<GameEvent> ตามลำดับใน array
            return JsonSerializer.Deserialize<List<GameEvent>>(json, JsonOptions);
        }
        catch
        {
            // ถ้า error ใดๆ (ไฟล์ไม่มี, JSON พัง) ให้คืน null แทน crash
            return null;
        }
    }
}
