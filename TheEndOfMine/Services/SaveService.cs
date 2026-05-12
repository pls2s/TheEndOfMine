using System.Text.Json;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class SaveService
{
    private const string SaveFileName = "checkpoint.json";
    private const string DailySaveFileName = "daily_checkpoint.json";
    private static string SavePath => Path.Combine(FileSystem.AppDataDirectory, SaveFileName);
    private static string DailySavePath => Path.Combine(FileSystem.AppDataDirectory, DailySaveFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public void SaveCheckpoint(GameState state)
    {
        SaveState(state, SavePath);
    }

    public void SaveDailyCheckpoint(GameState state)
    {
        if (state.Difficulty == Difficulty.Hard) return;
        SaveState(state, DailySavePath);
    }

    public async Task<GameState?> LoadCheckpointAsync()
    {
        return await LoadStateAsync(SavePath);
    }

    public async Task<GameState?> LoadDailyCheckpointAsync()
    {
        return await LoadStateAsync(DailySavePath);
    }

    public bool HasSave() => File.Exists(SavePath);
    public bool HasDailyCheckpoint() => File.Exists(DailySavePath);

    public void DeleteSave()
    {
        TryDelete(SavePath);
    }

    public void DeleteDailyCheckpoint()
    {
        TryDelete(DailySavePath);
    }

    public void DeleteAllSaves()
    {
        DeleteSave();
        DeleteDailyCheckpoint();
    }

    private static void SaveState(GameState state, string path)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch { /* ไม่ crash ถ้า save ไม่ได้ */ }
    }

    private static async Task<GameState?> LoadStateAsync(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<GameState>(json, JsonOptions);
        }
        catch { return null; }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
}
