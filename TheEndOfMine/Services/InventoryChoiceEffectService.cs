using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class InventoryChoiceEffectService
{
    public static void Apply(GameState state, EventChoice choice)
    {
        var inventory = state.Survivor.Inventory;

        if (choice.ItemReward != null)
            inventory.AddItem(choice.ItemReward);

        ConsumeMentionedUsableItem(inventory, choice);
        WearMentionedDurableItem(inventory, choice);
    }

    private static void ConsumeMentionedUsableItem(Inventory inventory, EventChoice choice)
    {
        var text = BuildChoiceText(choice);
        if (!MentionsUseAction(text))
            return;

        var item = inventory.GetItems().FirstOrDefault(item => item.IsUsable && MentionsItem(text, item));
        if (item == null)
            return;

        inventory.RemoveItem(item);
    }

    private static void WearMentionedDurableItem(Inventory inventory, EventChoice choice)
    {
        var text = BuildChoiceText(choice);
        if (!MentionsUseAction(text))
            return;

        var item = inventory.GetItems().FirstOrDefault(item => IsDurableTool(item) && MentionsItem(text, item));
        if (item?.Durability == null)
            return;

        item.Durability = Math.Max(0, item.Durability.Value - 1);
        if (item.Durability <= 0)
            inventory.RemoveItem(item);
    }

    private static bool MentionsUseAction(string text)
    {
        return ContainsAny(
            text,
            "ใช้", "กิน", "ดื่ม", "รักษา", "ทำแผล", "พันแผล", "ปฐมพยาบาล",
            "เปิด", "งัด", "แงะ", "ตัด", "ฟัน", "ทุบ", "ส่อง", "ปีน", "ผูก", "ดึง",
            "use", "eat", "drink", "treat", "bandage", "open", "cut", "climb", "tie", "pull");
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

    private static bool MentionsItem(string text, Item item)
    {
        return MentionsToken(text, item.NameTh) ||
               MentionsToken(text, item.NameEn) ||
               MentionsToken(text, item.Id) ||
               MentionsToken(text, item.StoryAlias);
    }

    private static bool MentionsToken(string text, string? token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               text.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildChoiceText(EventChoice choice)
    {
        return $"{choice.Text} {choice.ResultText}";
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        return tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
