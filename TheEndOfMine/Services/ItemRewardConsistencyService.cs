using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class ItemRewardConsistencyService
{
    private static readonly ItemProfile[] Profiles =
    [
        new("antiseptic", "น้ำยาฆ่าเชื้อ", "Antiseptic", "Medicine", ["น้ำยาฆ่าเชื้อ", "ฆ่าเชื้อ", "antiseptic", "แอลกอฮอล์ล้างแผล"]),
        new("backpack", "กระเป๋าเป้", "Backpack", "Misc", ["กระเป๋าเป้", "backpack"]),
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
        new("water_bottle", "ขวดน้ำ", "Water Bottle", "Water", ["ขวดน้ำ", "น้ำขวด", "น้ำดื่ม", "น้ำสะอาด", "น้ำกรอง", "water bottle", "drinking water"]),
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
        foreach (var item in choice.GetItemRewards())
            Normalize(item, $"{choice.Text} {choice.ResultText}");

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
        item.DescriptionTh = GetProfileDescription(profile.Alias);
        ApplyProfileEffects(item, profile.Alias);
    }

    private static ItemProfile? FindProfile(Item item, string context)
    {
        var itemCoreText = $"{item.NameTh} {item.NameEn} {item.Id} {item.Category} {item.Subcategory} {item.StoryAlias}";
        var itemCoreMatch = BestProfileMatch(itemCoreText);
        if (itemCoreMatch != null)
            return itemCoreMatch;

        var descriptionMatch = BestProfileMatch(item.DescriptionTh);
        if (descriptionMatch != null)
            return descriptionMatch;

        var aliasMatch = Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Alias, item.StoryAlias, StringComparison.OrdinalIgnoreCase));
        if (aliasMatch != null)
            return aliasMatch;

        var contextMatch = BestProfileMatch(RemoveGenericInventoryPhrases(context));
        if (contextMatch != null)
            return contextMatch;

        return item.Category.ToLowerInvariant() switch
        {
            "food" => Profiles.First(profile => profile.Alias == "canned_food"),
            "water" when LooksLikeDrinkableWater(item) => Profiles.First(profile => profile.Alias == "water_bottle"),
            "medicine" or "medical" => Profiles.First(profile => profile.Alias == "first_aid_kit"),
            "weapon" => Profiles.First(profile => profile.Alias == "knife"),
            "tool" => Profiles.First(profile => profile.Alias == "wrench"),
            _ => null
        };
    }

    private static bool LooksLikeDrinkableWater(Item item)
    {
        var text = $"{item.NameTh} {item.NameEn} {item.Id} {item.Subcategory} {item.StoryAlias}".ToLowerInvariant();
        if (ContainsAny(text, "น้ำมัน", "น้ำยา", "กันน้ำ", "ดำน้ำ", "ว่ายน้ำ", "น้ำตาล", "น้ำปลา", "waterproof", "oil"))
            return false;

        return ContainsAny(text, "ขวดน้ำ", "น้ำขวด", "น้ำดื่ม", "น้ำสะอาด", "น้ำกรอง", "น้ำฝน", "กระติกน้ำ", "water bottle", "drinking water", "canteen");
    }

    private static ItemProfile? BestProfileMatch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lowered = text.ToLowerInvariant();
        return Profiles
            .Select(profile => new
            {
                Profile = profile,
                Score = profile.Keywords.Count(keyword => lowered.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .FirstOrDefault()
            ?.Profile;
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        return tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string RemoveGenericInventoryPhrases(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text
            .Replace("ใส่กระเป๋า", "", StringComparison.OrdinalIgnoreCase)
            .Replace("เก็บเข้ากระเป๋า", "", StringComparison.OrdinalIgnoreCase)
            .Replace("เก็บใส่กระเป๋า", "", StringComparison.OrdinalIgnoreCase)
            .Replace("ในกระเป๋า", "", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetProfileDescription(string alias)
    {
        return alias switch
        {
            "antiseptic" => "น้ำยาฆ่าเชื้อสำหรับล้างแผล ลดความเสี่ยงติดเชื้อหลังสัมผัสสิ่งสกปรก",
            "backpack" => "กระเป๋าเป้สำหรับขยายช่องเก็บของทันทีเมื่อได้รับ ไม่กินช่องในกระเป๋าหลักและใช้รักษาแผลหรือกินดื่มไม่ได้",
            "bandage" => "ผ้าพันแผลขนาดเล็ก ใช้พันแผลหรือหยุดเลือดเบื้องต้น ใช้แล้วไม่สามารถนำกลับมาใช้ใหม่ได้",
            "battery_pack" => "แบตเตอรี่สำรองสำหรับชาร์จหรือจ่ายไฟให้อุปกรณ์ขนาดเล็ก",
            "binoculars" => "กล้องส่องทางไกล ใช้มองสำรวจพื้นที่ไกล ๆ ก่อนตัดสินใจเคลื่อนที่",
            "blanket" => "ผ้าห่มเก่าแต่ยังใช้งานได้ ช่วยกันหนาวและพักฟื้นตอนหยุดพัก",
            "canned_food" => "อาหารกระป๋องที่ยังพอกินได้ ใช้เพิ่มค่าอาหารเมื่อเปิดกิน",
            "canteen" => "กระติกน้ำสำหรับพกน้ำดื่ม ช่วยให้เดินทางได้นานขึ้น",
            "compass" => "เข็มทิศสำหรับจับทิศทาง ช่วยลดโอกาสหลงทางในพื้นที่รกร้าง",
            "cookpot" => "หม้อสนามสำหรับต้มน้ำหรืออุ่นอาหารเมื่อมีแหล่งความร้อน",
            "first_aid_kit" => "ชุดปฐมพยาบาล มีอุปกรณ์รักษาแผลพื้นฐาน ช่วยฟื้นพลังชีวิตและลดความเสี่ยงติดเชื้อ",
            "flare" => "พลุสัญญาณสำหรับขอความช่วยเหลือหรือเบี่ยงเบนความสนใจในระยะไกล",
            "flashlight" => "ไฟฉายสำหรับส่องทางในพื้นที่มืด ต้องระวังเรื่องแบตเตอรี่และเสียงจากการใช้งาน",
            "fuel_can" => "ถังน้ำมันสำหรับเติมเชื้อเพลิงหรือใช้กับอุปกรณ์ที่ต้องการน้ำมัน",
            "gloves" => "ถุงมือช่วยป้องกันมือจากเศษแก้ว สนิม และคราบสกปรก",
            "helmet" => "หมวกนิรภัยช่วยป้องกันศีรษะจากเศษซากหรือของตกใส่",
            "knife" => "มีดพับขนาดเล็ก ใช้ตัดของ ป้องกันตัว หรือจัดการสิ่งกีดขวางบางอย่าง",
            "knife_sheath" => "ปลอกมีดสำหรับเก็บมีดให้ปลอดภัยและหยิบใช้ได้เร็วขึ้น",
            "lighter" => "ไฟแช็กสำหรับจุดไฟ ใช้กับการก่อไฟหรือให้แสงชั่วคราว",
            "lockpick_set" => "ชุดสะเดาะกุญแจ ใช้เปิดล็อกบางประเภทโดยไม่ทำเสียงดังมาก",
            "machete" => "มีดพร้าคม ใช้ตัดสิ่งกีดขวางหรือป้องกันตัวได้ดีกว่ามีดเล็ก",
            "map" => "แผนที่เก่าของพื้นที่ ช่วยวางเส้นทางและหาจุดสำคัญ",
            "mask" => "หน้ากากช่วยกรองฝุ่น ควัน หรือกลิ่นปนเปื้อนบางส่วน",
            "matches" => "ไม้ขีดไฟ ใช้จุดไฟได้แต่มีจำนวนจำกัดและต้องเก็บให้แห้ง",
            "medicine_bottle" => "ขวดยาสำหรับรักษาอาการเจ็บป่วยเบื้องต้น ใช้เมื่อจำเป็น",
            "painkillers" => "ยาแก้ปวด ลดอาการบาดเจ็บและช่วยให้เคลื่อนไหวต่อได้ชั่วคราว",
            "pliers" => "คีมสำหรับหนีบ ดึง หรือตัดลวดในงานซ่อมและเปิดทาง",
            "radio" => "วิทยุสื่อสารสำหรับรับสัญญาณหรือขอความช่วยเหลือเมื่อมีแบตเตอรี่",
            "radio_battery" => "แบตเตอรี่วิทยุ ใช้เป็นพลังงานสำรองให้วิทยุหรืออุปกรณ์สื่อสาร",
            "rope_coil" => "ม้วนเชือก ใช้ผูก ลาก ปีน หรือทำทางผ่านจุดอันตราย",
            "screwdriver" => "ไขควงสำหรับขันน็อต เปิดฝาครอบ หรือแกะอุปกรณ์บางอย่าง",
            "sewing_kit" => "ชุดเย็บแผล ใช้ปิดแผลลึกหรือซ่อมผ้าในสถานการณ์จำเป็น",
            "sleeping_bag" => "ถุงนอนช่วยให้พักผ่อนได้ดีขึ้นในพื้นที่เย็นหรือพื้นแข็ง",
            "stove" => "เตาพกพาสำหรับอุ่นอาหารหรือต้มน้ำเมื่อมีเชื้อเพลิง",
            "tape_roll" => "เทปพันสายไฟ ใช้ซ่อมของชั่วคราว มัดยึด หรือปิดรอยรั่วเล็ก ๆ",
            "torch" => "คบเพลิงให้แสงสว่างแรงแต่ดึงดูดความสนใจได้ง่าย",
            "water_bottle" => "ขวดน้ำดื่ม ใช้เพิ่มค่าน้ำเมื่อดื่ม",
            "water_filter" => "เครื่องกรองน้ำ ช่วยทำให้น้ำสกปรกปลอดภัยขึ้นก่อนดื่ม",
            "whistle" => "นกหวีดสำหรับส่งสัญญาณเสียงดัง ใช้เรียกคนหรือเบี่ยงเบนความสนใจ",
            "wrench" => "ประแจสำหรับขันน็อต ซ่อมอุปกรณ์ หรือใช้เป็นอาวุธฉุกเฉิน",
            _ => "ไอเทมเอาตัวรอดที่พบจากเหตุการณ์"
        };
    }

    private static void ApplyProfileEffects(Item item, string alias)
    {
        item.Effects ??= new ItemEffects();
        ResetProfileLockedEffects(item.Effects);

        switch (alias)
        {
            case "canned_food":
                item.Effects.HungerRestore = Math.Max(34f, item.Effects.HungerRestore.GetValueOrDefault());
                item.Effects.OneTimeUse = true;
                return;
            case "water_bottle":
            case "canteen":
                item.Effects.ThirstRestore = Math.Max(38f, item.Effects.ThirstRestore.GetValueOrDefault());
                item.Effects.OneTimeUse = true;
                return;
            case "first_aid_kit":
                item.Effects.HpRestore = Math.Max(24f, item.Effects.HpRestore.GetValueOrDefault());
                item.Effects.BiteInfectionReduce = Math.Max(10f, item.Effects.BiteInfectionReduce.GetValueOrDefault());
                item.Effects.OneTimeUse = true;
                return;
            case "bandage":
            case "antiseptic":
            case "medicine_bottle":
            case "painkillers":
            case "sewing_kit":
                item.Effects.HpRestore = Math.Max(16f, item.Effects.HpRestore.GetValueOrDefault());
                item.Effects.BiteInfectionReduce = Math.Max(6f, item.Effects.BiteInfectionReduce.GetValueOrDefault());
                item.Effects.OneTimeUse = true;
                return;
            case "backpack":
                item.Effects.IsContainer = true;
                item.Effects.CarryCapacityBonus = Math.Max(4f, item.Effects.CarryCapacityBonus.GetValueOrDefault());
                item.WeightKg = item.WeightKg <= 0 ? 1.1f : item.WeightKg;
                item.DurabilityMax ??= 1;
                item.Durability ??= item.DurabilityMax;
                return;
        }
    }

    private static void ResetProfileLockedEffects(ItemEffects effects)
    {
        effects.HpRestore = 0;
        effects.HungerRestore = 0;
        effects.ThirstRestore = 0;
        effects.FatigueRestore = 0;
        effects.BiteInfectionReduce = 0;
        effects.OneTimeUse = false;
        effects.CarryCapacityBonus = 0;
        effects.IsContainer = false;
    }

    private static void EnsureResultMentionsReward(EventChoice choice)
    {
        var rewardNames = choice.GetItemRewards()
            .Select(item => string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rewardNames.Count == 0)
            return;

        if (rewardNames.All(rewardName => choice.ResultText.Contains(rewardName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var rewardText = string.Join(", ", rewardNames);
        choice.ResultText = string.IsNullOrWhiteSpace(choice.ResultText)
            ? $"คุณเก็บ {rewardText} ใส่กระเป๋า"
            : $"{choice.ResultText}\nคุณเก็บ {rewardText} ใส่กระเป๋า";
    }

    private sealed record ItemProfile(
        string Alias,
        string NameTh,
        string NameEn,
        string Category,
        IReadOnlyCollection<string> Keywords);
}
