using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class EventChoiceInventoryGuard
{
    private static readonly ToolUseRule[] ToolUseRules =
    [
        new("ชะแลง", ["ชะแลง", "ชะเเลง", "crowbar"], "ค่อย ๆ ตรวจจุดเปิดด้วยมือเปล่าอย่างระวัง"),
        new("ไขควง", ["ไขควง", "screwdriver"], "ลองหมุนหรือเขย่าจุดล็อกอย่างระวัง"),
        new("คีม", ["คีม", "pliers"], "ค่อย ๆ แกะส่วนที่ติดอยู่ด้วยมือเปล่า"),
        new("เชือก", ["เชือก", "rope"], "มองหาเส้นทางที่ลงได้โดยไม่ต้องใช้เชือก"),
        new("ไฟฉาย", ["ไฟฉาย", "flashlight", "torch"], "ค่อย ๆ ใช้แสงรอบตัวสำรวจทางข้างหน้า"),
        new("มีด", ["มีด", "knife"], "ถอยมาตั้งหลักและเลี่ยงการปะทะตรง ๆ"),
        new("ขวาน", ["ขวาน", "axe"], "มองหาทางอ้อมแทนการพังทางเข้า"),
        new("ค้อน", ["ค้อน", "hammer"], "ค่อย ๆ ตรวจรอยแตกและหาทางเปิดที่ปลอดภัยกว่า")
    ];

    private static readonly string[] ToolActionWords =
    [
        "ใช้", "งัด", "แงะ", "เปิด", "ตัด", "ฟัน", "ทุบ", "พัง", "ส่อง", "ปีน", "ผูก", "ดึง"
    ];

    public static void Normalize(GameEvent gameEvent, Inventory? inventory)
    {
        var items = inventory?.GetItems().ToList() ?? [];
        foreach (var choice in gameEvent.Choices)
            Normalize(choice, items);
    }

    public static void Normalize(EventChoice choice, Inventory? inventory)
    {
        Normalize(choice, inventory?.GetItems().ToList() ?? []);
    }

    public static void Normalize(EventChoice choice, IReadOnlyCollection<Item> availableItems)
    {
        foreach (var rule in ToolUseRules)
        {
            if (!MentionsAny(choice.Text, rule.Aliases) && !MentionsAny(choice.ResultText, rule.Aliases))
                continue;

            if (!LooksLikeToolUse(choice.Text) && !LooksLikeToolUse(choice.ResultText))
                continue;

            if (HasMatchingItem(availableItems, rule))
                continue;

            choice.Text = RewriteUnavailableToolText(choice.Text, rule);
            choice.ResultText = RewriteUnavailableToolResult(choice.ResultText, rule);
            choice.InventoryEffectNote = $"ไม่มี{rule.DisplayName}: ผลลัพธ์ถูกปรับให้เสี่ยงน้อยลงแต่ไม่ได้ใช้ไอเทม";
        }

        ApplyItemAdvantages(choice, availableItems);
    }

    private static void ApplyItemAdvantages(EventChoice choice, IReadOnlyCollection<Item> availableItems)
    {
        if (availableItems.Count == 0)
            return;

        var text = $"{choice.Text} {choice.ResultText}";
        var best = ItemAdvantageRules
            .Where(rule => MentionsAny(text, rule.Triggers) || MentionsAny(text, rule.ItemAliases))
            .Select(rule => new { Rule = rule, Item = FindMatchingItem(availableItems, rule.ItemAliases) })
            .FirstOrDefault(match => match.Item != null);

        if (best == null)
            return;

        choice.HpEffect = ImproveNegative(choice.HpEffect, best.Rule.HpProtection);
        choice.HungerEffect = ImproveNegative(choice.HungerEffect, best.Rule.HungerProtection);
        choice.ThirstEffect = ImproveNegative(choice.ThirstEffect, best.Rule.ThirstProtection);
        choice.FatigueEffect = ImproveNegative(choice.FatigueEffect, best.Rule.FatigueProtection);

        var itemName = GetItemName(best.Item!);
        choice.InventoryEffectNote = $"ใช้ประโยชน์จาก {itemName}: ลดผลเสียของตัวเลือกนี้";

        if (!choice.Text.Contains(itemName, StringComparison.OrdinalIgnoreCase))
            choice.Text = $"{choice.Text} ({itemName})";
    }

    private static Item? FindMatchingItem(IReadOnlyCollection<Item> items, IReadOnlyCollection<string> aliases)
    {
        return items.FirstOrDefault(item =>
        {
            var haystack = $"{item.Id} {item.NameTh} {item.NameEn} {item.Category} {item.Subcategory} {item.StoryAlias}";
            return aliases.Any(alias => haystack.Contains(alias, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static float ImproveNegative(float value, float protection)
    {
        if (value >= 0 || protection <= 0)
            return value;

        return Math.Min(0, value + protection);
    }

    private static string GetItemName(Item item)
    {
        return string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh;
    }

    private static bool LooksLikeToolUse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return ToolActionWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MentionsAny(string text, IReadOnlyCollection<string> aliases)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return aliases.Any(alias => text.Contains(alias, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasMatchingItem(IReadOnlyCollection<Item> items, ToolUseRule rule)
    {
        return items.Any(item =>
        {
            var haystack = $"{item.Id} {item.NameTh} {item.NameEn} {item.Category} {item.Subcategory} {item.StoryAlias}";
            return rule.Aliases.Any(alias => haystack.Contains(alias, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static string RewriteUnavailableToolText(string text, ToolUseRule rule)
    {
        if (string.IsNullOrWhiteSpace(text))
            return rule.FallbackChoiceText;

        var rewritten = text;
        foreach (var alias in rule.Aliases)
            rewritten = rewritten.Replace(alias, "มือเปล่า", StringComparison.OrdinalIgnoreCase);

        if (LooksLikeForcedToolUse(rewritten))
            return rule.FallbackChoiceText;

        return rewritten;
    }

    private static string RewriteUnavailableToolResult(string text, ToolUseRule rule)
    {
        var fallback = $"คุณไม่มี{rule.DisplayName}ติดตัว จึงไม่ฝืนใช้ของที่ไม่มี และเปลี่ยนเป็นค่อย ๆ ตรวจทางด้วยความระวัง";
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        var rewritten = text;
        foreach (var alias in rule.Aliases)
            rewritten = rewritten.Replace(alias, "เครื่องมือที่ไม่มีติดตัว", StringComparison.OrdinalIgnoreCase);

        return LooksLikeForcedToolUse(rewritten)
            ? fallback
            : rewritten;
    }

    private static bool LooksLikeForcedToolUse(string text)
    {
        return text.Contains("ใช้มือเปล่า", StringComparison.OrdinalIgnoreCase) &&
               (text.Contains("งัด", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ตัด", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ทุบ", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("พัง", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ส่อง", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record ToolUseRule(string DisplayName, IReadOnlyCollection<string> Aliases, string FallbackChoiceText);

    private static readonly ItemAdvantageRule[] ItemAdvantageRules =
    [
        new(
            ["มืด", "ความมืด", "ไฟดับ", "ใต้ดิน", "อุโมงค์", "dark", "tunnel"],
            ["ไฟฉาย", "flashlight", "torch"],
            HpProtection: 6,
            HungerProtection: 0,
            ThirstProtection: 0,
            FatigueProtection: 8),
        new(
            ["แผล", "เลือด", "บาด", "กัด", "เจ็บ", "ติดเชื้อ", "wound", "bite"],
            ["ผ้าพันแผล", "ชุดปฐมพยาบาล", "น้ำยาฆ่าเชื้อ", "bandage", "first_aid", "antiseptic", "medicine"],
            HpProtection: 10,
            HungerProtection: 0,
            ThirstProtection: 0,
            FatigueProtection: 0),
        new(
            ["ปีน", "ข้าม", "หลุม", "ช่องว่าง", "ดาดฟ้า", "สะพาน", "climb", "gap", "roof"],
            ["เชือก", "rope"],
            HpProtection: 8,
            HungerProtection: 0,
            ThirstProtection: 0,
            FatigueProtection: 10),
        new(
            ["หลง", "ทาง", "แผนที่", "ทิศ", "route", "map"],
            ["แผนที่", "เข็มทิศ", "map", "compass"],
            HpProtection: 0,
            HungerProtection: 4,
            ThirstProtection: 4,
            FatigueProtection: 8),
        new(
            ["ประตู", "ล็อก", "กลอน", "กุญแจ", "ตู้", "lock", "door"],
            ["ไขควง", "ชุดสะเดาะกุญแจ", "คีม", "screwdriver", "lockpick", "pliers"],
            HpProtection: 4,
            HungerProtection: 0,
            ThirstProtection: 0,
            FatigueProtection: 8),
        new(
            ["ซอมบี้", "โจมตี", "กัด", "ปะทะ", "zombie", "attack"],
            ["มีด", "มีดพับ", "มีดพร้า", "knife", "machete"],
            HpProtection: 10,
            HungerProtection: 0,
            ThirstProtection: 0,
            FatigueProtection: 5)
    ];

    private sealed record ItemAdvantageRule(
        IReadOnlyCollection<string> Triggers,
        IReadOnlyCollection<string> ItemAliases,
        float HpProtection,
        float HungerProtection,
        float ThirstProtection,
        float FatigueProtection);
}
