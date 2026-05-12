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
        }
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
}
