using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class ThaiNarrativeTextNormalizer
{
    public static void Normalize(GameEvent gameEvent)
    {
        gameEvent.Title = Normalize(gameEvent.Title);
        gameEvent.Description = Normalize(gameEvent.Description);

        foreach (var choice in gameEvent.Choices)
        {
            choice.Text = Normalize(choice.Text);
            choice.ResultText = Normalize(choice.ResultText);
        }
    }

    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text
            .Replace("เสียงร้องขอช่วยเหลือ", "เสียงร้องขอความช่วยเหลือ", StringComparison.Ordinal)
            .Replace("ร้องขอช่วยเหลือ", "ร้องขอความช่วยเหลือ", StringComparison.Ordinal)
            .Replace("ขอช่วยเหลือ", "ขอความช่วยเหลือ", StringComparison.Ordinal)
            .Replace("คนนึ่ง", "คนหนึ่ง", StringComparison.Ordinal)
            .Replace("คนนึง", "คนหนึ่ง", StringComparison.Ordinal)
            .Replace("มีดสนิม", "มีดขึ้นสนิม", StringComparison.Ordinal)
            .Replace("กลิ่นเน่าเหม็นคละคลัง", "กลิ่นเน่าเหม็นคลุ้ง", StringComparison.Ordinal)
            .Replace("กลิ่นเหม็นคละคลัง", "กลิ่นเหม็นคลุ้ง", StringComparison.Ordinal)
            .Replace("เปิดประตูช้า ๆ ด้วยเท้า", "ค่อย ๆ ผลักประตูให้เปิดออก", StringComparison.Ordinal)
            .Replace("เปิดประตูช้าๆ ด้วยเท้า", "ค่อย ๆ ผลักประตูให้เปิดออก", StringComparison.Ordinal)
            .Replace("เปิดประตูด้วยเท้า", "ผลักประตูให้เปิดออกอย่างระวัง", StringComparison.Ordinal)
            .Replace("เปิดไฟฉาย พบศพ", "เปิดไฟฉายสำรวจ พบร่าง", StringComparison.Ordinal)
            .Replace("พบศพผู้หญิง", "พบร่างของผู้หญิง", StringComparison.Ordinal)
            .Replace("น้ำหนักของความระมัดระวัง", "สัญชาตญาณระวังภัย", StringComparison.Ordinal)
            .Replace("หยิบมันขึ้นมา — ดูเหมือนเธอจะพยายามหนี", "เก็บมันขึ้นมา ก่อนสังเกตว่าเธออาจพยายามหนี", StringComparison.Ordinal);

        normalized = NormalizeSpacing(normalized);
        return normalized;
    }

    private static string NormalizeSpacing(string text)
    {
        return text
            .Replace("  ", " ", StringComparison.Ordinal)
            .Replace(" ๆ", " ๆ", StringComparison.Ordinal)
            .Trim();
    }
}
