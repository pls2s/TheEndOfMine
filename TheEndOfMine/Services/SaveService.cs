using System.Text.Json;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class SaveService
{
    private const string SaveFileName = "checkpoint.json";
    private static string SavePath => Path.Combine(FileSystem.AppDataDirectory, SaveFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public void SaveCheckpoint(GameState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(SavePath, json);
        }
        catch { /* ไม่ crash ถ้า save ไม่ได้ */ }
    }

    public async Task<GameState?> LoadCheckpointAsync()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            var json = await File.ReadAllTextAsync(SavePath);
            return JsonSerializer.Deserialize<GameState>(json, JsonOptions);
        }
        catch { return null; }
    }

    public bool HasSave() => File.Exists(SavePath);

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}
