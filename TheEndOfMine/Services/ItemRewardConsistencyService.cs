using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class ItemRewardConsistencyService
{
    private static readonly ItemProfile[] Profiles =
    [
        new("antiseptic", "น้ำยาฆ่าเชื้อ", "Antiseptic", "Medicine", ["น้ำยาฆ่าเชื้อ", "ฆ่าเชื้อ", "antiseptic", "แอลกอฮอล์ล้างแผล"]),
        new("backpack", "กระเป๋าเป้", "Backpack", "Misc", ["กระเป๋าเป้", "กระเป๋า", "backpack"]),
        new("bandage", "ผ้าพันแผล", "Bandage", "Medicine", ["ผ้าพันแผล", "ผ้าก๊อซ", "bandage"]),
        new("battery_pack", "แบตเตอรี่สำรอง", "Battery Pack", "Tool", ["แบตเตอรี่สำรอง", "แบตสำรอง", "battery pack", "power bank"]),
        new("binoculars", "กล้องส่องทางไกล", "Binoculars", "Tool", ["กล้องส่องทางไกล", "binoculars"]),
        new("blanket", "ผ้าห่ม", "Blanket", "Misc", ["ผ้าห่ม", "blanket"]),
        new("canned_food", "อาหารกระป๋อง", "Canned Food", "Food", ["อาหารกระป๋อง", "กระป๋องอาหาร", "canned food", "อาหาร"]),
        new("canteen", "กระติกน้ำ", "Canteen", "Water", ["กระติกน้ำ", "canteen"]),
        new("compass", "เข็มทิศ", "Compass", "Tool", ["เข็มทิศ", "compass"]),
        new("cookpot", "หม้อสนาม", "Cookpot", "Tool", ["หม้อสนาม", "หม้อ", "cookpot"]),
        new("first_aid_kit", "ชุดปฐมพยาบาล", "First Aid Kit", "Medicine", ["ชุดปฐมพยาบาล", "กล่องปฐมพยาบาล", "first aid", "first_aid"]),
        new("flare", "พลุสัญญาณ", "Flare", "Tool", ["พลุสัญญาณ", "flare"]),
        new("flashlight", "ไฟฉาย", "Flashlight", "Tool", ["ไฟฉาย", "flashlight"]),
        new("fuel_can", "ถังน้ำมัน", "Fuel Can", "Tool", ["ถังน้ำมัน", "น้ำมัน", "fuel can"]),
        new("gloves", "ถุงมือ", "Gloves", "Misc", ["ถุงมือ", "gloves"]),
        new("helmet", "หมวกนิรภัย", "Helmet", "Misc", ["หมวกนิรภัย", "หมวกกันน็อก", "helmet"]),
        new("knife", "มีดพับ", "Knife", "Weapon", ["มีดพับ", "มีด", "knife"]),
        new("knife_sheath", "ปลอกมีด", "Knife Sheath", "Misc", ["ปลอกมีด", "knife sheath"]),
        new("lighter", "ไฟแช็ก", "Lighter", "Tool", ["ไฟแช็ก", "lighter"]),
        new("lockpick_set", "ชุดสะเดาะกุญแจ", "Lockpick Set", "Tool", ["ชุดสะเดาะกุญแจ", "สะเดาะกุญแจ", "lockpick"]),
        new("machete", "มีดพร้า", "Machete", "Weapon", ["มีดพร้า", "machete"]),
        new("map", "แผนที่", "Map", "Tool", ["แผนที่", "map"]),
        new("mask", "หน้ากาก", "Mask", "Misc", ["หน้ากาก", "mask"]),
        new("matches", "ไม้ขีดไฟ", "Matches", "Tool", ["ไม้ขีดไฟ", "matches"]),
        new("medicine_bottle", "ขวดยา", "Medicine Bottle", "Medicine", ["ขวดยา", "ยาเม็ด", "ยา", "medicine bottle"]),
        new("painkillers", "ยาแก้ปวด", "Painkillers", "Medicine", ["ยาแก้ปวด", "painkiller", "painkillers"]),
        new("pliers", "คีม", "Pliers", "Tool", ["คีม", "pliers"]),
        new("radio", "วิทยุสื่อสาร", "Radio", "Tool", ["วิทยุสื่อสาร", "วิทยุ", "radio"]),
        new("radio_battery", "แบตเตอรี่วิทยุ", "Radio Battery", "Tool", ["แบตเตอรี่วิทยุ", "radio battery"]),
        new("rope_coil", "ม้วนเชือก", "Rope Coil", "Tool", ["ม้วนเชือก", "เชือก", "rope"]),
        new("screwdriver", "ไขควง", "Screwdriver", "Tool", ["ไขควง", "screwdriver"]),
        new("sewing_kit", "ชุดเย็บแผล", "Sewing Kit", "Medicine", ["ชุดเย็บแผล", "เข็มเย็บแผล", "sewing kit"]),
        new("sleeping_bag", "ถุงนอน", "Sleeping Bag", "Misc", ["ถุงนอน", "sleeping bag"]),
        new("stove", "เตาพกพา", "Stove", "Tool", ["เตาพกพา", "เตา", "stove"]),
        new("tape_roll", "เทปพันสายไฟ", "Tape Roll", "Tool", ["เทปพันสายไฟ", "เทป", "tape"]),
        new("torch", "คบเพลิง", "Torch", "Tool", ["คบเพลิง", "torch"]),
        new("water_bottle", "ขวดน้ำ", "Water Bottle", "Water", ["ขวดน้ำ", "น้ำขวด", "น้ำดื่ม", "water bottle", "น้ำ"]),
        new("water_filter", "เครื่องกรองน้ำ", "Water Filter", "Tool", ["เครื่องกรองน้ำ", "ที่กรองน้ำ", "water filter"]),
        new("whistle", "นกหวีด", "Whistle", "Tool", ["นกหวีด", "whistle"]),
        new("wrench", "ประแจ", "Wrench", "Tool", ["ประแจ", "wrench"])
    ];

    public static void Normalize(GameEvent gameEvent)
    {
        foreach (var choice in gameEvent.Choices)
            Normalize(choice);
    }

    public static void Normalize(EventChoice choice)
    {
        if (choice.ItemReward == null)
            return;

        Normalize(choice.ItemReward, $"{choice.Text} {choice.ResultText}");
        EnsureResultMentionsReward(choice);
    }

    public static void Normalize(Item item, string context = "")
    {
        var profile = FindProfile(item, context);
        if (profile == null)
            return;

        item.StoryAlias = profile.Alias;
        item.ImagePath = $"story/item/item_{profile.Alias}.png";
        item.NameTh = profile.NameTh;
        item.NameEn = profile.NameEn;
        item.Category = profile.Category;
        item.DescriptionTh = string.IsNullOrWhiteSpace(item.DescriptionTh)
            ? $"ไอเทมประเภท{profile.Category}ที่พบจากเหตุการณ์"
            : item.DescriptionTh;
    }

    private static ItemProfile? FindProfile(Item item, string context)
    {
        var text = $"{item.NameTh} {item.NameEn} {item.Id} {item.Category} {item.Subcategory} {item.DescriptionTh} {context}".ToLowerInvariant();
        var semanticMatch = Profiles
            .Select(profile => new
            {
                Profile = profile,
                Score = profile.Keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .FirstOrDefault();

        if (semanticMatch != null)
            return semanticMatch.Profile;

        var aliasMatch = Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Alias, item.StoryAlias, StringComparison.OrdinalIgnoreCase));
        if (aliasMatch != null)
            return aliasMatch;

        return item.Category.ToLowerInvariant() switch
        {
            "food" => Profiles.First(profile => profile.Alias == "canned_food"),
            "water" => Profiles.First(profile => profile.Alias == "water_bottle"),
            "medicine" or "medical" => Profiles.First(profile => profile.Alias == "first_aid_kit"),
            "weapon" => Profiles.First(profile => profile.Alias == "knife"),
            "tool" => Profiles.First(profile => profile.Alias == "wrench"),
            _ => null
        };
    }

    private static void EnsureResultMentionsReward(EventChoice choice)
    {
        if (choice.ItemReward == null)
            return;

        var rewardName = string.IsNullOrWhiteSpace(choice.ItemReward.NameTh)
            ? choice.ItemReward.NameEn
            : choice.ItemReward.NameTh;

        if (string.IsNullOrWhiteSpace(rewardName) ||
            choice.ResultText.Contains(rewardName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        choice.ResultText = string.IsNullOrWhiteSpace(choice.ResultText)
            ? $"คุณเก็บ {rewardName} ใส่กระเป๋า"
            : $"{choice.ResultText}\nคุณเก็บ {rewardName} ใส่กระเป๋า";
    }

    private sealed record ItemProfile(
        string Alias,
        string NameTh,
        string NameEn,
        string Category,
        IReadOnlyCollection<string> Keywords);
}
