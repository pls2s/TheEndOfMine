using System.Text.Json;
using System.Text.Json.Serialization;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

/// <summary>
/// โหลด items.json + items_catalog จาก story_tree
/// ค้นหา item ด้วย id หรือ story_alias
/// </summary>
public class ItemService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private Dictionary<string, Item>? _itemsById;
    private Dictionary<string, Item>? _itemsByAlias;
    private Dictionary<string, StoryItem>? _storyItems;

    // ---- Load items.json ----

    public async Task LoadItemsAsync()
    {
        if (_itemsById != null) return;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("items.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var doc = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
            var itemsArray = doc.GetProperty("items");
            var items = JsonSerializer.Deserialize<List<Item>>(itemsArray.GetRawText(), JsonOptions) ?? new();

            _itemsById = new Dictionary<string, Item>();
            _itemsByAlias = new Dictionary<string, Item>();

            foreach (var item in items)
            {
                item.Durability = item.DurabilityMax;
                _itemsById[item.Id] = item;
                if (!string.IsNullOrEmpty(item.StoryAlias))
                    _itemsByAlias[item.StoryAlias] = item;
            }
        }
        catch
        {
            _itemsById = new();
            _itemsByAlias = new();
        }
    }

    // ---- Set story items catalog (จาก StoryTree.ItemsCatalog) ----

    public void SetStoryCatalog(Dictionary<string, StoryItem> catalog)
    {
        _storyItems = catalog;
    }

    // ---- Lookup ----

    /// <summary>
    /// หา item ด้วย id (เช่น "wpn_0001") หรือ story_alias (เช่น "kitchen_knife")
    /// </summary>
    public Item? GetItem(string idOrAlias)
    {
        if (_itemsById != null && _itemsById.TryGetValue(idOrAlias, out var byId))
            return byId;
        if (_itemsByAlias != null && _itemsByAlias.TryGetValue(idOrAlias, out var byAlias))
            return byAlias;
        return null;
    }

    /// <summary>
    /// หา story item catalog (สำหรับ display/quick lookup)
    /// </summary>
    public StoryItem? GetStoryItem(string alias)
    {
        if (_storyItems != null && _storyItems.TryGetValue(alias, out var item))
            return item;
        return null;
    }

    /// <summary>
    /// สร้าง Item instance ใหม่ (clone) สำหรับใส่ inventory
    /// </summary>
    public Item? CreateItemInstance(string idOrAlias)
    {
        var template = GetItem(idOrAlias);
        if (template == null) return null;

        return new Item
        {
            Id = template.Id,
            NameTh = template.NameTh,
            NameEn = template.NameEn,
            Category = template.Category,
            Subcategory = template.Subcategory,
            Rarity = template.Rarity,
            WeightKg = template.WeightKg,
            TradeValue = template.TradeValue,
            Stackable = template.Stackable,
            MaxStack = template.MaxStack,
            FoundIn = template.FoundIn,
            DurabilityMax = template.DurabilityMax,
            Durability = template.DurabilityMax,
            Effects = template.Effects,
            DescriptionTh = template.DescriptionTh,
            StoryAlias = template.StoryAlias
        };
    }
}
