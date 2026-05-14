using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class InventoryChoiceEffectService
{
    public static void Apply(GameState state, EventChoice choice)
    {
        var inventory = state.Survivor.Inventory;

        var consumedItem = ConsumeExplicitItem(inventory, choice);
        WearExplicitDurableItem(inventory, choice, consumedItem);

        foreach (var item in choice.GetItemRewards())
            inventory.AddItem(item, expandIfFull: true);
    }

    private static Item? ConsumeExplicitItem(Inventory inventory, EventChoice choice)
    {
        var item = FindInventoryItem(inventory, choice.ConsumedItemId);
        if (item == null)
            return null;

        inventory.RemoveItem(item);
        return item;
    }

    private static void WearExplicitDurableItem(Inventory inventory, EventChoice choice, Item? consumedItem)
    {
        var item = FindInventoryItem(inventory, choice.UsedItemId);
        if (item == null || ReferenceEquals(item, consumedItem) || item.Durability == null || !IsDurableTool(item))
            return;

        item.Durability = Math.Max(0, item.Durability.Value - 1);
        if (item.Durability <= 0)
            inventory.RemoveItem(item);
    }

    private static Item? FindInventoryItem(Inventory inventory, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        var token = itemId.Trim();
        var items = inventory.GetItems().ToList();

        return items.FirstOrDefault(item =>
                   MatchesItemToken(token, item.Id) ||
                   MatchesItemToken(token, item.StoryAlias) ||
                   MatchesItemToken(token, item.NameTh) ||
                   MatchesItemToken(token, item.NameEn))
               ?? items.FirstOrDefault(item =>
                   ContainsItemToken(token, item.Id) ||
                   ContainsItemToken(token, item.StoryAlias) ||
                   ContainsItemToken(token, item.NameTh) ||
                   ContainsItemToken(token, item.NameEn));
    }

    private static bool IsDurableTool(Item item)
    {
        var category = item.Category.ToLowerInvariant();
        var effects = item.Effects;
        return category is "tool" or "weapon" ||
               effects?.IsGeneralTool == true ||
               effects?.IsLightSource == true ||
               effects?.IsRope == true ||
               effects?.IsSurvivalTool == true ||
               effects?.DmgMin > 0 ||
               effects?.DmgMax > 0;
    }

    private static bool MatchesItemToken(string token, string? itemValue)
    {
        return !string.IsNullOrWhiteSpace(itemValue) &&
               string.Equals(token, itemValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsItemToken(string token, string? itemValue)
    {
        return !string.IsNullOrWhiteSpace(itemValue) &&
               itemValue.Contains(token, StringComparison.OrdinalIgnoreCase);
    }
}
