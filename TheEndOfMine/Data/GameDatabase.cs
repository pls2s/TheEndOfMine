using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TheEndOfMine.Models;

namespace TheEndOfMine.Data;

public class GameDatabase
{
    private readonly string _saveFolder;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public GameDatabase(string? saveFolder = null)
    {
        if (string.IsNullOrWhiteSpace(saveFolder))
        {
            // Default to %AppData%/TheEndOfMine for desktop; this is safe cross-platform fallback for MAUI too.
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            saveFolder = Path.Combine(baseDir, "TheEndOfMine");
        }

        _saveFolder = saveFolder;
        try { Directory.CreateDirectory(_saveFolder); } catch { }
    }

    private string SurvivorPath => Path.Combine(_saveFolder, "survivor.json");
    private string GameStatePath => Path.Combine(_saveFolder, "gamestate.json");
    private string InventoryPath => Path.Combine(_saveFolder, "inventory.json");

    public async Task SaveAsync(Survivor survivor, GameState state, Inventory? inventory = null)
    {
        if (survivor is null) throw new ArgumentNullException(nameof(survivor));
        if (state is null) throw new ArgumentNullException(nameof(state));

        try
        {
            var survJson = JsonSerializer.Serialize(survivor, _jsonOptions);
            await File.WriteAllTextAsync(SurvivorPath, survJson).ConfigureAwait(false);

            var stateJson = JsonSerializer.Serialize(state, _jsonOptions);
            await File.WriteAllTextAsync(GameStatePath, stateJson).ConfigureAwait(false);

            if (inventory != null)
            {
                var invJson = JsonSerializer.Serialize(inventory, _jsonOptions);
                await File.WriteAllTextAsync(InventoryPath, invJson).ConfigureAwait(false);
            }
        }
        catch
        {
            // swallow IO errors; callers can surface if needed
        }
    }

    public async Task<(Survivor? survivor, GameState? state, Inventory? inventory)> LoadAsync()
    {
        Survivor? survivor = null;
        GameState? state = null;
        Inventory? inventory = null;

        try
        {
            if (File.Exists(SurvivorPath))
            {
                var s = await File.ReadAllTextAsync(SurvivorPath).ConfigureAwait(false);
                survivor = JsonSerializer.Deserialize<Survivor>(s, _jsonOptions);
            }

            if (File.Exists(GameStatePath))
            {
                var s = await File.ReadAllTextAsync(GameStatePath).ConfigureAwait(false);
                state = JsonSerializer.Deserialize<GameState>(s, _jsonOptions);
            }

            if (File.Exists(InventoryPath))
            {
                var s = await File.ReadAllTextAsync(InventoryPath).ConfigureAwait(false);
                inventory = JsonSerializer.Deserialize<Inventory>(s, _jsonOptions);
            }
        }
        catch
        {
            // ignore deserialization errors and return nulls so caller can handle
        }

        return (survivor, state, inventory);
    }

    public void HandleDeathPersistence(GameState state)
    {
        try
        {
            if (state != null && state.Difficulty == TheEndOfMine.Models.Difficulty.Hard)
            {
                DeleteAllSaves();
            }
        }
        catch { }
    }

    public void DeleteAllSaves()
    {
        TryDelete(SurvivorPath);
        TryDelete(GameStatePath);
        TryDelete(InventoryPath);
    }

    private void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
